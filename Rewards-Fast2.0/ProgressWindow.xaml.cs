using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace Rewards_Fast2._0
{
    public partial class ProgressWindow : Window
    {
        private int _total;

        public ProgressWindow(int total)
        {
            InitializeComponent();
            _total = total;
            ProgressBar.Maximum = total;
        }

        public void UpdateProgress(int current, int total)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar.Value = current;
                StatusText.Text = $"Генерация: {current} из {total}";
            });
        }
    }
}
