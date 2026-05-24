using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Rewards_Fast2._0.Models
{
    /// <summary>
    /// Модель блока-изображения (печать, подпись)
    /// </summary>
    public class ImageBlockData
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Путь к файлу изображения</summary>
        public string ImagePath { get; set; } = string.Empty;

        /// <summary>Позиция X на холсте</summary>
        public double PositionX { get; set; } = 100;

        /// <summary>Позиция Y на холсте</summary>
        public double PositionY { get; set; } = 100;

        /// <summary>Ширина изображения</summary>
        public double Width { get; set; } = 100;

        /// <summary>Высота изображения</summary>
        public double Height { get; set; } = 100;

        /// <summary>Видимость</summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>Название (для отображения в списке)</summary>
        public string Name { get; set; } = "Изображение";

        [JsonIgnore]
        public BitmapImage? Source { get; set; }

        /// <summary>Загрузить изображение по пути</summary>
        public void LoadImage()
        {
            if (string.IsNullOrEmpty(ImagePath) || !System.IO.File.Exists(ImagePath))
                return;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(ImagePath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            Source = bitmap;
        }
    }
}
