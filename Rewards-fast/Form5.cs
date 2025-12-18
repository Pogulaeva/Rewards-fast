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
        string FIO;
        string foldername;
        Image image;
        string image2;

        public Template_Constructor(string param1, string param2, object objParam)
        {
            InitializeComponent();

            FIO = param1;
            foldername = param2;
            if (objParam is Image)
                image = (Image)objParam;
            else if (objParam is string)
                image2 = (string)objParam;
        }
    }

}
