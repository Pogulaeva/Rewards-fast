using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace Rewards_Fast2._0.Models
{
    /// <summary>
    /// Тип текстового блока
    /// </summary>
    public enum TextBlockType
    {
        /// <summary>Обычный текст (редактируется пользователем)</summary>
        StaticText,
        /// <summary>Специальный блок для ФИО (заполняется из Excel)</summary>
        PersonName
    }
    /// <summary>
    /// Модель текстового блока на шаблоне наградного материала
    /// </summary>
    public class TextBlockData
    {
        /// <summary>Уникальный идентификатор блока</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Тип блока (обычный текст или ФИО)</summary>
        public TextBlockType Type { get; set; } = TextBlockType.StaticText;

        /// <summary>Текст блока (для PersonName этот текст не используется при генерации)</summary>
        public string Text { get; set; } = "Новый блок";

        /// <summary>Название шрифта</summary>
        public string FontFamily { get; set; } = "Times New Roman";

        /// <summary>Размер шрифта</summary>
        public double FontSize { get; set; } = 24;

        /// <summary>Цвет текста в HEX</summary>
        public string FontColorHex { get; set; } = "#000000";

        /// <summary>Жирный</summary>
        public bool IsBold { get; set; } = false;

        /// <summary>Курсив</summary>
        public bool IsItalic { get; set; } = false;

        /// <summary>Позиция X на холсте</summary>
        public double PositionX { get; set; } = 100;

        /// <summary>Позиция Y на холсте</summary>
        public double PositionY { get; set; } = 100;

        /// <summary>Виден ли блок на превью</summary>
        public bool IsVisible { get; set; } = true;

        // Вспомогательное свойство для WPF (цвет как Brush)
        [JsonIgnore]
        public SolidColorBrush FontColorBrush => new SolidColorBrush((Color)ColorConverter.ConvertFromString(FontColorHex));
    }
}
