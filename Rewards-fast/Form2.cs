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
    public partial class Additional_information : Form
    {
        public Additional_information()
        {
            InitializeComponent();
        }

        private void button_choosing_location_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                textBox_location.Text = dialog.SelectedPath;
            }
        }

        private void button_image_location_Click(object sender, EventArgs e)
        {
            OpenFileDialog Filedialog = new OpenFileDialog();
            if (Filedialog.ShowDialog() == DialogResult.OK)
            {
                textBox_image_location.Text = Filedialog.FileName;
            }
        }

        private void button_Full_Name_list_location_Click(object sender, EventArgs e)
        {
            OpenFileDialog Filedialog = new OpenFileDialog();
            if (Filedialog.ShowDialog() == DialogResult.OK)
            {
                textBox_Full_Name_list_location.Text = Filedialog.FileName;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Initial_form newForm = new Initial_form();
            newForm.Show();
            this.Hide();
        }

        private void button_your_template_Click(object sender, EventArgs e)
        {
            this.Hide();
        }
    }
}
