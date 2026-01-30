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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Windows.Storage;

namespace ufm
{
    public sealed partial class TileViewerContent : UserControl, IDisposable, ISupportsIconSize, IRefreshablePanel, INotifyPropertyChanged
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

        // Для динамического вычисления MaximumRowsOrColumns
        private int _maxRowsOrColumns = 1;
        public int MaxRowsOrColumns
        {
            get => _maxRowsOrColumns;
            private set
            {
                if (_maxRowsOrColumns != value)
                {
                    _maxRowsOrColumns = value;
                    OnPropertyChanged();
                }
            }
        }

        // Свойство для режима отображения
        private string _displayMode = "Horizontal";
        public string DisplayMode
        {
            get => _displayMode;
            set
            {
                if (_displayMode != value)
                {
                    _displayMode = value;
                    UpdateDisplayMode();
                    OnPropertyChanged();
                }
            }
        }

        // Текущий активный контрол
        private ListViewBase _currentItemsControl;

        public double ItemWidth
        {
            get => _itemWidth;
            private set
            {
                if (_itemWidth != value)
                {
                    _itemWidth = value;
                    UpdateItemsControlLayout();
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
                    UpdateItemsControlLayout();
                }
            }
        }

        private string _selectedSize = "Medium";

        private const int HorizontalPadding = 10;
        private const int VerticalPadding = 8;
        private const int TextBlockHeight = 40;
        private const int MinimumItemWidth = 100;
        private const int MinimumItemHeight = 40;

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

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Конструктор и Dispose

        public TileViewerContent()
        {
            InitializeComponent();

            NavigationSettingsMediator.RegisterPanel(this);

            _dummyHistory = new DirectoryHistory("MyComputer", "Мой Компьютер");

            _fileSystemService = new FileSystemService();

            // Устанавливаем текущий контрол
            _currentItemsControl = ItemsListView;
            ItemsListView.ItemsSource = Items;
            ItemsGridView.ItemsSource = Items;

            Loaded += OnLoaded;

            // Инициализируем таймер выделения при наведении
            InitializeHoverTimer();

            // Подписываемся на события обоих контролов
            SubscribeToEvents(ItemsListView);
            SubscribeToEvents(ItemsGridView);

            // Добавляем обработчик кликов
            ItemsListView.ItemClick += ItemsControl_OnItemClick;
            ItemsGridView.ItemClick += ItemsControl_OnItemClick;

            this.Loaded += (s, e) => InitializeSelectionCanvas();

            CalculateItemDimensions();
        }

        private void SubscribeToEvents(ListViewBase itemsControl)
        {
            itemsControl.ContainerContentChanging += ItemsControl_ContainerContentChanging;
            itemsControl.DoubleTapped += ItemsControl_DoubleTapped;
            itemsControl.SelectionChanged += ItemsControl_SelectionChanged;
            itemsControl.KeyDown += ItemsControl_KeyDown;
            itemsControl.KeyUp += ItemsControl_KeyUp;
            itemsControl.PreviewKeyDown += ItemsControl_PreviewKeyDown;

            itemsControl.PointerEntered += ItemsControl_PointerEntered;
            itemsControl.PointerExited += ItemsControl_PointerExited;
            itemsControl.PointerMoved += ItemsControl_PointerMoved;

            itemsControl.PointerPressed += ItemsControl_PointerPressed;
            itemsControl.PointerReleased += ItemsControl_PointerReleased;
            itemsControl.PointerMoved += ItemsControl_PointerMovedForDrag;
            itemsControl.SizeChanged += ItemsControl_SizeChanged;
        }

        private void UnsubscribeFromEvents(ListViewBase itemsControl)
        {
            itemsControl.ContainerContentChanging -= ItemsControl_ContainerContentChanging;
            itemsControl.DoubleTapped -= ItemsControl_DoubleTapped;
            itemsControl.SelectionChanged -= ItemsControl_SelectionChanged;
            itemsControl.KeyDown -= ItemsControl_KeyDown;
            itemsControl.KeyUp -= ItemsControl_KeyUp;
            itemsControl.PreviewKeyDown -= ItemsControl_PreviewKeyDown;

            itemsControl.PointerEntered -= ItemsControl_PointerEntered;
            itemsControl.PointerExited -= ItemsControl_PointerExited;
            itemsControl.PointerMoved -= ItemsControl_PointerMoved;

            itemsControl.PointerPressed -= ItemsControl_PointerPressed;
            itemsControl.PointerReleased -= ItemsControl_PointerReleased;
            itemsControl.PointerMoved -= ItemsControl_PointerMovedForDrag;
            itemsControl.SizeChanged -= ItemsControl_SizeChanged;
        }

        public void Dispose()
        {
            NavigationSettingsMediator.UnregisterPanel(this);
            _currentOperationCts?.Cancel();
            _currentOperationCts?.Dispose();
            _dummyHistory?.Dispose();

            Loaded -= OnLoaded;

            // Отписываемся от событий обоих контролов
            UnsubscribeFromEvents(ItemsListView);
            UnsubscribeFromEvents(ItemsGridView);

            ItemsListView.ItemClick -= ItemsControl_OnItemClick;
            ItemsGridView.ItemClick -= ItemsControl_OnItemClick;

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

        #region INotifyPropertyChanged implementation

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Обновление режима отображения

        private void UpdateDisplayMode()
        {
            Debug.WriteLine($"[TileViewerContent] Switching to DisplayMode: {DisplayMode}");

            // Сохраняем текущее выделение
            var selectedItems = _currentItemsControl?.SelectedItems?.Cast<ExplorerItemViewModel>().ToList();

            // Переключаем видимость контролов
            switch (DisplayMode.ToLower())
            {
                case "horizontal":
                case "list":
                    ItemsListView.Visibility = Visibility.Visible;
                    ItemsGridView.Visibility = Visibility.Collapsed;
                    _currentItemsControl = ItemsListView;
                    break;

                case "vertical":
                case "icons":
                    ItemsListView.Visibility = Visibility.Collapsed;
                    ItemsGridView.Visibility = Visibility.Visible;
                    _currentItemsControl = ItemsGridView;
                    break;

                default:
                    ItemsListView.Visibility = Visibility.Visible;
                    ItemsGridView.Visibility = Visibility.Collapsed;
                    _currentItemsControl = ItemsListView;
                    break;
            }

            // Восстанавливаем выделение
            if (selectedItems != null && _currentItemsControl != null)
            {
                _currentItemsControl.SelectedItems.Clear();
                foreach (var item in selectedItems)
                {
                    _currentItemsControl.SelectedItems.Add(item);
                }
            }

            // Обновляем размеры элементов
            CalculateItemDimensions();
            UpdateItemsControlLayout();

            // Обновляем все элементы
            UpdateAllTiles();
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

        private void ItemsControl_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (!SingleClickOpenItem) return;

            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl) return;

            var element = e.OriginalSource as FrameworkElement;
            var item = FindParentDataContext<ExplorerItemViewModel>(element);

            if (item != null && item != _hoveredItem)
            {
                _hoveredItem = item;
                StartHoverTimer();
            }
        }

        private void ItemsControl_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (!SingleClickOpenItem) return;

            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl) return;

            StopHoverTimer();
            _hoveredItem = null;
        }

        private void ItemsControl_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!SingleClickOpenItem) return;

            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl) return;

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

        private void ItemsControl_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl) return;

            var point = e.GetCurrentPoint(itemsControl);

            // Сохраняем точку начала выделения
            _dragStartPoint = new Vector2((float)point.Position.X, (float)point.Position.Y);
            _wasClickHandled = false;

            // Если нажата левая кнопка мыши
            if (point.Properties.IsLeftButtonPressed)
            {
                _isLeftMouseButtonPressed = true;
                _isMouseMovingWithButton = false;

                // Ищем элемент под курсором
                var clickedItem = FindItemAtPoint(point.Position, itemsControl);
                if (clickedItem != null)
                {
                    // Если кликнули на элементе - обрабатываем как обычный клик
                    e.Handled = false;
                }
                else
                {
                    // Если кликнули на пустом месте - начинаем выделение областью
                    _isDragSelecting = true;
                    itemsControl.CapturePointer(e.Pointer);
                    CreateSelectionRectangle();
                    UpdateSelectionRectangle(_dragStartPoint, _dragStartPoint);

                    // Очищаем выделение если не нажат Ctrl
                    if (!_isCtrlPressed)
                    {
                        itemsControl.SelectedItems.Clear();
                    }

                    e.Handled = true;
                }
            }
        }

        private Control FindItemAtPoint(Windows.Foundation.Point point, ListViewBase itemsControl)
        {
            var elements = VisualTreeHelper.FindElementsInHostCoordinates(point, itemsControl);
            return elements.OfType<Control>().FirstOrDefault(c => c is ListViewItem || c is GridViewItem);
        }

        private void ItemsControl_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl) return;

            var point = e.GetCurrentPoint(itemsControl);

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
                if (itemsControl.PointerCaptures != null && itemsControl.PointerCaptures.Count > 0)
                {
                    itemsControl.ReleasePointerCapture(e.Pointer);
                }
            }

            if (_isDragSelecting)
            {
                _isDragSelecting = false;

                // Всегда сбрасываем при выделении областью
                _wasClickHandled = false;

                // Освобождаем указатель если он был захвачен
                if (itemsControl.PointerCaptures != null && itemsControl.PointerCaptures.Count > 0)
                {
                    itemsControl.ReleasePointerCapture(e.Pointer);
                }

                // Удаляем прямоугольник выделения
                RemoveSelectionRectangle();

                e.Handled = true;
            }
        }

        private void ItemsControl_PointerMovedForDrag(object sender, PointerRoutedEventArgs e)
        {
            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl) return;

            var point = e.GetCurrentPoint(itemsControl);

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
                    itemsControl.CapturePointer(e.Pointer);
                    // Создаем прямоугольник выделения
                    CreateSelectionRectangle();
                    UpdateSelectionRectangle(_dragStartPoint,
                        new Vector2((float)currentPosition.X, (float)currentPosition.Y));

                    // Очищаем выделение если не нажат Ctrl
                    if (!_isCtrlPressed)
                    {
                        itemsControl.SelectedItems.Clear();
                    }
                }

                if (_isDragSelecting)
                {
                    var currentPoint = new Vector2((float)point.Position.X, (float)point.Position.Y);

                    // Обновляем прямоугольник выделения
                    UpdateSelectionRectangle(_dragStartPoint, currentPoint);

                    // НЕМЕДЛЕННО выделяем элементы внутри прямоугольника
                    PerformRectangleSelection(_dragStartPoint, currentPoint, itemsControl, true);

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
        private void PerformRectangleSelection(Vector2 startPoint, Vector2 endPoint, ListViewBase itemsControl, bool applyImmediately = false)
        {
            if (Items.Count == 0) return;

            // Определяем границы прямоугольника
            float left = Math.Min(startPoint.X, endPoint.X);
            float right = Math.Max(startPoint.X, endPoint.X);
            float top = Math.Min(startPoint.Y, endPoint.Y);
            float bottom = Math.Max(startPoint.Y, endPoint.Y);

            var newSelectedIndices = new HashSet<int>();

            // Используем ItemsWrapGrid.Children для быстрого доступа к видимым элементам
            var panel = itemsControl.ItemsPanelRoot as ItemsWrapGrid;
            if (panel != null)
            {
                // Проходим по всем видимым элементам
                foreach (var child in panel.Children)
                {
                    if (child is FrameworkElement container && container.Visibility == Visibility.Visible)
                    {
                        int index = itemsControl.IndexFromContainer(container);
                        if (index >= 0 && index < Items.Count)
                        {
                            // Получаем позицию элемента
                            var transform = container.TransformToVisual(itemsControl);
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
                                if (itemsControl.SelectedItems.Contains(item))
                                {
                                    itemsControl.SelectedItems.Remove(item);
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
                        if (!itemsControl.SelectedItems.Contains(item))
                        {
                            itemsControl.SelectedItems.Add(item);
                        }
                    }
                }

                // Обновляем визуальное состояние
                UpdateSelectionVisual(itemsControl);
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
                _selectedSize = "Medium";
                Debug.WriteLine("Используются настройки по умолчанию.");
            }
            else
            {
                Debug.WriteLine($"Загружены настройки: Размер = {_selectedSize}");
            }

            CalculateItemDimensions();

            // Дать время UI на инициализацию
            await Task.Delay(50);

            UpdateAllTiles();
            UpdateItemsControlLayout();

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
            UpdateItemsControlLayout();
            ItemsListView.UpdateLayout();
            ItemsGridView.UpdateLayout();
        }

        private void CalculateItemDimensions()
        {
            var sizeParams = SizeManagerTile.GetSize(_selectedSize);
            if (DisplayMode.ToLower() == "vertical" || DisplayMode.ToLower() == "icons")
            {
                ItemWidth = Math.Max(sizeParams.Width + 20, MinimumItemWidth);
                ItemHeight = Math.Max(sizeParams.Height + 25, MinimumItemHeight);
            }
            else
            {
                ItemWidth = Math.Max(sizeParams.Width + 20, MinimumItemWidth);
                ItemHeight = Math.Max(sizeParams.Height + 20, MinimumItemHeight);
            }
            Debug.WriteLine($"Calculated dimensions: Width={ItemWidth}, Height={ItemHeight}");
        }

        #endregion

        #region Обновление отображения элементов

        private void UpdateAllTiles()
        {
            // Обновляем все видимые элементы
            UpdateTilesInControl(ItemsListView);
            UpdateTilesInControl(ItemsGridView);
        }

        private void UpdateTilesInControl(ListViewBase itemsControl)
        {
            foreach (var item in itemsControl.Items)
            {
                // Используем ContainerFromItem с правильным типом
                var container = GetContainerFromItem(itemsControl, item);
                if (container != null)
                {
                    var tile = GetContentTemplateRootFromContainer(container);
                    if (tile is BaseTileControl baseTile)
                    {
                        baseTile.UpdateSize(_selectedSize);
                    }
                }
            }
        }

        private FrameworkElement GetContainerFromItem(ListViewBase itemsControl, object item)
        {
            if (itemsControl is ListView listView)
            {
                return listView.ContainerFromItem(item) as FrameworkElement;
            }
            else if (itemsControl is GridView gridView)
            {
                return gridView.ContainerFromItem(item) as FrameworkElement;
            }
            return null;
        }

        private FrameworkElement GetContentTemplateRootFromContainer(FrameworkElement container)
        {
            if (container is ListViewItem listViewItem)
            {
                return listViewItem.ContentTemplateRoot as FrameworkElement;
            }
            else if (container is GridViewItem gridViewItem)
            {
                return gridViewItem.ContentTemplateRoot as FrameworkElement;
            }
            return null;
        }

        private void ItemsControl_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.Phase != 0) return;

            // Используем правильный тип для контейнера
            if (args.ItemContainer is ListViewItem listViewItem)
            {
                var tile = listViewItem.ContentTemplateRoot as BaseTileControl;
                if (tile != null)
                {
                    tile.UpdateSize(_selectedSize);
                }
            }
            else if (args.ItemContainer is GridViewItem gridViewItem)
            {
                var tile = gridViewItem.ContentTemplateRoot as BaseTileControl;
                if (tile != null)
                {
                    tile.UpdateSize(_selectedSize);
                }
            }
        }

        #endregion

        #region Обновление выделения

        private void UpdateSelectionVisual(ListViewBase itemsControl)
        {
            foreach (var item in itemsControl.SelectedItems)
            {
                var container = itemsControl.ContainerFromItem(item) as Control;
                if (container != null)
                {
                    VisualStateManager.GoToState(container, "Selected", false);
                }
            }
        }

        private void SelectItemOnHover(ExplorerItemViewModel item)
        {
            if (item == null || _isDragSelecting || _currentItemsControl == null) return;

            // НЕ выделяем элемент ".." при наведении!
            if (item.Name == "..") return;

            if (_currentItemsControl.SelectedItems.Contains(item))
                return;

            if (!_isCtrlPressed && !_isShiftPressed)
            {
                _currentItemsControl.SelectedItems.Clear();
            }

            if (!_currentItemsControl.SelectedItems.Contains(item))
            {
                _currentItemsControl.SelectedItems.Add(item);
            }

            _currentItemsControl.SelectedItem = item;

            Debug.WriteLine($"Hover selection: {item.Name}");
        }

        #endregion

        #region Layout и обновление UI

        private void UpdateItemsControlLayout()
        {
            // Обновляем оба контрола
            UpdateControlLayout(ItemsListView);
            UpdateControlLayout(ItemsGridView);
        }

        private void UpdateControlLayout(ListViewBase itemsControl)
        {
            if (itemsControl?.ItemsPanelRoot is ItemsWrapGrid wrapGrid)
            {
                wrapGrid.ItemWidth = ItemWidth;
                wrapGrid.ItemHeight = ItemHeight;

                if (itemsControl == ItemsListView)
                {
                    // Динамически вычисляем максимальное количество строк/колонок для ListView
                    UpdateMaxRowsOrColumns();
                    wrapGrid.MaximumRowsOrColumns = MaxRowsOrColumns;
                }
                else if (itemsControl == ItemsGridView)
                {
                    // Фиксированное значение для GridView
                    wrapGrid.MaximumRowsOrColumns = 24;
                }

                Debug.WriteLine($"Control layout updated: ItemWidth={wrapGrid.ItemWidth}, ItemHeight={wrapGrid.ItemHeight}, MaxRowsOrColumns={wrapGrid.MaximumRowsOrColumns}");
                UpdateSelectionVisual(itemsControl);
            }
        }

        private void UpdateMaxRowsOrColumns()
        {
            if (ItemHeight <= 0) return;

            var actualHeight = ItemsListView.ActualHeight;
            if (actualHeight > 0 && ItemHeight > 0)
            {
                int maxRows = Math.Max(1, (int)((actualHeight - 20) / ItemHeight));
                MaxRowsOrColumns = maxRows;
                Debug.WriteLine($"MaxRowsOrColumns calculated: {maxRows} rows (Height={actualHeight}, ItemHeight={ItemHeight})");
            }
        }

        // Обработчик изменения размера контрола
        private void ItemsControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var itemsControl = sender as ListViewBase;
            Debug.WriteLine($"ItemsControl size changed: {e.NewSize.Width}x{e.NewSize.Height} for {itemsControl.GetType().Name}");

            if (itemsControl == ItemsListView)
            {
                UpdateMaxRowsOrColumns();
            }
            UpdateControlLayout(itemsControl);
        }

        private void UpdateUIForSelection()
        {
            int selectedCount = _currentItemsControl?.SelectedItems.Count ?? 0;
            Debug.WriteLine($"Update UI: {selectedCount} items selected");
        }

        #endregion

        #region Обработка кликов

        private async void ItemsControl_OnItemClick(object sender, ItemClickEventArgs e)
        {
            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl) return;

            // Защита от быстрых повторных кликов
            var now = DateTime.Now;
            if ((now - _lastClickTime).TotalMilliseconds < 300)
            {
                Debug.WriteLine($"[{PanelId}] Click throttled - too fast");
                return;
            }
            _lastClickTime = now;

            Debug.WriteLine($"[{PanelId}] === ItemsControl_OnItemClick START ===");
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
                    Debug.WriteLine($"[{PanelId}] === ItemsControl_OnItemClick END (skipped) ===");
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
                        _shiftSelectionStartItem = _currentItemsControl.SelectedItem as ExplorerItemViewModel;
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
                    if (_currentItemsControl.SelectedItems.Contains(item))
                    {
                        _currentItemsControl.SelectedItems.Remove(item);
                    }
                    else
                    {
                        _currentItemsControl.SelectedItems.Add(item);
                    }
                    _shiftSelectionStartItem = item;
                }
                else
                {
                    // Обычный клик - открываем элемент
                    _currentItemsControl.SelectedItems.Clear();
                    _currentItemsControl.SelectedItem = item;
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
                        _shiftSelectionStartItem = _currentItemsControl.SelectedItem as ExplorerItemViewModel;
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
                    if (_currentItemsControl.SelectedItems.Contains(item))
                    {
                        _currentItemsControl.SelectedItems.Remove(item);
                    }
                    else
                    {
                        _currentItemsControl.SelectedItems.Add(item);
                    }
                    _shiftSelectionStartItem = item;
                }
                else
                {
                    // Обычный клик - выделяем и ждем возможного двойного клика
                    _currentItemsControl.SelectedItems.Clear();
                    _currentItemsControl.SelectedItem = item;
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

            Debug.WriteLine($"[{PanelId}] === ItemsControl_OnItemClick END ===");
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
            UpdateItemsControlLayout();

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
            UpdateItemsControlLayout();

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
                    UpdateItemsControlLayout();
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
                    UpdateItemsControlLayout();
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
                    UpdateItemsControlLayout();
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

        private void ItemsControl_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl) return;

            UpdateModifierKeyState(e.Key, true);

            bool isCtrlPressed = _isCtrlPressed;
            bool isShiftPressed = _isShiftPressed;

            Debug.WriteLine($"KeyDown: {e.Key}, Ctrl: {isCtrlPressed}, Shift: {isShiftPressed}");

            switch (e.Key)
            {
                case VirtualKey.A when isCtrlPressed && !isShiftPressed:
                    _currentItemsControl.SelectAll();
                    e.Handled = true;
                    Debug.WriteLine("Ctrl+A: Select all");
                    break;

                case VirtualKey.Space when isCtrlPressed:
                    ToggleCurrentSelection();
                    e.Handled = true;
                    Debug.WriteLine("Ctrl+Space: Toggle selection");
                    break;

                case VirtualKey.Enter:
                    if (_currentItemsControl.SelectedItem != null)
                    {
                        OpenSelectedItem();
                        e.Handled = true;
                        Debug.WriteLine("Enter: Open selected item");
                    }
                    break;

                case VirtualKey.F2:
                    if (_currentItemsControl.SelectedItems.Count == 1)
                    {
                        RenameSelectedItem();
                        e.Handled = true;
                        Debug.WriteLine("F2: Rename selected item");
                    }
                    break;

                case VirtualKey.Delete:
                    if (_currentItemsControl.SelectedItems.Count > 0)
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

        private void ItemsControl_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl) return;

            UpdateModifierKeyState(e.Key, false);

            if (e.Key == VirtualKey.Shift)
            {
                _shiftSelectionStartItem = null;
                Debug.WriteLine("Shift released, reset selection start");
            }
        }

        private void ItemsControl_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            // Можем добавить предварительную обработку при необходимости
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
            if (_currentItemsControl == null) return;

            int currentIndex = _currentItemsControl.SelectedIndex;
            int newIndex = currentIndex;

            // Разная логика для ListView и GridView
            if (_currentItemsControl == ItemsListView)
            {
                // Логика для ListView (горизонтальный режим)
                int itemsPerColumn = CalculateItemsPerColumnForListView();

                switch (key)
                {
                    case VirtualKey.Up:
                        newIndex = Math.Max(0, currentIndex - 1);
                        break;
                    case VirtualKey.Down:
                        newIndex = Math.Min(Items.Count - 1, currentIndex + 1);
                        break;
                    case VirtualKey.Left:
                        newIndex = Math.Max(0, currentIndex - itemsPerColumn);
                        break;
                    case VirtualKey.Right:
                        newIndex = Math.Min(Items.Count - 1, currentIndex + itemsPerColumn);
                        break;
                }
            }
            else
            {
                // Логика для GridView (вертикальный режим)
                int itemsPerRow = CalculateItemsPerRowForGridView();

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
                    _currentItemsControl.SelectedItem = newItem;
                    _currentItemsControl.ScrollIntoView(newItem);
                }
                else
                {
                    _currentItemsControl.SelectedItems.Clear();
                    _currentItemsControl.SelectedItem = newItem;
                    _currentItemsControl.ScrollIntoView(newItem);
                    _shiftSelectionStartItem = newItem;
                }

                Debug.WriteLine($"Arrow navigation: {key} from {currentIndex} to {newIndex}, Ctrl={isCtrlPressed}, Shift={isShiftPressed}");
            }
        }

        private void HandleHomeEndNavigation(VirtualKey key, bool isCtrlPressed, bool isShiftPressed)
        {
            if (_currentItemsControl == null) return;

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
                    _currentItemsControl.SelectedItem = newItem;
                    _currentItemsControl.ScrollIntoView(newItem);
                }
                else
                {
                    _currentItemsControl.SelectedItems.Clear();
                    _currentItemsControl.SelectedItem = newItem;
                    _currentItemsControl.ScrollIntoView(newItem);
                }

                Debug.WriteLine($"{key} navigation to index {newIndex}");
            }
        }

        private void HandlePageNavigation(VirtualKey key, bool isCtrlPressed, bool isShiftPressed)
        {
            if (_currentItemsControl == null) return;

            int currentIndex = _currentItemsControl.SelectedIndex;
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
                    _currentItemsControl.SelectedItems.Clear();
                    _currentItemsControl.SelectedItem = newItem;
                    _currentItemsControl.ScrollIntoView(newItem);
                }

                Debug.WriteLine($"{key}: from {currentIndex} to {newIndex}");
            }
        }

        private int CalculateItemsPerColumnForListView()
        {
            // Для горизонтальной ориентации ListView - это количество элементов в колонке (по вертикали)
            return MaxRowsOrColumns;
        }

        private int CalculateItemsPerRowForGridView()
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

            var itemsControl = _currentItemsControl;
            var panel = itemsControl?.ItemsPanelRoot as FrameworkElement;
            if (panel != null && panel.ActualHeight > 0)
            {
                int rowsPerPage = (int)Math.Floor(panel.ActualHeight / ItemHeight);
                if (itemsControl == ItemsListView)
                {
                    return rowsPerPage * CalculateItemsPerColumnForListView();
                }
                else
                {
                    return rowsPerPage * CalculateItemsPerRowForGridView();
                }
            }

            return 20;
        }

        #endregion

        #region Выделение элементов

        private void HandleShiftArrowSelection(int newIndex)
        {
            if (_shiftSelectionStartItem == null)
            {
                _shiftSelectionStartItem = _currentItemsControl.SelectedItem as ExplorerItemViewModel;
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
            int currentIndex = _currentItemsControl.SelectedIndex;
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
                _currentItemsControl.SelectedItems.Clear();
            }

            for (int i = minIndex; i <= maxIndex; i++)
            {
                if (!_currentItemsControl.SelectedItems.Contains(Items[i]))
                {
                    _currentItemsControl.SelectedItems.Add(Items[i]);
                }
            }

            _currentItemsControl.SelectedItem = Items[endIndex];
            _currentItemsControl.ScrollIntoView(Items[endIndex]);
        }

        private void ToggleCurrentSelection()
        {
            if (_currentItemsControl.SelectedItem is ExplorerItemViewModel currentItem)
            {
                if (_currentItemsControl.SelectedItems.Contains(currentItem))
                {
                    _currentItemsControl.SelectedItems.Remove(currentItem);
                    Debug.WriteLine($"Removed {currentItem.Name} from selection");
                }
                else
                {
                    _currentItemsControl.SelectedItems.Add(currentItem);
                    Debug.WriteLine($"Added {currentItem.Name} to selection");
                }
            }
        }

        #endregion

        #region Обработка двойного клика

        private async void ItemsControl_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl) return;

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

        private void ItemsControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl) return;

            Debug.WriteLine($"[{PanelId}] Selection changed: {itemsControl.SelectedItems.Count} items selected");

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

            // ВАЖНО: Сбрасываем состояние редактирования при открытии другого элемента
            // Отменяем редактирование, если оно было активно
            if (_currentItemsControl.SelectedItem is ExplorerItemViewModel selectedItem && selectedItem.IsEditing)
            {
                Debug.WriteLine($"[{PanelId}] Cancelling edit mode before opening new item");
                var container = GetContainerFromItem(_currentItemsControl, selectedItem);
                var tile = GetContentTemplateRootFromContainer(container) as BaseTileControl;
                if (tile != null && tile.IsEditing)
                {
                    tile.CancelEditing();
                }
            }

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
            if (_currentItemsControl.SelectedItem is ExplorerItemViewModel selectedItem)
            {
                await OpenItem(selectedItem);
            }
        }

        private void RenameSelectedItem()
        {
            try
            {
                Debug.WriteLine($"[{PanelId}] === RenameSelectedItem START ===");

                if (_currentItemsControl.SelectedItem is ExplorerItemViewModel selectedItem)
                {
                    Debug.WriteLine($"[{PanelId}] Selected item: {selectedItem.Name}");
                    Debug.WriteLine($"[{PanelId}] IsEditing: {selectedItem.IsEditing}, EditRequested: {selectedItem.EditRequested}");

                    // Проверяем, не редактируется ли уже этот элемент
                    if (selectedItem.IsEditing)
                    {
                        Debug.WriteLine($"[{PanelId}] Item is already in edit mode");
                        return;
                    }

                    // Находим контейнер элемента
                    var container = GetContainerFromItem(_currentItemsControl, selectedItem);
                    if (container != null)
                    {
                        // Получаем контрол элемента
                        var tile = GetContentTemplateRootFromContainer(container) as BaseTileControl;
                        if (tile != null)
                        {
                            if (tile.CanEdit)
                            {
                                Debug.WriteLine($"[{PanelId}] Calling StartEditing on tile control");
                                tile.StartEditing();
                            }
                            else
                            {
                                Debug.WriteLine($"[{PanelId}] Tile control doesn't support editing");
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"[{PanelId}] ContentTemplateRoot is not BaseTileControl");
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"[{PanelId}] Container not found, scrolling into view...");

                        // Прокручиваем к элементу
                        _currentItemsControl.ScrollIntoView(selectedItem);

                        // Пытаемся снова через небольшую задержку
                        _ = this.DispatcherQueue.EnqueueAsync(async () =>
                        {
                            await Task.Delay(100);

                            container = GetContainerFromItem(_currentItemsControl, selectedItem);
                            var tile = GetContentTemplateRootFromContainer(container) as BaseTileControl;
                            if (tile != null && tile.CanEdit)
                            {
                                Debug.WriteLine($"[{PanelId}] Retry successful, calling StartEditing");
                                tile.StartEditing();
                            }
                        }, Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal);
                    }
                }
                else
                {
                    Debug.WriteLine($"[{PanelId}] No item selected");
                }

                Debug.WriteLine($"[{PanelId}] === RenameSelectedItem END ===");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{PanelId}] Error in RenameSelectedItem: {ex.Message}");
            }
        }

        private async void DeleteSelectedItems()
        {
            var selectedItems = _currentItemsControl.SelectedItems.Cast<ExplorerItemViewModel>().ToList();
            if (selectedItems.Count > 0)
            {
                Debug.WriteLine($"Delete {selectedItems.Count} items");
            }
        }

        #endregion

        #region Обновление и Refresh

        public void RefreshNavigation()
        {
            Debug.WriteLine($"[TileViewerContent {PanelId}] Refreshing navigation via mediator");

            _ = this.DispatcherQueue.EnqueueAsync(() =>
            {
                if (_currentItemsControl != null)
                {
                    _currentItemsControl.SelectedItem = null;
                    _currentItemsControl.SelectedItems.Clear();
                }
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
                Debug.WriteLine($"[TileViewerContent {PanelId}] Starting refresh");

                string pathToReload = await DispatcherQueue.EnqueueAsync(() =>
                {
                    string path = _currentLoadedPath;
                    if (string.IsNullOrEmpty(path) && PanelManager != null)
                        path = PanelManager.CurrentPath;

                    _currentLoadedPath = null;
                    _isInitialized = false;
                    Items.Clear();
                    if (_currentItemsControl != null)
                    {
                        _currentItemsControl.SelectedItems.Clear();
                    }
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
                    UpdateItemsControlLayout();

                    return path;
                });

                Debug.WriteLine($"[TileViewerContent {PanelId}] Reloading path: '{pathToReload}'");

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

                Debug.WriteLine($"[TileViewerContent {PanelId}] Refresh completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileViewerContent {PanelId}] Refresh error: {ex}");

                try
                {
                    await DispatcherQueue.EnqueueAsync(() => LoadInitialContent());
                }
                catch (Exception fallbackEx)
                {
                    Debug.WriteLine($"[TileViewerContent {PanelId}] Fallback failed: {fallbackEx}");
                }
            }
        }

        #endregion
    }
}