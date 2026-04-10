using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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
    public class TextBlockData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _text = "Новый блок";

        /// <summary>Уникальный идентификатор блока</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Тип блока (обычный текст или ФИО)</summary>
        public TextBlockType Type { get; set; } = TextBlockType.StaticText;

        /// <summary>Текст блока (для PersonName этот текст не используется при генерации)</summary>
        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayName)); // Уведомляем об изменении DisplayName
                }
            }
        }

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

        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(Text))
                    return "Новый блок";

                var words = Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (words.Length >= 3)
                    return $"{words[0]} {words[1]} {words[2]}";

                if (words.Length >= 2)
                    return $"{words[0]} {words[1]}";

                return Text.Length > 20 ? Text.Substring(0, 20) + "..." : Text;
            }
        }

        // Вспомогательное свойство для WPF (цвет как Brush)
        [JsonIgnore]
        public SolidColorBrush FontColorBrush => new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(FontColorHex));
    }
}