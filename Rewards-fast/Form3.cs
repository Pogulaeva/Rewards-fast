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
        public Choosing_template()
        {
            InitializeComponent();
        }

        private void button_Back_Click(object sender, EventArgs e)
        {
            Initial_form newForm = new Initial_form();
            newForm.Show();
            this.Hide();
        }

        private void pictureBox_template1_Click(object sender, EventArgs e)
        {
            if (pictureBox_template1.Image != null)
            {
                // Запоминаем выбранное изображение
                selectedImage = pictureBox_template1.Image;

                // Для проверки можно вывести сообщение
                MessageBox.Show("Изображение выбрано и сохранено для дальнейшей работы.");

                Additional_information2 newForm = new Additional_information2();
                newForm.ShowDialog();

                //После завершения работы Additional_information2, закрываем текущую
                this.Close();
            }
        }

        private void pictureBox_template2_Click(object sender, EventArgs e)
        {
            if (pictureBox_template2.Image != null)
            {
                // Запоминаем выбранное изображение
                selectedImage = pictureBox_template2.Image;

                // Для проверки можно вывести сообщение
                MessageBox.Show("Изображение выбрано и сохранено для дальнейшей работы.");

                Additional_information2 newForm = new Additional_information2();
                newForm.ShowDialog();

                //После завершения работы Additional_information2, закрываем текущую
                this.Close();
            }
        }

        private void pictureBox_template3_Click(object sender, EventArgs e)
        {
            if (pictureBox_template3.Image != null)
            {
                // Запоминаем выбранное изображение
                selectedImage = pictureBox_template3.Image;

                // Для проверки можно вывести сообщение
                MessageBox.Show("Изображение выбрано и сохранено для дальнейшей работы.");

                Additional_information2 newForm = new Additional_information2();
                newForm.ShowDialog();

                //После завершения работы Additional_information2, закрываем текущую
                this.Close();
            }
        }

        private void pictureBox_template4_Click(object sender, EventArgs e)
        {
            if (pictureBox_template4.Image != null)
            {
                // Запоминаем выбранное изображение
                selectedImage = pictureBox_template4.Image;

                // Для проверки можно вывести сообщение
                MessageBox.Show("Изображение выбрано и сохранено для дальнейшей работы.");

                Additional_information2 newForm = new Additional_information2();
                newForm.ShowDialog();

                //После завершения работы Additional_information2, закрываем текущую
                this.Close();
            }
        }

        private void pictureBox_template5_Click(object sender, EventArgs e)
        {
            if (pictureBox_template5.Image != null)
            {
                // Запоминаем выбранное изображение
                selectedImage = pictureBox_template5.Image;

                // Для проверки можно вывести сообщение
                MessageBox.Show("Изображение выбрано и сохранено для дальнейшей работы.");

                Additional_information2 newForm = new Additional_information2();
                newForm.ShowDialog();

                //После завершения работы Additional_information2, закрываем текущую
                this.Close();
            }
        }

        private void pictureBox_template6_Click(object sender, EventArgs e)
        {
            if (pictureBox_template6.Image != null)
            {
                // Запоминаем выбранное изображение
                selectedImage = pictureBox_template6.Image;

                // Для проверки можно вывести сообщение
                MessageBox.Show("Изображение выбрано и сохранено для дальнейшей работы.");

                Additional_information2 newForm = new Additional_information2();
                newForm.ShowDialog();

                //После завершения работы Additional_information2, закрываем текущую
                this.Close();
            }
        }

        private Form imagePreview;

        private void pictureBox_template2_MouseHover(object sender, EventArgs e)
        {
            // Показываем увеличенное изображение при наведении мыши
            PictureBox pb = sender as PictureBox;
            if (pb != null && pb.Image != null)
            {
                ShowImagePreview2(pb.Image);
            }
        }

        private void ShowImagePreview2(Image img)
        {
            // Перед созданием новой формы убедимся, что прежняя закрыта
            ClosePreviousPreview();

            // Создание новой формы для отображения увеличенного изображения
            imagePreview = new Form()
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                TopMost = true,
                BackColor = Color.Black,
                ShowInTaskbar = false
            };

            // Настройка размеров формы под размеры изображения
            imagePreview.ClientSize = new Size(img.Width, img.Height);

            // Подготовка компонента PictureBox для показа изображения
            var previewPictureBox = new PictureBox()
            {
                Dock = DockStyle.Fill,
                Image = img,
                SizeMode = PictureBoxSizeMode.Zoom
            };

            // Добавление элемента PictureBox на форму
            imagePreview.Controls.Add(previewPictureBox);

            // Центрирование формы на рабочем столе
            var screen = Screen.PrimaryScreen.WorkingArea;
            imagePreview.Location = new Point((screen.Width - imagePreview.Width) / 2, (screen.Height - imagePreview.Height) / 2);

            // Реакция на потерю фокуса формы (закрытие при переходе внимания на другое окно)
            imagePreview.Deactivate += (s, ev) =>
            {
                CloseCurrentPreview();
            };

            // Отображение формы
            imagePreview.Show();
        }

        private void ClosePreviousPreview()
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
                    imagePreview = null; // Сброс ссылки на форму
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
                    imagePreview = null; // Сброс ссылки на форму
                }
            }
        }

        private void pictureBox_template1_MouseHover(object sender, EventArgs e)
        {
            // Показываем увеличенное изображение при наведении мыши
            PictureBox pb = sender as PictureBox;
            if (pb != null && pb.Image != null)
            {
                ShowImagePreview2(pb.Image);
            }
        }

        private void pictureBox_template3_MouseHover(object sender, EventArgs e)
        {
            // Показываем увеличенное изображение при наведении мыши
            PictureBox pb = sender as PictureBox;
            if (pb != null && pb.Image != null)
            {
                ShowImagePreview2(pb.Image);
            }
        }

        private void pictureBox_template4_MouseHover(object sender, EventArgs e)
        {
            // Показываем увеличенное изображение при наведении мыши
            PictureBox pb = sender as PictureBox;
            if (pb != null && pb.Image != null)
            {
                ShowImagePreview2(pb.Image);
            }
        }

        private void pictureBox_template5_MouseHover(object sender, EventArgs e)
        {
            // Показываем увеличенное изображение при наведении мыши
            PictureBox pb = sender as PictureBox;
            if (pb != null && pb.Image != null)
            {
                ShowImagePreview2(pb.Image);
            }
        }

        private void pictureBox_template6_MouseHover(object sender, EventArgs e)
        {
            // Показываем увеличенное изображение при наведении мыши
            PictureBox pb = sender as PictureBox;
            if (pb != null && pb.Image != null)
            {
                ShowImagePreview2(pb.Image);
            }
        }
    }
}
