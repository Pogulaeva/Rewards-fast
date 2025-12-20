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
        Dictionary<System.Windows.Forms.Label, int> startYPositions = new Dictionary<System.Windows.Forms.Label, int>();

        private const double smoothingFactor = 0.5; // коэффициент сглаживания (от 0 до 1)

        // Переменная для хранения текущего выбранного Label
        private System.Windows.Forms.Label selectedLabel;

        // Переменные для сохранения текущего состояния
        private string originalText;
        private string originalFontName;
        private float originalFontSize;
        private bool originalIsBold;
        private bool originalIsItalic;
        private bool originalIsUnderlined;

        // Глобальные переменные
        string currentFontName = "";
        float currentFontSize = 0f;
        bool isBold = false;
        bool isItalic = false;
        bool isUnderlined = false;

        class CustomToolStripProfessionalRenderer : ProfessionalColorTable
        {
            // Переопределяем нужные нам свойства для изменения цветов
            public override Color MenuItemSelectedGradientBegin => Color.GreenYellow;   // Цвет нажатого пункта меню сверху
            public override Color MenuItemSelectedGradientEnd => Color.LightGreen;       // Цвет нажатого пункта меню снизу
            public override Color MenuItemPressedGradientBegin => Color.DarkSeaGreen;   // Цвет фокуса пункт меню сверху
            public override Color MenuItemPressedGradientEnd => Color.MediumSpringGreen;// Цвет фокуса пункт меню снизу
            public override Color MenuItemBorder => Color.Black;                         // Границы пунктов меню
        }

        public Template_Constructor(string param1, string param2, object objParam)
        {
            InitializeComponent();
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

            // Инициализируем состояние перетаскивания и начальные позиции
            draggingLabels.Add(label_initial_speech, false);
            draggingLabels.Add(label_FIO, false);
            draggingLabels.Add(label_final_speech, false);
            draggingLabels.Add(label_City_year, false);

            startYPositions.Add(label_initial_speech, 0);
            startYPositions.Add(label_FIO, 0);
            startYPositions.Add(label_final_speech, 0);
            startYPositions.Add(label_City_year, 0);

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
        }

        // Универсальный обработчик MouseDown
        private void label_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var label = (System.Windows.Forms.Label)sender;
                draggingLabels[label] = true;
                startYPositions[label] = label.Top - e.Y;
            }
        }

        // Универсальный обработчик MouseMove
        private void label_MouseMove(object sender, MouseEventArgs e)
        {
            var label = (System.Windows.Forms.Label)sender;
            if (draggingLabels.TryGetValue(label, out bool dragging) && dragging)
            {
                int targetY = Math.Max(0, Math.Min(startYPositions[label] + e.Y, template_image.Height - label.Height));
                int smoothedY = (int)((targetY * smoothingFactor) + (label.Top * (1 - smoothingFactor))); // сглаживаем позицию
                label.Location = new Point(label.Left, smoothedY);
            }
        }

        // Универсальный обработчик MouseUp
        private void label_MouseUp(object sender, MouseEventArgs e)
        {
            var label = (System.Windows.Forms.Label)sender;
            draggingLabels[label] = false;
        }

        private void textBox_Changing_font_Clik(object sender, EventArgs e)
        {
            using (var fontDialog = new FontDialog())
            {
                if (fontDialog.ShowDialog() == DialogResult.OK)
                {
                    // Вместо применения нового шрифта, показываем только его название
                    textBox_Changing_font.Text = fontDialog.Font.Name;
                    // Получаем размер шрифта и выводим его во второе текстовое поле
                    textBox_Size.Text = fontDialog.Font.Size.ToString();

                    ChangeLabelColor(label_Bold, fontDialog.Font.Bold);
                    ChangeLabelColor(label_Italics, fontDialog.Font.Italic);
                    ChangeLabelColor(label_Underlined, fontDialog.Font.Underline);
                }
            }
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

        private void textBox_Size_Click(object sender, EventArgs e)
        {
            using (var fontDialog = new FontDialog())
            {
                if (fontDialog.ShowDialog() == DialogResult.OK)
                {
                    // Вместо применения нового шрифта, показываем только его название
                    textBox_Changing_font.Text = fontDialog.Font.Name;
                    // Получаем размер шрифта и выводим его во второе текстовое поле
                    textBox_Size.Text = fontDialog.Font.Size.ToString();
                }
            }
        }
        private void label_Bold_Click(object sender, EventArgs e)
        {
            isBold = !isBold;
            label_Bold.BackColor = isBold ? Color.MediumSeaGreen : Color.SeaGreen;
            ApplyCurrentFontSettingsToSelectedLabel(); // Немедленно обновляем шрифт
        }

        private void label_Italics_Click(object sender, EventArgs e)
        {
            isItalic = !isItalic;
            label_Italics.BackColor = isItalic ? Color.MediumSeaGreen : Color.SeaGreen;
            ApplyCurrentFontSettingsToSelectedLabel(); // Немедленно обновляем шрифт
        }

        private void label_Underlined_Click(object sender, EventArgs e)
        {
            isUnderlined = !isUnderlined;
            label_Underlined.BackColor = isUnderlined ? Color.MediumSeaGreen : Color.SeaGreen;
            ApplyCurrentFontSettingsToSelectedLabel(); // Немедленно обновляем шрифт
        }

        // Метод для обновления фона индикаторов стиля шрифта
        private void UpdateLabelIndicators(Font font)
        {
            // Проверка и смена цвета для жирного
            label_Bold.BackColor = font.Bold ? Color.MediumSpringGreen : Color.SeaGreen;

            // Проверка и смена цвета для курсива
            label_Italics.BackColor = font.Italic ? Color.MediumSpringGreen : Color.SeaGreen;

            // Проверка и смена цвета для подчеркивания
            label_Underlined.BackColor = font.Underline ? Color.MediumSpringGreen : Color.SeaGreen;
        }

        private void label_FIO_Click(object sender, EventArgs e)
        {
            // Делаем текстовое поле доступным только для чтения
            richTextBox_Changing_text.ReadOnly = true;
            label_case.Visible = true;
            comboBox_case.Visible = true;

            System.Windows.Forms.Label clickedLabel = (System.Windows.Forms.Label)sender;

            // Сначала очистим предыдущий текст
            richTextBox_Changing_text.Clear();

            // Теперь запишем новый текст из лейбла
            richTextBox_Changing_text.AppendText(clickedLabel.Text + Environment.NewLine);

            // Запись названия и размера шрифта в соответствующие текстовые поля
            textBox_Changing_font.Text = clickedLabel.Font.Name;
            textBox_Size.Text = clickedLabel.Font.Size.ToString();

            // Обновляем цвета фоновых лейблов, отражающих стиль шрифта
            UpdateLabelIndicators(clickedLabel.Font);
        }

        private void label_initial_speech_Click(object sender, EventArgs e)
        {
            // Делаем текстовое поле доступным только для чтения
            richTextBox_Changing_text.ReadOnly = false;
            label_case.Visible = false;
            comboBox_case.Visible = false;

            // Получаем ссылку на выбранный Label
            selectedLabel = (System.Windows.Forms.Label)sender;

            // Сохраняем текущее состояние выбранного Label
            originalText = selectedLabel.Text;
            originalFontName = selectedLabel.Font.Name;
            originalFontSize = selectedLabel.Font.Size;
            originalIsBold = selectedLabel.Font.Bold;
            originalIsItalic = selectedLabel.Font.Italic;
            originalIsUnderlined = selectedLabel.Font.Underline;

            // Загружаем текущее состояние в интерфейс
            richTextBox_Changing_text.Text = originalText;
            textBox_Changing_font.Text = originalFontName;
            textBox_Size.Text = originalFontSize.ToString();
            label_Bold.BackColor = originalIsBold ? Color.MediumSeaGreen : Color.SeaGreen;
            label_Italics.BackColor = originalIsItalic ? Color.MediumSeaGreen : Color.SeaGreen;
            label_Underlined.BackColor = originalIsUnderlined ? Color.MediumSeaGreen : Color.SeaGreen;
        }

        private void label_final_speech_Click(object sender, EventArgs e)
        {
            // Делаем текстовое поле доступным только для чтения
            richTextBox_Changing_text.ReadOnly = false;
            label_case.Visible = false;
            comboBox_case.Visible = false;

            System.Windows.Forms.Label clickedLabel = (System.Windows.Forms.Label)sender;

            // Сначала очистим предыдущий текст
            richTextBox_Changing_text.Clear();

            // Записываем текст лейбла в RichTextBox
            richTextBox_Changing_text.AppendText(clickedLabel.Text + Environment.NewLine);

            // Запись названия и размера шрифта в соответствующие текстовые поля
            textBox_Changing_font.Text = clickedLabel.Font.Name;
            textBox_Size.Text = clickedLabel.Font.Size.ToString();

            // Обновляем цвета фоновых лейблов, отражающих стиль шрифта
            UpdateLabelIndicators(clickedLabel.Font);
        }

        private void label_post_Click(object sender, EventArgs e)
        {
            // Делаем текстовое поле доступным только для чтения
            richTextBox_Changing_text.ReadOnly = false;
            label_case.Visible = false;
            comboBox_case.Visible = false;

            System.Windows.Forms.Label clickedLabel = (System.Windows.Forms.Label)sender;

            // Сначала очистим предыдущий текст
            richTextBox_Changing_text.Clear();

            // Записываем текст лейбла в RichTextBox
            richTextBox_Changing_text.AppendText(clickedLabel.Text + Environment.NewLine);

            // Запись названия и размера шрифта в соответствующие текстовые поля
            textBox_Changing_font.Text = clickedLabel.Font.Name;
            textBox_Size.Text = clickedLabel.Font.Size.ToString();

            // Обновляем цвета фоновых лейблов, отражающих стиль шрифта
            UpdateLabelIndicators(clickedLabel.Font);
        }

        private void label_signature_decryption_Click(object sender, EventArgs e)
        {
            // Делаем текстовое поле доступным только для чтения
            richTextBox_Changing_text.ReadOnly = false;
            label_case.Visible = false;
            comboBox_case.Visible = false;

            System.Windows.Forms.Label clickedLabel = (System.Windows.Forms.Label)sender;

            // Сначала очистим предыдущий текст
            richTextBox_Changing_text.Clear();

            // Записываем текст лейбла в RichTextBox
            richTextBox_Changing_text.AppendText(clickedLabel.Text + Environment.NewLine);

            // Запись названия и размера шрифта в соответствующие текстовые поля
            textBox_Changing_font.Text = clickedLabel.Font.Name;
            textBox_Size.Text = clickedLabel.Font.Size.ToString();

            // Обновляем цвета фоновых лейблов, отражающих стиль шрифта
            UpdateLabelIndicators(clickedLabel.Font);
        }

        private void label_City_year_Click(object sender, EventArgs e)
        {
            // Делаем текстовое поле доступным только для чтения
            richTextBox_Changing_text.ReadOnly = false;
            label_case.Visible = false;
            comboBox_case.Visible = false;

            System.Windows.Forms.Label clickedLabel = (System.Windows.Forms.Label)sender;

            // Сначала очистим предыдущий текст
            richTextBox_Changing_text.Clear();

            // Записываем текст лейбла в RichTextBox
            richTextBox_Changing_text.AppendText(clickedLabel.Text + Environment.NewLine);

            // Запись названия и размера шрифта в соответствующие текстовые поля
            textBox_Changing_font.Text = clickedLabel.Font.Name;
            textBox_Size.Text = clickedLabel.Font.Size.ToString();

            // Обновляем цвета фоновых лейблов, отражающих стиль шрифта
            UpdateLabelIndicators(clickedLabel.Font);
        }

        // Обработчик изменения шрифта в TextBox
        private void textBox_Changing_font_TextChanged(object sender, EventArgs e)
        {
            // Просто сохраним новое имя шрифта
            currentFontName = textBox_Changing_font.Text.Trim();
        }


        // Обработчик изменения размера шрифта
        private void textBox_Size_TextChanged(object sender, EventArgs e)
        {
            // Чистим строку от пробельных символов
            string cleanedSize = textBox_Size.Text.Trim();

            // Парсим размер шрифта
            if (float.TryParse(cleanedSize, out float parsedSize))
            {
                currentFontSize = parsedSize;
            }
            else
            {
                // Если размер оказался некорректным, выдаём предупреждение
                MessageBox.Show("Недопустимый размер шрифта.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Метод для финального применения изменений к выбранному Label
        private void ApplyChangesToSelectedLabel()
        {
            try
            {
                // Проверка наличия подходящего семейства шрифтов
                if (!System.Drawing.FontFamily.Families.Any(ff => ff.Name.Equals(currentFontName)))
                {
                    throw new Exception($"Шрифт '{currentFontName}' не найден.");
                }

                // Проверка корректности размера шрифта
                if (currentFontSize <= 0 || float.IsNaN(currentFontSize))
                {
                    throw new Exception("Размер шрифта должен быть положительным числом.");
                }

                // Создание нового шрифта с учётом всех текущих условий
                FontStyle style = FontStyle.Regular;
                if (isBold) style |= FontStyle.Bold;
                if (isItalic) style |= FontStyle.Italic;
                if (isUnderlined) style |= FontStyle.Underline;

                // Создание нового шрифта
                Font newFont = new Font(currentFontName, currentFontSize, style);

                // Применение нового шрифта и текста к выбранному Label
                selectedLabel.Text = richTextBox_Changing_text.Text;
                selectedLabel.Font = newFont;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при применении изменений: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button_Apply_changes_Click(object sender, EventArgs e)
        {
            ApplyChangesToSelectedLabel();
        }

        private void ApplyCurrentFontSettingsToSelectedLabel()
        {
            try
            {
                // Проверка наличия подходящего семейства шрифтов
                if (!System.Drawing.FontFamily.Families.Any(ff => ff.Name.Equals(currentFontName)))
                {
                    throw new Exception($"Шрифт '{currentFontName}' не найден.");
                }

                // Проверка корректности размера шрифта
                if (currentFontSize <= 0 || float.IsNaN(currentFontSize))
                {
                    throw new Exception("Размер шрифта должен быть положительным числом.");
                }

                // Создание нового шрифта с учётом всех текущих условий
                FontStyle style = FontStyle.Regular;
                if (isBold) style |= FontStyle.Bold;
                if (isItalic) style |= FontStyle.Italic;
                if (isUnderlined) style |= FontStyle.Underline;

                // Создание нового шрифта
                Font newFont = new Font(currentFontName, currentFontSize, style);

                // Применение нового шрифта и текста к выбранному Label
                selectedLabel.Text = richTextBox_Changing_text.Text;
                selectedLabel.Font = newFont;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при применении изменений: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}