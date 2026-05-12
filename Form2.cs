using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PixelLab
{
    public partial class Form2 : Form
    {
        private string currentFileName = "";
        private long currentFileSize = 0;
        public enum ColorSpace
        {
            RGB,
            HSV,
            YUV,
            YCbCr,
            LAB,
            CMYK
        }

        // المكونات
        private Panel mainPanel;
        private PictureBox pictureBox;
        private FlowLayoutPanel buttonPanel;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel infoLabel;
        private ToolStripStatusLabel pixelInfoLabel;

        // بيانات الصورة
        private Bitmap originalBitmap;
        private Bitmap currentBitmap;
        private string currentFilePath = "";
        // واجهة التحكم بأنظمة الألوان
        private ComboBox colorSpaceCombo;
        private TrackBar[] channelSliders;
        private NumericUpDown[] channelValues;
        private Button[] disableButtons;
        private CheckBox[] showChannelOnlyCheckBoxes;
        private Panel controlPanel;
        private Label[] channelLabels;
        //private Button applyButton;

        // بيانات العمل
        private ColorSpace currentColorSpace = ColorSpace.RGB;
        private double[,,] currentChannels; // [width, height, channelIndex] للتخزين المؤقت
        private int channelCount = 3;
        private bool[] channelDisabled;
        private bool[] showChannelOnly;
        private float[] channelMultipliers; // للتعديل
        public Form2()
        {
            InitializeComponent();
            Init();
            SetupDragDrop();
        }

        public void Init()
        {
            this.Text = "PixelLab – مختبر الصور التفاعلي";
            this.Size = new Size(1000, 700);
            this.MinimumSize = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.DoubleBuffered = true;

            // لوحة الأزرار (FlowLayout)
            buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 50,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoSize = false,
                Padding = new Padding(5),
                BackColor = Color.FromArgb(30, 30, 35)
            };
            AddButtons();
            Controls.Add(buttonPanel);





            // لوحة التحكم بأنظمة الألوان (على اليمين)
            controlPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 270,
                BackColor = Color.FromArgb(35, 35, 40),
                Padding = new Padding(8)
            };
            Controls.Add(controlPanel);
            SetupColorControlPanel();




            // لوحة تمرير تحتوي على PictureBox
            mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(28, 28, 32)
                
            };
            pictureBox = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.AutoSize,
                Location = Point.Empty
            };
            pictureBox.MouseMove += PictureBox_MouseMove;
            mainPanel.Controls.Add(pictureBox);
            Controls.Add(mainPanel);

            // شريط الحالة
            statusStrip = new StatusStrip();
            infoLabel = new ToolStripStatusLabel("لا توجد صورة");
            pixelInfoLabel = new ToolStripStatusLabel("الماوس خارج الصورة");
            statusStrip.Items.Add(infoLabel);
            statusStrip.Items.Add(new ToolStripStatusLabel(" | "));
            statusStrip.Items.Add(pixelInfoLabel);
            Controls.Add(statusStrip);
        }
        private void SetupColorControlPanel()
        {
            int yOffset = 10;

            // عنوان
            Label title = new Label { Text = "🎨 التحكم بأنظمة الألوان", ForeColor = Color.White, Font = new Font("Segoe UI", 12, FontStyle.Bold), Top = yOffset, Left = 10, AutoSize = true };
            controlPanel.Controls.Add(title);
            yOffset += 35;

            // اختيار النظام اللوني
            Label spaceLabel = new Label { Text = "النظام اللوني:", ForeColor = Color.White, Top = yOffset, Left = 10, AutoSize = true };
            controlPanel.Controls.Add(spaceLabel);
            colorSpaceCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Top = yOffset, Left = 110, Width = 150 };
            colorSpaceCombo.Items.AddRange(Enum.GetNames(typeof(ColorSpace)));
            colorSpaceCombo.SelectedIndex = 0;
            colorSpaceCombo.SelectedIndexChanged += ColorSpaceCombo_SelectedIndexChanged;
            controlPanel.Controls.Add(colorSpaceCombo);
            yOffset += 35;

            // مصفوفات التحكم
            channelSliders = new TrackBar[5];
            channelValues = new NumericUpDown[5];
            disableButtons = new Button[5];
            showChannelOnlyCheckBoxes = new CheckBox[5];
            channelLabels = new Label[5];
            channelDisabled = new bool[5];
            showChannelOnly = new bool[5];
            channelMultipliers = new float[5];

            for (int i = 0; i < 5; i++)
            {
                int idx = i;
                channelLabels[i] = new Label { Text = $"قناة {i + 1}:", ForeColor = Color.White, Top = yOffset, Left = 10, AutoSize = true };
                controlPanel.Controls.Add(channelLabels[i]);

                channelSliders[i] = new TrackBar { Minimum = 0, Maximum = 255, Value = 128, Top = yOffset, Left = 70, Width = 120, TickFrequency = 16 };
                channelSliders[i].ValueChanged += (s, e) => { channelValues[idx].Value = channelSliders[idx].Value; RebuildImageFromChannels(); };
                controlPanel.Controls.Add(channelSliders[i]);

                channelValues[i] = new NumericUpDown { Minimum = 0, Maximum = 255, Value = 128, Top = yOffset, Left = 195, Width = 45 };
                channelValues[i].ValueChanged += (s, e) => { channelSliders[idx].Value = (int)channelValues[idx].Value; RebuildImageFromChannels(); };
                controlPanel.Controls.Add(channelValues[i]);

                disableButtons[i] = new Button { Text = "تعطيل", FlatStyle = FlatStyle.Flat, BackColor = Color.DarkRed, ForeColor = Color.White, Top = yOffset + 28, Left = 10, Width = 80, Height = 28 };
                disableButtons[i].Click += (s, e) => ToggleChannelDisable(idx);
                controlPanel.Controls.Add(disableButtons[i]);

                showChannelOnlyCheckBoxes[i] = new CheckBox { Text = "عرض فقط", ForeColor = Color.White, Top = yOffset + 28, Left = 100, AutoSize = true };
                showChannelOnlyCheckBoxes[i].CheckedChanged += (s, e) => ToggleShowChannelOnly(idx);
                controlPanel.Controls.Add(showChannelOnlyCheckBoxes[i]);

                yOffset += 65;
            }
            // (ملاحظة: لا يوجد applyButton هنا نهائياً)

            // زر تطبيق التغييرات (يمكن تحديث تلقائي لكن هذا يدوي للتحكم)
            //applyButton = new Button { Text = "تطبيق التعديلات", FlatStyle = FlatStyle.Flat, BackColor = Color.SteelBlue, ForeColor = Color.White, Top = yOffset, Left = 50, Width = 180, Height = 40 };
            //applyButton.Click += (s, e) => RebuildImageFromChannels();
            //controlPanel.Controls.Add(applyButton);

            // إخفاء القنوات الزائدة في البداية (RGB = 3 قنوات)
            UpdateChannelVisibility();
        }
        private void AddButtons()
        {
            AddButton("🌐 عرض فضاءات الألوان", (s, e) => new ColorSpaceVisualizer().ShowDialog());
            AddButton("📂 فتح صورة", (s, e) => OpenImage());
            AddButton("💾 حفظ الصورة", (s, e) => SaveImage());
            AddButton("🔄 إعادة ضبط", (s, e) => ResetImage());
            //AddButton("🎨 تقليل عدد الألوان", (s, e) => ShowQuantizationDialog());
            //AddButton("⚪ تدرج رمادي", (s, e) => ApplyEffect(Grayscale));
            //AddButton("🎞️ نفي (Negative)", (s, e) => ApplyEffect(Negative));
            AddButton("➕ تعديل السطوع", (s, e) => ShowBrightnessDialog());
            //AddButton("🌀 تمويه (Blur)", (s, e) => ApplyConvolutionFilter(ConvolutionKernel.Blur3x3));
            //AddButton("✨ حدة (Sharpen)", (s, e) => ApplyConvolutionFilter(ConvolutionKernel.Sharpen3x3));
            //AddButton("🔍 كشف الحواف", (s, e) => ApplyConvolutionFilter(ConvolutionKernel.EdgeDetect));
            AddButton("🔴 عرض القناة الحمراء", (s, e) => ApplyEffect(RedChannel));
            AddButton("🟢 عرض القناة الخضراء", (s, e) => ApplyEffect(GreenChannel));
            AddButton("🔵 عرض القناة الزرقاء", (s, e) => ApplyEffect(BlueChannel));
            // أنظمة الألوان
            AddButton("🎨 تحويل إلى HSV (Hue/Saturation/Value)", (s, e) => ApplyEffect(RGBtoHSVImage));
            AddButton("🌈 تحويل إلى YUV (Luma+Chroma)", (s, e) => ApplyEffect(RGBtoYUVImage));
            AddButton("🧪 تحويل إلى CIE L*a*b*", (s, e) => ApplyEffect(RGBtoLABImage));
            AddButton("📺 تحويل إلى YCbCr", (s, e) => ApplyEffect(RGBtoYCbCrImage));
            AddButton("🖨️ تحويل إلى CMYK (بدون K)", (s, e) => ApplyEffect(RGBtoCMYKImage));
        }

        private void AddButton(string text, EventHandler clickHandler)
        {  
            var btn = new Button
            {
                Text = text,
                AutoSize = false,
                Height = 40,
                Width = 90,
                Margin = new Padding(3),
                BackColor = Color.FromArgb(60, 60, 70),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += clickHandler;
            buttonPanel.Controls.Add(btn);
          
        }

        private void SetupDragDrop()
        {
            this.AllowDrop = true;
            this.DragEnter += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    e.Effect = DragDropEffects.Copy;
            };
            this.DragDrop += (s, e) =>
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0 && IsImageFile(files[0]))
                    LoadImage(files[0]);
            };
            pictureBox.AllowDrop = true;
            pictureBox.DragEnter += (s, e) => (s as Control).DoDragDrop(e.Data, DragDropEffects.Copy);
            pictureBox.DragDrop += (s, e) => this.OnDragDrop(e);
        }


        //private void ShowQuantizationDialog()
        //{
        //    if (currentBitmap == null) return;
        //    using (Form dialog = new Form())
        //    {
        //        dialog.Text = "تقليل عدد الألوان";
        //        dialog.Size = new Size(300, 150);
        //        dialog.StartPosition = FormStartPosition.CenterParent;
        //        dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
        //        dialog.MaximizeBox = false;
        //        dialog.MinimizeBox = false;

        //        Label lbl = new Label { Text = "عدد الألوان المطلوب:", Location = new Point(20, 20), AutoSize = true };
        //        NumericUpDown numColors = new NumericUpDown { Minimum = 2, Maximum = 256, Value = 16, Location = new Point(150, 18), Width = 60 };
        //        Button btnOk = new Button { Text = "تطبيق", DialogResult = DialogResult.OK, Location = new Point(100, 70), Size = new Size(80, 30) };
        //        Button btnCancel = new Button { Text = "إلغاء", DialogResult = DialogResult.Cancel, Location = new Point(190, 70), Size = new Size(80, 30) };

        //        dialog.Controls.Add(lbl);
        //        dialog.Controls.Add(numColors);
        //        dialog.Controls.Add(btnOk);
        //        dialog.Controls.Add(btnCancel);

        //        if (dialog.ShowDialog() == DialogResult.OK)
        //        {
        //            int colorCount = (int)numColors.Value;
        //            Cursor = Cursors.WaitCursor;
        //            try
        //            {
        //                Bitmap quantized = QuantizeImage(currentBitmap, colorCount);
        //                currentBitmap.Dispose();
        //                currentBitmap = quantized;
        //                UpdateImageInfo();
        //                SetImage(currentBitmap);
        //                infoLabel.Text = $"تم تقليل الألوان إلى {colorCount} لون";
        //            }
        //            catch (Exception ex)
        //            {
        //                MessageBox.Show($"خطأ: {ex.Message}");
        //            }
        //            finally
        //            {
        //                Cursor = Cursors.Default;
        //            }
        //        }
        //    }
        //}


        private void UpdateImageInfo()
        {
            if (currentBitmap == null)
            {
                infoLabel.Text = "لا توجد صورة";
                return;
            }
            string dimensions = $"{currentBitmap.Width}×{currentBitmap.Height}";
            string format = currentBitmap.RawFormat.ToString().Replace("System.Drawing.Imaging.", "");
            string sizeStr = (currentFileSize / 1024.0).ToString("F1") + " KB";
            string colorDepth = currentBitmap.PixelFormat.ToString();
            string info = $"📄 {currentFileName} | {format} | {sizeStr} | {dimensions} | {colorDepth}";
            infoLabel.BackColor = Color.WhiteSmoke;
            infoLabel.Text = info;
        }
        private Bitmap QuantizeImage(Bitmap src, int colorCount)
        {
            colorCount = Math.Min(256, Math.Max(2, colorCount));
            Dictionary<Color, int> colorFrequency = new Dictionary<Color, int>();
            for (int y = 0; y < src.Height; y++)
            {
                for (int x = 0; x < src.Width; x++)
                {
                    Color c = src.GetPixel(x, y);
                    if (colorFrequency.ContainsKey(c))
                        colorFrequency[c]++;
                    else
                        colorFrequency[c] = 1;
                }
            }
            var sortedColors = colorFrequency.OrderByDescending(kvp => kvp.Value).Take(colorCount).Select(kvp => kvp.Key).ToList();
            if (sortedColors.Count < colorCount)
                colorCount = sortedColors.Count;
            Bitmap dst = new Bitmap(src.Width, src.Height);
            for (int y = 0; y < src.Height; y++)
            {
                for (int x = 0; x < src.Width; x++)
                {
                    Color orig = src.GetPixel(x, y);
                    Color nearest = FindNearestColor(orig, sortedColors);
                    dst.SetPixel(x, y, nearest);
                }
            }
            return dst;
        }

        private Color FindNearestColor(Color target, List<Color> palette)
        {
            Color nearest = palette[0];
            int minDist = int.MaxValue;
            foreach (Color c in palette)
            {
                int dr = target.R - c.R;
                int dg = target.G - c.G;
                int db = target.B - c.B;
                int dist = dr * dr + dg * dg + db * db;
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = c;
                    if (dist == 0) break;
                }
            }
            return nearest;
        }
        private bool IsImageFile(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif";
        }

        private void OpenImage()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
                if (ofd.ShowDialog() == DialogResult.OK)
                    LoadImage(ofd.FileName);
            }
        }

        private void LoadImage(string path)
        {
            try
            {
                Bitmap loaded = new Bitmap(path);
                originalBitmap = new Bitmap(loaded);
                currentBitmap = new Bitmap(loaded);
                SetImage(currentBitmap);
                currentFilePath = path;
                infoLabel.Text = $"الصورة: {Path.GetFileName(path)} | الأبعاد: {currentBitmap.Width}×{currentBitmap.Height} | عمق اللون: 32bpp";

                loaded.Dispose();
                currentFileName = Path.GetFileName(path);
                FileInfo fi = new FileInfo(path);
                currentFileSize = fi.Length;
                UpdateImageInfo();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الصورة:\n{ex.Message}", "PixelLab", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetImage(Bitmap bmp)
        {
            if (pictureBox.Image != null)
                pictureBox.Image.Dispose();
            pictureBox.Image = bmp;
        }

        private void SaveImage()
        {
            if (currentBitmap == null) return;
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap Image|*.bmp";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    //currentBitmap.Save(sfd.FileName, GetImageFormat(sfd.FilterIndex));
                    MessageBox.Show("تم حفظ الصورة بنجاح.", "PixelLab", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    currentFileName = Path.GetFileName(sfd.FileName);
                    FileInfo fi = new FileInfo(sfd.FileName);
                    currentFileSize = fi.Length;
                    UpdateImageInfo();
                }
            }
        }

        private void ResetImage()
        {
            if (originalBitmap == null) return;
            currentBitmap = new Bitmap(originalBitmap);
            SetImage(currentBitmap);
            UpdateImageInfo();
            //UpdateInfoLabel("تمت الإعادة إلى الأصل");
            infoLabel.Text = "تمت الإعادة إلى الأصل";


            //if (originalBitmap != null)
            //{
            //    originalBitmap = new Bitmap(currentBitmap); // استعادة
            //    ExtractChannelsFromOriginal();
            //    RebuildImageFromChannels();
            //}
        }

        private void ApplyEffect(Func<Bitmap, Bitmap> effect)
        {
            if (currentBitmap == null) return;
            Cursor = Cursors.WaitCursor;
            try
            {
                Bitmap newBitmap = effect(currentBitmap);
                currentBitmap.Dispose();
                currentBitmap = newBitmap;
                UpdateImageInfo();
                SetImage(currentBitmap);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ أثناء التطبيق: {ex.Message}", "PixelLab", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        // ---------- المؤثرات الأساسية ----------
        //private Bitmap Grayscale(Bitmap src)
        //{
        //    Bitmap bmp = new Bitmap(src.Width, src.Height);
        //    for (int y = 0; y < src.Height; y++)
        //    {
        //        for (int x = 0; x < src.Width; x++)
        //        {
        //            Color c = src.GetPixel(x, y);
        //            int gray = (int)(c.R * 0.299 + c.G * 0.587 + c.B * 0.114);
        //            bmp.SetPixel(x, y, Color.FromArgb(gray, gray, gray));
        //        }
        //    }
        //    return bmp;
        //}

        //private Bitmap Negative(Bitmap src)
        //{
        //    Bitmap bmp = new Bitmap(src.Width, src.Height);
        //    for (int y = 0; y < src.Height; y++)
        //    {
        //        for (int x = 0; x < src.Width; x++)
        //        {
        //            Color c = src.GetPixel(x, y);
        //            bmp.SetPixel(x, y, Color.FromArgb(255 - c.R, 255 - c.G, 255 - c.B));
        //        }
        //    }
        //    return bmp;
        //}

        private Bitmap AdjustBrightness(Bitmap src, int brightness)
        {

            if (brightness==0)  {

                //return currentBitmap;
                //return originalBitmap;
                return new Bitmap(src);
            }
            Bitmap bmp = new Bitmap(src.Width, src.Height);
            for (int y = 0; y < src.Height; y++)
            {
                for (int x = 0; x < src.Width; x++)
                {
                    Color c = src.GetPixel(x, y);
                    int r = Clamp(c.R + brightness);
                    int g = Clamp(c.G + brightness);
                    int b = Clamp(c.B + brightness);
                    bmp.SetPixel(x, y, Color.FromArgb(r, g, b));
                }
            }
            return bmp;
        }

        private void ShowBrightnessDialog()
        {
            if (currentBitmap == null) return;
            using (Form dialog = new Form())
            {
                dialog.Text = "تعديل السطوع";
                dialog.Size = new Size(300, 150);
                dialog.StartPosition = FormStartPosition.CenterParent;
                TrackBar track = new TrackBar { Minimum = -100, Maximum = 100, Value = 0, Dock = DockStyle.Top };
                Button btnOk = new Button { Text = "تطبيق", DialogResult = DialogResult.OK, Dock = DockStyle.Bottom };
                dialog.Controls.Add(track);
                dialog.Controls.Add(btnOk);
                //dialog.ShowDialog();
                //track.ValueChanged += (s, e) => ApplyEffect(bmp => AdjustBrightness(bmp, track.Value));
                if (dialog.ShowDialog() == DialogResult.OK)
                    ApplyEffect(bmp => AdjustBrightness(bmp, track.Value));
            }
        }

        private int Clamp(int value) => Math.Max(0, Math.Min(255, value));

        // ---------- قنوات الألوان ----------
        private Bitmap RedChannel(Bitmap src)
        {
            Bitmap bmp = new Bitmap(src.Width, src.Height);
            for (int y = 0; y < src.Height; y++)
                for (int x = 0; x < src.Width; x++)
                {
                    Color c = src.GetPixel(x, y);
                    bmp.SetPixel(x, y, Color.FromArgb(c.R, 0, 0));
                }
            return bmp;
        }

        private Bitmap GreenChannel(Bitmap src)
        {
            Bitmap bmp = new Bitmap(src.Width, src.Height);
            for (int y = 0; y < src.Height; y++)
                for (int x = 0; x < src.Width; x++)
                {
                    Color c = src.GetPixel(x, y);
                    bmp.SetPixel(x, y, Color.FromArgb(0, c.G, 0));
                }
            return bmp;
        }

        private Bitmap BlueChannel(Bitmap src)
        {
            Bitmap bmp = new Bitmap(src.Width, src.Height);
            for (int y = 0; y < src.Height; y++)
                for (int x = 0; x < src.Width; x++)
                {
                    Color c = src.GetPixel(x, y);
                    bmp.SetPixel(x, y, Color.FromArgb(0, 0, c.B));
                }
            return bmp;
        }

        // ---------- مرشحات الالتفاف (Convolution) ----------
        //private void ApplyConvolutionFilter(float[,] kernel)
        //{
        //    if (currentBitmap == null) return;
        //    Cursor = Cursors.WaitCursor;
        //    try
        //    {
        //        Bitmap result = Convolve(currentBitmap, kernel);
        //        currentBitmap.Dispose();
        //        currentBitmap = result;
        //        SetImage(currentBitmap);
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"خطأ في المرشح: {ex.Message}");
        //    }
        //    finally { Cursor = Cursors.Default; }
        //}

        //private Bitmap Convolve(Bitmap src, float[,] kernel)
        //{
        //    int kernelSize = kernel.GetLength(0);
        //    int offset = kernelSize / 2;
        //    Bitmap bmp = new Bitmap(src.Width, src.Height);
        //    BitmapData srcData = src.LockBits(new Rectangle(0, 0, src.Width, src.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        //    BitmapData dstData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

        //    unsafe
        //    {
        //        byte* srcPtr = (byte*)srcData.Scan0;
        //        byte* dstPtr = (byte*)dstData.Scan0;
        //        int stride = srcData.Stride;

        //        for (int y = 0; y < src.Height; y++)
        //        {
        //            for (int x = 0; x < src.Width; x++)
        //            {
        //                float r = 0, g = 0, b = 0;
        //                for (int ky = -offset; ky <= offset; ky++)
        //                {
        //                    for (int kx = -offset; kx <= offset; kx++)
        //                    {
        //                        int ix = x + kx;
        //                        int iy = y + ky;
        //                        if (ix >= 0 && ix < src.Width && iy >= 0 && iy < src.Height)
        //                        {
        //                            byte* pixel = srcPtr + iy * stride + ix * 4;
        //                            float weight = kernel[ky + offset, kx + offset];
        //                            r += pixel[2] * weight;
        //                            g += pixel[1] * weight;
        //                            b += pixel[0] * weight;
        //                        }
        //                    }
        //                }
        //                byte* dstPixel = dstPtr + y * stride + x * 4;
        //                dstPixel[2] = ClampByte((int)r);
        //                dstPixel[1] = ClampByte((int)g);
        //                dstPixel[0] = ClampByte((int)b);
        //                dstPixel[3] = 255;
        //            }
        //        }
        //    }
        //    src.UnlockBits(srcData);
        //    bmp.UnlockBits(dstData);
        //    return bmp;
        //}

        private byte ClampByte(int val) => (byte)Math.Max(0, Math.Min(255, val));
        // أنوية المرشحات
        //private static class ConvolutionKernel
        //{
        //    public static float[,] Blur3x3 => new float[,] { { 1 / 9f, 1 / 9f, 1 / 9f }, { 1 / 9f, 1 / 9f, 1 / 9f }, { 1 / 9f, 1 / 9f, 1 / 9f } };
        //    public static float[,] Sharpen3x3 => new float[,] { { 0, -1, 0 }, { -1, 5, -1 }, { 0, -1, 0 } };
        //    public static float[,] EdgeDetect => new float[,] { { -1, -1, -1 }, { -1, 8, -1 }, { -1, -1, -1 } };
        //}

        // ---------- تفاعل الماوس (إظهار قيمة البكسل) ----------
        private void PictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (currentBitmap == null)
            {
                pixelInfoLabel.Text = "لا توجد صورة";
                return;
            }
            if (e.X >= 0 && e.X < currentBitmap.Width && e.Y >= 0 && e.Y < currentBitmap.Height)
            {
                Color c = currentBitmap.GetPixel(e.X, e.Y);
                pixelInfoLabel.Text = $"X:{e.X} Y:{e.Y} | R:{c.R} G:{c.G} B:{c.B}";
                pixelInfoLabel.BackColor = Color.WhiteSmoke;
            }
            else
            {
                pixelInfoLabel.Text = "خارج حدود الصورة";
                pixelInfoLabel.BackColor = Color.LightGray;
            }
        }

      


        // -------------------- تحويلات أنظمة الألوان --------------------

        // HSV: Hue (0-360) -> Red, Saturation (0-1) -> Green, Value (0-1) -> Blue
        private Bitmap RGBtoHSVImage(Bitmap src)
        {
            Bitmap dst = new Bitmap(src.Width, src.Height);
            for (int y = 0; y < src.Height; y++)
            {
                for (int x = 0; x < src.Width; x++)
                {
                    Color c = src.GetPixel(x, y);
                    double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
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
                    // تطبيع Hue إلى [0-255] (من 360 درجة)
                    byte hByte = (byte)(h / 360.0 * 255);
                    byte sByte = (byte)(s * 255);
                    byte vByte = (byte)(v * 255);
                    dst.SetPixel(x, y, Color.FromArgb(hByte, sByte, vByte));
                }
            }
            return dst;
        }

        // YUV (PAL/NTSC): Y = 0.299R+0.587G+0.114B,  U = -0.147R-0.289G+0.436B,  V = 0.615R-0.515G-0.100B
        // نطاق U/V عادة -128..127 -> نطوي إلى 0-255
        private Bitmap RGBtoYUVImage(Bitmap src)
        {
            Bitmap dst = new Bitmap(src.Width, src.Height);
            for (int y = 0; y < src.Height; y++)
            {
                for (int x = 0; x < src.Width; x++)
                {
                    Color c = src.GetPixel(x, y);
                    double R = c.R, G = c.G, B = c.B;
                    double Y = 0.299 * R + 0.587 * G + 0.114 * B;
                    double U = -0.147 * R - 0.289 * G + 0.436 * B + 128;
                    double V = 0.615 * R - 0.515 * G - 0.100 * B + 128;
                    byte yByte = ClampByte((int)Y);
                    byte uByte = ClampByte((int)U);
                    byte vByte = ClampByte((int)V);
                    dst.SetPixel(x, y, Color.FromArgb(yByte, uByte, vByte));
                }
            }
            return dst;
        }

        // CIE L*a*b*: L* 0-100, a* -128..127, b* -128..127 -> نطبيعها إلى 0-255
        private Bitmap RGBtoLABImage(Bitmap src)
        {
            Bitmap dst = new Bitmap(src.Width, src.Height);
            for (int y = 0; y < src.Height; y++)
            {
                for (int x = 0; x < src.Width; x++)
                {
                    Color c = src.GetPixel(x, y);
                    double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
                    // التحويل إلى XYZ
                    r = (r > 0.04045) ? Math.Pow((r + 0.055) / 1.055, 2.4) : r / 12.92;
                    g = (g > 0.04045) ? Math.Pow((g + 0.055) / 1.055, 2.4) : g / 12.92;
                    b = (b > 0.04045) ? Math.Pow((b + 0.055) / 1.055, 2.4) : b / 12.92;
                    double X = r * 0.4124564 + g * 0.3575761 + b * 0.1804375;
                    double Y = r * 0.2126729 + g * 0.7151522 + b * 0.0721750;
                    double Z = r * 0.0193339 + g * 0.1191920 + b * 0.9503041;
                    // D65 مرجع
                    X /= 0.95047;
                    Y /= 1.00000;
                    Z /= 1.08883;
                    Func<double, double> f = t => (t > 0.008856) ? Math.Pow(t, 1.0 / 3.0) : (7.787 * t + 16.0 / 116.0);
                    double L = 116.0 * f(Y) - 16.0;
                    double a = 500.0 * (f(X) - f(Y));
                    double b_ = 200.0 * (f(Y) - f(Z));
                    // تطبيع L [0-100] -> [0-255], a و b [-128..127] -> [0-255]
                    byte Lb = (byte)(L / 100.0 * 255);
                    byte ab = (byte)ClampByte((int)(a + 128));
                    byte bb = (byte)ClampByte((int)(b_ + 128));
                    dst.SetPixel(x, y, Color.FromArgb(Lb, ab, bb));
                }
            }
            return dst;
        }

        // YCbCr (نطاق كامل 0-255 لكل مكون)
        private Bitmap RGBtoYCbCrImage(Bitmap src)
        {
            Bitmap dst = new Bitmap(src.Width, src.Height);
            for (int y = 0; y < src.Height; y++)
            {
                for (int x = 0; x < src.Width; x++)
                {
                    Color c = src.GetPixel(x, y);
                    double R = c.R, G = c.G, B = c.B;
                    double Y = 0.299 * R + 0.587 * G + 0.114 * B;
                    double Cb = (B - Y) * 0.564 + 128;
                    double Cr = (R - Y) * 0.713 + 128;
                    byte yByte = ClampByte((int)Y);
                    byte cbByte = ClampByte((int)Cb);
                    byte crByte = ClampByte((int)Cr);
                    dst.SetPixel(x, y, Color.FromArgb(yByte, cbByte, crByte));
                }
            }
            return dst;
        }

        // CMYK: تحويل RGB إلى C,M,Y (بدون K نظراً لثلاث قنوات فقط) – نطاق 0-255
        private Bitmap RGBtoCMYKImage(Bitmap src)
        {
            Bitmap dst = new Bitmap(src.Width, src.Height);
            for (int y = 0; y < src.Height; y++)
            {
                for (int x = 0; x < src.Width; x++)
                {
                    Color c = src.GetPixel(x, y);
                    double R = c.R / 255.0, G = c.G / 255.0, B = c.B / 255.0;
                    double K = 1 - Math.Max(R, Math.Max(G, B));
                    double C = (1 - R - K) / (1 - K + 1e-8);
                    double M = (1 - G - K) / (1 - K + 1e-8);
                    double Y = (1 - B - K) / (1 - K + 1e-8);
                    byte cByte = (byte)(C * 255);
                    byte mByte = (byte)(M * 255);
                    byte yByte = (byte)(Y * 255);
                    dst.SetPixel(x, y, Color.FromArgb(cByte, mByte, yByte));
                }
            }
            return dst;
        }








        //private void ColorSpaceCombo_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    currentColorSpace = (ColorSpace)colorSpaceCombo.SelectedIndex;
        //    UpdateChannelVisibility();
        //    if (originalBitmap != null)
        //    {
        //        ExtractChannelsFromOriginal(); // استخراج قنوات الصورة الأصلية في الفضاء الجديد
        //        RebuildImageFromChannels(); // إعادة بناء الصورة بناءً على القنوات الجديدة
        //    }
        //}

        private void ColorSpaceCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentColorSpace = (ColorSpace)colorSpaceCombo.SelectedIndex;
            UpdateChannelVisibility();
            if (currentBitmap != null)
            {
                Cursor = Cursors.WaitCursor;
                try
                {
                    ExtractChannelsFromOriginal();   // استخراج القنوات من الصورة الحالية
                    RebuildImageFromChannels();      // إعادة بناء الصورة حسب الفضاء الجديد
                    pictureBox.Refresh();            // تحديث العرض
                }
                finally
                {
                    Cursor = Cursors.Default;
                }
            }
        }

        private void UpdateChannelVisibility()
        {
            channelCount = (currentColorSpace == ColorSpace.CMYK || currentColorSpace == ColorSpace.LAB) ? 4 : 3;
            for (int i = 0; i < 5; i++)
            {
                bool visible = i < channelCount;
                channelLabels[i].Visible = visible;
                channelSliders[i].Visible = visible;
                channelValues[i].Visible = visible;
                disableButtons[i].Visible = visible;
                showChannelOnlyCheckBoxes[i].Visible = visible;
            }
            // تحديث تسميات القنوات حسب الفضاء
            string[] names = GetChannelNames();
            for (int i = 0; i < channelCount; i++)
                channelLabels[i].Text = names[i] + ":";
        }

        private string[] GetChannelNames()
        {
            switch (currentColorSpace)
            {
                case ColorSpace.RGB: return new[] { "الأحمر (R)", "الأخضر (G)", "الأزرق (B)" };
                case ColorSpace.HSV: return new[] { "Hue (صبغة)", "Saturation (تشبع)", "Value (قيمة)" };
                case ColorSpace.YUV: return new[] { "Y (لومينانس)", "U (كرومينانس)", "V (كرومينانس)" };
                case ColorSpace.YCbCr: return new[] { "Y (لومينانس)", "Cb (فرق الأزرق)", "Cr (فرق الأحمر)" };
                case ColorSpace.LAB: return new[] { "L* (إضاءة)", "a* (أخضر-أحمر)", "b* (أزرق-أصفر)", "اختياري" };
                case ColorSpace.CMYK: return new[] { "C (سيان)", "M (ماجنتا)", "Y (أصفر)", "K (أسود)" };
                default: return new[] { "قناة 1", "قناة 2", "قناة 3" };
            }
        }

        //private void ExtractChannelsFromOriginal()
        //{
        //    if (originalBitmap == null) return;
        //    int w = originalBitmap.Width, h = originalBitmap.Height;
        //    currentChannels = new double[w, h, channelCount];
        //    for (int y = 0; y < h; y++)
        //    {
        //        for (int x = 0; x < w; x++)
        //        {
        //            Color c = originalBitmap.GetPixel(x, y);
        //            double[] channels = ColorToChannels(c, currentColorSpace);
        //            for (int ch = 0; ch < channelCount; ch++)
        //                currentChannels[x, y, ch] = channels[ch];
        //        }
        //    }
        //    // إعادة ضبط التعديلات
        //    for (int i = 0; i < channelCount; i++)
        //    {
        //        channelMultipliers[i] = 1.0f;
        //        channelDisabled[i] = false;
        //        showChannelOnly[i] = false;
        //        disableButtons[i].Text = "تعطيل";
        //        disableButtons[i].BackColor = Color.DarkRed;
        //        showChannelOnlyCheckBoxes[i].Checked = false;
        //        // تعيين قيم الشرائح بناءً على متوسط القناة أو قيمة افتراضية
        //        channelSliders[i].Value = 128;
        //        channelValues[i].Value = 128;
        //    }
        //}

        private void ExtractChannelsFromOriginal()
        {
            if (currentBitmap == null) return;
            int w = currentBitmap.Width, h = currentBitmap.Height;
            int expectedChannels = channelCount;  // عدد القنوات المطلوب حسب الفضاء الحالي
            currentChannels = new double[w, h, expectedChannels];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = currentBitmap.GetPixel(x, y);
                    double[] channels = ColorToChannels(c, currentColorSpace);

                    // إذا كان عدد القنوات أقل من المطلوب، نكمل بالصفر
                    for (int ch = 0; ch < expectedChannels; ch++)
                    {
                        if (ch < channels.Length)
                            currentChannels[x, y, ch] = channels[ch];
                        else
                            currentChannels[x, y, ch] = 0;  // قيمة افتراضية للقنوات الإضافية
                    }
                }
            }

            // إعادة ضبط التعديلات
            for (int i = 0; i < channelCount; i++)
            {
                channelMultipliers[i] = 1.0f;
                channelDisabled[i] = false;
                showChannelOnly[i] = false;
                disableButtons[i].Text = "تعطيل";
                disableButtons[i].BackColor = Color.DarkRed;
                showChannelOnlyCheckBoxes[i].Checked = false;
                channelSliders[i].Value = 128;
                channelValues[i].Value = 128;
            }
        }










        private double[] ColorToChannels(Color c, ColorSpace space)
        {
            double R = c.R, G = c.G, B = c.B;
            switch (space)
            {
                case ColorSpace.RGB: return new[] { R, G, B };
                case ColorSpace.HSV:
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
                    return new[] { h / 360.0 * 255, s * 255, v * 255 };
                case ColorSpace.YUV:
                    double Y = 0.299 * R + 0.587 * G + 0.114 * B;
                    double U = -0.147 * R - 0.289 * G + 0.436 * B + 128;
                    double V = 0.615 * R - 0.515 * G - 0.100 * B + 128;
                    return new[] { Y, U, V };
                case ColorSpace.YCbCr:
                    double Yc = 0.299 * R + 0.587 * G + 0.114 * B;
                    double Cb = (B - Yc) * 0.564 + 128;
                    double Cr = (R - Yc) * 0.713 + 128;
                    return new[] { Yc, Cb, Cr };
                case ColorSpace.LAB:
                    return RGBtoLABArray(R, G, B); // وظيفة منفصلة ترجع L,a,b في المجال 0-255
                case ColorSpace.CMYK:
                    double r1 = R / 255.0, g1 = G / 255.0, b1 = B / 255.0;
                    double K = 1 - Math.Max(r1, Math.Max(g1, b1));
                    double C = (1 - r1 - K) / (1 - K + 1e-8);
                    double M = (1 - g1 - K) / (1 - K + 1e-8);
                    double Ycmy = (1 - b1 - K) / (1 - K + 1e-8);
                    return new[] { C * 255, M * 255, Ycmy * 255, K * 255 };
                default: return new[] { R, G, B };
            }
        }





        private double[] RGBtoLABArray(double R, double G, double B)
        {
            double r = R / 255.0, g = G / 255.0, b = B / 255.0;
            r = (r > 0.04045) ? Math.Pow((r + 0.055) / 1.055, 2.4) : r / 12.92;
            g = (g > 0.04045) ? Math.Pow((g + 0.055) / 1.055, 2.4) : g / 12.92;
            b = (b > 0.04045) ? Math.Pow((b + 0.055) / 1.055, 2.4) : b / 12.92;
            double X = r * 0.4124564 + g * 0.3575761 + b * 0.1804375;
            double Y = r * 0.2126729 + g * 0.7151522 + b * 0.0721750;
            double Z = r * 0.0193339 + g * 0.1191920 + b * 0.9503041;
            X /= 0.95047; Y /= 1.00000; Z /= 1.08883;
            Func<double, double> f = t => (t > 0.008856) ? Math.Pow(t, 1.0 / 3.0) : (7.787 * t + 16.0 / 116.0);
            double L = 116.0 * f(Y) - 16.0;
            double a = 500.0 * (f(X) - f(Y));
            double b_ = 200.0 * (f(Y) - f(Z));
            // نطبيع L [0-100] -> [0-255], a,b [-128..127] -> [0-255]
            double Ln = L / 100.0 * 255;
            double an = a + 128;
            double bn = b_ + 128;
            // في LAB نستخدم فقط 3 قنوات، لكن channelCount قد يكون 4 (للتوافق)، نضيف قناة رابعة صفر
            return new double[] { Ln, an, bn, 0 };
        }

        //private void UpdateChannelFromSlider(int idx)
        //{
        //    if (idx >= channelValues.Length) return;
        //    channelValues[idx].Value = channelSliders[idx].Value;
        //        RebuildImageFromChannels();
        //}

        //private void UpdateChannelFromNumeric(int idx)
        //{
        //    channelSliders[idx].Value = (int)channelValues[idx].Value;
        //  //  if (applyButton.Enabled)
        //        RebuildImageFromChannels();
        //}

        private void ToggleChannelDisable(int idx)
        {
            channelDisabled[idx] = !channelDisabled[idx];
            disableButtons[idx].Text = channelDisabled[idx] ? "تمكين" : "تعطيل";
            disableButtons[idx].BackColor = channelDisabled[idx] ? Color.DarkGreen : Color.DarkRed;
            RebuildImageFromChannels();
        }

        private void ToggleShowChannelOnly(int idx)
        {
            showChannelOnly[idx] = showChannelOnlyCheckBoxes[idx].Checked;
            RebuildImageFromChannels();
        }

        private void RebuildImageFromChannels()
        {
            if (currentBitmap == null || currentChannels == null) return;
            int w = currentBitmap.Width;
            int h = currentBitmap.Height;
            Bitmap newBmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            // قفل البكسلين في الصورة الجديدة
            System.Drawing.Imaging.BitmapData bmpData = newBmp.LockBits(
                new Rectangle(0, 0, w, h),
                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                for (int y = 0; y < h; y++)
                {
                    byte* row = ptr + y * bmpData.Stride;
                    for (int x = 0; x < w; x++)
                    {
                        double[] ch = new double[channelCount];
                        // جمع القنوات حسب حالة العرض والتعطيل
                        for (int i = 0; i < channelCount; i++)
                        {
                            double originalVal = currentChannels[x, y, i];
                            if (showChannelOnly[i])
                            {
                                ch[i] = originalVal;
                                for (int j = 0; j < channelCount; j++)
                                    if (j != i) ch[j] = (currentColorSpace == ColorSpace.YUV || currentColorSpace == ColorSpace.YCbCr) ? 128 : 0;
                                break;
                            }
                            else
                            {
                                double modified = originalVal;
                                if (channelDisabled[i])
                                    modified = (currentColorSpace == ColorSpace.YUV || currentColorSpace == ColorSpace.YCbCr) ? 128 : 0;
                                else
                                {
                                    double factor = (channelSliders[i].Value - 128) / 128.0;
                                    double maxDelta = (currentColorSpace == ColorSpace.HSV && i == 0) ? 255 : 128;
                                    modified += factor * maxDelta;
                                    modified = Math.Max(0, Math.Min(255, modified));
                                }
                                ch[i] = modified;
                            }
                        }
                        Color finalColor = ChannelsToColor(ch, currentColorSpace);
                        row[x * 4] = finalColor.B;     // Blue
                        row[x * 4 + 1] = finalColor.G; // Green
                        row[x * 4 + 2] = finalColor.R; // Red
                        row[x * 4 + 3] = finalColor.A; // Alpha
                    }
                }
            }
            newBmp.UnlockBits(bmpData);

            Bitmap old = currentBitmap;
            currentBitmap = newBmp;
            SetImage(currentBitmap);
            old.Dispose();
            UpdateImageInfo();
        }

        private Color ChannelsToColor(double[] ch, ColorSpace space)
        {
            switch (space)
            {
                case ColorSpace.RGB:
                    return Color.FromArgb((int)ch[0], (int)ch[1], (int)ch[2]);
                case ColorSpace.HSV:
                    double h = ch[0] / 255.0 * 360.0;
                    double s = ch[1] / 255.0;
                    double v = ch[2] / 255.0;
                    return HSVtoRGB(h, s, v);
                case ColorSpace.YUV:
                    double Y = ch[0];
                    double U = ch[1] - 128;
                    double V = ch[2] - 128;
                    double R = Y + 1.13983 * V;
                    double G = Y - 0.39465 * U - 0.58060 * V;
                    double B = Y + 2.03211 * U;
                    return Color.FromArgb(ClampByte((int)R), ClampByte((int)G), ClampByte((int)B));
                case ColorSpace.YCbCr:
                    double Yc = ch[0];
                    double Cb = ch[1] - 128;
                    double Cr = ch[2] - 128;
                    double Rc = Yc + 1.40200 * Cr;
                    double Gc = Yc - 0.34414 * Cb - 0.71414 * Cr;
                    double Bc = Yc + 1.77200 * Cb;
                    return Color.FromArgb(ClampByte((int)Rc), ClampByte((int)Gc), ClampByte((int)Bc));
                case ColorSpace.LAB:
                    double L = ch[0] / 255.0 * 100.0;
                    double a = ch[1] - 128;
                    double b = ch[2] - 128;
                    return LABtoRGB(L, a, b);
                case ColorSpace.CMYK:
                    double C = ch[0] / 255.0;
                    double M = ch[1] / 255.0;
                    double Ycmy = ch[2] / 255.0;
                    double K = (ch.Length > 3) ? ch[3] / 255.0 : 0;
                    double Rcmy = (1 - C) * (1 - K);
                    double Gcmy = (1 - M) * (1 - K);
                    double Bcmy = (1 - Ycmy) * (1 - K);
                    return Color.FromArgb(ClampByte((int)(Rcmy * 255)), ClampByte((int)(Gcmy * 255)), ClampByte((int)(Bcmy * 255)));
                default:
                    return Color.Black;
            }
        }

        private Color HSVtoRGB(double H, double S, double V)
        {
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
            // التحويل من LAB إلى XYZ ثم إلى RGB (مرجع D65)
            double y = (L + 16) / 116.0;
            double x = a / 500.0 + y;
            double z = y - b / 200.0;
            double[] xyz = new double[3];
            Func<double, double> fInv = t => (Math.Pow(t, 3) > 0.008856) ? Math.Pow(t, 3) : (t - 16.0 / 116.0) / 7.787;
            xyz[0] = fInv(x) * 0.95047;
            xyz[1] = fInv(y) * 1.00000;
            xyz[2] = fInv(z) * 1.08883;
            // XYZ to RGB (sRGB)
            double r = xyz[0] * 3.2404542 + xyz[1] * -1.5371385 + xyz[2] * -0.4985314;
            double g = xyz[0] * -0.9692660 + xyz[1] * 1.8760108 + xyz[2] * 0.0415560;
            double b_ = xyz[0] * 0.0556434 + xyz[1] * -0.2040259 + xyz[2] * 1.0572252;
            r = (r > 0.0031308) ? 1.055 * Math.Pow(r, 1 / 2.4) - 0.055 : 12.92 * r;
            g = (g > 0.0031308) ? 1.055 * Math.Pow(g, 1 / 2.4) - 0.055 : 12.92 * g;
            b_ = (b_ > 0.0031308) ? 1.055 * Math.Pow(b_, 1 / 2.4) - 0.055 : 12.92 * b_;
            return Color.FromArgb(ClampByte((int)(r * 255)), ClampByte((int)(g * 255)), ClampByte((int)(b_ * 255)));
        }

        //private double Clamp(double val, double min, double max) => Math.Max(min, Math.Min(max, val));



    }



}
