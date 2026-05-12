using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace PixelLab
{
    public class ColorSpaceVisualizer : Form
    {
        private TrackBar zoomBar;
        private Label zoomLabel;
        private float zoom = 1.0f;
        private Label colorInfoLabel;
        private Timer autoRotateTimer;
        private CheckBox autoRotateCheck;
        private ComboBox colorSpaceCombo;
        private TrackBar rotationX, rotationY;
        private Label angleXLabel, angleYLabel;
        private PictureBox canvas;
        private Timer rotationTimer;
        private float angleX = 30, angleY = 30;
        private ColorSpaceType currentSpace = ColorSpaceType.RGB;

        public enum ColorSpaceType { RGB, HSV, YUV, LAB }

        public ColorSpaceVisualizer()
        {
            Text = "معاينة فضاءات الألوان - PixelLab";
            Size = new Size(800, 700);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(40, 40, 45);
            DoubleBuffered = true;

            // لوحة التحكم العلوية
            Panel topPanel = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(30, 30, 35) };
            Label lbl = new Label { Text = "اختر نظام الألوان:", ForeColor = Color.White, Location = new Point(20, 15), AutoSize = true };
            colorSpaceCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(20, 40), Width = 150 };
            colorSpaceCombo.Items.AddRange(new[] { "RGB (مكعب)", "HSV (مخروط)", "YUV (متوازي)", "CIE LAB (مجسم)" });
            colorSpaceCombo.SelectedIndex = 0;
            colorSpaceCombo.SelectedIndexChanged += (s, e) => { currentSpace = (ColorSpaceType)colorSpaceCombo.SelectedIndex; canvas.Invalidate(); };

            Label rotX = new Label { Text = "دوران X:", ForeColor = Color.White, Location = new Point(200, 15), AutoSize = true };
            rotationX = new TrackBar { Minimum = -180, Maximum = 180, Value = 30, Location = new Point(200, 35), Width = 150, TickFrequency = 30 };
            rotationX.ValueChanged += (s, e) => { angleX = rotationX.Value; angleXLabel.Text = $"X: {angleX}°"; canvas.Invalidate(); };
            angleXLabel = new Label { Text = "X: 30°", ForeColor = Color.White, Location = new Point(360, 42), AutoSize = true };

            Label rotY = new Label { Text = "دوران Y:", ForeColor = Color.White, Location = new Point(450, 15), AutoSize = true };
            rotationY = new TrackBar { Minimum = -180, Maximum = 180, Value = 30, Location = new Point(450, 35), Width = 150, TickFrequency = 30 };
            rotationY.ValueChanged += (s, e) => { angleY = rotationY.Value; angleYLabel.Text = $"Y: {angleY}°"; canvas.Invalidate(); };
            angleYLabel = new Label { Text = "Y: 30°", ForeColor = Color.White, Location = new Point(610, 42), AutoSize = true };



            // Zoom (تكبير)
            Label zoomLbl = new Label { Text = "تكبير:", ForeColor = Color.White, Location = new Point(700, 15), AutoSize = true };
            zoomBar = new TrackBar { Minimum = 50, Maximum = 200, Value = 100, Location = new Point(700, 35), Width = 80, TickFrequency = 25 };
            zoomBar.ValueChanged += (s, e) => { zoom = zoomBar.Value / 100f; canvas.Invalidate(); };
            zoomLabel = new Label { Text = "100%", ForeColor = Color.White, Location = new Point(790, 42), AutoSize = true };
            zoomBar.ValueChanged += (s, e) => zoomLabel.Text = $"{zoomBar.Value}%";

            // تدوير تلقائي
            autoRotateCheck = new CheckBox { Text = "تدوير تلقائي", ForeColor = Color.White, Location = new Point(620, 15), AutoSize = true };
            autoRotateCheck.CheckedChanged += (s, e) => {
                if (autoRotateCheck.Checked)
                {
                    autoRotateTimer = new Timer { Interval = 50 };
                    autoRotateTimer.Tick += (ts, te) =>
                    {
                        angleY += 2;
                        if (angleY > 180)
                            angleY = -180;  // العودة إلى الحد الأدنى بدلاً من تجاوز الحد الأعلى
                        rotationY.Value = (int)angleY;
                        canvas.Invalidate();
                    }; autoRotateTimer.Start();
                }
                else autoRotateTimer?.Stop();
            };

            // زر اختيار لون
            Button pickColorBtn = new Button { Text = "اختر لوناً", FlatStyle = FlatStyle.Flat, BackColor = Color.SteelBlue, ForeColor = Color.White, Location = new Point(620, 35), Size = new Size(100, 28) };
            pickColorBtn.Click += (s, e) => { using (ColorDialog cd = new ColorDialog()) if (cd.ShowDialog() == DialogResult.OK) UpdateColorInfo(cd.Color); };

            // معلومات الألوان (أسفل النافذة)
            colorInfoLabel = new Label { Text = "اختر لوناً من الزر أعلاه لعرض قيمه في جميع الأنظمة", ForeColor = Color.LightGreen, Dock = DockStyle.Bottom, Height = 70, BackColor = Color.FromArgb(30, 30, 35), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10), Font = new Font("Consolas", 9) };

            // أضف كل هذه العناصر إلى topPanel
            topPanel.Controls.Add(zoomLbl);
            topPanel.Controls.Add(zoomBar);
            topPanel.Controls.Add(zoomLabel);
            topPanel.Controls.Add(autoRotateCheck);
            topPanel.Controls.Add(pickColorBtn);
            Controls.Add(colorInfoLabel);




            topPanel.Controls.Add(lbl);
            topPanel.Controls.Add(colorSpaceCombo);
            topPanel.Controls.Add(rotX);
            topPanel.Controls.Add(rotationX);
            topPanel.Controls.Add(angleXLabel);
            topPanel.Controls.Add(rotY);
            topPanel.Controls.Add(rotationY);
            topPanel.Controls.Add(angleYLabel);
            Controls.Add(topPanel);

            // منطقة الرسم
            canvas = new PictureBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(28, 28, 32) };
            canvas.Paint += Canvas_Paint;
            Controls.Add(canvas);

            canvas.MouseWheel += (s, e) => {
                zoomBar.Value = Math.Max(50, Math.Min(200, zoomBar.Value + (e.Delta > 0 ? 10 : -10)));
            };
        }

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(canvas.BackColor);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // مركز الرسم
            int cx = canvas.Width / 2;
            int cy = canvas.Height / 2;
            float size = Math.Min(canvas.Width, canvas.Height) * 0.35f;

            // مصفوفة الإسقاط
            Func<Point3D, PointF> project = p =>
            {
                // تدوير حول X ثم Y
                double radX = angleX * Math.PI / 180;
                double radY = angleY * Math.PI / 180;
                double y1 = p.Y * Math.Cos(radX) - p.Z * Math.Sin(radX);
                double z1 = p.Y * Math.Sin(radX) + p.Z * Math.Cos(radX);
                double x2 = p.X * Math.Cos(radY) + z1 * Math.Sin(radY);
                double z2 = -p.X * Math.Sin(radY) + z1 * Math.Cos(radY);
                double factor = 400 / (400 + z2);
                float xp = cx + (float)(x2 * size * zoom * factor);
                float yp = cy - (float)(y1 * size * zoom * factor);
                return new PointF(xp, yp);
            };

            switch (currentSpace)
            {
                case ColorSpaceType.RGB:
                    DrawRGBCube(g, project);
                    break;
                case ColorSpaceType.HSV:
                    DrawHSVCone(g, project, cx, cy, size);
                    break;
                case ColorSpaceType.YUV:
                    DrawYUVBox(g, project);
                    break;
                case ColorSpaceType.LAB:
                    DrawLABSolid(g, project);
                    break;
            }
        }

        private void DrawRGBCube(Graphics g, Func<Point3D, PointF> project)
        {
            // رؤوس المكعب (R,G,B) بقيم من 0 إلى 255
            Point3D[] vertices = new Point3D[8];
            for (int i = 0; i < 8; i++)
            {
                float r = (i & 1) != 0 ? 1 : 0;
                float gv = (i & 2) != 0 ? 1 : 0;
                float b = (i & 4) != 0 ? 1 : 0;
                vertices[i] = new Point3D(r, gv, b);
            }

            // حواف المكعب
            int[,] edges = {
                {0,1},{0,2},{0,4},{1,3},{1,5},{2,3},{2,6},{3,7},{4,5},{4,6},{5,7},{6,7}
            };

            // رسم الحواف
            Pen edgePen = new Pen(Color.White, 2);
            for (int i = 0; i < edges.GetLength(0); i++)
            {
                PointF p1 = project(vertices[edges[i, 0]]);
                PointF p2 = project(vertices[edges[i, 1]]);
                g.DrawLine(edgePen, p1, p2);
            }

            // رسم الرؤوس مع الألوان
            for (int i = 0; i < vertices.Length; i++)
            {
                PointF pt = project(vertices[i]);
                Color col = Color.FromArgb((int)(vertices[i].X * 255), (int)(vertices[i].Y * 255), (int)(vertices[i].Z * 255));
                using (Brush brush = new SolidBrush(col))
                {
                    g.FillEllipse(brush, pt.X - 6, pt.Y - 6, 12, 12);
                }
                g.DrawEllipse(Pens.Black, pt.X - 6, pt.Y - 6, 12, 12);
            }
        }

        private void DrawHSVCone(Graphics g, Func<Point3D, PointF> project, int cx, int cy, float size)
        {
            // رسم مخروط: Hue (زاوية) / Saturation (نصف القطر) / Value (الارتفاع)
            int segments = 24;
            float radius = size * 0.8f;
            float height = size * 1.2f;

            // قاعدة المخروط (قاع)
            for (int i = 0; i < segments; i++)
            {
                double angle1 = i * 2 * Math.PI / segments;
                double angle2 = (i + 1) * 2 * Math.PI / segments;
                float x1 = (float)Math.Cos(angle1) * radius;
                float y1 = (float)Math.Sin(angle1) * radius;
                float x2 = (float)Math.Cos(angle2) * radius;
                float y2 = (float)Math.Sin(angle2) * radius;
                Point3D p1 = new Point3D(x1 / size, y1 / size, -0.5f);
                Point3D p2 = new Point3D(x2 / size, y2 / size, -0.5f);
                // اللون حسب Hue
                Color col1 = HSVtoRGB(angle1 * 180 / Math.PI, 1, 1);
                Color col2 = HSVtoRGB(angle2 * 180 / Math.PI, 1, 1);
                using (Pen pen = new Pen(col1, 2))
                    g.DrawLine(pen, project(p1), project(p2));
            }

            // خطوط من القمة إلى القاعدة
            Point3D top = new Point3D(0, 0, 0.6f);
            for (int i = 0; i < segments; i += 2)
            {
                double angle = i * 2 * Math.PI / segments;
                float x = (float)Math.Cos(angle) * radius;
                float y = (float)Math.Sin(angle) * radius;
                Point3D basePoint = new Point3D(x / size, y / size, -0.5f);
                g.DrawLine(Pens.LightGray, project(top), project(basePoint));
            }

            // رسم القمة
            PointF topPt = project(top);
            g.FillEllipse(Brushes.White, topPt.X - 5, topPt.Y - 5, 10, 10);
        }

        private void DrawYUVBox(Graphics g, Func<Point3D, PointF> project)
        {
            // Y [0,1], U [-0.5,0.5], V [-0.5,0.5] لكن نعرض كمتوازي
            Point3D[] corners = new Point3D[8];
            for (int i = 0; i < 8; i++)
            {
                float y = (i & 1) != 0 ? 1 : 0;
                float u = (i & 2) != 0 ? 0.5f : -0.5f;
                float v = (i & 4) != 0 ? 0.5f : -0.5f;
                corners[i] = new Point3D(u, v, y); // X=U, Y=V, Z=Y
            }

            int[,] edges = { { 0, 1 }, { 0, 2 }, { 0, 4 }, { 1, 3 }, { 1, 5 }, { 2, 3 }, { 2, 6 }, { 3, 7 }, { 4, 5 }, { 4, 6 }, { 5, 7 }, { 6, 7 } };
            for (int i = 0; i < edges.GetLength(0); i++)
            {
                PointF p1 = project(corners[edges[i, 0]]);
                PointF p2 = project(corners[edges[i, 1]]);
                g.DrawLine(Pens.LightGray, p1, p2);
            }

            // تلوين الرؤوس حسب RGB المحول من YUV
            for (int i = 0; i < corners.Length; i++)
            {
                Point3D c = corners[i];
                float Y = c.Z;
                float U = c.X;
                float V = c.Y;
                float R = Y + 1.13983f * V;
                float G = Y - 0.39465f * U - 0.58060f * V;
                float B = Y + 2.03211f * U;
                R = Math.Max(0, Math.Min(1, R));
                G = Math.Max(0, Math.Min(1, G));
                B = Math.Max(0, Math.Min(1, B));
                Color col = Color.FromArgb((int)(R * 255), (int)(G * 255), (int)(B * 255));
                PointF pt = project(c);
                using (Brush br = new SolidBrush(col))
                    g.FillEllipse(br, pt.X - 5, pt.Y - 5, 10, 10);
            }
        }

        private void DrawLABSolid(Graphics g, Func<Point3D, PointF> project)
        {
            // L [0,100], a [-80,80], b [-80,80] لكن نطبيع إلى [0,1] للعرض
            // نأخذ 8 نقاط رئيسية: L=0/100 مع a,b قصوى
            float[] Lvals = { 0, 1 };
            float[] avals = { 0, 1 };
            float[] bvals = { 0, 1 };
            List<Point3D> points = new List<Point3D>();
            foreach (float L in Lvals)
                foreach (float a in avals)
                    foreach (float b in bvals)
                        points.Add(new Point3D(a, b, L));
            // حواف متوازي
            int[,] edges = { { 0, 1 }, { 0, 2 }, { 0, 4 }, { 1, 3 }, { 1, 5 }, { 2, 3 }, { 2, 6 }, { 3, 7 }, { 4, 5 }, { 4, 6 }, { 5, 7 }, { 6, 7 } };
            for (int i = 0; i < edges.GetLength(0); i++)
            {
                PointF p1 = project(points[edges[i, 0]]);
                PointF p2 = project(points[edges[i, 1]]);
                g.DrawLine(Pens.LightGray, p1, p2);
            }
            for (int i = 0; i < points.Count; i++)
            {
                Point3D p = points[i];
                // تحويل L,a,b إلى RGB تقريبي
                float L = p.Z * 100;
                float a = (p.X * 160) - 80;
                float b = (p.Y * 160) - 80;
                Color col = LABtoRGB(L, a, b);
                PointF pt = project(p);
                using (Brush br = new SolidBrush(col))
                    g.FillEllipse(br, pt.X - 5, pt.Y - 5, 10, 10);
            }
        }

        private Color HSVtoRGB(double H, double S, double V)
        {
            // نفس الدالة المستخدمة في MainForm
            double r = 0, g = 0, b = 0;
            if (S == 0) r = g = b = V;
            else
            {
                H = H / 60.0;
                int i = (int)Math.Floor(H);
                double f = H - i;
                double p = V * (1 - S);
                double q = V * (1 - S * f);
                double t = V * (1 - S * (1 - f));
                switch (i)
                {
                    case 0: r = V; g = t; b = p; break;
                    case 1: r = q; g = V; b = p; break;
                    case 2: r = p; g = V; b = t; break;
                    case 3: r = p; g = q; b = V; break;
                    case 4: r = t; g = p; b = V; break;
                    default: r = V; g = p; b = q; break;
                }
            }
            return Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255));
        }

        private Color LABtoRGB(double L, double a, double b)
        {
            // تحويل مختصر (نفس الموجود في MainForm)
            double y = (L + 16) / 116.0;
            double x = a / 500.0 + y;
            double z = y - b / 200.0;
            double x3 = x * x * x, y3 = y * y * y, z3 = z * z * z;
            double X = (x3 > 0.008856) ? x3 : (x - 16.0 / 116.0) / 7.787;
            double Y = (y3 > 0.008856) ? y3 : (y - 16.0 / 116.0) / 7.787;
            double Z = (z3 > 0.008856) ? z3 : (z - 16.0 / 116.0) / 7.787;
            X *= 0.95047; Y *= 1.0; Z *= 1.08883;
            double R = X * 3.2404542 + Y * -1.5371385 + Z * -0.4985314;
            double G = X * -0.9692660 + Y * 1.8760108 + Z * 0.0415560;
            double B = X * 0.0556434 + Y * -0.2040259 + Z * 1.0572252;
            R = (R > 0.0031308) ? (1.055 * Math.Pow(R, 1 / 2.4) - 0.055) : 12.92 * R;
            G = (G > 0.0031308) ? (1.055 * Math.Pow(G, 1 / 2.4) - 0.055) : 12.92 * G;
            B = (B > 0.0031308) ? (1.055 * Math.Pow(B, 1 / 2.4) - 0.055) : 12.92 * B;
            int r = (int)Math.Max(0, Math.Min(255, R * 255));
            int g = (int)Math.Max(0, Math.Min(255, G * 255));
            int bl = (int)Math.Max(0, Math.Min(255, B * 255));
            return Color.FromArgb(r, g, bl);
        }

        private struct Point3D { public float X, Y, Z; public Point3D(float x, float y, float z) { X = x; Y = y; Z = z; } }


        private void UpdateColorInfo(Color c)
        {
            double R = c.R, G = c.G, B = c.B;
            string rgb = $"RGB → ({R}, {G}, {B})";

            // HSV
            double r = R / 255.0, g = G / 255.0, b = B / 255.0;
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;
            double h = 0, s = 0, v = max;
            if (delta != 0)
            {
                s = delta / max;
                if (max == r) h = 60 * (((g - b) / delta) % 6);
                else if (max == g) h = 60 * (((b - r) / delta) + 2);
                else h = 60 * (((r - g) / delta) + 4);
                if (h < 0) h += 360;
            }
            string hsv = $"HSV → ({h:F0}°, {s * 100:F0}%, {v * 100:F0}%)";

            // YUV
            double Y = 0.299 * R + 0.587 * G + 0.114 * B;
            double U = -0.147 * R - 0.289 * G + 0.436 * B;
            double V_ = 0.615 * R - 0.515 * G - 0.100 * B;
            string yuv = $"YUV → ({Y:F0}, {U:+0;-0;0:F0}, {V_:+0;-0;0:F0})";

            // YCbCr
            double Yc = Y;
            double Cb = (B - Yc) * 0.564;
            double Cr = (R - Yc) * 0.713;
            string ycbcr = $"YCbCr → ({Yc:F0}, {Cb:+0;-0;0:F0}, {Cr:+0;-0;0:F0})";

            // CIE L*a*b*
            double rl = R / 255.0, gl = G / 255.0, bl = B / 255.0;
            rl = (rl > 0.04045) ? Math.Pow((rl + 0.055) / 1.055, 2.4) : rl / 12.92;
            gl = (gl > 0.04045) ? Math.Pow((gl + 0.055) / 1.055, 2.4) : gl / 12.92;
            bl = (bl > 0.04045) ? Math.Pow((bl + 0.055) / 1.055, 2.4) : bl / 12.92;
            double X = rl * 0.4124564 + gl * 0.3575761 + bl * 0.1804375;
            double Y_ = rl * 0.2126729 + gl * 0.7151522 + bl * 0.0721750;
            double Z = rl * 0.0193339 + gl * 0.1191920 + bl * 0.9503041;
            X /= 0.95047; Y_ /= 1.00000; Z /= 1.08883;
            Func<double, double> f = t => (t > 0.008856) ? Math.Pow(t, 1.0 / 3.0) : (7.787 * t + 16.0 / 116.0);
            double labL = 116.0 * f(Y_) - 16.0;
            double labA = 500.0 * (f(X) - f(Y_));
            double labB = 200.0 * (f(Y_) - f(Z));
            string lab = $"CIE L*a*b* → ({labL:F1}, {labA:+0;-0;0:F1}, {labB:+0;-0;0:F1})";

            // CMYK
            double k = 1 - Math.Max(r, Math.Max(g, b));
            double Cc = (1 - r - k) / (1 - k + 1e-8);
            double Mm = (1 - g - k) / (1 - k + 1e-8);
            double Yy = (1 - b - k) / (1 - k + 1e-8);
            string cmyk = $"CMYK → ({Cc * 100:F0}%, {Mm * 100:F0}%, {Yy * 100:F0}%, {k * 100:F0}%)";

            colorInfoLabel.Text = $"{rgb}    {hsv}    {yuv}    {ycbcr}    {lab}    {cmyk}";
            colorInfoLabel.BackColor = c;
            colorInfoLabel.ForeColor = (c.GetBrightness() > 0.5) ? Color.Black : Color.White;
        }





    }
}