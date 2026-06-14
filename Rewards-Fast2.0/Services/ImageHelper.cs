using System;
using System.Windows.Media.Imaging;


namespace Rewards_Fast2._0.Services
{
    public static class ImageHelper
    {
        /// <summary>
        /// Загружает изображение из файла
        /// </summary>
        /// <param name="path">Путь к файлу</param>
        /// <param name="decodePixelWidth">Если указано, уменьшает изображение до указанной ширины (сохраняя пропорции)</param>
        /// <returns>Загруженное и замороженное изображение</returns>
        public static BitmapImage LoadImage(string path, int? decodePixelWidth = null)
        {
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
                throw new System.IO.FileNotFoundException($"Файл не найден: {path}");

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;

            if (decodePixelWidth.HasValue && decodePixelWidth.Value > 0)
                bitmap.DecodePixelWidth = decodePixelWidth.Value;

            bitmap.EndInit();
            bitmap.Freeze(); // Замораживаем для потокобезопасности и оптимизации памяти
            return bitmap;
        }
    }
}
