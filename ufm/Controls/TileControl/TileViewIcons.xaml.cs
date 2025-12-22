//using CommunityToolkit.WinUI;
//using Core_FileManagement;
//using Microsoft.UI.Dispatching;
//using Microsoft.UI.Xaml;
//using Microsoft.UI.Xaml.Controls;
//using Microsoft.UI.Xaml.Input;
//using Microsoft.UI.Xaml.Media;
//using Microsoft.UI.Xaml.Media.Imaging;
//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using Windows.Storage;

//namespace ufm
//{
//    public sealed partial class TileViewIcons : UserControl, IDisposable, ISupportsIconSize, IRefreshablePanel
//    {
//        public string PanelId { get; set; } = "DefaultPanel";
//        public PanelManager PanelManager { get; private set; }
//        public event EventHandler NavigationChanged;

//        private CancellationTokenSource _currentOperationCts;
//        private readonly IDirectoryHistory _dummyHistory;
//        private string _currentLoadedPath;

//        // Флаг для отслеживания инициализации
//        private bool _isInitialized = false;
//        private bool _isLoading = false;
//        public ObservableCollection<ExplorerItemViewModel> Items { get; } = new ObservableCollection<ExplorerItemViewModel>();

//        // Сервис для работы с файловой системой
//        private readonly FileSystemService _fileSystemService;

//        // Размеры автоматически вычисляются на основе параметров иконок
//        private double _itemWidth;
//        private double _itemHeight;

//        public double ItemWidth
//        {
//            get => _itemWidth;
//            private set
//            {
//                if (_itemWidth != value)
//                {
//                    _itemWidth = value;
//                    UpdateGridViewLayout();
//                }
//            }
//        }

//        public double ItemHeight
//        {
//            get => _itemHeight;
//            private set
//            {
//                if (_itemHeight != value)
//                {
//                    _itemHeight = value;
//                    UpdateGridViewLayout();
//                }
//            }
//        }

//        // Параметры отображения элементов
//        private string _selectedSize = "Medium";


//        // Отступы и padding для расчета общего размера
//        private const int HorizontalPadding = 10;
//        private const int VerticalPadding = 8;
//        private const int TextBlockHeight = 40;
//        private const int MinimumItemWidth = 100;
//        private const int MinimumItemHeight = 100;

//        public bool SingleClickOpenItem
//        {
//            get
//            {
//                try
//                {
//                    // Используем App.SettingsManager для получения настройки
//                    return App.SettingsManager?.GetSetting<bool>("SingleClickOpen", true) ?? true;
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"Error getting Single Click Open setting: {ex}");
//                    return true; // По умолчанию показываем
//                }
//            }
//        }
//        public TileViewIcons()
//        {
//            InitializeComponent();

//            NavigationSettingsMediator.RegisterPanel(this);//13 10 2025

//            // Создаем фиктивную историю для элементов отображения
//            _dummyHistory = new DirectoryHistory("MyComputer", "Мой Компьютер");

//            // Инициализируем сервис файловой системы
//            _fileSystemService = new FileSystemService();

//            ItemsGridView.ItemsSource = Items;
//            Loaded += OnLoaded;

//            // Подписка на событие изменения контейнера
//            ItemsGridView.ContainerContentChanging += ItemsGridView_ContainerContentChanging;

//            // Инициализируем начальные размеры
//            CalculateItemDimensions();
//        }

//        public void Dispose()
//        {
//            NavigationSettingsMediator.UnregisterPanel(this);
//            _currentOperationCts?.Cancel();
//            _currentOperationCts?.Dispose();
//            _dummyHistory?.Dispose();

//            Loaded -= OnLoaded;
//            ItemsGridView.ContainerContentChanging -= ItemsGridView_ContainerContentChanging;

//            // Очищаем индивидуальный кэш этой панели
//            _fileSystemService.ClearPanelCache(PanelId);

//            _fileSystemService?.Dispose();

//            if (PanelManager != null)
//            {
//                PanelManager.NavigationChanged -= OnPanelNavigationChanged;
//            }

//            foreach (var item in Items)
//            {
//                item?.Dispose();
//            }
//        }

//        public void SetPanelManager(PanelManager panelManager)
//        {
//            if (PanelManager != null)
//            {
//                PanelManager.NavigationChanged -= OnPanelNavigationChanged;
//            }

//            // Очищаем индивидуальный кэш при смене менеджера
//            _fileSystemService.ClearPanelCache(PanelId);

//            PanelManager = panelManager;

//            if (PanelManager != null)
//            {
//                PanelManager.NavigationChanged += OnPanelNavigationChanged;
//                SetIconSize(PanelManager.State.IconSize);
//            }
//        }

//        private void OnPanelNavigationChanged(object sender, EventArgs e)
//        {
//            if (_isLoading) return; // Игнорируем события во время загрузки

//            if (PanelManager != null && PanelManager.CurrentPath != _currentLoadedPath)
//            {
//                DispatcherQueue.TryEnqueue(async () =>
//                {
//                    await LoadPathContents(PanelManager.CurrentPath);
//                });
//            }
//        }

//        private async void OnLoaded(object sender, RoutedEventArgs e)
//        {
//            if (string.IsNullOrEmpty(_selectedSize))
//            {
//                _selectedSize = "Medium";
//                Debug.WriteLine("Используются настройки по умолчанию.");
//            }
//            else
//            {
//                Debug.WriteLine($"Загружены настройки: Размер = {_selectedSize}");
//            }


//            CalculateItemDimensions();
//            UpdateAllTiles();
//            UpdateGridViewLayout();

//            // Загружаем содержимое в зависимости от состояния PanelManager
//            if (PanelManager != null && !string.IsNullOrEmpty(PanelManager.CurrentPath))
//            {
//                await LoadPathContents(PanelManager.CurrentPath);
//                _isInitialized = true;
//            }
//            else if (!_isInitialized)
//            {
//                LoadInitialContent();
//                _isInitialized = true;
//            }
//        }

//        private void OnNavigationChanged()
//        {
//            Debug.WriteLine($"[{PanelId}] Navigation changed, raising event");
//            NavigationChanged?.Invoke(this, EventArgs.Empty);
//        }

//        public void SetIconSize(string size)
//        {
//            _selectedSize = size;

//            // Сохраняем в PanelManager
//            if (PanelManager != null)
//            {
//                PanelManager.UpdateState(state => state.IconSize = size);
//            }

//            CalculateItemDimensions();
//            Debug.WriteLine($"[{PanelId}] Установлен размер: {_selectedSize}");

//            UpdateAllTiles();
//            UpdateGridViewLayout();
//            ItemsGridView.UpdateLayout();
//        }

//        #region Расчет размеров элементов

//        private void CalculateItemDimensions()
//        {
//            // Получаем базовые размеры из менеджера
//            var sizeParams = SizeManagerTile.GetSize(_selectedSize);

//            // Добавляем дополнительные отступы (20 пикселей к ширине, 25 к высоте)
//            ItemWidth = Math.Max(sizeParams.Width + 15, MinimumItemWidth);
//            ItemHeight = Math.Max(sizeParams.Height + 45, MinimumItemHeight);

//            Debug.WriteLine($"Calculated dimensions: Width={ItemWidth}, Height={ItemHeight}, Base=({sizeParams.Width}x{sizeParams.Height})");
//        }

//        #endregion

//        #region Обновление отображения элементов

//        private void UpdateAllTiles()
//        {
//            // Обновляем все видимые элементы
//            foreach (var item in ItemsGridView.Items)
//            {
//                var container = ItemsGridView.ContainerFromItem(item) as GridViewItem;
//                if (container != null)
//                {
//                    var tile = container.ContentTemplateRoot as BaseTileControl;
//                    if (tile != null)
//                    {
//                        tile.UpdateSize(_selectedSize);
//                    }
//                }
//            }
//        }

//        // Обработчик для обновления новых элементов при прокрутке
//        private void ItemsGridView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
//        {
//            if (args.Phase != 0) return;

//            if (args.ItemContainer is GridViewItem container)
//            {
//                var tile = container.ContentTemplateRoot as BaseTileControl;
//                if (tile != null)
//                {
//                    tile.UpdateSize(_selectedSize);
//                }
//            }
//        }
//        #endregion

//        private void UpdateGridViewLayout()
//        {
//            if (ItemsGridView?.ItemsPanelRoot is ItemsWrapGrid wrapGrid)
//            {
//                wrapGrid.ItemWidth = ItemWidth;
//                wrapGrid.ItemHeight = ItemHeight;
//                Debug.WriteLine($"GridView layout updated: ItemWidth={wrapGrid.ItemWidth}, ItemHeight={wrapGrid.ItemHeight}");
//            }
//        }

//        private async void LoadInitialContent()
//        {
//            // ИСПРАВЛЕНО: добавляем проверку на уже загруженное содержимое
//            if (_currentLoadedPath == "MyComputer" && Items.Count > 0)
//            {
//                Debug.WriteLine($"[{PanelId}] MyComputer content already loaded, skipping");
//                return;
//            }

//            CancelCurrentOperation();
//            Items.Clear();
//            UpdateGridViewLayout();

//            try
//            {
//                Debug.WriteLine($"[{PanelId}] Loading MyComputer content...");
//                var items = await _fileSystemService.LoadMyComputerAsync(PanelId, _dummyHistory);
//                foreach (var item in items)
//                {
//                    Items.Add(item);
//                }
//                _currentLoadedPath = "MyComputer";
//                OnNavigationChanged();
//                Debug.WriteLine($"[{PanelId}] MyComputer content loaded successfully, items count: {Items.Count}");
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[{PanelId}] Error loading initial content: {ex.Message}");
//            }
//        }

//        internal async Task LoadPathContents(string path)
//        {
//            // ИСПРАВЛЕНО: добавляем проверку на уже загруженный путь
//            if (_isLoading || _currentLoadedPath == path)
//                return;

//            try
//            {
//                _isLoading = true;
//                switch (path)
//                {
//                    case "MyComputer":
//                        LoadInitialContent();
//                        _currentLoadedPath = path;
//                        break;

//                    case "Drives":
//                        await LoadDrives();
//                        _currentLoadedPath = path;
//                        break;

//                    case string p when Directory.Exists(p):
//                        await LoadFolderContents(path);
//                        _currentLoadedPath = path;

//                        // Принудительно обновляем навигацию для PanelManager
//                        if (PanelManager != null && PanelManager.CurrentPath != path)
//                        {
//                            PanelManager.NavigateTo(path);
//                        }
//                        break;

//                    default:
//                        // ИСПРАВЛЕНО: не вызываем LoadInitialContent повторно
//                        if (_currentLoadedPath != "MyComputer")
//                        {
//                            LoadInitialContent();
//                            _currentLoadedPath = "MyComputer";
//                        }
//                        break;
//                }
//            }
//            finally
//            {
//                _isLoading = false;
//            }


//            OnNavigationChanged();
//        }

//        //private async void ItemsGridView_OnItemClick(object sender, ItemClickEventArgs e)
//        //{
//        //    if (e.ClickedItem is not ExplorerItemViewModel item) return;

//        //    try
//        //    {
//        //        if (item.Name == "..")
//        //        {
//        //            PanelManager?.GoBack();
//        //            return;
//        //        }
//        //        if (item.FilePath == "Drives" ||
//        //            item.FilePath == "MyComputer" ||
//        //            Directory.Exists(item.FilePath))
//        //        {
//        //            await LoadPathContents(item.FilePath);
//        //            PanelManager?.NavigateTo(item.FilePath);
//        //        }
//        //        else if (File.Exists(item.FilePath))
//        //        {
//        //            Debug.WriteLine($"File selected: {item.FilePath}");
//        //        }
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        Debug.WriteLine($"Error in ItemClick: {ex.Message}");
//        //    }
//        //}
//        private async void ItemsGridView_OnItemClick(object sender, ItemClickEventArgs e)
//        {
//            if (e.ClickedItem is not ExplorerItemViewModel item) return;

//            var currentTime = DateTime.Now;
//            bool isSingleClickMode = SingleClickOpenItem;

//            if (isSingleClickMode)
//            {
//                // Режим одного клика - сразу открываем
//                await OpenItem(item);
//            }
//            else
//            {
//                // Режим двойного клика
//                bool isDoubleClick = (_lastClickedItem == item &&
//                                     (currentTime - _lastClickTime).TotalMilliseconds < 500);

//                if (isDoubleClick)
//                {
//                    // Это двойной клик - открываем
//                    await OpenItem(item);
//                    _lastClickedItem = null;
//                }
//                else
//                {
//                    // Это первый клик - выделяем элемент
//                    ItemsGridView.SelectedItem = item;
//                    _lastClickedItem = item;
//                    _lastClickTime = currentTime;
//                }
//            }
//        }
//        // В класс TileViewIcons добавьте:
//        private async void ItemsGridView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
//        {
//            // В режиме одного клика игнорируем двойное нажатие
//            if (SingleClickOpenItem) return;

//            // Находим элемент, на который было совершено двойное нажатие
//            var element = e.OriginalSource as FrameworkElement;
//            while (element != null && element.DataContext as ExplorerItemViewModel == null)
//            {
//                element = VisualTreeHelper.GetParent(element) as FrameworkElement;
//            }

//            if (element?.DataContext is ExplorerItemViewModel item)
//            {
//                await OpenItem(item);
//            }
//        }

//        private async Task OpenItem(ExplorerItemViewModel item)
//        {
//            try
//            {
//                // Сбрасываем выделение при открытии
//                ItemsGridView.SelectedItem = null;
//                _lastClickedItem = null;

//                if (item.Name == "..")
//                {
//                    PanelManager?.GoBack();
//                    return;
//                }

//                if (item.FilePath == "Drives" ||
//                    item.FilePath == "MyComputer" ||
//                    Directory.Exists(item.FilePath))
//                {
//                    await LoadPathContents(item.FilePath);
//                    PanelManager?.NavigateTo(item.FilePath);
//                }
//                else if (File.Exists(item.FilePath))
//                {
//                    Debug.WriteLine($"File selected: {item.FilePath}");
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Error opening item {item?.Name}: {ex.Message}");
//            }
//        }

//        private void CancelCurrentOperation()
//        {
//            _currentOperationCts?.Cancel();
//            _currentOperationCts?.Dispose();
//            _currentOperationCts = new CancellationTokenSource();
//            _fileSystemService.CancelAllOperations();
//        }

//        private async Task LoadDrives()
//        {
//            // ИСПРАВЛЕНО: проверяем, не загружены ли уже диски
//            if (_currentLoadedPath == "Drives" && Items.Count > 1) // >1 потому что есть кнопка "Назад"
//            {
//                Debug.WriteLine($"[{PanelId}] Drives already loaded, skipping");
//                return;
//            }

//            CancelCurrentOperation();

//            Items.Clear();
//            UpdateGridViewLayout();

//            try
//            {
//                Debug.WriteLine($"[{PanelId}] Loading drives...");
//                var driveItems = await _fileSystemService.LoadDrivesAsync(_dummyHistory);
//                foreach (var item in driveItems)
//                {
//                    Items.Add(item);
//                }
//                Debug.WriteLine($"[{PanelId}] Drives loaded successfully, items count: {Items.Count}");
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[{PanelId}] Error loading drives: {ex.Message}");
//                LoadInitialContent();
//            }

//            OnNavigationChanged();
//        }

//        private async Task LoadFolderContents(string folderPath)
//        {
//            if (string.IsNullOrEmpty(folderPath))
//                return;

//            if (!Directory.Exists(folderPath))
//            {
//                Debug.WriteLine($"Directory does not exist: {folderPath}");
//                PanelManager?.GoBack();
//                return;
//            }

//            // ИСПРАВЛЕНО: проверяем, не загружена ли уже эта папка
//            if (_currentLoadedPath == folderPath && Items.Count > 0)
//            {
//                Debug.WriteLine($"[{PanelId}] Folder {folderPath} already loaded, skipping");
//                return;
//            }

//            CancelCurrentOperation();

//            try
//            {
//                Debug.WriteLine($"[{PanelId}] Loading folder contents: {folderPath}");
//                var folderItems = await _fileSystemService.LoadFolderContentsAsync(folderPath, _dummyHistory);

//                // Обновляем UI
//                await this.DispatcherQueue.EnqueueAsync(() =>
//                {
//                    Items.Clear();
//                    foreach (var item in folderItems)
//                    {
//                        Items.Add(item);
//                    }
//                    UpdateGridViewLayout();
//                });
//                Debug.WriteLine($"[{PanelId}] Folder contents loaded successfully, items count: {Items.Count}");
//            }
//            catch (OperationCanceledException)
//            {
//                Debug.WriteLine($"[{PanelId}] Folder loading canceled");
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[{PanelId}] Error loading folder contents {folderPath}: {ex.Message}");
//                PanelManager?.GoBack();
//            }
//        }

//        public void RefreshNavigation()
//        {
//            Debug.WriteLine($"[TileViewIcons {PanelId}] Refreshing navigation via mediator");

//            _ = this.DispatcherQueue.EnqueueAsync(() =>
//            {
//                Task task = RefreshContent();
//            }, Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal);
//        }

//        //public async Task RefreshContent()
//        //{
//        //    try
//        //    {
//        //        Debug.WriteLine($"[TileViewIcons {PanelId}] Starting refresh");

//        //        // Сохраняем путь ДО сброса состояния
//        //        string pathToReload = _currentLoadedPath;
//        //        if (string.IsNullOrEmpty(pathToReload) && PanelManager != null)
//        //        {
//        //            pathToReload = PanelManager.CurrentPath;
//        //        }

//        //        Debug.WriteLine($"[TileViewIcons {PanelId}] Reloading path: '{pathToReload}'");

//        //        // 1. Сбрасываем кэш
//        //        _fileSystemService.ClearPanelCache(PanelId);

//        //        // 2. Отменяем операции
//        //        CancelCurrentOperation();

//        //        // 3. Сбрасываем критическое состояние
//        //        _currentLoadedPath = null;  // ⚡ ВАЖНО: сбросить перед загрузкой
//        //        _isInitialized = false;

//        //        // 4. Очищаем UI
//        //        Items.Clear();
//        //        UpdateGridViewLayout();

//        //        // 5. Перезагружаем
//        //        if (!string.IsNullOrEmpty(pathToReload))
//        //        {
//        //            await LoadPathContents(pathToReload);
//        //        }
//        //        else
//        //        {
//        //            LoadInitialContent();
//        //        }

//        //        Debug.WriteLine($"[TileViewIcons {PanelId}] Refresh completed");
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        Debug.WriteLine($"[TileViewIcons {PanelId}] RefreshContent error: {ex}");

//        //        // Fallback
//        //        try
//        //        {
//        //            LoadInitialContent();
//        //        }
//        //        catch (Exception fallbackEx)
//        //        {
//        //            Debug.WriteLine($"[TileViewIcons {PanelId}] Fallback also failed: {fallbackEx}");
//        //        }
//        //    }
//        //}
//        public async Task RefreshContent()
//        {
//            try
//            {
//                Debug.WriteLine($"[TileViewIcons {PanelId}] Starting refresh");

//                // 1. Получаем путь в UI-потоке
//                string pathToReload = await DispatcherQueue.EnqueueAsync(() =>
//                {
//                    string path = _currentLoadedPath;
//                    if (string.IsNullOrEmpty(path) && PanelManager != null)
//                        path = PanelManager.CurrentPath;

//                    // Очищаем UI
//                    _currentLoadedPath = null;
//                    _isInitialized = false;
//                    Items.Clear();
//                    UpdateGridViewLayout();

//                    return path;
//                });

//                Debug.WriteLine($"[TileViewIcons {PanelId}] Reloading path: '{pathToReload}'");

//                // 2. Фоновые операции
//                _fileSystemService.ClearPanelCache(PanelId);
//                CancelCurrentOperation();

//                // 3. Загрузка (вернет Task, но обрабатываем в UI)
//                if (!string.IsNullOrEmpty(pathToReload))
//                {
//                    await LoadPathContents(pathToReload);
//                }
//                else
//                {
//                    await Task.Run(() => LoadInitialContent());
//                }

//                Debug.WriteLine($"[TileViewIcons {PanelId}] Refresh completed");
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileViewIcons {PanelId}] Refresh error: {ex}");

//                // Fallback
//                try
//                {
//                    await DispatcherQueue.EnqueueAsync(() => LoadInitialContent());
//                }
//                catch (Exception fallbackEx)
//                {
//                    Debug.WriteLine($"[TileViewIcons {PanelId}] Fallback failed: {fallbackEx}");
//                }
//            }
//        }
//    }
//}


//using CommunityToolkit.WinUI;
//using Core_FileManagement;
//using Microsoft.UI.Dispatching;
//using Microsoft.UI.Xaml;
//using Microsoft.UI.Xaml.Controls;
//using Microsoft.UI.Xaml.Input;
//using Microsoft.UI.Xaml.Media;
//using Microsoft.UI.Xaml.Media.Imaging;
//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using Windows.Storage;

//namespace ufm
//{
//    public sealed partial class TileViewIcons : UserControl, IDisposable, ISupportsIconSize, IRefreshablePanel
//    {
//        public string PanelId { get; set; } = "DefaultPanel";
//        public PanelManager PanelManager { get; private set; }
//        public event EventHandler NavigationChanged;

//        private CancellationTokenSource _currentOperationCts;
//        private readonly IDirectoryHistory _dummyHistory;
//        private string _currentLoadedPath;

//        // Флаг для отслеживания инициализации
//        private bool _isInitialized = false;
//        private bool _isLoading = false;
//        public ObservableCollection<ExplorerItemViewModel> Items { get; } = new ObservableCollection<ExplorerItemViewModel>();

//        // Сервис для работы с файловой системой
//        private readonly FileSystemService _fileSystemService;

//        // Размеры автоматически вычисляются на основе параметров иконок
//        private double _itemWidth;
//        private double _itemHeight;

//        // Поля для отслеживания двойного клика
//        private DateTime _lastClickTime = DateTime.MinValue;
//        private ExplorerItemViewModel _lastClickedItem = null;

//        public double ItemWidth
//        {
//            get => _itemWidth;
//            private set
//            {
//                if (_itemWidth != value)
//                {
//                    _itemWidth = value;
//                    UpdateGridViewLayout();
//                }
//            }
//        }

//        public double ItemHeight
//        {
//            get => _itemHeight;
//            private set
//            {
//                if (_itemHeight != value)
//                {
//                    _itemHeight = value;
//                    UpdateGridViewLayout();
//                }
//            }
//        }

//        // Параметры отображения элементов
//        private string _selectedSize = "Medium";

//        // Отступы и padding для расчета общего размера
//        private const int HorizontalPadding = 10;
//        private const int VerticalPadding = 8;
//        private const int TextBlockHeight = 40;
//        private const int MinimumItemWidth = 100;
//        private const int MinimumItemHeight = 100;

//        public bool SingleClickOpenItem
//        {
//            get
//            {
//                try
//                {
//                    // Используем App.SettingsManager для получения настройки
//                    // Значение по умолчанию изменено на false (режим двойного клика)
//                    return App.SettingsManager?.GetSetting<bool>("SingleClickOpen", false) ?? false;
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"Error getting Single Click Open setting: {ex}");
//                    return false; // По умолчанию двойной клик
//                }
//            }
//        }

//        public TileViewIcons()
//        {
//            InitializeComponent();

//            NavigationSettingsMediator.RegisterPanel(this);//13 10 2025

//            // Создаем фиктивную историю для элементов отображения
//            _dummyHistory = new DirectoryHistory("MyComputer", "Мой Компьютер");

//            // Инициализируем сервис файловой системы
//            _fileSystemService = new FileSystemService();

//            ItemsGridView.ItemsSource = Items;
//            Loaded += OnLoaded;

//            // Подписка на событие изменения контейнера
//            ItemsGridView.ContainerContentChanging += ItemsGridView_ContainerContentChanging;

//            // Подписка на событие двойного клика
//            ItemsGridView.DoubleTapped += ItemsGridView_DoubleTapped;

//            // Инициализируем начальные размеры
//            CalculateItemDimensions();
//        }

//        public void Dispose()
//        {
//            NavigationSettingsMediator.UnregisterPanel(this);
//            _currentOperationCts?.Cancel();
//            _currentOperationCts?.Dispose();
//            _dummyHistory?.Dispose();

//            Loaded -= OnLoaded;
//            ItemsGridView.ContainerContentChanging -= ItemsGridView_ContainerContentChanging;
//            ItemsGridView.DoubleTapped -= ItemsGridView_DoubleTapped;

//            // Очищаем индивидуальный кэш этой панели
//            _fileSystemService.ClearPanelCache(PanelId);

//            _fileSystemService?.Dispose();

//            if (PanelManager != null)
//            {
//                PanelManager.NavigationChanged -= OnPanelNavigationChanged;
//            }

//            foreach (var item in Items)
//            {
//                item?.Dispose();
//            }
//        }

//        public void SetPanelManager(PanelManager panelManager)
//        {
//            if (PanelManager != null)
//            {
//                PanelManager.NavigationChanged -= OnPanelNavigationChanged;
//            }

//            // Очищаем индивидуальный кэш при смене менеджера
//            _fileSystemService.ClearPanelCache(PanelId);

//            PanelManager = panelManager;

//            if (PanelManager != null)
//            {
//                PanelManager.NavigationChanged += OnPanelNavigationChanged;
//                SetIconSize(PanelManager.State.IconSize);
//            }
//        }

//        private void OnPanelNavigationChanged(object sender, EventArgs e)
//        {
//            if (_isLoading) return; // Игнорируем события во время загрузки

//            if (PanelManager != null && PanelManager.CurrentPath != _currentLoadedPath)
//            {
//                DispatcherQueue.TryEnqueue(async () =>
//                {
//                    await LoadPathContents(PanelManager.CurrentPath);
//                });
//            }
//        }

//        private async void OnLoaded(object sender, RoutedEventArgs e)
//        {
//            if (string.IsNullOrEmpty(_selectedSize))
//            {
//                _selectedSize = "Medium";
//                Debug.WriteLine("Используются настройки по умолчанию.");
//            }
//            else
//            {
//                Debug.WriteLine($"Загружены настройки: Размер = {_selectedSize}");
//            }

//            CalculateItemDimensions();
//            UpdateAllTiles();
//            UpdateGridViewLayout();

//            // Загружаем содержимое в зависимости от состояния PanelManager
//            if (PanelManager != null && !string.IsNullOrEmpty(PanelManager.CurrentPath))
//            {
//                await LoadPathContents(PanelManager.CurrentPath);
//                _isInitialized = true;
//            }
//            else if (!_isInitialized)
//            {
//                LoadInitialContent();
//                _isInitialized = true;
//            }
//        }

//        private void OnNavigationChanged()
//        {
//            Debug.WriteLine($"[{PanelId}] Navigation changed, raising event");
//            NavigationChanged?.Invoke(this, EventArgs.Empty);
//        }

//        public void SetIconSize(string size)
//        {
//            _selectedSize = size;

//            // Сохраняем в PanelManager
//            if (PanelManager != null)
//            {
//                PanelManager.UpdateState(state => state.IconSize = size);
//            }

//            CalculateItemDimensions();
//            Debug.WriteLine($"[{PanelId}] Установлен размер: {_selectedSize}");

//            UpdateAllTiles();
//            UpdateGridViewLayout();
//            ItemsGridView.UpdateLayout();
//        }

//        #region Расчет размеров элементов

//        private void CalculateItemDimensions()
//        {
//            // Получаем базовые размеры из менеджера
//            var sizeParams = SizeManagerTile.GetSize(_selectedSize);

//            // Добавляем дополнительные отступы (20 пикселей к ширине, 25 к высоте)
//            ItemWidth = Math.Max(sizeParams.Width + 15, MinimumItemWidth);
//            ItemHeight = Math.Max(sizeParams.Height + 45, MinimumItemHeight);

//            Debug.WriteLine($"Calculated dimensions: Width={ItemWidth}, Height={ItemHeight}, Base=({sizeParams.Width}x{sizeParams.Height})");
//        }

//        #endregion

//        #region Обновление отображения элементов

//        private void UpdateAllTiles()
//        {
//            // Обновляем все видимые элементы
//            foreach (var item in ItemsGridView.Items)
//            {
//                var container = ItemsGridView.ContainerFromItem(item) as GridViewItem;
//                if (container != null)
//                {
//                    var tile = container.ContentTemplateRoot as BaseTileControl;
//                    if (tile != null)
//                    {
//                        tile.UpdateSize(_selectedSize);
//                    }
//                }
//            }
//        }

//        // Обработчик для обновления новых элементов при прокрутке
//        private void ItemsGridView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
//        {
//            if (args.Phase != 0) return;

//            if (args.ItemContainer is GridViewItem container)
//            {
//                var tile = container.ContentTemplateRoot as BaseTileControl;
//                if (tile != null)
//                {
//                    tile.UpdateSize(_selectedSize);
//                }
//            }
//        }

//        #endregion

//        private void UpdateGridViewLayout()
//        {
//            if (ItemsGridView?.ItemsPanelRoot is ItemsWrapGrid wrapGrid)
//            {
//                wrapGrid.ItemWidth = ItemWidth;
//                wrapGrid.ItemHeight = ItemHeight;
//                Debug.WriteLine($"GridView layout updated: ItemWidth={wrapGrid.ItemWidth}, ItemHeight={wrapGrid.ItemHeight}");
//            }
//        }

//        private async void LoadInitialContent()
//        {
//            // ИСПРАВЛЕНО: добавляем проверку на уже загруженное содержимое
//            if (_currentLoadedPath == "MyComputer" && Items.Count > 0)
//            {
//                Debug.WriteLine($"[{PanelId}] MyComputer content already loaded, skipping");
//                return;
//            }

//            CancelCurrentOperation();
//            Items.Clear();
//            UpdateGridViewLayout();

//            try
//            {
//                Debug.WriteLine($"[{PanelId}] Loading MyComputer content...");
//                var items = await _fileSystemService.LoadMyComputerAsync(PanelId, _dummyHistory);
//                foreach (var item in items)
//                {
//                    Items.Add(item);
//                }
//                _currentLoadedPath = "MyComputer";
//                OnNavigationChanged();
//                Debug.WriteLine($"[{PanelId}] MyComputer content loaded successfully, items count: {Items.Count}");
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[{PanelId}] Error loading initial content: {ex.Message}");
//            }
//        }

//        internal async Task LoadPathContents(string path)
//        {
//            // ИСПРАВЛЕНО: добавляем проверку на уже загруженный путь
//            if (_isLoading || _currentLoadedPath == path)
//                return;

//            try
//            {
//                _isLoading = true;
//                switch (path)
//                {
//                    case "MyComputer":
//                        LoadInitialContent();
//                        _currentLoadedPath = path;
//                        break;

//                    case "Drives":
//                        await LoadDrives();
//                        _currentLoadedPath = path;
//                        break;

//                    case string p when Directory.Exists(p):
//                        await LoadFolderContents(path);
//                        _currentLoadedPath = path;

//                        // Принудительно обновляем навигацию для PanelManager
//                        if (PanelManager != null && PanelManager.CurrentPath != path)
//                        {
//                            PanelManager.NavigateTo(path);
//                        }
//                        break;

//                    default:
//                        // ИСПРАВЛЕНО: не вызываем LoadInitialContent повторно
//                        if (_currentLoadedPath != "MyComputer")
//                        {
//                            LoadInitialContent();
//                            _currentLoadedPath = "MyComputer";
//                        }
//                        break;
//                }
//            }
//            finally
//            {
//                _isLoading = false;
//            }

//            OnNavigationChanged();
//        }

//        private async void ItemsGridView_OnItemClick(object sender, ItemClickEventArgs e)
//        {
//            if (e.ClickedItem is not ExplorerItemViewModel item) return;

//            var currentTime = DateTime.Now;
//            bool isSingleClickMode = SingleClickOpenItem;

//            if (isSingleClickMode)
//            {
//                // Режим одного клика - сразу открываем
//                await OpenItem(item);
//            }
//            else
//            {
//                // Режим двойного клика
//                bool isDoubleClick = (_lastClickedItem == item &&
//                                     (currentTime - _lastClickTime).TotalMilliseconds < 500);

//                if (isDoubleClick)
//                {
//                    // Это двойной клик - открываем
//                    await OpenItem(item);
//                    _lastClickedItem = null;
//                }
//                else
//                {
//                    // Это первый клик - выделяем элемент
//                    ItemsGridView.SelectedItem = item;
//                    _lastClickedItem = item;
//                    _lastClickTime = currentTime;
//                }
//            }
//        }

//        private async void ItemsGridView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
//        {
//            // В режиме одного клика игнорируем двойное нажатие
//            if (SingleClickOpenItem) return;

//            // Находим элемент, на который было совершено двойное нажатие
//            var element = e.OriginalSource as FrameworkElement;
//            while (element != null && element.DataContext as ExplorerItemViewModel == null)
//            {
//                element = VisualTreeHelper.GetParent(element) as FrameworkElement;
//            }

//            if (element?.DataContext is ExplorerItemViewModel item)
//            {
//                await OpenItem(item);
//            }
//        }

//        private async Task OpenItem(ExplorerItemViewModel item)
//        {
//            try
//            {
//                // Сбрасываем выделение при открытии
//                ItemsGridView.SelectedItem = null;
//                _lastClickedItem = null;

//                if (item.Name == "..")
//                {
//                    PanelManager?.GoBack();
//                    return;
//                }

//                if (item.FilePath == "Drives" ||
//                    item.FilePath == "MyComputer" ||
//                    Directory.Exists(item.FilePath))
//                {
//                    await LoadPathContents(item.FilePath);
//                    PanelManager?.NavigateTo(item.FilePath);
//                }
//                else if (File.Exists(item.FilePath))
//                {
//                    Debug.WriteLine($"File selected: {item.FilePath}");
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Error opening item {item?.Name}: {ex.Message}");
//            }
//        }

//        private void CancelCurrentOperation()
//        {
//            _currentOperationCts?.Cancel();
//            _currentOperationCts?.Dispose();
//            _currentOperationCts = new CancellationTokenSource();
//            _fileSystemService.CancelAllOperations();
//        }

//        private async Task LoadDrives()
//        {
//            // ИСПРАВЛЕНО: проверяем, не загружены ли уже диски
//            if (_currentLoadedPath == "Drives" && Items.Count > 1) // >1 потому что есть кнопка "Назад"
//            {
//                Debug.WriteLine($"[{PanelId}] Drives already loaded, skipping");
//                return;
//            }

//            CancelCurrentOperation();

//            Items.Clear();
//            UpdateGridViewLayout();

//            try
//            {
//                Debug.WriteLine($"[{PanelId}] Loading drives...");
//                var driveItems = await _fileSystemService.LoadDrivesAsync(_dummyHistory);
//                foreach (var item in driveItems)
//                {
//                    Items.Add(item);
//                }
//                Debug.WriteLine($"[{PanelId}] Drives loaded successfully, items count: {Items.Count}");
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[{PanelId}] Error loading drives: {ex.Message}");
//                LoadInitialContent();
//            }

//            OnNavigationChanged();
//        }

//        private async Task LoadFolderContents(string folderPath)
//        {
//            if (string.IsNullOrEmpty(folderPath))
//                return;

//            if (!Directory.Exists(folderPath))
//            {
//                Debug.WriteLine($"Directory does not exist: {folderPath}");
//                PanelManager?.GoBack();
//                return;
//            }

//            // ИСПРАВЛЕНО: проверяем, не загружена ли уже эта папка
//            if (_currentLoadedPath == folderPath && Items.Count > 0)
//            {
//                Debug.WriteLine($"[{PanelId}] Folder {folderPath} already loaded, skipping");
//                return;
//            }

//            CancelCurrentOperation();

//            try
//            {
//                Debug.WriteLine($"[{PanelId}] Loading folder contents: {folderPath}");
//                var folderItems = await _fileSystemService.LoadFolderContentsAsync(folderPath, _dummyHistory);

//                // Обновляем UI
//                await this.DispatcherQueue.EnqueueAsync(() =>
//                {
//                    Items.Clear();
//                    foreach (var item in folderItems)
//                    {
//                        Items.Add(item);
//                    }
//                    UpdateGridViewLayout();
//                });
//                Debug.WriteLine($"[{PanelId}] Folder contents loaded successfully, items count: {Items.Count}");
//            }
//            catch (OperationCanceledException)
//            {
//                Debug.WriteLine($"[{PanelId}] Folder loading canceled");
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[{PanelId}] Error loading folder contents {folderPath}: {ex.Message}");
//                PanelManager?.GoBack();
//            }
//        }

//        public void RefreshNavigation()
//        {
//            Debug.WriteLine($"[TileViewIcons {PanelId}] Refreshing navigation via mediator");

//            _ = this.DispatcherQueue.EnqueueAsync(() =>
//            {
//                // Сбрасываем состояние двойного клика при обновлении навигации
//                ItemsGridView.SelectedItem = null;
//                _lastClickedItem = null;
//                _lastClickTime = DateTime.MinValue;

//                Task task = RefreshContent();
//            }, Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal);
//        }

//        public async Task RefreshContent()
//        {
//            try
//            {
//                Debug.WriteLine($"[TileViewIcons {PanelId}] Starting refresh");

//                // 1. Получаем путь в UI-потоке
//                string pathToReload = await DispatcherQueue.EnqueueAsync(() =>
//                {
//                    string path = _currentLoadedPath;
//                    if (string.IsNullOrEmpty(path) && PanelManager != null)
//                        path = PanelManager.CurrentPath;

//                    // Очищаем UI
//                    _currentLoadedPath = null;
//                    _isInitialized = false;
//                    Items.Clear();
//                    UpdateGridViewLayout();

//                    return path;
//                });

//                Debug.WriteLine($"[TileViewIcons {PanelId}] Reloading path: '{pathToReload}'");

//                // 2. Фоновые операции
//                _fileSystemService.ClearPanelCache(PanelId);
//                CancelCurrentOperation();

//                // 3. Загрузка (вернет Task, но обрабатываем в UI)
//                if (!string.IsNullOrEmpty(pathToReload))
//                {
//                    await LoadPathContents(pathToReload);
//                }
//                else
//                {
//                    await Task.Run(() => LoadInitialContent());
//                }

//                Debug.WriteLine($"[TileViewIcons {PanelId}] Refresh completed");
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileViewIcons {PanelId}] Refresh error: {ex}");

//                // Fallback
//                try
//                {
//                    await DispatcherQueue.EnqueueAsync(() => LoadInitialContent());
//                }
//                catch (Exception fallbackEx)
//                {
//                    Debug.WriteLine($"[TileViewIcons {PanelId}] Fallback failed: {fallbackEx}");
//                }
//            }
//        }
//    }
//}


using CommunityToolkit.WinUI;
using Core_FileManagement;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace ufm
{
    public sealed partial class TileViewIcons : UserControl, IDisposable, ISupportsIconSize, IRefreshablePanel
    {
        public string PanelId { get; set; } = "DefaultPanel";
        public PanelManager PanelManager { get; private set; }
        public event EventHandler NavigationChanged;

        private CancellationTokenSource _currentOperationCts;
        private readonly IDirectoryHistory _dummyHistory;
        private string _currentLoadedPath;

        // Флаг для отслеживания инициализации
        private bool _isInitialized = false;
        private bool _isLoading = false;
        public ObservableCollection<ExplorerItemViewModel> Items { get; } = new ObservableCollection<ExplorerItemViewModel>();

        // Сервис для работы с файловой системой
        private readonly FileSystemService _fileSystemService;

        // Размеры автоматически вычисляются на основе параметров иконок
        private double _itemWidth;
        private double _itemHeight;

        // Поля для отслеживания двойного клика (только для режима двойного клика)
        private DateTime _lastClickTime = DateTime.MinValue;
        private ExplorerItemViewModel _lastClickedItem = null;

        public double ItemWidth
        {
            get => _itemWidth;
            private set
            {
                if (_itemWidth != value)
                {
                    _itemWidth = value;
                    UpdateGridViewLayout();
                }
            }
        }

        public double ItemHeight
        {
            get => _itemHeight;
            private set
            {
                if (_itemHeight != value)
                {
                    _itemHeight = value;
                    UpdateGridViewLayout();
                }
            }
        }

        // Параметры отображения элементов
        private string _selectedSize = "Medium";

        // Отступы и padding для расчета общего размера
        private const int HorizontalPadding = 10;
        private const int VerticalPadding = 8;
        private const int TextBlockHeight = 40;
        private const int MinimumItemWidth = 100;
        private const int MinimumItemHeight = 100;

        public bool SingleClickOpenItem
        {
            get
            {
                try
                {
                    // Используем App.SettingsManager для получения настройки
                    // Значение по умолчанию изменено на false (режим двойного клика)
                    return App.SettingsManager?.GetSetting<bool>("SingleClickOpen", false) ?? false;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting Single Click Open setting: {ex}");
                    return false; // По умолчанию двойной клик
                }
            }
        }

        public TileViewIcons()
        {
            InitializeComponent();

            NavigationSettingsMediator.RegisterPanel(this);//13 10 2025

            // Создаем фиктивную историю для элементов отображения
            _dummyHistory = new DirectoryHistory("MyComputer", "Мой Компьютер");

            // Инициализируем сервис файловой системы
            _fileSystemService = new FileSystemService();

            ItemsGridView.ItemsSource = Items;
            Loaded += OnLoaded;

            // Подписка на событие изменения контейнера
            ItemsGridView.ContainerContentChanging += ItemsGridView_ContainerContentChanging;

            // Подписка на событие двойного клика
            ItemsGridView.DoubleTapped += ItemsGridView_DoubleTapped;

            // Инициализируем начальные размеры
            CalculateItemDimensions();
        }

        public void Dispose()
        {
            NavigationSettingsMediator.UnregisterPanel(this);
            _currentOperationCts?.Cancel();
            _currentOperationCts?.Dispose();
            _dummyHistory?.Dispose();

            Loaded -= OnLoaded;
            ItemsGridView.ContainerContentChanging -= ItemsGridView_ContainerContentChanging;
            ItemsGridView.DoubleTapped -= ItemsGridView_DoubleTapped;

            // Очищаем индивидуальный кэш этой панели
            _fileSystemService.ClearPanelCache(PanelId);

            _fileSystemService?.Dispose();

            if (PanelManager != null)
            {
                PanelManager.NavigationChanged -= OnPanelNavigationChanged;
            }

            foreach (var item in Items)
            {
                item?.Dispose();
            }
        }

        public void SetPanelManager(PanelManager panelManager)
        {
            if (PanelManager != null)
            {
                PanelManager.NavigationChanged -= OnPanelNavigationChanged;
            }

            // Очищаем индивидуальный кэш при смене менеджера
            _fileSystemService.ClearPanelCache(PanelId);

            PanelManager = panelManager;

            if (PanelManager != null)
            {
                PanelManager.NavigationChanged += OnPanelNavigationChanged;
                SetIconSize(PanelManager.State.IconSize);
            }
        }

        private void OnPanelNavigationChanged(object sender, EventArgs e)
        {
            if (_isLoading) return; // Игнорируем события во время загрузки

            if (PanelManager != null && PanelManager.CurrentPath != _currentLoadedPath)
            {
                DispatcherQueue.TryEnqueue(async () =>
                {
                    await LoadPathContents(PanelManager.CurrentPath);
                });
            }
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedSize))
            {
                _selectedSize = "Medium";
                Debug.WriteLine("Используются настройки по умолчанию.");
            }
            else
            {
                Debug.WriteLine($"Загружены настройки: Размер = {_selectedSize}");
            }

            CalculateItemDimensions();
            UpdateAllTiles();
            UpdateGridViewLayout();

            // Загружаем содержимое в зависимости от состояния PanelManager
            if (PanelManager != null && !string.IsNullOrEmpty(PanelManager.CurrentPath))
            {
                await LoadPathContents(PanelManager.CurrentPath);
                _isInitialized = true;
            }
            else if (!_isInitialized)
            {
                LoadInitialContent();
                _isInitialized = true;
            }
        }

        private void OnNavigationChanged()
        {
            Debug.WriteLine($"[{PanelId}] Navigation changed, raising event");
            NavigationChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetIconSize(string size)
        {
            _selectedSize = size;

            // Сохраняем в PanelManager
            if (PanelManager != null)
            {
                PanelManager.UpdateState(state => state.IconSize = size);
            }

            CalculateItemDimensions();
            Debug.WriteLine($"[{PanelId}] Установлен размер: {_selectedSize}");

            UpdateAllTiles();
            UpdateGridViewLayout();
            ItemsGridView.UpdateLayout();
        }

        #region Расчет размеров элементов

        private void CalculateItemDimensions()
        {
            // Получаем базовые размеры из менеджера
            var sizeParams = SizeManagerTile.GetSize(_selectedSize);

            // Добавляем дополнительные отступы (20 пикселей к ширине, 25 к высоте)
            ItemWidth = Math.Max(sizeParams.Width + 15, MinimumItemWidth);
            ItemHeight = Math.Max(sizeParams.Height + 45, MinimumItemHeight);

            Debug.WriteLine($"Calculated dimensions: Width={ItemWidth}, Height={ItemHeight}, Base=({sizeParams.Width}x{sizeParams.Height})");
        }

        #endregion

        #region Обновление отображения элементов

        private void UpdateAllTiles()
        {
            // Обновляем все видимые элементы
            foreach (var item in ItemsGridView.Items)
            {
                var container = ItemsGridView.ContainerFromItem(item) as GridViewItem;
                if (container != null)
                {
                    var tile = container.ContentTemplateRoot as BaseTileControl;
                    if (tile != null)
                    {
                        tile.UpdateSize(_selectedSize);
                    }
                }
            }
        }

        // Обработчик для обновления новых элементов при прокрутке
        private void ItemsGridView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.Phase != 0) return;

            if (args.ItemContainer is GridViewItem container)
            {
                var tile = container.ContentTemplateRoot as BaseTileControl;
                if (tile != null)
                {
                    tile.UpdateSize(_selectedSize);
                }
            }
        }

        #endregion

        private void UpdateGridViewLayout()
        {
            if (ItemsGridView?.ItemsPanelRoot is ItemsWrapGrid wrapGrid)
            {
                wrapGrid.ItemWidth = ItemWidth;
                wrapGrid.ItemHeight = ItemHeight;
                Debug.WriteLine($"GridView layout updated: ItemWidth={wrapGrid.ItemWidth}, ItemHeight={wrapGrid.ItemHeight}");
            }
        }

        private async void LoadInitialContent()
        {
            // ИСПРАВЛЕНО: добавляем проверку на уже загруженное содержимое
            if (_currentLoadedPath == "MyComputer" && Items.Count > 0)
            {
                Debug.WriteLine($"[{PanelId}] MyComputer content already loaded, skipping");
                return;
            }

            CancelCurrentOperation();
            Items.Clear();
            UpdateGridViewLayout();

            try
            {
                Debug.WriteLine($"[{PanelId}] Loading MyComputer content...");
                var items = await _fileSystemService.LoadMyComputerAsync(PanelId, _dummyHistory);
                foreach (var item in items)
                {
                    Items.Add(item);
                }
                _currentLoadedPath = "MyComputer";
                OnNavigationChanged();
                Debug.WriteLine($"[{PanelId}] MyComputer content loaded successfully, items count: {Items.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{PanelId}] Error loading initial content: {ex.Message}");
            }
        }

        internal async Task LoadPathContents(string path)
        {
            // ИСПРАВЛЕНО: добавляем проверку на уже загруженный путь
            if (_isLoading || _currentLoadedPath == path)
                return;

            try
            {
                _isLoading = true;
                switch (path)
                {
                    case "MyComputer":
                        LoadInitialContent();
                        _currentLoadedPath = path;
                        break;

                    case "Drives":
                        await LoadDrives();
                        _currentLoadedPath = path;
                        break;

                    case string p when Directory.Exists(p):
                        await LoadFolderContents(path);
                        _currentLoadedPath = path;

                        // Принудительно обновляем навигацию для PanelManager
                        if (PanelManager != null && PanelManager.CurrentPath != path)
                        {
                            PanelManager.NavigateTo(path);
                        }
                        break;

                    default:
                        // ИСПРАВЛЕНО: не вызываем LoadInitialContent повторно
                        if (_currentLoadedPath != "MyComputer")
                        {
                            LoadInitialContent();
                            _currentLoadedPath = "MyComputer";
                        }
                        break;
                }
            }
            finally
            {
                _isLoading = false;
            }

            OnNavigationChanged();
        }

        private async void ItemsGridView_OnItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is not ExplorerItemViewModel item) return;

            bool isSingleClickMode = SingleClickOpenItem;

            if (isSingleClickMode)
            {
                // Режим одного клика - сразу открываем
                await OpenItem(item);
            }
            else
            {
                // Режим двойного клика - только выделяем элемент
                // Открытие будет происходить через ItemsGridView_DoubleTapped
                ItemsGridView.SelectedItem = item;

                // Сохраняем информацию о последнем клике для обработки двойного клика вручную
                // (хотя в этом режиме лучше полагаться на DoubleTapped событие)
                var currentTime = DateTime.Now;
                bool isDoubleClick = (_lastClickedItem == item &&
                                     (currentTime - _lastClickTime).TotalMilliseconds < 500);

                if (isDoubleClick)
                {
                    // Если пользователь очень быстро кликает, обрабатываем как двойной клик
                    await OpenItem(item);
                    _lastClickedItem = null;
                }
                else
                {
                    // Просто сохраняем информацию о клике
                    _lastClickedItem = item;
                    _lastClickTime = currentTime;
                }
            }
        }

        private async void ItemsGridView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            // В режиме одного клика игнорируем двойное нажатие
            if (SingleClickOpenItem) return;

            // Находим элемент, на который было совершено двойное нажатие
            var element = e.OriginalSource as FrameworkElement;
            while (element != null && element.DataContext as ExplorerItemViewModel == null)
            {
                element = VisualTreeHelper.GetParent(element) as FrameworkElement;
            }

            if (element?.DataContext is ExplorerItemViewModel item)
            {
                await OpenItem(item);
            }
        }

        private async Task OpenItem(ExplorerItemViewModel item)
        {
            try
            {
                // Сбрасываем выделение при открытии
                ItemsGridView.SelectedItem = null;
                _lastClickedItem = null;
                _lastClickTime = DateTime.MinValue;

                if (item.Name == "..")
                {
                    PanelManager?.GoBack();
                    return;
                }

                if (item.FilePath == "Drives" ||
                    item.FilePath == "MyComputer" ||
                    Directory.Exists(item.FilePath))
                {
                    await LoadPathContents(item.FilePath);
                    PanelManager?.NavigateTo(item.FilePath);
                }
                else if (File.Exists(item.FilePath))
                {
                    Debug.WriteLine($"File selected: {item.FilePath}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening item {item?.Name}: {ex.Message}");
            }
        }

        private void CancelCurrentOperation()
        {
            _currentOperationCts?.Cancel();
            _currentOperationCts?.Dispose();
            _currentOperationCts = new CancellationTokenSource();
            _fileSystemService.CancelAllOperations();
        }

        private async Task LoadDrives()
        {
            // ИСПРАВЛЕНО: проверяем, не загружены ли уже диски
            if (_currentLoadedPath == "Drives" && Items.Count > 1) // >1 потому что есть кнопка "Назад"
            {
                Debug.WriteLine($"[{PanelId}] Drives already loaded, skipping");
                return;
            }

            CancelCurrentOperation();

            Items.Clear();
            UpdateGridViewLayout();

            try
            {
                Debug.WriteLine($"[{PanelId}] Loading drives...");
                var driveItems = await _fileSystemService.LoadDrivesAsync(_dummyHistory);
                foreach (var item in driveItems)
                {
                    Items.Add(item);
                }
                Debug.WriteLine($"[{PanelId}] Drives loaded successfully, items count: {Items.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{PanelId}] Error loading drives: {ex.Message}");
                LoadInitialContent();
            }

            OnNavigationChanged();
        }

        private async Task LoadFolderContents(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
                return;

            if (!Directory.Exists(folderPath))
            {
                Debug.WriteLine($"Directory does not exist: {folderPath}");
                PanelManager?.GoBack();
                return;
            }

            // ИСПРАВЛЕНО: проверяем, не загружена ли уже эта папка
            if (_currentLoadedPath == folderPath && Items.Count > 0)
            {
                Debug.WriteLine($"[{PanelId}] Folder {folderPath} already loaded, skipping");
                return;
            }

            CancelCurrentOperation();

            try
            {
                Debug.WriteLine($"[{PanelId}] Loading folder contents: {folderPath}");
                var folderItems = await _fileSystemService.LoadFolderContentsAsync(folderPath, _dummyHistory);

                // Обновляем UI
                await this.DispatcherQueue.EnqueueAsync(() =>
                {
                    Items.Clear();
                    foreach (var item in folderItems)
                    {
                        Items.Add(item);
                    }
                    UpdateGridViewLayout();
                });
                Debug.WriteLine($"[{PanelId}] Folder contents loaded successfully, items count: {Items.Count}");
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"[{PanelId}] Folder loading canceled");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{PanelId}] Error loading folder contents {folderPath}: {ex.Message}");
                PanelManager?.GoBack();
            }
        }

        public void RefreshNavigation()
        {
            Debug.WriteLine($"[TileViewIcons {PanelId}] Refreshing navigation via mediator");

            _ = this.DispatcherQueue.EnqueueAsync(() =>
            {
                // Сбрасываем состояние двойного клика при обновлении навигации
                ItemsGridView.SelectedItem = null;
                _lastClickedItem = null;
                _lastClickTime = DateTime.MinValue;

                Task task = RefreshContent();
            }, Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal);
        }

        public async Task RefreshContent()
        {
            try
            {
                Debug.WriteLine($"[TileViewIcons {PanelId}] Starting refresh");

                // 1. Получаем путь в UI-потоке
                string pathToReload = await DispatcherQueue.EnqueueAsync(() =>
                {
                    string path = _currentLoadedPath;
                    if (string.IsNullOrEmpty(path) && PanelManager != null)
                        path = PanelManager.CurrentPath;

                    // Очищаем UI
                    _currentLoadedPath = null;
                    _isInitialized = false;
                    Items.Clear();
                    UpdateGridViewLayout();

                    return path;
                });

                Debug.WriteLine($"[TileViewIcons {PanelId}] Reloading path: '{pathToReload}'");

                // 2. Фоновые операции
                _fileSystemService.ClearPanelCache(PanelId);
                CancelCurrentOperation();

                // 3. Загрузка (вернет Task, но обрабатываем в UI)
                if (!string.IsNullOrEmpty(pathToReload))
                {
                    await LoadPathContents(pathToReload);
                }
                else
                {
                    await Task.Run(() => LoadInitialContent());
                }

                Debug.WriteLine($"[TileViewIcons {PanelId}] Refresh completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileViewIcons {PanelId}] Refresh error: {ex}");

                // Fallback
                try
                {
                    await DispatcherQueue.EnqueueAsync(() => LoadInitialContent());
                }
                catch (Exception fallbackEx)
                {
                    Debug.WriteLine($"[TileViewIcons {PanelId}] Fallback failed: {fallbackEx}");
                }
            }
        }
    }
}