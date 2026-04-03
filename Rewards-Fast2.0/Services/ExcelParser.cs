using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using Rewards_Fast2._0.Models;

namespace Rewards_Fast2._0.Services
{
    /// <summary>
    /// Парсер Excel/CSV файлов с ФИО
    /// Поддерживает форматы:
    /// - одна колонка (ФИО целиком, ФИ)
    /// - две колонки (Фамилия, Имя)
    /// - три колонки (Фамилия, Имя, Отчество)
    /// - инициалы в именах/отчествах (не склоняются)
    /// </summary>
    public class ExcelParser
    {
        // Статический конструктор для регистрации кодировок
        static ExcelParser()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        /// <summary>
        /// Разбор файла и создание списка Person
        /// </summary>
        public List<Person> Parse(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Файл не найден: {filePath}");

            string extension = Path.GetExtension(filePath).ToLower();

            if (extension == ".xlsx" || extension == ".xls")
            {
                // Для настоящих Excel пока заглушка, пока используем CSV
                throw new NotSupportedException(
                    "Для работы с .xlsx файлами требуется библиотека EPPlus. " +
                    "Пока используйте .csv или .txt файлы. ");
            }
            else if (extension == ".csv" || extension == ".txt")
            {
                return ParseCsv(filePath);
            }
            else
            {
                throw new NotSupportedException($"Формат файла {extension} не поддерживается. Используйте .csv или .txt");
            }
        }

        /// <summary>
        /// Парсинг CSV/TXT файла
        /// </summary>
        private List<Person> ParseCsv(string filePath)
        {
            List<Person> persons = new List<Person>();

            // Пробуем разные кодировки
            string[]? lines = null;
            Encoding[] encodings = { Encoding.UTF8, Encoding.GetEncoding(1251), Encoding.Default };

            foreach (var encoding in encodings)
            {
                try
                {
                    lines = File.ReadAllLines(filePath, encoding);
                    if (lines.Length > 0)
                        break;
                }
                catch { }
            }

            if (lines == null || lines.Length == 0)
                return persons;

            // Определяем разделитель
            char delimiter = DetectDelimiter(lines);

            // Определяем, есть ли заголовки
            bool hasHeader = DetectHasHeader(lines, delimiter);

            int startRow = hasHeader ? 1 : 0;

            for (int i = startRow; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] parts = SplitLine(line, delimiter);
                Person? person = ParsePersonFromParts(parts);

                if (person != null && (!string.IsNullOrEmpty(person.LastName) || !string.IsNullOrEmpty(person.FirstName)))
                {
                    persons.Add(person);
                }
            }

            return persons;
        }

        /// <summary>
        /// Определение разделителя
        /// </summary>
        private char DetectDelimiter(string[] lines)
        {
            char[] possibleDelimiters = { ',', ';', '\t', '|' };

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                foreach (char delimiter in possibleDelimiters)
                {
                    if (line.Contains(delimiter))
                        return delimiter;
                }
            }

            // Если разделитель не найден, считаем что данные разделены пробелами
            return ' ';
        }

        /// <summary>
        /// Определение наличия заголовков
        /// </summary>
        private bool DetectHasHeader(string[] lines, char delimiter)
        {
            if (lines.Length < 2)
                return false;

            string firstLine = lines[0];
            string secondLine = lines[1];

            string[] firstParts = SplitLine(firstLine, delimiter);
            string[] secondParts = SplitLine(secondLine, delimiter);

            // Ключевые слова, по которым определяем заголовок
            string[] headerKeywords = {
                "фамилия", "имя", "отчество", "фио", "фи",
                "fio", "last", "first", "middle", "name"
            };

            bool firstLineLooksLikeHeader = false;
            foreach (string part in firstParts)
            {
                string lowerPart = part.ToLower().Trim('"', '\'', ' ');
                if (headerKeywords.Any(kw => lowerPart.Contains(kw)))
                {
                    firstLineLooksLikeHeader = true;
                    break;
                }
            }

            // Если первая строка не похожа на заголовок, но вторая похожа на ФИО
            bool secondLineLooksLikeFio = secondParts.Length >= 2 && secondParts.Length <= 3;
            bool firstLineHasLetters = firstParts.Any(p => p.Length > 0 && char.IsLetter(p[0]));

            return firstLineLooksLikeHeader || (firstLineHasLetters && secondLineLooksLikeFio);
        }

        /// <summary>
        /// Разделение строки на части
        /// </summary>
        private string[] SplitLine(string line, char delimiter)
        {
            if (delimiter == ' ')
            {
                return line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                return line.Split(delimiter);
            }
        }

        /// <summary>
        /// Создание Person из массива частей
        /// </summary>
        private Person? ParsePersonFromParts(string[] parts)
        {
            if (parts.Length == 0)
                return null;

            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = parts[i].Trim().Trim('"', '\'', ' ');
            }

            Person person = new Person();

            if (parts.Length == 3)
            {
                person.LastName = parts[0];
                person.FirstName = parts[1];
                person.MiddleName = parts[2];
            }
            else if (parts.Length == 2)
            {
                person.LastName = parts[0];
                person.FirstName = parts[1];
                person.MiddleName = "";
            }
            else if (parts.Length == 1)
            {
                string fullName = parts[0];
                string[] nameParts = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (nameParts.Length >= 3)
                {
                    person.LastName = nameParts[0];
                    person.FirstName = nameParts[1];
                    person.MiddleName = nameParts[2];
                }
                else if (nameParts.Length == 2)
                {
                    person.LastName = nameParts[0];

                    // Если вторая часть — инициалы, сохраняем как есть
                    if (IsInitials(nameParts[1]))
                    {
                        person.FirstName = nameParts[1];
                    }
                    else
                    {
                        person.FirstName = nameParts[1];
                    }
                    person.MiddleName = "";
                }
                else
                {
                    return null;
                }
            }
            else
            {
                person.LastName = parts[0];
                person.FirstName = parts[1];
                person.MiddleName = parts.Length > 2 ? parts[2] : "";
            }

            return person;
        }


        /// <summary>
        /// Проверяет, является ли строка инициалами (например, "И.И." или "И.")
        /// </summary>
        private bool IsInitials(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            // Инициалы содержат точки и обычно короткие
            return input.Contains(".") && input.Length <= 5;
        }
    }
}
