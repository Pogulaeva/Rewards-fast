using Microsoft.Win32;
using Rewards_Fast2._0;
using Rewards_Fast2._0.Models;
using Rewards_Fast2._0.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Brushes = System.Windows.Media.Brushes;
using Cursor = System.Windows.Input.Cursor;
using Cursors = System.Windows.Input.Cursors;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Panel = System.Windows.Controls.Panel;
using Point = System.Windows.Point;

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

        private string _resizeHandle = "";
        private Point _resizeStartPoint;
        private double _resizeStartWidth;
        private double _resizeStartHeight;
        private bool _isResizeMode = false;
        private ImageBlockData? _resizingImage = null;
        private bool _isResizing = false;
        private double _resizeStartX;
        private double _resizeStartY;

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

            // Кнопка генерации изначально неактивна
            GenerateButton.IsEnabled = false;
            GenerateButton.Opacity = 0.6;  // Визуально показываем, что кнопка неактивна
            GenerateButton.ToolTip = "Сначала загрузите фоновое изображение";

            SaveTemplateButton.IsEnabled = false;
            SaveTemplateButton.Opacity = 0.6;
            SaveTemplateButton.ToolTip = "Сначала загрузите фоновое изображение";

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

            this.SizeChanged += (s, e) => RefreshPreview();
            PreviewCanvas.MouseLeftButtonDown += PreviewCanvas_MouseLeftButtonDown;

            // Обработчик закрытия главного окна
            this.Closing += (s, e) =>
            {
                if (_progressWindow != null && _progressWindow.IsVisible)
                {
                    _isGenerationCancelled = true;
                }
            };
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

            // Получаем путь к папке с фонами в выходной директории
            string backgroundsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Backgrounds");

            if (Directory.Exists(backgroundsPath))
            {
                foreach (string file in Directory.GetFiles(backgroundsPath))
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
                            backgrounds.Add(new BackgroundItem
                            {
                                FilePath = file,
                                Thumbnail = bitmap,
                                IsBuiltIn = true
                            });
                        }
                        catch { }
                    }
                }
            }

            // Загружаем пользовательские фоны из папки в документах
            if (Directory.Exists(BackgroundsFolder))
            {
                foreach (string file in Directory.GetFiles(BackgroundsFolder))
                {
                    string ext = System.IO.Path.GetExtension(file).ToLower();
                    if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp")
                    {
                        // Проверяем, не добавили ли уже этот файл
                        if (backgrounds.Any(b => b.FilePath == file)) continue;

                        try
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.UriSource = new Uri(file, UriKind.Absolute);
                            bitmap.DecodePixelWidth = 80;
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            backgrounds.Add(new BackgroundItem
                            {
                                FilePath = file,
                                Thumbnail = bitmap,
                                IsBuiltIn = false
                            });
                        }
                        catch { }
                    }
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
                else if (child is System.Windows.Controls.Image img)
                {
                    img.MouseLeftButtonDown -= Image_MouseLeftButtonDown;
                    img.MouseMove -= Image_MouseMove;
                    img.MouseLeftButtonUp -= Image_MouseLeftButtonUp;
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

            // Вычисляем масштаб
            double scaleX = availableWidth / realWidth;
            double scaleY = availableHeight / realHeight;
            double scale = Math.Min(scaleX, scaleY);

            // Устанавливаем размер Canvas
            double canvasWidth = realWidth * scale;
            double canvasHeight = realHeight * scale;

            PreviewCanvas.Width = canvasWidth;
            PreviewCanvas.Height = canvasHeight;
            _currentScale = scale;

            // Добавляем фон
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

            // Добавляем текстовые блоки
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
                    TextWrapping = TextWrapping.Wrap,
                    Tag = block,
                    TextDecorations = block.IsUnderline ? TextDecorations.Underline : null
                };

                // ИЗМЕРИМ РЕАЛЬНЫЕ РАЗМЕРЫ ТЕКСТА!
                textBlock.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));

                // Сохраняем реальные размеры в блоке (в координатах фона, без масштаба)
                block.ActualWidth = textBlock.DesiredSize.Width / scale;
                block.ActualHeight = textBlock.DesiredSize.Height / scale;

                // Устанавливаем ширину текстового блока (но не ограничиваем, пусть будет сколько нужно)
                // Если хотим ограничить максимальную ширину:
                double maxWidth = realWidth * 0.8;
                if (block.ActualWidth > maxWidth)
                {
                    textBlock.Width = maxWidth * scale;
                    textBlock.TextWrapping = TextWrapping.Wrap;
                    // Переизмеряем с ограничением ширины
                    textBlock.Measure(new System.Windows.Size(maxWidth * scale, double.PositiveInfinity));
                    block.ActualWidth = textBlock.DesiredSize.Width / scale;
                    block.ActualHeight = textBlock.DesiredSize.Height / scale;
                }

                textBlock.MouseLeftButtonDown += TextBlock_MouseLeftButtonDown;
                textBlock.MouseMove += TextBlock_MouseMove;
                textBlock.MouseLeftButtonUp += TextBlock_MouseLeftButtonUp;
                textBlock.Cursor = Cursors.SizeAll;

                // Корректируем позицию, если она выходит за границы
                double maxX = realWidth - block.ActualWidth;
                double maxY = realHeight - block.ActualHeight;

                if (block.PositionX > maxX) block.PositionX = Math.Max(0, maxX);
                if (block.PositionY > maxY) block.PositionY = Math.Max(0, maxY);
                if (block.PositionX < 0) block.PositionX = 0;
                if (block.PositionY < 0) block.PositionY = 0;

                Canvas.SetLeft(textBlock, block.PositionX * scale);
                Canvas.SetTop(textBlock, block.PositionY * scale);
                PreviewCanvas.Children.Add(textBlock);
            }

            // Добавляем изображения (без изменений)
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

                image.MouseLeftButtonDown += Image_MouseLeftButtonDown;
                image.MouseMove += Image_MouseMove;
                image.MouseLeftButtonUp += Image_MouseLeftButtonUp;
                image.Cursor = Cursors.SizeAll;

                Canvas.SetLeft(image, imageBlock.PositionX * scale);
                Canvas.SetTop(image, imageBlock.PositionY * scale);
                PreviewCanvas.Children.Add(image);
            }

            // Добавляем маркеры
            if (_isResizeMode && _resizingImage != null && _currentTemplate.ImageBlocks.Contains(_resizingImage))
            {
                AddResizeHandles(_resizingImage);
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
            RefreshPreview();

            GenerateButton.IsEnabled = true;
            GenerateButton.Opacity = 1;
            GenerateButton.ToolTip = null;

            SaveTemplateButton.IsEnabled = true;
            SaveTemplateButton.Opacity = 1;
            SaveTemplateButton.ToolTip = null;
        }

        private void BackgroundThumbnail_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is BackgroundItem item)
            {
                if (item.IsBuiltIn)
                {
                    _currentTemplate.BackgroundPath = item.FilePath;
                    RefreshPreview();

                    GenerateButton.IsEnabled = true;
                    GenerateButton.Opacity = 1;
                    GenerateButton.ToolTip = null;

                    SaveTemplateButton.IsEnabled = true;
                    SaveTemplateButton.Opacity = 1;
                    SaveTemplateButton.ToolTip = null;
                }
                else
                {
                    SetBackground(item.FilePath);
                }
            }
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
                    {
                        SetBackground(template.BackgroundPath);  // Этот метод активирует кнопки
                    }
                    else
                    {
                        // Если в шаблоне нет фона — кнопки остаются неактивными
                        GenerateButton.IsEnabled = false;
                        GenerateButton.Opacity = 0.6;
                        GenerateButton.ToolTip = "В шаблоне нет фонового изображения";

                        SaveTemplateButton.IsEnabled = false;
                        SaveTemplateButton.Opacity = 0.6;
                        SaveTemplateButton.ToolTip = "В шаблоне нет фонового изображения";
                    }

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
                Filter = "Все поддерживаемые файлы (*.xlsx;*.xls;*.csv;*.txt)|*.xlsx;*.xls;*.csv;*.txt|" +
                         "Excel файлы (*.xlsx;*.xls)|*.xlsx;*.xls|" +
                         "CSV файлы (*.csv)|*.csv|" +
                         "Текстовые файлы (*.txt)|*.txt"
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

                // Склоняем ОБОИМИ способами (нужно для будущей генерации и сравнения)
                _declensionService.DeclinePersonsBothWays(_persons);

                // Обновляем таблицу в зависимости от выбранного падежа
                RefreshPersonsGrid();

                System.Windows.MessageBox.Show($"Загружено {_persons.Count} человек", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshPersonsGrid(bool showMismatch = true)
        {
            var displayList = _persons.Select(p => new PersonDisplay
            {
                FullName = p.FullName,
                DisplayNameLibrary = _useDative ? p.FullNameDativeLibrary : p.FullName,
                DisplayNameFallback = _useDative ? p.FullNameDativeFallback : p.FullName,
                // В именительном падеже расхождений быть не может
                HasDeclensionMismatch = showMismatch && _useDative && p.HasDeclensionMismatch
            }).ToList();

            PersonsGrid.ItemsSource = displayList;
        }

        private void Case_Changed(object sender, RoutedEventArgs e)
        {
            if (DativeCase == null || NominativeCase == null)
                return;

            _useDative = DativeCase.IsChecked == true;

            // Если выбран именительный падеж - сбрасываем флаг расхождения для отображения
            if (!_useDative)
            {
                // Обновляем таблицу без подсветки
                RefreshPersonsGrid(showMismatch: false);
            }
            else
            {
                // Для дательного падежа показываем подсветку
                RefreshPersonsGrid(showMismatch: true);
            }
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

            return subfolders.OrderByDescending(d => Directory.GetCreationTime(d)).FirstOrDefault() ?? baseFolder;
        }

        private List<Person> _personsWithMismatch = new List<Person>();

        private bool _isGenerationCancelled = false;
        private ProgressWindow? _progressWindow;

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_persons.Count == 0)
            {
                System.Windows.MessageBox.Show("Сначала загрузите файл с ФИО", "Нет данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Блокируем кнопки
            GenerateButton.IsEnabled = false;
            SaveTemplateButton.IsEnabled = false;

            _isGenerationCancelled = false;

            try
            {
                await Task.Run(() => _declensionService.DeclinePersonsBothWays(_persons));

                if (!_useDative)
                {
                    foreach (var person in _persons)
                        person.FullNameDative = person.FullName;

                    await Dispatcher.InvokeAsync(() => RefreshPersonsGrid());
                    await GenerateCertificatesAsync();
                    return;
                }

                var personsWithMismatch = _persons.Where(p => p.HasDeclensionMismatch).ToList();
                bool? useLibraryForMismatch = null;

                if (personsWithMismatch.Any())
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        var dialog = new ChoiceDialog(personsWithMismatch);
                        var choice = dialog.GetResult();
                        switch (choice)
                        {
                            case ChoiceDialog.UserChoice.Option1: useLibraryForMismatch = true; break;
                            case ChoiceDialog.UserChoice.Option2: useLibraryForMismatch = false; break;
                            case ChoiceDialog.UserChoice.Cancel: SaveMismatchedPersonsList(personsWithMismatch); return;
                            case ChoiceDialog.UserChoice.ClosedByX: return;
                        }
                    });
                }

                foreach (var person in _persons)
                {
                    if (person.HasDeclensionMismatch && useLibraryForMismatch.HasValue)
                        person.FullNameDative = useLibraryForMismatch.Value ? person.FullNameDativeLibrary : person.FullNameDativeFallback;
                    else
                        person.FullNameDative = person.FullNameDativeLibrary;
                }

                await Dispatcher.InvokeAsync(() => RefreshPersonsGrid());
                await GenerateCertificatesAsync(personsWithMismatch, useLibraryForMismatch);
            }
            finally
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    GenerateButton.IsEnabled = true;
                    SaveTemplateButton.IsEnabled = !string.IsNullOrEmpty(_currentTemplate.BackgroundPath);
                });
            }
        }

        private async Task GenerateCertificatesAsync(List<Person>? personsWithMismatch = null, bool? useLibraryForMismatch = null)
        {
            string outputFolder = OutputFolderBox.Text;
            if (string.IsNullOrEmpty(outputFolder)) outputFolder = DefaultOutputFolder;
            string dateFolder = System.IO.Path.Combine(outputFolder, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
            Directory.CreateDirectory(dateFolder);

            if (AutoSaveTemplateCheckBox.IsChecked == true)
            {
                string autoSavePath = System.IO.Path.Combine(dateFolder, "шаблон.json");
                await _templateService.SaveTemplateAsync(_currentTemplate, autoSavePath);
            }

            if (personsWithMismatch != null && personsWithMismatch.Any() && useLibraryForMismatch.HasValue)
            {
                SaveDeclensionChoiceReport(dateFolder, personsWithMismatch, useLibraryForMismatch.Value);
            }

            string format = (ImageFormatBox.SelectedItem as ComboBoxItem)?.Content.ToString()?.ToLower() ?? "png";

            _progressWindow = new ProgressWindow(_persons.Count);
            int generated = 0;

            // Показываем окно
            _progressWindow.Show();

            // Запускаем генерацию
            var generationTask = Task.Run(() =>
            {
                for (int i = 0; i < _persons.Count; i++)
                {
                    if (_isGenerationCancelled)
                        break;

                    var person = _persons[i];
                    var currentIndex = i + 1;

                    // Обновляем прогресс
                    _progressWindow.Dispatcher.Invoke(() =>
                    {
                        _progressWindow.UpdateProgress(currentIndex, _persons.Count);
                    });

                    string nameToInsert = person.FullNameDative;
                    string fileName = GenerateFileName(person, currentIndex, format);
                    string fullPath = System.IO.Path.Combine(dateFolder, fileName);

                    // Генерируем
                    _imageGenerator.GenerateSingleCertificate(_currentTemplate, nameToInsert, fullPath, format);
                    generated++;
                }
            });

            // Ждём завершения или отмены
            while (!generationTask.IsCompleted && !_isGenerationCancelled)
            {
                await Task.Delay(100);
                // Проверяем, не закрыто ли окно
                if (!_progressWindow.IsVisible)
                {
                    _isGenerationCancelled = true;
                }
            }

            // Если пользователь закрыл окно, отменяем
            if (_isGenerationCancelled && !generationTask.IsCompleted)
            {
                generationTask = null;
            }
            else
            {
                await generationTask;
            }

            _progressWindow.Close();

            string message;
            if (_isGenerationCancelled)
            {
                message = $"⚠️ Генерация прервана!\nСоздано: {generated}\nПапка: {dateFolder}";
            }
            else
            {
                message = $"✅ Генерация завершена!\nСоздано: {generated}\nПапка: {dateFolder}";
            }

            System.Windows.MessageBox.Show(message, "Результат", MessageBoxButton.OK, MessageBoxImage.Information);
            if (generated > 0)
                System.Diagnostics.Process.Start("explorer.exe", dateFolder);
        }

        /// <summary>
        /// Генерация имени файла
        /// </summary>
        private string GenerateFileName(Person person, int index, string format)
        {
            string safeName = person.FullName
                .Replace(' ', '_')
                .Replace('.', '_')
                .Replace(',', '_')
                .Replace('(', '_')
                .Replace(')', '_')
                .Replace('"', '_')
                .Replace('№', '_');

            if (safeName.Length > 50)
                safeName = safeName.Substring(0, 50);

            return $"{index:0000}_{safeName}.{format.ToLower()}";
        }

        /// <summary>
        /// Сохраняет список проблемных ФИО построчно (только исходные ФИО)
        /// </summary>
        private void SaveMismatchedPersonsList(List<Person> personsWithMismatch)
        {
            string outputFolder = OutputFolderBox.Text;
            if (string.IsNullOrEmpty(outputFolder)) outputFolder = DefaultOutputFolder;

            string dateFolder = System.IO.Path.Combine(outputFolder, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
            Directory.CreateDirectory(dateFolder);

            string mismatchFile = System.IO.Path.Combine(dateFolder, "проблемные_фио.txt");

            using (var writer = new StreamWriter(mismatchFile, false, Encoding.UTF8))
            {
                foreach (var p in personsWithMismatch)
                {
                    writer.WriteLine(p.FullName);
                }
            }

            System.Windows.MessageBox.Show(
                $"📁 Создан файл с проблемными ФИО:\n{mismatchFile}",
                "Файл создан",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            System.Diagnostics.Process.Start("explorer.exe", dateFolder);
        }

        /// <summary>
        /// Сохраняет отчёт о выбранном склонении для проблемных ФИО
        /// </summary>
        private void SaveDeclensionChoiceReport(string dateFolder, List<Person> personsWithMismatch, bool usedLibrary)
        {
            string reportFile = System.IO.Path.Combine(dateFolder, "отчет_о_выборе_склонения.txt");

            using (var writer = new StreamWriter(reportFile, false, Encoding.UTF8))
            {
                writer.WriteLine("╔════════════════════════════════════════════════════════════════════════════════╗");
                writer.WriteLine("║                    Отчёт о выборе варианта склонения                           ║");
                writer.WriteLine("╚════════════════════════════════════════════════════════════════════════════════╝");
                writer.WriteLine();
                writer.WriteLine($"📅 Дата генерации: {DateTime.Now}");
                writer.WriteLine($"🔧 Выбранный метод: {(usedLibrary ? "NPetrovich (библиотечный)" : "Самописный (резервный)")}");
                writer.WriteLine($"📊 Количество проблемных ФИО: {personsWithMismatch.Count}");
                writer.WriteLine();
                writer.WriteLine(new string('═', 60));
                writer.WriteLine();

                foreach (var p in personsWithMismatch)
                {
                    writer.WriteLine($"Исходное: {p.FullName}");
                    writer.WriteLine($"Использовано: {(usedLibrary ? p.FullNameDativeLibrary : p.FullNameDativeFallback)}");
                    writer.WriteLine(new string('─', 40));
                }
            }
        }

        private void BlocksListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _isResizeMode = false;
            _resizingImage = null;
            _isResizing = false;

            _isUpdatingProperties = true;

            _selectedBlock = BlocksListBox.SelectedItem as TextBlockData;

            if (_selectedImage != null)
            {
                _selectedImage = null;
                ImagesListBox.SelectedItem = null;
            }

            if (_selectedBlock != null)
            {
                // Показываем свойства текста, скрываем свойства изображений
                ShowTextProperties();

                TextPropertyBox.IsEnabled = true;
                FontFamilyBox.IsEnabled = true;
                FontSizeBox.IsEnabled = true;
                FontSizeUp.IsEnabled = true;
                FontSizeDown.IsEnabled = true;
                BoldToggle.IsEnabled = true;
                ItalicToggle.IsEnabled = true;

                ImageWidthBox.IsEnabled = false;
                ImageHeightBox.IsEnabled = false;

                TextPropertyBox.Text = _selectedBlock.Text;
                FontFamilyBox.SelectedItem = _selectedBlock.FontFamily;
                FontSizeBox.Text = _selectedBlock.FontSize.ToString();
                BoldToggle.IsChecked = _selectedBlock.IsBold;
                ItalicToggle.IsChecked = _selectedBlock.IsItalic;
                UnderlineToggle.IsChecked = _selectedBlock.IsUnderline;

                if (!_isDraggingBlock)
                {
                    PositionXBox.Text = _selectedBlock.PositionX.ToString();
                    PositionYBox.Text = _selectedBlock.PositionY.ToString();
                }

                CenterYes.IsChecked = _selectedBlock.CenterAtGeneration;
                CenterNo.IsChecked = !_selectedBlock.CenterAtGeneration;
            }

            _isUpdatingProperties = false;
            RefreshPreview();
        }


        private void Position_Changed(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingProperties || _isUpdatingImageProperties) return;
            if (_isDraggingBlock || _isDraggingImage) return;

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
                // Используем РЕАЛЬНУЮ ширину текстового блока
                double blockWidth = _selectedBlock.ActualWidth;
                double blockHeight = _selectedBlock.ActualHeight;

                if (double.TryParse(PositionXBox.Text, out double x))
                {
                    _selectedBlock.PositionX = Math.Clamp(x, 0, realWidth - blockWidth);
                    PositionXBox.Text = _selectedBlock.PositionX.ToString("F0"); // Корректируем
                }

                if (double.TryParse(PositionYBox.Text, out double y))
                {
                    _selectedBlock.PositionY = Math.Clamp(y, 0, realHeight - blockHeight);
                    PositionYBox.Text = _selectedBlock.PositionY.ToString("F0"); // Корректируем
                }

                RefreshPreview();
            }
            else if (_selectedImage != null)
            {
                if (double.TryParse(PositionXBox.Text, out double x))
                {
                    _selectedImage.PositionX = Math.Clamp(x, 0, realWidth - _selectedImage.Width);
                }

                if (double.TryParse(PositionYBox.Text, out double y))
                {
                    _selectedImage.PositionY = Math.Clamp(y, 0, realHeight - _selectedImage.Height);
                }

                var imageElement = PreviewCanvas.Children
                    .OfType<System.Windows.Controls.Image>()
                    .FirstOrDefault(img => img.Tag == _selectedImage);

                if (imageElement != null)
                {
                    Canvas.SetLeft(imageElement, _selectedImage.PositionX * _currentScale);
                    Canvas.SetTop(imageElement, _selectedImage.PositionY * _currentScale);
                }

                if (_isResizeMode && _resizingImage == _selectedImage)
                {
                    UpdateResizeHandles(_selectedImage);
                }
            }
        }

        private void UpdateTextBlockActualSize(TextBlockData block)
        {
            // Временно создаём TextBlock для измерения
            var tempTextBlock = new System.Windows.Controls.TextBlock
            {
                Text = block.Text,
                FontFamily = new System.Windows.Media.FontFamily(block.FontFamily),
                FontSize = block.FontSize,
                FontWeight = block.IsBold ? FontWeights.Bold : FontWeights.Normal,
                FontStyle = block.IsItalic ? FontStyles.Italic : FontStyles.Normal,
                TextWrapping = TextWrapping.Wrap
            };

            double maxWidth = 800 * 0.8; // Максимальная ширина относительно фона
            tempTextBlock.Measure(new System.Windows.Size(maxWidth, double.PositiveInfinity));

            block.ActualWidth = tempTextBlock.DesiredSize.Width;
            block.ActualHeight = tempTextBlock.DesiredSize.Height;
        }

        private void AddBlockButton_Click(object sender, RoutedEventArgs e)
        {
            double realWidth = 800;
            double realHeight = 600;

            if (!string.IsNullOrEmpty(_currentTemplate.BackgroundPath) && File.Exists(_currentTemplate.BackgroundPath))
            {
                var tempImage = LoadBitmapImage(_currentTemplate.BackgroundPath);
                realWidth = tempImage.Width;
                realHeight = tempImage.Height;
            }

            int nextNumber = _currentTemplate.TextBlocks.Count + 1;

            var newBlock = new TextBlockData
            {
                Id = Guid.NewGuid().ToString(),
                Text = $"Текстовый блок {nextNumber}",
                PositionX = realWidth / 2 - 100,
                PositionY = realHeight / 2 - 20,
                FontSize = 24,
                FontFamily = "Times New Roman"
            };

            _currentTemplate.TextBlocks.Add(newBlock);
            RefreshBlocksList();
            RefreshPreview();
        }

        // Удаление текстового блока
        private void DeleteTextBlock_Click(object sender, RoutedEventArgs e)
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
                UpdateTextBlockActualSize(_selectedBlock); // ОБНОВЛЯЕМ РАЗМЕРЫ!

                // Обновляем отображение в списке без пересоздания
                var listBox = BlocksListBox;
                if (listBox != null && listBox.ItemsSource != null)
                {
                    // Обновляем только текущий элемент
                    var collection = listBox.ItemsSource as System.Collections.ObjectModel.ObservableCollection<TextBlockData>;
                    if (collection != null)
                    {
                        int index = collection.IndexOf(_selectedBlock);
                        if (index >= 0)
                        {
                            collection[index] = _selectedBlock; // принудительное обновление
                        }
                    }
                }

                RefreshPreview();
            }
        }


        private void FontProperty_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingProperties) return;
            if (_selectedBlock != null && FontFamilyBox.SelectedItem != null)
            {
                _selectedBlock.FontFamily = FontFamilyBox.SelectedItem.ToString() ?? "Times New Roman";
                UpdateTextBlockActualSize(_selectedBlock); // ОБНОВЛЯЕМ РАЗМЕРЫ!
                RefreshPreview();
            }
        }

        private void FontProperty_Changed(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingProperties) return;
            if (_selectedBlock != null && float.TryParse(FontSizeBox.Text, out float size))
            {
                _selectedBlock.FontSize = size;
                UpdateTextBlockActualSize(_selectedBlock); // ОБНОВЛЯЕМ РАЗМЕРЫ!
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
                _selectedBlock.IsUnderline = UnderlineToggle.IsChecked ?? false;  // ДОБАВЬТЕ ЭТУ СТРОКУ
                RefreshPreview();
            }
        }

        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var textBlock = sender as System.Windows.Controls.TextBlock;
            if (textBlock != null && textBlock.Tag is TextBlockData block)
            {
                if (_selectedBlock != block)
                {
                    _selectedBlock = block;
                    BlocksListBox.SelectedItem = block;
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

            double deltaX = (currentPoint.X - _dragStartPointCanvas.X) / _currentScale;
            double deltaY = (currentPoint.Y - _dragStartPointCanvas.Y) / _currentScale;

            double newRealX = _dragStartPointBlock.X + deltaX;
            double newRealY = _dragStartPointBlock.Y + deltaY;

            // Получаем размеры фона
            double realWidth = 800;
            double realHeight = 600;

            if (!string.IsNullOrEmpty(_currentTemplate.BackgroundPath) && File.Exists(_currentTemplate.BackgroundPath))
            {
                var tempImage = LoadBitmapImage(_currentTemplate.BackgroundPath);
                realWidth = tempImage.Width;
                realHeight = tempImage.Height;
            }

            // Используем РЕАЛЬНУЮ ширину текстового блока (как у изображений!)
            double blockWidth = _draggedBlockData.ActualWidth;
            double blockHeight = _draggedBlockData.ActualHeight;

            // ТАКИЕ ЖЕ ОГРАНИЧЕНИЯ, КАК У ИЗОБРАЖЕНИЙ
            _draggedBlockData.PositionX = Math.Clamp(newRealX, 0, realWidth - blockWidth);
            _draggedBlockData.PositionY = Math.Clamp(newRealY, 0, realHeight - blockHeight);

            Canvas.SetLeft(textBlock, _draggedBlockData.PositionX * _currentScale);
            Canvas.SetTop(textBlock, _draggedBlockData.PositionY * _currentScale);

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
                if (_selectedImage != imageBlock)
                {
                    _selectedImage = imageBlock;
                    ImagesListBox.SelectedItem = imageBlock;
                    // Обновляем поля свойств при выборе
                    PositionXBox.Text = imageBlock.PositionX.ToString("F0");
                    PositionYBox.Text = imageBlock.PositionY.ToString("F0");
                    ImageWidthBox.Text = imageBlock.Width.ToString("F0");
                    ImageHeightBox.Text = imageBlock.Height.ToString("F0");

                    // Если были маркеры - удаляем их
                    if (_isResizeMode)
                    {
                        _isResizeMode = false;
                        _resizingImage = null;
                        var handles = PreviewCanvas.Children
                            .OfType<Border>()
                            .Where(b => b.Tag is Tuple<ImageBlockData, string>)
                            .ToList();
                        foreach (var handle in handles)
                            PreviewCanvas.Children.Remove(handle);
                    }
                }

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

            // Вычисляем смещение
            double deltaX = (currentPoint.X - _dragStartPointImageCanvas.X) / _currentScale;
            double deltaY = (currentPoint.Y - _dragStartPointImageCanvas.Y) / _currentScale;

            double newX = _dragStartPointImageReal.X + deltaX;
            double newY = _dragStartPointImageReal.Y + deltaY;

            // Получаем размеры фона
            double realWidth = 800, realHeight = 600;
            if (!string.IsNullOrEmpty(_currentTemplate.BackgroundPath) && File.Exists(_currentTemplate.BackgroundPath))
            {
                var tempImage = LoadBitmapImage(_currentTemplate.BackgroundPath);
                realWidth = tempImage.Width;
                realHeight = tempImage.Height;
            }

            // Ограничиваем перемещение
            _draggedImage.PositionX = Math.Clamp(newX, 0, realWidth - _draggedImage.Width);
            _draggedImage.PositionY = Math.Clamp(newY, 0, realHeight - _draggedImage.Height);

            // Обновляем позицию изображения на Canvas
            Canvas.SetLeft(image, _draggedImage.PositionX * _currentScale);
            Canvas.SetTop(image, _draggedImage.PositionY * _currentScale);

            // ОБНОВЛЯЕМ ПОЛЯ СВОЙСТВ в реальном времени
            if (_selectedImage == _draggedImage)
            {
                PositionXBox.Text = _draggedImage.PositionX.ToString("F0");
                PositionYBox.Text = _draggedImage.PositionY.ToString("F0");
            }

            // Обновляем маркеры, если они есть
            if (_isResizeMode && _resizingImage == _draggedImage)
            {
                UpdateResizeHandles(_draggedImage);
            }
        }

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingImage = false;

            var image = sender as System.Windows.Controls.Image;
            if (image != null)
                image.ReleaseMouseCapture();

            // Убеждаемся, что поля свойств обновлены финальными значениями
            if (_draggedImage != null && _selectedImage == _draggedImage)
            {
                PositionXBox.Text = _draggedImage.PositionX.ToString("F0");
                PositionYBox.Text = _draggedImage.PositionY.ToString("F0");
            }

            _draggedImage = null;
        }

        private void ImagesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedImage = ImagesListBox.SelectedItem as ImageBlockData;

            if (_selectedBlock != null)
            {
                _selectedBlock = null;
                BlocksListBox.SelectedItem = null;
            }

            if (_selectedImage != null)
            {
                // Показываем свойства изображения, скрываем свойства текста
                ShowImageProperties();

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

        private void AddResizeHandles(ImageBlockData image)
        {
            if (image == null) return;

            // Удаляем старые маркеры для этого изображения
            var oldHandles = PreviewCanvas.Children
                .OfType<Border>()
                .Where(b => b.Tag is Tuple<ImageBlockData, string> tuple && tuple.Item1 == image)
                .ToList();

            foreach (var oldHandle in oldHandles)
            {
                PreviewCanvas.Children.Remove(oldHandle);
            }

            double left = image.PositionX * _currentScale;
            double top = image.PositionY * _currentScale;
            double width = image.Width * _currentScale;
            double height = image.Height * _currentScale;
            double handleSize = 12;

            var corners = new[] { "tl", "tr", "bl", "br" };
            var positions = new (double left, double top)[]
            {
        (left - handleSize/2, top - handleSize/2),
        (left + width - handleSize/2, top - handleSize/2),
        (left - handleSize/2, top + height - handleSize/2),
        (left + width - handleSize/2, top + height - handleSize/2)
            };

            for (int i = 0; i < corners.Length; i++)
            {
                var handle = new Border
                {
                    Width = handleSize,
                    Height = handleSize,
                    Background = Brushes.White,
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(handleSize / 2),
                    Tag = new Tuple<ImageBlockData, string>(image, corners[i])
                };

                handle.MouseLeftButtonDown += ResizeHandle_MouseLeftButtonDown;
                handle.MouseMove += ResizeHandle_MouseMove;
                handle.MouseLeftButtonUp += ResizeHandle_MouseLeftButtonUp;
                handle.Cursor = GetResizeCursor(corners[i]);

                Canvas.SetLeft(handle, positions[i].left);
                Canvas.SetTop(handle, positions[i].top);
                Panel.SetZIndex(handle, 100);
                PreviewCanvas.Children.Add(handle);
            }
        }

        private Cursor GetResizeCursor(string handle)
        {
            return handle switch
            {
                "tl" or "br" => Cursors.SizeNWSE,
                "tr" or "bl" => Cursors.SizeNESW,
                _ => Cursors.Arrow
            };
        }

        private void ResizeHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var handle = sender as Border;
            if (handle?.Tag is Tuple<ImageBlockData, string> data)
            {
                _isResizing = true;
                _resizeHandle = data.Item2;
                _resizingImage = data.Item1;
                _resizeStartPoint = e.GetPosition(PreviewCanvas);
                _resizeStartWidth = _resizingImage.Width;
                _resizeStartHeight = _resizingImage.Height;
                _resizeStartX = _resizingImage.PositionX;
                _resizeStartY = _resizingImage.PositionY;

                handle.CaptureMouse();
                e.Handled = true;
            }
        }

        private void ResizeHandle_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isResizing || _resizingImage == null) return;
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                _isResizing = false;
                return;
            }

            Point currentPoint = e.GetPosition(PreviewCanvas);
            double deltaX = (currentPoint.X - _resizeStartPoint.X) / _currentScale;
            double deltaY = (currentPoint.Y - _resizeStartPoint.Y) / _currentScale;

            double aspect = _resizeStartWidth / _resizeStartHeight;
            double newWidth = _resizeStartWidth;
            double newHeight = _resizeStartHeight;
            double newX = _resizeStartX;
            double newY = _resizeStartY;

            switch (_resizeHandle)
            {
                case "tl":
                    newWidth = _resizeStartWidth - deltaX;
                    newHeight = newWidth / aspect;
                    newX = _resizeStartX + (_resizeStartWidth - newWidth);
                    newY = _resizeStartY + (_resizeStartHeight - newHeight);
                    break;
                case "tr":
                    newWidth = _resizeStartWidth + deltaX;
                    newHeight = newWidth / aspect;
                    newX = _resizeStartX;
                    newY = _resizeStartY + (_resizeStartHeight - newHeight);
                    break;
                case "bl":
                    newWidth = _resizeStartWidth - deltaX;
                    newHeight = newWidth / aspect;
                    newX = _resizeStartX + (_resizeStartWidth - newWidth);
                    newY = _resizeStartY;
                    break;
                case "br":
                    newWidth = _resizeStartWidth + deltaX;
                    newHeight = newWidth / aspect;
                    newX = _resizeStartX;
                    newY = _resizeStartY;
                    break;
            }

            newWidth = Math.Max(20, newWidth);
            newHeight = Math.Max(20, newHeight);

            double realWidth = 800;
            double realHeight = 600;
            if (!string.IsNullOrEmpty(_currentTemplate.BackgroundPath) && File.Exists(_currentTemplate.BackgroundPath))
            {
                var tempImage = LoadBitmapImage(_currentTemplate.BackgroundPath);
                realWidth = tempImage.Width;
                realHeight = tempImage.Height;
            }

            newX = Math.Clamp(newX, 0, realWidth - newWidth);
            newY = Math.Clamp(newY, 0, realHeight - newHeight);

            _resizingImage.Width = newWidth;
            _resizingImage.Height = newHeight;
            _resizingImage.PositionX = newX;
            _resizingImage.PositionY = newY;

            var imageElement = PreviewCanvas.Children
                .OfType<System.Windows.Controls.Image>()
                .FirstOrDefault(img => img.Tag == _resizingImage);

            if (imageElement != null)
            {
                imageElement.Width = newWidth * _currentScale;
                imageElement.Height = newHeight * _currentScale;
                Canvas.SetLeft(imageElement, newX * _currentScale);
                Canvas.SetTop(imageElement, newY * _currentScale);
            }

            UpdateResizeHandles(_resizingImage);

            if (_selectedImage == _resizingImage)
            {
                ImageWidthBox.Text = newWidth.ToString("F0");
                ImageHeightBox.Text = newHeight.ToString("F0");
                PositionXBox.Text = newX.ToString("F0");
                PositionYBox.Text = newY.ToString("F0");
            }
        }

        private void UpdateResizeHandles(ImageBlockData image)
        {
            // Находим все маркеры для этого изображения
            var handles = PreviewCanvas.Children
                .OfType<Border>()
                .Where(b => b.Tag is Tuple<ImageBlockData, string> tuple && tuple.Item1 == image)
                .ToList();

            if (handles.Count == 0) return;

            double left = image.PositionX * _currentScale;
            double top = image.PositionY * _currentScale;
            double width = image.Width * _currentScale;
            double height = image.Height * _currentScale;
            double handleSize = 12;

            // Обновляем позиции маркеров
            var positions = new (double left, double top)[]
            {
        (left - handleSize/2, top - handleSize/2),           // top-left
        (left + width - handleSize/2, top - handleSize/2),   // top-right
        (left - handleSize/2, top + height - handleSize/2),  // bottom-left
        (left + width - handleSize/2, top + height - handleSize/2) // bottom-right
            };

            for (int i = 0; i < handles.Count && i < positions.Length; i++)
            {
                Canvas.SetLeft(handles[i], positions[i].left);
                Canvas.SetTop(handles[i], positions[i].top);
            }
        }


        private void ResizeHandle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isResizing = false;
            _isResizeMode = false;
            _resizingImage = null;

            var handle = sender as Border;
            handle?.ReleaseMouseCapture();

            // Удаляем ВСЕ маркеры
            var allHandles = PreviewCanvas.Children
                .OfType<Border>()
                .Where(b => b.Tag is Tuple<ImageBlockData, string>)
                .ToList();

            foreach (var h in allHandles)
            {
                PreviewCanvas.Children.Remove(h);
            }

            // Обновляем поля свойств
            if (_selectedImage != null)
            {
                ImageWidthBox.Text = _selectedImage.Width.ToString("F0");
                ImageHeightBox.Text = _selectedImage.Height.ToString("F0");
                PositionXBox.Text = _selectedImage.PositionX.ToString("F0");
                PositionYBox.Text = _selectedImage.PositionY.ToString("F0");
            }
        }

        private void EnterResizeMode_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedImage == null) return;

            _isResizeMode = true;
            _resizingImage = _selectedImage;
            AddResizeHandles(_selectedImage); // Добавляем маркеры без перерисовки всего Canvas
        }

        private void DeleteImage_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedImage != null)
            {
                _currentTemplate.ImageBlocks.Remove(_selectedImage);
                _selectedImage = null;
                _resizingImage = null;
                _isResizeMode = false;
                RefreshImagesList();
                RefreshPreview();
            }
        }

        private void PreviewCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Проверяем, был ли клик по маркеру или изображению
            var hitElement = e.OriginalSource as DependencyObject;
            bool isHandleClick = false;
            bool isImageClick = false;

            while (hitElement != null && hitElement != PreviewCanvas)
            {
                if (hitElement is Border && (hitElement as Border)?.Tag is Tuple<ImageBlockData, string>)
                {
                    isHandleClick = true;
                    break;
                }
                if (hitElement is System.Windows.Controls.Image)
                {
                    isImageClick = true;
                    break;
                }
                hitElement = VisualTreeHelper.GetParent(hitElement);
            }

            // Если кликнули не по маркеру и не по изображению - удаляем маркеры
            if (!isHandleClick && !isImageClick && _isResizeMode)
            {
                _isResizeMode = false;
                _resizingImage = null;
                _isResizing = false;

                // Удаляем все маркеры
                var allHandles = PreviewCanvas.Children
                    .OfType<Border>()
                    .Where(b => b.Tag is Tuple<ImageBlockData, string>)
                    .ToList();

                foreach (var handle in allHandles)
                {
                    PreviewCanvas.Children.Remove(handle);
                }
            }
        }
        // Общий метод для выбора элемента при правом клике в любом ListBox
        private void ListBox_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var listBox = sender as System.Windows.Controls.ListBox;
            if (listBox == null) return;

            // Находим элемент, на который кликнули
            var item = ItemsControl.ContainerFromElement(listBox, e.OriginalSource as DependencyObject) as ListBoxItem;
            if (item != null)
            {
                // Выбираем этот элемент
                item.IsSelected = true;
                e.Handled = true; // Чтобы событие не пошло дальше
            }
        }

        private void ShowTextProperties()
        {
            TextPropertiesPanel.Visibility = Visibility.Visible;
            ImagePropertiesPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowImageProperties()
        {
            TextPropertiesPanel.Visibility = Visibility.Collapsed;
            ImagePropertiesPanel.Visibility = Visibility.Visible;
        }

        private void CenterAtGeneration_Changed(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingProperties) return;
            if (_selectedBlock == null) return;

            _selectedBlock.CenterAtGeneration = CenterYes.IsChecked == true;
        }
    }

    public class BackgroundItem
    {
        public string FilePath { get; set; } = string.Empty;
        public ImageSource? Thumbnail { get; set; }
        public bool IsBuiltIn { get; set; } = false;
    }

    public class PersonDisplay
    {
        public string FullName { get; set; } = string.Empty;
        public string DisplayNameLibrary { get; set; } = string.Empty;
        public string DisplayNameFallback { get; set; } = string.Empty;
        public bool HasDeclensionMismatch { get; set; } = false;
    }
}