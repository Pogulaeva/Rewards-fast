using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Rewards_Fast2._0.Models;

namespace Rewards_Fast2._0.Services
{
    public class TemplateService
    {
        public async Task SaveTemplateAsync(Template template, string filePath)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(template, options);
            await File.WriteAllTextAsync(filePath, json);
        }

        public async Task<Template?> LoadTemplateAsync(string filePath)
        {
            string json = await File.ReadAllTextAsync(filePath);
            var template = JsonSerializer.Deserialize<Template>(json);

            // После загрузки восстанавливаем источники изображений
            if (template != null && template.ImageBlocks != null)
            {
                foreach (var image in template.ImageBlocks)
                {
                    image.LoadImage(); // Перезагружаем BitmapImage из пути
                }
            }

            return template;
        }
    }
}
