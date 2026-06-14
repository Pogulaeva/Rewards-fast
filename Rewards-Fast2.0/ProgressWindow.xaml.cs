using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace Rewards_Fast2._0
{
    public partial class ProgressWindow : Window
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private bool _isComplete = false;
        private bool _isClosingByCode = false;
        private readonly ManualResetEventSlim _pauseEvent = new ManualResetEventSlim(true); // По умолчанию НЕ на паузе

        public CancellationToken CancellationToken => _cts.Token;
        public ManualResetEventSlim PauseEvent => _pauseEvent;

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

                if (current >= total)
                {
                    _isComplete = true;
                }
            });
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Если окно закрывается по команде из MainWindow - разрешаем
            if (_isClosingByCode)
            {
                base.OnClosing(e);
                return;
            }

            // Если генерация уже завершена - закрываемся без вопросов
            if (_isComplete)
            {
                base.OnClosing(e);
                return;
            }

            // СТАВИМ ГЕНЕРАЦИЮ НА ПАУЗУ
            _pauseEvent.Reset();

            // Иначе спрашиваем пользователя
            var result = System.Windows.MessageBox.Show(
                "Генерация не завершена. Прервать?\n\nВсе уже сгенерированные файлы будут удалены.",
                "Подтверждение отмены",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            // СНИМАЕМ С ПАУЗЫ
            _pauseEvent.Set();

            if (result == MessageBoxResult.Yes)
            {
                // Пользователь подтвердил отмену
                _cts.Cancel();
                e.Cancel = true; // НЕ закрываем окно сейчас
            }
            else
            {
                // Пользователь передумал - отменяем закрытие
                e.Cancel = true;
            }
        }

        public void CloseWindow()
        {
            Dispatcher.Invoke(() =>
            {
                _isClosingByCode = true;
                _isComplete = true;
                Close();
            });
        }
    }
}