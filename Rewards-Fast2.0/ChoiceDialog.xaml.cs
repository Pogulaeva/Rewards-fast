using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Rewards_Fast2._0.Models;

namespace Rewards_Fast2._0
{
    public partial class ChoiceDialog : Window
    {
        public enum UserChoice
        {
            Option1,
            Option2,
            Cancel,
            ClosedByX  // Новый вариант для закрытия через крестик
        }

        private UserChoice _result = UserChoice.ClosedByX;  // По умолчанию - закрытие через X

        public ChoiceDialog(List<Person> personsWithMismatch)
        {
            InitializeComponent();

            string examples = string.Join("\n", personsWithMismatch.Take(5).Select(p =>
                $"  • {p.FullName}\n    → Вариант 1: {p.FullNameDativeLibrary}\n    → Вариант 2: {p.FullNameDativeFallback}"));

            if (personsWithMismatch.Count > 5)
                examples += $"\n  … и ещё {personsWithMismatch.Count - 5} записей";

            ExamplesText.Text = $"📊 Обнаружено {personsWithMismatch.Count} ФИО с расхождениями:\n\n{examples}";
        }

        private void Option1Button_Click(object sender, RoutedEventArgs e)
        {
            _result = UserChoice.Option1;
            Close();
        }

        private void Option2Button_Click(object sender, RoutedEventArgs e)
        {
            _result = UserChoice.Option2;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _result = UserChoice.Cancel;
            Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // При закрытии через X результат остаётся ClosedByX
            base.OnClosing(e);
        }

        public UserChoice GetResult()
        {
            ShowDialog();
            return _result;
        }
    }
}