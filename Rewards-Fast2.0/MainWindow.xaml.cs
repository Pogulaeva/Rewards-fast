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

using Point = System.Windows.Point;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Cursors = System.Windows.Input.Cursors;

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
        private bool _isDraggingBlock = false;
        private bool _isUpdatingProperties = false;
        private TextBlockData? _draggedBlockData = null;
        private Point _dragStartPointCanvas;
        private Point _dragStartPointBlock;
        private double _currentScale = 1.0;
        private ImageBlockData? _selectedImage;
        private bool _isUpdatingImageProperties = false;

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
                _useDative = DativeCase.IsChecked == true;
                RefreshPersonsGrid();
            };
            OpenFolderButton.IsEnabled = false;
            InitializeAppFolders();
            LoadFonts();
            LoadBackgroundLibrary();
            SetupDefaultTemplate();
            OutputFolderBox.Text = DefaultOutputFolder;

            // Добавить эту строку ↓
            this.SizeChanged += (s, e) => RefreshPreview();
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
            if (PreviewCanvas == null) return;

            // Отвязываем старые обработчики
            foreach (UIElement child in PreviewCanvas.Children)
            {
                if (child is System.Windows.Controls.TextBlock tb)
                {
                    tb.MouseLeftButtonDown -= TextBlock_MouseLeftButtonDown;
                    tb.MouseMove -= TextBlock_MouseMove;
                    tb.MouseLeftButtonUp -= TextBlock_MouseLeftButtonUp;
                }
            }

            PreviewCanvas.Children.Clear();

            // Реальные размеры фона
            double realWidth = 800;
            double realHeight = 600;
            BitmapImage? backgroundImage = null;

            if (!string.IsNullOrEmpty(_currentTemplate.BackgroundPath) && File.Exists(_currentTemplate.BackgroundPath))
            {
                backgroundImage = LoadBitmapImage(_currentTemplate.BackgroundPath);
                realWidth = backgroundImage.Width;
                realHeight = backgroundImage.Height;
            }

            // Получаем доступный размер панели
            var parentGrid = PreviewCanvas.Parent as Grid;
            double availableWidth = parentGrid?.ActualWidth ?? 800;
            double availableHeight = parentGrid?.ActualHeight ?? 600;

            if (availableWidth <= 0) availableWidth = 800;
            if (availableHeight <= 0) availableHeight = 600;

            // Вычисляем масштаб, чтобы вписать грамоту в панель
            double scaleX = availableWidth / realWidth;
            double scaleY = availableHeight / realHeight;
            double scale = Math.Min(scaleX, scaleY);

            // Устанавливаем размер Canvas с учётом масштаба
            double canvasWidth = realWidth * scale;
            double canvasHeight = realHeight * scale;

            PreviewCanvas.Width = canvasWidth;
            PreviewCanvas.Height = canvasHeight;

            // Добавляем фон (растягиваем под Canvas)
            if (backgroundImage != null)
            {
                var image = new System.Windows.Controls.Image
                {
                    Source = backgroundImage,
                    Stretch = Stretch.Fill,
                    Width = canvasWidth,
                    Height = canvasHeight
                };
                PreviewCanvas.Children.Add(image);
            }

            // Добавляем текстовые блоки с масштабированием
            foreach (var block in _currentTemplate.TextBlocks)
            {
                if (!block.IsVisible) continue;

                double fontSize = block.FontSize * scale;
                if (fontSize < 4) fontSize = 4;

                var textBlock = new System.Windows.Controls.TextBlock
                {
                    Text = block.Text,
                    FontFamily = new System.Windows.Media.FontFamily(block.FontFamily),
                    FontSize = fontSize,
                    FontWeight = block.IsBold ? FontWeights.Bold : FontWeights.Normal,
                    FontStyle = block.IsItalic ? FontStyles.Italic : FontStyles.Normal,
                    Foreground = block.FontColorBrush,
                    TextAlignment = TextAlignment.Center,
                    Width = canvasWidth * 0.8,
                    TextWrapping = TextWrapping.Wrap,
                    Tag = block
                };

                textBlock.MouseLeftButtonDown += TextBlock_MouseLeftButtonDown;
                textBlock.MouseMove += TextBlock_MouseMove;
                textBlock.MouseLeftButtonUp += TextBlock_MouseLeftButtonUp;
                textBlock.Cursor = Cursors.SizeAll;

                // Масштабируем координаты
                Canvas.SetLeft(textBlock, block.PositionX * scale);
                Canvas.SetTop(textBlock, block.PositionY * scale);
                PreviewCanvas.Children.Add(textBlock);
            }

            // Добавляем изображения
            foreach (var imageBlock in _currentTemplate.ImageBlocks)
            {
                if (!imageBlock.IsVisible || imageBlock.Source == null) continue;

                var image = new System.Windows.Controls.Image
                {
                    Source = imageBlock.Source,
                    Width = imageBlock.Width * scale,
                    Height = imageBlock.Height * scale,
                    Stretch = Stretch.Fill,
                    Tag = imageBlock
                };

                // Добавляем обработчики для перетаскивания
                image.MouseLeftButtonDown += Image_MouseLeftButtonDown;
                image.MouseMove += Image_MouseMove;
                image.MouseLeftButtonUp += Image_MouseLeftButtonUp;
                image.Cursor = Cursors.SizeAll;

                Canvas.SetLeft(image, imageBlock.PositionX * scale);
                Canvas.SetTop(image, imageBlock.PositionY * scale);
                PreviewCanvas.Children.Add(image);
            }


            _currentScale = scale;
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
                    RefreshImagesList();

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
                int generated = 0;

                // Запускаем генерацию в UI-потоке, но с обновлением прогресса через Dispatcher
                await Task.Run(() =>
                {
                    for (int i = 0; i < _persons.Count; i++)
                    {
                        var person = _persons[i];

                        // Обновляем прогресс через Dispatcher
                        progressWindow.Dispatcher.Invoke(() =>
                        {
                            progressWindow.UpdateProgress(i + 1, _persons.Count);
                        });

                        string nameToInsert = _useDative ? person.FullNameDative : person.FullName;
                        string fileName = GenerateFileName(person, i + 1, format);
                        string fullPath = System.IO.Path.Combine(dateFolder, fileName);

                        // Генерацию одной грамоты выносим в UI-поток через Dispatcher
                        Dispatcher.Invoke(() =>
                        {
                            _imageGenerator.GenerateSingleCertificate(_currentTemplate, nameToInsert, fullPath, format);
                        });

                        generated++;
                    }
                });

                // Альтернатива — просто синхронный вызов (но тогда прогресс не будет обновляться)
                // generated = _imageGenerator.GenerateCertificates(_currentTemplate, _persons, dateFolder, _useDative, format);

                progressWindow.Close();
                System.Windows.MessageBox.Show($"Генерация завершена!\nСоздано: {generated}\nПапка: {dateFolder}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                System.Diagnostics.Process.Start("explorer.exe", dateFolder);
            }
            catch (Exception ex)
            {
                progressWindow.Close();
                System.Windows.MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GenerateFileName(Person person, int index, string format)
        {
            string safeName = person.FullName
                .Replace(' ', '_')
                .Replace('.', '_')
                .Replace(',', '_')
                .Replace('(', '_')
                .Replace(')', '_');

            if (safeName.Length > 50)
                safeName = safeName.Substring(0, 50);

            return $"{index:0000}_{safeName}.{format.ToLower()}";
        }

        // При выборе текстового блока возвращаем активность
        private void BlocksListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _isUpdatingProperties = true;

            _selectedBlock = BlocksListBox.SelectedItem as TextBlockData;

            if (_selectedImage != null)
            {
                _selectedImage = null;
                ImagesListBox.SelectedItem = null;
            }

            if (_selectedBlock != null)
            {
                // Включаем свойства текста
                TextPropertyBox.IsEnabled = true;
                FontFamilyBox.IsEnabled = true;
                FontSizeBox.IsEnabled = true;
                FontSizeUp.IsEnabled = true;
                FontSizeDown.IsEnabled = true;
                BoldToggle.IsEnabled = true;
                ItalicToggle.IsEnabled = true;

                // ОТКЛЮЧАЕМ поля размера изображения
                ImageWidthBox.IsEnabled = false;
                ImageHeightBox.IsEnabled = false;

                TextPropertyBox.Text = _selectedBlock.Text;
                FontFamilyBox.SelectedItem = _selectedBlock.FontFamily;
                FontSizeBox.Text = _selectedBlock.FontSize.ToString();
                BoldToggle.IsChecked = _selectedBlock.IsBold;
                ItalicToggle.IsChecked = _selectedBlock.IsItalic;

                if (!_isDraggingBlock)
                {
                    PositionXBox.Text = _selectedBlock.PositionX.ToString();
                    PositionYBox.Text = _selectedBlock.PositionY.ToString();
                }
            }

            _isUpdatingProperties = false;
        }


        private void Position_Changed(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingProperties) return;

            // Получаем реальные размеры фона
            double realWidth = 800;
            double realHeight = 600;

            if (!string.IsNullOrEmpty(_currentTemplate.BackgroundPath) && File.Exists(_currentTemplate.BackgroundPath))
            {
                var tempImage = LoadBitmapImage(_currentTemplate.BackgroundPath);
                realWidth = tempImage.Width;
                realHeight = tempImage.Height;
            }

            if (_selectedBlock != null)
            {
                if (double.TryParse(PositionXBox.Text, out double x))
                {
                    // Ограничиваем реальными размерами фона
                    _selectedBlock.PositionX = Math.Clamp(x, 0, realWidth - 50);
                }

                if (double.TryParse(PositionYBox.Text, out double y))
                {
                    _selectedBlock.PositionY = Math.Clamp(y, 0, realHeight - 50);
                }

                RefreshPreview();
            }
        }

        private void AddBlockButton_Click(object sender, RoutedEventArgs e)
        {
            // Получаем реальные размеры фона
            double realWidth = 800;
            double realHeight = 600;

            if (!string.IsNullOrEmpty(_currentTemplate.BackgroundPath) && File.Exists(_currentTemplate.BackgroundPath))
            {
                var tempImage = LoadBitmapImage(_currentTemplate.BackgroundPath);
                realWidth = tempImage.Width;
                realHeight = tempImage.Height;
            }

            var newBlock = new TextBlockData
            {
                Id = Guid.NewGuid().ToString(),
                Text = "Новый блок",
                PositionX = realWidth / 2 - 100,  // Центр по X
                PositionY = realHeight / 2 - 20,   // Центр по Y
                FontSize = 24,
                FontFamily = "Times New Roman"
            };

            _currentTemplate.TextBlocks.Add(newBlock);
            RefreshBlocksList();
            RefreshPreview();
        }


        private void DeleteBlockButton_Click(object sender, RoutedEventArgs e)
        {
            // Если выбрано изображение
            if (_selectedImage != null)
            {
                _currentTemplate.ImageBlocks.Remove(_selectedImage);
                _selectedImage = null;
                RefreshImagesList();
                RefreshPreview();
                return;
            }

            // Если выбран текстовый блок
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

        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var textBlock = sender as System.Windows.Controls.TextBlock;
            if (textBlock != null && textBlock.Tag is TextBlockData block)
            {
                // Автоматически выбираем блок в списке
                if (_selectedBlock != block)
                {
                    _selectedBlock = block;
                    BlocksListBox.SelectedItem = block;
                    // Обновляем поля свойств
                    TextPropertyBox.Text = block.Text;
                    FontFamilyBox.SelectedItem = block.FontFamily;
                    FontSizeBox.Text = block.FontSize.ToString();
                    BoldToggle.IsChecked = block.IsBold;
                    ItalicToggle.IsChecked = block.IsItalic;
                    PositionXBox.Text = block.PositionX.ToString();
                    PositionYBox.Text = block.PositionY.ToString();
                }

                _isDraggingBlock = true;
                _draggedBlockData = block;
                _dragStartPointCanvas = e.GetPosition(PreviewCanvas);
                _dragStartPointBlock = new Point(block.PositionX, block.PositionY);
                textBlock.CaptureMouse();
                e.Handled = true;
            }
        }
        private void TextBlock_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                _isDraggingBlock = false;
                _draggedBlockData = null;
                return;
            }

            if (!_isDraggingBlock || _draggedBlockData == null) return;

            var textBlock = sender as System.Windows.Controls.TextBlock;
            if (textBlock == null) return;

            Point currentPoint = e.GetPosition(PreviewCanvas);

            // Пересчет с правильным учетом масштаба
            double deltaX = (currentPoint.X - _dragStartPointCanvas.X) / _currentScale;
            double deltaY = (currentPoint.Y - _dragStartPointCanvas.Y) / _currentScale;

            double newRealX = _dragStartPointBlock.X + deltaX;
            double newRealY = _dragStartPointBlock.Y + deltaY;

            // Получаем реальные размеры фона
            double realWidth = 800;
            double realHeight = 600;

            if (!string.IsNullOrEmpty(_currentTemplate.BackgroundPath) && File.Exists(_currentTemplate.BackgroundPath))
            {
                var tempImage = LoadBitmapImage(_currentTemplate.BackgroundPath);
                realWidth = tempImage.Width;
                realHeight = tempImage.Height;
            }

            // Ограничиваем перемещение границами фона
            _draggedBlockData.PositionX = Math.Clamp(newRealX, 0, realWidth - 50);
            _draggedBlockData.PositionY = Math.Clamp(newRealY, 0, realHeight - 50);

            // Обновляем позицию на Canvas
            Canvas.SetLeft(textBlock, _draggedBlockData.PositionX * _currentScale);
            Canvas.SetTop(textBlock, _draggedBlockData.PositionY * _currentScale);

            // Обновляем поля свойств
            if (_selectedBlock == _draggedBlockData)
            {
                PositionXBox.Text = _draggedBlockData.PositionX.ToString("F0");
                PositionYBox.Text = _draggedBlockData.PositionY.ToString("F0");
            }
        }

        private void TextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingBlock = false;
            _draggedBlockData = null;
            var textBlock = sender as System.Windows.Controls.TextBlock;
            if (textBlock != null)
            {
                textBlock.ReleaseMouseCapture();
            }
        }

        private ImageBlockData? _draggedImage = null;
        private bool _isDraggingImage = false;
        private Point _dragStartPointImageCanvas;
        private Point _dragStartPointImageReal;

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var image = sender as System.Windows.Controls.Image;
            if (image?.Tag is ImageBlockData imageBlock)
            {
                _isDraggingImage = true;
                _draggedImage = imageBlock;
                _dragStartPointImageCanvas = e.GetPosition(PreviewCanvas);
                _dragStartPointImageReal = new Point(imageBlock.PositionX, imageBlock.PositionY);
                image.CaptureMouse();
                e.Handled = true;
            }
        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                _isDraggingImage = false;
                _draggedImage = null;
                return;
            }

            if (!_isDraggingImage || _draggedImage == null) return;

            var image = sender as System.Windows.Controls.Image;
            if (image == null) return;

            Point currentPoint = e.GetPosition(PreviewCanvas);
            double deltaX = (currentPoint.X - _dragStartPointImageCanvas.X) / _currentScale;
            double deltaY = (currentPoint.Y - _dragStartPointImageCanvas.Y) / _currentScale;

            double newX = _dragStartPointImageReal.X + deltaX;
            double newY = _dragStartPointImageReal.Y + deltaY;

            // Ограничения по границам фона
            double realWidth = 800, realHeight = 600;
            if (!string.IsNullOrEmpty(_currentTemplate.BackgroundPath) && File.Exists(_currentTemplate.BackgroundPath))
            {
                var tempImage = LoadBitmapImage(_currentTemplate.BackgroundPath);
                realWidth = tempImage.Width;
                realHeight = tempImage.Height;
            }

            _draggedImage.PositionX = Math.Clamp(newX, 0, realWidth - _draggedImage.Width);
            _draggedImage.PositionY = Math.Clamp(newY, 0, realHeight - _draggedImage.Height);

            Canvas.SetLeft(image, _draggedImage.PositionX * _currentScale);
            Canvas.SetTop(image, _draggedImage.PositionY * _currentScale);
        }

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingImage = false;
            _draggedImage = null;
            var image = sender as System.Windows.Controls.Image;
            if (image != null) image.ReleaseMouseCapture();
        }

        private void ImagesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _isUpdatingImageProperties = true;

            _selectedImage = ImagesListBox.SelectedItem as ImageBlockData;

            if (_selectedBlock != null)
            {
                _selectedBlock = null;
                BlocksListBox.SelectedItem = null;
            }

            if (_selectedImage != null)
            {
                TextPropertyBox.IsEnabled = false;
                FontFamilyBox.IsEnabled = false;
                FontSizeBox.IsEnabled = false;
                FontSizeUp.IsEnabled = false;
                FontSizeDown.IsEnabled = false;
                BoldToggle.IsEnabled = false;
                ItalicToggle.IsEnabled = false;

                ImageWidthBox.IsEnabled = true;
                ImageHeightBox.IsEnabled = true;

                if (!_isDraggingImage)
                {
                    PositionXBox.Text = _selectedImage.PositionX.ToString();
                    PositionYBox.Text = _selectedImage.PositionY.ToString();
                }

                ImageWidthBox.Text = _selectedImage.Width.ToString();
                ImageHeightBox.Text = _selectedImage.Height.ToString();
            }

            _isUpdatingImageProperties = false;
        }

        private void RefreshImagesList()
        {
            ImagesListBox.ItemsSource = null;
            ImagesListBox.ItemsSource = _currentTemplate.ImageBlocks;
        }

        private async void AddImageButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Выберите изображение (печать, подпись, логотип)",
                Filter = "Изображения (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp"
            };

            if (dialog.ShowDialog() == true)
            {
                // Определяем следующий номер для изображения
                int nextNumber = _currentTemplate.ImageBlocks.Count + 1;

                var newImage = new ImageBlockData
                {
                    Id = Guid.NewGuid().ToString(),
                    ImagePath = dialog.FileName,
                    Name = $"Изображение {nextNumber}",
                    PositionX = 200,
                    PositionY = 200,
                    Width = 80,
                    Height = 80
                };
                newImage.LoadImage();

                _currentTemplate.ImageBlocks.Add(newImage);
                RefreshImagesList();
                RefreshPreview();
            }
        }

        private void ImageSize_Changed(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingImageProperties) return;
            if (_selectedImage == null) return;

            if (double.TryParse(ImageWidthBox.Text, out double width))
                _selectedImage.Width = Math.Clamp(width, 20, 500);

            if (double.TryParse(ImageHeightBox.Text, out double height))
                _selectedImage.Height = Math.Clamp(height, 20, 500);

            RefreshPreview();
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