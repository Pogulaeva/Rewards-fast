using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Rewards_fast
{
    public partial class Choosing_template : Form
    {

        Image selectedImage;

        private Form imagePreview;

        private Timer hoverTimer;
        private bool isHoverActive = false;
        private PictureBox activePicBox;

        public Choosing_template()
        {
            InitializeComponent();

            // Инициализация таймера с интервалом 2 секунды
            hoverTimer = new Timer();
            hoverTimer.Interval = 2000; //Интервал: 2 секунды
            hoverTimer.Tick += HoverTimer_Tick;

            // Назначаем общие обработчики для всех PictureBox
            foreach (Control control in Controls)
            {
                if (control is PictureBox picBox)
                {
                    picBox.MouseHover += (sender, args) => OnMouseHover(picBox);
                    picBox.MouseMove += (sender, args) => OnMouseMove(picBox);
                    picBox.Click += (sender, args) => OnClick(picBox);
                }
            }
        }

        private void button_Back_Click(object sender, EventArgs e)
        {
            Initial_form newForm = new Initial_form();
            newForm.Show();
            this.Hide();
        }

        private void pictureBox_template1_Click(object sender, EventArgs e)
        {
            // Если происходит клик, останавливаем таймер и скрываем увеличенное изображение
            hoverTimer.Stop();
            isHoverActive = false;
            CloseCurrentPreview();

            if (pictureBox_template1.Image != null)
            {
                Additional_information2 newForm = new Additional_information2(pictureBox_template1.Image);
                newForm.ShowDialog();

                //После завершения работы Additional_information2, закрываем текущую
                this.Close();
            }
        }

        private void pictureBox_template2_Click(object sender, EventArgs e)
        {
            // Если происходит клик, останавливаем таймер и скрываем увеличенное изображение
            hoverTimer.Stop();
            isHoverActive = false;
            CloseCurrentPreview();

            if (pictureBox_template2.Image != null)
            {
                Additional_information2 newForm = new Additional_information2(pictureBox_template2.Image);
                newForm.ShowDialog();

                //После завершения работы Additional_information2, закрываем текущую
                this.Close();
            }
        }

        private void pictureBox_template3_Click(object sender, EventArgs e)
        {
            // Если происходит клик, останавливаем таймер и скрываем увеличенное изображение
            hoverTimer.Stop();
            isHoverActive = false;
            CloseCurrentPreview();

            if (pictureBox_template3.Image != null)
            {
                Additional_information2 newForm = new Additional_information2(pictureBox_template3.Image);
                newForm.ShowDialog();

                //После завершения работы Additional_information2, закрываем текущую
                this.Close();
            }
        }

        private void pictureBox_template4_Click(object sender, EventArgs e)
        {
            // Если происходит клик, останавливаем таймер и скрываем увеличенное изображение
            hoverTimer.Stop();
            isHoverActive = false;
            CloseCurrentPreview();

            if (pictureBox_template4.Image != null)
            {
                Additional_information2 newForm = new Additional_information2(pictureBox_template4.Image);
                newForm.ShowDialog();

                //После завершения работы Additional_information2, закрываем текущую
                this.Close();
            }
        }

        private void pictureBox_template5_Click(object sender, EventArgs e)
        {
            // Если происходит клик, останавливаем таймер и скрываем увеличенное изображение
            hoverTimer.Stop();
            isHoverActive = false;
            CloseCurrentPreview();

            if (pictureBox_template5.Image != null)
            {
                Additional_information2 newForm = new Additional_information2(pictureBox_template5.Image);
                newForm.ShowDialog();

                //После завершения работы Additional_information2, закрываем текущую
                this.Close();
            }
        }

        private void pictureBox_template6_Click(object sender, EventArgs e)
        {
            // Если происходит клик, останавливаем таймер и скрываем увеличенное изображение
            hoverTimer.Stop();
            isHoverActive = false;
            CloseCurrentPreview();

            if (pictureBox_template6.Image != null)
            {
                Additional_information2 newForm = new Additional_information2(pictureBox_template6.Image);
                newForm.ShowDialog();

                //После завершения работы Additional_information2, закрываем текущую
                this.Close();
            }
        }

        // Обработчик события MouseHover
        private void OnMouseHover(PictureBox picBox)
        {
            // Активируем активную картинку и начинаем отсчёт времени
            activePicBox = picBox;
            hoverTimer.Start();
        }

        // Обработчик события MouseMove
        private void OnMouseMove(PictureBox picBox)
        {
            // Останавливаем таймер при движении мыши
            hoverTimer.Stop();
        }

        // Обработчик события Click
        private void OnClick(PictureBox picBox)
        {
            // Останавливаем таймер и закрываем форму
            hoverTimer.Stop();
            ClosePreview();
        }

        // Обработчик завершения таймера
        private void HoverTimer_Tick(object sender, EventArgs e)
        {
            hoverTimer.Stop();

            // Проверяем, что активная картинка доступна
            if (activePicBox != null && activePicBox.Image != null)
            {
                ShowImagePreview(activePicBox.Image);
            }
        }

        // Показ увеличенного изображения
        private void ShowImagePreview(Image img)
        {
            ClosePreview();

            if (img != null)
            {
                imagePreview = new Form
                {
                    FormBorderStyle = FormBorderStyle.None,
                    StartPosition = FormStartPosition.Manual,
                    TopMost = true,
                    BackColor = Color.Black,
                    ShowInTaskbar = false
                };

                imagePreview.ClientSize = new Size(img.Width, img.Height);

                var previewPictureBox = new PictureBox
                {
                    Dock = DockStyle.Fill,
                    Image = img,
                    SizeMode = PictureBoxSizeMode.Zoom
                };

                imagePreview.Controls.Add(previewPictureBox);

                var screen = Screen.PrimaryScreen.WorkingArea;
                imagePreview.Location = new Point(
                    (screen.Width - imagePreview.Width) / 2,
                    (screen.Height - imagePreview.Height) / 2
                );

                imagePreview.Deactivate += (s, ev) =>
                {
                    ClosePreview();
                };

                imagePreview.Show();
            }
        }

        // Закрытие формы с увеличенным изображением
        private void ClosePreview()
        {
            if (imagePreview != null)
            {
                try
                {
                    imagePreview.Close();
                }
                catch (ObjectDisposedException)
                {
                    Debug.WriteLine("Форма уже была удалена.");
                }
                finally
                {
                    imagePreview = null;
                }
            }
        }

        private void CloseCurrentPreview()
        {
            if (imagePreview != null)
            {
                try
                {
                    imagePreview.Close();
                }
                catch (ObjectDisposedException)
                {
                    Debug.WriteLine("Форма уже была удалена.");
                }
                finally
                {
                    imagePreview = null;
                }
            }
        }
    }
}
