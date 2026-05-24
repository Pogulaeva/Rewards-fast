using System;
using System.Collections.Generic;
using System.Text;
using Rewards_Fast2._0.Models;
using NPetrovich;

namespace Rewards_Fast2._0.Services
{
    public interface INameDeclensionService
    {
        void DeclinePersons(List<Person> persons);
        string GetDativeCase(string fullName);
        void ClearCache();
    }

    /// <summary>
    /// Сервис для склонения ФИО в дательный падеж
    /// Использует библиотеку NPetrovich как основной метод,
    /// при ошибках — резервный самописный метод (БЕЗ ИЗМЕНЕНИЙ)
    /// </summary>
    public class NameDeclensionService : INameDeclensionService
    {
        private Petrovich? _petrovich;
        private readonly Dictionary<string, string> _declensionCache = new Dictionary<string, string>();
        private bool _useLibrary = true;

        public event EventHandler<string>? LibraryErrorOccurred;

        public NameDeclensionService()
        {
            try
            {
                _petrovich = new Petrovich();
                _useLibrary = true;
                System.Diagnostics.Debug.WriteLine("NPetrovich успешно загружена");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки NPetrovich: {ex.Message}");
                _useLibrary = false;
                LibraryErrorOccurred?.Invoke(this, $"NPetrovich не загружена: {ex.Message}");
            }
        }

        /// <summary>
        /// Склоняет список людей (заполняет LastNameDative, FirstNameDative, MiddleNameDative)
        /// </summary>
        public void DeclinePersons(List<Person> persons)
        {
            if (persons == null || persons.Count == 0)
                return;

            foreach (var person in persons)
            {
                if (!string.IsNullOrEmpty(person.LastNameDative))
                    continue;

                try
                {
                    if (_useLibrary && _petrovich != null)
                    {
                        DeclineWithLibrary(person);
                    }
                    else
                    {
                        DeclineWithFallback(person);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка в основном методе для {person.FullName}: {ex.Message}, используем резервный");
                    DeclineWithFallback(person);
                }
            }
        }

        /// <summary>
        /// Склонение с помощью библиотеки NPetrovich в дательный падеж
        /// </summary>
        /// <summary>
        /// Склонение с помощью библиотеки NPetrovich в дательный падеж
        /// </summary>
        private void DeclineWithLibrary(Person person)
        {
            if (_petrovich == null)
                throw new InvalidOperationException("Библиотека NPetrovich не инициализирована");

            _petrovich.LastName = person.LastName;
            _petrovich.FirstName = person.FirstName;
            _petrovich.MiddleName = person.MiddleName;
            _petrovich.AutoDetectGender = true;

            var inflected = _petrovich.InflectTo(Case.Dative);

            // Фамилия и имя — через библиотеку
            person.LastNameDative = string.IsNullOrEmpty(inflected.LastName) ? person.LastName : inflected.LastName;
            person.FirstNameDative = string.IsNullOrEmpty(inflected.FirstName) ? person.FirstName : inflected.FirstName;

            // Отчество — ВСЕГДА через проверенный fallback
            var gender = DetermineGender(person.MiddleName);
            person.MiddleNameDative = ConvertPatronymicToDative(person.MiddleName, gender);
        }

        public string GetDativeCase(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return fullName;

            if (_declensionCache.TryGetValue(fullName, out string? cached) && cached != null)
                return cached;

            string result = fullName;

            try
            {
                if (_useLibrary && _petrovich != null)
                {
                    var parts = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 3)
                    {
                        _petrovich.LastName = parts[0];
                        _petrovich.FirstName = parts[1];
                        _petrovich.MiddleName = parts[2];
                        _petrovich.AutoDetectGender = true;

                        var inflected = _petrovich.InflectTo(Case.Dative);
                        result = $"{inflected.LastName} {inflected.FirstName} {inflected.MiddleName}".Trim();
                    }
                    else
                    {
                        result = DeclineFullNameToDative(fullName);
                    }
                }
                else
                {
                    result = DeclineFullNameToDative(fullName);
                }
            }
            catch
            {
                result = DeclineFullNameToDative(fullName);
            }

            _declensionCache[fullName] = result;
            return result;
        }

        public void ClearCache()
        {
            _declensionCache.Clear();
        }

        private bool IsInitials(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;
            return input.Contains(".") && input.Length <= 5;
        }

        #region САМОПИСНЫЙ МЕТОД

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
                // Правила склонения мужских фамилий в дательном падеже
                if (lastName.EndsWith("ых") || lastName.EndsWith("их") || lastName.EndsWith("е") || lastName.EndsWith("и") || lastName.EndsWith("о") || lastName.EndsWith("у") || lastName.EndsWith("ы") || lastName.EndsWith("э") || lastName.EndsWith("ю"))
                    return lastName;
                else if (lastName.EndsWith("ов") || lastName.EndsWith("ев") || lastName.EndsWith("ин") || lastName.EndsWith("ын") || lastName.EndsWith("н") || lastName.EndsWith("в") || lastName.EndsWith("б") || lastName.EndsWith("г") || lastName.EndsWith("д") || lastName.EndsWith("ж") || lastName.EndsWith("з") || lastName.EndsWith("к") || lastName.EndsWith("л") || lastName.EndsWith("м") || lastName.EndsWith("п") || lastName.EndsWith("р") || lastName.EndsWith("с") || lastName.EndsWith("т") || lastName.EndsWith("ф") || lastName.EndsWith("х") || lastName.EndsWith("ц") || lastName.EndsWith("ч") || lastName.EndsWith("ш") || lastName.EndsWith("щ"))
                    return lastName + "у";
                else if (lastName.EndsWith("ский") || lastName.EndsWith("цкий"))
                    return lastName.Substring(0, lastName.Length - 2) + "ому";
                else if (lastName.EndsWith("ий"))
                    return lastName.Substring(0, lastName.Length - 2) + "ему";
                else if (lastName.EndsWith("ый"))
                    return lastName.Substring(0, lastName.Length - 2) + "ому";
                else if (lastName.EndsWith("ой"))
                    return lastName.Substring(0, lastName.Length - 1) + "му";
                else if (lastName.EndsWith("й") || lastName.EndsWith("ь"))
                    return lastName.Substring(0, lastName.Length - 1) + "ю";
                else if (lastName.EndsWith("ия"))
                    return lastName.Substring(0, lastName.Length - 1) + "и";
                else if (lastName.EndsWith("ея") || lastName.EndsWith("ая") || lastName.EndsWith("оя") || lastName.EndsWith("уя") || lastName.EndsWith("эя") || lastName.EndsWith("юя") || lastName.EndsWith("яя"))
                    return lastName.Substring(0, lastName.Length - 1) + "е";
                else if (lastName.EndsWith("иа") || lastName.EndsWith("еа") || lastName.EndsWith("аа") || lastName.EndsWith("оа") || lastName.EndsWith("уа") || lastName.EndsWith("эа") || lastName.EndsWith("юа") || lastName.EndsWith("яа"))
                    return lastName;
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
                else if (lastName.EndsWith("н") || lastName.EndsWith("в") || lastName.EndsWith("б") ||
                         lastName.EndsWith("г") || lastName.EndsWith("д") || lastName.EndsWith("ж") ||
                         lastName.EndsWith("з") || lastName.EndsWith("к") || lastName.EndsWith("л") ||
                         lastName.EndsWith("м") || lastName.EndsWith("п") || lastName.EndsWith("р") ||
                         lastName.EndsWith("с") || lastName.EndsWith("т") || lastName.EndsWith("ф") ||
                         lastName.EndsWith("х") || lastName.EndsWith("ц") || lastName.EndsWith("ч") ||
                         lastName.EndsWith("ш") || lastName.EndsWith("щ"))
                    return lastName;
                else if (lastName == "Топчая")
                    return "Топчей";
                else if (lastName.EndsWith("ия") || lastName.EndsWith("ея") || lastName.EndsWith("ая") ||
                         lastName.EndsWith("оя") || lastName.EndsWith("уя") || lastName.EndsWith("эя") ||
                         lastName.EndsWith("юя") || lastName.EndsWith("яя"))
                    return lastName.Substring(0, lastName.Length - 1) + "е";
                else if (lastName.EndsWith("иа") || lastName.EndsWith("еа") || lastName.EndsWith("аа") ||
                         lastName.EndsWith("оа") || lastName.EndsWith("уа") || lastName.EndsWith("эа") ||
                         lastName.EndsWith("юа") || lastName.EndsWith("яа"))
                    return lastName;
                else
                    return lastName.Substring(0, lastName.Length - 1) + "ой";
            }
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

        private void DeclineWithFallback(Person person)
        {
            Gender gender = DetermineGender(person.MiddleName);

            string lastNameDative = string.IsNullOrEmpty(person.LastName) ? "" : ConvertLastNameToDative(person.LastName, gender);
            string firstNameDative = "";
            string middleNameDative = "";

            if (!IsInitials(person.FirstName) && !string.IsNullOrEmpty(person.FirstName))
            {
                firstNameDative = ConvertFirstNameToDative(person.FirstName, gender);
            }
            else
            {
                firstNameDative = person.FirstName;
            }

            middleNameDative = string.IsNullOrEmpty(person.MiddleName) ? "" : ConvertPatronymicToDative(person.MiddleName, gender);

            // Сохраняем в отдельные поля для fallback
            person.LastNameDativeFallback = lastNameDative;
            person.FirstNameDativeFallback = firstNameDative;
            person.MiddleNameDativeFallback = middleNameDative;
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

        /// <summary>
        /// Склоняет список людей ОБОИМИ способами (библиотека + fallback)
        /// Сохраняет оба результата в Person
        /// </summary>
        public void DeclinePersonsBothWays(List<Person> persons)
        {
            if (persons == null || persons.Count == 0)
                return;

            foreach (var person in persons)
            {
                // Склоняем библиотекой (заполняет LastNameDative, FirstNameDative, MiddleNameDative)
                DeclineWithLibrary(person);

                // Склоняем fallback-методом (заполняет LastNameDativeFallback, FirstNameDativeFallback, MiddleNameDativeFallback)
                DeclineWithFallback(person);

                // Свойства FullNameDativeLibrary и FullNameDativeFallback 
                // автоматически сформируются из заполненных полей при обращении к ним
            }
        }

        #endregion
    }
}