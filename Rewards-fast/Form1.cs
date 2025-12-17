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
    public partial class Initial_form : Form
    {
        public Initial_form()
        {
            InitializeComponent();
        }

        private void button_your_template_Click(object sender, EventArgs e)
        {
            Additional_information newForm = new Additional_information();
            newForm.Show();
            this.Hide();
        }

        private void button_available_template_Click(object sender, EventArgs e)
        {
            Choosing_template newForm2 = new Choosing_template();
            newForm2.Show();
            this.Hide();
        }
    }
}
