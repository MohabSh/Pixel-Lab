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
            // Toolbar
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
            toolbar.Controls.Add(btnSave);
            toolbar.Controls.Add(btnLoad);
            toolbar.Controls.Add(btnReset);

            // System tabs
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

            // Drop zone
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

            // Split view
            Panel viewPanel = new Panel()
            {
                Dock = DockStyle.Fill,
                Visible = false
            };

            // Original
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

            // Modified
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

            // Right pane
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
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };
            rightPane.Controls.Add(channelPanel);
            rightPane.Controls.Add(lblChTitle);

            // Checkbox row
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

            // Status bar
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

            // Content area
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

            // Events
            btnLoad.Click += (s, e) =>
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";
                    if (ofd.ShowDialog() == DialogResult.OK)
                        LoadImage(ofd.FileName, dropZone, viewPanel, rightPane, origPanel, contentArea);
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
                    MessageBox.Show("لا توجد صورة لحفظها!", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Title = "حفظ الصورة";
                    sfd.Filter = "PNG Image|*.png|JPEG Image|*.jpg|Bitmap Image|*.bmp";
                    sfd.FileName = "edited_image";

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        SaveImageToDisk(sfd.FileName);
                        MessageBox.Show("تم حفظ الصورة بنجاح!", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            };

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
                    InitChannels();
                    ApplyChannels();
                };
            }

            Action<object, DragEventArgs> doDragEnter = (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    e.Effect = DragDropEffects.Copy;
            };
            Action<object, DragEventArgs> doDragDrop = (s, e) =>
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0 && IsImage(files[0]))
                    LoadImage(files[0], dropZone, viewPanel, rightPane, origPanel, contentArea);
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
                string res = $"  بكسل ({pt.X}, {pt.Y}) → ";
                if (chkRGB.Checked) res += $"RGB({c.R}, {c.G}, {c.B})   ";
                if (chkHSV.Checked)
                {
                    var v = ColorConverters.RGBtoHSV(c);
                    res += $"HSV({v.H:0}°, {v.S:0.00}, {v.V:0.00})   ";
                }
                if (chkYUV.Checked)
                {
                    var v = ColorConverters.RGBtoYUV(c);
                    res += $"YUV({v.Y:0}, {v.U:0}, {v.V:0})   ";
                }
                if (chkYCbCr.Checked)
                {
                    var v = ColorConverters.RGBtoYCbCr(c);
                    res += $"YCbCr({v.Y:0}, {v.Cb:0}, {v.Cr:0})   ";
                }
                if (chkLAB.Checked)
                {
                    var v = ColorConverters.RGBtoLAB(c);
                    res += $"LAB({v.L:0}, {v.A:0}, {v.B:0})   ";
                }
                if (chkCMYK.Checked)
                {
                    var v = ColorConverters.RGBtoCMYK(c);
                    res += $"CMYK({v.C:0.00}, {v.M:0.00}, {v.Y:0.00}, {v.K:0.00})   ";
                }
                if (res.EndsWith("→ ")) res += "(لا يوجد نظام محدد)";
                lblResult.Text = res;
            };

            Resize += (s, e) =>
            {
                if (viewPanel.Visible)
                    origPanel.Width = (contentArea.Width - rightPane.Width) / 2;
            };
        }

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
            {
                Text = text,
                Left = x,
                Top = 22,
                Width = 60
            };
        }

        void LoadImage(string path,
            Panel dropZone, Panel viewPanel,
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

            int halfW = (contentArea.Width - rightPane.Width) / 2;
            origPanel.Width = halfW;

            InitChannels();
            ApplyChannels();
        }

        void InitChannels()
        {
            var sys = ColorSystemFactory.GetSystem(activeSys);
            int n = sys.Names.Length;
            chEnabled = new bool[n];
            chOffset = new double[n];
            for (int i = 0; i < n; i++) { chEnabled[i] = true; chOffset[i] = 0; }
            BuildChannelUI(sys);
        }

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
                int slMin = (int)(mn - (mn + mx) / 2);
                int slMax = (int)(mx - (mn + mx) / 2);

                TrackBar sl = new TrackBar()
                {
                    Left = 16,
                    Top = 44,
                    Width = 224,
                    Minimum = slMin,
                    Maximum = slMax,
                    Value = 0,
                    TickFrequency = Math.Max(1, (slMax - slMin) / 10),
                    SmallChange = 1
                };

                chk.CheckedChanged += (s, e) =>
                {
                    chEnabled[ci] = chk.Checked;
                    sl.Enabled = chk.Checked;
                    ApplyChannels();
                };

                sl.ValueChanged += (s, e) =>
                {
                    chOffset[ci] = sl.Value;
                    valLbl.Text = $"تعديل: {(sl.Value >= 0 ? "+" : "")}{sl.Value}";
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

        void ApplyChannels()
        {
            if (originalBitmap == null) return;
            var sys = ColorSystemFactory.GetSystem(activeSys);
            int W = originalBitmap.Width;
            int H = originalBitmap.Height;

            Rectangle rect = new Rectangle(0, 0, W, H);
            BitmapData srcD = originalBitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            modifiedBitmap?.Dispose();
            modifiedBitmap = new Bitmap(W, H, PixelFormat.Format32bppArgb);
            BitmapData dstD = modifiedBitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            unsafe
            {
                byte* src = (byte*)srcD.Scan0;
                byte* dst = (byte*)dstD.Scan0;
                int stride = srcD.Stride;

                for (int py = 0; py < H; py++)
                {
                    for (int px = 0; px < W; px++)
                    {
                        int idx = py * stride + px * 4;
                        byte b0 = src[idx];
                        byte g0 = src[idx + 1];
                        byte r0 = src[idx + 2];
                        byte a0 = src[idx + 3];

                        double[] space = sys.ToSpace(r0, g0, b0);
                        int n = sys.Names.Length;

                        for (int c = 0; c < n; c++)
                        {
                            if (!chEnabled[c])
                                space[c] = (sys.Ranges[c].min + sys.Ranges[c].max) / 2.0;
                            else
                                space[c] += chOffset[c];

                            space[c] = Math.Min(sys.Ranges[c].max, Math.Max(sys.Ranges[c].min, space[c]));
                        }

                        int[] rgb = sys.FromSpace(space);
                        dst[idx] = (byte)Clamp(rgb[2]);
                        dst[idx + 1] = (byte)Clamp(rgb[1]);
                        dst[idx + 2] = (byte)Clamp(rgb[0]);
                        dst[idx + 3] = a0;
                    }
                }
            }

            originalBitmap.UnlockBits(srcD);
            modifiedBitmap.UnlockBits(dstD);

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

            int x = (int)((mouse.X - imgRect.Left) * xRatio);
            int y = (int)((mouse.Y - imgRect.Top) * yRatio);

            return new Point(x, y);
        }

        Rectangle GetImageRectangle(PictureBox pb)
        {
            if (pb.Image == null) return Rectangle.Empty;

            int imgW = pb.Image.Width;
            int imgH = pb.Image.Height;
            int boxW = pb.ClientSize.Width;
            int boxH = pb.ClientSize.Height;

            float imgRatio = (float)imgW / imgH;
            float boxRatio = (float)boxW / boxH;

            int drawW, drawH;
            if (imgRatio > boxRatio)
            {
                drawW = boxW;
                drawH = (int)(boxW / imgRatio);
            }
            else
            {
                drawH = boxH;
                drawW = (int)(boxH * imgRatio);
            }

            int x = (boxW - drawW) / 2;
            int y = (boxH - drawH) / 2;

            return new Rectangle(x, y, drawW, drawH);
        }
        //update image info
        void UpdateImageInfo(string path)
        {
            try
            {
                var fileInfo = new System.IO.FileInfo(path);

                string fileName = fileInfo.Name;
                string ext = fileInfo.Extension.ToUpper();
                double sizeKB = fileInfo.Length / 1024.0;
                string sizeStr = sizeKB < 1024
                    ? $"{sizeKB:0.0} KB"
                    : $"{sizeKB / 1024.0:0.0} MB";

                int w = originalBitmap.Width;
                int h = originalBitmap.Height;

                string pixelFormat = originalBitmap.PixelFormat.ToString();

                lblImageInfo.Text =
                    $"الاسم: {fileName}   |   الصيغة: {ext}   |   الحجم: {sizeStr}   |   الأبعاد: {w}×{h}   |   PixelFormat: {pixelFormat}";
            }
            catch
            {
                lblImageInfo.Text = "تعذّر قراءة معلومات الصورة";
            }
        }
        //save Image 
        void SaveImageToDisk(string path)
        {
            try
            {
                string ext = System.IO.Path.GetExtension(path).ToLower();

                switch (ext)
                {
                    case ".png":
                        modifiedBitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                        break;

                    case ".jpg":
                    case ".jpeg":
                        modifiedBitmap.Save(path, System.Drawing.Imaging.ImageFormat.Jpeg);
                        break;

                    case ".bmp":
                        modifiedBitmap.Save(path, System.Drawing.Imaging.ImageFormat.Bmp);
                        break;

                    default:
                        MessageBox.Show("صيغة غير مدعومة!", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء الحفظ:\n" + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


    }
}
 