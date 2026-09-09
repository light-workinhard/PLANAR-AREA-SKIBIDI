using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PLANAR_AREA_SKIBIDI
{
    // Everything here is built in code except the background gif, which is still
    // pulled from Form1.resx (the one asset that can't be "generated"). No designer
    // file needed - just drop this .cs (+ Form1.resx) into a new project and go.
    public class Form1 : Form
    {
        // ---------- theme ----------
        private static readonly Color Bg = Color.FromArgb(30, 30, 30);
        private static readonly Color Accent = Color.Lime;
        private static readonly Font FontField = new("Segoe UI", 15F);
        private static readonly Font FontBtn = new("Segoe UI", 9F, FontStyle.Bold);

        // label/box vertical slots for a shape's 1st/2nd/3rd input row
        private static readonly int[] LabelY = { 105, 148, 188 };
        private static readonly int[] BoxY = { 105, 145, 185 };

        private string selectedShape = string.Empty;
        private readonly ToolTip inputToolTip = new();
        private Panel titlePanel;
        private TextBox resultBox;
        private PictureBox previewBox;

        private class ShapeInfo
        {
            public Label[] Labels;
            public TextBox[] Boxes;
            public Func<double[], double> Area;
            public Action<Graphics, Brush, int, int, double[]> Fill;
        }

        private readonly Dictionary<string, ShapeInfo> shapes = new();
        private readonly List<Label> allLabels = new();
        private readonly List<TextBox> allInputBoxes = new();

        public Form1()
        {
            SuspendLayout();
            BuildForm();
            BuildShapeTable();
            HideAllInputs();
            CreateTitlePage();
            ResumeLayout(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) inputToolTip?.Dispose();
            base.Dispose(disposing);
        }

        // ================= UI CONSTRUCTION =================

        private void BuildForm()
        {
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(429, 727);
            FormBorderStyle = FormBorderStyle.Fixed3D;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "PAC";

            // --- the only non-generated piece: the background gif from Form1.resx ---
            var bg = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.StretchImage,
                TabStop = false
            };
            var res = new ComponentResourceManager(typeof(Form1));
            bg.Image = (Image)res.GetObject("pictureBox2.Image");
            Controls.Add(bg);

            // --- shape selector buttons, 3x3 grid ---
            string[] shapeNames = { "Circle", "Square", "Rectangle", "Triangle", "Parallelogram",
                                     "Trapezoid", "Rhombus", "Oval", "Polygon" };
            int[] colX = { 6, 99, 193 };
            int[] rowY = { 6, 35, 67 };
            for (int i = 0; i < shapeNames.Length; i++)
            {
                string name = shapeNames[i];
                Controls.Add(MakeShapeButton(name, new Point(colX[i % 3], rowY[i / 3]), (s, e) => ShowForShape(name)));
            }

            // --- calculate button ---
            var calcBtn = new Button
            {
                BackColor = Accent,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.Black,
                Location = new Point(287, 2),
                Size = new Size(133, 217),
                Text = "CALCULATE",
                UseVisualStyleBackColor = false
            };
            calcBtn.Click += CalculateClick;
            Controls.Add(calcBtn);

            // --- result readout ---
            resultBox = new TextBox
            {
                BackColor = Bg,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 30F, FontStyle.Bold),
                ForeColor = Accent,
                Location = new Point(12, 225),
                Multiline = true,
                ReadOnly = true,
                Size = new Size(408, 81),
                TextAlign = HorizontalAlignment.Center
            };
            Controls.Add(resultBox);

            // --- shape preview canvas ---
            previewBox = new PictureBox
            {
                BackColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(12, 312),
                Size = new Size(408, 408),
                TabStop = false
            };
            Controls.Add(previewBox);

            bg.SendToBack();
        }

        private static Button MakeShapeButton(string text, Point loc, EventHandler onClick)
        {
            var b = new Button
            {
                BackColor = Bg,
                FlatStyle = FlatStyle.Flat,
                Font = FontBtn,
                ForeColor = Accent,
                Location = loc,
                Size = new Size(88, 32),
                Text = text,
                UseVisualStyleBackColor = false
            };
            b.FlatAppearance.BorderColor = Accent;
            b.FlatAppearance.BorderSize = 2;
            b.Click += onClick;
            return b;
        }

        private static Label MakeFieldLabel(string text, int y) => new()
        {
            AutoSize = true,
            BackColor = Bg,
            Font = FontField,
            ForeColor = Accent,
            Location = new Point(6, y),
            Text = text,
            Visible = false
        };

        private TextBox MakeInputBox(int y)
        {
            var tb = new TextBox
            {
                BackColor = Bg,
                Font = FontField,
                ForeColor = Accent,
                Location = new Point(181, y),
                Size = new Size(100, 34),
                Visible = false
            };
            tb.KeyPress += NumericTextBox_KeyPress;
            return tb;
        }

        private void CreateTitlePage()
        {
            titlePanel = new Panel
            {
                Size = new Size(449, 770),
                BackColor = Color.FromArgb(0, 51, 25) // dark green
            };

            Label titleLabel = new()
            {
                Text = "PLANAR" + Environment.NewLine + "AREA" + Environment.NewLine + "CALCULATOR",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                AutoSize = false,
                Size = new Size(449, 200),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };

            Button startButton = new()
            {
                Text = "ENTER",
                Font = new Font("Segoe UI", 18),
                Size = new Size(200, 120),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(0, 51, 25),
                FlatStyle = FlatStyle.Flat
            };
            startButton.FlatAppearance.BorderColor = Color.White;
            startButton.FlatAppearance.BorderSize = 2;
            startButton.Click += (s, e) => titlePanel.Visible = false;

            titlePanel.Controls.Add(titleLabel);
            titlePanel.Controls.Add(startButton);

            void Reposition()
            {
                titleLabel.Left = (titlePanel.Width - titleLabel.Width) / 2;
                titleLabel.Top = (titlePanel.Height / 2) - titleLabel.Height - 20;
                startButton.Left = (titlePanel.Width - startButton.Width) / 2;
                startButton.Top = titleLabel.Bottom + 30;
            }

            titlePanel.Resize += (s, e) => Reposition();
            Reposition();

            Controls.Add(titlePanel);
            titlePanel.BringToFront();
        }

        // ================= SHAPE DATA (geometry + math live together) =================

        private void BuildShapeTable()
        {
            void Add(string name, string[] labelTexts, Func<double[], double> area,
                     Action<Graphics, Brush, int, int, double[]> fill)
            {
                var labels = new Label[labelTexts.Length];
                var boxes = new TextBox[labelTexts.Length];
                for (int i = 0; i < labelTexts.Length; i++)
                {
                    labels[i] = MakeFieldLabel(labelTexts[i], LabelY[i]);
                    boxes[i] = MakeInputBox(BoxY[i]);
                    Controls.Add(labels[i]);
                    Controls.Add(boxes[i]);
                    allLabels.Add(labels[i]);
                    allInputBoxes.Add(boxes[i]);
                }
                shapes[name] = new ShapeInfo { Labels = labels, Boxes = boxes, Area = area, Fill = fill };
            }

            Add("Circle", new[] { "Radius: " },
                v => Math.PI * v[0] * v[0],
                (g, br, w, h, v) =>
                {
                    float d = (float)(2 * v[0]);
                    g.FillEllipse(br, (w - d) / 2, (h - d) / 2, d, d);
                });

            Add("Square", new[] { "Side: " },
                v => v[0] * v[0],
                (g, br, w, h, v) =>
                {
                    float pos = (w - (float)v[0]) / 2;
                    g.FillRectangle(br, pos, pos, (float)v[0], (float)v[0]);
                });

            Add("Rectangle", new[] { "Length: ", "Width: " },
                v => v[0] * v[1],
                (g, br, w, h, v) =>
                {
                    float x = (w - (float)v[0]) / 2, y = (h - (float)v[1]) / 2;
                    g.FillRectangle(br, x, y, (float)v[0], (float)v[1]);
                });

            Add("Triangle", new[] { "Base: ", "Height: " },
                v => 0.5 * v[0] * v[1],
                (g, br, w, h, v) =>
                {
                    double b = v[0], ht = v[1];
                    float x = (w - (float)b) / 2, y = (h - (float)ht) / 2;
                    Point[] pts = { new((int)x, (int)(y + ht)), new((int)(x + b), (int)(y + ht)), new((int)(x + b / 2), (int)y) };
                    g.FillPolygon(br, pts);
                });

            Add("Parallelogram", new[] { "Base: ", "Height: " },
                v => v[0] * v[1],
                (g, br, w, h, v) =>
                {
                    double b = v[0], ht = v[1];
                    float x = (w - (float)b) / 2, y = (h - (float)ht) / 2;
                    int slant = (int)(b / 4);
                    Point[] pts = { new((int)x, (int)y), new((int)(x + b), (int)y), new((int)(x + b - slant), (int)(y + ht)), new((int)(x - slant), (int)(y + ht)) };
                    g.FillPolygon(br, pts);
                });

            Add("Trapezoid", new[] { "Base 1: ", "Base 2: ", "Height: " },
                v => 0.5 * (v[0] + v[1]) * v[2],
                (g, br, w, h, v) =>
                {
                    double b1 = v[0], b2 = v[1], ht = v[2];
                    float x = (w - (float)b1) / 2, y = (h - (float)ht) / 2;
                    float diff = (float)((b1 - b2) / 2);
                    Point[] pts = { new((int)x, (int)(y + ht)), new((int)(x + b1), (int)(y + ht)), new((int)(x + b1 - diff), (int)y), new((int)(x + diff), (int)y) };
                    g.FillPolygon(br, pts);
                });

            Add("Rhombus", new[] { "Diagonal 1: ", "Diagonal 2: " },
                v => 0.5 * v[0] * v[1],
                (g, br, w, h, v) =>
                {
                    double d1 = v[0], d2 = v[1];
                    float xc = w / 2f, yc = h / 2f;
                    Point[] pts = { new((int)xc, (int)(yc - d2 / 2)), new((int)(xc + d1 / 2), (int)yc), new((int)xc, (int)(yc + d2 / 2)), new((int)(xc - d1 / 2), (int)yc) };
                    g.FillPolygon(br, pts);
                });

            Add("Oval", new[] { "Axis A: ", "Axis B: " },
                v => Math.PI * v[0] * v[1],
                (g, br, w, h, v) =>
                {
                    float dA = (float)(2 * v[0]), dB = (float)(2 * v[1]);
                    g.FillEllipse(br, (w - dA) / 2, (h - dB) / 2, dA, dB);
                });

            Add("Polygon", new[] { "Sides: ", "Side Length: ", "Apothem: " },
                v => 0.5 * v[0] * v[1] * v[2],
                (g, br, w, h, v) =>
                {
                    int n = (int)v[0];
                    double side = v[1], apothem = v[2];
                    float xc = w / 2f, yc = h / 2f;
                    float radius = (float)(apothem + side / 2);
                    Point[] pts = new Point[n];
                    for (int i = 0; i < n; i++)
                    {
                        double angle = 2 * Math.PI * i / n - Math.PI / 2;
                        pts[i] = new Point((int)(xc + radius * Math.Cos(angle)), (int)(yc + radius * Math.Sin(angle)));
                    }
                    g.FillPolygon(br, pts);
                });
        }

        // ================= BEHAVIOR =================

        private void HideAllInputs()
        {
            foreach (var lbl in allLabels) lbl.Visible = false;
            foreach (var box in allInputBoxes) box.Visible = false;
        }

        private void ClearAllInputs()
        {
            foreach (var box in allInputBoxes) box.Text = string.Empty;
        }

        private void ShowForShape(string shape)
        {
            HideAllInputs();
            selectedShape = shape;
            resultBox.Text = string.Empty;
            ClearAllInputs();

            if (!shapes.TryGetValue(shape, out var info)) return;
            foreach (var l in info.Labels)
            {
                l.Visible = true;
                l.BringToFront();
            }
            foreach (var b in info.Boxes)
            {
                b.Visible = true;
                b.BringToFront();
            }
            this.Refresh();
        }

        private static bool TryGetValues(ShapeInfo info, out double[] values)
        {
            values = new double[info.Boxes.Length];
            for (int i = 0; i < info.Boxes.Length; i++)
                if (!double.TryParse(info.Boxes[i].Text, out values[i]))
                    return false;
            return true;
        }

        private void DrawShape()
        {
            if (string.IsNullOrEmpty(selectedShape) || !shapes.TryGetValue(selectedShape, out var info)) return;
            if (!TryGetValues(info, out double[] values)) return;

            var bmp = new Bitmap(previewBox.Width, previewBox.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Black);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using Brush brush = new SolidBrush(Color.Lime);
                info.Fill(g, brush, previewBox.Width, previewBox.Height, values);
            }
            previewBox.Image = bmp;
        }

        private void CalculateClick(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedShape) || !shapes.TryGetValue(selectedShape, out var info))
            {
                MessageBox.Show("Please select a shape first.");
                return;
            }

            if (!TryGetValues(info, out double[] values))
            {
                MessageBox.Show("Please enter valid numeric values.");
                return;
            }

            try
            {
                resultBox.Text = info.Area(values).ToString("F2");
                DrawShape();
                ClearAllInputs();
            }
            catch (Exception)
            {
                MessageBox.Show("Please enter valid numeric values.");
            }
        }

        private void NumericTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (sender is not TextBox tb) return;
            if (char.IsControl(e.KeyChar)) return;

            int selStart = tb.SelectionStart;
            int selLen = tb.SelectionLength;
            string current = tb.Text ?? string.Empty;
            string prospective;
            try { prospective = current.Remove(selStart, selLen).Insert(selStart, e.KeyChar.ToString()); }
            catch { prospective = current + e.KeyChar; }

            if (Regex.IsMatch(prospective, "^-?\\d*(?:\\.\\d{0,2})?$")) return;

            e.Handled = true;
            try { inputToolTip.Show("Only numbers allowed. Optional leading '-' and up to 2 decimals.", tb, tb.Width + 5, 0, 1500); }
            catch { }
        }


    }
}