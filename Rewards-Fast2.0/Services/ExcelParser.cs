using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using Rewards_Fast2._0.Models;
using OfficeOpenXml;

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
            ExcelPackage.License.SetNonCommercialPersonal("RewardsFast");
        }

        /// <summary>
        /// Разбор файла и создание списка Person
        /// </summary>
        public List<Person> Parse(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Файл не найден: {filePath}");

            string extension = System.IO.Path.GetExtension(filePath).ToLower();

            if (extension == ".xlsx" || extension == ".xls")
            {
                return ParseExcelFile(filePath);  // ← вызов нового метода
            }
            else if (extension == ".csv" || extension == ".txt")
            {
                return ParseCsv(filePath);
            }
            else
            {
                throw new NotSupportedException($"Формат файла {extension} не поддерживается. Используйте .xlsx, .xls, .csv или .txt");
            }
        }

        /// <summary>
        /// Парсинг CSV/TXT файла
        /// </summary>
        private List<Person> ParseCsv(string filePath)
        {
            List<Person> persons = new List<Person>();

            // Пробуем разные кодировки (без 1251, которая вызывает ошибку)
            string[]? lines = null;

            // Сначала пробуем UTF-8
            try
            {
                lines = File.ReadAllLines(filePath, Encoding.UTF8);
                System.Diagnostics.Debug.WriteLine($"UTF-8: прочитано {lines?.Length ?? 0} строк");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UTF-8 ошибка: {ex.Message}");
            }

            // Если UTF-8 не дал результата, пробуем системную кодировку (на русской Windows это 1251)
            if (lines == null || lines.Length == 0)
            {
                try
                {
                    lines = File.ReadAllLines(filePath, Encoding.Default);
                    System.Diagnostics.Debug.WriteLine($"Default: прочитано {lines?.Length ?? 0} строк");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Default ошибка: {ex.Message}");
                }
            }

            // Если всё ещё нет строк, пробуем без указания кодировки
            if (lines == null || lines.Length == 0)
            {
                try
                {
                    lines = File.ReadAllLines(filePath);
                    System.Diagnostics.Debug.WriteLine($"Без кодировки: прочитано {lines?.Length ?? 0} строк");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Без кодировки ошибка: {ex.Message}");
                    return persons;
                }
            }

            if (lines == null || lines.Length == 0)
            {
                System.Diagnostics.Debug.WriteLine("Не удалось прочитать файл");
                return persons;
            }

            // Определяем разделитель
            char delimiter = DetectDelimiter(lines);
            System.Diagnostics.Debug.WriteLine($"Разделитель: '{delimiter}'");

            // Определяем, есть ли заголовки
            bool hasHeader = DetectHasHeader(lines, delimiter);
            System.Diagnostics.Debug.WriteLine($"HasHeader: {hasHeader}");

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

        private List<Person> ParseExcelFile(string filePath)
        {
            var persons = new List<Person>();

            try
            {
                // Проверяем, что это действительно Excel файл
                string extension = System.IO.Path.GetExtension(filePath).ToLower();
                if (extension != ".xlsx" && extension != ".xls")
                {
                    throw new NotSupportedException($"Файл {extension} не является Excel файлом");
                }

                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    if (worksheet.Dimension == null)
                    {
                        System.Diagnostics.Debug.WriteLine("Excel файл пуст");
                        return persons;
                    }

                    int rowCount = worksheet.Dimension.Rows;
                    System.Diagnostics.Debug.WriteLine($"Excel: {rowCount} строк");

                    // Определяем структуру по первой строке
                    bool hasHeader = false;
                    bool threeColumns = false;

                    if (rowCount > 0)
                    {
                        var cell1 = worksheet.Cells[1, 1]?.Value?.ToString() ?? "";
                        var cell2 = worksheet.Cells[1, 2]?.Value?.ToString() ?? "";
                        var cell3 = worksheet.Cells[1, 3]?.Value?.ToString() ?? "";

                        bool firstRowLooksLikeHeader =
                            cell1.Contains("фамил", StringComparison.OrdinalIgnoreCase) ||
                            cell1.Contains("Фамилия") ||
                            cell2.Contains("имя", StringComparison.OrdinalIgnoreCase) ||
                            cell2.Contains("Имя") ||
                            cell3.Contains("отчеств", StringComparison.OrdinalIgnoreCase) ||
                            cell3.Contains("Отчество");

                        threeColumns = !string.IsNullOrEmpty(cell1) &&
                                       !string.IsNullOrEmpty(cell2) &&
                                       !string.IsNullOrEmpty(cell3);

                        hasHeader = firstRowLooksLikeHeader;
                    }

                    int startRow = hasHeader ? 2 : 1;

                    for (int row = startRow; row <= rowCount; row++)
                    {
                        string lastName = (worksheet.Cells[row, 1]?.Value?.ToString() ?? "").Trim();
                        string firstName = (worksheet.Cells[row, 2]?.Value?.ToString() ?? "").Trim();
                        string middleName = threeColumns ? (worksheet.Cells[row, 3]?.Value?.ToString() ?? "").Trim() : "";

                        if (!string.IsNullOrEmpty(lastName) && !string.IsNullOrEmpty(firstName))
                        {
                            persons.Add(new Person
                            {
                                LastName = lastName,
                                FirstName = firstName,
                                MiddleName = middleName ?? ""
                            });
                        }
                        else if (!string.IsNullOrEmpty(lastName) && string.IsNullOrEmpty(firstName))
                        {
                            var parts = lastName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2)
                            {
                                persons.Add(new Person
                                {
                                    LastName = parts[0],
                                    FirstName = parts[1],
                                    MiddleName = parts.Length >= 3 ? parts[2] : ""
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка чтения Excel: {ex.Message}");
                throw;
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
        /// Определение наличия заголовков (более точная версия)
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
        "fio", "last", "first", "middle", "name",
        "фамилию", "имя", "отчество", // возможные варианты
        "lastname", "firstname", "middlename"
    };

            // Проверяем, похожа ли первая строка на заголовок
            bool firstLineLooksLikeHeader = false;
            int headerKeywordCount = 0;

            foreach (string part in firstParts)
            {
                string lowerPart = part.ToLower().Trim('"', '\'', ' ');
                if (headerKeywords.Any(kw => lowerPart.Contains(kw)))
                {
                    headerKeywordCount++;
                    if (headerKeywordCount >= 2) // Хотя бы два слова похожи на заголовки
                    {
                        firstLineLooksLikeHeader = true;
                        break;
                    }
                }
            }

            // Если первая строка явно похожа на заголовок
            if (firstLineLooksLikeHeader)
                return true;

            // Проверяем, что вторая строка похожа на ФИО (содержит буквы и пробелы)
            bool secondLineLooksLikeFio = false;
            if (secondParts.Length >= 2 && secondParts.Length <= 3)
            {
                // Проверяем, что все части содержат буквы (русские или латинские)
                bool allPartsHaveLetters = secondParts.All(part =>
                    !string.IsNullOrWhiteSpace(part) &&
                    part.Any(c => char.IsLetter(c)));

                if (allPartsHaveLetters)
                {
                    // Дополнительно: проверяем, что нет явных признаков заголовка во второй строке
                    bool secondLineHasHeaderKeywords = secondParts.Any(part =>
                        headerKeywords.Any(kw => part.ToLower().Contains(kw)));

                    if (!secondLineHasHeaderKeywords)
                        secondLineLooksLikeFio = true;
                }
            }

            // Если первая строка НЕ похожа на заголовок, но вторая похожа на ФИО
            bool firstLineHasLetters = firstParts.Any(p => p.Length > 0 && p.Any(c => char.IsLetter(c)));

            // Если первая строка имеет нормальную длину и второе поле похоже на имя
            if (firstLineHasLetters && secondLineLooksLikeFio)
                return false; // Первая строка - это данные, а не заголовок

            // Если первая строка содержит очень короткие поля (возможно инициалы или буквы) 
            // и вторая тоже, то скорее всего заголовков нет
            if (firstParts.Length == secondParts.Length &&
                firstParts.Length >= 2 && firstParts.Length <= 3)
            {
                bool firstHasShortParts = firstParts.Any(p => p.Length <= 2);
                bool secondHasLongParts = secondParts.All(p => p.Length > 2);

                // Если в первой строке есть короткие части (инициалы), а во второй нет - значит первая строка это данные
                if (firstHasShortParts && secondHasLongParts)
                    return false;
            }

            // По умолчанию считаем, что заголовков нет
            return false;
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
