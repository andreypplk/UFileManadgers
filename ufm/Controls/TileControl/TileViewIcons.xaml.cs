using CommunityToolkit.WinUI;
using Core_FileManagement;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Windows.Storage;

namespace ufm
{
    public sealed partial class TileViewIcons : UserControl, IDisposable, ISupportsIconSize, IRefreshablePanel
    {
        #region Поля и свойства

        public string PanelId { get; set; } = "DefaultPanel";
        public PanelManager PanelManager { get; private set; }
        public event EventHandler NavigationChanged;

        private CancellationTokenSource _currentOperationCts;
        private readonly IDirectoryHistory _dummyHistory;
        private string _currentLoadedPath;

        private bool _isInitialized = false;
        private bool _isLoading = false;
        public ObservableCollection<ExplorerItemViewModel> Items { get; } = new ObservableCollection<ExplorerItemViewModel>();

        private readonly FileSystemService _fileSystemService;

        private double _itemWidth;
        private double _itemHeight;

        private DateTime _lastClickTime = DateTime.MinValue;
        private ExplorerItemViewModel _lastClickedItem = null;

        private ExplorerItemViewModel _shiftSelectionStartItem = null;
        private int _lastSelectedIndex = -1;

        private bool _isCtrlPressed = false;
        private bool _isShiftPressed = false;
        private bool _isAltPressed = false;

        private bool _isDragSelecting = false;
        private bool _isMouseDown = false;
        private Vector2 _dragStartPoint;
        private Rectangle _selectionRectangle;
        private Canvas _selectionCanvas;
        private ScrollViewer _gridViewScrollViewer;

        private DispatcherTimer _hoverSelectionTimer;
        private ExplorerItemViewModel _hoveredItem = null;
        private const int HOVER_DELAY_MS = 300;

        private bool _wasClickHandled = false;

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

        private string _selectedSize = "Medium";

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
                    return App.SettingsManager?.GetSetting<bool>("SingleClickOpen", false) ?? false;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting Single Click Open setting: {ex}");
                    return false;
                }
            }
        }

        #endregion

        #region Конструктор и Dispose

        public TileViewIcons()
        {
            InitializeComponent();

            NavigationSettingsMediator.RegisterPanel(this);

            _dummyHistory = new DirectoryHistory("MyComputer", "Мой Компьютер");

            _fileSystemService = new FileSystemService();

            ItemsGridView.ItemsSource = Items;
            Loaded += OnLoaded;

            InitializeHoverSelectionTimer();

            ItemsGridView.ContainerContentChanging += ItemsGridView_ContainerContentChanging;
            ItemsGridView.DoubleTapped += ItemsGridView_DoubleTapped;
            ItemsGridView.SelectionChanged += ItemsGridView_SelectionChanged;
            ItemsGridView.KeyDown += ItemsGridView_KeyDown;
            ItemsGridView.KeyUp += ItemsGridView_KeyUp;
            ItemsGridView.PreviewKeyDown += ItemsGridView_PreviewKeyDown;

            ItemsGridView.PointerEntered += ItemsGridView_PointerEntered;
            ItemsGridView.PointerExited += ItemsGridView_PointerExited;
            ItemsGridView.PointerMoved += ItemsGridView_PointerMoved;

            ItemsGridView.PointerPressed += ItemsGridView_PointerPressed;
            ItemsGridView.PointerReleased += ItemsGridView_PointerReleased;
            ItemsGridView.PointerMoved += ItemsGridView_PointerMovedForDrag;

            this.Loaded += (s, e) => InitializeSelectionCanvas();

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
            ItemsGridView.SelectionChanged -= ItemsGridView_SelectionChanged;
            ItemsGridView.KeyDown -= ItemsGridView_KeyDown;
            ItemsGridView.KeyUp -= ItemsGridView_KeyUp;
            ItemsGridView.PreviewKeyDown -= ItemsGridView_PreviewKeyDown;
            ItemsGridView.PointerEntered -= ItemsGridView_PointerEntered;
            ItemsGridView.PointerExited -= ItemsGridView_PointerExited;
            ItemsGridView.PointerMoved -= ItemsGridView_PointerMoved;
            ItemsGridView.PointerPressed -= ItemsGridView_PointerPressed;
            ItemsGridView.PointerReleased -= ItemsGridView_PointerReleased;
            ItemsGridView.PointerMoved -= ItemsGridView_PointerMovedForDrag;

            if (_hoverSelectionTimer != null)
            {
                _hoverSelectionTimer.Stop();
                _hoverSelectionTimer.Tick -= HoverSelectionTimer_Tick;
                _hoverSelectionTimer = null;
            }

            _fileSystemService.ClearPanelCache(PanelId);

            _fileSystemService?.Dispose();

            if (PanelManager != null)
            {
                PanelManager.NavigationChanged -= OnPanelNavigationChanged;
            }

            if (_selectionCanvas != null)
            {
                var grid = ItemsGridView.Parent as Grid;
                if (grid != null)
                {
                    grid.Children.Remove(_selectionCanvas);
                }
                _selectionCanvas = null;
            }

            foreach (var item in Items)
            {
                item?.Dispose();
            }
        }

        #endregion

        #region Инициализация таймера выделения при наведении

        private void InitializeHoverSelectionTimer()
        {
            _hoverSelectionTimer = new DispatcherTimer();
            _hoverSelectionTimer.Interval = TimeSpan.FromMilliseconds(HOVER_DELAY_MS);
            _hoverSelectionTimer.Tick += HoverSelectionTimer_Tick;
        }

        private void HoverSelectionTimer_Tick(object sender, object e)
        {
            _hoverSelectionTimer.Stop();

            if (_hoveredItem != null && SingleClickOpenItem && !_isCtrlPressed && !_isShiftPressed && !_isDragSelecting)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    SelectItemOnHover(_hoveredItem);
                });
            }
        }

        private void SelectItemOnHover(ExplorerItemViewModel item)
        {
            if (item == null || _isDragSelecting) return;

            if (ItemsGridView.SelectedItems.Contains(item))
                return;

            if (!_isCtrlPressed && !_isShiftPressed)
            {
                ItemsGridView.SelectedItems.Clear();
            }

            if (!ItemsGridView.SelectedItems.Contains(item))
            {
                ItemsGridView.SelectedItems.Add(item);
            }

            ItemsGridView.SelectedItem = item;

            Debug.WriteLine($"Hover selection: {item.Name}");
        }

        #endregion

        #region Инициализация Canvas для выделения - ПРАВИЛЬНАЯ РАМКА из второго кода

        private void InitializeSelectionCanvas()
        {
            if (_selectionCanvas != null) return;

            _selectionCanvas = new Canvas
            {
                IsHitTestVisible = false,
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent)
            };

            var grid = ItemsGridView.Parent as Grid;
            if (grid != null)
            {
                grid.Children.Add(_selectionCanvas);
                Canvas.SetZIndex(_selectionCanvas, 1000);
            }
        }

        #endregion

        #region Обработка событий мыши для выделения при наведении

        private void ItemsGridView_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (!SingleClickOpenItem || _isDragSelecting) return;

            var element = e.OriginalSource as FrameworkElement;
            var item = FindParentDataContext<ExplorerItemViewModel>(element);

            if (item != null && item != _hoveredItem)
            {
                _hoveredItem = item;
                StartHoverSelectionTimer();
            }
        }

        private void ItemsGridView_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (!SingleClickOpenItem || _isDragSelecting) return;

            StopHoverSelectionTimer();
            _hoveredItem = null;
        }

        private void ItemsGridView_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!SingleClickOpenItem || _isDragSelecting) return;

            var element = e.OriginalSource as FrameworkElement;
            var item = FindParentDataContext<ExplorerItemViewModel>(element);

            if (item != null && item != _hoveredItem)
            {
                _hoveredItem = item;
                RestartHoverSelectionTimer();
            }
            else if (item == null)
            {
                StopHoverSelectionTimer();
                _hoveredItem = null;
            }
        }

        private T FindParentDataContext<T>(FrameworkElement element) where T : class
        {
            while (element != null)
            {
                if (element.DataContext is T dataContext)
                    return dataContext;

                element = VisualTreeHelper.GetParent(element) as FrameworkElement;
            }
            return null;
        }

        private void StartHoverSelectionTimer()
        {
            if (_hoverSelectionTimer != null && !_hoverSelectionTimer.IsEnabled)
            {
                _hoverSelectionTimer.Start();
            }
        }

        private void StopHoverSelectionTimer()
        {
            if (_hoverSelectionTimer != null && _hoverSelectionTimer.IsEnabled)
            {
                _hoverSelectionTimer.Stop();
            }
        }

        private void RestartHoverSelectionTimer()
        {
            StopHoverSelectionTimer();
            StartHoverSelectionTimer();
        }

        #endregion

        #region Выделение областью мышью - СОВМЕЩЕННЫЙ ВАРИАНТ

        private void ItemsGridView_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(ItemsGridView);

            // Сохраняем точку начала выделения
            _dragStartPoint = new Vector2((float)point.Position.X, (float)point.Position.Y);
            _wasClickHandled = false;

            // Если нажата левая кнопка мыши и не на элементе
            if (point.Properties.IsLeftButtonPressed)
            {
                var element = e.OriginalSource as FrameworkElement;
                var item = FindParentDataContext<ExplorerItemViewModel>(element);

                // Если кликнули на пустом месте (не на элементе)
                if (item == null)
                {
                    _isDragSelecting = true;
                    ItemsGridView.CapturePointer(e.Pointer);

                    // Создаем прямоугольник выделения (из второго кода)
                    CreateSelectionRectangle();
                    UpdateSelectionRectangle(_dragStartPoint, _dragStartPoint);

                    e.Handled = true;
                }
            }
        }

        private void ItemsGridView_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragSelecting)
            {
                _isDragSelecting = false;
                ItemsGridView.ReleasePointerCapture(e.Pointer);

                // Удаляем прямоугольник выделения (из второго кода)
                RemoveSelectionRectangle();

                e.Handled = true;
            }

            _wasClickHandled = false;
        }

        private void ItemsGridView_PointerMovedForDrag(object sender, PointerRoutedEventArgs e)
        {
            if (!_isDragSelecting) return;

            var point = e.GetCurrentPoint(ItemsGridView);
            var currentPoint = new Vector2((float)point.Position.X, (float)point.Position.Y);

            // Обновляем прямоугольник выделения (из второго кода)
            UpdateSelectionRectangle(_dragStartPoint, currentPoint);

            // Выделяем элементы внутри прямоугольника (работающее выделение из первого кода)
            SelectItemsInRectangle(_dragStartPoint, currentPoint);

            e.Handled = true;
        }

        // Методы для правильной рамки из второго кода
        private void CreateSelectionRectangle()
        {
            if (_selectionRectangle != null) return;

            if (_selectionCanvas == null)
            {
                InitializeSelectionCanvas();
            }

            _selectionRectangle = new Rectangle
            {
                Stroke = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue),
                StrokeThickness = 1,
                Fill = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue) { Opacity = 0.3 },
                StrokeDashArray = new DoubleCollection() { 2, 2 },
                Width = 0,
                Height = 0
            };

            if (_selectionCanvas != null)
            {
                _selectionCanvas.Children.Clear();
                _selectionCanvas.Children.Add(_selectionRectangle);

                Canvas.SetLeft(_selectionRectangle, 0);
                Canvas.SetTop(_selectionRectangle, 0);
            }
        }

        private void UpdateSelectionRectangle(Vector2 startPoint, Vector2 endPoint)
        {
            if (_selectionRectangle == null || _selectionCanvas == null) return;

            float left = Math.Min(startPoint.X, endPoint.X);
            float top = Math.Min(startPoint.Y, endPoint.Y);
            float width = Math.Abs(endPoint.X - startPoint.X);
            float height = Math.Abs(endPoint.Y - startPoint.Y);

            Canvas.SetLeft(_selectionRectangle, left);
            Canvas.SetTop(_selectionRectangle, top);
            _selectionRectangle.Width = width;
            _selectionRectangle.Height = height;
        }

        private void RemoveSelectionRectangle()
        {
            if (_selectionRectangle != null && _selectionCanvas != null)
            {
                _selectionCanvas.Children.Remove(_selectionRectangle);
                _selectionRectangle = null;
            }
        }

        // Работающее выделение из первого кода
        private void SelectItemsInRectangle(Vector2 startPoint, Vector2 endPoint)
        {
            if (Items.Count == 0) return;

            // Определяем границы прямоугольника
            float left = Math.Min(startPoint.X, endPoint.X);
            float right = Math.Max(startPoint.X, endPoint.X);
            float top = Math.Min(startPoint.Y, endPoint.Y);
            float bottom = Math.Max(startPoint.Y, endPoint.Y);

            // Получаем ScrollViewer для учета прокрутки
            if (_gridViewScrollViewer == null)
            {
                _gridViewScrollViewer = FindVisualChild<ScrollViewer>(ItemsGridView);
            }

            // Очищаем выделение, если не нажат Ctrl
            if (!_isCtrlPressed)
            {
                ItemsGridView.SelectedItems.Clear();
            }

            // Проходим по всем элементам GridView (работающий подход из первого кода)
            for (int i = 0; i < Items.Count; i++)
            {
                var container = ItemsGridView.ContainerFromIndex(i) as GridViewItem;
                if (container != null && container.Visibility == Visibility.Visible)
                {
                    var transform = container.TransformToVisual(ItemsGridView);
                    var position = transform.TransformPoint(new Windows.Foundation.Point(0, 0));

                    // Получаем позицию элемента
                    float itemLeft = (float)position.X;
                    float itemTop = (float)position.Y;
                    float itemRight = itemLeft + (float)container.ActualWidth;
                    float itemBottom = itemTop + (float)container.ActualHeight;

                    // Проверяем пересечение с прямоугольником выделения
                    bool intersects = itemRight > left && itemLeft < right &&
                                      itemBottom > top && itemTop < bottom;

                    var item = Items[i];
                    if (intersects)
                    {
                        // Добавляем элемент в выделение
                        if (!ItemsGridView.SelectedItems.Contains(item))
                        {
                            ItemsGridView.SelectedItems.Add(item);
                        }
                    }
                    else if (!_isCtrlPressed)
                    {
                        // Убираем элемент из выделения, если не нажат Ctrl
                        if (ItemsGridView.SelectedItems.Contains(item))
                        {
                            ItemsGridView.SelectedItems.Remove(item);
                        }
                    }
                }
                else if (!_isCtrlPressed)
                {
                    // Если контейнер не видим или не создан, и не нажат Ctrl,
                    // убираем элемент из выделения
                    var item = Items[i];
                    if (ItemsGridView.SelectedItems.Contains(item))
                    {
                        ItemsGridView.SelectedItems.Remove(item);
                    }
                }
            }

            // Обновляем визуальное состояние
            UpdateSelectionVisual();
        }

        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                {
                    return result;
                }

                var descendant = FindVisualChild<T>(child);
                if (descendant != null)
                {
                    return descendant;
                }
            }
            return null;
        }

        #endregion

        #region PanelManager и навигация

        public void SetPanelManager(PanelManager panelManager)
        {
            if (PanelManager != null)
            {
                PanelManager.NavigationChanged -= OnPanelNavigationChanged;
            }

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
            if (_isLoading) return;

            if (PanelManager != null && PanelManager.CurrentPath != _currentLoadedPath)
            {
                DispatcherQueue.TryEnqueue(async () =>
                {
                    await LoadPathContents(PanelManager.CurrentPath);
                });
            }
        }

        private void OnNavigationChanged()
        {
            Debug.WriteLine($"[{PanelId}] Navigation changed, raising event");
            NavigationChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Загрузка и инициализация

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

        #endregion

        #region Размеры иконок

        public void SetIconSize(string size)
        {
            _selectedSize = size;

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

        private void CalculateItemDimensions()
        {
            var sizeParams = SizeManagerTile.GetSize(_selectedSize);
            ItemWidth = Math.Max(sizeParams.Width + 20, MinimumItemWidth);
            ItemHeight = Math.Max(sizeParams.Height + 25, MinimumItemHeight);
            Debug.WriteLine($"Calculated dimensions: Width={ItemWidth}, Height={ItemHeight}");
        }

        #endregion

        #region Обновление отображения элементов

        private void UpdateAllTiles()
        {
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

        #region Обновление выделения

        private void UpdateSelectionVisual()
        {
            foreach (var item in ItemsGridView.SelectedItems)
            {
                var container = ItemsGridView.ContainerFromItem(item) as GridViewItem;
                if (container != null)
                {
                    VisualStateManager.GoToState(container, "Selected", false);
                }
            }
        }

        #endregion

        #region Layout и обновление UI

        private void UpdateGridViewLayout()
        {
            if (ItemsGridView?.ItemsPanelRoot is ItemsWrapGrid wrapGrid)
            {
                wrapGrid.ItemWidth = ItemWidth;
                wrapGrid.ItemHeight = ItemHeight;
                Debug.WriteLine($"GridView layout updated: ItemWidth={wrapGrid.ItemWidth}, ItemHeight={wrapGrid.ItemHeight}");
                UpdateSelectionVisual();
            }
        }

        private void UpdateUIForSelection()
        {
            int selectedCount = ItemsGridView.SelectedItems.Count;
            Debug.WriteLine($"Update UI: {selectedCount} items selected");
        }

        #endregion

        #region Загрузка содержимого

        private async void LoadInitialContent()
        {
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

                        if (PanelManager != null && PanelManager.CurrentPath != path)
                        {
                            PanelManager.NavigateTo(path);
                        }
                        break;

                    default:
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

        private async Task LoadDrives()
        {
            if (_currentLoadedPath == "Drives" && Items.Count > 1)
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

        private void CancelCurrentOperation()
        {
            _currentOperationCts?.Cancel();
            _currentOperationCts?.Dispose();
            _currentOperationCts = new CancellationTokenSource();
            _fileSystemService.CancelAllOperations();
        }

        #endregion

        #region Обработка клавиатуры

        private bool IsCtrlPressed() => _isCtrlPressed;
        private bool IsShiftPressed() => _isShiftPressed;
        private bool IsAltPressed() => _isAltPressed;

        private void ItemsGridView_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            UpdateModifierKeyState(e.Key, true);

            bool isCtrlPressed = _isCtrlPressed;
            bool isShiftPressed = _isShiftPressed;

            Debug.WriteLine($"KeyDown: {e.Key}, Ctrl: {isCtrlPressed}, Shift: {isShiftPressed}");

            switch (e.Key)
            {
                case VirtualKey.A when isCtrlPressed && !isShiftPressed:
                    ItemsGridView.SelectAll();
                    e.Handled = true;
                    Debug.WriteLine("Ctrl+A: Select all");
                    break;

                case VirtualKey.Space when isCtrlPressed:
                    ToggleCurrentSelection();
                    e.Handled = true;
                    Debug.WriteLine("Ctrl+Space: Toggle selection");
                    break;

                case VirtualKey.Enter:
                    if (ItemsGridView.SelectedItem != null)
                    {
                        OpenSelectedItem();
                        e.Handled = true;
                        Debug.WriteLine("Enter: Open selected item");
                    }
                    break;

                case VirtualKey.F2:
                    if (ItemsGridView.SelectedItems.Count == 1)
                    {
                        RenameSelectedItem();
                        e.Handled = true;
                        Debug.WriteLine("F2: Rename selected item");
                    }
                    break;

                case VirtualKey.Delete:
                    if (ItemsGridView.SelectedItems.Count > 0)
                    {
                        DeleteSelectedItems();
                        e.Handled = true;
                        Debug.WriteLine("Delete: Delete selected items");
                    }
                    break;

                case VirtualKey.Up:
                case VirtualKey.Down:
                case VirtualKey.Left:
                case VirtualKey.Right:
                    HandleArrowKeyNavigation(e.Key, isCtrlPressed, isShiftPressed);
                    e.Handled = true;
                    break;

                case VirtualKey.Home:
                case VirtualKey.End:
                    HandleHomeEndNavigation(e.Key, isCtrlPressed, isShiftPressed);
                    e.Handled = true;
                    break;

                case VirtualKey.PageUp:
                case VirtualKey.PageDown:
                    HandlePageNavigation(e.Key, isCtrlPressed, isShiftPressed);
                    e.Handled = true;
                    break;
            }
        }

        private void ItemsGridView_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            UpdateModifierKeyState(e.Key, false);

            if (e.Key == VirtualKey.Shift)
            {
                _shiftSelectionStartItem = null;
                Debug.WriteLine("Shift released, reset selection start");
            }
        }

        private void ItemsGridView_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
        }

        private void UpdateModifierKeyState(VirtualKey key, bool isPressed)
        {
            switch (key)
            {
                case VirtualKey.Control:
                case VirtualKey.LeftControl:
                case VirtualKey.RightControl:
                    _isCtrlPressed = isPressed;
                    break;
                case VirtualKey.Shift:
                case VirtualKey.LeftShift:
                case VirtualKey.RightShift:
                    _isShiftPressed = isPressed;
                    break;
                case VirtualKey.Menu:
                case VirtualKey.LeftMenu:
                case VirtualKey.RightMenu:
                    _isAltPressed = isPressed;
                    break;
            }
        }

        #endregion

        #region Навигация клавишами

        private void HandleArrowKeyNavigation(VirtualKey key, bool isCtrlPressed, bool isShiftPressed)
        {
            int currentIndex = ItemsGridView.SelectedIndex;
            int newIndex = currentIndex;
            int itemsPerRow = CalculateItemsPerRow();

            switch (key)
            {
                case VirtualKey.Up:
                    newIndex = Math.Max(0, currentIndex - itemsPerRow);
                    break;
                case VirtualKey.Down:
                    newIndex = Math.Min(Items.Count - 1, currentIndex + itemsPerRow);
                    break;
                case VirtualKey.Left:
                    newIndex = Math.Max(0, currentIndex - 1);
                    break;
                case VirtualKey.Right:
                    newIndex = Math.Min(Items.Count - 1, currentIndex + 1);
                    break;
            }

            if (newIndex != currentIndex && newIndex >= 0 && newIndex < Items.Count)
            {
                var newItem = Items[newIndex];

                if (isShiftPressed)
                {
                    HandleShiftArrowSelection(newIndex);
                }
                else if (isCtrlPressed)
                {
                    ItemsGridView.SelectedItem = newItem;
                    ItemsGridView.ScrollIntoView(newItem);
                    _lastSelectedIndex = newIndex;
                }
                else
                {
                    ItemsGridView.SelectedItems.Clear();
                    ItemsGridView.SelectedItem = newItem;
                    ItemsGridView.ScrollIntoView(newItem);
                    _lastSelectedIndex = newIndex;
                    _shiftSelectionStartItem = newItem;
                }

                Debug.WriteLine($"Arrow navigation: {key} from {currentIndex} to {newIndex}, Ctrl={isCtrlPressed}, Shift={isShiftPressed}");
            }
        }

        private void HandleHomeEndNavigation(VirtualKey key, bool isCtrlPressed, bool isShiftPressed)
        {
            int newIndex = -1;

            switch (key)
            {
                case VirtualKey.Home:
                    newIndex = 0;
                    break;
                case VirtualKey.End:
                    newIndex = Items.Count - 1;
                    break;
            }

            if (newIndex >= 0 && newIndex < Items.Count)
            {
                var newItem = Items[newIndex];

                if (isShiftPressed)
                {
                    HandleShiftRangeSelection(newIndex);
                }
                else if (isCtrlPressed)
                {
                    ItemsGridView.SelectedItem = newItem;
                    ItemsGridView.ScrollIntoView(newItem);
                }
                else
                {
                    ItemsGridView.SelectedItems.Clear();
                    ItemsGridView.SelectedItem = newItem;
                    ItemsGridView.ScrollIntoView(newItem);
                }

                Debug.WriteLine($"{key} navigation to index {newIndex}");
            }
        }

        private void HandlePageNavigation(VirtualKey key, bool isCtrlPressed, bool isShiftPressed)
        {
            int currentIndex = ItemsGridView.SelectedIndex;
            int itemsPerPage = CalculateItemsPerPage();
            int newIndex = currentIndex;

            switch (key)
            {
                case VirtualKey.PageUp:
                    newIndex = Math.Max(0, currentIndex - itemsPerPage);
                    break;
                case VirtualKey.PageDown:
                    newIndex = Math.Min(Items.Count - 1, currentIndex + itemsPerPage);
                    break;
            }

            if (newIndex != currentIndex)
            {
                var newItem = Items[newIndex];

                if (isShiftPressed)
                {
                    HandleShiftRangeSelection(newIndex);
                }
                else
                {
                    ItemsGridView.SelectedItems.Clear();
                    ItemsGridView.SelectedItem = newItem;
                    ItemsGridView.ScrollIntoView(newItem);
                }

                Debug.WriteLine($"{key}: from {currentIndex} to {newIndex}");
            }
        }

        private int CalculateItemsPerRow()
        {
            if (ItemWidth <= 0) return 1;

            var grid = ItemsGridView.ItemsPanelRoot as ItemsWrapGrid;
            if (grid != null && grid.ActualWidth > 0)
            {
                return (int)Math.Floor(grid.ActualWidth / ItemWidth);
            }

            return 6;
        }

        private int CalculateItemsPerPage()
        {
            if (ItemHeight <= 0) return 1;

            var grid = ItemsGridView.ItemsPanelRoot as FrameworkElement;
            if (grid != null && grid.ActualHeight > 0)
            {
                int rowsPerPage = (int)Math.Floor(grid.ActualHeight / ItemHeight);
                return rowsPerPage * CalculateItemsPerRow();
            }

            return 20;
        }

        #endregion

        #region Выделение элементов

        private void HandleShiftArrowSelection(int newIndex)
        {
            if (_shiftSelectionStartItem == null)
            {
                _shiftSelectionStartItem = ItemsGridView.SelectedItem as ExplorerItemViewModel;
                if (_shiftSelectionStartItem == null && Items.Count > 0)
                {
                    _shiftSelectionStartItem = Items[0];
                }
            }

            if (_shiftSelectionStartItem != null)
            {
                int startIndex = Items.IndexOf(_shiftSelectionStartItem);
                int endIndex = newIndex;

                SelectRange(startIndex, endIndex);
                Debug.WriteLine($"Shift selection from {startIndex} to {endIndex}");
            }
        }

        private void HandleShiftRangeSelection(int newIndex)
        {
            int currentIndex = ItemsGridView.SelectedIndex;
            if (currentIndex >= 0)
            {
                SelectRange(currentIndex, newIndex);
                Debug.WriteLine($"Shift range selection from {currentIndex} to {newIndex}");
            }
        }

        private void SelectRange(int startIndex, int endIndex)
        {
            if (startIndex < 0 || endIndex < 0 || startIndex >= Items.Count || endIndex >= Items.Count)
                return;

            int minIndex = Math.Min(startIndex, endIndex);
            int maxIndex = Math.Max(startIndex, endIndex);

            if (!_isCtrlPressed)
            {
                ItemsGridView.SelectedItems.Clear();
            }

            for (int i = minIndex; i <= maxIndex; i++)
            {
                if (!ItemsGridView.SelectedItems.Contains(Items[i]))
                {
                    ItemsGridView.SelectedItems.Add(Items[i]);
                }
            }

            ItemsGridView.SelectedItem = Items[endIndex];
            ItemsGridView.ScrollIntoView(Items[endIndex]);
        }

        private void ToggleCurrentSelection()
        {
            if (ItemsGridView.SelectedItem is ExplorerItemViewModel currentItem)
            {
                if (ItemsGridView.SelectedItems.Contains(currentItem))
                {
                    ItemsGridView.SelectedItems.Remove(currentItem);
                    Debug.WriteLine($"Removed {currentItem.Name} from selection");
                }
                else
                {
                    ItemsGridView.SelectedItems.Add(currentItem);
                    Debug.WriteLine($"Added {currentItem.Name} to selection");
                }
            }
        }

        #endregion

        #region Обработка мыши

        private async void ItemsGridView_OnItemClick(object sender, ItemClickEventArgs e)
        {
            if (_wasClickHandled) return;

            if (e.ClickedItem is not ExplorerItemViewModel item) return;

            bool isSingleClickMode = SingleClickOpenItem;
            bool isCtrlPressed = _isCtrlPressed;
            bool isShiftPressed = _isShiftPressed;

            _wasClickHandled = true;

            if (isSingleClickMode)
            {
                if (isShiftPressed)
                {
                    if (_shiftSelectionStartItem == null)
                    {
                        _shiftSelectionStartItem = ItemsGridView.SelectedItem as ExplorerItemViewModel;
                    }

                    if (_shiftSelectionStartItem != null)
                    {
                        int startIndex = Items.IndexOf(_shiftSelectionStartItem);
                        int endIndex = Items.IndexOf(item);

                        if (startIndex >= 0 && endIndex >= 0)
                        {
                            SelectRange(startIndex, endIndex);
                        }
                    }
                }
                else if (isCtrlPressed)
                {
                    if (ItemsGridView.SelectedItems.Contains(item))
                    {
                        ItemsGridView.SelectedItems.Remove(item);
                    }
                    else
                    {
                        ItemsGridView.SelectedItems.Add(item);
                    }
                    _shiftSelectionStartItem = item;
                }
                else
                {
                    await OpenItem(item);
                }
            }
            else
            {
                if (isShiftPressed)
                {
                    if (_shiftSelectionStartItem == null)
                    {
                        _shiftSelectionStartItem = ItemsGridView.SelectedItem as ExplorerItemViewModel;
                    }

                    if (_shiftSelectionStartItem != null)
                    {
                        int startIndex = Items.IndexOf(_shiftSelectionStartItem);
                        int endIndex = Items.IndexOf(item);

                        if (startIndex >= 0 && endIndex >= 0)
                        {
                            SelectRange(startIndex, endIndex);
                        }
                    }
                }
                else if (isCtrlPressed)
                {
                    if (ItemsGridView.SelectedItems.Contains(item))
                    {
                        ItemsGridView.SelectedItems.Remove(item);
                    }
                    else
                    {
                        ItemsGridView.SelectedItems.Add(item);
                    }
                    _shiftSelectionStartItem = item;
                }
                else
                {
                    ItemsGridView.SelectedItems.Clear();
                    ItemsGridView.SelectedItem = item;
                    _shiftSelectionStartItem = item;
                    _lastSelectedIndex = Items.IndexOf(item);

                    var currentTime = DateTime.Now;
                    bool isDoubleClick = (_lastClickedItem == item &&
                                         (currentTime - _lastClickTime).TotalMilliseconds < 500);

                    if (isDoubleClick)
                    {
                        await OpenItem(item);
                        _lastClickedItem = null;
                    }
                    else
                    {
                        _lastClickedItem = item;
                        _lastClickTime = currentTime;
                    }
                }
            }
        }

        private async void ItemsGridView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (SingleClickOpenItem) return;

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

        #endregion

        #region Обработка выделения

        private void ItemsGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine($"[{PanelId}] Selection changed: {ItemsGridView.SelectedItems.Count} items selected");

            if (ItemsGridView.SelectedItem != null)
            {
                _lastSelectedIndex = Items.IndexOf(ItemsGridView.SelectedItem as ExplorerItemViewModel);
            }

            foreach (ExplorerItemViewModel addedItem in e.AddedItems)
            {
                Debug.WriteLine($"  [+] Added to selection: {addedItem?.Name}");
            }

            foreach (ExplorerItemViewModel removedItem in e.RemovedItems)
            {
                Debug.WriteLine($"  [-] Removed from selection: {removedItem?.Name}");
            }

            UpdateUIForSelection();
        }

        #endregion

        #region Операции с элементами

        private async Task OpenItem(ExplorerItemViewModel item)
        {
            try
            {
                _shiftSelectionStartItem = null;
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
                    Debug.WriteLine($"Opening file: {item.FilePath}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening item {item?.Name}: {ex.Message}");
            }
        }

        private async void OpenSelectedItem()
        {
            if (ItemsGridView.SelectedItem is ExplorerItemViewModel selectedItem)
            {
                await OpenItem(selectedItem);
            }
        }

        private void RenameSelectedItem()
        {
            if (ItemsGridView.SelectedItem is ExplorerItemViewModel selectedItem)
            {
                Debug.WriteLine($"Rename item: {selectedItem.Name}");
            }
        }

        private async void DeleteSelectedItems()
        {
            var selectedItems = ItemsGridView.SelectedItems.Cast<ExplorerItemViewModel>().ToList();
            if (selectedItems.Count > 0)
            {
                Debug.WriteLine($"Delete {selectedItems.Count} items");
            }
        }

        #endregion

        #region Обновление и Refresh

        public void RefreshNavigation()
        {
            Debug.WriteLine($"[TileViewIcons {PanelId}] Refreshing navigation via mediator");

            _ = this.DispatcherQueue.EnqueueAsync(() =>
            {
                ItemsGridView.SelectedItem = null;
                ItemsGridView.SelectedItems.Clear();
                _lastClickedItem = null;
                _lastClickTime = DateTime.MinValue;
                _shiftSelectionStartItem = null;
                _lastSelectedIndex = -1;
                _isCtrlPressed = false;
                _isShiftPressed = false;
                _isAltPressed = false;
                _hoveredItem = null;
                _isDragSelecting = false;
                StopHoverSelectionTimer();
                RemoveSelectionRectangle();

                Task task = RefreshContent();
            }, Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal);
        }

        public async Task RefreshContent()
        {
            try
            {
                Debug.WriteLine($"[TileViewIcons {PanelId}] Starting refresh");

                string pathToReload = await DispatcherQueue.EnqueueAsync(() =>
                {
                    string path = _currentLoadedPath;
                    if (string.IsNullOrEmpty(path) && PanelManager != null)
                        path = PanelManager.CurrentPath;

                    _currentLoadedPath = null;
                    _isInitialized = false;
                    Items.Clear();
                    ItemsGridView.SelectedItems.Clear();
                    _shiftSelectionStartItem = null;
                    _lastSelectedIndex = -1;
                    _isCtrlPressed = false;
                    _isShiftPressed = false;
                    _isAltPressed = false;
                    _hoveredItem = null;
                    _isDragSelecting = false;
                    StopHoverSelectionTimer();
                    RemoveSelectionRectangle();
                    UpdateGridViewLayout();

                    return path;
                });

                Debug.WriteLine($"[TileViewIcons {PanelId}] Reloading path: '{pathToReload}'");

                _fileSystemService.ClearPanelCache(PanelId);
                CancelCurrentOperation();

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

        #endregion
    }
}