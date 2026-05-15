using System;
using System.Drawing;

namespace ImageLab
{
    public class ColorSystem
    {
        public string[] Names;
        public (double min, double max)[] Ranges;
        public Color[] Colors;
        public Func<double, double, double, double[]> ToSpace;
        public Func<double[], int[]> FromSpace;
    }
}
