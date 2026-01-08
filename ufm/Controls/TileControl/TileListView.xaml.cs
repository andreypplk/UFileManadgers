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
    public sealed partial class TileListView : UserControl, IDisposable, ISupportsIconSize, IRefreshablePanel
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

        private bool _isCtrlPressed = false;
        private bool _isShiftPressed = false;
        private bool _isAltPressed = false;

        private bool _isDragSelecting = false;
        private bool _isLeftMouseButtonPressed = false;
        private bool _isMouseMovingWithButton = false;
        private Vector2 _dragStartPoint;
        private Rectangle _selectionRectangle;
        private Canvas _selectionCanvas;

        private DispatcherTimer _hoverTimer;
        private ExplorerItemViewModel _hoveredItem = null;
        private const int HOVER_DELAY_MS = 50;

        private bool _wasClickHandled = false;

        // Временный набор индексов для выделения областью
        private HashSet<int> _tempSelectedIndices = new HashSet<int>();

        // Семафор для защиты от параллельных навигационных операций
        private readonly SemaphoreSlim _navigationSemaphore = new SemaphoreSlim(1, 1);
        private bool _isProcessingBackNavigation = false;

        public double ItemWidth
        {
            get => _itemWidth;
            private set
            {
                if (_itemWidth != value)
                {
                    _itemWidth = value;
                    UpdateListViewLayout();
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
                    UpdateListViewLayout();
                }
            }
        }

        private string _selectedSize = "List Medium";

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

        public TileListView()
        {
            InitializeComponent();

            NavigationSettingsMediator.RegisterPanel(this);

            _dummyHistory = new DirectoryHistory("MyComputer", "Мой Компьютер");

            _fileSystemService = new FileSystemService();

            ItemsListView.ItemsSource = Items;
            Loaded += OnLoaded;

            // Инициализируем таймер выделения при наведении
            InitializeHoverTimer();

            ItemsListView.ContainerContentChanging += ItemsListView_ContainerContentChanging;
            ItemsListView.DoubleTapped += ItemsListView_DoubleTapped;
            ItemsListView.SelectionChanged += ItemsListView_SelectionChanged;
            ItemsListView.KeyDown += ItemsListView_KeyDown;
            ItemsListView.KeyUp += ItemsListView_KeyUp;
            ItemsListView.PreviewKeyDown += ItemsListView_PreviewKeyDown;

            ItemsListView.PointerEntered += ItemsListView_PointerEntered;
            ItemsListView.PointerExited += ItemsListView_PointerExited;
            ItemsListView.PointerMoved += ItemsListView_PointerMoved;

            ItemsListView.PointerPressed += ItemsListView_PointerPressed;
            ItemsListView.PointerReleased += ItemsListView_PointerReleased;
            ItemsListView.PointerMoved += ItemsListView_PointerMovedForDrag;

            // Добавляем обработчик кликов на ItemsListView
            ItemsListView.ItemClick += ItemsListView_OnItemClick;

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
            ItemsListView.ContainerContentChanging -= ItemsListView_ContainerContentChanging;
            ItemsListView.DoubleTapped -= ItemsListView_DoubleTapped;
            ItemsListView.SelectionChanged -= ItemsListView_SelectionChanged;
            ItemsListView.KeyDown -= ItemsListView_KeyDown;
            ItemsListView.KeyUp -= ItemsListView_KeyUp;
            ItemsListView.PreviewKeyDown -= ItemsListView_PreviewKeyDown;
            ItemsListView.PointerEntered -= ItemsListView_PointerEntered;
            ItemsListView.PointerExited -= ItemsListView_PointerExited;
            ItemsListView.PointerMoved -= ItemsListView_PointerMoved;
            ItemsListView.PointerPressed -= ItemsListView_PointerPressed;
            ItemsListView.PointerReleased -= ItemsListView_PointerReleased;
            ItemsListView.PointerMoved -= ItemsListView_PointerMovedForDrag;
            ItemsListView.ItemClick -= ItemsListView_OnItemClick;

            // Останавливаем и освобождаем таймер
            if (_hoverTimer != null)
            {
                _hoverTimer.Stop();
                _hoverTimer.Tick -= HoverTimer_Tick;
                _hoverTimer = null;
            }

            _fileSystemService.ClearPanelCache(PanelId);

            _fileSystemService?.Dispose();

            if (PanelManager != null)
            {
                PanelManager.NavigationChanged -= OnPanelNavigationChanged;
            }

            if (_selectionCanvas != null)
            {
                var grid = ItemsListView.Parent as Grid;
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

            _tempSelectedIndices.Clear();
            _navigationSemaphore?.Dispose();
        }

        #endregion

        #region Инициализация таймера выделения при наведении

        private void InitializeHoverTimer()
        {
            _hoverTimer = new DispatcherTimer();
            _hoverTimer.Interval = TimeSpan.FromMilliseconds(HOVER_DELAY_MS);
            _hoverTimer.Tick += HoverTimer_Tick;
        }

        private void HoverTimer_Tick(object sender, object e)
        {
            _hoverTimer.Stop();

            if (_hoveredItem != null && SingleClickOpenItem && !_isCtrlPressed && !_isShiftPressed && !_isDragSelecting)
            {
                // Выделяем элемент при наведении
                SelectItemOnHover(_hoveredItem);
            }
        }

        private void StartHoverTimer()
        {
            if (_hoverTimer != null && !_hoverTimer.IsEnabled)
            {
                _hoverTimer.Start();
            }
        }

        private void StopHoverTimer()
        {
            if (_hoverTimer != null && _hoverTimer.IsEnabled)
            {
                _hoverTimer.Stop();
            }
        }

        private void RestartHoverTimer()
        {
            StopHoverTimer();
            StartHoverTimer();
        }

        #endregion

        #region Инициализация Canvas для выделения

        private void InitializeSelectionCanvas()
        {
            if (_selectionCanvas != null) return;

            _selectionCanvas = new Canvas
            {
                IsHitTestVisible = false,
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent)
            };

            var grid = ItemsListView.Parent as Grid;
            if (grid != null)
            {
                grid.Children.Add(_selectionCanvas);
                Canvas.SetZIndex(_selectionCanvas, 1000);
            }
        }

        #endregion

        #region Обработка событий мыши для выделения при наведении

        private void ItemsListView_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (!SingleClickOpenItem) return;

            var element = e.OriginalSource as FrameworkElement;
            var item = FindParentDataContext<ExplorerItemViewModel>(element);

            if (item != null && item != _hoveredItem)
            {
                _hoveredItem = item;
                StartHoverTimer();
            }
        }

        private void ItemsListView_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (!SingleClickOpenItem) return;

            StopHoverTimer();
            _hoveredItem = null;
        }

        private void ItemsListView_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!SingleClickOpenItem) return;

            var element = e.OriginalSource as FrameworkElement;
            var item = FindParentDataContext<ExplorerItemViewModel>(element);

            if (item != null && item != _hoveredItem)
            {
                _hoveredItem = item;
                RestartHoverTimer();
            }
            else if (item == null)
            {
                StopHoverTimer();
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

        #endregion

        #region Выделение областью мышью - С НЕМЕДЛЕННЫМ ВЫДЕЛЕНИЕМ

        private void ItemsListView_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(ItemsListView);

            // Сохраняем точку начала выделения
            _dragStartPoint = new Vector2((float)point.Position.X, (float)point.Position.Y);
            _wasClickHandled = false;

            // Если нажата левая кнопка мыши
            if (point.Properties.IsLeftButtonPressed)
            {
                _isLeftMouseButtonPressed = true;
                _isMouseMovingWithButton = false;

                // Ищем элемент под курсором
                var clickedItem = FindItemAtPoint(point.Position);
                if (clickedItem != null)
                {
                    // Если кликнули на элементе - обрабатываем как обычный клик
                    // Не обрабатываем событие, чтобы позволить стандартной обработке клика через ItemsListView_OnItemClick
                    e.Handled = false;
                }
                else
                {
                    // Если кликнули на пустом месте - начинаем выделение областью
                    _isDragSelecting = true;
                    ItemsListView.CapturePointer(e.Pointer);
                    CreateSelectionRectangle();
                    UpdateSelectionRectangle(_dragStartPoint, _dragStartPoint);

                    // Очищаем выделение если не нажат Ctrl
                    if (!_isCtrlPressed)
                    {
                        ItemsListView.SelectedItems.Clear();
                    }

                    e.Handled = true;
                }
            }
        }

        private ListViewItem FindItemAtPoint(Windows.Foundation.Point point)
        {
            var elements = VisualTreeHelper.FindElementsInHostCoordinates(point, ItemsListView);
            return elements.OfType<ListViewItem>().FirstOrDefault();
        }

        private void ItemsListView_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(ItemsListView);

            // Сбрасываем состояние при отпускании левой кнопки
            if (!point.Properties.IsLeftButtonPressed)
            {
                _isLeftMouseButtonPressed = false;
                _isMouseMovingWithButton = false;

                // В режиме одинарного клика сразу сбрасываем флаг
                // В режиме двойного клика - сбрасываем только если нет двойного клика
                if (SingleClickOpenItem)
                {
                    _wasClickHandled = false;
                }

                // Освобождаем указатель только если он был захвачен
                if (ItemsListView.PointerCaptures != null && ItemsListView.PointerCaptures.Count > 0)
                {
                    ItemsListView.ReleasePointerCapture(e.Pointer);
                }
            }

            if (_isDragSelecting)
            {
                _isDragSelecting = false;

                // Всегда сбрасываем при выделении областью
                _wasClickHandled = false;

                // Освобождаем указатель если он был захвачен
                if (ItemsListView.PointerCaptures != null && ItemsListView.PointerCaptures.Count > 0)
                {
                    ItemsListView.ReleasePointerCapture(e.Pointer);
                }

                // Удаляем прямоугольник выделения
                RemoveSelectionRectangle();

                e.Handled = true;
            }
        }

        private void ItemsListView_PointerMovedForDrag(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(ItemsListView);

            // ВАЖНО: проверяем зажата ли левая кнопка И движется ли мышь
            if (_isLeftMouseButtonPressed && point.Properties.IsLeftButtonPressed)
            {
                var currentPosition = point.Position;

                // Определяем, двинулась ли мышь достаточно далеко от начальной точки
                float distance = Vector2.Distance(_dragStartPoint,
                    new Vector2((float)currentPosition.X, (float)currentPosition.Y));

                // Если мышь сдвинулась больше чем на 3 пикселя - это "движение с зажатой кнопкой"
                if (distance > 3.0f && !_isMouseMovingWithButton)
                {
                    _isMouseMovingWithButton = true;
                }

                // Если движемся дальше определенного порога - начинаем выделение областью
                if (distance > 10.0f && !_isDragSelecting)
                {
                    _isDragSelecting = true;
                    // Захватываем указатель ТОЛЬКО при начале выделения областью
                    ItemsListView.CapturePointer(e.Pointer);
                    // Создаем прямоугольник выделения
                    CreateSelectionRectangle();
                    UpdateSelectionRectangle(_dragStartPoint,
                        new Vector2((float)currentPosition.X, (float)currentPosition.Y));

                    // Очищаем выделение если не нажат Ctrl
                    if (!_isCtrlPressed)
                    {
                        ItemsListView.SelectedItems.Clear();
                    }
                }

                if (_isDragSelecting)
                {
                    var currentPoint = new Vector2((float)point.Position.X, (float)point.Position.Y);

                    // Обновляем прямоугольник выделения
                    UpdateSelectionRectangle(_dragStartPoint, currentPoint);

                    // НЕМЕДЛЕННО выделяем элементы внутри прямоугольника
                    PerformRectangleSelection(_dragStartPoint, currentPoint, true);

                    e.Handled = true;
                }
            }
        }

        // Методы для правильной рамки
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

        // ОПТИМИЗИРОВАННОЕ выделение прямоугольником с НЕМЕДЛЕННЫМ применением
        private void PerformRectangleSelection(Vector2 startPoint, Vector2 endPoint, bool applyImmediately = false)
        {
            if (Items.Count == 0) return;

            // Определяем границы прямоугольника
            float left = Math.Min(startPoint.X, endPoint.X);
            float right = Math.Max(startPoint.X, endPoint.X);
            float top = Math.Min(startPoint.Y, endPoint.Y);
            float bottom = Math.Max(startPoint.Y, endPoint.Y);

            var newSelectedIndices = new HashSet<int>();

            // Используем ItemsWrapGrid.Children для быстрого доступа к видимым элементам
            var panel = ItemsListView.ItemsPanelRoot as ItemsWrapGrid;
            if (panel != null)
            {
                // Проходим по всем видимым элементам
                foreach (var child in panel.Children)
                {
                    if (child is ListViewItem container && container.Visibility == Visibility.Visible)
                    {
                        int index = ItemsListView.IndexFromContainer(container);
                        if (index >= 0 && index < Items.Count)
                        {
                            // Получаем позицию элемента
                            var transform = container.TransformToVisual(ItemsListView);
                            var position = transform.TransformPoint(new Windows.Foundation.Point(0, 0));

                            float itemLeft = (float)position.X;
                            float itemTop = (float)position.Y;
                            float itemRight = itemLeft + (float)container.ActualWidth;
                            float itemBottom = itemTop + (float)container.ActualHeight;

                            // Проверяем пересечение с прямоугольником выделения
                            bool intersects = itemRight > left && itemLeft < right &&
                                              itemBottom > top && itemTop < bottom;

                            if (intersects)
                            {
                                newSelectedIndices.Add(index);
                            }
                            else if (!_isCtrlPressed && applyImmediately)
                            {
                                // Если элемент не входит в прямоугольник и не нажат Ctrl - снимаем выделение
                                var item = Items[index];
                                if (ItemsListView.SelectedItems.Contains(item))
                                {
                                    ItemsListView.SelectedItems.Remove(item);
                                }
                            }
                        }
                    }
                }
            }

            // НЕМЕДЛЕННО применяем выделение
            if (applyImmediately)
            {
                // Добавляем элементы из временного набора
                foreach (int index in newSelectedIndices)
                {
                    if (index >= 0 && index < Items.Count)
                    {
                        var item = Items[index];
                        if (!ItemsListView.SelectedItems.Contains(item))
                        {
                            ItemsListView.SelectedItems.Add(item);
                        }
                    }
                }

                // Обновляем визуальное состояние
                UpdateSelectionVisual();
            }
            else
            {
                // Сохраняем для применения позже
                _tempSelectedIndices = newSelectedIndices;
            }
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

        private async void OnPanelNavigationChanged(object sender, EventArgs e)
        {
            if (_isLoading) return;

            if (PanelManager != null && PanelManager.CurrentPath != _currentLoadedPath)
            {
                // Добавляем небольшую задержку, чтобы дать завершиться текущей навигации
                await Task.Delay(100);

                if (PanelManager.CurrentPath != _currentLoadedPath)
                {
                    await LoadPathContents(PanelManager.CurrentPath);
                }
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
                _selectedSize = "List Medium";
                Debug.WriteLine("Используются настройки по умолчанию.");
            }
            else
            {
                Debug.WriteLine($"Загружены настройки: Размер = {_selectedSize}");
            }

            CalculateItemDimensions();
            UpdateAllTiles();
            UpdateListViewLayout();

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
            UpdateListViewLayout();
            ItemsListView.UpdateLayout();
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
            foreach (var item in ItemsListView.Items)
            {
                var container = ItemsListView.ContainerFromItem(item) as ListViewItem;
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

        private void ItemsListView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.Phase != 0) return;

            if (args.ItemContainer is ListViewItem container)
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
            foreach (var item in ItemsListView.SelectedItems)
            {
                var container = ItemsListView.ContainerFromItem(item) as ListViewItem;
                if (container != null)
                {
                    VisualStateManager.GoToState(container, "Selected", false);
                }
            }
        }

        private void SelectItemOnHover(ExplorerItemViewModel item)
        {
            if (item == null || _isDragSelecting) return;

            // НЕ выделяем элемент ".." при наведении!
            if (item.Name == "..") return;

            if (ItemsListView.SelectedItems.Contains(item))
                return;

            if (!_isCtrlPressed && !_isShiftPressed)
            {
                ItemsListView.SelectedItems.Clear();
            }

            if (!ItemsListView.SelectedItems.Contains(item))
            {
                ItemsListView.SelectedItems.Add(item);
            }

            ItemsListView.SelectedItem = item;

            Debug.WriteLine($"Hover selection: {item.Name}");
        }

        #endregion

        #region Layout и обновление UI

        private void UpdateListViewLayout()
        {
            if (ItemsListView?.ItemsPanelRoot is ItemsWrapGrid wrapGrid)
            {
                wrapGrid.ItemWidth = ItemWidth;
                wrapGrid.ItemHeight = ItemHeight;
                Debug.WriteLine($"ListView layout updated: ItemWidth={wrapGrid.ItemWidth}, ItemHeight={wrapGrid.ItemHeight}");
                UpdateSelectionVisual();
            }
        }

        private void UpdateUIForSelection()
        {
            int selectedCount = ItemsListView.SelectedItems.Count;
            Debug.WriteLine($"Update UI: {selectedCount} items selected");
        }

        #endregion

        #region Обработка кликов

        private async void ItemsListView_OnItemClick(object sender, ItemClickEventArgs e)
        {
            // Защита от быстрых повторных кликов
            var now = DateTime.Now;
            if ((now - _lastClickTime).TotalMilliseconds < 300)
            {
                Debug.WriteLine($"[{PanelId}] Click throttled - too fast");
                return;
            }
            _lastClickTime = now;

            Debug.WriteLine($"[{PanelId}] === ItemsListView_OnItemClick START ===");
            Debug.WriteLine($"[{PanelId}] SingleClickOpenItem: {SingleClickOpenItem}");

            if (e.ClickedItem is not ExplorerItemViewModel item) return;

            Debug.WriteLine($"[{PanelId}] Clicked item: {item.Name}");

            int clickedIndex = Items.IndexOf(item);
            if (clickedIndex < 0) return;

            bool isSingleClickMode = SingleClickOpenItem;
            bool isCtrlPressed = _isCtrlPressed;
            bool isShiftPressed = _isShiftPressed;

            // ТОЛЬКО в режиме двойного клика используем флаг _wasClickHandled
            if (!isSingleClickMode)
            {
                if (_wasClickHandled)
                {
                    Debug.WriteLine($"[{PanelId}] Click already handled, returning");
                    Debug.WriteLine($"[{PanelId}] === ItemsListView_OnItemClick END (skipped) ===");
                    return;
                }
                _wasClickHandled = true;
                Debug.WriteLine($"[{PanelId}] Set _wasClickHandled = true");
            }

            if (isSingleClickMode)
            {
                // Режим ОДИНОЧНОГО клика - открываем сразу
                if (isShiftPressed)
                {
                    // Shift+клик - выделение диапазона
                    if (_shiftSelectionStartItem == null)
                    {
                        _shiftSelectionStartItem = ItemsListView.SelectedItem as ExplorerItemViewModel;
                        if (_shiftSelectionStartItem == null && Items.Count > 0)
                        {
                            _shiftSelectionStartItem = Items[0];
                        }
                    }

                    if (_shiftSelectionStartItem != null)
                    {
                        int startIndex = Items.IndexOf(_shiftSelectionStartItem);
                        int endIndex = clickedIndex;

                        SelectRange(startIndex, endIndex);
                    }
                }
                else if (isCtrlPressed)
                {
                    // Ctrl+клик - множественное выделение
                    if (ItemsListView.SelectedItems.Contains(item))
                    {
                        ItemsListView.SelectedItems.Remove(item);
                    }
                    else
                    {
                        ItemsListView.SelectedItems.Add(item);
                    }
                    _shiftSelectionStartItem = item;
                }
                else
                {
                    // Обычный клик - открываем элемент
                    ItemsListView.SelectedItems.Clear();
                    ItemsListView.SelectedItem = item;
                    _shiftSelectionStartItem = item;

                    // Используем оптимизированный метод открытия
                    await OpenItemByIndex(clickedIndex);
                }
            }
            else
            {
                // Режим ДВОЙНОГО клика
                if (isShiftPressed)
                {
                    // Shift+клик - выделение диапазона
                    if (_shiftSelectionStartItem == null)
                    {
                        _shiftSelectionStartItem = ItemsListView.SelectedItem as ExplorerItemViewModel;
                        if (_shiftSelectionStartItem == null && Items.Count > 0)
                        {
                            _shiftSelectionStartItem = Items[0];
                        }
                    }

                    if (_shiftSelectionStartItem != null)
                    {
                        int startIndex = Items.IndexOf(_shiftSelectionStartItem);
                        int endIndex = clickedIndex;

                        SelectRange(startIndex, endIndex);
                    }
                }
                else if (isCtrlPressed)
                {
                    // Ctrl+клик - множественное выделение
                    if (ItemsListView.SelectedItems.Contains(item))
                    {
                        ItemsListView.SelectedItems.Remove(item);
                    }
                    else
                    {
                        ItemsListView.SelectedItems.Add(item);
                    }
                    _shiftSelectionStartItem = item;
                }
                else
                {
                    // Обычный клик - выделяем и ждем возможного двойного клика
                    ItemsListView.SelectedItems.Clear();
                    ItemsListView.SelectedItem = item;
                    _shiftSelectionStartItem = item;

                    var currentTime = DateTime.Now;
                    bool isDoubleClick = (_lastClickedItem == item &&
                                         (currentTime - _lastClickTime).TotalMilliseconds < 500);

                    if (isDoubleClick)
                    {
                        // Двойной клик - открываем элемент
                        await OpenItemByIndex(clickedIndex);
                        _lastClickedItem = null;
                        _lastClickTime = DateTime.MinValue;
                    }
                    else
                    {
                        // Первый клик - сохраняем для возможного двойного
                        _lastClickedItem = item;
                        _lastClickTime = currentTime;

                        // Сбрасываем флаг через короткое время, чтобы не блокировать следующий клик
                        _ = this.DispatcherQueue.EnqueueAsync(async () =>
                        {
                            await Task.Delay(500);
                            _wasClickHandled = false;
                        }, Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
                    }
                }
            }

            Debug.WriteLine($"[{PanelId}] === ItemsListView_OnItemClick END ===");
        }

        #endregion

        #region Загрузка содержимого

        private async void LoadInitialContent()
        {
            Debug.WriteLine($"[{PanelId}] === LoadInitialContent START ===");

            if (_currentLoadedPath == "MyComputer" && Items.Count > 0)
            {
                Debug.WriteLine($"[{PanelId}] MyComputer content already loaded, skipping");
                Debug.WriteLine($"[{PanelId}] === LoadInitialContent END (skipped) ===");
                return;
            }

            CancelCurrentOperation();
            Items.Clear();
            UpdateListViewLayout();

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

            Debug.WriteLine($"[{PanelId}] === LoadInitialContent END ===");
        }

        internal async Task LoadPathContents(string path)
        {
            // Блокируем параллельные вызовы навигации
            await _navigationSemaphore.WaitAsync();
            try
            {
                Debug.WriteLine($"[{PanelId}] === LoadPathContents START ===");
                Debug.WriteLine($"[{PanelId}] Path: '{path}', Current: '{_currentLoadedPath}', IsLoading: {_isLoading}");

                if (_isLoading || _currentLoadedPath == path)
                {
                    Debug.WriteLine($"[{PanelId}] Skipping - already loading or same path");
                    Debug.WriteLine($"[{PanelId}] === LoadPathContents END (skipped) ===");
                    return;
                }

                try
                {
                    _isLoading = true;
                    switch (path)
                    {
                        case "MyComputer":
                            Debug.WriteLine($"[{PanelId}] Case: MyComputer");
                            LoadInitialContent();
                            _currentLoadedPath = path;
                            break;

                        case "Drives":
                            Debug.WriteLine($"[{PanelId}] Case: Drives");
                            await LoadDrives();
                            _currentLoadedPath = path;
                            break;

                        case string p when Directory.Exists(p):
                            Debug.WriteLine($"[{PanelId}] Case: Directory exists");
                            await LoadFolderContents(path);
                            _currentLoadedPath = path;

                            if (PanelManager != null && PanelManager.CurrentPath != path)
                            {
                                PanelManager.NavigateTo(path);
                            }
                            break;

                        default:
                            Debug.WriteLine($"[{PanelId}] Case: Default");
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
                Debug.WriteLine($"[{PanelId}] === LoadPathContents END ===");
            }
            finally
            {
                _navigationSemaphore.Release();
            }
        }

        private async Task LoadDrives()
        {
            Debug.WriteLine($"[{PanelId}] === LoadDrives START ===");
            Debug.WriteLine($"[{PanelId}] _currentLoadedPath: '{_currentLoadedPath}', Items.Count: {Items.Count}");

            if (_currentLoadedPath == "Drives" && Items.Count > 1)
            {
                Debug.WriteLine($"[{PanelId}] Drives already loaded, skipping");
                Debug.WriteLine($"[{PanelId}] === LoadDrives END (skipped) ===");
                return;
            }

            CancelCurrentOperation();
            Items.Clear();
            UpdateListViewLayout();

            try
            {
                Debug.WriteLine($"[{PanelId}] Loading drives...");
                var driveItems = await _fileSystemService.LoadDrivesAsync(_dummyHistory);

                await this.DispatcherQueue.EnqueueAsync(() =>
                {
                    Items.Clear();
                    foreach (var item in driveItems)
                    {
                        Items.Add(item);
                    }
                    UpdateListViewLayout();
                    Debug.WriteLine($"[{PanelId}] Drives loaded successfully, items count: {Items.Count}");
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{PanelId}] ERROR in LoadDrives: {ex.Message}");
                Debug.WriteLine($"[{PanelId}] Type: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"[{PanelId}] Inner: {ex.InnerException.Message}");
                }
                Debug.WriteLine($"[{PanelId}] StackTrace: {ex.StackTrace}");

                // НЕ вызываем LoadInitialContent() при ошибке!
                // Просто показываем пустой список
                await this.DispatcherQueue.EnqueueAsync(() =>
                {
                    Items.Clear();
                    UpdateListViewLayout();
                    Debug.WriteLine($"[{PanelId}] Cleared items due to error");
                });
            }

            OnNavigationChanged();
            Debug.WriteLine($"[{PanelId}] === LoadDrives END ===");
        }

        private async Task LoadFolderContents(string folderPath)
        {
            Debug.WriteLine($"[{PanelId}] === LoadFolderContents START ===");
            Debug.WriteLine($"[{PanelId}] FolderPath: '{folderPath}'");

            if (string.IsNullOrEmpty(folderPath))
            {
                Debug.WriteLine($"[{PanelId}] Folder path is empty, returning");
                Debug.WriteLine($"[{PanelId}] === LoadFolderContents END (empty) ===");
                return;
            }

            if (!Directory.Exists(folderPath))
            {
                Debug.WriteLine($"[{PanelId}] Directory does not exist: {folderPath}");
                PanelManager?.GoBack();
                Debug.WriteLine($"[{PanelId}] === LoadFolderContents END (not exists) ===");
                return;
            }

            if (_currentLoadedPath == folderPath && Items.Count > 0)
            {
                Debug.WriteLine($"[{PanelId}] Folder {folderPath} already loaded, skipping");
                Debug.WriteLine($"[{PanelId}] === LoadFolderContents END (skipped) ===");
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
                    UpdateListViewLayout();
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

            Debug.WriteLine($"[{PanelId}] === LoadFolderContents END ===");
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

        private void ItemsListView_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            UpdateModifierKeyState(e.Key, true);

            bool isCtrlPressed = _isCtrlPressed;
            bool isShiftPressed = _isShiftPressed;

            Debug.WriteLine($"KeyDown: {e.Key}, Ctrl: {isCtrlPressed}, Shift: {isShiftPressed}");

            switch (e.Key)
            {
                case VirtualKey.A when isCtrlPressed && !isShiftPressed:
                    ItemsListView.SelectAll();
                    e.Handled = true;
                    Debug.WriteLine("Ctrl+A: Select all");
                    break;

                case VirtualKey.Space when isCtrlPressed:
                    ToggleCurrentSelection();
                    e.Handled = true;
                    Debug.WriteLine("Ctrl+Space: Toggle selection");
                    break;

                case VirtualKey.Enter:
                    if (ItemsListView.SelectedItem != null)
                    {
                        OpenSelectedItem();
                        e.Handled = true;
                        Debug.WriteLine("Enter: Open selected item");
                    }
                    break;

                case VirtualKey.F2:
                    if (ItemsListView.SelectedItems.Count == 1)
                    {
                        RenameSelectedItem();
                        e.Handled = true;
                        Debug.WriteLine("F2: Rename selected item");
                    }
                    break;

                case VirtualKey.Delete:
                    if (ItemsListView.SelectedItems.Count > 0)
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

        private void ItemsListView_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            UpdateModifierKeyState(e.Key, false);

            if (e.Key == VirtualKey.Shift)
            {
                _shiftSelectionStartItem = null;
                Debug.WriteLine("Shift released, reset selection start");
            }
        }

        private void ItemsListView_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
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
            int currentIndex = ItemsListView.SelectedIndex;
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
                    ItemsListView.SelectedItem = newItem;
                    ItemsListView.ScrollIntoView(newItem);
                }
                else
                {
                    ItemsListView.SelectedItems.Clear();
                    ItemsListView.SelectedItem = newItem;
                    ItemsListView.ScrollIntoView(newItem);
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
                    ItemsListView.SelectedItem = newItem;
                    ItemsListView.ScrollIntoView(newItem);
                }
                else
                {
                    ItemsListView.SelectedItems.Clear();
                    ItemsListView.SelectedItem = newItem;
                    ItemsListView.ScrollIntoView(newItem);
                }

                Debug.WriteLine($"{key} navigation to index {newIndex}");
            }
        }

        private void HandlePageNavigation(VirtualKey key, bool isCtrlPressed, bool isShiftPressed)
        {
            int currentIndex = ItemsListView.SelectedIndex;
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
                    ItemsListView.SelectedItems.Clear();
                    ItemsListView.SelectedItem = newItem;
                    ItemsListView.ScrollIntoView(newItem);
                }

                Debug.WriteLine($"{key}: from {currentIndex} to {newIndex}");
            }
        }

        private int CalculateItemsPerRow()
        {
            if (ItemWidth <= 0) return 1;

            var grid = ItemsListView.ItemsPanelRoot as ItemsWrapGrid;
            if (grid != null && grid.ActualWidth > 0)
            {
                return (int)Math.Floor(grid.ActualWidth / ItemWidth);
            }

            return 6;
        }

        private int CalculateItemsPerPage()
        {
            if (ItemHeight <= 0) return 1;

            var grid = ItemsListView.ItemsPanelRoot as FrameworkElement;
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
                _shiftSelectionStartItem = ItemsListView.SelectedItem as ExplorerItemViewModel;
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
            int currentIndex = ItemsListView.SelectedIndex;
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
                ItemsListView.SelectedItems.Clear();
            }

            for (int i = minIndex; i <= maxIndex; i++)
            {
                if (!ItemsListView.SelectedItems.Contains(Items[i]))
                {
                    ItemsListView.SelectedItems.Add(Items[i]);
                }
            }

            ItemsListView.SelectedItem = Items[endIndex];
            ItemsListView.ScrollIntoView(Items[endIndex]);
        }

        private void ToggleCurrentSelection()
        {
            if (ItemsListView.SelectedItem is ExplorerItemViewModel currentItem)
            {
                if (ItemsListView.SelectedItems.Contains(currentItem))
                {
                    ItemsListView.SelectedItems.Remove(currentItem);
                    Debug.WriteLine($"Removed {currentItem.Name} from selection");
                }
                else
                {
                    ItemsListView.SelectedItems.Add(currentItem);
                    Debug.WriteLine($"Added {currentItem.Name} to selection");
                }
            }
        }

        #endregion

        #region Обработка двойного клика

        private async void ItemsListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (SingleClickOpenItem) return;

            var element = e.OriginalSource as FrameworkElement;
            while (element != null && element.DataContext as ExplorerItemViewModel == null)
            {
                element = VisualTreeHelper.GetParent(element) as FrameworkElement;
            }

            if (element?.DataContext is ExplorerItemViewModel item)
            {
                int index = Items.IndexOf(item);
                if (index >= 0)
                {
                    await OpenItemByIndex(index);
                }
            }
        }

        #endregion

        #region Обработка выделения

        private void ItemsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine($"[{PanelId}] Selection changed: {ItemsListView.SelectedItems.Count} items selected");

            foreach (ExplorerItemViewModel addedItem in e.AddedItems)
            {
                Debug.WriteLine($"  [+] Added to selection: {addedItem?.Name}");
            }

            foreach (ExplorerItemViewModel removedItem in e.RemovedItems)
            {
                Debug.WriteLine($"  [-] Removed from selection: {removedItem?.Name}");
                if (removedItem?.Name == "..")
                {
                    // При удалении элемента ".." из выделения сбрасываем hover-таймер
                    StopHoverTimer();
                    _hoveredItem = null;
                }
            }

            UpdateUIForSelection();
        }

        #endregion

        #region Операции с элементами

        private async Task OpenItem(ExplorerItemViewModel item)
        {
            try
            {
                // Сбрасываем состояние
                _shiftSelectionStartItem = null;
                _lastClickedItem = null;
                _lastClickTime = DateTime.MinValue;

                // Всегда сбрасываем флаг при открытии элемента
                _wasClickHandled = false;

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

        // Оптимизированный метод открытия по индексу
        private async Task OpenItemByIndex(int index)
        {
            Debug.WriteLine($"[{PanelId}] === OpenItemByIndex START ===");
            Debug.WriteLine($"[{PanelId}] Index: {index}, SingleClickOpenItem: {SingleClickOpenItem}");

            // Защита от параллельных обработок навигации назад
            if (_isProcessingBackNavigation)
            {
                Debug.WriteLine($"[{PanelId}] Already processing back navigation, skipping");
                Debug.WriteLine($"[{PanelId}] === OpenItemByIndex END (skipped) ===");
                return;
            }

            if (index < 0 || index >= Items.Count)
            {
                Debug.WriteLine($"[{PanelId}] Invalid index, returning");
                Debug.WriteLine($"[{PanelId}] === OpenItemByIndex END (invalid index) ===");
                return;
            }

            var item = Items[index];
            Debug.WriteLine($"[{PanelId}] Item: {item.Name}, Path: {item.FilePath}");

            if (item.Name == "..")
            {
                _isProcessingBackNavigation = true;
                try
                {
                    Debug.WriteLine($"[{PanelId}] Processing '..' navigation");
                    Debug.WriteLine($"[{PanelId}] PanelManager.CurrentPath before: {PanelManager?.CurrentPath}");
                    Debug.WriteLine($"[{PanelId}] _currentLoadedPath before: {_currentLoadedPath}");

                    PanelManager?.GoBack();

                    // Сбрасываем состояние после перехода
                    _shiftSelectionStartItem = null;
                    _lastClickedItem = null;
                    _lastClickTime = DateTime.MinValue;
                    _wasClickHandled = false;
                    StopHoverTimer();
                    _hoveredItem = null;

                    // Короткая задержка для завершения навигации
                    await Task.Delay(50);

                    Debug.WriteLine($"[{PanelId}] PanelManager.CurrentPath after: {PanelManager?.CurrentPath}");
                }
                finally
                {
                    _isProcessingBackNavigation = false;
                }
                Debug.WriteLine($"[{PanelId}] === OpenItemByIndex END (back navigation) ===");
                return;
            }

            // Сбрасываем состояние
            _shiftSelectionStartItem = null;
            _lastClickedItem = null;
            _lastClickTime = DateTime.MinValue;
            _wasClickHandled = false;

            string path = item.FilePath;

            if (path == "Drives" || path == "MyComputer" || Directory.Exists(path))
            {
                await LoadPathContents(path);
                PanelManager?.NavigateTo(path);
            }
            else if (File.Exists(path))
            {
                Debug.WriteLine($"Opening file: {path}");
            }

            Debug.WriteLine($"[{PanelId}] === OpenItemByIndex END ===");
        }

        private async void OpenSelectedItem()
        {
            if (ItemsListView.SelectedItem is ExplorerItemViewModel selectedItem)
            {
                await OpenItem(selectedItem);
            }
        }

        private void RenameSelectedItem()
        {
            if (ItemsListView.SelectedItem is ExplorerItemViewModel selectedItem)
            {
                Debug.WriteLine($"Rename item: {selectedItem.Name}");
            }
        }

        private async void DeleteSelectedItems()
        {
            var selectedItems = ItemsListView.SelectedItems.Cast<ExplorerItemViewModel>().ToList();
            if (selectedItems.Count > 0)
            {
                Debug.WriteLine($"Delete {selectedItems.Count} items");
            }
        }

        #endregion

        #region Обновление и Refresh

        public void RefreshNavigation()
        {
            Debug.WriteLine($"[TileListView {PanelId}] Refreshing navigation via mediator");

            _ = this.DispatcherQueue.EnqueueAsync(() =>
            {
                ItemsListView.SelectedItem = null;
                ItemsListView.SelectedItems.Clear();
                _lastClickedItem = null;
                _lastClickTime = DateTime.MinValue;
                _shiftSelectionStartItem = null;
                _isCtrlPressed = false;
                _isShiftPressed = false;
                _isAltPressed = false;
                _hoveredItem = null;
                _isDragSelecting = false;
                _isLeftMouseButtonPressed = false;
                _isMouseMovingWithButton = false;
                _isProcessingBackNavigation = false;

                // Останавливаем таймер при обновлении
                StopHoverTimer();

                RemoveSelectionRectangle();
                _tempSelectedIndices.Clear();

                Task task = RefreshContent();
            }, Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal);
        }

        public async Task RefreshContent()
        {
            try
            {
                Debug.WriteLine($"[TileListView {PanelId}] Starting refresh");

                string pathToReload = await DispatcherQueue.EnqueueAsync(() =>
                {
                    string path = _currentLoadedPath;
                    if (string.IsNullOrEmpty(path) && PanelManager != null)
                        path = PanelManager.CurrentPath;

                    _currentLoadedPath = null;
                    _isInitialized = false;
                    Items.Clear();
                    ItemsListView.SelectedItems.Clear();
                    _shiftSelectionStartItem = null;
                    _isCtrlPressed = false;
                    _isShiftPressed = false;
                    _isAltPressed = false;
                    _hoveredItem = null;
                    _isDragSelecting = false;
                    _isLeftMouseButtonPressed = false;
                    _isMouseMovingWithButton = false;
                    _isProcessingBackNavigation = false;

                    // Останавливаем таймер
                    StopHoverTimer();

                    RemoveSelectionRectangle();
                    _tempSelectedIndices.Clear();
                    UpdateListViewLayout();

                    return path;
                });

                Debug.WriteLine($"[TileListView {PanelId}] Reloading path: '{pathToReload}'");

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

                Debug.WriteLine($"[TileListView {PanelId}] Refresh completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileListView {PanelId}] Refresh error: {ex}");

                try
                {
                    await DispatcherQueue.EnqueueAsync(() => LoadInitialContent());
                }
                catch (Exception fallbackEx)
                {
                    Debug.WriteLine($"[TileListView {PanelId}] Fallback failed: {fallbackEx}");
                }
            }
        }

        #endregion
    }
}