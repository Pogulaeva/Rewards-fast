using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Rewards_fast
{
    public class BorderPanel : Panel  // Важно: наследуемся от Panel
    {
        private Color _borderColor = Color.Red;
        private int _borderWidth = 2;

        public BorderPanel()
        {
            // Простая инициализация без сложной логики
            this.SetStyle(ControlStyles.UserPaint |
                         ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.DoubleBuffer |
                         ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();
        }

        public Color BorderColor
        {
            get { return _borderColor; }
            set
            {
                _borderColor = value;
                this.Invalidate();
            }
        }

        public int BorderWidth
        {
            get { return _borderWidth; }
            set
            {
                _borderWidth = Math.Max(1, value);
                this.Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Всегда вызываем базовый метод
            base.OnPaint(e);

            // Если ширина границы 0 - ничего не рисуем
            if (_borderWidth <= 0) return;

            // Если контрол не видим - выходим
            if (!this.Visible) return;

            // Рисуем границу
            try
            {
                using (Pen pen = new Pen(_borderColor, _borderWidth))
                {
                    // Учитываем ширину границы при рисовании
                    int halfBorder = _borderWidth / 2;
                    Rectangle rect = new Rectangle(
                        halfBorder,
                        halfBorder,
                        this.Width - _borderWidth,
                        this.Height - _borderWidth
                    );
                    e.Graphics.DrawRectangle(pen, rect);
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку в отладочных целях
                Debug.WriteLine($"Ошибка рисования BorderPanel: {ex.Message}");
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            this.Invalidate(); // Перерисовываем при изменении размера
        }
    }
}