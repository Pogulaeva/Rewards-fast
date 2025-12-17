using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Rewards_fast
{
    public partial class Additional_information2 : Form
    {
        public Additional_information2()
        {
            InitializeComponent();
        }

        private void button_Full_Name_list_location_Click(object sender, EventArgs e)
        {
            OpenFileDialog Filedialog = new OpenFileDialog();
            if (Filedialog.ShowDialog() == DialogResult.OK)
            {
                textBox_Full_Name_list_location.Text = Filedialog.FileName;
            }
        }

        private void button_choosing_location_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                textBox_location.Text = dialog.SelectedPath;
            }
        }

        private void button_template_Click(object sender, EventArgs e)
        {
            //Создание папки в выбранной директории
            string folderPath = Path.Combine(textBox_location.Text, textBox_folder_name.Text);
            DirectoryInfo directoryInfo = Directory.CreateDirectory(folderPath);
            MessageBox.Show($"Папка успешно создана по пути: {directoryInfo.FullName}", "Успех");

            //Передача данных о пути списка ФИО
            string FIO = textBox_Full_Name_list_location.Text;

            this.Close();         // закрываем текущую форму
        }
    }
}
