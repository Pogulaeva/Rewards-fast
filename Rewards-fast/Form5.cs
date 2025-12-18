using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Rewards_fast
{
    public partial class Template_Constructor : Form
    {
        public string Variable1 { get; set; }
        public string Variable2 { get; set; }
        public string Variable3 { get; set; }

        public Template_Constructor(string param1, string param2, object objParam)
        {
            InitializeComponent();
            // Присваиваем значения переменным
            Variable1 = param1;
            Variable2 = param2;
            if (objParam is Image)
                Variable3 = "Изображение";
            else if (objParam is string)
                Variable3 = (string)objParam;

            SetTextBoxValues();
        }
        // Метод для присвоения значений TextBox из переменных
        private void SetTextBoxValues()
        {
            textBox1.Text = Variable1 ?? "";
            textBox2.Text = Variable2 ?? "";
            textBox3.Text = Variable3 ?? "";
        }
    }
}
