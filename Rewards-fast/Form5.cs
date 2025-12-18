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
    public partial class Template_Constructor : Form
    {
        public string Txt
        {
            get { return textBox1.Text;}
            set { textBox1.Text = value;}
        }

        public string Txt2
        {
            get { return textBox2.Text; }
            set { textBox2.Text = value; }
        }

        public string Txt3
        {
            get { return textBox3.Text; }
            set { textBox3.Text = value; }
        }

        public Template_Constructor()
        {
            InitializeComponent();
        }
    }
}
