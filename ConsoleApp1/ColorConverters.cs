using System;
using System.Drawing;

namespace ImageLab
{
    public static class ColorConverters
    {
        public static (double H, double S, double V) RGBtoHSV(Color c)
        {
            double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
            double mx = Math.Max(r, Math.Max(g, b)), mn = Math.Min(r, Math.Min(g, b)), d = mx - mn;
            double h = 0;
            if (d != 0)
            {
                if (mx == r) h = 60 * (((g - b) / d) % 6);
                else if (mx == g) h = 60 * ((b - r) / d + 2);
                else h = 60 * ((r - g) / d + 4);
            }
            if (h < 0) h += 360;
            return (h, mx == 0 ? 0 : d / mx, mx);
        }

        public static (double Y, double U, double V) RGBtoYUV(Color c)
        {
            return (0.299 * c.R + 0.587 * c.G + 0.114 * c.B,
                   -0.14713 * c.R - 0.28886 * c.G + 0.436 * c.B,
                   0.615 * c.R - 0.51499 * c.G - 0.10001 * c.B);
        }

        public static (double Y, double Cb, double Cr) RGBtoYCbCr(Color c)
        {
            return (0.299 * c.R + 0.587 * c.G + 0.114 * c.B,
                   128 - 0.168736 * c.R - 0.331264 * c.G + 0.5 * c.B,
                   128 + 0.5 * c.R - 0.418688 * c.G - 0.081312 * c.B);
        }

        public static (double L, double A, double B) RGBtoLAB(Color c)
        {
            double r = PivotRGB(c.R / 255.0), g = PivotRGB(c.G / 255.0), b = PivotRGB(c.B / 255.0);
            double x = PivotXYZ((r * 0.4124 + g * 0.3576 + b * 0.1805) / 0.95047);
            double y = PivotXYZ(r * 0.2126 + g * 0.7152 + b * 0.0722);
            double z = PivotXYZ((r * 0.0193 + g * 0.1192 + b * 0.9505) / 1.08883);
            return (Math.Max(0, 116 * y - 16), 500 * (x - y), 200 * (y - z));
        }

        public static (double C, double M, double Y, double K) RGBtoCMYK(Color c)
        {
            double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
            double k = 1 - Math.Max(r, Math.Max(g, b));
            if (k == 1) return (0, 0, 0, 1);
            return ((1 - r - k) / (1 - k), (1 - g - k) / (1 - k), (1 - b - k) / (1 - k), k);
        }

        public static double PivotRGB(double n)
            => (n > 0.04045) ? Math.Pow((n + 0.055) / 1.055, 2.4) : n / 12.92;

        public static double UnpivotRGB(double n)
            => (n > 0.0031308) ? 1.055 * Math.Pow(n, 1 / 2.4) - 0.055 : 12.92 * n;

        public static double PivotXYZ(double n)
            => (n > 0.008856) ? Math.Pow(n, 1.0 / 3) : (7.787 * n) + 16.0 / 116;
    }
}
