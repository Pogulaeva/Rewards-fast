using Microsoft.Win32;
using Rewards_Fast2._0;
using Rewards_Fast2._0.Models;
using Rewards_Fast2._0.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Rewards_Fast2._0
{
    public partial class MainWindow : Window
    {
        private readonly ExcelParser _excelParser = new ExcelParser();
        private readonly NameDeclensionService _declensionService = new NameDeclensionService();
        private readonly ImageGenerator _imageGenerator = new ImageGenerator();
        private readonly TemplateService _templateService = new TemplateService();

        private Template _currentTemplate = new Template();
        private List<Person> _persons = new List<Person>();
        private bool _useDative = false;
        private TextBlockData? _selectedBlock;
        private bool _hasGenerated = false;
        private bool _isUpdatingProperties = false;

        private static readonly string AppDataFolder =
            System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RewardsFast");
        private static readonly string BackgroundsFolder =
            System.IO.Path.Combine(AppDataFolder, "Фоны");
        private static readonly string TemplatesFolder =
            System.IO.Path.Combine(AppDataFolder, "Мои шаблоны");
        private static readonly string DefaultOutputFolder =
            System.IO.Path.Combine(AppDataFolder, "Сгенерированные награды");

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) =>
            {
                // Теперь все элементы точно созданы
                _useDative = DativeCase.IsChecked == true;
                RefreshPersonsGrid();
            };
            OpenFolderButton.IsEnabled = false;
            InitializeAppFolders();
            LoadFonts();
            LoadBackgroundLibrary();
            SetupDefaultTemplate();
            OutputFolderBox.Text = DefaultOutputFolder;
        }

        private void InitializeAppFolders()
        {
            try
            {
                if (!Directory.Exists(AppDataFolder)) Directory.CreateDirectory(AppDataFolder);
                if (!Directory.Exists(BackgroundsFolder)) Directory.CreateDirectory(BackgroundsFolder);
                if (!Directory.Exists(TemplatesFolder)) Directory.CreateDirectory(TemplatesFolder);
                if (!Directory.Exists(DefaultOutputFolder)) Directory.CreateDirectory(DefaultOutputFolder);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка при создании папок: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadFonts()
        {
            var fonts = System.Drawing.FontFamily.Families.Select(f => f.Name).OrderBy(f => f);
            FontFamilyBox.ItemsSource = fonts;
            FontFamilyBox.SelectedItem = "Times New Roman";
        }

        private void LoadBackgroundLibrary()
        {
            var backgrounds = new List<BackgroundItem>();
            if (!Directory.Exists(BackgroundsFolder)) return;

            foreach (string file in Directory.GetFiles(BackgroundsFolder))
            {
                string ext = System.IO.Path.GetExtension(file).ToLower();
                if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp")
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(file, UriKind.Absolute);
                        bitmap.DecodePixelWidth = 80;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        backgrounds.Add(new BackgroundItem { FilePath = file, Thumbnail = bitmap });
                    }
                    catch { }
                }
            }
            BackgroundLibraryItems.ItemsSource = backgrounds;
        }

        private void SetupDefaultTemplate()
        {
            _currentTemplate = new Template();
            _currentTemplate.TextBlocks.Add(new TextBlockData
            {
                Id = Guid.NewGuid().ToString(),
                Type = TextBlockType.PersonName,
                Text = "Иванов Иван Иванович",
                FontFamily = "Times New Roman",
                FontSize = 48,
                FontColorHex = "#000000",
                IsBold = true,
                PositionX = 200,
                PositionY = 250
            });
            RefreshBlocksList();
            RefreshPreview();
        }

        private void RefreshBlocksList()
        {
            BlocksListBox.ItemsSource = null;
            BlocksListBox.ItemsSource = _currentTemplate.TextBlocks;
        }

        private void RefreshPreview()
        {
            PreviewCanvas.Children.Clear();

            if (!string.IsNullOrEmpty(_currentTemplate.BackgroundPath) && File.Exists(_currentTemplate.BackgroundPath))
            {
                var image = new System.Windows.Controls.Image
                {
                    Source = LoadBitmapImage(_currentTemplate.BackgroundPath),
                    Width = PreviewCanvas.Width,
                    Height = PreviewCanvas.Height
                };
                PreviewCanvas.Children.Add(image);
            }

            foreach (var block in _currentTemplate.TextBlocks)
            {
                if (!block.IsVisible) continue;
                var textBlock = new System.Windows.Controls.TextBlock
                {
                    Text = block.Text,
                    FontFamily = new System.Windows.Media.FontFamily(block.FontFamily),
                    FontSize = block.FontSize,
                    FontWeight = block.IsBold ? FontWeights.Bold : FontWeights.Normal,
                    FontStyle = block.IsItalic ? FontStyles.Italic : FontStyles.Normal,
                    Foreground = block.FontColorBrush,
                    Width = 400,
                    TextWrapping = TextWrapping.Wrap
                };
                Canvas.SetLeft(textBlock, block.PositionX);
                Canvas.SetTop(textBlock, block.PositionY);
                PreviewCanvas.Children.Add(textBlock);
            }
        }

        private BitmapImage LoadBitmapImage(string path)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            return bitmap;
        }

        private void SetBackground(string imagePath)
        {
            _currentTemplate.BackgroundPath = imagePath;
            BackgroundThumbnail.Source = LoadBitmapImage(imagePath);
            RefreshPreview();
        }

        private void BackgroundThumbnail_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is BackgroundItem item)
                SetBackground(item.FilePath);
        }

        private void UniversalDragOver(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
                e.Effects = System.Windows.DragDropEffects.Copy;
            else
                e.Effects = System.Windows.DragDropEffects.None;
        }

        private async void UniversalDrop(object sender, System.Windows.DragEventArgs e)
        {
            var files = e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[];
            if (files == null || files.Length == 0) return;

            string file = files[0];
            string ext = System.IO.Path.GetExtension(file).ToLower();

            if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp")
            {
                SetBackground(file);
            }
            else if (ext == ".json")
            {
                await LoadTemplateFromFile(file);
            }
            else
            {
                System.Windows.MessageBox.Show("Поддерживаются форматы: PNG, JPG, JSON", "Неверный формат",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ExcelDragOver(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)) e.Effects = System.Windows.DragDropEffects.Copy;
            else e.Effects = System.Windows.DragDropEffects.None;
        }

        private async void ExcelDrop(object sender, System.Windows.DragEventArgs e)
        {
            var files = e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[];
            if (files != null && files.Length > 0)
                await LoadExcelFile(files[0]);
        }

        private async void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Выберите файл",
                Filter = "Поддерживаемые файлы (*.png;*.jpg;*.jpeg;*.bmp;*.json)|*.png;*.jpg;*.jpeg;*.bmp;*.json|" +
                         "Изображения (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|" +
                         "Шаблоны (*.json)|*.json|Все файлы (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                string file = dialog.FileName;
                string ext = System.IO.Path.GetExtension(file).ToLower();

                if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp")
                {
                    SetBackground(file);
                }
                else if (ext == ".json")
                {
                    await LoadTemplateFromFile(file);
                }
                else
                {
                    System.Windows.MessageBox.Show("Неподдерживаемый формат файла", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private async Task LoadTemplateFromFile(string filePath)
        {
            try
            {
                var template = await _templateService.LoadTemplateAsync(filePath);
                if (template != null)
                {
                    _currentTemplate = template;
                    RefreshBlocksList();
                    if (!string.IsNullOrEmpty(template.BackgroundPath) && File.Exists(template.BackgroundPath))
                        SetBackground(template.BackgroundPath);
                    RefreshPreview();
                    System.Windows.MessageBox.Show("Шаблон загружен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SaveTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Сохранить шаблон",
                Filter = "Шаблоны (*.json)|*.json",
                InitialDirectory = TemplatesFolder,
                FileName = "шаблон.json"
            };
            if (dialog.ShowDialog() == true)
            {
                await _templateService.SaveTemplateAsync(_currentTemplate, dialog.FileName);
                System.Windows.MessageBox.Show("Шаблон сохранён", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void SelectExcelButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Выберите файл с ФИО",
                Filter = "CSV файлы (*.csv)|*.csv|Текстовые файлы (*.txt)|*.txt"
            };
            if (dialog.ShowDialog() == true)
                await LoadExcelFile(dialog.FileName);
        }

        private async Task LoadExcelFile(string filePath)
        {
            try
            {
                var persons = await Task.Run(() => _excelParser.Parse(filePath));
                if (persons == null || persons.Count == 0)
                {
                    System.Windows.MessageBox.Show("Не найдены ФИО в файле", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                _persons = persons;
                _declensionService.DeclinePersons(_persons);
                RefreshPersonsGrid();
                System.Windows.MessageBox.Show($"Загружено {_persons.Count} человек", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshPersonsGrid()
        {
            var displayList = _persons.Select(p => new PersonDisplay { FullName = p.FullName, DisplayName = p.GetFullName(_useDative) }).ToList();
            PersonsGrid.ItemsSource = displayList;
        }

        private void Case_Changed(object sender, RoutedEventArgs e)
        {
            // Проверяем, что радиокнопка уже создана
            if (DativeCase == null || NominativeCase == null)
                return;

            _useDative = DativeCase.IsChecked == true;
            RefreshPersonsGrid();
        }


        private void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    OutputFolderBox.Text = dialog.SelectedPath;
            }
        }

        private void OpenOutputFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_hasGenerated)
            {
                System.Windows.MessageBox.Show("Сначала сгенерируйте грамоты", "Нет результатов",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string folder = OutputFolderBox.Text;
            if (string.IsNullOrEmpty(folder)) folder = DefaultOutputFolder;

            // Открываем последнюю папку с результатами
            string latestFolder = GetLatestOutputFolder(folder);
            if (Directory.Exists(latestFolder))
                System.Diagnostics.Process.Start("explorer.exe", latestFolder);
            else
                System.Windows.MessageBox.Show("Папка с результатами не найдена", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string GetLatestOutputFolder(string baseFolder)
        {
            if (!Directory.Exists(baseFolder)) return baseFolder;

            var subfolders = Directory.GetDirectories(baseFolder);
            if (subfolders.Length == 0) return baseFolder;

            // Возвращаем самую новую папку (по дате создания)
            return subfolders.OrderByDescending(d => Directory.GetCreationTime(d)).FirstOrDefault() ?? baseFolder;
        }

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_persons.Count == 0)
            {
                System.Windows.MessageBox.Show("Сначала загрузите файл с ФИО", "Нет данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string outputFolder = OutputFolderBox.Text;
            if (string.IsNullOrEmpty(outputFolder)) outputFolder = DefaultOutputFolder;
            string dateFolder = System.IO.Path.Combine(outputFolder, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
            Directory.CreateDirectory(dateFolder);

            if (AutoSaveTemplateCheckBox.IsChecked == true)
            {
                string autoSavePath = System.IO.Path.Combine(dateFolder, "шаблон.json");
                await _templateService.SaveTemplateAsync(_currentTemplate, autoSavePath);
            }

            string format = (ImageFormatBox.SelectedItem as ComboBoxItem)?.Content.ToString()?.ToLower() ?? "png";
            var progressWindow = new ProgressWindow(_persons.Count);
            progressWindow.Show();

            try
            {
                int generated = await Task.Run(() => _imageGenerator.GenerateCertificates(
                    _currentTemplate, _persons, dateFolder, _useDative, format));

                progressWindow.Close();
                System.Windows.MessageBox.Show($"Генерация завершена!\nСоздано: {generated}\nПапка: {dateFolder}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                System.Diagnostics.Process.Start("explorer.exe", dateFolder);
            }
            catch (Exception ex)
            {
                progressWindow.Close();
                System.Windows.MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            _hasGenerated = true;
            OpenFolderButton.IsEnabled = true;
        }

        private void BlocksListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _isUpdatingProperties = true;

            _selectedBlock = BlocksListBox.SelectedItem as TextBlockData;
            if (_selectedBlock != null)
            {
                TextPropertyBox.Text = _selectedBlock.Text;
                FontFamilyBox.SelectedItem = _selectedBlock.FontFamily;
                FontSizeBox.Text = _selectedBlock.FontSize.ToString();
                BoldToggle.IsChecked = _selectedBlock.IsBold;
                ItalicToggle.IsChecked = _selectedBlock.IsItalic;

                // Временно отключаем обновление позиции
                PositionXBox.Text = _selectedBlock.PositionX.ToString();
                PositionYBox.Text = _selectedBlock.PositionY.ToString();
            }

            _isUpdatingProperties = false;
        }

        private void Position_Changed(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingProperties) return;

            if (_selectedBlock != null)
            {
                if (double.TryParse(PositionXBox.Text, out double x))
                {
                    // Ограничиваем X: не меньше 0 и не больше ширины холста минус примерная ширина блока
                    double maxX = PreviewCanvas.Width - 200;
                    _selectedBlock.PositionX = Math.Clamp(x, 0, maxX);
                }

                if (double.TryParse(PositionYBox.Text, out double y))
                {
                    // Ограничиваем Y: не меньше 0 и не больше высоты холста минус примерная высота блока
                    double maxY = PreviewCanvas.Height - 100;
                    _selectedBlock.PositionY = Math.Clamp(y, 0, maxY);
                }

                // Обновляем поля, если значение было скорректировано
                PositionXBox.Text = _selectedBlock.PositionX.ToString();
                PositionYBox.Text = _selectedBlock.PositionY.ToString();

                RefreshPreview();
            }
        }

        private void AddBlockButton_Click(object sender, RoutedEventArgs e)
        {
            _currentTemplate.TextBlocks.Add(new TextBlockData { Id = Guid.NewGuid().ToString(), Text = "Новый блок", PositionX = 200, PositionY = 300 });
            RefreshBlocksList();
            RefreshPreview();
        }

        private void DeleteBlockButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedBlock != null)
            {
                if (_selectedBlock.Type == TextBlockType.PersonName)
                {
                    System.Windows.MessageBox.Show("Нельзя удалить блок с ФИО. Он необходим для генерации.",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _currentTemplate.TextBlocks.Remove(_selectedBlock);
                _selectedBlock = null;
                RefreshBlocksList();
                RefreshPreview();
            }
        }

        private void TextPropertyBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingProperties) return;
            if (_selectedBlock != null)
            {
                _selectedBlock.Text = TextPropertyBox.Text;
                RefreshPreview();
            }
        }

        private void FontProperty_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingProperties) return;  // Добавить
            if (_selectedBlock != null && FontFamilyBox.SelectedItem != null)
            {
                _selectedBlock.FontFamily = FontFamilyBox.SelectedItem.ToString() ?? "Times New Roman";
                RefreshPreview();
            }
        }

        private void FontProperty_Changed(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingProperties) return;  // Добавить
            if (_selectedBlock != null && float.TryParse(FontSizeBox.Text, out float size))
            {
                _selectedBlock.FontSize = size;
                RefreshPreview();
            }
        }
        private void FontSizeUp_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedBlock != null)
            {
                _selectedBlock.FontSize += 2;
                FontSizeBox.Text = _selectedBlock.FontSize.ToString();
                RefreshPreview();
            }
        }

        private void FontSizeDown_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedBlock != null && _selectedBlock.FontSize > 6)
            {
                _selectedBlock.FontSize -= 2;
                FontSizeBox.Text = _selectedBlock.FontSize.ToString();
                RefreshPreview();
            }
        }

        private void FontStyle_Changed(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingProperties) return;
            if (_selectedBlock != null)
            {
                _selectedBlock.IsBold = BoldToggle.IsChecked ?? false;
                _selectedBlock.IsItalic = ItalicToggle.IsChecked ?? false;
                RefreshPreview();
            }
        }

        private void AiSuggestButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Функция ИИ-помощника в разработке", "В разработке", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public class BackgroundItem
    {
        public string FilePath { get; set; } = string.Empty;
        public ImageSource? Thumbnail { get; set; }
    }

    public class PersonDisplay
    {
        public string FullName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
}