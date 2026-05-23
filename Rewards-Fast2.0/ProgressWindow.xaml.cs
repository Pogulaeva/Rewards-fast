using System;
using System.Windows;

namespace Rewards_Fast2._0
{
    public partial class ProgressWindow : Window
    {
        public ProgressWindow(int total)
        {
            InitializeComponent();
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