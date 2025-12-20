using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
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
            label_Bold.BackColor = Color.MediumSeaGreen;
        }

        private void label_Italics_Click(object sender, EventArgs e)
        {
            label_Italics.BackColor = Color.MediumSeaGreen;
        }

        private void label_Underlined_Click(object sender, EventArgs e)
        {
            label_Underlined.BackColor = Color.MediumSeaGreen;
        }

        private void label_FIO_Click(object sender, EventArgs e)
        {
            // Делаем текстовое поле доступным только для чтения
            richTextBox_Changing_text.ReadOnly = true;
            label_case.Visible = true;
            comboBox_case.Visible = true;
        }

        private void label_initial_speech_Click(object sender, EventArgs e)
        {
            // Делаем текстовое поле доступным только для чтения
            richTextBox_Changing_text.ReadOnly = false;
            label_case.Visible = false;
            comboBox_case.Visible = false;
        }

        private void label_final_speech_Click(object sender, EventArgs e)
        {
            // Делаем текстовое поле доступным только для чтения
            richTextBox_Changing_text.ReadOnly = false;
            label_case.Visible = false;
            comboBox_case.Visible = false;
        }

        private void label_post_Click(object sender, EventArgs e)
        {
            // Делаем текстовое поле доступным только для чтения
            richTextBox_Changing_text.ReadOnly = false;
            label_case.Visible = false;
            comboBox_case.Visible = false;
        }

        private void label_signature_decryption_Click(object sender, EventArgs e)
        {
            // Делаем текстовое поле доступным только для чтения
            richTextBox_Changing_text.ReadOnly = false;
            label_case.Visible = false;
            comboBox_case.Visible = false;
        }

        private void label_City_year_Click(object sender, EventArgs e)
        {
            // Делаем текстовое поле доступным только для чтения
            richTextBox_Changing_text.ReadOnly = false;
            label_case.Visible = false;
            comboBox_case.Visible = false;
        }
    }
}