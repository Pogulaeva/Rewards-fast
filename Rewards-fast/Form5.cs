using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Rewards_fast
{
    public partial class Template_Constructor : Form
    {
        string FIO; // Эта переменная теперь содержит путь к CSV файлу
        string foldername;
        Image image;
        string image2;

        private int current_case = 1; // Текущий падеж для Label_FIO

        Dictionary<System.Windows.Forms.Label, bool> draggingLabels = new Dictionary<System.Windows.Forms.Label, bool>();
        Dictionary<System.Windows.Forms.Label, Point> startDragPosition = new Dictionary<System.Windows.Forms.Label, Point>();

        // Отдельный словарь для PictureBox
        Dictionary<PictureBox, Point> pictureBoxStartDrag = new Dictionary<PictureBox, Point>();

        // Активный лейбл (для которого будем вносить изменения)
        private System.Windows.Forms.Label activeLabel;

        class CustomToolStripProfessionalRenderer : ProfessionalColorTable
        {
            // Переопределяем нужные нам свойства для изменения цветов
            public override Color MenuItemSelectedGradientBegin => Color.GreenYellow;
            public override Color MenuItemSelectedGradientEnd => Color.LightGreen;
            public override Color MenuItemPressedGradientBegin => Color.DarkSeaGreen;
            public override Color MenuItemPressedGradientEnd => Color.MediumSpringGreen;
            public override Color MenuItemBorder => Color.Black;
        }

        // Инициализируем список лейблов заранее
        private readonly List<System.Windows.Forms.Label> _labelsList = new List<System.Windows.Forms.Label>();

        private List<PictureBox> _addedPictureBoxes = new List<PictureBox>();

        // Новые переменные для управления границами
        private Rectangle borderBounds = Rectangle.Empty;
        private bool isSettingBounds = false;
        private Point boundsStartPoint;
        private BorderPanel borderVisualizer;
        private bool isBorderVisible = false;

        // Переменная для отслеживания разрешенных направлений перемещения
        private Dictionary<System.Windows.Forms.Label, bool> verticalOnlyLabels = new Dictionary<System.Windows.Forms.Label, bool>();

        public Template_Constructor(string param1, string param2, object objParam)
        {
            InitializeComponent();
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            // Назначаем рендереру своё оформление
            menuStrip1.Renderer = new ToolStripProfessionalRenderer(new CustomToolStripProfessionalRenderer());

            FIO = param1; // Теперь param1 - это путь к CSV файлу
            foldername = param2;

            if (objParam is Image img)
            {
                image = img;
                template_image.Image = image;
            }
            else if (objParam is string path)
            {
                image2 = path;
                template_image.Image = Image.FromFile(image2);
            }

            // ВАЖНО: Устанавливаем правильный режим масштабирования
            template_image.SizeMode = PictureBoxSizeMode.Zoom;

            // Добавляем лейблы в список
            _labelsList.AddRange(new[]
            {
                label_initial_speech,
                label_FIO,
                label_final_speech,
                label_City_year,
                label_post,
                label_signature_decryption
            });

            // Определяем, какие лейблы можно перемещать только по вертикали
            verticalOnlyLabels[label_initial_speech] = true;
            verticalOnlyLabels[label_FIO] = true;
            verticalOnlyLabels[label_final_speech] = true;
            verticalOnlyLabels[label_City_year] = true;
            verticalOnlyLabels[label_post] = false;         // Можно перемещать по X и Y
            verticalOnlyLabels[label_signature_decryption] = false; // Можно перемещать по X и Y

            // Переносим все лейблы на template_image
            template_image.Controls.Clear();
            foreach (var label in _labelsList)
            {
                template_image.Controls.Add(label);
                label.BringToFront();
            }

            ResizeLabelsAccordingToImage();

            // Инициализируем состояние перетаскивания
            foreach (var label in _labelsList)
            {
                draggingLabels[label] = false;
                startDragPosition[label] = Point.Empty;

                label.MouseDown += label_MouseDown;
                label.MouseMove += label_MouseMove;
                label.MouseUp += label_MouseUp;
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

            // Обработчик изменения текста
            richTextBox_Changing_text.TextChanged += richTextBox_Changing_text_TextChanged;

            // Инициализируем визуализатор границ
            InitializeBorderVisualizer();

            // Добавляем обработчики для template_image
            template_image.MouseDown += template_image_MouseDown;
            template_image.MouseMove += template_image_MouseMove;
            template_image.MouseUp += template_image_MouseUp;

            // Устанавливаем выравнивание для всех лейблов
            foreach (var label in _labelsList)
            {
                if (label == label_post || label == label_signature_decryption)
                {
                    label.TextAlign = ContentAlignment.MiddleLeft;
                }
                else
                {
                    label.TextAlign = ContentAlignment.MiddleCenter;
                }
            }

            button_Sending_message.Image = ResizeImage(Properties.Resources.значок_отправки_сообщения, 17, 17);

            // Загружаем первое ФИО из CSV файла
            LoadFirstFioFromCsv();
        }

        private void LoadFirstFioFromCsv()
        {
            if (!File.Exists(FIO))
                return;

            try
            {
                var fioList = ReadSimpleCsvFile(FIO);
                if (fioList != null && fioList.Count > 0)
                {
                    label_FIO.Text = GetFioInCase(fioList[0], current_case);
                    richTextBox_Changing_text.Text = label_FIO.Text;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке CSV файла: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ResizeLabelsAccordingToImage()
        {
            if (template_image.Image == null) return;

            // Размеры оригинального изображения
            int imgWidth = template_image.Image.Width;
            int imgHeight = template_image.Image.Height;

            // Размеры PictureBox
            int pbWidth = template_image.ClientRectangle.Width;
            int pbHeight = template_image.ClientRectangle.Height;

            // Вычисляем масштаб
            double scaleByWidth = (double)pbWidth / imgWidth;
            double scaleByHeight = (double)pbHeight / imgHeight;
            double finalScale = Math.Min(scaleByWidth, scaleByHeight);

            // Размер масштабированного изображения
            int scaledImgWidth = (int)(imgWidth * finalScale);
            int scaledImgHeight = (int)(imgHeight * finalScale);

            // Смещение изображения в PictureBox
            int xOffsetImage = (pbWidth - scaledImgWidth) / 2;
            int yOffsetImage = (pbHeight - scaledImgHeight) / 2;

            // Ширина для основных лейблов
            int labelWidth = Math.Max(scaledImgWidth - 100, 100);

            if (borderBounds != Rectangle.Empty && isBorderVisible)
            {
                AdjustLabelsToBounds();
                ConstrainAllLabelsToBounds();
                FixBorderBounds();
            }
            else
            {
                // Режим без границ - центрируем всё
                foreach (var label in _labelsList)
                {
                    if (label == label_post || label == label_signature_decryption)
                    {
                        label.AutoSize = true;
                        label.TextAlign = ContentAlignment.MiddleLeft;
                        // Подпись и должность не центрируем
                    }
                    else
                    {
                        label.AutoSize = false;
                        label.Width = labelWidth;
                        label.TextAlign = ContentAlignment.MiddleCenter;
                        // Центрируем по горизонтали
                        label.Left = xOffsetImage + (scaledImgWidth - labelWidth) / 2;
                        AdjustLabelSize(label);
                    }
                }
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            FixBorderBounds();
            ResizeLabelsAccordingToImage();
        }

        private void OnLabelClick(object sender, EventArgs e)
        {
            activeLabel = (System.Windows.Forms.Label)sender;

            if (activeLabel == label_FIO)
            {
                richTextBox_Changing_text.Text = activeLabel.Text;
                richTextBox_Changing_text.ReadOnly = true;
                label_case.Visible = true;
                comboBox_case.Visible = true;
                comboBox_case.SelectedIndex = current_case - 1;
            }
            else
            {
                richTextBox_Changing_text.Text = activeLabel.Text;
                richTextBox_Changing_text.ReadOnly = false;
                label_case.Visible = false;
                comboBox_case.Visible = false;
            }

            textBox_Changing_font.Text = activeLabel.Font.Name;
            textBox_Size.Text = activeLabel.Font.Size.ToString();

            ChangeLabelColor(label_Bold, activeLabel.Font.Bold);
            ChangeLabelColor(label_Italics, activeLabel.Font.Italic);
            ChangeLabelColor(label_Underlined, activeLabel.Font.Underline);
        }

        private void OnTextBoxChangingFontTextChanged(object sender, EventArgs e)
        {
            if (activeLabel != null)
            {
                activeLabel.Font = new Font(textBox_Changing_font.Text.Trim(), activeLabel.Font.Size, activeLabel.Font.Style);
                AdjustLabelSize(activeLabel);
            }
        }

        private void OnTextBoxSizeTextChanged(object sender, EventArgs e)
        {
            if (float.TryParse(textBox_Size.Text, out float size) && activeLabel != null)
            {
                activeLabel.Font = new Font(activeLabel.Font.Name, size, activeLabel.Font.Style);
                AdjustLabelSize(activeLabel);
            }
        }

        private void textBox_Changing_font_Click(object sender, EventArgs e)
        {
            ShowFontDialog();
        }

        private void textBox_Size_Click(object sender, EventArgs e)
        {
            ShowFontDialog();
        }

        private void ShowFontDialog()
        {
            using (var fontDialog = new FontDialog())
            {
                if (fontDialog.ShowDialog() == DialogResult.OK && activeLabel != null)
                {
                    activeLabel.Font = fontDialog.Font;
                    textBox_Changing_font.Text = fontDialog.Font.Name;
                    textBox_Size.Text = fontDialog.Font.Size.ToString();

                    ChangeLabelColor(label_Bold, fontDialog.Font.Bold);
                    ChangeLabelColor(label_Italics, fontDialog.Font.Italic);
                    ChangeLabelColor(label_Underlined, fontDialog.Font.Underline);
                }
            }
        }

        private void OnLabelBoldClick(object sender, EventArgs e)
        {
            ToggleFontStyle(FontStyle.Bold, label_Bold);
        }

        private void OnLabelItalicsClick(object sender, EventArgs e)
        {
            ToggleFontStyle(FontStyle.Italic, label_Italics);
        }

        private void OnLabelUnderlinedClick(object sender, EventArgs e)
        {
            ToggleFontStyle(FontStyle.Underline, label_Underlined);
        }

        private void ToggleFontStyle(FontStyle style, System.Windows.Forms.Label styleLabel)
        {
            if (activeLabel != null)
            {
                FontStyle newStyle = activeLabel.Font.Style ^ style;
                activeLabel.Font = new Font(
                    activeLabel.Font.Name,
                    activeLabel.Font.Size,
                    newStyle);

                bool isSet = (newStyle & style) == style;
                ChangeLabelColor(styleLabel, isSet);
                AdjustLabelSize(activeLabel);
            }
        }

        private void richTextBox_Changing_text_TextChanged(object sender, EventArgs e)
        {
            if (activeLabel != null)
            {
                activeLabel.Text = richTextBox_Changing_text.Text.Trim();
                AdjustLabelSize(activeLabel);
            }
        }

        private void label_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var label = (System.Windows.Forms.Label)sender;
                draggingLabels[label] = true;
                startDragPosition[label] = new Point(e.X, e.Y);
            }
        }

        private void label_MouseMove(object sender, MouseEventArgs e)
        {
            var label = (System.Windows.Forms.Label)sender;

            if (draggingLabels.TryGetValue(label, out bool dragging) && dragging)
            {
                int dx = e.X - startDragPosition[label].X;
                int dy = e.Y - startDragPosition[label].Y;

                int newLeft = label.Left;
                int newTop = label.Top + dy; // Всегда можно перемещать по вертикали

                // Проверяем, можно ли перемещать по горизонтали
                if (!verticalOnlyLabels[label])
                {
                    newLeft = label.Left + dx; // Можно перемещать и по горизонтали
                }

                if (borderBounds != Rectangle.Empty && isBorderVisible)
                {
                    if (!verticalOnlyLabels[label])
                    {
                        newLeft = Math.Max(borderBounds.Left,
                                  Math.Min(newLeft, borderBounds.Right - label.Width));
                    }

                    newTop = Math.Max(borderBounds.Top,
                             Math.Min(newTop, borderBounds.Bottom - label.Height));
                }
                else
                {
                    int maxLeft = template_image.Width - label.Width;
                    int maxTop = template_image.Height - label.Height;

                    if (!verticalOnlyLabels[label])
                    {
                        newLeft = Math.Max(0, Math.Min(newLeft, maxLeft));
                    }

                    newTop = Math.Max(0, Math.Min(newTop, maxTop));
                }

                label.Location = new Point(newLeft, newTop);
                startDragPosition[label] = new Point(e.X, e.Y);
            }
        }

        private void label_MouseUp(object sender, MouseEventArgs e)
        {
            var label = (System.Windows.Forms.Label)sender;
            draggingLabels[label] = false;

            if (borderBounds != Rectangle.Empty && isBorderVisible)
            {
                EnsureLabelWithinBounds(label);
            }
        }

        private void ChangeLabelColor(System.Windows.Forms.Label label, bool isSelected)
        {
            label.BackColor = isSelected ? Color.MediumSeaGreen : Color.SeaGreen;
        }

        private void вставитьПечатьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddImageElement("Выберите изображение печати", 80, 80);
        }

        private void вставитьПодписьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddImageElement("Выберите файл подписи", 50, 50);
        }

        private void AddImageElement(string title, int width, int height)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = title;
                dialog.Filter = "Изображения (*.png;*.jpg;*.bmp)|*.png;*.jpg;*.bmp|Все файлы (*.*)|*.*";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    Image image = Image.FromFile(dialog.FileName);
                    PictureBox pictureBox = new PictureBox
                    {
                        Image = image,
                        SizeMode = PictureBoxSizeMode.Zoom,
                        Size = new Size(width, height),
                        BorderStyle = BorderStyle.FixedSingle,
                        BackColor = Color.Transparent,
                        Location = new Point(50, 50)
                    };

                    pictureBox.MouseDown += PictureBox_MouseDown;
                    pictureBox.MouseMove += PictureBox_MouseMove;
                    pictureBox.MouseUp += PictureBox_MouseUp;

                    template_image.Controls.Add(pictureBox);
                    _addedPictureBoxes.Add(pictureBox);
                    pictureBox.BringToFront();
                }
            }
        }

        private void PictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var pictureBox = (PictureBox)sender;
                pictureBoxStartDrag[pictureBox] = e.Location;
            }
        }

        private void PictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            var pictureBox = (PictureBox)sender;

            if (e.Button == MouseButtons.Left && pictureBoxStartDrag.ContainsKey(pictureBox))
            {
                Point startPoint = pictureBoxStartDrag[pictureBox];
                int deltaX = e.X - startPoint.X;
                int deltaY = e.Y - startPoint.Y;

                pictureBox.Left += deltaX;
                pictureBox.Top += deltaY;

                pictureBoxStartDrag[pictureBox] = e.Location;
            }
        }

        private void PictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            var pictureBox = (PictureBox)sender;
            pictureBoxStartDrag.Remove(pictureBox);
        }

        private void сохранитьКакToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!File.Exists(FIO))
            {
                MessageBox.Show("CSV файл не найден!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                List<string> fioList = ReadSimpleCsvFile(FIO);
                if (fioList == null || fioList.Count == 0)
                {
                    MessageBox.Show("Не удалось прочитать ФИО из файла.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Подтверждение перед генерацией
                string confirmMessage = $"Найдено {fioList.Count} ФИО\n\n";
                confirmMessage += $"Падеж для ФИО: {GetCaseName(current_case)}\n";
                confirmMessage += $"Папка сохранения: {foldername}\n\n";
                confirmMessage += "Начать генерацию грамот?";

                if (MessageBox.Show(confirmMessage, "Подтверждение",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    GenerateCertificatesFromList(fioList, "");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обработке файла:\n{ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetCaseName(int caseType)
        {
            switch (caseType)
            {
                case 1: return "Именительный (Кто?)";
                case 2: return "Родительный (Кого?)";
                case 3: return "Дательный (Кому?)";
                case 4: return "Винительный (Кого?)";
                case 5: return "Творительный (Кем?)";
                case 6: return "Предложный (О ком?)";
                default: return "Именительный";
            }
        }

        private string GetShortName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName) || !fullName.Contains(" "))
                return fullName;

            var parts = fullName.Split(' ');
            if (parts.Length >= 3)
            {
                return $"{parts[0]} {parts[1][0]}.{parts[2][0]}.";
            }

            return fullName.Length > 20 ? fullName.Substring(0, 20) + "..." : fullName;
        }

        private void SaveSingleCertificate(string originalFio, string fioInCase, string signatureText, int index)
        {
            // Сохраняем текущее состояние всех элементов
            var labelSettings = _labelsList.ToDictionary(l => l, SaveLabelSettings);

            try
            {
                // Временно меняем текст в Label_FIO
                label_FIO.Text = fioInCase;

                if (!string.IsNullOrEmpty(signatureText))
                {
                    label_signature_decryption.Text = signatureText;
                }

                // Оригинальное изображение
                Image originalImage = template_image.Image;

                using (Bitmap bitmap = new Bitmap(originalImage.Width, originalImage.Height))
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

                    // Рисуем фон
                    g.DrawImage(originalImage, 0, 0, originalImage.Width, originalImage.Height);

                    // Рисуем все лейблы с их текущими настройками
                    foreach (System.Windows.Forms.Label label in _labelsList)
                    {
                        if (!label.Visible || string.IsNullOrEmpty(label.Text)) continue;

                        // Получаем позицию и размер на оригинальном изображении
                        Point positionOnImage = GetElementPositionOnOriginalImage(label);
                        SizeF elementSize = GetElementSizeOnOriginalImage(label);

                        // Масштабируем шрифт
                        float fontSize = label.Font.Size * GetFontScaleFactor();

                        using (Font scaledFont = new Font(label.Font.FontFamily, fontSize, label.Font.Style))
                        using (Brush textBrush = new SolidBrush(label.ForeColor))
                        {
                            StringFormat format = GetStringFormat(label.TextAlign);
                            RectangleF textRect = new RectangleF(
                                positionOnImage.X,
                                positionOnImage.Y,
                                elementSize.Width,
                                elementSize.Height);

                            g.DrawString(label.Text, scaledFont, textBrush, textRect, format);
                        }
                    }

                    // Рисуем печати и подписи
                    foreach (PictureBox pictureBox in _addedPictureBoxes)
                    {
                        if (!pictureBox.Visible || pictureBox.Image == null) continue;

                        Point positionOnImage = GetElementPositionOnOriginalImage(pictureBox);
                        SizeF elementSize = GetElementSizeOnOriginalImage(pictureBox);

                        g.DrawImage(pictureBox.Image,
                            new Rectangle(
                                (int)positionOnImage.X,
                                (int)positionOnImage.Y,
                                (int)elementSize.Width,
                                (int)elementSize.Height));
                    }

                    // Сохраняем файл
                    string safeFio = GetSafeFileName(originalFio);
                    string filename = Path.Combine(foldername, $"{index:0000}_{safeFio}.jpg");

                    // Сохраняем с максимальным качеством
                    var encoder = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders()
                        .First(codec => codec.FormatID == System.Drawing.Imaging.ImageFormat.Jpeg.Guid);
                    var encoderParams = new System.Drawing.Imaging.EncoderParameters(1);
                    encoderParams.Param[0] = new System.Drawing.Imaging.EncoderParameter(
                        System.Drawing.Imaging.Encoder.Quality, 100L);

                    bitmap.Save(filename, encoder, encoderParams);
                }
            }
            finally
            {
                // Восстанавливаем исходное состояние
                foreach (var kvp in labelSettings)
                {
                    RestoreLabelSettings(kvp.Key, kvp.Value);
                }
            }
        }

        private string GetSafeFileName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "fio";

            // Удаляем недопустимые символы
            var invalidChars = Path.GetInvalidFileNameChars();
            var result = new string(input.Where(c => !invalidChars.Contains(c)).ToArray());

            // Заменяем пробелы на подчеркивания
            result = result.Replace(' ', '_');

            // Ограничиваем длину
            if (result.Length > 50)
            {
                result = result.Substring(0, 50);
            }

            return result;
        }

        private void GenerateCertificatesFromList(List<string> fioList, string suffix = "")
        {
            if (fioList == null || fioList.Count == 0)
                return;

            // Сохраняем оригинальное состояние
            string originalFioText = label_FIO.Text;
            Font originalFont = label_FIO.Font;
            Color originalColor = label_FIO.ForeColor;

            try
            {
                // Форма прогресса
                Form progressForm = new Form
                {
                    Text = $"Генерация грамот{suffix}",
                    Width = 500,
                    Height = 150,
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false
                };

                System.Windows.Forms.ProgressBar progressBar = new System.Windows.Forms.ProgressBar
                {
                    Dock = DockStyle.Top,
                    Minimum = 0,
                    Maximum = fioList.Count,
                    Height = 30,
                    Style = ProgressBarStyle.Continuous
                };

                System.Windows.Forms.Label statusLabel = new System.Windows.Forms.Label
                {
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Arial", 10)
                };

                System.Windows.Forms.Button cancelButton = new System.Windows.Forms.Button
                {
                    Text = "Отмена",
                    Dock = DockStyle.Bottom,
                    Height = 40
                };

                progressForm.Controls.AddRange(new Control[] { progressBar, statusLabel, cancelButton });

                bool cancelled = false;
                cancelButton.Click += (s, e) => cancelled = true;

                int successCount = 0;
                int errorCount = 0;

                // Запускаем генерацию
                Task.Run(() =>
                {
                    for (int i = 0; i < fioList.Count && !cancelled; i++)
                    {
                        string fio = fioList[i];

                        // Обновляем UI
                        progressForm.Invoke(new Action(() =>
                        {
                            progressBar.Value = i + 1;
                            statusLabel.Text = $"{i + 1}/{fioList.Count}: {GetShortName(fio)}";
                        }));

                        try
                        {
                            // Устанавливаем ФИО в нужном падеже
                            this.Invoke(new Action(() =>
                            {
                                label_FIO.Text = GetFioInCase(fio, current_case);
                                AdjustLabelSize(label_FIO);
                                label_FIO.Refresh();
                            }));

                            // Сохраняем изображение
                            SaveSingleCertificate(fio, label_FIO.Text, "", i + 1);
                            successCount++;

                            // Небольшая пауза для стабильности
                            Thread.Sleep(50);
                        }
                        catch (Exception ex)
                        {
                            errorCount++;
                            Debug.WriteLine($"Ошибка для ФИО {fio}: {ex.Message}");
                        }
                    }

                    // Закрываем форму прогресса
                    progressForm.Invoke(new Action(() => progressForm.Close()));

                    // Показываем результат
                    this.Invoke(new Action(() =>
                    {
                        string resultMessage = cancelled ?
                            $"Генерация прервана. Успешно создано: {successCount} из {fioList.Count}" :
                            $"Генерация завершена!\nУспешно: {successCount}\nОшибок: {errorCount}\n\n" +
                            $"Файлы сохранены в:\n{foldername}";

                        MessageBox.Show(resultMessage, "Результат",
                            MessageBoxButtons.OK, cancelled ? MessageBoxIcon.Information : MessageBoxIcon.Information);
                    }));
                });

                progressForm.ShowDialog();
            }
            finally
            {
                // Восстанавливаем оригинальное состояние
                label_FIO.Text = originalFioText;
                label_FIO.Font = originalFont;
                label_FIO.ForeColor = originalColor;
                label_FIO.Refresh();
            }
        }

        private List<string> ReadSimpleCsvFile(string filePath)
        {
            List<string> fioList = new List<string>();

            try
            {
                // Пробуем разные кодировки
                Encoding[] encodingsToTry =
                {
                    Encoding.UTF8,                    // CSV UTF-8 из Excel
                    Encoding.GetEncoding(1251),       // Windows-1251 (русская)
                    Encoding.GetEncoding(866),        // DOS-кодировка
                    Encoding.Default                  // Системная по умолчанию
                };

                foreach (var encoding in encodingsToTry)
                {
                    try
                    {
                        string[] allLines = File.ReadAllLines(filePath, encoding);
                        fioList.Clear();

                        foreach (string line in allLines)
                        {
                            string trimmedLine = line.Trim();
                            if (string.IsNullOrWhiteSpace(trimmedLine))
                                continue;

                            // Убираем лишние кавычки если есть
                            trimmedLine = trimmedLine.Trim('"', '\'', ' ', '\t');

                            // Пробуем разные разделители
                            string[] parts = trimmedLine.Split(new char[] { ',', ';', '\t', '|' },
                                                              StringSplitOptions.RemoveEmptyEntries);

                            if (parts.Length >= 3)
                            {
                                // Формат: три колонки (Фамилия, Имя, Отчество)
                                string lastName = parts[0].Trim();
                                string firstName = parts[1].Trim();
                                string middleName = parts[2].Trim();

                                if (!string.IsNullOrWhiteSpace(lastName))
                                {
                                    string fullFio = $"{lastName} {firstName} {middleName}".Trim();
                                    fioList.Add(fullFio);
                                }
                            }
                            else if (parts.Length == 1 && !string.IsNullOrWhiteSpace(parts[0]))
                            {
                                // Формат: одна колонка с полным ФИО
                                string fullFio = parts[0].Trim();

                                // Проверяем, что это похоже на ФИО (минимум 2 слова)
                                if (fullFio.Contains(" ") && fullFio.Split(' ').Length >= 2)
                                {
                                    fioList.Add(fullFio);
                                }
                            }
                        }

                        if (fioList.Count > 0)
                        {
                            break;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при чтении файла:\n{ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            return fioList;
        }

        // Вспомогательные методы для расчета позиции и размера
        private float GetScaleFactor()
        {
            if (template_image.Image == null) return 1.0f;

            Image currentImage = template_image.Image;
            Size imgSize = currentImage.Size;
            Size pbSize = template_image.ClientSize;

            // Вычисляем масштаб для центрированного изображения
            float scaleX = (float)pbSize.Width / imgSize.Width;
            float scaleY = (float)pbSize.Height / imgSize.Height;
            float scale = Math.Min(scaleX, scaleY);

            // Обратный масштаб для пересчета на оригинальное изображение
            return 1.0f / scale;
        }

        private float GetFontScaleFactor()
        {
            // Коэффициент масштабирования шрифта
            return GetScaleFactor();
        }

        private Point GetElementPositionOnOriginalImage(Control element)
        {
            if (template_image.Image == null) return Point.Empty;

            // Получаем текущее изображение в PictureBox
            Image currentImage = template_image.Image;
            Size imgSize = currentImage.Size;
            Size pbSize = template_image.ClientSize;

            // Вычисляем масштаб для центрированного изображения
            float scaleX = (float)pbSize.Width / imgSize.Width;
            float scaleY = (float)pbSize.Height / imgSize.Height;
            float scale = Math.Min(scaleX, scaleY);

            // Масштабированные размеры изображения
            int scaledWidth = (int)(imgSize.Width * scale);
            int scaledHeight = (int)(imgSize.Height * scale);

            // Смещение для центрирования
            int offsetX = (pbSize.Width - scaledWidth) / 2;
            int offsetY = (pbSize.Height - scaledHeight) / 2;

            // Позиция элемента относительно масштабированного изображения
            int elementX = element.Left - offsetX;
            int elementY = element.Top - offsetY;

            // Если элемент вне области изображения, корректируем
            if (elementX < 0) elementX = 0;
            if (elementY < 0) elementY = 0;
            if (elementX > scaledWidth) elementX = scaledWidth - element.Width;
            if (elementY > scaledHeight) elementY = scaledHeight - element.Height;

            // Масштабируем на оригинальное изображение
            float originalScale = GetScaleFactor();

            int xOnOriginal = (int)(elementX * originalScale);
            int yOnOriginal = (int)(elementY * originalScale);

            return new Point(xOnOriginal, yOnOriginal);
        }

        private SizeF GetElementSizeOnOriginalImage(Control element)
        {
            float scale = GetScaleFactor();

            return new SizeF(
                element.Width * scale,
                element.Height * scale
            );
        }

        private StringFormat GetStringFormat(ContentAlignment alignment)
        {
            StringFormat format = new StringFormat();
            format.FormatFlags = StringFormatFlags.LineLimit;
            format.Trimming = StringTrimming.Word;

            if (alignment == ContentAlignment.TopLeft || alignment == ContentAlignment.MiddleLeft || alignment == ContentAlignment.BottomLeft)
                format.Alignment = StringAlignment.Near;
            else if (alignment == ContentAlignment.TopCenter || alignment == ContentAlignment.MiddleCenter || alignment == ContentAlignment.BottomCenter)
                format.Alignment = StringAlignment.Center;
            else
                format.Alignment = StringAlignment.Far;

            if (alignment == ContentAlignment.TopLeft || alignment == ContentAlignment.TopCenter || alignment == ContentAlignment.TopRight)
                format.LineAlignment = StringAlignment.Near;
            else if (alignment == ContentAlignment.MiddleLeft || alignment == ContentAlignment.MiddleCenter || alignment == ContentAlignment.MiddleRight)
                format.LineAlignment = StringAlignment.Center;
            else
                format.LineAlignment = StringAlignment.Far;

            return format;
        }

        private void изменениеГраницТекстаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (isSettingBounds)
            {
                isSettingBounds = false;
                изменениеГраницТекстаToolStripMenuItem.Text = "Изменение границ текста";
                borderVisualizer.Visible = false;

                if (borderVisualizer.Width > 20 && borderVisualizer.Height > 20)
                {
                    borderBounds = new Rectangle(
                        borderVisualizer.Left,
                        borderVisualizer.Top,
                        borderVisualizer.Width,
                        borderVisualizer.Height
                    );

                    isBorderVisible = true;
                    AdjustLabelsToBounds();
                    ConstrainAllLabelsToBounds();
                    показатьСкрытьГраницыToolStripMenuItem.Text = "Скрыть границы";
                    показатьСкрытьГраницыToolStripMenuItem_Click(null, EventArgs.Empty);

                    MessageBox.Show($"Границы установлены!\nПозиция: X={borderBounds.X}, Y={borderBounds.Y}\n" +
                                  $"Размер: {borderBounds.Width}×{borderBounds.Height}\n" +
                                  $"Label автоматически размещены внутри границ.",
                                  "Границы установлены", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Границы не установлены. Создайте область большего размера.",
                                  "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                template_image.Cursor = Cursors.Default;
            }
            else
            {
                isSettingBounds = true;
                изменениеГраницТекстаToolStripMenuItem.Text = "Завершить установку границ";
                template_image.Cursor = Cursors.Cross;
                borderBounds = Rectangle.Empty;

                borderVisualizer.Location = new Point(50, 50);
                borderVisualizer.Size = new Size(0, 0);
                borderVisualizer.Visible = true;
                borderVisualizer.BringToFront();

                MessageBox.Show("Нажмите и перетащите мышью для создания области границ.\n" +
                               "Красная рамка покажет область, в которой можно перемещать текст.\n" +
                               "После установки все Label будут автоматически размещены внутри границ.",
                               "Установка границ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void InitializeBorderVisualizer()
        {
            borderVisualizer = new BorderPanel
            {
                BackColor = Color.FromArgb(30, Color.LightBlue),
                Padding = new Padding(0),
                BorderColor = Color.Red,
                BorderWidth = 2
            };

            template_image.Controls.Add(borderVisualizer);
            borderVisualizer.BringToFront();
            borderVisualizer.Visible = false;
            borderVisualizer.Cursor = Cursors.Cross;
        }

        private void template_image_MouseDown(object sender, MouseEventArgs e)
        {
            if (isSettingBounds && e.Button == MouseButtons.Left)
            {
                boundsStartPoint = e.Location;
                borderVisualizer.Location = e.Location;
                borderVisualizer.Size = new Size(1, 1);
                borderVisualizer.Visible = true;
                borderVisualizer.BringToFront();
            }
        }

        private void template_image_MouseMove(object sender, MouseEventArgs e)
        {
            if (isSettingBounds && e.Button == MouseButtons.Left && boundsStartPoint != Point.Empty)
            {
                int x = Math.Min(boundsStartPoint.X, e.X);
                int y = Math.Min(boundsStartPoint.Y, e.Y);
                int width = Math.Max(20, Math.Abs(e.X - boundsStartPoint.X));
                int height = Math.Max(20, Math.Abs(e.Y - boundsStartPoint.Y));

                borderVisualizer.Location = new Point(x, y);
                borderVisualizer.Size = new Size(width, height);
                borderVisualizer.Invalidate();
            }
        }

        private void template_image_MouseUp(object sender, MouseEventArgs e)
        {
            if (isSettingBounds && e.Button == MouseButtons.Left)
            {
                if (borderVisualizer.Width > 20 && borderVisualizer.Height > 20)
                {
                    if (borderVisualizer is BorderPanel borderPanel)
                    {
                        borderPanel.BorderColor = Color.Green;
                    }
                    borderVisualizer.Invalidate();
                }
                else
                {
                    borderVisualizer.Visible = false;
                }
            }
        }

        private void показатьСкрытьГраницыToolStripMenuItem_Click(object sender, EventArgs e)
        {
            isBorderVisible = !isBorderVisible;

            if (isBorderVisible && borderBounds != Rectangle.Empty)
            {
                borderVisualizer.Location = borderBounds.Location;
                borderVisualizer.Size = borderBounds.Size;

                if (borderVisualizer is BorderPanel borderPanel)
                {
                    borderPanel.BorderColor = Color.Red;
                    borderPanel.BorderWidth = 3;
                    borderPanel.BackColor = Color.FromArgb(30, Color.LightBlue);
                }

                borderVisualizer.Visible = true;
                borderVisualizer.BringToFront();
                показатьСкрытьГраницыToolStripMenuItem.Text = "Скрыть границы";
                borderVisualizer.Invalidate();
                AdjustLabelsToBounds();
            }
            else
            {
                borderVisualizer.Visible = false;
                показатьСкрытьГраницыToolStripMenuItem.Text = "Показать/Скрыть границы";
            }
        }

        private void сброситьГраницыToolStripMenuItem_Click(object sender, EventArgs e)
        {
            borderBounds = Rectangle.Empty;
            borderVisualizer.Visible = false;
            isSettingBounds = false;
            изменениеГраницТекстаToolStripMenuItem.Text = "Изменение границ текста";
            показатьСкрытьГраницыToolStripMenuItem.Text = "Показать/Скрыть границы";

            foreach (var label in _labelsList)
            {
                if (label != null && label != label_post && label != label_signature_decryption)
                {
                    label.AutoSize = true;
                }
            }

            ResizeLabelsAccordingToImage();

            MessageBox.Show("Все границы сброшены. Текст можно перемещать свободно.",
                           "Границы сброшены", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void AdjustLabelsToBounds()
        {
            if (borderBounds == Rectangle.Empty || !isBorderVisible) return;

            Rectangle validBounds = new Rectangle(
                Math.Max(0, borderBounds.Left),
                Math.Max(0, borderBounds.Top),
                Math.Min(borderBounds.Width, template_image.Width - borderBounds.Left),
                Math.Min(borderBounds.Height, template_image.Height - borderBounds.Top)
            );

            int padding = 10;
            Rectangle innerBounds = new Rectangle(
                validBounds.Left + padding,
                validBounds.Top + padding,
                Math.Max(0, validBounds.Width - 2 * padding),
                Math.Max(0, validBounds.Height - 2 * padding)
            );

            if (innerBounds.Width <= 0 || innerBounds.Height <= 0) return;

            var mainLabels = _labelsList.Where(l => l != null && l != label_post && l != label_signature_decryption).ToList();

            if (mainLabels.Count > 0)
            {
                int verticalSpacing = innerBounds.Height / (mainLabels.Count + 1);

                for (int i = 0; i < mainLabels.Count; i++)
                {
                    var label = mainLabels[i];
                    int maxLabelWidth = Math.Min(innerBounds.Width, validBounds.Width - 20);
                    label.Width = Math.Min(maxLabelWidth, label.Width);
                    label.TextAlign = ContentAlignment.MiddleCenter;

                    // Центрируем по горизонтали внутри границ
                    int centerX = validBounds.Left + validBounds.Width / 2;
                    label.Left = centerX - label.Width / 2;

                    // Ограничиваем горизонтально
                    if (label.Left < validBounds.Left) label.Left = validBounds.Left;
                    if (label.Right > validBounds.Right) label.Left = validBounds.Right - label.Width;

                    AdjustLabelSize(label);

                    // Позиция по вертикали
                    int labelY = validBounds.Top + padding + verticalSpacing * (i + 1);
                    int labelTop = Math.Max(validBounds.Top,
                                   Math.Min(labelY - label.Height / 2,
                                   validBounds.Bottom - label.Height));

                    label.Top = labelTop;

                    if (label.Bottom > validBounds.Bottom)
                    {
                        label.Top = validBounds.Bottom - label.Height;
                    }
                }
            }

            // Для подписи и должности оставляем текущее положение, но проверяем границы
            if (label_post != null)
            {
                label_post.TextAlign = ContentAlignment.MiddleLeft;
                EnsureLabelWithinBounds(label_post);
            }

            if (label_signature_decryption != null)
            {
                label_signature_decryption.TextAlign = ContentAlignment.MiddleLeft;
                EnsureLabelWithinBounds(label_signature_decryption);
            }
        }

        private void AdjustLabelSize(System.Windows.Forms.Label label)
        {
            if (label == null) return;

            ContentAlignment originalAlignment = label.TextAlign;

            int maxWidth;
            if (borderBounds != Rectangle.Empty && isBorderVisible)
            {
                maxWidth = borderBounds.Width - 20;
            }
            else if (label == label_post || label == label_signature_decryption)
            {
                label.AutoSize = true;
                label.TextAlign = ContentAlignment.MiddleLeft;
                return;
            }
            else
            {
                maxWidth = template_image.Width - 100;
            }

            using (Graphics g = label.CreateGraphics())
            {
                SizeF textSize;
                if (originalAlignment == ContentAlignment.MiddleCenter ||
                    originalAlignment == ContentAlignment.TopCenter ||
                    originalAlignment == ContentAlignment.BottomCenter)
                {
                    textSize = g.MeasureString(label.Text, label.Font);
                }
                else
                {
                    textSize = g.MeasureString(label.Text, label.Font, maxWidth);
                }

                int padding = 10;
                int newWidth = Math.Min((int)Math.Ceiling(textSize.Width) + padding * 2, maxWidth);
                int newHeight = (int)Math.Ceiling(textSize.Height) + padding * 2;

                newWidth = Math.Max(newWidth, 100);
                newHeight = Math.Max(newHeight, 30);

                label.Size = new Size(newWidth, newHeight);
                label.TextAlign = originalAlignment;

                // Центрируем только если это центральное выравнивание и нет границ
                if (borderBounds == Rectangle.Empty || !isBorderVisible)
                {
                    if (originalAlignment == ContentAlignment.MiddleCenter ||
                        originalAlignment == ContentAlignment.TopCenter ||
                        originalAlignment == ContentAlignment.BottomCenter)
                    {
                        CenterLabelInImage(label);
                    }
                }
            }
        }

        private void EnsureLabelWithinBounds(System.Windows.Forms.Label label)
        {
            if (label.Parent != template_image || borderBounds == Rectangle.Empty || !isBorderVisible)
                return;

            bool needsAdjustment = false;
            int newLeft = label.Left;
            int newTop = label.Top;

            if (label.Right > borderBounds.Right)
            {
                newLeft = borderBounds.Right - label.Width;
                needsAdjustment = true;
            }

            if (label.Left < borderBounds.Left)
            {
                newLeft = borderBounds.Left;
                needsAdjustment = true;
            }

            if (label.Bottom > borderBounds.Bottom)
            {
                newTop = borderBounds.Bottom - label.Height;
                needsAdjustment = true;
            }

            if (label.Top < borderBounds.Top)
            {
                newTop = borderBounds.Top;
                needsAdjustment = true;
            }

            if (needsAdjustment)
            {
                label.Location = new Point(newLeft, newTop);
                label.Invalidate();
            }
        }

        private void CenterLabelInImage(System.Windows.Forms.Label label)
        {
            if (label == label_post || label == label_signature_decryption) return;

            int centerX = template_image.Width / 2;
            label.Left = centerX - label.Width / 2;
        }

        private void ConstrainAllLabelsToBounds()
        {
            if (borderBounds == Rectangle.Empty || !isBorderVisible) return;

            Rectangle validBounds = new Rectangle(
                Math.Max(0, borderBounds.Left),
                Math.Max(0, borderBounds.Top),
                Math.Min(borderBounds.Width, template_image.Width - borderBounds.Left),
                Math.Min(borderBounds.Height, template_image.Height - borderBounds.Top)
            );

            foreach (var label in _labelsList)
            {
                if (label == null) continue;
                bool wasAdjusted = false;

                // Для всех лейблов проверяем вертикальные границы
                if (label.Top < validBounds.Top)
                {
                    label.Top = validBounds.Top;
                    wasAdjusted = true;
                }
                else if (label.Bottom > validBounds.Bottom)
                {
                    label.Top = validBounds.Bottom - label.Height;
                    wasAdjusted = true;
                }

                // Для центральных лейблов (кроме post и signature) центрируем по горизонтали
                if (label != label_post && label != label_signature_decryption)
                {
                    int centerX = validBounds.Left + validBounds.Width / 2;
                    label.Left = centerX - label.Width / 2;

                    // Ограничиваем горизонтально
                    if (label.Left < validBounds.Left)
                    {
                        label.Left = validBounds.Left;
                        wasAdjusted = true;
                    }
                    else if (label.Right > validBounds.Right)
                    {
                        label.Left = validBounds.Right - label.Width;
                        wasAdjusted = true;
                    }
                }
                else
                {
                    // Для post и signature проверяем горизонтальные границы
                    if (label.Left < validBounds.Left)
                    {
                        label.Left = validBounds.Left;
                        wasAdjusted = true;
                    }
                    else if (label.Right > validBounds.Right)
                    {
                        label.Left = validBounds.Right - label.Width;
                        wasAdjusted = true;
                    }
                }

                if (wasAdjusted) label.Invalidate();
            }
        }

        private void FixBorderBounds()
        {
            if (borderBounds != Rectangle.Empty)
            {
                borderBounds = new Rectangle(
                    Math.Max(0, borderBounds.Left),
                    Math.Max(0, borderBounds.Top),
                    Math.Min(borderBounds.Width, template_image.Width - borderBounds.Left),
                    Math.Min(borderBounds.Height, template_image.Height - borderBounds.Top)
                );

                if (borderVisualizer != null && isBorderVisible)
                {
                    borderVisualizer.Location = borderBounds.Location;
                    borderVisualizer.Size = borderBounds.Size;
                    borderVisualizer.Invalidate();
                }
            }
        }

        private void оПриложенииИРазработчикеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Данное приложение представляет собой систему генерации наградных материалов.\n" +
                          "Приложение создано в рамках студенческой дипломной работы 2025-2026гг.\n" +
                          "Разработчик: Погуляева Полина Михайловна.\n" +
                          "По всем вопросам писать сюда: ulina.pog@yandex.ru",
                          "О приложении", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void инструкцияПоРаботеСПриложениемToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Данный раздел находится в статусе разработки",
                          "Уведомление", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void comboBox_case_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox_case.SelectedIndex >= 0)
            {
                current_case = comboBox_case.SelectedIndex + 1;

                if (activeLabel == label_FIO)
                {
                    string fioInCase = GetFioInCase(label_FIO.Text, current_case);
                    label_FIO.Text = fioInCase;
                    richTextBox_Changing_text.Text = fioInCase;
                }
            }
        }

        private void textBox_Request_input_field_TextChanged(object sender, EventArgs e)
        {
            MessageBox.Show("Функция консультирования с ИИ находится в статусе разработки",
                          "Уведомление", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void button_Sending_message_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Функция консультирования с ИИ находится в статусе разработки",
                          "Уведомление", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private Image ResizeImage(Image originalImage, int width, int height)
        {
            Bitmap resizedImage = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(resizedImage))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(originalImage, 0, 0, width, height);
            }

            return resizedImage;
        }

        private class LabelSettings
        {
            public string Text { get; set; }
            public Font Font { get; set; }
            public ContentAlignment TextAlign { get; set; }
            public Point Location { get; set; }
            public Size Size { get; set; }
            public Color ForeColor { get; set; }
        }

        private LabelSettings SaveLabelSettings(System.Windows.Forms.Label label)
        {
            return new LabelSettings
            {
                Text = label.Text,
                Font = new Font(label.Font.FontFamily, label.Font.Size, label.Font.Style),
                TextAlign = label.TextAlign,
                Location = label.Location,
                Size = label.Size,
                ForeColor = label.ForeColor
            };
        }

        private void RestoreLabelSettings(System.Windows.Forms.Label label, LabelSettings settings)
        {
            if (settings == null) return;

            label.Text = settings.Text;
            label.Font = settings.Font;
            label.TextAlign = settings.TextAlign;
            label.Location = settings.Location;
            label.Size = settings.Size;
            label.ForeColor = settings.ForeColor;
        }

        // Обновленная функция для преобразования ФИО в падеж
        private string GetFioInCase(string fio, int caseType)
        {
            if (string.IsNullOrWhiteSpace(fio))
                return fio;

            // Разделяем ФИО на части
            var parts = fio.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
                return fio; // Неполное ФИО - возвращаем как есть

            string lastName = parts[0];
            string firstName = parts[1];
            string middleName = parts[2];

            // Для дательного падежа (caseType = 3) используем ваши методы
            if (caseType == 3)
            {
                Gender gender = DetermineGender(middleName);
                string convertedLastName = ConvertLastNameToCase(lastName, gender);
                string convertedFirstName = ConvertFirstNameToCase(firstName, gender);
                string convertedPatronymic = ConvertPatronymicToCase(middleName, gender);

                return $"{convertedLastName} {convertedFirstName} {convertedPatronymic}";
            }

            // Для других падежей оставляем простую логику
            switch (caseType)
            {
                case 1: // Именительный
                    return $"{lastName} {firstName} {middleName}";
                case 2: // Родительный
                    return $"{lastName}а {firstName}а {middleName}а";
                case 4: // Винительный
                    return $"{lastName}а {firstName}а {middleName}а";
                case 5: // Творительный
                    return $"{lastName}ым {firstName}ом {middleName}ом";
                case 6: // Предложный
                    return $"{lastName}е {firstName}е {middleName}е";
                default:
                    return fio;
            }
        }

        // Методы из вашей консольной программы
        private Gender DetermineGender(string patronymic)
        {
            // Определение гендера по отчеству
            if (patronymic.EndsWith("ич") || patronymic.EndsWith("лы") || patronymic.EndsWith("зы"))
                return Gender.Male;
            else if (patronymic.EndsWith("на") || patronymic.EndsWith("зы") || patronymic.EndsWith("лы"))
                return Gender.Female;
            else
                return Gender.Unknown;
        }

        private string ConvertLastNameToCase(string lastName, Gender gender)
        {
            // Анализатор для фамилии в дательном падеже
            if (gender == Gender.Male)
            {
                // Правила склонения мужских фамилий в дательном падеже
                if (lastName.EndsWith("ых") || lastName.EndsWith("их") || lastName.EndsWith("е") ||
                    lastName.EndsWith("и") || lastName.EndsWith("о") || lastName.EndsWith("у") ||
                    lastName.EndsWith("ы") || lastName.EndsWith("э") || lastName.EndsWith("ю"))
                    return lastName;
                else if (lastName.EndsWith("ов") || lastName.EndsWith("ев") || lastName.EndsWith("ин") ||
                         lastName.EndsWith("ын") || lastName.EndsWith("н") || lastName.EndsWith("в") ||
                         lastName.EndsWith("б") || lastName.EndsWith("г") || lastName.EndsWith("д") ||
                         lastName.EndsWith("ж") || lastName.EndsWith("з") || lastName.EndsWith("к") ||
                         lastName.EndsWith("л") || lastName.EndsWith("м") || lastName.EndsWith("п") ||
                         lastName.EndsWith("р") || lastName.EndsWith("с") || lastName.EndsWith("т") ||
                         lastName.EndsWith("ф") || lastName.EndsWith("х") || lastName.EndsWith("ц") ||
                         lastName.EndsWith("ч") || lastName.EndsWith("ш") || lastName.EndsWith("щ"))
                    return lastName + "у";
                else if (lastName.EndsWith("ский") || lastName.EndsWith("цкий"))
                    return lastName.Substring(0, lastName.Length - 2) + "ому";
                else if (lastName.EndsWith("ий"))
                    return lastName.Substring(0, lastName.Length - 2) + "ему";
                else if (lastName.EndsWith("ой"))
                    return lastName.Substring(0, lastName.Length - 1) + "му";
                else if (lastName.EndsWith("й") || lastName.EndsWith("ь"))
                    return lastName.Substring(0, lastName.Length - 1) + "ю";
                else if (lastName.EndsWith("ия") || lastName.EndsWith("ея") || lastName.EndsWith("ая") ||
                         lastName.EndsWith("оя") || lastName.EndsWith("уя") || lastName.EndsWith("эя") ||
                         lastName.EndsWith("юя") || lastName.EndsWith("яя"))
                    return lastName.Substring(0, lastName.Length - 1) + "е";
                else if (lastName.EndsWith("иа") || lastName.EndsWith("еа") || lastName.EndsWith("аа") ||
                         lastName.EndsWith("оа") || lastName.EndsWith("уа") || lastName.EndsWith("эа") ||
                         lastName.EndsWith("юа") || lastName.EndsWith("яа"))
                    return lastName;
                else
                    return lastName + "е";
            }
            else if (gender == Gender.Female)
            {
                // Правила склонения женских фамилий в дательном падеже
                if (lastName.EndsWith("ина"))
                    return lastName.Substring(0, lastName.Length - 1) + "е";
                else if (lastName.EndsWith("ых") || lastName.EndsWith("их") || lastName.EndsWith("е") ||
                         lastName.EndsWith("и") || lastName.EndsWith("о") || lastName.EndsWith("у") ||
                         lastName.EndsWith("ы") || lastName.EndsWith("э") || lastName.EndsWith("ю"))
                    return lastName;
                else if (lastName.EndsWith("й") || lastName.EndsWith("ь"))
                    return lastName;
                else if (lastName.EndsWith("н") || lastName.EndsWith("в") || lastName.EndsWith("б") ||
                         lastName.EndsWith("г") || lastName.EndsWith("д") || lastName.EndsWith("ж") ||
                         lastName.EndsWith("з") || lastName.EndsWith("к") || lastName.EndsWith("л") ||
                         lastName.EndsWith("м") || lastName.EndsWith("п") || lastName.EndsWith("р") ||
                         lastName.EndsWith("с") || lastName.EndsWith("т") || lastName.EndsWith("ф") ||
                         lastName.EndsWith("х") || lastName.EndsWith("ц") || lastName.EndsWith("ч") ||
                         lastName.EndsWith("ш") || lastName.EndsWith("щ"))
                    return lastName;
                else if (lastName == "Топчая")
                    return "Топчей";
                else if (lastName.EndsWith("ия") || lastName.EndsWith("ея") || lastName.EndsWith("ая") ||
                         lastName.EndsWith("оя") || lastName.EndsWith("уя") || lastName.EndsWith("эя") ||
                         lastName.EndsWith("юя") || lastName.EndsWith("яя"))
                    return lastName.Substring(0, lastName.Length - 1) + "е";
                else if (lastName.EndsWith("иа") || lastName.EndsWith("еа") || lastName.EndsWith("аа") ||
                         lastName.EndsWith("оа") || lastName.EndsWith("уа") || lastName.EndsWith("эа") ||
                         lastName.EndsWith("юа") || lastName.EndsWith("яа"))
                    return lastName;
                else
                    return lastName.Substring(0, lastName.Length - 1) + "ой";
            }
            else
                return lastName;
        }

        private string ConvertFirstNameToCase(string firstName, Gender gender)
        {
            // Анализатор для имени в дательном падеже
            if (gender == Gender.Male)
            {
                if (firstName.EndsWith("н"))
                    return firstName + "у";
                else if (firstName.EndsWith("а") || firstName.EndsWith("я"))
                    return firstName.Substring(0, firstName.Length - 1) + "е";
                else if (firstName.EndsWith("й") || firstName.EndsWith("ь"))
                    return firstName.Substring(0, firstName.Length - 1) + "ю";
                else
                    return firstName + "у";
            }
            else if (gender == Gender.Female)
            {
                if (firstName.EndsWith("ия"))
                    return firstName.Substring(0, firstName.Length - 1) + "и";
                else if (firstName.EndsWith("а") || firstName.EndsWith("я"))
                    return firstName.Substring(0, firstName.Length - 1) + "е";
                else
                    return firstName + "е";
            }
            else
                return firstName;
        }

        private string ConvertPatronymicToCase(string patronymic, Gender gender)
        {
            // Анализатор для отчества в дательном падеже
            if (gender == Gender.Male)
            {
                if (patronymic.EndsWith("ич"))
                    return patronymic.Substring(0, patronymic.Length - 2) + "ичу";
                else
                    return patronymic + "у";
            }
            else if (gender == Gender.Female)
            {
                if (patronymic.EndsWith("на"))
                    return patronymic.Substring(0, patronymic.Length - 2) + "не";
                else
                    return patronymic + "е";
            }
            else
                return patronymic;
        }

        // Enum для гендера (добавьте в класс)
        public enum Gender
        {
            Male,
            Female,
            Unknown
        }
    }
}