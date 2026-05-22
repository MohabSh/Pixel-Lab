using System;
using System.Drawing;

namespace ImageLab
{
    // ============================================================
    //  واجهة الفضاء البصري - كل نظام لوني يُنفّذ هذه الواجهة
    // ============================================================
    public interface IColorSpaceVisualizer
    {
        string SystemName { get; }
        bool Supports3D { get; }

        // رسم الفضاء ثنائي الأبعاد
        void Draw2D(Graphics g, int width, int height,
                    Color selectedColor, out PointF selectedPoint);

        // رسم الفضاء ثلاثي الأبعاد (projection على 2D canvas)
        void Draw3D(Graphics g, int width, int height,
                    Color selectedColor, float rotX, float rotY, float zoom,
                    out PointF selectedPoint);

        // تحويل نقطة النقر إلى لون
        Color PickColor2D(PointF point, int width, int height);
        Color PickColor3D(PointF point, int width, int height,
                          float rotX, float rotY, float zoom);
    }

    // ============================================================
    //  نقطة ثلاثية الأبعاد مع لونها
    // ============================================================
    public struct ColorPoint3D
    {
        public float X, Y, Z;
        public Color Color;
        public ColorPoint3D(float x, float y, float z, Color c)
        { X = x; Y = y; Z = z; Color = c; }
    }

    // ============================================================
    //  أدوات مساعدة مشتركة للإسقاط ثلاثي الأبعاد
    // ============================================================
    public static class Projection3D
    {
        public static PointF Project(float x, float y, float z,
            float rotX, float rotY, float zoom,
            int cx, int cy)
        {
            // تدوير حول محور Y
            float cosY = (float)Math.Cos(rotY);
            float sinY = (float)Math.Sin(rotY);
            float x1 = x * cosY - z * sinY;
            float z1 = x * sinY + z * cosY;

            // تدوير حول محور X
            float cosX = (float)Math.Cos(rotX);
            float sinX = (float)Math.Sin(rotX);
            float y1 = y * cosX - z1 * sinX;
            float z2 = y * sinX + z1 * cosX;

            // إسقاط منظوري بسيط
            float fov = 400 * zoom;
            float pz = z2 + 3f;
            float px = cx + (x1 * fov) / pz;
            float py = cy - (y1 * fov) / pz;

            return new PointF(px, py);
        }
    }
}