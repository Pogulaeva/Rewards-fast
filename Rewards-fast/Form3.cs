using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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

        private void PictureBox_template4_MouseDown(object sender, MouseEventArgs e)
        {
            // Правой кнопкой мыши показать увеличенное изображение
            if (e.Button == MouseButtons.Right)
            {
                PictureBox pb = sender as PictureBox;
                if (pb != null && pb.Image != null)
                {
                    ShowImagePreview(pb.Image);
                }
            }
        }

        private void pictureBox_template1_MouseDown_1(object sender, MouseEventArgs e)
        {
            // Правой кнопкой мыши показать увеличенное изображение
            if (e.Button == MouseButtons.Right)
            {
                PictureBox pb = sender as PictureBox;
                if (pb != null && pb.Image != null)
                {
                    ShowImagePreview(pb.Image);
                }
            }
        }

        private void pictureBox_template2_MouseDown(object sender, MouseEventArgs e)
        {
            // Правой кнопкой мыши показать увеличенное изображение
            if (e.Button == MouseButtons.Right)
            {
                PictureBox pb = sender as PictureBox;
                if (pb != null && pb.Image != null)
                {
                    ShowImagePreview(pb.Image);
                }
            }
        }



        private void ShowImagePreview(Image img)
        {
            // Если уже есть открытое предпросмотре - можно закрыть его и открыть снова
            if (imagePreview != null)
            {
                imagePreview.Close();
                imagePreview.Dispose();
                imagePreview = null;
            }

            imagePreview = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                TopMost = true,
                BackColor = Color.Black,
                ShowInTaskbar = false,
            };

            imagePreview.ClientSize = new Size(img.Width, img.Height);

            var previewPictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                Image = img,
                SizeMode = PictureBoxSizeMode.Zoom
            };

            imagePreview.Controls.Add(previewPictureBox);

            // Центрируем форму на экране
            var screen = Screen.PrimaryScreen.WorkingArea;
            imagePreview.Location = new Point(
                (screen.Width - imagePreview.Width) / 2,
                (screen.Height - imagePreview.Height) / 2
            );

            // Закрыть при клике вне окна
            imagePreview.Deactivate += (s, e) => imagePreview.Close();


            imagePreview.Show();
        }

        private void pictureBox_template3_MouseDown(object sender, MouseEventArgs e)
        {
            // Правой кнопкой мыши показать увеличенное изображение
            if (e.Button == MouseButtons.Right)
            {
                PictureBox pb = sender as PictureBox;
                if (pb != null && pb.Image != null)
                {
                    ShowImagePreview(pb.Image);
                }
            }
        }

        private void pictureBox_template5_MouseDown(object sender, MouseEventArgs e)
        {
            // Правой кнопкой мыши показать увеличенное изображение
            if (e.Button == MouseButtons.Right)
            {
                PictureBox pb = sender as PictureBox;
                if (pb != null && pb.Image != null)
                {
                    ShowImagePreview(pb.Image);
                }
            }
        }

        private void pictureBox_template6_MouseDown(object sender, MouseEventArgs e)
        {
            // Правой кнопкой мыши показать увеличенное изображение
            if (e.Button == MouseButtons.Right)
            {
                PictureBox pb = sender as PictureBox;
                if (pb != null && pb.Image != null)
                {
                    ShowImagePreview(pb.Image);
                }
            }
        }
    }
}
