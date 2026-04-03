using System;
using System.Collections.Generic;
using System.Text;
using Rewards_Fast2._0.Models;

namespace Rewards_Fast2._0.Services
{
    /// <summary>
    /// Интерфейс для склонения ФИО (чтобы можно было легко заменить реализацию)
    /// </summary>
    public interface INameDeclensionService
    {
        void DeclinePersons(List<Person> persons);
        string GetDativeCase(string fullName);
        void ClearCache();
    }

    /// <summary>
    /// Сервис для склонения ФИО в дательный падеж
    /// Текущая реализация: собственная логика (можно заменить на библиотеку)
    /// </summary>
    public class NameDeclensionService : INameDeclensionService
    {
        private readonly Dictionary<string, string> _declensionCache = new Dictionary<string, string>();

        /// <summary>
        /// Склоняет список людей (заполняет LastNameDative, FirstNameDative, MiddleNameDative)
        /// </summary>
        public void DeclinePersons(List<Person> persons)
        {
            if (persons == null || persons.Count == 0)
                return;

            foreach (var person in persons)
            {
                // Пропускаем, если уже есть склонённая форма
                if (!string.IsNullOrEmpty(person.LastNameDative))
                    continue;

                // Склоняем каждую часть отдельно (используя вашу логику)
                Gender gender = DetermineGender(person.MiddleName);

                person.LastNameDative = ConvertLastNameToDative(person.LastName, gender);
                person.FirstNameDative = ConvertFirstNameToDative(person.FirstName, gender);
                person.MiddleNameDative = ConvertPatronymicToDative(person.MiddleName, gender);
            }
        }

        /// <summary>
        /// Склоняет полное ФИО в дательный падеж (с кешированием)
        /// </summary>
        public string GetDativeCase(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return fullName;

            if (_declensionCache.TryGetValue(fullName, out string? cached) && cached != null)
                return cached;

            string result = DeclineFullNameToDative(fullName);
            _declensionCache[fullName] = result;
            return result;
        }

        /// <summary>
        /// Очистить кеш
        /// </summary>
        public void ClearCache()
        {
            _declensionCache.Clear();
        }

        #region ВАША ЛОГИКА СКЛОНЕНИЯ (перенесена из Form5.cs)

        private enum Gender
        {
            Male,
            Female,
            Unknown
        }

        private Gender DetermineGender(string patronymic)
        {
            if (string.IsNullOrEmpty(patronymic))
                return Gender.Unknown;

            if (patronymic.EndsWith("ич") || patronymic.EndsWith("лы") || patronymic.EndsWith("зы"))
                return Gender.Male;
            else if (patronymic.EndsWith("на") || patronymic.EndsWith("зы") || patronymic.EndsWith("лы"))
                return Gender.Female;
            else
                return Gender.Unknown;
        }

        private string ConvertLastNameToDative(string lastName, Gender gender)
        {
            if (string.IsNullOrEmpty(lastName))
                return lastName;

            if (gender == Gender.Male)
            {
                if (lastName.EndsWith("ых") || lastName.EndsWith("их") || lastName.EndsWith("е") ||
                    lastName.EndsWith("и") || lastName.EndsWith("о") || lastName.EndsWith("у") ||
                    lastName.EndsWith("ы") || lastName.EndsWith("э") || lastName.EndsWith("ю"))
                    return lastName;
                else if (lastName.EndsWith("ов") || lastName.EndsWith("ев") || lastName.EndsWith("ин") ||
                         lastName.EndsWith("ын") || lastName.EndsWith("н") || lastName.EndsWith("в") ||
                         lastName.EndsWith("б") || lastName.EndsWith("г") || lastName.EndsWith("д") ||
                         lastName.EndsWith("ж") || lastName.EndsWith("з") || lastName.EndsWith("к") ||
                         lastName.EndsWith("л") || lastName.EndsWith("м") || lastName.EndsWith("п") ||
                         lastName.EndsWith("р") || lastName.EndsWith("с") || lastName.EndsWith("т") ||
                         lastName.EndsWith("ф") || lastName.EndsWith("х") || lastName.EndsWith("ц") ||
                         lastName.EndsWith("ч") || lastName.EndsWith("ш") || lastName.EndsWith("щ"))
                    return lastName + "у";
                else if (lastName.EndsWith("ский") || lastName.EndsWith("цкий"))
                    return lastName.Substring(0, lastName.Length - 2) + "ому";
                else if (lastName.EndsWith("ий"))
                    return lastName.Substring(0, lastName.Length - 2) + "ему";
                else if (lastName.EndsWith("ой"))
                    return lastName.Substring(0, lastName.Length - 1) + "му";
                else if (lastName.EndsWith("й") || lastName.EndsWith("ь"))
                    return lastName.Substring(0, lastName.Length - 1) + "ю";
                else if (lastName.EndsWith("ия") || lastName.EndsWith("ея") || lastName.EndsWith("ая") ||
                         lastName.EndsWith("оя") || lastName.EndsWith("уя") || lastName.EndsWith("эя") ||
                         lastName.EndsWith("юя") || lastName.EndsWith("яя"))
                    return lastName.Substring(0, lastName.Length - 1) + "е";
                else
                    return lastName + "е";
            }
            else if (gender == Gender.Female)
            {
                if (lastName.EndsWith("ина"))
                    return lastName.Substring(0, lastName.Length - 1) + "е";
                else if (lastName.EndsWith("ых") || lastName.EndsWith("их") || lastName.EndsWith("е") ||
                         lastName.EndsWith("и") || lastName.EndsWith("о") || lastName.EndsWith("у") ||
                         lastName.EndsWith("ы") || lastName.EndsWith("э") || lastName.EndsWith("ю"))
                    return lastName;
                else if (lastName.EndsWith("й") || lastName.EndsWith("ь"))
                    return lastName;
                else if (lastName == "Топчая")
                    return "Топчей";
                else if (lastName.EndsWith("ия") || lastName.EndsWith("ея") || lastName.EndsWith("ая") ||
                         lastName.EndsWith("оя") || lastName.EndsWith("уя") || lastName.EndsWith("эя") ||
                         lastName.EndsWith("юя") || lastName.EndsWith("яя"))
                    return lastName.Substring(0, lastName.Length - 1) + "е";
                else
                    return lastName.Substring(0, lastName.Length - 1) + "ой";
            }
            else
                return lastName;
        }

        private string ConvertFirstNameToDative(string firstName, Gender gender)
        {
            if (string.IsNullOrEmpty(firstName))
                return firstName;

            if (gender == Gender.Male)
            {
                if (firstName.EndsWith("н"))
                    return firstName + "у";
                else if (firstName.EndsWith("а") || firstName.EndsWith("я"))
                    return firstName.Substring(0, firstName.Length - 1) + "е";
                else if (firstName.EndsWith("й") || firstName.EndsWith("ь"))
                    return firstName.Substring(0, firstName.Length - 1) + "ю";
                else
                    return firstName + "у";
            }
            else if (gender == Gender.Female)
            {
                if (firstName.EndsWith("ия"))
                    return firstName.Substring(0, firstName.Length - 1) + "и";
                else if (firstName.EndsWith("а") || firstName.EndsWith("я"))
                    return firstName.Substring(0, firstName.Length - 1) + "е";
                else
                    return firstName + "е";
            }
            else
                return firstName;
        }

        private string ConvertPatronymicToDative(string patronymic, Gender gender)
        {
            if (string.IsNullOrEmpty(patronymic))
                return patronymic;

            if (gender == Gender.Male)
            {
                if (patronymic.EndsWith("ич"))
                    return patronymic.Substring(0, patronymic.Length - 2) + "ичу";
                else
                    return patronymic + "у";
            }
            else if (gender == Gender.Female)
            {
                if (patronymic.EndsWith("на"))
                    return patronymic.Substring(0, patronymic.Length - 2) + "не";
                else
                    return patronymic + "е";
            }
            else
                return patronymic;
        }

        private string DeclineFullNameToDative(string fullName)
        {
            var parts = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
                return fullName;

            string lastName = parts[0];
            string firstName = parts[1];
            string middleName = parts[2];

            Gender gender = DetermineGender(middleName);

            return $"{ConvertLastNameToDative(lastName, gender)} " +
                   $"{ConvertFirstNameToDative(firstName, gender)} " +
                   $"{ConvertPatronymicToDative(middleName, gender)}";
        }

        #endregion
    }
}
