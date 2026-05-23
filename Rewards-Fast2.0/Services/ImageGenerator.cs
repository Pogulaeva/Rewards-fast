using Rewards_Fast2._0.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static Rewards_Fast2._0.Models.TextBlockData;

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
        public int GenerateCertificates(Template template, List<Person> persons, string outputFolder, bool useDative, string imageFormat = "png", IProgress<(int current, int total)>? progress = null)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));
            if (persons == null || persons.Count == 0)
                throw new ArgumentException("Список людей пуст", nameof(persons));
            if (string.IsNullOrEmpty(outputFolder))
                throw new ArgumentException("Папка сохранения не указана", nameof(outputFolder));

            Directory.CreateDirectory(outputFolder);

            int successCount = 0;
            int total = persons.Count;

            for (int i = 0; i < total; i++)
            {
                var person = persons[i];

                // Сообщаем о прогрессе (потокобезопасно)
                progress?.Report((i + 1, total));

                try
                {
                    string nameToInsert = useDative ? person.FullNameDative : person.FullName;
                    string fileName = GenerateFileName(person, i + 1, imageFormat);
                    string fullPath = System.IO.Path.Combine(outputFolder, fileName);

                    System.Diagnostics.Debug.WriteLine($"Генерация {i + 1}: {person.FullName} -> {fullPath}");

                    GenerateSingleCertificate(template, nameToInsert, fullPath, imageFormat);
                    successCount++;

                    System.Diagnostics.Debug.WriteLine($"Успешно: {successCount}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ОШИБКА для {person.FullName}: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"СТЕК: {ex.StackTrace}");
                }
            }

            return successCount;
        }

        /// <summary>
        /// Генерация одного изображения (работает в любом потоке)
        /// </summary>
        public void GenerateSingleCertificate(Template template, string personName, string outputPath, string format)
        {
            // Загружаем фон, чтобы узнать его размеры
            BitmapImage? backgroundImage = null;
            double canvasWidth = 800;
            double canvasHeight = 600;

            if (!string.IsNullOrEmpty(template.BackgroundPath) && File.Exists(template.BackgroundPath))
            {
                backgroundImage = LoadImage(template.BackgroundPath);
                canvasWidth = backgroundImage.Width;
                canvasHeight = backgroundImage.Height;
            }

            // Используем DrawingVisual вместо Canvas (можно в любом потоке)
            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                // Рисуем белый фон
                drawingContext.DrawRectangle(System.Windows.Media.Brushes.White, null, new Rect(0, 0, canvasWidth, canvasHeight));

                // Рисуем фоновое изображение
                if (backgroundImage != null)
                {
                    drawingContext.DrawImage(backgroundImage, new Rect(0, 0, canvasWidth, canvasHeight));
                }

                // Рисуем текстовые блоки
                foreach (var block in template.TextBlocks)
                {
                    if (!block.IsVisible) continue;

                    string textToShow = block.Text;
                    if (block.Type == TextBlockType.PersonName)
                        textToShow = personName;

                    if (string.IsNullOrEmpty(textToShow)) continue;

                    // Создаём шрифт
                    var typeface = new Typeface(
                        new System.Windows.Media.FontFamily(block.FontFamily),
                        block.IsItalic ? FontStyles.Italic : FontStyles.Normal,
                        block.IsBold ? FontWeights.Bold : FontWeights.Normal,
                        FontStretches.Normal);

                    // Создаём форматированный текст
                    var formattedText = new FormattedText(
                        textToShow,
                        System.Globalization.CultureInfo.CurrentCulture,
                        System.Windows.FlowDirection.LeftToRight,
                        typeface,
                        block.FontSize,
                        block.FontColorBrush,
                        VisualTreeHelper.GetDpi(drawingVisual).PixelsPerDip);

                    // ПОДЧЁРКИВАНИЕ
                    if (block.IsUnderline)
                    {
                        formattedText.SetTextDecorations(TextDecorations.Underline);
                    }

                    // Ограничиваем ширину
                    double maxWidth = canvasWidth * 0.8;
                    if (formattedText.Width > maxWidth)
                    {
                        formattedText.MaxTextWidth = maxWidth;
                    }

                    // Вычисляем позицию
                    double x = block.PositionX;
                    double y = block.PositionY;

                    // Если нужно центрировать
                    if (block.CenterAtGeneration)
                    {
                        x = (canvasWidth - formattedText.Width) / 2;
                    }

                    // Рисуем текст
                    drawingContext.DrawText(formattedText, new System.Windows.Point(x, y));
                }

                // Рисуем изображения (печать, подпись)
                foreach (var imageBlock in template.ImageBlocks)
                {
                    if (!imageBlock.IsVisible || string.IsNullOrEmpty(imageBlock.ImagePath) || !File.Exists(imageBlock.ImagePath))
                        continue;

                    try
                    {
                        var image = LoadImage(imageBlock.ImagePath);
                        drawingContext.DrawImage(image, new Rect(imageBlock.PositionX, imageBlock.PositionY, imageBlock.Width, imageBlock.Height));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка загрузки изображения: {ex.Message}");
                    }
                }
            }

            // Рендерим в Bitmap
            var renderBitmap = new RenderTargetBitmap(
                (int)canvasWidth,
                (int)canvasHeight,
                96,
                96,
                PixelFormats.Pbgra32);

            renderBitmap.Render(drawingVisual);

            // Сохраняем в файл
            BitmapEncoder encoder = format.ToLower() switch
            {
                "jpg" or "jpeg" => new JpegBitmapEncoder { QualityLevel = 90 },
                _ => new PngBitmapEncoder()
            };

            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            using (var fileStream = new FileStream(outputPath, FileMode.Create))
            {
                encoder.Save(fileStream);
            }
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

        /// <summary>
        /// Генерация одного изображения с возможностью отмены
        /// </summary>
        public async Task GenerateSingleCertificateAsync(Template template, string personName, string outputPath, string format, CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Загружаем фон
                BitmapImage? backgroundImage = null;
                double canvasWidth = 800;
                double canvasHeight = 600;

                if (!string.IsNullOrEmpty(template.BackgroundPath) && File.Exists(template.BackgroundPath))
                {
                    backgroundImage = LoadImage(template.BackgroundPath);
                    canvasWidth = backgroundImage.Width;
                    canvasHeight = backgroundImage.Height;
                }

                cancellationToken.ThrowIfCancellationRequested();

                var canvas = new Canvas
                {
                    Width = canvasWidth,
                    Height = canvasHeight,
                    Background = System.Windows.Media.Brushes.White
                };

                // Добавляем фон
                if (backgroundImage != null)
                {
                    var image = new System.Windows.Controls.Image
                    {
                        Source = backgroundImage,
                        Stretch = Stretch.Fill,
                        Width = canvasWidth,
                        Height = canvasHeight
                    };
                    canvas.Children.Add(image);
                }

                foreach (var block in template.TextBlocks)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!block.IsVisible) continue;

                    string textToShow = block.Text;
                    if (block.Type == TextBlockType.PersonName)
                        textToShow = personName;

                    if (string.IsNullOrEmpty(textToShow)) continue;

                    var textBlock = new System.Windows.Controls.TextBlock
                    {
                        Text = textToShow,
                        FontFamily = new System.Windows.Media.FontFamily(block.FontFamily),
                        FontSize = block.FontSize,
                        FontWeight = block.IsBold ? FontWeights.Bold : FontWeights.Normal,
                        FontStyle = block.IsItalic ? FontStyles.Italic : FontStyles.Normal,
                        Foreground = block.FontColorBrush,
                        TextWrapping = TextWrapping.Wrap
                    };

                    if (block.CenterAtGeneration)
                    {
                        textBlock.TextAlignment = System.Windows.TextAlignment.Center;
                        textBlock.Width = canvasWidth * 0.8;
                        Canvas.SetLeft(textBlock, (canvasWidth - textBlock.Width) / 2);
                    }
                    else
                    {
                        textBlock.TextAlignment = System.Windows.TextAlignment.Left;
                        Canvas.SetLeft(textBlock, block.PositionX);
                    }
                    Canvas.SetTop(textBlock, block.PositionY);
                    canvas.Children.Add(textBlock);
                }

                foreach (var imageBlock in template.ImageBlocks)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!imageBlock.IsVisible || string.IsNullOrEmpty(imageBlock.ImagePath) || !File.Exists(imageBlock.ImagePath))
                        continue;

                    try
                    {
                        var image = new System.Windows.Controls.Image
                        {
                            Source = LoadImage(imageBlock.ImagePath),
                            Width = imageBlock.Width,
                            Height = imageBlock.Height,
                            Stretch = Stretch.Fill
                        };

                        Canvas.SetLeft(image, imageBlock.PositionX);
                        Canvas.SetTop(image, imageBlock.PositionY);
                        canvas.Children.Add(image);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка загрузки изображения: {ex.Message}");
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                // Рендерим на UI потоке
                var renderComplete = new System.Threading.ManualResetEventSlim(false);
                Exception? renderException = null;

                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        canvas.Measure(new System.Windows.Size(canvas.Width, canvas.Height));
                        canvas.Arrange(new Rect(0, 0, canvas.Width, canvas.Height));
                        canvas.UpdateLayout();

                        var renderBitmap = new RenderTargetBitmap(
                            (int)canvas.Width,
                            (int)canvas.Height,
                            96d,
                            96d,
                            PixelFormats.Pbgra32);

                        renderBitmap.Render(canvas);

                        BitmapEncoder encoder = format.ToLower() switch
                        {
                            "jpg" or "jpeg" => new JpegBitmapEncoder { QualityLevel = 90 },
                            _ => new PngBitmapEncoder()
                        };

                        encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                        using (var fileStream = new FileStream(outputPath, FileMode.Create))
                        {
                            encoder.Save(fileStream);
                        }
                    }
                    catch (Exception ex)
                    {
                        renderException = ex;
                    }
                    finally
                    {
                        renderComplete.Set();
                    }
                });

                renderComplete.Wait();

                if (renderException != null)
                    throw renderException;

            }, cancellationToken);
        }
    }
}
