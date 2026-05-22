using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace ImageLab
{
    // ============================================================
    //  واجهة خوارزمية التكميم
    // ============================================================
    public interface IColorQuantizer
    {
        string Name { get; }
        Bitmap Quantize(Bitmap source, int colorCount);
    }

    // ============================================================
    //  Median Cut - خوارزمية القطع المتوسط
    // ============================================================
    public class MedianCutQuantizer : IColorQuantizer
    {
        public string Name => "Median Cut";

        public Bitmap Quantize(Bitmap source, int colorCount)
        {
            // جمع كل بكسلات الصورة
            var pixels = GetPixels(source);

            // بناء لوحة الألوان بخوارزمية Median Cut
            var palette = MedianCut(pixels, colorCount);

            // تعيين كل بكسل إلى أقرب لون في اللوحة
            return ApplyPalette(source, palette);
        }

        List<Color> GetPixels(Bitmap bmp)
        {
            var pixels = new List<Color>(bmp.Width * bmp.Height);
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var data = bmp.LockBits(rect, ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            unsafe
            {
                byte* ptr = (byte*)data.Scan0;
                for (int y = 0; y < bmp.Height; y++)
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        int idx = y * data.Stride + x * 4;
                        pixels.Add(Color.FromArgb(ptr[idx + 3], ptr[idx + 2],
                            ptr[idx + 1], ptr[idx]));
                    }
            }

            bmp.UnlockBits(data);
            return pixels;
        }

        List<Color> MedianCut(List<Color> pixels, int targetCount)
        {
            // قائمة صناديق الألوان
            var buckets = new List<List<Color>> { pixels };

            while (buckets.Count < targetCount)
            {
                // ابحث عن الصندوق ذو أكبر نطاق لوني
                int widest = FindWidestBucket(buckets);
                if (widest < 0) break;

                var bucket = buckets[widest];
                buckets.RemoveAt(widest);

                // قسّم الصندوق على المحور ذي أكبر نطاق
                SplitBucket(bucket, out var b1, out var b2);
                if (b1.Count > 0) buckets.Add(b1);
                if (b2.Count > 0) buckets.Add(b2);
            }

            // احسب متوسط كل صندوق كلون ممثّل
            return buckets
                .Where(b => b.Count > 0)
                .Select(AverageColor)
                .ToList();
        }

        int FindWidestBucket(List<List<Color>> buckets)
        {
            int idx = -1;
            int maxRange = -1;
            for (int i = 0; i < buckets.Count; i++)
            {
                var b = buckets[i];
                if (b.Count < 2) continue;
                int rng = ColorRange(b);
                if (rng > maxRange) { maxRange = rng; idx = i; }
            }
            return idx;
        }

        int ColorRange(List<Color> bucket)
        {
            int rMin = 255, rMax = 0, gMin = 255, gMax = 0, bMin = 255, bMax = 0;
            foreach (var c in bucket)
            {
                if (c.R < rMin) rMin = c.R; if (c.R > rMax) rMax = c.R;
                if (c.G < gMin) gMin = c.G; if (c.G > gMax) gMax = c.G;
                if (c.B < bMin) bMin = c.B; if (c.B > bMax) bMax = c.B;
            }
            return Math.Max(rMax - rMin, Math.Max(gMax - gMin, bMax - bMin));
        }

        void SplitBucket(List<Color> bucket,
            out List<Color> b1, out List<Color> b2)
        {
            // ابحث عن المحور ذي أكبر نطاق
            int rRange = bucket.Max(c => c.R) - bucket.Min(c => c.R);
            int gRange = bucket.Max(c => c.G) - bucket.Min(c => c.G);
            int bRange = bucket.Max(c => c.B) - bucket.Min(c => c.B);

            List<Color> sorted;
            if (rRange >= gRange && rRange >= bRange)
                sorted = bucket.OrderBy(c => c.R).ToList();
            else if (gRange >= bRange)
                sorted = bucket.OrderBy(c => c.G).ToList();
            else
                sorted = bucket.OrderBy(c => c.B).ToList();

            int mid = sorted.Count / 2;
            b1 = sorted.Take(mid).ToList();
            b2 = sorted.Skip(mid).ToList();
        }

        Color AverageColor(List<Color> bucket)
        {
            long r = 0, g = 0, b = 0;
            foreach (var c in bucket) { r += c.R; g += c.G; b += c.B; }
            int n = bucket.Count;
            return Color.FromArgb((int)(r / n), (int)(g / n), (int)(b / n));
        }

        Bitmap ApplyPalette(Bitmap source, List<Color> palette)
        {
            var result = new Bitmap(source.Width, source.Height,
                PixelFormat.Format32bppArgb);

            var srcRect = new Rectangle(0, 0, source.Width, source.Height);
            var srcData = source.LockBits(srcRect,
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var dstData = result.LockBits(srcRect,
                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            unsafe
            {
                byte* src = (byte*)srcData.Scan0;
                byte* dst = (byte*)dstData.Scan0;

                for (int y = 0; y < source.Height; y++)
                    for (int x = 0; x < source.Width; x++)
                    {
                        int idx = y * srcData.Stride + x * 4;
                        byte b0 = src[idx], g0 = src[idx + 1],
                             r0 = src[idx + 2], a0 = src[idx + 3];

                        Color nearest = FindNearest(Color.FromArgb(r0, g0, b0), palette);

                        dst[idx] = nearest.B;
                        dst[idx + 1] = nearest.G;
                        dst[idx + 2] = nearest.R;
                        dst[idx + 3] = a0;
                    }
            }

            source.UnlockBits(srcData);
            result.UnlockBits(dstData);
            return result;
        }

        Color FindNearest(Color c, List<Color> palette)
        {
            Color best = palette[0];
            long bestDist = long.MaxValue;
            foreach (var p in palette)
            {
                long dr = c.R - p.R, dg = c.G - p.G, db = c.B - p.B;
                long dist = dr * dr + dg * dg + db * db;
                if (dist < bestDist) { bestDist = dist; best = p; }
            }
            return best;
        }
    }

    // ============================================================
    //  نافذة التحكم بالتكميم
    // ============================================================
    public class QuantizeForm : Form
    {
        IColorQuantizer _quantizer = new MedianCutQuantizer();
        Bitmap _source;
        PictureBox _preview;
        TrackBar _slider;
        Label _lblCount;
        Label _lblAlgo;
        Panel _palettePanel;
        Bitmap _quantized;

        public event Action<Bitmap> QuantizeApplied;

        public QuantizeForm(Bitmap source)
        {
            _source = source;
            Text = "التحكم بعدد الألوان";
            Width = 500;
            Height = 560;
            MinimumSize = new Size(460, 500);
            BackColor = Color.FromArgb(22, 22, 30);
            BuildUI();
        }

        void BuildUI()
        {
            // ---- معاينة ----
            _preview = new PictureBox()
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(15, 15, 20),
                Image = (Bitmap)_source.Clone()
            };

            // ---- لوحة التحكم ----
            Panel ctrl = new Panel()
            {
                Dock = DockStyle.Bottom,
                Height = 180,
                BackColor = Color.FromArgb(30, 30, 42),
                Padding = new Padding(12)
            };

            _lblAlgo = new Label()
            {
                Text = "الخوارزمية: Median Cut",
                Left = 12,
                Top = 10,
                Width = 300,
                Height = 20,
                Font = new Font("Arial", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 180, 255)
            };

            _lblCount = new Label()
            {
                Text = "عدد الألوان: 16",
                Left = 12,
                Top = 36,
                Width = 280,
                Height = 20,
                Font = new Font("Consolas", 9),
                ForeColor = Color.FromArgb(200, 200, 220)
            };

            _slider = new TrackBar()
            {
                Left = 12,
                Top = 58,
                Width = 380,
                Minimum = 2,
                Maximum = 64,
                Value = 16,
                TickFrequency = 4,
                SmallChange = 1
            };

            _palettePanel = new Panel()
            {
                Left = 12,
                Top = 108,
                Width = 460,
                Height = 28,
                BackColor = Color.Transparent
            };

            Button btnApply = new Button()
            {
                Text = "✔ تطبيق على الصورة",
                Left = 320,
                Top = 130,
                Width = 150,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(55, 138, 100),
                ForeColor = Color.White,
                Font = new Font("Arial", 9)
            };

            ctrl.Controls.Add(_lblAlgo);
            ctrl.Controls.Add(_lblCount);
            ctrl.Controls.Add(_slider);
            ctrl.Controls.Add(_palettePanel);
            ctrl.Controls.Add(btnApply);

            Controls.Add(_preview);
            Controls.Add(ctrl);

            // ---- أحداث ----
            _slider.ValueChanged += (s, e) =>
            {
                _lblCount.Text = $"عدد الألوان: {_slider.Value}";
                UpdatePreview();
            };

            btnApply.Click += (s, e) =>
            {
                if (_quantized != null)
                    QuantizeApplied?.Invoke((Bitmap)_quantized.Clone());
            };

            UpdatePreview();
        }

        void UpdatePreview()
        {
            _quantized?.Dispose();
            _quantized = _quantizer.Quantize(_source, _slider.Value);
            _preview.Image?.Dispose();
            _preview.Image = (Bitmap)_quantized.Clone();
            DrawPalette(_quantized);
        }

        void DrawPalette(Bitmap bmp)
        {
            // استخراج الألوان الفريدة من الصورة المكمَّمة
            var colors = new HashSet<int>();
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var data = bmp.LockBits(rect, ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            unsafe
            {
                byte* ptr = (byte*)data.Scan0;
                for (int y = 0; y < bmp.Height; y += 4)
                    for (int x = 0; x < bmp.Width; x += 4)
                    {
                        int idx = y * data.Stride + x * 4;
                        int col = (ptr[idx + 2] << 16) | (ptr[idx + 1] << 8) | ptr[idx];
                        colors.Add(col);
                    }
            }

            bmp.UnlockBits(data);

            _palettePanel.Controls.Clear();
            int bx = 0;
            foreach (int col in colors.Take(64))
            {
                int r = (col >> 16) & 0xFF;
                int g = (col >> 8) & 0xFF;
                int b = col & 0xFF;
                var swatch = new Panel()
                {
                    Left = bx,
                    Top = 0,
                    Width = 20,
                    Height = 20,
                    BackColor = Color.FromArgb(r, g, b),
                    BorderStyle = BorderStyle.None
                };
                _palettePanel.Controls.Add(swatch);
                bx += 22;
                if (bx > 450) break;
            }
        }
    }
}