using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;

namespace Rewards_Fast2._0.Models
{
    public enum TextBlockType
    {
        StaticText,
        PersonName
    }

    public class TextBlockData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _text = "Новый блок";

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public TextBlockType Type { get; set; } = TextBlockType.StaticText;

        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        public string FontFamily { get; set; } = "Times New Roman";
        public double FontSize { get; set; } = 24;
        public string FontColorHex { get; set; } = "#000000";
        public bool IsBold { get; set; } = false;
        public bool IsItalic { get; set; } = false;

        // НОВОЕ СВОЙСТВО ДЛЯ ПОДЧЁРКИВАНИЯ
        public bool IsUnderline { get; set; } = false;

        public double PositionX { get; set; } = 100;
        public double PositionY { get; set; } = 100;
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

        private bool _centerAtGeneration = false;
        public bool CenterAtGeneration
        {
            get => _centerAtGeneration;
            set
            {
                if (_centerAtGeneration != value)
                {
                    _centerAtGeneration = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore]
        public double ActualWidth { get; set; } = 400;

        [JsonIgnore]
        public double ActualHeight { get; set; } = 50;

        [JsonIgnore]
        public SolidColorBrush FontColorBrush => new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(FontColorHex));
    }
}