using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace ImageLab
{
    // ============================================================
    //  نافذة الفضاء اللوني التفاعلية
    //  تدعم: 2D / 3D، التدوير، التكبير، اختيار الألوان،
    //         المزامنة مع الأنظمة الأخرى
    // ============================================================
    public class ColorSpaceForm : Form
    {
        // ---- حالة ----
        IColorSpaceVisualizer _vis;
        Color _selectedColor;
        bool _is3D = false;
        float _rotX = 0.4f, _rotY = 0.6f, _zoom = 1.0f;
        bool _dragging = false;
        Point _lastMouse;
        Bitmap _canvas;

        // ---- UI ----
        Panel _drawPanel;
        Label _lblValues;
        Label _lblAllSystems;
        ComboBox _cmbSystem;
        Button _btn2D, _btn3D;
        TrackBar _zoomBar;

        // ---- حدث ----
        public event Action<Color> ColorPicked;

        // ============================================================
        public ColorSpaceForm(string systemName, Color initialColor)
        {
            _selectedColor = initialColor;
            _vis = ColorSpaceVisualizerFactory.Create(systemName);

            Text = $"الفضاء اللوني — {systemName}";
            Width = 700;
            Height = 620;
            MinimumSize = new Size(600, 520);
            BackColor = Color.FromArgb(18, 18, 26);
            RightToLeft = RightToLeft.Yes;

            BuildUI();
            UpdateCanvas();
            UpdateValuesLabel();
        }

        // ============================================================
        void BuildUI()
        {
            // ---- شريط الأدوات ----
            Panel toolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Color.FromArgb(28, 28, 40),
                Padding = new Padding(8, 6, 8, 6)
            };

            Label lblSys = new Label
            {
                Text = "النظام:",
                Left = 8,
                Top = 14,
                Width = 50,
                Height = 20,
                ForeColor = Color.FromArgb(180, 200, 255),
                Font = new Font("Arial", 9)
            };

            _cmbSystem = new ComboBox
            {
                Left = 60,
                Top = 10,
                Width = 110,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(40, 40, 60),
                ForeColor = Color.White,
                Font = new Font("Arial", 9)
            };
            _cmbSystem.Items.AddRange(new[] { "RGB", "HSV", "YUV", "YCbCr", "LAB", "CMYK" });
            _cmbSystem.SelectedItem = _vis.SystemName;

            _btn2D = MakeToolBtn("2D", 190, !_is3D);
            _btn3D = MakeToolBtn("3D", 230, _is3D);

            Label lblZoom = new Label
            {
                Text = "تكبير:",
                Left = 285,
                Top = 14,
                Width = 45,
                Height = 20,
                ForeColor = Color.FromArgb(180, 200, 255),
                Font = new Font("Arial", 9)
            };
            _zoomBar = new TrackBar
            {
                Left = 330,
                Top = 8,
                Width = 140,
                Minimum = 5,
                Maximum = 30,
                Value = 10,
                TickFrequency = 5,
                SmallChange = 1
            };

            toolbar.Controls.AddRange(new Control[]
                { lblSys, _cmbSystem, _btn2D, _btn3D, lblZoom, _zoomBar });

            // ---- لوحة الرسم ----
            _drawPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(12, 12, 20),
                Cursor = Cursors.Cross
            };

            // ---- شريط القيم ----
            Panel infoBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 90,
                BackColor = Color.FromArgb(22, 22, 34)
            };

            Panel colorSwatch = new Panel
            {
                Left = 8,
                Top = 8,
                Width = 48,
                Height = 70,
                BackColor = _selectedColor,
                BorderStyle = BorderStyle.FixedSingle
            };

            _lblValues = new Label
            {
                Left = 64,
                Top = 4,
                Width = 280,
                Height = 80,
                Font = new Font("Consolas", 8),
                ForeColor = Color.FromArgb(100, 220, 180),
                Text = ""
            };

            _lblAllSystems = new Label
            {
                Left = 350,
                Top = 4,
                Width = 320,
                Height = 80,
                Font = new Font("Consolas", 7.5f),
                ForeColor = Color.FromArgb(180, 180, 220),
                Text = ""
            };

            infoBar.Controls.AddRange(new Control[]
                { colorSwatch, _lblValues, _lblAllSystems });

            Controls.Add(_drawPanel);
            Controls.Add(infoBar);
            Controls.Add(toolbar);

            // ================================================================
            //  أحداث
            // ================================================================
            _cmbSystem.SelectedIndexChanged += (s, e) =>
            {
                string sys = _cmbSystem.SelectedItem.ToString();
                _vis = ColorSpaceVisualizerFactory.Create(sys);
                Text = $"الفضاء اللوني — {sys}";
                if (!_vis.Supports3D && _is3D) _is3D = false;
                _btn3D.Enabled = _vis.Supports3D;
                UpdateCanvas();
                UpdateValuesLabel();
            };

            _btn2D.Click += (s, e) =>
            {
                _is3D = false;
                _btn2D.BackColor = Color.FromArgb(55, 138, 221);
                _btn3D.BackColor = Color.FromArgb(45, 45, 65);
                UpdateCanvas();
            };

            _btn3D.Click += (s, e) =>
            {
                if (!_vis.Supports3D) return;
                _is3D = true;
                _btn3D.BackColor = Color.FromArgb(55, 138, 221);
                _btn2D.BackColor = Color.FromArgb(45, 45, 65);
                UpdateCanvas();
            };

            _zoomBar.ValueChanged += (s, e) =>
            {
                _zoom = _zoomBar.Value / 10f;
                if (_is3D) UpdateCanvas();
            };

            _drawPanel.Paint += (s, e) =>
            {
                if (_canvas != null)
                    e.Graphics.DrawImage(_canvas, 0, 0,
                        _drawPanel.Width, _drawPanel.Height);
            };

            _drawPanel.Resize += (s, e) =>
            {
                UpdateCanvas();
            };

            _drawPanel.MouseDown += (s, e) =>
            {
                _dragging = true;
                _lastMouse = e.Location;
            };

            _drawPanel.MouseUp += (s, e) =>
            {
                _dragging = false;
                // اختيار اللون عند النقر (بدون سحب)
                if (Math.Abs(e.X - _lastMouse.X) < 4 && Math.Abs(e.Y - _lastMouse.Y) < 4)
                {
                    PickColorAt(e.Location);
                }
            };

            _drawPanel.MouseMove += (s, e) =>
            {
                if (_dragging && _is3D)
                {
                    float dx = (e.X - _lastMouse.X) * 0.01f;
                    float dy = (e.Y - _lastMouse.Y) * 0.01f;
                    _rotY += dx;
                    _rotX += dy;
                    _lastMouse = e.Location;
                    UpdateCanvas();
                }
            };

            _drawPanel.MouseWheel += (s, e) =>
            {
                _zoom = Math.Max(0.3f, Math.Min(4f, _zoom + e.Delta * 0.001f));
                _zoomBar.Value = Math.Max(5, Math.Min(30, (int)(_zoom * 10)));
                UpdateCanvas();
            };

            infoBar.Controls[0].BackColorChanged += (s, e) => { };

            Resize += (s, e) => UpdateCanvas();
        }

        // ============================================================
        //  رسم الكانفاس
        // ============================================================
        void UpdateCanvas()
        {
            if (_drawPanel.Width <= 0 || _drawPanel.Height <= 0) return;

            _canvas?.Dispose();
            _canvas = new Bitmap(_drawPanel.Width, _drawPanel.Height, PixelFormat.Format32bppArgb);

            using (var g = Graphics.FromImage(_canvas))
            {
                g.Clear(Color.FromArgb(12, 12, 20));
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                PointF selPt;
                if (_is3D && _vis.Supports3D)
                    _vis.Draw3D(g, _canvas.Width, _canvas.Height,
                        _selectedColor, _rotX, _rotY, _zoom, out selPt);
                else
                    _vis.Draw2D(g, _canvas.Width, _canvas.Height,
                        _selectedColor, out selPt);

                // رسم معلومات التدوير في وضع 3D
                if (_is3D)
                {
                    using (var f = new Font("Consolas", 7))
                    using (var br = new SolidBrush(Color.FromArgb(120, 255, 255, 255)))
                        g.DrawString("اسحب للتدوير | عجلة الماوس للتكبير", f, br, 4, _canvas.Height - 16);
                }
            }

            _drawPanel.Invalidate();
        }

        // ============================================================
        //  اختيار لون من نقطة في الكانفاس
        // ============================================================
        void PickColorAt(Point pt)
        {
            PointF p = new PointF(
                pt.X * _canvas.Width / (float)_drawPanel.Width,
                pt.Y * _canvas.Height / (float)_drawPanel.Height);

            Color picked;
            if (_is3D && _vis.Supports3D)
                picked = _vis.PickColor3D(p, _canvas.Width, _canvas.Height, _rotX, _rotY, _zoom);
            else
                picked = _vis.PickColor2D(p, _canvas.Width, _canvas.Height);

            _selectedColor = picked;
            UpdateValuesLabel();
            UpdateCanvas();
            ColorPicked?.Invoke(picked);
        }

        // ============================================================
        //  تحديث شريط القيم
        // ============================================================
        void UpdateValuesLabel()
        {
            var c = _selectedColor;

            // تحديث الـ swatch
            if (Controls.Count > 0)
            {
                var infoBar = Controls[1] as Panel;
                if (infoBar?.Controls.Count > 0)
                    infoBar.Controls[0].BackColor = c;
            }

            // قيم النظام الحالي
            string currentVals = GetCurrentSystemValues(c);
            _lblValues.Text = $"【{_vis.SystemName}】\n{currentVals}\nRGB({c.R}, {c.G}, {c.B})";

            // قيم جميع الأنظمة
            var hsv = ColorConverters.RGBtoHSV(c);
            var yuv = ColorConverters.RGBtoYUV(c);
            var ycbcr = ColorConverters.RGBtoYCbCr(c);
            var lab = ColorConverters.RGBtoLAB(c);
            var cmyk = ColorConverters.RGBtoCMYK(c);

            _lblAllSystems.Text =
                $"RGB  → ({c.R}, {c.G}, {c.B})\n" +
                $"HSV  → ({hsv.H:0}°, {hsv.S * 100:0}%, {hsv.V * 100:0}%)\n" +
                $"YUV  → ({yuv.Y:0}, {yuv.U:0}, {yuv.V:0})\n" +
                $"YCbCr→ ({ycbcr.Y:0}, {ycbcr.Cb:0}, {ycbcr.Cr:0})\n" +
                $"LAB  → ({lab.L:0}, {lab.A:0}, {lab.B:0})\n" +
                $"CMYK → ({cmyk.C:0.2f}, {cmyk.M:0.2f}, {cmyk.Y:0.2f}, {cmyk.K:0.2f})";
        }

        string GetCurrentSystemValues(Color c)
        {
            switch (_vis.SystemName)
            {
                case "RGB":
                    return $"R={c.R}  G={c.G}  B={c.B}";
                case "HSV":
                    var hsv = ColorConverters.RGBtoHSV(c);
                    return $"H={hsv.H:0}°  S={hsv.S * 100:0}%  V={hsv.V * 100:0}%";
                case "YUV":
                    var yuv = ColorConverters.RGBtoYUV(c);
                    return $"Y={yuv.Y:0}  U={yuv.U:0}  V={yuv.V:0}";
                case "YCbCr":
                    var ycc = ColorConverters.RGBtoYCbCr(c);
                    return $"Y={ycc.Y:0}  Cb={ycc.Cb:0}  Cr={ycc.Cr:0}";
                case "LAB":
                    var lab = ColorConverters.RGBtoLAB(c);
                    return $"L={lab.L:0}  A={lab.A:0}  B={lab.B:0}";
                case "CMYK":
                    var cmyk = ColorConverters.RGBtoCMYK(c);
                    return $"C={cmyk.C:0.2f}  M={cmyk.M:0.2f}  Y={cmyk.Y:0.2f}  K={cmyk.K:0.2f}";
                default:
                    return "";
            }
        }

        // ============================================================
        //  استدعاء خارجي: تعيين اللون المختار (من MainForm)
        // ============================================================
        public void SetSelectedColor(Color c)
        {
            _selectedColor = c;
            UpdateValuesLabel();
            UpdateCanvas();
        }

        // ============================================================
        //  مساعد
        // ============================================================
        Button MakeToolBtn(string text, int x, bool active)
        {
            return new Button
            {
                Text = text,
                Left = x,
                Top = 8,
                Width = 36,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = active ? Color.FromArgb(55, 138, 221) : Color.FromArgb(45, 45, 65),
                ForeColor = Color.White,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
        }
    }
}