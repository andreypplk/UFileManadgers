
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Storage;
using Core_FileManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Media;
using System.Threading.Tasks;
using CommunityToolkit.WinUI;
using System.Threading;

namespace ufm
{
    public sealed partial class TreePanelPg01 : Page, IDisposable
    {
        #region Поля и свойства

        // Параметры отображения элементов
        private string _selectedSize = "Medium";
        private string _activePanelId = "MainTree"; // Добавьте это в поля класса

        // Сервис для работы с файловой системой
        private readonly FileSystemService _fileSystemService;

        // Менеджер навигации
        private readonly NavigationManager _navigationManager;

        // История навигации для основного TreeView
        private readonly DirectoryHistory _history;

        // История навигации для специальных папок
        private readonly DirectoryHistory _historySpF;

        // Флаг для Dispose
        private bool _disposed = false;

        // Флаги инициализации
        private bool _isTreeViewSpFInitialized = false;
        private bool _isLoadingSpF = false;


        // Флаг выделения (упрощенная блокировка)
        private bool _isNavigationChangingSelection = false;

        // Текущий выделенный элемент
        private string _currentSelectedItemPath;
        public bool ExpandedTreeSelectedSetting
        {
            get
            {
                try
                {
                    // Используем App.SettingsManager для получения настройки
                    return App.SettingsManager?.GetSetting<bool>("ExpandedTreeSelected", true) ?? true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting ExpandedTreeSelected setting: {ex}");
                    return true; // По умолчанию показываем
                }
            }
        }
        public bool ExpanderNodesSFStartsSetting
        {
            get
            {
                try
                {
                    // Используем App.SettingsManager для получения настройки
                    return App.SettingsManager?.GetSetting<bool>("ExpanderNodesSFStarts", true) ?? true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting ExpanderNodesSFStarts setting: {ex}");
                    return true; // По умолчанию показываем
                }
            }
        }
        public bool ExpanderNodesMyPcStartsSetting
        {
            get
            {
                try
                {
                    // Используем App.SettingsManager для получения настройки
                    return App.SettingsManager?.GetSetting<bool>("ExpanderNodesMyPcStarts", true) ?? true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting ExpanderNodesMyPcStarts setting: {ex}");
                    return true; // По умолчанию показываем
                }
            }
        }
        #endregion

        #region Конструктор и инициализация

        public TreePanelPg01()
        {
            this.InitializeComponent();

            // Инициализация сервиса файловой системы
            _fileSystemService = new FileSystemService();

            // Инициализация менеджера навигации
            _navigationManager = new NavigationManager();

            // Инициализация истории навигации для основного TreeView
            _history = new DirectoryHistory("MyComputer", "Мой Компьютер");

            // Инициализация истории навигации для специальных папок
            _historySpF = new DirectoryHistory("SpecialFolders", "Специальные папки");

            // Регистрация панелей в менеджере навигации
            _navigationManager.RegisterPanel("MainTree", _history);
            _navigationManager.RegisterPanel("SpFTree", _historySpF);

            // Подписка на события навигации
            _navigationManager.NavigationChanged += OnNavigationChanged;

            // Подписка на события
            this.Loaded += TreePanelPg01_Loaded;

            // Основной TreeView
            treeView.Loaded += TreeView_OnLoaded;
            treeView.Expanding += TreeView_OnExpanding;
            treeView.Collapsed += TreeView_OnCollapsed;
            treeView.DoubleTapped += TreeView_OnDoubleTapped;
            treeView.SelectionChanged += TreeView_SelectionChanged;
            // TreeView специальных папок
            treeViewSpF.Loaded += TreeViewSpF_OnLoaded;
            treeViewSpF.Expanding += TreeViewSpF_OnExpanding;
            treeViewSpF.Collapsed += TreeViewSpF_OnCollapsed;
            treeViewSpF.DoubleTapped += TreeViewSpF_OnDoubleTapped;
            treeViewSpF.SelectionChanged += TreeViewSpF_SelectionChanged;

        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    // Исправленная строка:
                    this.Loaded -= TreePanelPg01_Loaded;

                    if (_navigationManager != null)
                        _navigationManager.NavigationChanged -= OnNavigationChanged;

                    if (treeView != null)
                    {
                        treeView.Loaded -= TreeView_OnLoaded;
                        treeView.Expanding -= TreeView_OnExpanding;
                        treeView.Collapsed -= TreeView_OnCollapsed;
                        treeView.DoubleTapped -= TreeView_OnDoubleTapped;
                        treeView.SelectionChanged -= TreeView_SelectionChanged;
                    }

                    if (treeViewSpF != null)
                    {
                        treeViewSpF.Loaded -= TreeViewSpF_OnLoaded;
                        treeViewSpF.Expanding -= TreeViewSpF_OnExpanding;
                        treeViewSpF.Collapsed -= TreeViewSpF_OnCollapsed;
                        treeViewSpF.DoubleTapped -= TreeViewSpF_OnDoubleTapped;
                        treeViewSpF.SelectionChanged -= TreeViewSpF_SelectionChanged;
                    }

                    _fileSystemService?.Dispose();
                    _history?.Dispose();
                    _historySpF?.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in Dispose: {ex.Message}");
                }
                finally
                {
                    GC.SuppressFinalize(this);
                    _disposed = true;
                }
            }
        }
        #endregion

        #region Обработчики событий страницы

        private void TreePanelPg01_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.SettingsManager != null)
            {
                _selectedSize = App.SettingsManager.GetSetting<string>("SelectedSizeIconTreeView");
            }

            if (string.IsNullOrEmpty(_selectedSize))
            {
                var localSettings = ApplicationData.Current.LocalSettings.Values;
                if (localSettings?.ContainsKey("SelectedSizeIconTreeView") == true)
                {
                    _selectedSize = localSettings["SelectedSizeIconTreeView"]?.ToString();
                }
            }

            if (string.IsNullOrEmpty(_selectedSize))
            {
                _selectedSize = "Medium";
                Debug.WriteLine("Используются настройки по умолчанию.");
            }
            else
            {
                Debug.WriteLine($"Загружены настройки: Размер = {_selectedSize}");
            }

            SetSelectedRadioButton(_selectedSize);
            UpdateAllTiles();
        }
        #endregion

        #region Управление размерами элементов

        private void SetSelectedRadioButton(string selectedSize)
        {
            ExtraSmallSizeRadioButton.IsChecked = false;
            SmallSizeRadioButton.IsChecked = false;
            MediumSizeRadioButton.IsChecked = false;
            LargeSizeRadioButton.IsChecked = false;
            ExtraLargeSizeRadioButton.IsChecked = false;

            switch (selectedSize)
            {
                case "Extra Small":
                    ExtraSmallSizeRadioButton.IsChecked = true;
                    break;
                case "Small":
                    SmallSizeRadioButton.IsChecked = true;
                    break;
                case "Medium":
                    MediumSizeRadioButton.IsChecked = true;
                    break;
                case "Large":
                    LargeSizeRadioButton.IsChecked = true;
                    break;
                case "Extra Large":
                    ExtraLargeSizeRadioButton.IsChecked = true;
                    break;
            }
        }

        private void SizeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton && radioButton.Tag is string selectedSize)
            {
                _selectedSize = selectedSize;
               
                bool saved = false;
                if (App.SettingsManager != null)
                {
                    try
                    {
                        App.SettingsManager.SaveSetting("SelectedSizeIconTreeView", _selectedSize);
                        saved = true;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Ошибка SettingsManager: {ex.Message}");
                    }
                }

                if (!saved)
                {
                    try
                    {
                        var localSettings = ApplicationData.Current.LocalSettings.Values;
                        localSettings["SelectedSizeIconTreeView"] = _selectedSize;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Ошибка LocalSettings: {ex.Message}");
                    }
                }

                UpdateAllTiles();
            }
        }

        #endregion

        #region Работа с основным TreeView
        //private async void TreeView_OnLoaded(object sender, RoutedEventArgs e)
        //{
        //    // Очищаем существующие узлы и создаем корневой узел "Мой компьютер"
        //    treeView.RootNodes.Clear();

        //    var myComputerNode = new TreeViewNode
        //    {
        //        Content = new ExplorerItemViewModel(_history)
        //        {
        //            Name = "Мой Компьютер",
        //            FilePath = "MyComputer",
        //            ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/computer.png")),
        //            IsTreeViewNode = true
        //        },
        //        IsExpanded = true
        //    };

        //    treeView.RootNodes.Add(myComputerNode);

        //    // СРАЗУ загружаем диски и первый уровень вложенности
        //    DispatcherQueue.TryEnqueue(async () =>
        //    {

        //        UpdateTileSize(myComputerNode);

        //        LoadDrivesSync(myComputerNode);
        //        // Предзагружаем первый уровень для быстрого доступа при навигации
        //        await PreloadFirstLevelAsync(myComputerNode);
        //        ExpandMyComputerNode(myComputerNode);
        //    });
        //}
        private async void TreeView_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Очищаем существующие узлы и создаем корневой узел "Мой компьютер"
            treeView.RootNodes.Clear();

            var myComputerNode = new TreeViewNode
            {
                Content = new ExplorerItemViewModel(_history)
                {
                    Name = "Мой Компьютер",
                    FilePath = "MyComputer",
                    ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/computer.png")),
                    IsTreeViewNode = true
                },
                IsExpanded = ExpanderNodesMyPcStartsSetting, // Используем настройку
                HasUnrealizedChildren = true
            };

            treeView.RootNodes.Add(myComputerNode);

            // СРАЗУ загружаем диски и первый уровень вложенности
            DispatcherQueue.TryEnqueue(async () =>
            {
                UpdateTileSize(myComputerNode);

                //LoadDrivesSync(myComputerNode);
         
                // Раскрываем узел только если это разрешено настройкой
                if (ExpanderNodesMyPcStartsSetting)
                {
                    LoadDrivesSync(myComputerNode);
                    await PreloadFirstLevelAsync(myComputerNode);

                    ExpandMyComputerNode(myComputerNode);
                    Debug.WriteLine("Основной TreeView: узел 'Мой компьютер' раскрыт (настройка разрешает)");
                }
                else
                {
                    myComputerNode.IsExpanded = false;
                    Debug.WriteLine("Основной TreeView: узел 'Мой компьютер' не раскрыт (настройка запрещает)");
                }
            });
        }

        private async Task PreloadFirstLevelAsync(TreeViewNode myComputerNode)
        {
            foreach (var driveNode in myComputerNode.Children)
            {
                if (driveNode.Content is ExplorerItemViewModel driveItem && Directory.Exists(driveItem.FilePath))
                {
                    try
                    {
                        // Используем СУЩЕСТВУЮЩИЙ метод загрузки подпапок
                        var firstLevelItems = await _fileSystemService.LoadFoldersOnlyAsync(driveItem.FilePath, _history);

                        // Добавляем первые несколько папок для быстрого доступа
                        foreach (var item in firstLevelItems.Take(3))
                        {
                            item.IsTreeViewNode = true;
                            var node = new TreeViewNode
                            {
                                Content = item,
                                HasUnrealizedChildren = true
                            };
                            driveNode.Children.Add(node);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Ошибка предзагрузки для {driveItem.FilePath}: {ex.Message}");
                    }
                }
            }
        }


        private void ExpandMyComputerNode(TreeViewNode myComputerNode)
        {
            try
            {
                if (myComputerNode.HasUnrealizedChildren || myComputerNode.Children.Count == 0)
                {
                    myComputerNode.HasUnrealizedChildren = false;
                    LoadDrivesSync(myComputerNode);

                    // Убеждаемся, что узел раскрыт - как в SpF
                    if (ExpanderNodesMyPcStartsSetting) // Проверяем настройку
                    {
                        myComputerNode.IsExpanded = true;
                        Debug.WriteLine("Основной TreeView: узел 'Мой компьютер' раскрыт (настройка разрешает)");
                    }
                    else
                    {
                        myComputerNode.IsExpanded = false;
                        Debug.WriteLine("Основной TreeView: узел 'Мой компьютер' не раскрыт (настройка запрещает)");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при раскрытии узла 'Мой компьютер': {ex.Message}");
            }
            UpdateTileSize(myComputerNode);
        }

        private async void TreeView_OnExpanding(TreeView sender, TreeViewExpandingEventArgs args)
        {
            if (args.Node.Content is not ExplorerItemViewModel treeItem)
                return;

            try
            {
                if (treeItem.Name == "Мой Компьютер" && (args.Node.HasUnrealizedChildren || args.Node.Children.Count == 0))
                {
                    args.Node.HasUnrealizedChildren = false;
                    LoadDrivesSync(args.Node);
                    UpdateAllTiles();
                }
                else if (args.Node.HasUnrealizedChildren || args.Node.Children.Count == 0)
                {
                    args.Node.HasUnrealizedChildren = false;
                    await LoadSubfoldersAsync(args.Node, treeItem.FilePath);
                    UpdateAllTiles();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при раскрытии узла: {ex.Message}");
            }
        }

        private void TreeView_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (treeView.SelectedNode == null) return;

            if (!treeView.SelectedNode.IsExpanded)
            {
                treeView.Expand(treeView.SelectedNode);
            }
            else
            {
                treeView.Collapse(treeView.SelectedNode);
            }
        }

        private void TreeView_OnCollapsed(TreeView sender, TreeViewCollapsedEventArgs args)
        {
            if (args.Node.HasChildren && args.Node.Children.Count > 0)
            {
                args.Node.Children.Clear();
                args.Node.HasUnrealizedChildren = true;
            }
        }

        #endregion

        #region Работа с TreeView специальных папок

        private void TreeViewSpF_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_isTreeViewSpFInitialized) return;

            treeViewSpF.ItemsSource = null;
            treeViewSpF.RootNodes.Clear();

            var specialFoldersNode = new TreeViewNode
            {
                Content = _fileSystemService.CreateSpecialFoldersItem(_historySpF),
                IsExpanded = ExpanderNodesSFStartsSetting, // Используем настройку
                HasUnrealizedChildren = true
            };

            treeViewSpF.RootNodes.Add(specialFoldersNode);
            _isTreeViewSpFInitialized = true;
            UpdateAllTiles();

            // СРАЗУ загружаем содержимое и предзагружаем
            DispatcherQueue.TryEnqueue(async () =>
            {
                await LoadHomeContentsAsync(specialFoldersNode);

                // Предзагружаем первый уровень для быстрого доступа при навигации
                await PreloadFirstLevelForSpFAsync(specialFoldersNode);

                // Раскрываем узел если это нужно по настройке
                if (ExpanderNodesSFStartsSetting)
                {
                    ExpandSpecialFoldersNode(specialFoldersNode);
                }
                else
                {
                    Debug.WriteLine("TreeView специальных папок: корневой узел не раскрыт (настройка)");
                }
            });
        }
        private async Task PreloadFirstLevelForSpFAsync(TreeViewNode specialFoldersNode)
        {
            foreach (var systemFolderNode in specialFoldersNode.Children)
            {
                if (systemFolderNode.Content is ExplorerItemViewModel folderItem && Directory.Exists(folderItem.FilePath))
                {
                    try
                    {
                        // Используем СУЩЕСТВУЮЩИЙ метод загрузки подпапок
                        var firstLevelItems = await _fileSystemService.LoadFoldersOnlyAsync(folderItem.FilePath, _historySpF);

                        // Добавляем первые несколько папок для быстрого доступа
                        foreach (var item in firstLevelItems.Take(3))
                        {
                            item.IsTreeViewNode = true; // ДОБАВЛЕНО IsTreeViewNode = true
                            var node = new TreeViewNode
                            {
                                Content = item,
                                HasUnrealizedChildren = true
                            };
                            systemFolderNode.Children.Add(node);
                            UpdateTileSizeSpF(node);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Ошибка предзагрузки для {folderItem.FilePath}: {ex.Message}");
                    }
                }
            }
        }
        private void ExpandSpecialFoldersNode(TreeViewNode specialFoldersNode)
        {
            try
            {
                if (specialFoldersNode.HasUnrealizedChildren || specialFoldersNode.Children.Count == 0)
                {
                    specialFoldersNode.HasUnrealizedChildren = false;

                    // Синхронный вызов асинхронного метода
                 
                    // Раскрываем узел только если это разрешено настройкой
                    if (ExpanderNodesSFStartsSetting)
                    {
                        specialFoldersNode.IsExpanded = true;
                        Debug.WriteLine("TreeView специальных папок: корневой узел раскрыт (настройка разрешает)");
                    }
                    else
                    {
                        specialFoldersNode.IsExpanded = false;
                        Debug.WriteLine("TreeView специальных папок: корневой узел не раскрыт (настройка запрещает)");
                    }

                    UpdateAllTiles();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при раскрытии узла специальных папок: {ex.Message}");
            }
        }

        private async void TreeViewSpF_OnExpanding(TreeView sender, TreeViewExpandingEventArgs args)
        {
            if (_isLoadingSpF || args.Node.Content is not ExplorerItemViewModel treeItem)
                return;

            try
            {
                _isLoadingSpF = true;

                // Если это корневой узел специальных папок и у него нет дочерних элементов
                if (treeItem.FilePath == "SpecialFolders" && args.Node.Children.Count == 0)
                {
                    Debug.WriteLine("Раскрытие корневого узла специальных папок");
                    args.Node.HasUnrealizedChildren = false;
                    await LoadHomeContentsAsync(args.Node);
                    UpdateAllTiles();
                }
                // Если это обычная папка и у нее нет дочерних элементов
                else if (Directory.Exists(treeItem.FilePath) && args.Node.Children.Count == 0)
                {
                    Debug.WriteLine($"Раскрытие папки: {treeItem.FilePath}");
                    args.Node.HasUnrealizedChildren = false;
                    await LoadSubfoldersForSpFAsync(args.Node, treeItem.FilePath);
                    UpdateAllTiles();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при раскрытии узла специальных папок: {ex.Message}");
            }
            finally
            {
                _isLoadingSpF = false;
            }
            UpdateAllTiles();
        }

        // Асинхронная версия для событий Expanding
        private async Task LoadHomeContentsAsync(TreeViewNode parentNode)
        {
            try
            {
                // ОЧИЩАЕМ существующие дочерние узлы
                parentNode.Children.Clear();

                Debug.WriteLine("LoadHomeContentsAsync начал выполнение");

                // Используем LoadHomeAsync для загрузки системных папок
                var homeItems = await _fileSystemService.LoadHomeAsync("TreeViewSpF", _historySpF);

                Debug.WriteLine($"LoadHomeAsync завершен, получено {homeItems.Count} элементов");

                foreach (var item in homeItems)
                {
                    item.IsTreeViewNode = true; // ДОБАВЛЕНО IsTreeViewNode = true
                    var node = new TreeViewNode
                    {
                        Content = item,
                        HasUnrealizedChildren = Directory.Exists(item.FilePath)
                    };

                    parentNode.Children.Add(node);
                    UpdateTileSizeSpF(node);
                }

                Debug.WriteLine("LoadHomeContentsAsync завершен успешно");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки домашнего содержимого: {ex.Message}");
            }
        }

        private void TreeViewSpF_OnCollapsed(TreeView sender, TreeViewCollapsedEventArgs args)
        {
            // Очищаем дочерние элементы при сворачивании для оптимизации
            // Не очищаем корневой узел "Специальные папки"
            if (args.Node.HasChildren && args.Node.Children.Count > 0 &&
                args.Node.Content is ExplorerItemViewModel item &&
                item.FilePath != "SpecialFolders")
            {
                args.Node.Children.Clear();
                args.Node.HasUnrealizedChildren = true;
                Debug.WriteLine($"Свернут узел: {item.FilePath}, дочерние элементы очищены");
            }
        }

        private void TreeViewSpF_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (treeViewSpF.SelectedNode == null) return;

            if (!treeViewSpF.SelectedNode.IsExpanded)
            {
                treeViewSpF.Expand(treeViewSpF.SelectedNode);
            }
            else
            {
                treeViewSpF.Collapse(treeViewSpF.SelectedNode);
            }
        }

        #endregion

        #region Методы работы с узлами TreeView

        private void LoadDrivesSync(TreeViewNode parentNode)
        {
            try
            {
                // Используем FileSystemService для загрузки дисков
                var driveItems = _fileSystemService.LoadDrivesSync(_history);

                foreach (var driveItem in driveItems)
                {
                    driveItem.IsTreeViewNode = true;
                    var driveNode = new TreeViewNode
                    {
                        Content = driveItem,
                        HasUnrealizedChildren = true // У дисков могут быть подпапки
                    };

                    parentNode.Children.Add(driveNode);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки дисков через FileSystemService: {ex.Message}");
            }
        }

        private async Task LoadSubfoldersAsync(TreeViewNode parentNode, string folderPath)
        {
            try
            {
                // Используем FileSystemService для загрузки подпапок
                var folderItems = await _fileSystemService.LoadSubfoldersForTreeViewAsync(folderPath, _history);

                foreach (var item in folderItems)
                {
                    item.IsTreeViewNode = true;
                    var node = new TreeViewNode
                    {
                        Content = item,
                        HasUnrealizedChildren = true // Указываем, что могут быть вложенные папки
                    };

                    parentNode.Children.Add(node);
                    UpdateTileSize(node);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки подпапок через FileSystemService: {ex.Message}");
            }
        }

        private async Task LoadSubfoldersForSpFAsync(TreeViewNode parentNode, string folderPath)
        {
            try
            {
                // Используем FileSystemService для загрузки подпапок
                var folderItems = await _fileSystemService.LoadFoldersOnlyAsync(folderPath, _historySpF);

                foreach (var item in folderItems)
                {
                    item.IsTreeViewNode = true; // ДОБАВЛЕНО IsTreeViewNode = true
                    var node = new TreeViewNode
                    {
                        Content = item,
                        HasUnrealizedChildren = true
                    };

                    parentNode.Children.Add(node);
                    UpdateTileSizeSpF(node);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки подпапок для специальных папок: {ex.Message}");
            }
        }
        private bool IsItemInTreeView(IList<TreeViewNode> nodes, ExplorerItemViewModel targetItem)
        {
            if (nodes == null) return false;

            foreach (var node in nodes)
            {
                if (node?.Content == targetItem)
                    return true;

                if (node?.Children?.Count > 0)
                {
                    if (IsItemInTreeView(node.Children, targetItem))
                        return true;
                }
            }
            return false;
        }
        #endregion

        #region Обновление отображения элементов
        private async void UpdateTreeViewSelection(string panelId, string path)
        {
            try
            {
                TreeView targetTreeView = panelId == "MainTree" ? treeView : treeViewSpF;
                if (targetTreeView == null || string.IsNullOrEmpty(path)) return;

                _isNavigationChangingSelection = true;

                Debug.WriteLine($"=== UpdateTreeViewSelection START ===");
                Debug.WriteLine($"Panel: {panelId}, Path: {path}");

                // Логируем корневые узлы для отладки
                Debug.WriteLine($"Root nodes count: {targetTreeView.RootNodes.Count}");
                foreach (var rootNode in targetTreeView.RootNodes)
                {
                    if (rootNode?.Content is ExplorerItemViewModel rootItem)
                    {
                        Debug.WriteLine($"Root node: '{rootItem.FilePath}' -> '{rootItem.Name}'");

                        // Убеждаемся, что узел "Мой компьютер" раскрыт и диски загружены
                        if (rootItem.FilePath == "MyComputer")
                        {
                            await EnsureMyComputerExpanded(rootNode);
                        }
                    }
                }

                // Если путь виртуальный (MyComputer, SpecialFolders)
                if (IsVirtualPath(path))
                {
                    Debug.WriteLine($"Virtual path detected: {path}");
                    var targetItem = FindItemByPath(targetTreeView.RootNodes, path);
                    if (targetItem != null)
                    {
                        targetTreeView.SelectedItem = targetItem;
                        _currentSelectedItemPath = targetItem.FilePath;
                        _activePanelId = panelId;
                        Debug.WriteLine($"Virtual path selected: {path}");
                    }
                    return;
                }

                // Для реальных путей разбиваем на сегменты и раскрываем рекурсивно
                var pathSegments = SplitPath(path);
                Debug.WriteLine($"Path segments: {string.Join(" | ", pathSegments)}");

                if (pathSegments.Length == 0) return;

                // Начинаем с корневых узлов
                bool found = await ExpandAndSelectPath(targetTreeView.RootNodes, pathSegments, 0, panelId);

                if (!found)
                {
                    Debug.WriteLine($"Path not found after recursive search: {path}");
                    // Попробуем простой поиск
                    var simpleItem = FindItemByPath(targetTreeView.RootNodes, path);
                    if (simpleItem != null)
                    {
                        targetTreeView.SelectedItem = simpleItem;
                        _currentSelectedItemPath = simpleItem.FilePath;
                        _activePanelId = panelId;
                        Debug.WriteLine($"Found via simple search: {path}");
                    }
                    else
                    {
                        Debug.WriteLine($"Not found via simple search either: {path}");
                    }
                }
                else
                {
                    Debug.WriteLine($"Path found via recursive search: {path}");
                }

                Debug.WriteLine($"=== UpdateTreeViewSelection END ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка обновления выделения: {ex.Message}");
            }
            finally
            {
                _isNavigationChangingSelection = false;
                UpdateNavigationButtons();
            }
        }

        private async Task<bool> ExpandAndSelectPath(IList<TreeViewNode> nodes, string[] pathSegments, int currentIndex, string panelId)
        {
            if (currentIndex >= pathSegments.Length)
            {
                Debug.WriteLine($"ExpandAndSelectPath: END - currentIndex ({currentIndex}) >= pathSegments.Length ({pathSegments.Length})");
                return false;
            }

            string currentPath = pathSegments[currentIndex];
            bool isLastSegment = currentIndex == pathSegments.Length - 1;

            Debug.WriteLine($"ExpandAndSelectPath: Looking for '{currentPath}' at index {currentIndex}, isLast: {isLastSegment}");
            Debug.WriteLine($"ExpandAndSelectPath: Checking {nodes.Count} nodes");

            foreach (var node in nodes)
            {
                if (node?.Content is not ExplorerItemViewModel item)
                {
                    continue;
                }

                string normalizedItemPath = NormalizePath(item.FilePath);
                string normalizedCurrentPath = NormalizePath(currentPath);

                Debug.WriteLine($"ExpandAndSelectPath: Comparing '{normalizedItemPath}' with '{normalizedCurrentPath}'");

                // Сравниваем нормализованные пути
                if (normalizedItemPath == normalizedCurrentPath)
                {
                    Debug.WriteLine($"ExpandAndSelectPath: MATCH FOUND for '{currentPath}'");

                    if (isLastSegment)
                    {
                        // Нашли целевой элемент - выделяем его
                        var targetTreeView = panelId == "MainTree" ? treeView : treeViewSpF;
                        targetTreeView.SelectedItem = item;
                        _currentSelectedItemPath = item.FilePath;
                        _activePanelId = panelId;

                        Debug.WriteLine($"ExpandAndSelectPath: SELECTED '{item.FilePath}'");

                        // Раскрываем узел если свернут
                        if (!node.IsExpanded)
                        {
                            //Настройка разворачивать при выделении
                            if (ExpandedTreeSelectedSetting)
                            {
                                targetTreeView.Expand(node);
                            }

                            Debug.WriteLine($"ExpandAndSelectPath: Expanded node '{item.FilePath}'");
                        }
                        return true;
                    }
                    else
                    {
                        Debug.WriteLine($"ExpandAndSelectPath: Intermediate node found, proceeding to children");

                        // Это промежуточный узел - раскрываем его и продолжаем поиск
                        if (!node.IsExpanded)
                        {
                            Debug.WriteLine($"ExpandAndSelectPath: Expanding intermediate node '{item.FilePath}'");
                            await ExpandNode(node, item.FilePath, panelId);
                        }
                        else
                        {
                            Debug.WriteLine($"ExpandAndSelectPath: Node '{item.FilePath}' is already expanded");
                        }

                        // Даем время на загрузку дочерних элементов
                        if (node.Children.Count == 0)
                        {
                            Debug.WriteLine($"ExpandAndSelectPath: No children yet, waiting for load...");
                            await Task.Delay(10);
                        }

                        // Рекурсивно ищем в дочерних узлах
                        Debug.WriteLine($"ExpandAndSelectPath: Searching in {node.Children.Count} children for next segment: '{pathSegments[currentIndex + 1]}'");
                        bool found = await ExpandAndSelectPath(node.Children, pathSegments, currentIndex + 1, panelId);
                        if (found)
                        {
                            Debug.WriteLine($"ExpandAndSelectPath: Found in children!");
                            return true;
                        }
                        else
                        {
                            Debug.WriteLine($"ExpandAndSelectPath: Not found in children");
                        }
                    }
                }
                else
                {
                    // Если текущий узел не совпадает, но у него есть дети - проверяем их рекурсивно
                    // Это нужно для случая, когда мы ищем диск D:\ внутри узла MyComputer
                    if (node.Children?.Count > 0)
                    {
                        Debug.WriteLine($"ExpandAndSelectPath: Checking children of '{normalizedItemPath}' for '{normalizedCurrentPath}'");
                        bool foundInChildren = await ExpandAndSelectPath(node.Children, pathSegments, currentIndex, panelId);
                        if (foundInChildren) return true;
                    }
                }
            }

            Debug.WriteLine($"ExpandAndSelectPath: No match found for '{currentPath}' in {nodes.Count} nodes");
            return false;
        }
        private async Task EnsureMyComputerExpanded(TreeViewNode myComputerNode)
        {
            if (myComputerNode?.Content is ExplorerItemViewModel item && item.FilePath == "MyComputer")
            {
                if (!myComputerNode.IsExpanded)
                {
                    Debug.WriteLine("EnsureMyComputerExpanded: Expanding MyComputer node");
                    treeView.Expand(myComputerNode);
                }

                // Если диски еще не загружены - загружаем их
                if (myComputerNode.Children.Count == 0)
                {
                    Debug.WriteLine("EnsureMyComputerExpanded: Loading drives");
                    LoadDrivesSync(myComputerNode);
                }
            }
        }
        private async Task ExpandNode(TreeViewNode node, string path, string panelId)
        {
            try
            {
                Debug.WriteLine($"ExpandNode: Expanding '{path}'");

                if (panelId == "MainTree")
                {
                    if (node.HasUnrealizedChildren || node.Children.Count == 0)
                    {
                        Debug.WriteLine($"ExpandNode: Loading subfolders for '{path}'");
                        node.HasUnrealizedChildren = false;
                        await LoadSubfoldersAsync(node, path);
                        Debug.WriteLine($"ExpandNode: Loaded {node.Children.Count} children");
                    }
                    else
                    {
                        Debug.WriteLine($"ExpandNode: Node '{path}' already has {node.Children.Count} children");
                    }
                }
                else // SpFTree
                {
                    if (node.HasUnrealizedChildren || node.Children.Count == 0)
                    {
                        Debug.WriteLine($"ExpandNode: Loading subfolders for SpF '{path}'");
                        node.HasUnrealizedChildren = false;
                        await LoadSubfoldersForSpFAsync(node, path);
                        Debug.WriteLine($"ExpandNode: Loaded {node.Children.Count} children");
                    }
                    else
                    {
                        Debug.WriteLine($"ExpandNode: SpF Node '{path}' already has {node.Children.Count} children");
                    }
                }

                // Раскрываем узел
                var targetTreeView = panelId == "MainTree" ? treeView : treeViewSpF;

                if (!node.IsExpanded)
                {
                    targetTreeView.Expand(node);
                    Debug.WriteLine($"ExpandNode: Node '{path}' expanded, children count: {node.Children.Count}");
                }
                else
                {
                    Debug.WriteLine($"ExpandNode: Node '{path}' was already expanded");
                }

                // Даем больше времени на отрисовку и загрузку
                await Task.Delay(20);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка раскрытия узла {path}: {ex.Message}");
            }
        }
        private string[] SplitPath(string path)
        {
            if (string.IsNullOrEmpty(path) || IsVirtualPath(path))
                return new string[] { path };

            try
            {
                var normalized = NormalizePath(path);
                var result = new List<string>();

                Debug.WriteLine($"SplitPath input: '{path}', normalized: '{normalized}'");

                // Обработка корневого диска (например "D:\")
                if (normalized.Length == 3 && normalized[1] == ':' && normalized[2] == '\\')
                {
                    result.Add(normalized);
                    Debug.WriteLine($"SplitPath: Root drive detected, result: {string.Join(" -> ", result)}");
                    return result.ToArray();
                }

                // Разбиваем путь на компоненты
                string root = Path.GetPathRoot(normalized);
                string[] parts = normalized.Substring(root.Length)
                    .Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

                Debug.WriteLine($"SplitPath: root='{root}', parts={string.Join(",", parts)}");

                // Строим полные пути для каждого сегмента
                string currentPath = root.TrimEnd(Path.DirectorySeparatorChar);

                // Добавляем корневой диск с чертой
                if (!string.IsNullOrEmpty(root))
                {
                    string rootPath = root.TrimEnd(Path.DirectorySeparatorChar);
                    // Для диска добавляем обратную черту
                    if (rootPath.Length == 2 && rootPath[1] == ':')
                    {
                        rootPath += Path.DirectorySeparatorChar;
                    }
                    result.Add(rootPath);
                    currentPath = rootPath;
                }

                // Добавляем остальные сегменты
                foreach (var part in parts)
                {
                    currentPath = Path.Combine(currentPath, part);
                    result.Add(currentPath);
                }

                Debug.WriteLine($"SplitPath result: {string.Join(" -> ", result)}");
                return result.ToArray();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SplitPath for '{path}': {ex.Message}");
                return new string[] { path };
            }
        }

        private void UpdateAllTiles()
        {
            // Обновляем основной TreeView
            if (treeView?.RootNodes?.Count > 0)
            {
                foreach (var node in treeView.RootNodes)
                {
                    UpdateTileSize(node);
                }
            }

            // Обновляем TreeView специальных папок
            if (treeViewSpF?.RootNodes?.Count > 0)
            {
                foreach (var node in treeViewSpF.RootNodes)
                {
                    UpdateTileSizeSpF(node);
                }
            }
        }
        private void UpdateTileSize(TreeViewNode node)
        {
            if (node == null) return;

            var container = treeView?.ContainerFromNode(node) as TreeViewItem;
            var tile = container?.ContentTemplateRoot as BaseTileControl;

            if (tile != null)
            {
                tile.UpdateSize(_selectedSize);
            }

            if (node.IsExpanded)
            {
                foreach (var childNode in node.Children)
                {
                    UpdateTileSize(childNode);
                }
            }
        }

        private void UpdateTileSizeSpF(TreeViewNode node)
        {
            if (node == null) return;

            var container = treeViewSpF?.ContainerFromNode(node) as TreeViewItem;
            var tile = container?.ContentTemplateRoot as BaseTileControl;

            if (tile != null)
            {
                tile.UpdateSize(_selectedSize);
            }

            if (node.IsExpanded)
            {
                foreach (var childNode in node.Children)
                {
                    UpdateTileSizeSpF(childNode);
                }
            }
        }
        #endregion

        #region Управление поиском
        private ExplorerItemViewModel FindItemByPath(IList<TreeViewNode> nodes, string path)
        {
            if (nodes == null) return null;

            foreach (var node in nodes)
            {
                if (node?.Content is ExplorerItemViewModel item)
                {
                    string normalizedItemPath = NormalizePath(item.FilePath);
                    string normalizedTargetPath = NormalizePath(path);

                    Debug.WriteLine($"FindItemByPath: Comparing '{normalizedItemPath}' with '{normalizedTargetPath}'");

                    // Сравниваем пути (нормализованные)
                    if (normalizedItemPath == normalizedTargetPath)
                    {
                        Debug.WriteLine($"FindItemByPath: MATCH FOUND!");
                        return item;
                    }
                }

                if (node?.Children?.Count > 0)
                {
                    var found = FindItemByPath(node.Children, path);
                    if (found != null)
                        return found;
                }
            }

            Debug.WriteLine($"FindItemByPath: No match found for '{path}'");
            return null;
        }

        private bool IsVirtualPath(string path)
        {
            return path == "MyComputer" || path == "SpecialFolders" || path == "..";
        }

        private string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path) || IsVirtualPath(path))
                return path;

            try
            {
                // Для путей дисков (например "D:" или "D:\") нормализуем к формату с чертой
                if (path.Length == 2 && path[1] == ':')
                {
                    return path + Path.DirectorySeparatorChar;
                }

                if (path.Length == 3 && path[1] == ':' && path[2] == '\\')
                {
                    return path; // Уже нормализован
                }

                string fullPath = Path.GetFullPath(path);
                return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch
            {
                return path;
            }
        }
        #endregion

        #region Управление навигацией
        private void OnNavigationChanged(object sender, NavigationEventArgs e)
        {
            // Всегда обновляем активную панель, если она валидная
            if (e.PanelId == "MainTree" || e.PanelId == "SpFTree")
            {
                _activePanelId = e.PanelId;
            }
            else
            {
                // Fallback на последнюю известную панель
                _activePanelId = _activePanelId ?? "MainTree";
                Debug.WriteLine($"Неизвестная панель: {e.PanelId}, используется: {_activePanelId}");
            }

            UpdateNavigationButtons();

            _isNavigationChangingSelection = true;
            try
            {
                UpdateTreeViewSelection(_activePanelId, e.Path);
            }
            finally
            {
                _isNavigationChangingSelection = false;
            }
        }

        private void UpdateNavigationButtons()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    string activePanel = _activePanelId ?? "MainTree";
                    IDirectoryHistory activeHistory = GetActiveHistory();
                    bool upButtonEnabled = false;

                    if (!string.IsNullOrEmpty(_currentSelectedItemPath) && Directory.Exists(_currentSelectedItemPath))
                    {
                        var parent = Directory.GetParent(_currentSelectedItemPath);
                        upButtonEnabled = parent != null && Directory.Exists(parent.FullName);
                    }

                    BackButton.IsEnabled = activeHistory?.CanMoveBack ?? false;
                    ForwardButton.IsEnabled = activeHistory?.CanMoveForward ?? false;
                    UpButton.IsEnabled = upButtonEnabled;

                    Debug.WriteLine($"Navigation buttons - Panel: {activePanel}, Back: {BackButton.IsEnabled}, Forward: {ForwardButton.IsEnabled}, Up: {UpButton.IsEnabled}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка обновления кнопок навигации: {ex.Message}");
                }
            });
        }
        private void TreeViewSpF_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
        {
            if (_isNavigationChangingSelection) return;

            if (treeViewSpF.SelectedNode?.Content is ExplorerItemViewModel selectedItem)
            {
                // ПРОВЕРЯЕМ: изменился ли путь?
                if (_currentSelectedItemPath == selectedItem.FilePath)
                {
                    Debug.WriteLine($"TreeViewSpF_SelectionChanged: путь не изменился ({selectedItem.FilePath}), пропускаем навигацию");
                    return;
                }

                _currentSelectedItemPath = selectedItem.FilePath;
                _activePanelId = "SpFTree";

                _navigationManager.NavigateTo(selectedItem.FilePath, "SpFTree");
                UpdateNavigationButtons();

                Debug.WriteLine($"TreeViewSpF_SelectionChanged: навигация к {selectedItem.FilePath}");
            }
        }

        private void TreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
        {
            if (_isNavigationChangingSelection) return;

            if (treeView.SelectedNode?.Content is ExplorerItemViewModel selectedItem)
            {
                // ПРОВЕРЯЕМ: изменился ли путь?
                if (_currentSelectedItemPath == selectedItem.FilePath)
                {
                    Debug.WriteLine($"TreeView_SelectionChanged: путь не изменился ({selectedItem.FilePath}), пропускаем навигацию");
                    return;
                }

                _currentSelectedItemPath = selectedItem.FilePath;
                _activePanelId = "MainTree";

                _navigationManager.NavigateTo(selectedItem.FilePath, "MainTree");
                UpdateNavigationButtons();

                Debug.WriteLine($"TreeView_SelectionChanged: навигация к {selectedItem.FilePath}");
            }
        }

        #endregion

        #region Навигационные кнопки

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            string activePanel = GetActivePanel();
            if (_navigationManager.CanGoBack(activePanel))
            {
                _navigationManager.GoBack(activePanel);
                // UpdateNavigationButtons() вызовется автоматически через OnNavigationChanged
            }
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            string activePanel = GetActivePanel();
            if (_navigationManager.CanGoForward(activePanel))
            {
                _navigationManager.GoForward(activePanel);
                // UpdateNavigationButtons() вызовется автоматически через OnNavigationChanged
            }
        }

        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(_currentSelectedItemPath) &&
                    Directory.Exists(_currentSelectedItemPath))
                {
                    var parentDir = Directory.GetParent(_currentSelectedItemPath);
                    if (parentDir != null)
                    {
                        string activePanel = GetActivePanel();
                        _navigationManager.NavigateTo(parentDir.FullName, activePanel);
                        // UpdateNavigationButtons() вызовется автоматически через OnNavigationChanged
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка навигации вверх: {ex.Message}");
            }
        }

        private string GetActivePanel()
        {
            // Просто возвращаем текущую активную панель
            return _activePanelId ?? "MainTree";
        }
        // Получаем историю на основе активной панели
        private DirectoryHistory GetActiveHistory()
        {
            string activePanel = GetActivePanel();
            return activePanel == "SpFTree" ? _historySpF : _history;
        }
        #endregion
    }
}
