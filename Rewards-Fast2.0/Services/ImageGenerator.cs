using Rewards_Fast2._0.Models;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Rewards_Fast2._0.Services
{
    /// <summary>
    /// Генератор изображений наградных материалов
    /// </summary>
    public class ImageGenerator
    {
        /// <summary>
        /// Генерация одного изображения (работает в любом потоке)
        /// </summary>
        /// <param name="template">Шаблон грамоты</param>
        public void GenerateSingleCertificate(Template template, string personName, string outputPath, string format)
        {
            // Загружаем фон, чтобы узнать его размеры
            BitmapImage? backgroundImage = null;
            double canvasWidth = 800;
            double canvasHeight = 600;

            if (!string.IsNullOrEmpty(template.BackgroundPath) && File.Exists(template.BackgroundPath))
            {
                backgroundImage = ImageHelper.LoadImage(template.BackgroundPath);
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
                        var image = ImageHelper.LoadImage(imageBlock.ImagePath);
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
    }
}
