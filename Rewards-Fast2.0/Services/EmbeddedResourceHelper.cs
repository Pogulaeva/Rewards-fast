using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace Rewards_Fast2._0.Services
{
    public static class EmbeddedResourceHelper
    {
        /// <summary>
        /// Получить все встроенные фоны из Resources/Backgrounds
        /// </summary>
        public static List<EmbeddedBackground> GetEmbeddedBackgrounds()
        {
            var backgrounds = new List<EmbeddedBackground>();
            var assembly = Assembly.GetExecutingAssembly();

            // Ищем все ресурсы с путём, содержащим "Resources.Backgrounds"
            var resourceNames = assembly.GetManifestResourceNames()
                .Where(name => name.Contains("Resources.Backgrounds") &&
                              (name.EndsWith(".png") || name.EndsWith(".jpg") || name.EndsWith(".jpeg") || name.EndsWith(".bmp")))
                .ToList();

            foreach (var resourceName in resourceNames)
            {
                try
                {
                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream != null)
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.StreamSource = stream;
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            bitmap.Freeze(); // Для потокобезопасности

                            // Извлекаем простое имя файла из полного пути ресурса
                            var fileName = resourceName.Split('.').Reverse().Skip(1).First() + "." + resourceName.Split('.').Last();

                            backgrounds.Add(new EmbeddedBackground
                            {
                                Name = fileName,
                                Image = bitmap,
                                ResourcePath = resourceName
                            });
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка загрузки ресурса {resourceName}: {ex.Message}");
                }
            }

            return backgrounds;
        }
    }

    public class EmbeddedBackground
    {
        public string Name { get; set; } = string.Empty;
        public BitmapImage Image { get; set; }
        public string ResourcePath { get; set; } = string.Empty;
    }
}