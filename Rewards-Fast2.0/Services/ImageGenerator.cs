using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Rewards_Fast2._0.Models;

namespace Rewards_Fast2._0.Services
{
    /// <summary>
    /// Генератор изображений наградных материалов
    /// </summary>
    public class ImageGenerator
    {
        private readonly NameDeclensionService _declensionService;

        public ImageGenerator()
        {
            _declensionService = new NameDeclensionService();
        }

        /// <summary>
        /// Генерация пачки изображений
        /// </summary>
        /// <param name="template">Шаблон грамоты</param>
        /// <param name="persons">Список людей</param>
        /// <param name="outputFolder">Папка для сохранения</param>
        /// <param name="useDative">Использовать дательный падеж</param>
        /// <param name="imageFormat">Формат изображения (png, jpg)</param>
        /// <param name="onProgress">Прогресс (текущий индекс, общее количество)</param>
        /// <returns>Количество успешно сгенерированных файлов</returns>
        public int GenerateCertificates(
            Template template,
            List<Person> persons,
            string outputFolder,
            bool useDative,
            string imageFormat = "png",
            Action<int, int>? onProgress = null)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));
            if (persons == null || persons.Count == 0)
                throw new ArgumentException("Список людей пуст", nameof(persons));
            if (string.IsNullOrEmpty(outputFolder))
                throw new ArgumentException("Папка сохранения не указана", nameof(outputFolder));

            // Создаём папку, если её нет
            Directory.CreateDirectory(outputFolder);

            int successCount = 0;
            int total = persons.Count;

            for (int i = 0; i < total; i++)
            {
                var person = persons[i];
                onProgress?.Invoke(i + 1, total);

                try
                {
                    string nameToInsert = useDative ? person.FullNameDative : person.FullName;
                    string fileName = GenerateFileName(person, i + 1, imageFormat);
                    string fullPath = Path.Combine(outputFolder, fileName);

                    GenerateSingleCertificate(template, nameToInsert, fullPath, imageFormat);
                    successCount++;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка генерации для {person.FullName}: {ex.Message}");
                }
            }

            return successCount;
        }

        /// <summary>
        /// Генерация одного изображения
        /// </summary>
        private void GenerateSingleCertificate(Template template, string personName, string outputPath, string format)
        {
            // Создаём Canvas для рендеринга
            var canvas = new Canvas
            {
                Width = 800,   // Базовая ширина
                Height = 600,  // Базовая высота
                Background = System.Windows.Media.Brushes.White
            };

            // Добавляем фон
            if (!string.IsNullOrEmpty(template.BackgroundPath) && File.Exists(template.BackgroundPath))
            {
                var image = new System.Windows.Controls.Image
                {
                    Source = LoadImage(template.BackgroundPath),
                    Stretch = Stretch.Fill,
                    Width = canvas.Width,
                    Height = canvas.Height
                };
                canvas.Children.Add(image);
            }

            // Добавляем все текстовые блоки
            foreach (var block in template.TextBlocks)
            {
                if (!block.IsVisible)
                    continue;

                string textToShow = block.Text;

                // Если это блок с ФИО, подставляем имя
                if (block.Type == TextBlockType.PersonName)
                {
                    textToShow = personName;
                }

                if (string.IsNullOrEmpty(textToShow))
                    continue;

                var textBlock = new System.Windows.Controls.TextBlock
                {
                    Text = textToShow,
                    FontFamily = new System.Windows.Media.FontFamily(block.FontFamily),
                    FontSize = block.FontSize,
                    FontWeight = block.IsBold ? FontWeights.Bold : FontWeights.Normal,
                    FontStyle = block.IsItalic ? FontStyles.Italic : FontStyles.Normal,
                    Foreground = block.FontColorBrush,
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top,
                    Width = 400,  // Примерная ширина
                    TextWrapping = TextWrapping.Wrap
                };

                Canvas.SetLeft(textBlock, block.PositionX);
                Canvas.SetTop(textBlock, block.PositionY);
                canvas.Children.Add(textBlock);
            }

            // Рендерим в изображение
            RenderToImage(canvas, outputPath, format);
        }

        /// <summary>
        /// Загрузка изображения из файла
        /// </summary>
        private BitmapImage LoadImage(string path)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            return bitmap;
        }

        /// <summary>
        /// Рендеринг Canvas в файл
        /// </summary>
        private void RenderToImage(Canvas canvas, string outputPath, string format)
        {
            // Принудительно обновляем размеры
            canvas.Measure(new System.Windows.Size(canvas.Width, canvas.Height));
            canvas.Arrange(new Rect(0, 0, canvas.Width, canvas.Height));
            canvas.UpdateLayout();

            // Рендерим
            var renderBitmap = new RenderTargetBitmap(
                (int)canvas.Width,
                (int)canvas.Height,
                96d,
                96d,
                PixelFormats.Pbgra32);

            renderBitmap.Render(canvas);

            // Кодируем
            BitmapEncoder encoder = format.ToLower() switch
            {
                "jpg" or "jpeg" => new JpegBitmapEncoder { QualityLevel = 90 },
                _ => new PngBitmapEncoder()
            };

            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            // Сохраняем
            using (var fileStream = new FileStream(outputPath, FileMode.Create))
            {
                encoder.Save(fileStream);
            }
        }

        /// <summary>
        /// Генерация имени файла
        /// </summary>
        private string GenerateFileName(Person person, int index, string format)
        {
            // Очищаем ФИО от недопустимых символов
            string safeName = person.FullName
                .Replace(' ', '_')
                .Replace('.', '_')
                .Replace(',', '_')
                .Replace('(', '_')
                .Replace(')', '_');

            // Ограничиваем длину
            if (safeName.Length > 50)
                safeName = safeName.Substring(0, 50);

            return $"{index:0000}_{safeName}.{format.ToLower()}";
        }
    }
}
