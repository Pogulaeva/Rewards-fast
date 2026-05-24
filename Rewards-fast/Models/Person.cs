using System;
using System.Collections.Generic;
using System.Text;

namespace Rewards_Fast2._0.Models
{
    public class Person
    {
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;

        public string FullName => $"{LastName} {FirstName} {MiddleName}".Trim();

        // Результаты склонения от библиотеки NPetrovich
        public string LastNameDative { get; set; } = string.Empty;
        public string FirstNameDative { get; set; } = string.Empty;
        public string MiddleNameDative { get; set; } = string.Empty;

        // Результаты склонения от резервного (самописного) метода
        public string LastNameDativeFallback { get; set; } = string.Empty;
        public string FirstNameDativeFallback { get; set; } = string.Empty;
        public string MiddleNameDativeFallback { get; set; } = string.Empty;

        // Поля для хранения полных ФИО в дательном падеже
        private string _fullNameDativeLibrary = string.Empty;
        private string _fullNameDativeFallback = string.Empty;
        private string _fullNameDative = string.Empty;

        /// <summary>
        /// ФИО в дательном падеже от библиотеки NPetrovich
        /// </summary>
        public string FullNameDativeLibrary
        {
            get => string.IsNullOrEmpty(_fullNameDativeLibrary)
                ? $"{LastNameDative} {FirstNameDative} {MiddleNameDative}".Trim()
                : _fullNameDativeLibrary;
            set => _fullNameDativeLibrary = value;
        }

        /// <summary>
        /// ФИО в дательном падеже от резервного (самописного) метода
        /// </summary>
        public string FullNameDativeFallback
        {
            get => string.IsNullOrEmpty(_fullNameDativeFallback)
                ? $"{LastNameDativeFallback} {FirstNameDativeFallback} {MiddleNameDativeFallback}".Trim()
                : _fullNameDativeFallback;
            set => _fullNameDativeFallback = value;
        }

        /// <summary>
        /// ФИО в дательном падеже (выбранный пользователем вариант)
        /// </summary>
        public string FullNameDative
        {
            get => string.IsNullOrEmpty(_fullNameDative)
                ? FullNameDativeLibrary
                : _fullNameDative;
            set => _fullNameDative = value;
        }

        /// <summary>
        /// Расходятся ли результаты двух методов склонения
        /// </summary>
        public bool HasDeclensionMismatch =>
            !string.IsNullOrEmpty(FullNameDativeLibrary) &&
            !string.IsNullOrEmpty(FullNameDativeFallback) &&
            FullNameDativeLibrary != FullNameDativeFallback;

        public string GetFullName(bool useDative)
        {
            return useDative ? FullNameDative : FullName;
        }

        public bool IsDeclined => !string.IsNullOrEmpty(LastNameDative) ||
                                   !string.IsNullOrEmpty(FirstNameDative) ||
                                   !string.IsNullOrEmpty(MiddleNameDative);
    }
}