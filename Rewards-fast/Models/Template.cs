using System;
using System.Collections.Generic;
using System.Text;

namespace Rewards_Fast2._0.Models
{
    /// <summary>
    /// Модель шаблона грамоты (изображение + текстовые блоки)
    /// </summary>
    public class Template
    {
        /// <summary>Название шаблона (для отображения)</summary>
        public string Name { get; set; } = "Новый шаблон";

        /// <summary>Путь к файлу фона (может быть относительным или абсолютным)</summary>
        public string BackgroundPath { get; set; } = string.Empty;

        /// <summary>Встроенное изображение фона в base64 (опционально, если нужно хранить внутри шаблона)</summary>
        public string BackgroundBase64 { get; set; } = string.Empty;

        /// <summary>Список текстовых блоков</summary>
        public List<TextBlockData> TextBlocks { get; set; } = new List<TextBlockData>();

        /// <summary>Дата создания шаблона</summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>Проверяет, есть ли в шаблоне блок типа PersonName</summary>
        public bool HasPersonNameBlock => TextBlocks.Exists(b => b.Type == TextBlockType.PersonName);

        public List<ImageBlockData> ImageBlocks { get; set; } = new List<ImageBlockData>();

        /// <summary>Возвращает блок PersonName (если есть), иначе null</summary>
        public TextBlockData? GetPersonNameBlock()
        {
            return TextBlocks.Find(b => b.Type == TextBlockType.PersonName);
        }

        /// <summary>Создаёт копию шаблона (глубокое копирование)</summary>
        public Template Clone()
        {
            return new Template
            {
                Name = this.Name,
                BackgroundPath = this.BackgroundPath,
                BackgroundBase64 = this.BackgroundBase64,
                TextBlocks = new List<TextBlockData>(this.TextBlocks),
                CreatedDate = this.CreatedDate
            };
        }
    }
}
