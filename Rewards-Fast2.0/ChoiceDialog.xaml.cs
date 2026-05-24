using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Rewards_Fast2._0.Models;

namespace Rewards_Fast2._0
{
    public partial class ChoiceDialog : Window
    {
        public enum UserChoice
        {
            Generate,      // Сгенерировать с выбранными вариантами
            Skip,          // Пропустить проблемные, создать файл со списком
            ClosedByX      // Закрыто через крестик (отмена)
        }

        public class ProblemPerson : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;

            public string FullName { get; set; } = string.Empty;
            public string Option1 { get; set; } = string.Empty;
            public string Option2 { get; set; } = string.Empty;

            private string _selectedOption = "Library";
            public string SelectedOption
            {
                get => _selectedOption;
                set
                {
                    _selectedOption = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedOption)));
                }
            }

            public ProblemPerson(Person person)
            {
                FullName = person.FullName;
                Option1 = person.FullNameDativeLibrary;
                Option2 = person.FullNameDativeFallback;
                SelectedOption = "Library";
            }
        }

        private ObservableCollection<ProblemPerson> _problemPersons = new ObservableCollection<ProblemPerson>();

        public UserChoice Result { get; private set; } = UserChoice.ClosedByX;
        public Dictionary<string, bool> SelectedOptions { get; private set; } = new Dictionary<string, bool>();

        public ChoiceDialog(List<Person> personsWithMismatch)
        {
            InitializeComponent();

            foreach (var person in personsWithMismatch)
            {
                _problemPersons.Add(new ProblemPerson(person));
            }

            ProblemsGrid.ItemsSource = _problemPersons;
            InfoText.Text = $"📊 Обнаружено {personsWithMismatch.Count} ФИО с расхождениями.\n" +
                           "Выберите вариант склонения для каждого ФИО индивидуально или используйте кнопки массового выбора.";
        }

        private void AllOption1Button_Click(object sender, RoutedEventArgs e)
        {
            foreach (var person in _problemPersons)
            {
                person.SelectedOption = "Library";
            }
        }

        private void AllOption2Button_Click(object sender, RoutedEventArgs e)
        {
            foreach (var person in _problemPersons)
            {
                person.SelectedOption = "Fallback";
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var person in _problemPersons)
            {
                person.SelectedOption = "Library";
            }
        }

        private void ProblemsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var item = ProblemsGrid.SelectedItem as ProblemPerson;
            if (item != null)
            {
                item.SelectedOption = item.SelectedOption == "Library" ? "Fallback" : "Library";
            }
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var person in _problemPersons)
            {
                SelectedOptions[person.FullName] = person.SelectedOption == "Library";
            }
            Result = UserChoice.Generate;
            Close();
        }

        private void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            Result = UserChoice.Skip;
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Если закрыли через крестик - считаем как отмену
            if (Result == UserChoice.ClosedByX)
            {
                Result = UserChoice.ClosedByX;
            }
            base.OnClosing(e);
        }
    }
}