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
        public Image ImageToShow { get; set; }

        public Additional_information2(Image its)
        {
            InitializeComponent();

            ImageToShow = its;
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

            Template_Constructor newForm = new Template_Constructor(textBox_Full_Name_list_location.Text, folderPath, ImageToShow);
            newForm.Show();
            this.Close();         // закрываем текущую форму
        }
    }
}
