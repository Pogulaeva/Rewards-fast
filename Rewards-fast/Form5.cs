using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Rewards_fast
{
    // Добавьте этот класс в начало файла (перед классом формы)
    public class BorderPanel : Panel
    {
        private Color _borderColor = Color.Red;
        private int _borderWidth = 2;

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
            base.OnPaint(e);

            using (Pen pen = new Pen(_borderColor, _borderWidth))
            {
                // Учитываем ширину пера при рисовании
                int offset = _borderWidth / 2;
                Rectangle rect = new Rectangle(
                    offset,
                    offset,
                    this.Width - _borderWidth,
                    this.Height - _borderWidth
                );
                e.Graphics.DrawRectangle(pen, rect);
            }

            // Рисуем текст "Границы" в углу
            using (Font font = new Font("Arial", 10, FontStyle.Bold))
            using (Brush brush = new SolidBrush(_borderColor))
            {
                e.Graphics.DrawString("Границы", font, brush, 5, 5);
            }

            // Рисуем диагональные линии для визуального эффекта
            using (Pen diagonalPen = new Pen(Color.FromArgb(50, _borderColor), 1))
            {
                e.Graphics.DrawLine(diagonalPen, 0, 0, this.Width, this.Height);
                e.Graphics.DrawLine(diagonalPen, this.Width, 0, 0, this.Height);
            }
        }
    }

    public partial class Template_Constructor : Form
    {
        string FIO;
        string foldername;
        Image image;
        string image2;

        Dictionary<System.Windows.Forms.Label, bool> draggingLabels = new Dictionary<System.Windows.Forms.Label, bool>();
        Dictionary<System.Windows.Forms.Label, Point> startDragPosition = new Dictionary<System.Windows.Forms.Label, Point>();

        private const double smoothingFactor = 0.5; // коэффициент сглаживания (от 0 до 1)

        // Активный лейбл (для которого будем вносить изменения)
        private System.Windows.Forms.Label activeLabel;

        class CustomToolStripProfessionalRenderer : ProfessionalColorTable
        {
            // Переопределяем нужные нам свойства для изменения цветов
            public override Color MenuItemSelectedGradientBegin => Color.GreenYellow;   // Цвет нажатого пункта меню сверху
            public override Color MenuItemSelectedGradientEnd => Color.LightGreen;       // Цвет нажатого пункта меню снизу
            public override Color MenuItemPressedGradientBegin => Color.DarkSeaGreen;   // Цвет фокуса пункт меню сверху
            public override Color MenuItemPressedGradientEnd => Color.MediumSpringGreen;// Цвет фокуса пункт меню снизу
            public override Color MenuItemBorder => Color.Black;                         // Границы пунктов меню
        }

        // Инициализируем список лейблов заранее
        private readonly List<System.Windows.Forms.Label> _labelsList = new List<System.Windows.Forms.Label>();

        private List<PictureBox> _addedPictureBoxes = new List<PictureBox>();

        // Переменные для перетаскивания
        private Control currentDraggingControl;
        private Point startDragPoint;

        class ResizeHandle : Control
        {
            public ResizeHandle()
            {
                Size = new Size(10, 10); // Маленькие квадратики
                BackColor = Color.Red;    // Яркий цвет для заметности
            }
        }

        // Новые переменные для управления границами
        private Rectangle borderBounds = Rectangle.Empty;
        private bool isSettingBounds = false;
        private Point boundsStartPoint;
        private Panel borderVisualizer;
        private bool isBorderVisible = false;


        public Template_Constructor(string param1, string param2, object objParam)
        {
            InitializeComponent();
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true); // Аппаратное ускорение

            // Назначаем рендереру своё оформление
            menuStrip1.Renderer = new ToolStripProfessionalRenderer(new CustomToolStripProfessionalRenderer());

            FIO = param1;
            foldername = param2;
            if (objParam is Image)
            {
                image = (Image)objParam;
                template_image.Image = image;
            }
            else
            {
                if (objParam is string)
                {
                    image2 = (string)objParam;
                    template_image.Image = Image.FromFile(image2);
                }
            }

            // Добавляем лейблы в список единожды
            _labelsList.AddRange(new[]
            {
        label_initial_speech,
        label_FIO,
        label_final_speech,
        label_City_year,
        label_post,
        label_signature_decryption
    });
            foreach (var label in _labelsList)
            {
                if (label.Parent != template_image)
                {
                    // Удаляем из текущего родителя
                    var parent = label.Parent;
                    if (parent != null)
                    {
                        parent.Controls.Remove(label);
                    }
                    // Добавляем на template_image
                    template_image.Controls.Add(label);
                    label.BringToFront();
                }
            }

            // Также убедимся, что template_image имеет правильные настройки
            template_image.Controls.Clear();
            foreach (var label in _labelsList)
            {
                template_image.Controls.Add(label);
                label.BringToFront();
            }
            // Настроим ширину лейблов согласно размеру адаптированного изображения
            ResizeLabelsAccordingToImage();

            // Инициализируем состояние перетаскивания и начальные позиции
            draggingLabels.Add(label_initial_speech, false);
            draggingLabels.Add(label_FIO, false);
            draggingLabels.Add(label_final_speech, false);
            draggingLabels.Add(label_City_year, false);
            draggingLabels.Add(label_post, false);
            draggingLabels.Add(label_signature_decryption, false);

            startDragPosition.Add(label_initial_speech, Point.Empty);
            startDragPosition.Add(label_FIO, Point.Empty);
            startDragPosition.Add(label_final_speech, Point.Empty);
            startDragPosition.Add(label_City_year, Point.Empty);
            startDragPosition.Add(label_post, Point.Empty);
            startDragPosition.Add(label_signature_decryption, Point.Empty);

            // Подключаем обработчики событий
            label_initial_speech.MouseDown += new MouseEventHandler(label_MouseDown);
            label_initial_speech.MouseMove += new MouseEventHandler(label_MouseMove);
            label_initial_speech.MouseUp += new MouseEventHandler(label_MouseUp);

            label_FIO.MouseDown += new MouseEventHandler(label_MouseDown);
            label_FIO.MouseMove += new MouseEventHandler(label_MouseMove);
            label_FIO.MouseUp += new MouseEventHandler(label_MouseUp);

            label_final_speech.MouseDown += new MouseEventHandler(label_MouseDown);
            label_final_speech.MouseMove += new MouseEventHandler(label_MouseMove);
            label_final_speech.MouseUp += new MouseEventHandler(label_MouseUp);

            label_City_year.MouseDown += new MouseEventHandler(label_MouseDown);
            label_City_year.MouseMove += new MouseEventHandler(label_MouseMove);
            label_City_year.MouseUp += new MouseEventHandler(label_MouseUp);

            label_post.MouseDown += new MouseEventHandler(label_MouseDown);
            label_post.MouseMove += new MouseEventHandler(label_MouseMove);
            label_post.MouseUp += new MouseEventHandler(label_MouseUp);

            label_signature_decryption.MouseDown += new MouseEventHandler(label_MouseDown);
            label_signature_decryption.MouseMove += new MouseEventHandler(label_MouseMove);
            label_signature_decryption.MouseUp += new MouseEventHandler(label_MouseUp);

            // Привязываем обработчик клика для каждого лейбла
            foreach (var label in _labelsList)
            {
                label.Click += OnLabelClick;
            }

            // Обработчики изменения текста
            textBox_Changing_font.TextChanged += OnTextBoxChangingFontTextChanged;
            textBox_Size.TextChanged += OnTextBoxSizeTextChanged;

            // Обработчики изменения стилей
            label_Bold.Click += OnLabelBoldClick;
            label_Italics.Click += OnLabelItalicsClick;
            label_Underlined.Click += OnLabelUnderlinedClick;

            // Обработчики клика для выбора шрифта и размера
            textBox_Changing_font.Click += textBox_Changing_font_Click;
            textBox_Size.Click += textBox_Size_Click;

            // Обработчик изменения текста в RichTextBox
            richTextBox_Changing_text.TextChanged += richTextBox_Changing_text_TextChanged;

            // Инициализируем визуализатор границ
            InitializeBorderVisualizer();

            // Добавляем обработчики для template_image
            template_image.MouseDown += template_image_MouseDown;
            template_image.MouseMove += template_image_MouseMove;
            template_image.MouseUp += template_image_MouseUp;

            // Инициализируем переменные границ
            borderBounds = Rectangle.Empty;
            isSettingBounds = false;
            isBorderVisible = false;
        }

        private void ResizeLabelsAccordingToImage()
        {
            if (template_image.Image == null)
            {
                return; // Нет изображения
            }

            // Размеры оригинального изображения
            int imgWidth = template_image.Image.Width;
            int imgHeight = template_image.Image.Height;

            // Размеры контейнера (PictureBox)
            int pbWidth = template_image.ClientRectangle.Width;
            int pbHeight = template_image.ClientRectangle.Height;

            // Вычислим коэффициенты масштабирования
            double scaleByWidth = (double)pbWidth / imgWidth;
            double scaleByHeight = (double)pbHeight / imgHeight;

            // Минимальный масштабирующий коэффициент
            double finalScale = Math.Min(scaleByWidth, scaleByHeight);

            // Пересчитанная ширина изображения
            int scaledImgWidth = (int)(imgWidth * finalScale);
            int scaledImgHeight = (int)(imgHeight * finalScale);

            // Вычисляем положение изображения внутри PictureBox
            int xOffsetImage = (pbWidth - scaledImgWidth) / 2; // Горизонтальное смещение
            int yOffsetImage = (pbHeight - scaledImgHeight) / 2; // Вертикальное смещение

            // Корректируем ширину на 50 пикселей и смещаем на 25 пикселей по оси X
            int adjustedWidthForMainLabels = Math.Max(scaledImgWidth - 80, 0); // Не допускаем отрицательной ширины
            int xOffset = 40; // Смещение по оси X

            // Если есть активные границы, используем их
            if (borderBounds != Rectangle.Empty && isBorderVisible)
            {
                AdjustLabelsToBounds();
            }
            else
            {
                // Применяем ширину и позицию ко всем лейблам, кроме двух особых случаев
                foreach (var label in _labelsList)
                {
                    if (label != null)
                    {
                        label.AutoSize = false;          // Отключаем автоматический подбор размера
                        label.Dock = DockStyle.None;      // Отменяем докирование
                        label.TextAlign = ContentAlignment.MiddleCenter; // Центрируем текст

                        // Для особых лейблов устанавливаем фиксированную ширину
                        if (label == label_post || label == label_signature_decryption)
                        {
                            label.AutoSize = true;           // Фиксированная ширина
                                                             // Ничего не делаем с позицией, сохраняем её как есть
                        }
                        else
                        {
                            // Для основных лейблов применяем уменьшение ширины и смещение по оси X
                            label.Width = adjustedWidthForMainLabels; // Ширина уменьшается на 50 пикселей

                            // Смещаем только по оси X, сохраняя существующую позицию по оси Y
                            label.Location = new Point(xOffsetImage + xOffset, label.Location.Y); // Изменяем только X
                        }
                    }
                }
            }
        }

        // Обработчик изменения размеров формы
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ResizeLabelsAccordingToImage(); // Перерассчитываем размеры лейблов при изменении размеров формы
        }

        // Обработчик клика по лейблу
        private void OnLabelClick(object sender, EventArgs e)
        {
            activeLabel = (System.Windows.Forms.Label)sender;

            // Заполняем RichTextBox текстом из лейбла
            richTextBox_Changing_text.Text = activeLabel.Text;

            // Передача имени шрифта в первый TextBox
            textBox_Changing_font.Text = activeLabel.Font.Name;

            // Передача размера шрифта во второй TextBox
            textBox_Size.Text = activeLabel.Font.Size.ToString();

            // Обновление визуального представления стиля шрифта
            ChangeLabelColor(label_Bold, activeLabel.Font.Bold);
            ChangeLabelColor(label_Italics, activeLabel.Font.Italic);
            ChangeLabelColor(label_Underlined, activeLabel.Font.Underline);
        }

        // Обработчик изменения имени шрифта
        private void OnTextBoxChangingFontTextChanged(object sender, EventArgs e)
        {
            if (activeLabel != null)
            {
                activeLabel.Font = new Font(textBox_Changing_font.Text.Trim(), activeLabel.Font.Size, activeLabel.Font.Style);
            }
        }

        // Обработчик изменения размера шрифта
        private void OnTextBoxSizeTextChanged(object sender, EventArgs e)
        {
            if (float.TryParse(textBox_Size.Text, out float size) && activeLabel != null)
            {
                activeLabel.Font = new Font(activeLabel.Font.Name, size, activeLabel.Font.Style);
            }
        }

        // Обработчик двойного клика по текстовому полю
        private void textBox_Changing_font_Click(object sender, EventArgs e)
        {
            using (var fontDialog = new FontDialog())
            {
                if (fontDialog.ShowDialog() == DialogResult.OK)
                {
                    if (activeLabel != null)
                    {
                        activeLabel.Font = fontDialog.Font;
                        textBox_Changing_font.Text = fontDialog.Font.Name;
                        textBox_Size.Text = fontDialog.Font.Size.ToString();

                        // Обновляем состояние меток стилей
                        ChangeLabelColor(label_Bold, fontDialog.Font.Bold);
                        ChangeLabelColor(label_Italics, fontDialog.Font.Italic);
                        ChangeLabelColor(label_Underlined, fontDialog.Font.Underline);
                    }
                }
            }
        }

        private void textBox_Size_Click(object sender, EventArgs e)
        {
            using (var fontDialog = new FontDialog())
            {
                if (fontDialog.ShowDialog() == DialogResult.OK)
                {
                    if (activeLabel != null)
                    {
                        activeLabel.Font = fontDialog.Font;
                        textBox_Changing_font.Text = fontDialog.Font.Name;
                        textBox_Size.Text = fontDialog.Font.Size.ToString();

                        // Обновляем состояние меток стилей
                        ChangeLabelColor(label_Bold, fontDialog.Font.Bold);
                        ChangeLabelColor(label_Italics, fontDialog.Font.Italic);
                        ChangeLabelColor(label_Underlined, fontDialog.Font.Underline);
                    }
                }
            }
        }

        // Обработчик включения/отключения Bold
        private void OnLabelBoldClick(object sender, EventArgs e)
        {
            if (activeLabel != null)
            {
                activeLabel.Font = new Font(
                    activeLabel.Font.Name,
                    activeLabel.Font.Size,
                    activeLabel.Font.Style ^ FontStyle.Bold); // Переключение флага Bold
                ChangeLabelColor(label_Bold, activeLabel.Font.Bold);
            }
        }

        // Обработчик включения/отключения Italics
        private void OnLabelItalicsClick(object sender, EventArgs e)
        {
            if (activeLabel != null)
            {
                activeLabel.Font = new Font(
                    activeLabel.Font.Name,
                    activeLabel.Font.Size,
                    activeLabel.Font.Style ^ FontStyle.Italic); // Переключение флага Italics
                ChangeLabelColor(label_Italics, activeLabel.Font.Italic);
            }
        }

        // Обработчик включения/отключения Underlined
        private void OnLabelUnderlinedClick(object sender, EventArgs e)
        {
            if (activeLabel != null)
            {
                activeLabel.Font = new Font(
                    activeLabel.Font.Name,
                    activeLabel.Font.Size,
                    activeLabel.Font.Style ^ FontStyle.Underline); // Переключение флага Underline
                ChangeLabelColor(label_Underlined, activeLabel.Font.Underline);
            }
        }

        // Обработчик изменения текста в RichTextBox
        private void richTextBox_Changing_text_TextChanged(object sender, EventArgs e)
        {
            if (activeLabel != null)
            {
                activeLabel.Text = richTextBox_Changing_text.Text.Trim();
            }
        }

        // Универсальный обработчик MouseDown
        private void label_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var label = (System.Windows.Forms.Label)sender;
                draggingLabels[label] = true;
                startDragPosition[label] = new Point(e.X, e.Y); // Начальная точка захвата
            }
        }

        // Универсальный обработчик MouseMove
        private void label_MouseMove(object sender, MouseEventArgs e)
        {
            var label = (System.Windows.Forms.Label)sender;
            if (draggingLabels.TryGetValue(label, out bool dragging) && dragging)
            {
                int dx = e.X - startDragPosition[label].X;
                int dy = e.Y - startDragPosition[label].Y;

                // Вычисляем новую позицию
                int newLeft = label.Left + dx;
                int newTop = label.Top + dy;

                // Ограничения по размерам PictureBox
                int leftLimit = 0;
                int rightLimit = template_image.Width - label.Width;
                int topLimit = 0;
                int bottomLimit = template_image.Height - label.Height;

                if (label == label_post || label == label_signature_decryption)
                {
                    // Свободное перемещение по обеим осям
                    newLeft = Math.Max(leftLimit, Math.Min(newLeft, rightLimit));
                    newTop = Math.Max(topLimit, Math.Min(newTop, bottomLimit));

                    // Применяем ограничение границ
                    Point constrainedPoint = ConstrainToBounds(label, new Point(newLeft, newTop));
                    label.Location = constrainedPoint;
                }
                else
                {
                    // Для основных Label:
                    // Если есть границы, разрешаем перемещение только внутри них
                    if (borderBounds != Rectangle.Empty && isBorderVisible)
                    {
                        // Горизонтальное движение ограничено границами
                        newLeft = Math.Max(borderBounds.Left,
                                  Math.Min(newLeft, borderBounds.Right - label.Width));

                        // Вертикальное движение ограничено границами
                        newTop = Math.Max(borderBounds.Top,
                                 Math.Min(newTop, borderBounds.Bottom - label.Height));

                        label.Location = new Point(newLeft, newTop);
                    }
                    else
                    {
                        // Без границ - только вертикальное перемещение
                        newTop = Math.Max(topLimit, Math.Min(newTop, bottomLimit));
                        label.Location = new Point(label.Left, newTop);
                    }
                }

                // Обновляем начальную позицию для плавного перемещения
                startDragPosition[label] = new Point(e.X, e.Y);
            }
        }

        // Универсальный обработчик MouseUp
        private void label_MouseUp(object sender, EventArgs e)
        {
            var label = (System.Windows.Forms.Label)sender;
            draggingLabels[label] = false;
        }

        private void ChangeLabelColor(System.Windows.Forms.Label label, bool isSelected)
        {
            if (isSelected)
            {
                label.BackColor = Color.MediumSeaGreen; // Меняем цвет на яркий зеленый
            }
            else
            {
                label.BackColor = Color.SeaGreen; // Возвращаемся к обычному цвету
            }
        }

        private void label_FIO_Click(object sender, EventArgs e)
        {
            richTextBox_Changing_text.ReadOnly = true;
            label_case.Visible = true;
            comboBox_case.Visible = true;
        }

        private void label_initial_speech_Click(object sender, EventArgs e)
        {
            richTextBox_Changing_text.ReadOnly = false;
            label_case.Visible = false;
            comboBox_case.Visible = false;
        }

        private void label_final_speech_Click(object sender, EventArgs e)
        {
            richTextBox_Changing_text.ReadOnly = false;
            label_case.Visible = false;
            comboBox_case.Visible = false;
        }

        private void label_post_Click(object sender, EventArgs e)
        {
            richTextBox_Changing_text.ReadOnly = false;
            label_case.Visible = false;
            comboBox_case.Visible = false;
        }

        private void label_signature_decryption_Click(object sender, EventArgs e)
        {
            richTextBox_Changing_text.ReadOnly = false;
            label_case.Visible = false;
            comboBox_case.Visible = false;
        }

        private void label_City_year_Click(object sender, EventArgs e)
        {
            richTextBox_Changing_text.ReadOnly = false;
            label_case.Visible = false;
            comboBox_case.Visible = false;
        }

        private void вставитьПечатьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "Выберите изображение печати";
            dialog.Filter = "Изображения (*.png;*.jpg;*.bmp)|*.png;*.jpg;*.bmp|Все файлы (*.*)|*.*";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string printFilePath = dialog.FileName;
                Image digitalPrint = Image.FromFile(printFilePath);

                // Создаем PictureBox для печати
                PictureBox printBox = new PictureBox();
                printBox.Image = digitalPrint;
                printBox.SizeMode = PictureBoxSizeMode.Zoom;
                printBox.Size = new Size(80, 80);
                printBox.BorderStyle = BorderStyle.FixedSingle;
                printBox.BackColor = Color.Transparent;

                // Размещаем PictureBox на template_image
                printBox.Location = new Point(50, 50); // Начальная позиция

                // Добавляем событие для перетаскивания
                printBox.MouseDown += Print_MouseDown;
                printBox.MouseMove += Print_MouseMove;
                printBox.MouseUp += Print_MouseUp;

                // Добавляем на template_image
                template_image.Controls.Add(printBox);
                _addedPictureBoxes.Add(printBox);
                printBox.BringToFront();
            }
        }

        private void Print_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                startDragPoint = e.Location;
            }
        }

        private void Print_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var box = (PictureBox)sender;
                int deltaX = e.X - startDragPoint.X;
                int deltaY = e.Y - startDragPoint.Y;

                // Приведём сумму к типу double, чтобы явно указать метод Round
                box.Left = (int)Math.Round((double)(box.Left + deltaX), MidpointRounding.AwayFromZero);
                box.Top = (int)Math.Round((double)(box.Top + deltaY), MidpointRounding.AwayFromZero);
            }
        }

        private void Print_MouseUp(object sender, MouseEventArgs e)
        {
            startDragPoint = Point.Empty;
        }

        private void вставитьПодписьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "Выберите файл подписи";
            dialog.Filter = "Изображения (*.png;*.jpg;*.bmp)|*.png;*.jpg;*.bmp|Все файлы (*.*)|*.*";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string signatureFilePath = dialog.FileName;
                Image digitalSignature = Image.FromFile(signatureFilePath);

                // Создаем PictureBox для подписи
                PictureBox signatureBox = new PictureBox();
                signatureBox.Image = digitalSignature;
                signatureBox.SizeMode = PictureBoxSizeMode.Zoom;
                signatureBox.Size = new Size(50, 50);
                signatureBox.BorderStyle = BorderStyle.FixedSingle;
                signatureBox.BackColor = Color.Transparent;

                // Размещаем PictureBox на template_image
                signatureBox.Location = new Point(100, 100); // Начальная позиция

                // Добавляем событие для перетаскивания
                signatureBox.MouseDown += Signature_MouseDown;
                signatureBox.MouseMove += Signature_MouseMove;
                signatureBox.MouseUp += Signature_MouseUp;

                // Добавляем на template_image
                template_image.Controls.Add(signatureBox);
                _addedPictureBoxes.Add(signatureBox);
                signatureBox.BringToFront();
            }
        }
        // Обработчики событий
        private Point startDragPoint2;

        private void Signature_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                startDragPoint2 = e.Location;
            }
        }

        private void Signature_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var box = (PictureBox)sender;
                int deltaX = e.X - startDragPoint2.X;
                int deltaY = e.Y - startDragPoint2.Y;

                box.Left += deltaX;
                box.Top += deltaY;
            }
        }

        private void Signature_MouseUp(object sender, MouseEventArgs e)
        {
            startDragPoint2 = Point.Empty;
        }

        private void сохранитьКакToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Проверяем наличие изображения-шаблона
            if (template_image.Image == null)
            {
                MessageBox.Show("Необходимо сначала выбрать шаблон изображения", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Получаем оригинальное изображение
            Image originalImage = template_image.Image;

            // Создаем новое изображение размером с оригинальный шаблон
            using (Bitmap bitmap = new Bitmap(originalImage.Width, originalImage.Height))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                    // РИСУЕМ БАЗОВЫЙ ШАБЛОН
                    g.DrawImage(originalImage, 0, 0, originalImage.Width, originalImage.Height);

                    // Вычисляем масштаб между PictureBox и оригинальным изображением
                    float scaleX = (float)originalImage.Width / template_image.ClientSize.Width;
                    float scaleY = (float)originalImage.Height / template_image.ClientSize.Height;

                    // Рисуем все лейблы (кроме label_FIO)
                    foreach (System.Windows.Forms.Label label in _labelsList)
                    {
                        if (label == label_FIO) continue;
                        if (!label.Visible || string.IsNullOrEmpty(label.Text)) continue;

                        // Позиция лейбла уже относительно template_image
                        int xOnImage = (int)(label.Left * scaleX);
                        int yOnImage = (int)(label.Top * scaleY);

                        // Масштабируем размер шрифта
                        float fontSize = label.Font.Size * Math.Min(scaleX, scaleY);
                        Font scaledFont = new Font(label.Font.FontFamily, fontSize, label.Font.Style);

                        // Масштабируем размер лейбла
                        int widthOnImage = (int)(label.Width * scaleX);
                        int heightOnImage = (int)(label.Height * scaleY);

                        // Создаем прямоугольник для текста
                        RectangleF textRect = new RectangleF(xOnImage, yOnImage, widthOnImage, heightOnImage);

                        // Создаем формат для выравнивания текста
                        StringFormat format = new StringFormat();

                        // Конвертируем ContentAlignment в StringFormat
                        switch (label.TextAlign)
                        {
                            case ContentAlignment.TopLeft:
                                format.Alignment = StringAlignment.Near;
                                format.LineAlignment = StringAlignment.Near;
                                break;
                            case ContentAlignment.TopCenter:
                                format.Alignment = StringAlignment.Center;
                                format.LineAlignment = StringAlignment.Near;
                                break;
                            case ContentAlignment.TopRight:
                                format.Alignment = StringAlignment.Far;
                                format.LineAlignment = StringAlignment.Near;
                                break;
                            case ContentAlignment.MiddleLeft:
                                format.Alignment = StringAlignment.Near;
                                format.LineAlignment = StringAlignment.Center;
                                break;
                            case ContentAlignment.MiddleCenter:
                                format.Alignment = StringAlignment.Center;
                                format.LineAlignment = StringAlignment.Center;
                                break;
                            case ContentAlignment.MiddleRight:
                                format.Alignment = StringAlignment.Far;
                                format.LineAlignment = StringAlignment.Center;
                                break;
                            case ContentAlignment.BottomLeft:
                                format.Alignment = StringAlignment.Near;
                                format.LineAlignment = StringAlignment.Far;
                                break;
                            case ContentAlignment.BottomCenter:
                                format.Alignment = StringAlignment.Center;
                                format.LineAlignment = StringAlignment.Far;
                                break;
                            case ContentAlignment.BottomRight:
                                format.Alignment = StringAlignment.Far;
                                format.LineAlignment = StringAlignment.Far;
                                break;
                        }

                        // Рисуем текст
                        using (Brush textBrush = new SolidBrush(label.ForeColor))
                        {
                            g.DrawString(label.Text, scaledFont, textBrush, textRect, format);
                        }

                        scaledFont.Dispose();
                    }

                    // Рисуем все добавленные PictureBox'ы (печати и подписи)
                    foreach (PictureBox pictureBox in _addedPictureBoxes)
                    {
                        if (!pictureBox.Visible || pictureBox.Image == null) continue;

                        // Позиция PictureBox уже относительно template_image
                        int xOnImage = (int)(pictureBox.Left * scaleX);
                        int yOnImage = (int)(pictureBox.Top * scaleY);
                        int widthOnImage = (int)(pictureBox.Width * scaleX);
                        int heightOnImage = (int)(pictureBox.Height * scaleY);

                        // Рисуем изображение
                        g.DrawImage(pictureBox.Image, xOnImage, yOnImage, widthOnImage, heightOnImage);
                    }
                }

                // Формируем полное имя файла
                string filename = Path.Combine(foldername, "template_saved_" + DateTime.Now.Ticks + ".jpg");

                // Сохраняем изображение
                bitmap.Save(filename, ImageFormat.Jpeg);

                MessageBox.Show("Шаблон успешно сохранён!\n" + filename, "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void изменениеГраницТекстаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (isSettingBounds)
            {
                // Завершаем установку границ
                isSettingBounds = false;
                изменениеГраницТекстаToolStripMenuItem.Text = "Изменение границ текста";

                // Скрываем визуализатор, но сохраняем его размеры
                borderVisualizer.Visible = false;

                // Сохраняем границы, если они валидны
                if (borderVisualizer.Width > 20 && borderVisualizer.Height > 20)
                {
                    borderBounds = new Rectangle(
                        borderVisualizer.Left,
                        borderVisualizer.Top,
                        borderVisualizer.Width,
                        borderVisualizer.Height
                    );

                    isBorderVisible = true;

                    // Автоматически перемещаем Label внутрь новых границ
                    AdjustLabelsToBounds();

                    // Обновляем текст в подменю "Показать/Скрыть границы"
                    показатьСкрытьГраницыToolStripMenuItem.Text = "Скрыть границы";

                    // Показываем границы
                    показатьСкрытьГраницыToolStripMenuItem_Click(null, EventArgs.Empty);

                    MessageBox.Show($"Границы установлены!\n" +
                                  $"Позиция: X={borderBounds.X}, Y={borderBounds.Y}\n" +
                                  $"Размер: {borderBounds.Width}×{borderBounds.Height}\n" +
                                  $"Label автоматически размещены внутри границ.",
                                  "Границы установлены",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Границы не установлены. Создайте область большего размера.",
                                  "Предупреждение",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Warning);
                }

                // Возвращаем обычный курсор
                template_image.Cursor = Cursors.Default;
            }
            else
            {
                // Начинаем установку границ
                isSettingBounds = true;
                изменениеГраницТекстаToolStripMenuItem.Text = "Завершить установку границ";

                // Устанавливаем курсор для рисования
                template_image.Cursor = Cursors.Cross;

                // Сбрасываем границы
                borderBounds = Rectangle.Empty;

                // Показываем инструкцию
                borderVisualizer.Location = new Point(50, 50);
                borderVisualizer.Size = new Size(0, 0); // Начинаем с нулевого размера
                borderVisualizer.Visible = true;
                borderVisualizer.BringToFront();

                MessageBox.Show("Нажмите и перетащите мышью для создания области границ.\n" +
                               "Красная рамка покажет область, в которой можно перемещать текст.\n" +
                               "После установки все Label будут автоматически размещены внутри границ.",
                               "Установка границ",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Information);
            }
        }

        private void InitializeBorderVisualizer()
        {
            borderVisualizer = new BorderPanel
            {
                BackColor = Color.FromArgb(30, Color.LightBlue), // Полупрозрачный фон
                Padding = new Padding(0)
            };

            // Устанавливаем свойства BorderPanel
            if (borderVisualizer is BorderPanel borderPanel)
            {
                borderPanel.BorderColor = Color.Red;
                borderPanel.BorderWidth = 2;
            }

            template_image.Controls.Add(borderVisualizer);
            borderVisualizer.BringToFront();
            borderVisualizer.Visible = false;
            borderVisualizer.Cursor = Cursors.Cross;
        }

        private bool IsWithinBounds(Control control)
        {
            if (borderBounds == Rectangle.Empty || !isBorderVisible)
                return true;

            Rectangle controlRect = new Rectangle(
                control.Left,
                control.Top,
                control.Width,
                control.Height
            );

            return borderBounds.Contains(controlRect);
        }

        private Point ConstrainToBounds(Control control, Point newLocation)
        {
            if (borderBounds == Rectangle.Empty || !isBorderVisible)
                return newLocation;

            // Ограничиваем позицию
            int newX = Math.Max(borderBounds.Left,
                       Math.Min(newLocation.X, borderBounds.Right - control.Width));
            int newY = Math.Max(borderBounds.Top,
                       Math.Min(newLocation.Y, borderBounds.Bottom - control.Height));

            // Визуальная обратная связь: подсвечиваем границы при приближении
            if (Math.Abs(newX - borderBounds.Left) < 5 ||
                Math.Abs(newX + control.Width - borderBounds.Right) < 5 ||
                Math.Abs(newY - borderBounds.Top) < 5 ||
                Math.Abs(newY + control.Height - borderBounds.Bottom) < 5)
            {
                // Мигаем границей
                if (borderVisualizer.Visible)
                {
                    ((BorderPanel)borderVisualizer).BorderColor = Color.Yellow;
                    borderVisualizer.Invalidate();

                    // Восстанавливаем цвет через 100 мс
                    Task.Delay(100).ContinueWith(_ =>
                    {
                        if (borderVisualizer != null && borderVisualizer.Visible)
                        {
                            ((BorderPanel)borderVisualizer).BorderColor = isSettingBounds ? Color.Green : Color.Red;
                            borderVisualizer.Invoke(new Action(() => borderVisualizer.Invalidate()));
                        }
                    });
                }
            }

            return new Point(newX, newY);
        }

        private void template_image_MouseDown(object sender, MouseEventArgs e)
        {
            if (isSettingBounds && e.Button == MouseButtons.Left)
            {
                boundsStartPoint = e.Location;
                borderVisualizer.Location = e.Location;
                borderVisualizer.Size = new Size(1, 1); // Минимальный размер
                borderVisualizer.Visible = true;
                borderVisualizer.BringToFront();
            }
        }

        private void template_image_MouseMove(object sender, MouseEventArgs e)
        {
            if (isSettingBounds && e.Button == MouseButtons.Left && boundsStartPoint != Point.Empty)
            {
                // Вычисляем прямоугольник от начальной точки до текущей позиции мыши
                int x = Math.Min(boundsStartPoint.X, e.X);
                int y = Math.Min(boundsStartPoint.Y, e.Y);
                int width = Math.Abs(e.X - boundsStartPoint.X);
                int height = Math.Abs(e.Y - boundsStartPoint.Y);

                // Минимальный размер области
                width = Math.Max(20, width);
                height = Math.Max(20, height);

                borderVisualizer.Location = new Point(x, y);
                borderVisualizer.Size = new Size(width, height);

                // Обновляем отображение
                borderVisualizer.Invalidate();
            }
        }

        private void template_image_MouseUp(object sender, MouseEventArgs e)
        {
            if (isSettingBounds && e.Button == MouseButtons.Left)
            {
                // Проверяем, достаточно ли большой прямоугольник
                if (borderVisualizer.Width > 20 && borderVisualizer.Height > 20)
                {
                    // Меняем цвет рамки на зеленый для подтверждения
                    ((BorderPanel)borderVisualizer).BorderColor = Color.Green;
                    borderVisualizer.Invalidate();
                }
                else
                {
                    // Если область слишком маленькая, скрываем ее
                    borderVisualizer.Visible = false;
                }
            }
        }

        private void показатьСкрытьГраницыToolStripMenuItem_Click(object sender, EventArgs e)
        {
            isBorderVisible = !isBorderVisible;

            if (isBorderVisible && borderBounds != Rectangle.Empty)
            {
                // Показываем границы
                borderVisualizer.Location = borderBounds.Location;
                borderVisualizer.Size = borderBounds.Size;

                // Используем BorderPanel
                if (borderVisualizer is BorderPanel borderPanel)
                {
                    borderPanel.BorderColor = Color.Red;
                    borderPanel.BorderWidth = 3;
                    borderPanel.BackColor = Color.FromArgb(30, Color.LightBlue);
                }

                borderVisualizer.Visible = true;
                borderVisualizer.BringToFront();

                // Обновляем текст пункта меню
                показатьСкрытьГраницыToolStripMenuItem.Text = "Скрыть границы";

                // Принудительно перерисовываем
                borderVisualizer.Invalidate();

                // Автоматически корректируем Label внутри границ
                AdjustLabelsToBounds();
            }
            else
            {
                // Скрываем границы
                borderVisualizer.Visible = false;
                показатьСкрытьГраницыToolStripMenuItem.Text = "Показать/Скрыть границы";
            }
        }

        private void сброситьГраницыToolStripMenuItem_Click(object sender, EventArgs e)
        {
            borderBounds = Rectangle.Empty;
            borderVisualizer.Visible = false;
            isBorderVisible = false;
            isSettingBounds = false;

            // Возвращаем первоначальный текст пункта меню
            изменениеГраницТекстаToolStripMenuItem.Text = "Изменение границ текста";
            показатьСкрытьГраницыToolStripMenuItem.Text = "Показать/Скрыть границы";

            // Восстанавливаем исходные размеры Label
            foreach (var label in _labelsList)
            {
                if (label == null) continue;

                if (label != label_post && label != label_signature_decryption)
                {
                    // Возвращаем авторазмер для текста
                    label.AutoSize = true;
                }
            }

            // Обновляем размеры Label
            ResizeLabelsAccordingToImage();

            MessageBox.Show("Все границы сброшены. Текст можно перемещать свободно.",
                           "Границы сброшены",
                           MessageBoxButtons.OK,
                           MessageBoxIcon.Information);
        }

        private void AdjustLabelsToBounds()
        {
            if (borderBounds == Rectangle.Empty || !isBorderVisible)
                return;

            // Для лучшего визуального восприятия - немного отступов внутри границ
            int padding = 5;
            Rectangle innerBounds = new Rectangle(
                borderBounds.Left + padding,
                borderBounds.Top + padding,
                borderBounds.Width - 2 * padding,
                borderBounds.Height - 2 * padding
            );

            // Распределяем Label вертикально внутри границ
            int totalLabels = 0;
            foreach (var label in _labelsList)
            {
                if (label != label_post && label != label_signature_decryption)
                    totalLabels++;
            }

            if (totalLabels > 0)
            {
                int verticalSpacing = innerBounds.Height / (totalLabels + 1);
                int currentY = innerBounds.Top + verticalSpacing;

                foreach (var label in _labelsList)
                {
                    if (label == null) continue;

                    if (label == label_post || label == label_signature_decryption)
                    {
                        // Для этих Label только проверяем позицию
                        if (!IsWithinBounds(label))
                        {
                            Point newLocation = ConstrainToBounds(label, label.Location);
                            label.Location = newLocation;
                        }
                    }
                    else
                    {
                        // Для основных Label:
                        // 1. Устанавливаем ширину равной ширине внутренних границ
                        label.Width = innerBounds.Width;

                        // 2. Выравниваем по центру горизонтально
                        label.Left = innerBounds.Left + (innerBounds.Width - label.Width) / 2;

                        // 3. Устанавливаем вертикальную позицию
                        int labelHeight = label.Height > 0 ? label.Height : 30; // Минимальная высота
                        label.Top = currentY - labelHeight / 2;

                        // 4. Отключаем авторазмер для контроля ширины
                        label.AutoSize = false;

                        // 5. Увеличиваем текущую Y позицию для следующего Label
                        currentY += verticalSpacing;
                    }
                }
            }
        }
    }
}