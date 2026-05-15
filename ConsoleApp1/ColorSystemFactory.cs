using System;
using System.Drawing;

namespace ImageLab
{
    public static class ColorSystemFactory
    {
        public static ColorSystem GetSystem(string name)
        {
            switch (name)
            {
                case "RGB":
                    return new ColorSystem
                    {
                        Names = new[] { "R - أحمر", "G - أخضر", "B - أزرق" },
                        Ranges = new[] { (0.0, 255.0), (0.0, 255.0), (0.0, 255.0) },
                        Colors = new[] {
                            Color.FromArgb(226, 75, 74),
                            Color.FromArgb(99, 153, 34),
                            Color.FromArgb(55, 138, 221)
                        },
                        ToSpace = (r, g, b) => new double[] { r, g, b },
                        FromSpace = v => new[] { (int)v[0], (int)v[1], (int)v[2] }
                    };
                case "HSV":
                    return new ColorSystem
                    {
                        Names = new[] { "H - تدرج اللون", "S - التشبع", "V - الإضاءة" },
                        Ranges = new[] { (0.0, 360.0), (0.0, 1.0), (0.0, 1.0) },
                        Colors = new[] {
                            Color.FromArgb(186, 117, 23),
                            Color.FromArgb(15, 110, 86),
                            Color.FromArgb(24, 95, 165)
                        },
                        ToSpace = (r, g, b) =>
                        {
                            double rf = r / 255.0, gf = g / 255.0, bf = b / 255.0;
                            double mx = Math.Max(rf, Math.Max(gf, bf));
                            double mn = Math.Min(rf, Math.Min(gf, bf));
                            double d = mx - mn, h = 0;
                            if (d != 0)
                            {
                                if (mx == rf) h = 60 * (((gf - bf) / d) % 6);
                                else if (mx == gf) h = 60 * ((bf - rf) / d + 2);
                                else h = 60 * ((rf - gf) / d + 4);
                            }
                            if (h < 0) h += 360;
                            return new[] { h, mx == 0 ? 0 : d / mx, mx };
                        },
                        FromSpace = v =>
                        {
                            double h = v[0], s = v[1], vv = v[2];
                            double c2 = vv * s, x = c2 * (1 - Math.Abs((h / 60) % 2 - 1)), m = vv - c2;
                            double r2 = 0, g2 = 0, b2 = 0;
                            if (h < 60) { r2 = c2; g2 = x; }
                            else if (h < 120) { r2 = x; g2 = c2; }
                            else if (h < 180) { g2 = c2; b2 = x; }
                            else if (h < 240) { g2 = x; b2 = c2; }
                            else if (h < 300) { r2 = x; b2 = c2; }
                            else { r2 = c2; b2 = x; }
                            return new[] {
                                (int)Math.Round((r2 + m) * 255),
                                (int)Math.Round((g2 + m) * 255),
                                (int)Math.Round((b2 + m) * 255)
                            };
                        }
                    };
                case "YUV":
                    return new ColorSystem
                    {
                        Names = new[] { "Y - الإضاءة", "U - مركبة U", "V - مركبة V" },
                        Ranges = new[] { (0.0, 255.0), (-111.0, 111.0), (-156.0, 156.0) },
                        Colors = new[] {
                            Color.FromArgb(83, 58, 183),
                            Color.FromArgb(216, 90, 48),
                            Color.FromArgb(29, 158, 117)
                        },
                        ToSpace = (r, g, b) => new[]{
                            0.299*r+0.587*g+0.114*b,
                            -0.14713*r-0.28886*g+0.436*b,
                            0.615*r-0.51499*g-0.10001*b},
                        FromSpace = v =>
                        {
                            double y = v[0], u = v[1], vv = v[2];
                            return new[]{
                                (int)Math.Round(Math.Min(255,Math.Max(0,y+1.13983*vv))),
                                (int)Math.Round(Math.Min(255,Math.Max(0,y-0.39465*u-0.58060*vv))),
                                (int)Math.Round(Math.Min(255,Math.Max(0,y+2.03211*u)))};
                        }
                    };
                case "YCbCr":
                    return new ColorSystem
                    {
                        Names = new[] { "Y - الإضاءة", "Cb - فرق الأزرق", "Cr - فرق الأحمر" },
                        Ranges = new[] { (0.0, 255.0), (0.0, 255.0), (0.0, 255.0) },
                        Colors = new[] {
                            Color.FromArgb(83, 58, 183),
                            Color.FromArgb(55, 138, 221),
                            Color.FromArgb(212, 83, 126)
                        },
                        ToSpace = (r, g, b) => new[]{
                            0.299*r+0.587*g+0.114*b,
                            128-0.168736*r-0.331264*g+0.5*b,
                            128+0.5*r-0.418688*g-0.081312*b},
                        FromSpace = v =>
                        {
                            double y = v[0], cb = v[1] - 128, cr = v[2] - 128;
                            return new[]{
                                (int)Math.Round(Math.Min(255,Math.Max(0,y+1.402*cr))),
                                (int)Math.Round(Math.Min(255,Math.Max(0,y-0.344136*cb-0.714136*cr))),
                                (int)Math.Round(Math.Min(255,Math.Max(0,y+1.772*cb)))};
                        }
                    };
                case "LAB":
                    return new ColorSystem
                    {
                        Names = new[] { "L - الإضاءة", "A - أخضر↔أحمر", "B - أزرق↔أصفر" },
                        Ranges = new[] { (0.0, 100.0), (-128.0, 127.0), (-128.0, 127.0) },
                        Colors = new[] {
                            Color.FromArgb(83, 58, 183),
                            Color.FromArgb(212, 83, 126),
                            Color.FromArgb(186, 117, 23)
                        },
                        ToSpace = (r, g, b) =>
                        {
                            double rf = ColorConverters.PivotRGB(r / 255.0);
                            double gf = ColorConverters.PivotRGB(g / 255.0);
                            double bf = ColorConverters.PivotRGB(b / 255.0);
                            double x = ColorConverters.PivotXYZ((rf * 0.4124 + gf * 0.3576 + bf * 0.1805) / 0.95047);
                            double y = ColorConverters.PivotXYZ(rf * 0.2126 + gf * 0.7152 + bf * 0.0722);
                            double z = ColorConverters.PivotXYZ((rf * 0.0193 + gf * 0.1192 + bf * 0.9505) / 1.08883);
                            return new[] { Math.Max(0, 116 * y - 16), 500 * (x - y), 200 * (y - z) };
                        },
                        FromSpace = v =>
                        {
                            double L = v[0], A = v[1], B = v[2];
                            double fy = (L + 16) / 116, fx = A / 500 + fy, fz = fy - B / 200;
                            double x = 0.95047 * (fx > 0.206897 ? fx * fx * fx : (fx - 16.0 / 116) / 7.787);
                            double y = fy > 0.206897 ? fy * fy * fy : (fy - 16.0 / 116) / 7.787;
                            double z = 1.08883 * (fz > 0.206897 ? fz * fz * fz : (fz - 16.0 / 116) / 7.787);
                            double r2 = x * 3.2406 + y * (-1.5372) + z * (-0.4986);
                            double g2 = x * (-0.9689) + y * 1.8758 + z * 0.0415;
                            double b2 = x * 0.0557 + y * (-0.2040) + z * 1.0570;
                            return new[]{
                                (int)Math.Round(Math.Min(255,Math.Max(0,ColorConverters.UnpivotRGB(r2)*255))),
                                (int)Math.Round(Math.Min(255,Math.Max(0,ColorConverters.UnpivotRGB(g2)*255))),
                                (int)Math.Round(Math.Min(255,Math.Max(0,ColorConverters.UnpivotRGB(b2)*255)))};
                        }
                    };
                case "CMYK":
                default:
                    return new ColorSystem
                    {
                        Names = new[] { "C - سماوي", "M - أرجواني", "Y - أصفر", "K - أسود" },
                        Ranges = new[] { (0.0, 1.0), (0.0, 1.0), (0.0, 1.0), (0.0, 1.0) },
                        Colors = new[] {
                            Color.FromArgb(29,158,117),
                            Color.FromArgb(212,83,126),
                            Color.FromArgb(186,117,23),
                            Color.FromArgb(44,44,42)
                        },
                        ToSpace = (r, g, b) =>
                        {
                            double rf = r / 255.0, gf = g / 255.0, bf = b / 255.0;
                            double k = 1 - Math.Max(rf, Math.Max(gf, bf));
                            if (k == 1) return new[] { 0.0, 0, 0, 1 };
                            return new[] {
                                (1 - rf - k) / (1 - k),
                                (1 - gf - k) / (1 - k),
                                (1 - bf - k) / (1 - k),
                                k };
                        },
                        FromSpace = v =>
                        {
                            double c = v[0], m = v[1], y = v[2], k = v[3];
                            return new[]{
                                (int)Math.Round(255*(1-c)*(1-k)),
                                (int)Math.Round(255*(1-m)*(1-k)),
                                (int)Math.Round(255*(1-y)*(1-k))};
                        }
                    };
            }
        }
    }
}
