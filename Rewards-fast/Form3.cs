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
            }
        }
    }
}
