using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace ImageLab
{
    // ============================================================
    //  Factory - ينشئ المُصوِّر المناسب لكل نظام
    // ============================================================
    public static class ColorSpaceVisualizerFactory
    {
        public static IColorSpaceVisualizer Create(string systemName)
        {
            switch (systemName)
            {
                case "RGB": return new RGBVisualizer();
                case "HSV": return new HSVVisualizer();
                case "YUV": return new YUVVisualizer();
                case "YCbCr": return new YCbCrVisualizer();
                case "LAB": return new LABVisualizer();
                case "CMYK": return new CMYKVisualizer();
                default: return new RGBVisualizer();
            }
        }
    }

    // ============================================================
    //  RGB - مكعب الألوان (3D) + مستوى RG (2D)
    // ============================================================
    public class RGBVisualizer : IColorSpaceVisualizer
    {
        public string SystemName => "RGB";
        public bool Supports3D => true;

        public void Draw2D(Graphics g, int w, int h,
            Color sel, out PointF selPt)
        {
            // مستوى RG: عرض جميع قيم R,G مع B ثابتة عند قيمة sel
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    int r = (int)(x * 255.0 / w);
                    int gr = (int)((h - 1 - y) * 255.0 / h);
                    using (var pen = new SolidBrush(Color.FromArgb(r, gr, sel.B)))
                        g.FillRectangle(pen, x, y, 1, 1);
                }
            }
            // رسم المحاور
            DrawAxis(g, w, h, "R →", "↑ G");
            // نقطة اللون المختار
            selPt = new PointF(sel.R * w / 255f, (255 - sel.G) * h / 255f);
            DrawSelector(g, selPt);
        }

        public void Draw3D(Graphics g, int w, int h,
            Color sel, float rotX, float rotY, float zoom, out PointF selPt)
        {
            int cx = w / 2, cy = h / 2;
            int step = 12;

            // رسم نقاط المكعب
            for (int r = 0; r <= 255; r += step)
                for (int gv = 0; gv <= 255; gv += step)
                    for (int b = 0; b <= 255; b += step)
                    {
                        float fx = r / 255f - 0.5f;
                        float fy = gv / 255f - 0.5f;
                        float fz = b / 255f - 0.5f;
                        var pt = Projection3D.Project(fx, fy, fz, rotX, rotY, zoom, cx, cy);
                        var col = Color.FromArgb(r, gv, b);
                        using (var br = new SolidBrush(col))
                            g.FillEllipse(br, pt.X - 2, pt.Y - 2, 4, 4);
                    }

            // نقطة اللون المختار
            float sx = sel.R / 255f - 0.5f;
            float sy = sel.G / 255f - 0.5f;
            float sz = sel.B / 255f - 0.5f;
            selPt = Projection3D.Project(sx, sy, sz, rotX, rotY, zoom, cx, cy);
            DrawSelector3D(g, selPt);

            // تسمية المحاور
            DrawCubeAxes(g, rotX, rotY, zoom, cx, cy);
        }

        public Color PickColor2D(PointF pt, int w, int h)
        {
            int r = Clamp((int)(pt.X * 255 / w));
            int gv = Clamp((int)((h - pt.Y) * 255 / h));
            return Color.FromArgb(r, gv, 128);
        }

        public Color PickColor3D(PointF pt, int w, int h,
            float rotX, float rotY, float zoom)
            => Color.FromArgb(128, 128, 128); // تقريبي

        // ---- مساعدات ----
        void DrawAxis(Graphics g, int w, int h, string xLabel, string yLabel)
        {
            using (var f = new Font("Consolas", 8))
            using (var br = new SolidBrush(Color.FromArgb(200, 255, 255, 255)))
            {
                g.DrawString(xLabel, f, br, w - 30, h - 16);
                g.DrawString(yLabel, f, br, 4, 4);
            }
        }

        void DrawCubeAxes(Graphics g, float rotX, float rotY, float zoom,
            int cx, int cy)
        {
            string[] labels = { "R", "G", "B" };
            float[][] dirs = {
                new[]{ 0.5f, 0f, 0f },
                new[]{ 0f, 0.5f, 0f },
                new[]{ 0f, 0f, 0.5f }
            };
            Color[] cols = {
                Color.FromArgb(255, 80, 80),
                Color.FromArgb(80, 220, 80),
                Color.FromArgb(80, 140, 255)
            };
            using (var f = new Font("Consolas", 9, FontStyle.Bold))
            {
                for (int i = 0; i < 3; i++)
                {
                    var o = Projection3D.Project(0, 0, 0, rotX, rotY, zoom, cx, cy);
                    var e = Projection3D.Project(dirs[i][0], dirs[i][1], dirs[i][2],
                        rotX, rotY, zoom, cx, cy);
                    using (var p = new Pen(cols[i], 2))
                        g.DrawLine(p, o, e);
                    using (var br = new SolidBrush(cols[i]))
                        g.DrawString(labels[i], f, br, e.X + 2, e.Y - 8);
                }
            }
        }

        void DrawSelector(Graphics g, PointF pt)
        {
            using (var p = new Pen(Color.White, 2))
            {
                g.DrawEllipse(p, pt.X - 6, pt.Y - 6, 12, 12);
            }
            using (var p = new Pen(Color.Black, 1))
            {
                g.DrawEllipse(p, pt.X - 7, pt.Y - 7, 14, 14);
            }
        }

        void DrawSelector3D(Graphics g, PointF pt)
        {
            using (var p = new Pen(Color.White, 2))
                g.DrawEllipse(p, pt.X - 8, pt.Y - 8, 16, 16);
            using (var p = new Pen(Color.OrangeRed, 1.5f))
                g.DrawEllipse(p, pt.X - 9, pt.Y - 9, 18, 18);
        }

        int Clamp(int v) => v < 0 ? 0 : v > 255 ? 255 : v;
    }

    // ============================================================
    //  HSV - قرص الألوان (2D) + مخروط (3D)
    // ============================================================
    public class HSVVisualizer : IColorSpaceVisualizer
    {
        public string SystemName => "HSV";
        public bool Supports3D => true;

        public void Draw2D(Graphics g, int w, int h,
            Color sel, out PointF selPt)
        {
            // قرص HS: H زاوية، S نصف القطر، V ثابت عند 1
            int cx = w / 2, cy = h / 2;
            int r = Math.Min(cx, cy) - 8;

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    float dx = x - cx, dy = y - cy;
                    float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (dist > r) continue;

                    float h2 = (float)(Math.Atan2(dy, dx) * 180 / Math.PI + 360) % 360;
                    float s = dist / r;
                    var col = HSVtoRGB(h2, s, 1.0f);
                    using (var br = new SolidBrush(col))
                        g.FillRectangle(br, x, y, 1, 1);
                }
            }

            // رسم حدود القرص
            using (var p = new Pen(Color.FromArgb(60, 255, 255, 255), 1))
                g.DrawEllipse(p, cx - r, cy - r, r * 2, r * 2);

            // نقطة اللون المختار
            var hsv = RGBtoHSV(sel);
            float ang = (float)(hsv.H * Math.PI / 180);
            float sr = (float)hsv.S * r;
            selPt = new PointF(cx + sr * (float)Math.Cos(ang),
                               cy + sr * (float)Math.Sin(ang));
            DrawSelector(g, selPt);

            DrawLabel(g, "H = زاوية   S = مسافة من المركز", w, h);
        }

        public void Draw3D(Graphics g, int w, int h,
            Color sel, float rotX, float rotY, float zoom, out PointF selPt)
        {
            int cx = w / 2, cy = h / 2;
            int stepH = 15, stepS = 5, stepV = 5;

            for (int hv = 0; hv < 360; hv += stepH)
                for (int sv = 0; sv <= 100; sv += stepS)
                    for (int vv = 0; vv <= 100; vv += stepV)
                    {
                        float H = hv, S = sv / 100f, V = vv / 100f;
                        float ang = (float)(H * Math.PI / 180);
                        float fx = S * V * (float)Math.Cos(ang) * 0.5f;
                        float fy = V * 0.5f - 0.25f;
                        float fz = S * V * (float)Math.Sin(ang) * 0.5f;

                        var pt = Projection3D.Project(fx, fy, fz, rotX, rotY, zoom, cx, cy);
                        var col = HSVtoRGB(H, S, V);
                        using (var br = new SolidBrush(col))
                            g.FillEllipse(br, pt.X - 2, pt.Y - 2, 4, 4);
                    }

            var hsvSel = RGBtoHSV(sel);
            float sa = (float)(hsvSel.H * Math.PI / 180);
            float sfx = (float)hsvSel.S * (float)hsvSel.V * (float)Math.Cos(sa) * 0.5f;
            float sfy = (float)hsvSel.V * 0.5f - 0.25f;
            float sfz = (float)hsvSel.S * (float)hsvSel.V * (float)Math.Sin(sa) * 0.5f;
            selPt = Projection3D.Project(sfx, sfy, sfz, rotX, rotY, zoom, cx, cy);
            DrawSelector3D(g, selPt);
        }

        public Color PickColor2D(PointF pt, int w, int h)
        {
            int cx = w / 2, cy = h / 2;
            int r = Math.Min(cx, cy) - 8;
            float dx = pt.X - cx, dy = pt.Y - cy;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
            float H = (float)(Math.Atan2(dy, dx) * 180 / Math.PI + 360) % 360;
            float S = Math.Min(dist / r, 1f);
            return HSVtoRGB(H, S, 1f);
        }

        public Color PickColor3D(PointF pt, int w, int h,
            float rotX, float rotY, float zoom)
            => Color.FromArgb(128, 128, 128);

        // ---- مساعدات ----
        (double H, double S, double V) RGBtoHSV(Color c)
        {
            double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
            double mx = Math.Max(r, Math.Max(g, b));
            double mn = Math.Min(r, Math.Min(g, b));
            double d = mx - mn, h = 0;
            if (d != 0)
            {
                if (mx == r) h = 60 * (((g - b) / d) % 6);
                else if (mx == g) h = 60 * ((b - r) / d + 2);
                else h = 60 * ((r - g) / d + 4);
            }
            if (h < 0) h += 360;
            return (h, mx == 0 ? 0 : d / mx, mx);
        }

        Color HSVtoRGB(float h, float s, float v)
        {
            double c = v * s, x = c * (1 - Math.Abs((h / 60) % 2 - 1)), m = v - c;
            double r = 0, g = 0, b = 0;
            if (h < 60) { r = c; g = x; }
            else if (h < 120) { r = x; g = c; }
            else if (h < 180) { g = c; b = x; }
            else if (h < 240) { g = x; b = c; }
            else if (h < 300) { r = x; b = c; }
            else { r = c; b = x; }
            return Color.FromArgb(
                (int)Math.Round((r + m) * 255),
                (int)Math.Round((g + m) * 255),
                (int)Math.Round((b + m) * 255));
        }

        void DrawSelector(Graphics g, PointF pt)
        {
            using (var p = new Pen(Color.White, 2))
                g.DrawEllipse(p, pt.X - 6, pt.Y - 6, 12, 12);
            using (var p = new Pen(Color.Black, 1))
                g.DrawEllipse(p, pt.X - 7, pt.Y - 7, 14, 14);
        }

        void DrawSelector3D(Graphics g, PointF pt)
        {
            using (var p = new Pen(Color.White, 2))
                g.DrawEllipse(p, pt.X - 8, pt.Y - 8, 16, 16);
        }

        void DrawLabel(Graphics g, string text, int w, int h)
        {
            using (var f = new Font("Consolas", 7))
            using (var br = new SolidBrush(Color.FromArgb(180, 255, 255, 255)))
                g.DrawString(text, f, br, 4, h - 16);
        }
    }

    // ============================================================
    //  YUV - مستوى UV (2D) + فضاء ثلاثي الأبعاد (3D)
    // ============================================================
    public class YUVVisualizer : IColorSpaceVisualizer
    {
        public string SystemName => "YUV";
        public bool Supports3D => true;

        public void Draw2D(Graphics g, int w, int h,
            Color sel, out PointF selPt)
        {
            var yuv = RGBtoYUV(sel);
            // مستوى UV عند Y=sel.Y
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    double U = -111 + x * 222.0 / w;
                    double V = 156 - y * 312.0 / h;
                    var col = YUVtoRGB(yuv.Y, U, V);
                    using (var br = new SolidBrush(col))
                        g.FillRectangle(br, x, y, 1, 1);
                }
            }
            DrawAxis(g, w, h, "U →", "↑ V");
            selPt = new PointF(
                (float)((yuv.U + 111) / 222 * w),
                (float)((156 - yuv.V) / 312 * h));
            DrawSelector(g, selPt);
        }

        public void Draw3D(Graphics g, int w, int h,
            Color sel, float rotX, float rotY, float zoom, out PointF selPt)
        {
            int cx = w / 2, cy = h / 2;
            int step = 20;

            for (int r = 0; r <= 255; r += step)
                for (int gv = 0; gv <= 255; gv += step)
                    for (int b = 0; b <= 255; b += step)
                    {
                        double Y = 0.299 * r + 0.587 * gv + 0.114 * b;
                        double U = -0.14713 * r - 0.28886 * gv + 0.436 * b;
                        double V = 0.615 * r - 0.51499 * gv - 0.10001 * b;

                        float fx = (float)(U / 111);
                        float fy = (float)(Y / 255 - 0.5);
                        float fz = (float)(V / 156);

                        var pt = Projection3D.Project(fx * 0.5f, fy * 0.5f, fz * 0.5f,
                            rotX, rotY, zoom, cx, cy);
                        using (var br = new SolidBrush(Color.FromArgb(r, gv, b)))
                            g.FillEllipse(br, pt.X - 2, pt.Y - 2, 4, 4);
                    }

            var yuv = RGBtoYUV(sel);
            float sfx = (float)(yuv.U / 111 * 0.5f);
            float sfy = (float)(yuv.Y / 255 - 0.5) * 0.5f;
            float sfz = (float)(yuv.V / 156 * 0.5f);
            selPt = Projection3D.Project(sfx, sfy, sfz, rotX, rotY, zoom, cx, cy);
            DrawSelector3D(g, selPt);
        }

        public Color PickColor2D(PointF pt, int w, int h)
        {
            double U = -111 + pt.X * 222.0 / w;
            double V = 156 - pt.Y * 312.0 / h;
            return YUVtoRGB(128, U, V);
        }

        public Color PickColor3D(PointF pt, int w, int h,
            float rotX, float rotY, float zoom)
            => Color.FromArgb(128, 128, 128);

        (double Y, double U, double V) RGBtoYUV(Color c) =>
            (0.299 * c.R + 0.587 * c.G + 0.114 * c.B,
             -0.14713 * c.R - 0.28886 * c.G + 0.436 * c.B,
             0.615 * c.R - 0.51499 * c.G - 0.10001 * c.B);

        Color YUVtoRGB(double y, double u, double v) => Color.FromArgb(
            Clamp((int)Math.Round(y + 1.13983 * v)),
            Clamp((int)Math.Round(y - 0.39465 * u - 0.58060 * v)),
            Clamp((int)Math.Round(y + 2.03211 * u)));

        void DrawAxis(Graphics g, int w, int h, string xl, string yl)
        {
            using (var f = new Font("Consolas", 8))
            using (var br = new SolidBrush(Color.FromArgb(200, 255, 255, 255)))
            { g.DrawString(xl, f, br, w - 28, h - 16); g.DrawString(yl, f, br, 4, 4); }
        }

        void DrawSelector(Graphics g, PointF pt)
        {
            using (var p = new Pen(Color.White, 2))
                g.DrawEllipse(p, pt.X - 6, pt.Y - 6, 12, 12);
        }

        void DrawSelector3D(Graphics g, PointF pt)
        {
            using (var p = new Pen(Color.White, 2))
                g.DrawEllipse(p, pt.X - 8, pt.Y - 8, 16, 16);
        }

        int Clamp(int v) => v < 0 ? 0 : v > 255 ? 255 : v;
    }

    // ============================================================
    //  YCbCr - مستوى CbCr (2D) + فضاء 3D
    // ============================================================
    public class YCbCrVisualizer : IColorSpaceVisualizer
    {
        public string SystemName => "YCbCr";
        public bool Supports3D => true;

        public void Draw2D(Graphics g, int w, int h,
            Color sel, out PointF selPt)
        {
            var ycc = RGBtoYCbCr(sel);
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    double Cb = x * 255.0 / w;
                    double Cr = (h - 1 - y) * 255.0 / h;
                    var col = YCbCrtoRGB(ycc.Y, Cb, Cr);
                    using (var br = new SolidBrush(col))
                        g.FillRectangle(br, x, y, 1, 1);
                }

            DrawAxis(g, w, h, "Cb →", "↑ Cr");
            selPt = new PointF((float)(ycc.Cb * w / 255), (float)((255 - ycc.Cr) * h / 255));
            DrawSelector(g, selPt);
        }

        public void Draw3D(Graphics g, int w, int h,
            Color sel, float rotX, float rotY, float zoom, out PointF selPt)
        {
            int cx = w / 2, cy = h / 2;
            int step = 20;
            for (int r = 0; r <= 255; r += step)
                for (int gv = 0; gv <= 255; gv += step)
                    for (int b = 0; b <= 255; b += step)
                    {
                        double Y = 0.299 * r + 0.587 * gv + 0.114 * b;
                        double Cb = 128 - 0.168736 * r - 0.331264 * gv + 0.5 * b;
                        double Cr = 128 + 0.5 * r - 0.418688 * gv - 0.081312 * b;
                        var pt = Projection3D.Project(
                            (float)(Cb / 255 - 0.5),
                            (float)(Y / 255 - 0.5),
                            (float)(Cr / 255 - 0.5),
                            rotX, rotY, zoom, cx, cy);
                        using (var br = new SolidBrush(Color.FromArgb(r, gv, b)))
                            g.FillEllipse(br, pt.X - 2, pt.Y - 2, 4, 4);
                    }

            var ycc = RGBtoYCbCr(sel);
            selPt = Projection3D.Project(
                (float)(ycc.Cb / 255 - 0.5),
                (float)(ycc.Y / 255 - 0.5),
                (float)(ycc.Cr / 255 - 0.5),
                rotX, rotY, zoom, cx, cy);
            DrawSelector3D(g, selPt);
        }

        public Color PickColor2D(PointF pt, int w, int h)
        {
            double Cb = pt.X * 255.0 / w;
            double Cr = (h - pt.Y) * 255.0 / h;
            return YCbCrtoRGB(128, Cb, Cr);
        }

        public Color PickColor3D(PointF pt, int w, int h, float rX, float rY, float z)
            => Color.FromArgb(128, 128, 128);

        (double Y, double Cb, double Cr) RGBtoYCbCr(Color c) =>
            (0.299 * c.R + 0.587 * c.G + 0.114 * c.B,
             128 - 0.168736 * c.R - 0.331264 * c.G + 0.5 * c.B,
             128 + 0.5 * c.R - 0.418688 * c.G - 0.081312 * c.B);

        Color YCbCrtoRGB(double y, double cb, double cr)
        {
            double cbOff = cb - 128, crOff = cr - 128;
            return Color.FromArgb(
                Clamp((int)Math.Round(y + 1.402 * crOff)),
                Clamp((int)Math.Round(y - 0.344136 * cbOff - 0.714136 * crOff)),
                Clamp((int)Math.Round(y + 1.772 * cbOff)));
        }

        void DrawAxis(Graphics g, int w, int h, string xl, string yl)
        {
            using (var f = new Font("Consolas", 8))
            using (var br = new SolidBrush(Color.FromArgb(200, 255, 255, 255)))
            { g.DrawString(xl, f, br, w - 32, h - 16); g.DrawString(yl, f, br, 4, 4); }
        }

        void DrawSelector(Graphics g, PointF pt)
        {
            using (var p = new Pen(Color.White, 2))
                g.DrawEllipse(p, pt.X - 6, pt.Y - 6, 12, 12);
        }

        void DrawSelector3D(Graphics g, PointF pt)
        {
            using (var p = new Pen(Color.White, 2))
                g.DrawEllipse(p, pt.X - 8, pt.Y - 8, 16, 16);
        }

        int Clamp(int v) => v < 0 ? 0 : v > 255 ? 255 : v;
    }

    // ============================================================
    //  LAB - مستوى AB (2D) + فضاء 3D
    // ============================================================
    public class LABVisualizer : IColorSpaceVisualizer
    {
        public string SystemName => "LAB";
        public bool Supports3D => true;

        public void Draw2D(Graphics g, int w, int h,
            Color sel, out PointF selPt)
        {
            var lab = RGBtoLAB(sel);
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    double A = -128 + x * 255.0 / w;
                    double B = 127 - y * 255.0 / h;
                    var col = LABtoRGB(lab.L, A, B);
                    using (var br = new SolidBrush(col))
                        g.FillRectangle(br, x, y, 1, 1);
                }

            DrawAxis(g, w, h, "A →", "↑ B");
            selPt = new PointF(
                (float)((lab.A + 128) / 255 * w),
                (float)((127 - lab.B) / 255 * h));
            DrawSelector(g, selPt);
        }

        public void Draw3D(Graphics g, int w, int h,
            Color sel, float rotX, float rotY, float zoom, out PointF selPt)
        {
            int cx = w / 2, cy = h / 2;
            int step = 20;
            for (int r = 0; r <= 255; r += step)
                for (int gv = 0; gv <= 255; gv += step)
                    for (int b = 0; b <= 255; b += step)
                    {
                        var lab = RGBtoLAB(Color.FromArgb(r, gv, b));
                        var pt = Projection3D.Project(
                            (float)(lab.A / 128 * 0.5),
                            (float)(lab.L / 100 * 0.5 - 0.25),
                            (float)(lab.B / 128 * 0.5),
                            rotX, rotY, zoom, cx, cy);
                        using (var br = new SolidBrush(Color.FromArgb(r, gv, b)))
                            g.FillEllipse(br, pt.X - 2, pt.Y - 2, 4, 4);
                    }

            var labSel = RGBtoLAB(sel);
            selPt = Projection3D.Project(
                (float)(labSel.A / 128 * 0.5),
                (float)(labSel.L / 100 * 0.5 - 0.25),
                (float)(labSel.B / 128 * 0.5),
                rotX, rotY, zoom, cx, cy);
            DrawSelector3D(g, selPt);
        }

        public Color PickColor2D(PointF pt, int w, int h)
        {
            double A = -128 + pt.X * 255.0 / w;
            double B = 127 - pt.Y * 255.0 / h;
            return LABtoRGB(50, A, B);
        }

        public Color PickColor3D(PointF pt, int w, int h, float rX, float rY, float z)
            => Color.FromArgb(128, 128, 128);

        (double L, double A, double B) RGBtoLAB(Color c)
        {
            double r = Pivot(c.R / 255.0), g = Pivot(c.G / 255.0), b = Pivot(c.B / 255.0);
            double x = PivotXYZ((r * 0.4124 + g * 0.3576 + b * 0.1805) / 0.95047);
            double y = PivotXYZ(r * 0.2126 + g * 0.7152 + b * 0.0722);
            double z2 = PivotXYZ((r * 0.0193 + g * 0.1192 + b * 0.9505) / 1.08883);
            return (Math.Max(0, 116 * y - 16), 500 * (x - y), 200 * (y - z2));
        }

        Color LABtoRGB(double L, double A, double B)
        {
            double fy = (L + 16) / 116, fx = A / 500 + fy, fz = fy - B / 200;
            double x = 0.95047 * (fx > 0.206897 ? fx * fx * fx : (fx - 16.0 / 116) / 7.787);
            double y = fy > 0.206897 ? fy * fy * fy : (fy - 16.0 / 116) / 7.787;
            double z = 1.08883 * (fz > 0.206897 ? fz * fz * fz : (fz - 16.0 / 116) / 7.787);
            double r = x * 3.2406 + y * (-1.5372) + z * (-0.4986);
            double g2 = x * (-0.9689) + y * 1.8758 + z * 0.0415;
            double b2 = x * 0.0557 + y * (-0.2040) + z * 1.0570;
            return Color.FromArgb(
                Clamp((int)Math.Round(UnPivot(r) * 255)),
                Clamp((int)Math.Round(UnPivot(g2) * 255)),
                Clamp((int)Math.Round(UnPivot(b2) * 255)));
        }

        double Pivot(double n) => n > 0.04045 ? Math.Pow((n + 0.055) / 1.055, 2.4) : n / 12.92;
        double UnPivot(double n) => n > 0.0031308 ? 1.055 * Math.Pow(n, 1 / 2.4) - 0.055 : 12.92 * n;
        double PivotXYZ(double n) => n > 0.008856 ? Math.Pow(n, 1.0 / 3) : 7.787 * n + 16.0 / 116;

        void DrawAxis(Graphics g, int w, int h, string xl, string yl)
        {
            using (var f = new Font("Consolas", 8))
            using (var br = new SolidBrush(Color.FromArgb(200, 255, 255, 255)))
            { g.DrawString(xl, f, br, w - 28, h - 16); g.DrawString(yl, f, br, 4, 4); }
        }

        void DrawSelector(Graphics g, PointF pt)
        {
            using (var p = new Pen(Color.White, 2))
                g.DrawEllipse(p, pt.X - 6, pt.Y - 6, 12, 12);
        }

        void DrawSelector3D(Graphics g, PointF pt)
        {
            using (var p = new Pen(Color.White, 2))
                g.DrawEllipse(p, pt.X - 8, pt.Y - 8, 16, 16);
        }

        int Clamp(int v) => v < 0 ? 0 : v > 255 ? 255 : v;
    }

    // ============================================================
    //  CMYK - مستوى CM (2D) + فضاء 3D
    // ============================================================
    public class CMYKVisualizer : IColorSpaceVisualizer
    {
        public string SystemName => "CMYK";
        public bool Supports3D => false;   // CMYK 4D → نكتفي بـ 2D

        public void Draw2D(Graphics g, int w, int h,
            Color sel, out PointF selPt)
        {
            var cmyk = RGBtoCMYK(sel);
            // مستوى CM عند Y=sel.Y, K=sel.K
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    double C = x * 1.0 / w;
                    double M = (h - 1 - y) * 1.0 / h;
                    var col = CMYKtoRGB(C, M, cmyk.Y, cmyk.K);
                    using (var br = new SolidBrush(col))
                        g.FillRectangle(br, x, y, 1, 1);
                }

            DrawAxis(g, w, h, "C →", "↑ M");
            selPt = new PointF((float)(cmyk.C * w), (float)((1 - cmyk.M) * h));
            DrawSelector(g, selPt);

            using (var f = new Font("Consolas", 7))
            using (var br = new SolidBrush(Color.FromArgb(200, 255, 255, 255)))
                g.DrawString($"Y={cmyk.Y:0.00}  K={cmyk.K:0.00}  (ثابتان)", f, br, 4, h - 16);
        }

        public void Draw3D(Graphics g, int w, int h,
            Color sel, float rotX, float rotY, float zoom, out PointF selPt)
        {
            // CMYK لا يدعم 3D - نعرض 2D بدلاً
            Draw2D(g, w, h, sel, out selPt);
        }

        public Color PickColor2D(PointF pt, int w, int h)
        {
            double C = pt.X / w;
            double M = (h - pt.Y) / h;
            return CMYKtoRGB(C, M, 0, 0);
        }

        public Color PickColor3D(PointF pt, int w, int h, float rX, float rY, float z)
            => PickColor2D(pt, w, h);

        (double C, double M, double Y, double K) RGBtoCMYK(Color c)
        {
            double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
            double k = 1 - Math.Max(r, Math.Max(g, b));
            if (k == 1) return (0, 0, 0, 1);
            return ((1 - r - k) / (1 - k), (1 - g - k) / (1 - k), (1 - b - k) / (1 - k), k);
        }

        Color CMYKtoRGB(double c, double m, double y, double k) => Color.FromArgb(
            Clamp((int)Math.Round(255 * (1 - c) * (1 - k))),
            Clamp((int)Math.Round(255 * (1 - m) * (1 - k))),
            Clamp((int)Math.Round(255 * (1 - y) * (1 - k))));

        void DrawAxis(Graphics g, int w, int h, string xl, string yl)
        {
            using (var f = new Font("Consolas", 8))
            using (var br = new SolidBrush(Color.FromArgb(200, 255, 255, 255)))
            { g.DrawString(xl, f, br, w - 28, h - 16); g.DrawString(yl, f, br, 4, 4); }
        }

        void DrawSelector(Graphics g, PointF pt)
        {
            using (var p = new Pen(Color.White, 2))
                g.DrawEllipse(p, pt.X - 6, pt.Y - 6, 12, 12);
        }

        int Clamp(int v) => v < 0 ? 0 : v > 255 ? 255 : v;
    }
}