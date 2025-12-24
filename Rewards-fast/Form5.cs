using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Rewards_fast
{
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
                int dx = e.X - startDragPosition[label].X; // разница по X
                int dy = e.Y - startDragPosition[label].Y; // разница по Y

                // Ограничения по размерам PictureBox
                int leftLimit = 0;
                int rightLimit = template_image.Width - label.Width;
                int topLimit = 0;
                int bottomLimit = template_image.Height - label.Height;

                if (label == label_post || label == label_signature_decryption)
                {
                    // Свободное перемещение по обеим осям
                    int newLeft = Math.Max(leftLimit, Math.Min(label.Left + dx, rightLimit));
                    int newTop = Math.Max(topLimit, Math.Min(label.Top + dy, bottomLimit));
                    label.Location = new Point(newLeft, newTop);
                }
                else
                {
                    // Только вертикальное перемещение
                    int newTop = Math.Max(topLimit, Math.Min(label.Top + dy, bottomLimit));
                    label.Location = new Point(label.Left, newTop);
                }
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

                // Добавляем событие для перетаскивания
                printBox.MouseDown += Print_MouseDown;
                printBox.MouseMove += Print_MouseMove;
                printBox.MouseUp += Print_MouseUp;

                // Добавляем на форму
                splitContainer2.Panel1.Controls.Add(printBox);
                printBox.BringToFront(); // Вывести вперед
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

                // Добавляем событие для перетаскивания
                signatureBox.MouseDown += Signature_MouseDown;
                signatureBox.MouseMove += Signature_MouseMove;
                signatureBox.MouseUp += Signature_MouseUp;

                // Добавляем на форму
                splitContainer2.Panel1.Controls.Add(signatureBox);
                signatureBox.BringToFront(); // Вывести вперед
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
    }
}