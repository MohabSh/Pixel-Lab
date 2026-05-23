using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace ImageLab
{
    public class MainForm : Form
    {
        // UI
        PictureBox pictureBox;
        PictureBox origBox;
        CheckBox chkRGB, chkHSV, chkYUV, chkYCbCr, chkLAB, chkCMYK;
        Button[] sysBtns;
        Panel channelPanel;
        Label lblResult;
        Label lblImageInfo;
        Label lblOrigTitle, lblModTitle;

        // Bitmaps
        Bitmap originalBitmap = null;
        Bitmap modifiedBitmap = null;

        // Channels
        bool[] chEnabled;
        double[] chOffset;

        string activeSys = "RGB";

        // نوافذ مفتوحة
        ColorSpaceForm _spaceForm = null;

        public MainForm()
        {
            Text = "Image Lab - Color Systems";
            Width = 1000;
            Height = 780;
            MinimumSize = new Size(900, 720);
            BackColor = Color.FromArgb(245, 245, 248);
            BuildUI();
        }

        void BuildUI()
        {
            // ---- Toolbar ----
            Panel toolbar = new Panel()
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.White,
                Padding = new Padding(10, 8, 10, 8)
            };

            Button btnLoad = MakeButton("📂  تحميل صورة", 10, 10);
            Button btnReset = MakeButton("↺  إعادة تعيين", 145, 10);
            Button btnSave = MakeButton("💾  حفظ الصورة", 280, 10);

            // ---- الأزرار الجديدة ----
            Button btnSpace = MakeButton("🎨  الفضاء اللوني", 415, 10);
            btnSpace.BackColor = Color.FromArgb(90, 60, 180);

            Button btnQuant = MakeButton("🎞  تقليل الألوان", 550, 10);
            btnQuant.BackColor = Color.FromArgb(160, 80, 20);

            toolbar.Controls.Add(btnSave);
            toolbar.Controls.Add(btnLoad);
            toolbar.Controls.Add(btnReset);
            toolbar.Controls.Add(btnSpace);
            toolbar.Controls.Add(btnQuant);

            // ---- System tabs ----
            Panel sysBar = new Panel()
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.FromArgb(235, 235, 240),
                Padding = new Padding(10, 5, 10, 5)
            };

            string[] sysNames = { "RGB", "HSV", "YUV", "YCbCr", "LAB", "CMYK" };
            sysBtns = new Button[sysNames.Length];
            int bx = 10;
            for (int i = 0; i < sysNames.Length; i++)
            {
                Button b = MakeSysTab(sysNames[i], bx, 5);
                sysBtns[i] = b;
                sysBar.Controls.Add(b);
                bx += b.Width + 6;
            }
            sysBtns[0].BackColor = Color.FromArgb(55, 138, 221);
            sysBtns[0].ForeColor = Color.White;

            // ---- Drop zone ----
            Panel dropZone = new Panel()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                AllowDrop = true
            };
            Label dropLabel = new Label()
            {
                Text = "اسحب وأفلت صورة هنا أو اضغط «تحميل صورة»",
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 14, FontStyle.Regular),
                ForeColor = Color.Gray
            };
            dropZone.Controls.Add(dropLabel);

            // ---- Split view ----
            Panel viewPanel = new Panel()
            { Dock = DockStyle.Fill, Visible = false };

            Panel origPanel = new Panel() { Dock = DockStyle.Left, Width = 0 };
            lblOrigTitle = new Label()
            {
                Text = "الصورة الأصلية",
                Dock = DockStyle.Top,
                Height = 24,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(220, 230, 255),
                ForeColor = Color.FromArgb(30, 60, 130)
            };
            origBox = new PictureBox()
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(240, 240, 245)
            };
            origPanel.Controls.Add(origBox);
            origPanel.Controls.Add(lblOrigTitle);

            Panel modPanel = new Panel() { Dock = DockStyle.Fill };
            lblModTitle = new Label()
            {
                Text = "الصورة المعدّلة  [RGB]",
                Dock = DockStyle.Top,
                Height = 24,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(220, 245, 230),
                ForeColor = Color.FromArgb(15, 110, 86)
            };
            pictureBox = new PictureBox()
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(240, 245, 240),
                AllowDrop = true
            };
            modPanel.Controls.Add(pictureBox);
            modPanel.Controls.Add(lblModTitle);

            viewPanel.Controls.Add(modPanel);
            viewPanel.Controls.Add(origPanel);

            // ---- Right pane ----
            Panel rightPane = new Panel()
            {
                Dock = DockStyle.Right,
                Width = 270,
                BackColor = Color.White,
                Padding = new Padding(10),
                AutoScroll = true,
                Visible = false
            };
            Label lblChTitle = new Label()
            {
                Text = "التحكم بالمركبات اللونية",
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Arial", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 80),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(4, 0, 0, 0)
            };
            channelPanel = new Panel()
            { Dock = DockStyle.Fill, AutoScroll = true };
            rightPane.Controls.Add(channelPanel);
            rightPane.Controls.Add(lblChTitle);

            // ---- Checkboxes ----
            GroupBox grpChk = new GroupBox()
            {
                Text = "عرض قيم عند مؤشر الماوس",
                Dock = DockStyle.Bottom,
                Height = 60,
                Font = new Font("Arial", 8),
                ForeColor = Color.FromArgb(90, 90, 110)
            };
            chkRGB = MakeChk("RGB", 15);
            chkHSV = MakeChk("HSV", 80);
            chkYUV = MakeChk("YUV", 145);
            chkYCbCr = MakeChk("YCbCr", 210);
            chkLAB = MakeChk("LAB", 295);
            chkCMYK = MakeChk("CMYK", 360);
            grpChk.Controls.AddRange(new Control[]
                { chkRGB, chkHSV, chkYUV, chkYCbCr, chkLAB, chkCMYK });

            // ---- Status bars ----
            lblResult = new Label()
            {
                Text = "حرّك الماوس فوق الصورة لرؤية قيم البكسل",
                Dock = DockStyle.Bottom,
                Height = 32,
                Font = new Font("Consolas", 8),
                BackColor = Color.FromArgb(30, 30, 40),
                ForeColor = Color.FromArgb(180, 220, 180),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };
            lblImageInfo = new Label()
            {
                Text = "معلومات الصورة غير متوفرة",
                Dock = DockStyle.Bottom,
                Height = 40,
                Font = new Font("Consolas", 8),
                BackColor = Color.FromArgb(45, 45, 55),
                ForeColor = Color.FromArgb(200, 200, 220),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };

            // ---- Content area ----
            Panel contentArea = new Panel() { Dock = DockStyle.Fill };
            contentArea.Controls.Add(viewPanel);
            contentArea.Controls.Add(dropZone);

            Controls.Add(contentArea);
            Controls.Add(rightPane);
            Controls.Add(grpChk);
            Controls.Add(lblResult);
            Controls.Add(lblImageInfo);
            Controls.Add(sysBar);
            Controls.Add(toolbar);

            // ================================================================
            //  أحداث
            // ================================================================

            btnLoad.Click += (s, e) =>
            {
                using (var ofd = new OpenFileDialog())
                {
                    ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
                    if (ofd.ShowDialog() == DialogResult.OK)
                        LoadImage(ofd.FileName, dropZone, viewPanel,
                            rightPane, origPanel, contentArea);
                }
            };

            btnReset.Click += (s, e) =>
            {
                if (originalBitmap == null) return;
                InitChannels();
                ApplyChannels();
            };

            btnSave.Click += (s, e) =>
            {
                if (modifiedBitmap == null)
                {
                    MessageBox.Show("لا توجد صورة لحفظها!", "تنبيه",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                using (var sfd = new SaveFileDialog())
                {
                    sfd.Title = "حفظ الصورة";
                    sfd.Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap Image|*.bmp";
                    sfd.FileName = "edited_image";
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        SaveImageToDisk(sfd.FileName);
                        MessageBox.Show("تم حفظ الصورة بنجاح!", "نجاح",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            };

            // ---- زر الفضاء اللوني ----
            btnSpace.Click += (s, e) =>
            {
                if (_spaceForm != null && !_spaceForm.IsDisposed)
                {
                    _spaceForm.BringToFront();
                    return;
                }

                Color initColor = Color.FromArgb(100, 149, 237);
                if (originalBitmap != null)
                {
                    int mx = originalBitmap.Width / 2;
                    int my = originalBitmap.Height / 2;
                    initColor = originalBitmap.GetPixel(mx, my);
                }

                _spaceForm = new ColorSpaceForm(activeSys, initColor);

                // مزامنة: عند اختيار لون من الفضاء، أظهر قيمه في الـ status bar
                _spaceForm.ColorPicked += (picked) =>
                {
                    lblResult.Text =
                        $"  لون مختار من الفضاء → " +
                        $"RGB({picked.R}, {picked.G}, {picked.B})   " +
                        $"HSV({ColorConverters.RGBtoHSV(picked).H:0}°)   " +
                        $"LAB({ColorConverters.RGBtoLAB(picked).L:0})";
                };

                _spaceForm.Show(this);
            };

            // ---- زر تقليل الألوان ----
            btnQuant.Click += (s, e) =>
            {
                if (originalBitmap == null)
                {
                    MessageBox.Show("الرجاء تحميل صورة أولاً!", "تنبيه",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var qf = new QuantizeForm((Bitmap)originalBitmap.Clone());
                qf.QuantizeApplied += (quantized) =>
                {
                    modifiedBitmap?.Dispose();
                    modifiedBitmap = quantized;
                    pictureBox.Image?.Dispose();
                    pictureBox.Image = (Bitmap)modifiedBitmap.Clone();
                    lblModTitle.Text = $"الصورة المعدّلة  [تكميم ألوان]";
                };
                qf.ShowDialog(this);
            };

            // ---- أزرار تبديل النظام ----
            foreach (Button b in sysBtns)
            {
                b.Click += (s, e) =>
                {
                    activeSys = ((Button)s).Text;
                    lblModTitle.Text = $"الصورة المعدّلة  [{activeSys}]";
                    foreach (Button x in sysBtns)
                    {
                        x.BackColor = Color.FromArgb(235, 235, 240);
                        x.ForeColor = Color.FromArgb(60, 60, 80);
                    }
                    ((Button)s).BackColor = Color.FromArgb(55, 138, 221);
                    ((Button)s).ForeColor = Color.White;

                    // تحديث نافذة الفضاء إن كانت مفتوحة
                    if (_spaceForm != null && !_spaceForm.IsDisposed)
                    {
                        _spaceForm.Close();
                        _spaceForm = null;
                    }

                    InitChannels();
                    ApplyChannels();
                };
            }

            // ---- Drag & Drop ----
            Action<object, DragEventArgs> doDragEnter = (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    e.Effect = DragDropEffects.Copy;
            };
            Action<object, DragEventArgs> doDragDrop = (s, e) =>
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0 && IsImage(files[0]))
                    LoadImage(files[0], dropZone, viewPanel,
                        rightPane, origPanel, contentArea);
            };

            pictureBox.AllowDrop = true;
            pictureBox.DragEnter += (s, e) => doDragEnter(s, e);
            pictureBox.DragDrop += (s, e) => doDragDrop(s, e);
            dropZone.AllowDrop = true;
            dropZone.DragEnter += (s, e) => doDragEnter(s, e);
            dropZone.DragDrop += (s, e) => doDragDrop(s, e);
            dropLabel.AllowDrop = true;
            dropLabel.DragEnter += (s, e) => doDragEnter(s, e);
            dropLabel.DragDrop += (s, e) => doDragDrop(s, e);
            dropLabel.Click += (s, e) => btnLoad.PerformClick();

            // ---- MouseMove على الصورة ----
            pictureBox.MouseMove += (s, e) =>
            {
                if (originalBitmap == null) return;
                Point pt = GetImagePixel(e.Location, pictureBox, originalBitmap);
                if (pt.X < 0 || pt.Y < 0 ||
                    pt.X >= originalBitmap.Width ||
                    pt.Y >= originalBitmap.Height)
                {
                    lblResult.Text = "المؤشر خارج حدود الصورة";
                    return;
                }
                Color c = originalBitmap.GetPixel(pt.X, pt.Y);

                // مزامنة مع نافذة الفضاء
                if (_spaceForm != null && !_spaceForm.IsDisposed)
                    _spaceForm.SetSelectedColor(c);

                string res = $"  بكسل ({pt.X}, {pt.Y}) → ";
                if (chkRGB.Checked) res += $"RGB({c.R}, {c.G}, {c.B})   ";
                if (chkHSV.Checked) { var v = ColorConverters.RGBtoHSV(c); res += $"HSV({v.H:0}°, {v.S:0.00}, {v.V:0.00})   "; }
                if (chkYUV.Checked) { var v = ColorConverters.RGBtoYUV(c); res += $"YUV({v.Y:0}, {v.U:0}, {v.V:0})   "; }
                if (chkYCbCr.Checked) { var v = ColorConverters.RGBtoYCbCr(c); res += $"YCbCr({v.Y:0}, {v.Cb:0}, {v.Cr:0})   "; }
                if (chkLAB.Checked) { var v = ColorConverters.RGBtoLAB(c); res += $"LAB({v.L:0}, {v.A:0}, {v.B:0})   "; }
                if (chkCMYK.Checked) { var v = ColorConverters.RGBtoCMYK(c); res += $"CMYK({v.C:0.00}, {v.M:0.00}, {v.Y:0.00}, {v.K:0.00})   "; }
                if (res.EndsWith("→ ")) res += "(لا يوجد نظام محدد)";
                lblResult.Text = res;
            };

            Resize += (s, e) =>
            {
                if (viewPanel.Visible)
                    origPanel.Width = (contentArea.Width - rightPane.Width) / 2;
            };
        }

        // ================================================================
        //  بناء واجهة المركبات (مع دعم نطاق CMYK الصغير)
        // ================================================================
        void BuildChannelUI(ColorSystem sys)
        {
            channelPanel.Controls.Clear();
            int y = 4;

            for (int i = sys.Names.Length - 1; i >= 0; i--)
            {
                int ci = i;
                Color accent = sys.Colors[ci];

                Panel row = new Panel()
                {
                    Left = 0,
                    Top = y,
                    Width = 248,
                    Height = 78,
                    BackColor = Color.FromArgb(248, 248, 252),
                    BorderStyle = BorderStyle.FixedSingle
                };

                Panel swatch = new Panel()
                {
                    Left = 6,
                    Top = 10,
                    Width = 8,
                    Height = 56,
                    BackColor = accent
                };

                CheckBox chk = new CheckBox()
                {
                    Text = sys.Names[ci],
                    Left = 20,
                    Top = 8,
                    Width = 210,
                    Checked = true,
                    Font = new Font("Arial", 9, FontStyle.Bold),
                    ForeColor = accent
                };

                Label valLbl = new Label()
                {
                    Text = "تعديل: +0",
                    Left = 20,
                    Top = 28,
                    Width = 210,
                    Font = new Font("Consolas", 7),
                    ForeColor = Color.Gray
                };

                var (mn, mx) = sys.Ranges[ci];
                double range = (mx - mn) / 2.0;
                bool isSmall = (mx - mn) <= 2.0;
                int scale = isSmall ? 100 : 1;

                int slMin = (int)Math.Floor(-range * scale);
                int slMax = (int)Math.Ceiling(range * scale);
                if (slMin == slMax) { slMin = -1; slMax = 1; }

                TrackBar sl = new TrackBar()
                {
                    Left = 16,
                    Top = 44,
                    Width = 224,
                    Minimum = slMin,
                    Maximum = slMax,
                    Value = 0,
                    TickFrequency = Math.Max(1, (slMax - slMin) / 10),
                    SmallChange = 1,
                    Tag = isSmall ? 1.0 / scale : 1.0
                };

                chk.CheckedChanged += (s, e) =>
                {
                    chEnabled[ci] = chk.Checked;
                    sl.Enabled = chk.Checked;
                    ApplyChannels();
                };

                sl.ValueChanged += (s, e) =>
                {
                    double factor = (double)sl.Tag;
                    chOffset[ci] = sl.Value * factor;
                    valLbl.Text = $"تعديل: {(chOffset[ci] >= 0 ? "+" : "")}{chOffset[ci]:0.00}";
                    ApplyChannels();
                };

                row.Controls.Add(swatch);
                row.Controls.Add(chk);
                row.Controls.Add(valLbl);
                row.Controls.Add(sl);
                channelPanel.Controls.Add(row);
                y += 84;
            }
        }

        // ================================================================
        //  باقي الدوال (بدون تغيير جوهري)
        // ================================================================
        Button MakeButton(string text, int x, int y)
        {
            return new Button()
            {
                Text = text,
                Left = x,
                Top = y,
                Width = 130,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(55, 138, 221),
                ForeColor = Color.White
            };
        }

        Button MakeSysTab(string text, int x, int y)
        {
            return new Button()
            {
                Text = text,
                Left = x,
                Top = y,
                Height = 24,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(235, 235, 240),
                ForeColor = Color.FromArgb(60, 60, 80),
                Padding = new Padding(8, 2, 8, 2)
            };
        }

        CheckBox MakeChk(string text, int x)
        {
            return new CheckBox()
            { Text = text, Left = x, Top = 22, Width = 60 };
        }

        void LoadImage(string path, Panel dropZone, Panel viewPanel,
            Panel rightPane, Panel origPanel, Panel contentArea)
        {
            originalBitmap?.Dispose();
            modifiedBitmap?.Dispose();

            Image img = Image.FromFile(path);
            originalBitmap = new Bitmap(img);
            modifiedBitmap = new Bitmap(img);
            UpdateImageInfo(path);
            img.Dispose();

            origBox.Image = (Bitmap)originalBitmap.Clone();
            pictureBox.Image = (Bitmap)modifiedBitmap.Clone();

            dropZone.Visible = false;
            viewPanel.Visible = true;
            rightPane.Visible = true;

            origPanel.Width = (contentArea.Width - rightPane.Width) / 2;

            InitChannels();
            ApplyChannels();
        }


        void InitChannels()
        {
            var sys = ColorSystemFactory.GetSystem(activeSys);

            int n = sys.Names.Length;

            chEnabled = new bool[n];
            chOffset = new double[n];

            for (int i = 0; i < n; i++)
            {
                chEnabled[i] = true;
                chOffset[i] = 0;
            }

            BuildChannelUI(sys);
        }

        void ApplyChannels()
        {
            if (originalBitmap == null) return;

            var sys = ColorSystemFactory.GetSystem(activeSys);

            var toSpace = sys.ToSpace;
            var fromSpace = sys.FromSpace;
            var ranges = sys.Ranges;
            var namesLen = sys.Names.Length;

            int w = originalBitmap.Width;
            int h = originalBitmap.Height;
            Rectangle rect = new Rectangle(0, 0, w, h);

            BitmapData src = originalBitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            if (modifiedBitmap != null) modifiedBitmap.Dispose();
            modifiedBitmap = new Bitmap(w, h, PixelFormat.Format32bppArgb);

            BitmapData dst = modifiedBitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            unsafe
            {
                byte* sp = (byte*)src.Scan0;
                byte* dp = (byte*)dst.Scan0;

                int stride = src.Stride;

                double[] space = new double[namesLen];
                int[] rgb;

                for (int y = 0; y < h; y++)
                {
                    int row = y * stride;

                    for (int x = 0; x < w; x++)
                    {
                        int i = row + x * 4;

                        byte b = sp[i];
                        byte g = sp[i + 1];
                        byte r = sp[i + 2];
                        byte a = sp[i + 3];

                        space = toSpace(r, g, b);

                        for (int c = 0; c < namesLen; c++)
                        {
                            if (!chEnabled[c])
                                space[c] = (ranges[c].min + ranges[c].max) * 0.5;
                            else
                                space[c] += chOffset[c];

                            if (space[c] < ranges[c].min) space[c] = ranges[c].min;
                            else if (space[c] > ranges[c].max) space[c] = ranges[c].max;
                        }

                        rgb = fromSpace(space);

                        dp[i] = (byte)Clamp(rgb[2]);
                        dp[i + 1] = (byte)Clamp(rgb[1]);
                        dp[i + 2] = (byte)Clamp(rgb[0]);
                        dp[i + 3] = a;
                    }
                }
            }

            originalBitmap.UnlockBits(src);
            modifiedBitmap.UnlockBits(dst);

            pictureBox.Image?.Dispose();
            pictureBox.Image = (Bitmap)modifiedBitmap.Clone();
        }
        int Clamp(int v) => v < 0 ? 0 : v > 255 ? 255 : v;

        bool IsImage(string path)
        {
            string ext = System.IO.Path.GetExtension(path).ToLower();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp";
        }

        Point GetImagePixel(Point mouse, PictureBox pb, Bitmap bmp)
        {
            if (pb.Image == null) return new Point(-1, -1);
            Rectangle imgRect = GetImageRectangle(pb);
            if (!imgRect.Contains(mouse)) return new Point(-1, -1);
            double xRatio = (double)bmp.Width / imgRect.Width;
            double yRatio = (double)bmp.Height / imgRect.Height;
            return new Point(
                (int)((mouse.X - imgRect.Left) * xRatio),
                (int)((mouse.Y - imgRect.Top) * yRatio));
        }

        Rectangle GetImageRectangle(PictureBox pb)
        {
            if (pb.Image == null) return Rectangle.Empty;
            int imgW = pb.Image.Width, imgH = pb.Image.Height;
            int boxW = pb.ClientSize.Width, boxH = pb.ClientSize.Height;
            float imgRatio = (float)imgW / imgH;
            float boxRatio = (float)boxW / boxH;
            int drawW, drawH;
            if (imgRatio > boxRatio) { drawW = boxW; drawH = (int)(boxW / imgRatio); }
            else { drawH = boxH; drawW = (int)(boxH * imgRatio); }
            return new Rectangle((boxW - drawW) / 2, (boxH - drawH) / 2, drawW, drawH);
        }

        void UpdateImageInfo(string path)
        {
            try
            {
                var fi = new System.IO.FileInfo(path);
                double kb = fi.Length / 1024.0;
                string sz = kb < 1024 ? $"{kb:0.0} KB" : $"{kb / 1024:0.0} MB";
                lblImageInfo.Text =
                    $"الاسم: {fi.Name}   |   الصيغة: {fi.Extension.ToUpper()}   |   " +
                    $"الحجم: {sz}   |   الأبعاد: {originalBitmap.Width}×{originalBitmap.Height}   |   " +
                    $"PixelFormat: {originalBitmap.PixelFormat}";
            }
            catch { lblImageInfo.Text = "تعذّر قراءة معلومات الصورة"; }
        }

        void SaveImageToDisk(string path)
        {
            try
            {
                switch (System.IO.Path.GetExtension(path).ToLower())
                {
                    case ".png": modifiedBitmap.Save(path, ImageFormat.Png); break;
                    case ".jpg":
                    case ".jpeg": modifiedBitmap.Save(path, ImageFormat.Jpeg); break;
                    case ".bmp": modifiedBitmap.Save(path, ImageFormat.Bmp); break;
                    default:
                        MessageBox.Show("صيغة غير مدعومة!", "خطأ",
                        MessageBoxButtons.OK, MessageBoxIcon.Error); break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ:\n" + ex.Message, "خطأ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}