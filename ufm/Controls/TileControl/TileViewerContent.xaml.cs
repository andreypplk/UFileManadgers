//using CommunityToolkit.WinUI;
//using Core_FileManagement;
//using Microsoft.UI.Dispatching;
//using Microsoft.UI.Input;
//using Microsoft.UI.Xaml;
//using Microsoft.UI.Xaml.Controls;
//using Microsoft.UI.Xaml.Controls.Primitives;
//using Microsoft.UI.Xaml.Input;
//using Microsoft.UI.Xaml.Media;
//using Microsoft.UI.Xaml.Media.Imaging;
//using Microsoft.UI.Xaml.Shapes;
//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.ComponentModel;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Numerics;
//using System.Runtime.CompilerServices;
//using System.Threading;
//using System.Threading.Tasks;
//using Windows.Storage;
//using Windows.System;
//using Windows.UI.Core;
//using Windows.UI.Input;

//namespace ufm
//{
//    public sealed partial class TileViewerContent : UserControl, IDisposable, ISupportsIconSize, IRefreshablePanel, INotifyPropertyChanged
//    {
//        #region Поля и свойства

//        public string PanelId { get; set; } = "DefaultPanel";
//        public PanelManager PanelManager { get; private set; }
//        public event EventHandler NavigationChanged;

//        private CancellationTokenSource _currentOperationCts;
//        private readonly IDirectoryHistory _dummyHistory;
//        private string _currentLoadedPath;

//        private bool _isInitialized = false;
//        private bool _isLoading = false;
//        public ObservableCollection<ExplorerItemViewModel> Items { get; } = new ObservableCollection<ExplorerItemViewModel>();

//        private readonly FileSystemService _fileSystemService;

//        private double _itemWidth;
//        private double _itemHeight;

//        private DateTime _lastClickTime = DateTime.MinValue;
//        private ExplorerItemViewModel _lastClickedItem = null;

//        private ExplorerItemViewModel _shiftSelectionStartItem = null;

//        private bool _isCtrlPressed = false;
//        private bool _isShiftPressed = false;
//        private bool _isAltPressed = false;

//        private bool _isDragSelecting = false;
//        private bool _isLeftMouseButtonPressed = false;
//        private bool _isMouseMovingWithButton = false;
//        private Vector2 _dragStartPoint;
//        private Rectangle _selectionRectangle;
//        private Canvas _selectionCanvas;
//        private Grid _parentGrid;

//        private DispatcherTimer _hoverTimer;
//        private ExplorerItemViewModel _hoveredItem = null;
//        private const int HOVER_DELAY_MS = 50;

//        private bool _wasClickHandled = false;

//        private HashSet<int> _tempSelectedIndices = new HashSet<int>();

//        private readonly SemaphoreSlim _navigationSemaphore = new SemaphoreSlim(1, 1);
//        private bool _isProcessingBackNavigation = false;

//        private int _maxRowsOrColumns = 1;
//        public int MaxRowsOrColumns
//        {
//            get => _maxRowsOrColumns;
//            private set
//            {
//                if (_maxRowsOrColumns != value)
//                {
//                    Debug.WriteLine($"[{PanelId}] [MaxRowsOrColumns] Changing from {_maxRowsOrColumns} to {value}");
//                    _maxRowsOrColumns = value;
//                    OnPropertyChanged();
//                }
//            }
//        }

//        private string _displayMode = "Horizontal";
//        public string DisplayMode
//        {
//            get => _displayMode;
//            set
//            {
//                if (_displayMode != value)
//                {
//                    Debug.WriteLine($"[{PanelId}] [DisplayMode] Changing from '{_displayMode}' to '{value}'");
//                    _displayMode = value;
//                    UpdateDisplayMode();
//                    OnPropertyChanged();
//                }
//            }
//        }

//        private ListViewBase _currentItemsControl;

//        public double ItemWidth
//        {
//            get => _itemWidth;
//            private set
//            {
//                if (_itemWidth != value)
//                {
//                    Debug.WriteLine($"[{PanelId}] [ItemWidth] Changing from {_itemWidth} to {value}");
//                    _itemWidth = value;
//                    UpdateItemsControlLayout();
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
//                    Debug.WriteLine($"[{PanelId}] [ItemHeight] Changing from {_itemHeight} to {value}");
//                    _itemHeight = value;
//                    UpdateItemsControlLayout();
//                }
//            }
//        }

//        private string _selectedSize = "Icons Medium";
//        private int _hoverSelectionLock = 0;

//        private const int HorizontalPadding = 10;
//        private const int VerticalPadding = 8;
//        private const int TextBlockHeight = 40;
//        private const int MinimumItemWidth = 100;
//        private const int MinimumItemHeight = 40;

//        // Защитный интервал после редактирования
//        private DateTime _lastEditEndTime = DateTime.MinValue;
//        private const int EditCooldownMs = 300;

//        // Защита от восстановления выделения после клика на пустое место
//        private DateTime _lastEmptySpaceClickTime = DateTime.MinValue;
//        private const int EmptySpaceClickCooldownMs = 300;

//        // Для множественного переименования
//        private bool _isMultiRenameMode = false;
//        private List<string> _multiRenamePaths;  // Список исходных путей выделенных элементов
//        private int _multiRenameCurrentIndex;    // Индекс текущего обрабатываемого пути
//        private const string MultiRenameLogPrefix = "[MultiRename]";

//        public bool SingleClickOpenItem
//        {
//            get
//            {
//                try
//                {
//                    return App.SettingsManager?.GetSetting<bool>("SingleClickOpen", false) ?? false;
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"[{PanelId}] [SingleClickOpen] Error: {ex}");
//                    return false;
//                }
//            }
//        }

//        public event PropertyChangedEventHandler PropertyChanged;

//        #endregion

//        #region Конструктор и Dispose

//        public TileViewerContent()
//        {
//            Debug.WriteLine($"[{PanelId}] [Constructor] Entering TileViewerContent constructor");

//            InitializeComponent();

//            NavigationSettingsMediator.RegisterPanel(this);

//            _dummyHistory = new DirectoryHistory("MyComputer", "Мой Компьютер");

//            _fileSystemService = new FileSystemService();

//            _currentItemsControl = ItemsListView;
//            ItemsListView.ItemsSource = Items;
//            ItemsGridView.ItemsSource = Items;

//            SubscribeToEvents(_currentItemsControl);
//            if (_currentItemsControl is ListView listView)
//                listView.ItemClick += ItemsControl_OnItemClick;

//            Loaded += OnLoaded;

//            InitializeHoverTimer();

//            this.Loaded += (s, e) => InitializeSelectionCanvas();

//            CalculateItemDimensions();

//            Debug.WriteLine($"[{PanelId}] [Constructor] Exiting, DisplayMode={_displayMode}, SelectedSize={_selectedSize}");
//        }

//        private void SubscribeToEvents(ListViewBase itemsControl)
//        {
//            Debug.WriteLine($"[{PanelId}] [SubscribeToEvents] Subscribing to events for {itemsControl.GetType().Name}");
//            itemsControl.ContainerContentChanging += ItemsControl_ContainerContentChanging;
//            itemsControl.DoubleTapped += ItemsControl_DoubleTapped;
//            itemsControl.SelectionChanged += ItemsControl_SelectionChanged;
//            itemsControl.KeyDown += ItemsControl_KeyDown;
//            itemsControl.KeyUp += ItemsControl_KeyUp;
//            itemsControl.PreviewKeyDown += ItemsControl_PreviewKeyDown;

//            itemsControl.PointerEntered += ItemsControl_PointerEntered;
//            itemsControl.PointerExited += ItemsControl_PointerExited;
//            itemsControl.PointerMoved += ItemsControl_PointerMoved;

//            itemsControl.PointerPressed += ItemsControl_PointerPressed;
//            itemsControl.PointerReleased += ItemsControl_PointerReleased;
//            itemsControl.PointerMoved += ItemsControl_PointerMovedForDrag;
//            itemsControl.SizeChanged += ItemsControl_SizeChanged;
//        }

//        private void UnsubscribeFromEvents(ListViewBase itemsControl)
//        {
//            Debug.WriteLine($"[{PanelId}] [UnsubscribeFromEvents] Unsubscribing from events for {itemsControl.GetType().Name}");
//            itemsControl.ContainerContentChanging -= ItemsControl_ContainerContentChanging;
//            itemsControl.DoubleTapped -= ItemsControl_DoubleTapped;
//            itemsControl.SelectionChanged -= ItemsControl_SelectionChanged;
//            itemsControl.KeyDown -= ItemsControl_KeyDown;
//            itemsControl.KeyUp -= ItemsControl_KeyUp;
//            itemsControl.PreviewKeyDown -= ItemsControl_PreviewKeyDown;

//            itemsControl.PointerEntered -= ItemsControl_PointerEntered;
//            itemsControl.PointerExited -= ItemsControl_PointerExited;
//            itemsControl.PointerMoved -= ItemsControl_PointerMoved;

//            itemsControl.PointerPressed -= ItemsControl_PointerPressed;
//            itemsControl.PointerReleased -= ItemsControl_PointerReleased;
//            itemsControl.PointerMoved -= ItemsControl_PointerMovedForDrag;
//            itemsControl.SizeChanged -= ItemsControl_SizeChanged;
//        }

//        public void Dispose()
//        {
//            Debug.WriteLine($"[{PanelId}] [Dispose] Entering Dispose");
//            NavigationSettingsMediator.UnregisterPanel(this);
//            _currentOperationCts?.Cancel();
//            _currentOperationCts?.Dispose();
//            _dummyHistory?.Dispose();

//            Loaded -= OnLoaded;

//            UnsubscribeFromEvents(ItemsListView);
//            UnsubscribeFromEvents(ItemsGridView);

//            ItemsListView.ItemClick -= ItemsControl_OnItemClick;
//            ItemsGridView.ItemClick -= ItemsControl_OnItemClick;

//            if (_hoverTimer != null)
//            {
//                _hoverTimer.Stop();
//                _hoverTimer.Tick -= HoverTimer_Tick;
//                _hoverTimer = null;
//            }

//            _fileSystemService.ClearPanelCache(PanelId);
//            _fileSystemService?.Dispose();

//            if (PanelManager != null)
//            {
//                PanelManager.NavigationChanged -= OnPanelNavigationChanged;
//            }

//            RemoveSelectionCanvas();

//            foreach (var item in Items)
//            {
//                item?.Dispose();
//            }

//            _tempSelectedIndices.Clear();
//            _navigationSemaphore?.Dispose();
//            Debug.WriteLine($"[{PanelId}] [Dispose] Exiting");
//        }

//        private void RemoveSelectionCanvas()
//        {
//            if (_selectionCanvas != null && _parentGrid != null)
//            {
//                _parentGrid.Children.Remove(_selectionCanvas);
//                _selectionCanvas = null;
//                Debug.WriteLine($"[{PanelId}] [RemoveSelectionCanvas] Canvas removed");
//            }
//        }

//        #endregion

//        #region INotifyPropertyChanged implementation

//        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
//        {
//            Debug.WriteLine($"[{PanelId}] [OnPropertyChanged] Property '{propertyName}' changed");
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        }

//        #endregion

//        #region Обновление режима отображения

//        private void UpdateDisplayMode()
//        {
//            Debug.WriteLine($"[{PanelId}] [UpdateDisplayMode] Switching to DisplayMode: {DisplayMode}");

//            var selectedItems = _currentItemsControl?.SelectedItems?.Cast<ExplorerItemViewModel>().ToList();
//            Debug.WriteLine($"[{PanelId}] [UpdateDisplayMode] Saved {selectedItems?.Count ?? 0} selected items");

//            var oldControl = _currentItemsControl;

//            switch (DisplayMode.ToLower())
//            {
//                case "horizontal":
//                case "list":
//                    ItemsListView.Visibility = Visibility.Visible;
//                    ItemsGridView.Visibility = Visibility.Collapsed;
//                    _currentItemsControl = ItemsListView;
//                    Debug.WriteLine($"[{PanelId}] [UpdateDisplayMode] Activated ListView");
//                    break;

//                case "vertical":
//                case "icons":
//                    ItemsListView.Visibility = Visibility.Collapsed;
//                    ItemsGridView.Visibility = Visibility.Visible;
//                    _currentItemsControl = ItemsGridView;
//                    Debug.WriteLine($"[{PanelId}] [UpdateDisplayMode] Activated GridView");
//                    break;

//                default:
//                    ItemsListView.Visibility = Visibility.Visible;
//                    ItemsGridView.Visibility = Visibility.Collapsed;
//                    _currentItemsControl = ItemsListView;
//                    Debug.WriteLine($"[{PanelId}] [UpdateDisplayMode] Default to ListView");
//                    break;
//            }

//            if (oldControl != _currentItemsControl)
//            {
//                Debug.WriteLine($"[{PanelId}] [UpdateDisplayMode] Control changed, re-subscribing events");
//                UnsubscribeFromEvents(oldControl);
//                if (oldControl is ListView oldListView)
//                    oldListView.ItemClick -= ItemsControl_OnItemClick;
//                else if (oldControl is GridView oldGridView)
//                    oldGridView.ItemClick -= ItemsControl_OnItemClick;

//                SubscribeToEvents(_currentItemsControl);
//                if (_currentItemsControl is ListView newListView)
//                    newListView.ItemClick += ItemsControl_OnItemClick;
//                else if (_currentItemsControl is GridView newGridView)
//                    newGridView.ItemClick += ItemsControl_OnItemClick;
//            }

//            // Пересоздаем Canvas при смене режима
//            RecreateSelectionCanvas();

//            if (selectedItems != null && _currentItemsControl != null)
//            {
//                _currentItemsControl.SelectedItems.Clear();
//                foreach (var item in selectedItems)
//                {
//                    _currentItemsControl.SelectedItems.Add(item);
//                }
//                Debug.WriteLine($"[{PanelId}] [UpdateDisplayMode] Restored {selectedItems.Count} selected items");
//            }

//            CalculateItemDimensions();
//            UpdateItemsControlLayout();
//            UpdateAllTiles();

//            Debug.WriteLine($"[{PanelId}] [UpdateDisplayMode] Completed");
//        }

//        // НОВЫЙ МЕТОД: Пересоздание Canvas для текущего режима
//        private void RecreateSelectionCanvas()
//        {
//            RemoveSelectionCanvas();
//            _selectionCanvas = null;
//            InitializeSelectionCanvas();
//        }

//        #endregion

//        #region Инициализация таймера выделения при наведении

//        private void InitializeHoverTimer()
//        {
//            Debug.WriteLine($"[{PanelId}] [InitializeHoverTimer] Creating hover timer");
//            _hoverTimer = new DispatcherTimer();
//            _hoverTimer.Interval = TimeSpan.FromMilliseconds(HOVER_DELAY_MS);
//            _hoverTimer.Tick += HoverTimer_Tick;
//        }

//        private void HoverTimer_Tick(object sender, object e)
//        {
//            Debug.WriteLine($"[{PanelId}] [HoverTimer_Tick] Timer tick");
//            _hoverTimer.Stop();

//            UpdateModifierKeyStateFromCore();

//            if (_hoveredItem != null && SingleClickOpenItem && !_isDragSelecting)
//            {
//                Debug.WriteLine($"[{PanelId}] [HoverTimer_Tick] Selecting item on hover: {_hoveredItem.Name}");

//                // Проверяем, что элемент все еще существует в коллекции
//                if (Items.Contains(_hoveredItem))
//                {
//                    SelectItemOnHover(_hoveredItem);
//                }
//                else
//                {
//                    Debug.WriteLine($"[{PanelId}] [HoverTimer_Tick] Hovered item no longer in collection, resetting");
//                    _hoveredItem = null;
//                }
//            }
//            else
//            {
//                Debug.WriteLine($"[{PanelId}] [HoverTimer_Tick] No action: HoveredItem={_hoveredItem?.Name}, SingleClickOpenItem={SingleClickOpenItem}, IsDragSelecting={_isDragSelecting}");
//            }
//        }

//        private void StartHoverTimer()
//        {
//            if (_hoverTimer != null && !_hoverTimer.IsEnabled)
//            {
//                Debug.WriteLine($"[{PanelId}] [StartHoverTimer] Starting timer");
//                _hoverTimer.Start();
//            }
//        }

//        private void StopHoverTimer()
//        {
//            if (_hoverTimer != null && _hoverTimer.IsEnabled)
//            {
//                Debug.WriteLine($"[{PanelId}] [StopHoverTimer] Stopping timer");
//                _hoverTimer.Stop();
//            }
//        }

//        private void RestartHoverTimer()
//        {
//            Debug.WriteLine($"[{PanelId}] [RestartHoverTimer] Restarting timer");
//            StopHoverTimer();
//            StartHoverTimer();
//        }

//        #endregion

//        #region Инициализация Canvas для выделения

//        private void InitializeSelectionCanvas()
//        {
//            Debug.WriteLine($"[{PanelId}] [InitializeSelectionCanvas] Entering");
//            if (_selectionCanvas != null)
//            {
//                Debug.WriteLine($"[{PanelId}] [InitializeSelectionCanvas] Canvas already exists, returning");
//                return;
//            }

//            _selectionCanvas = new Canvas
//            {
//                IsHitTestVisible = false,
//                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent)
//            };

//            // Получаем родительский Grid
//            _parentGrid = this.Content as Grid;
//            if (_parentGrid != null)
//            {
//                _parentGrid.Children.Add(_selectionCanvas);
//                Canvas.SetZIndex(_selectionCanvas, 1000);
//                Debug.WriteLine($"[{PanelId}] [InitializeSelectionCanvas] Canvas added to root grid");
//            }
//            else
//            {
//                Debug.WriteLine($"[{PanelId}] [InitializeSelectionCanvas] Root grid not found");
//            }
//        }

//        #endregion

//        #region Вспомогательный метод: обновление состояния клавиш-модификаторов

//        private void UpdateModifierKeyStateFromCore()
//        {
//            try
//            {
//                var ctrlState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
//                var shiftState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift);
//                var altState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu);

//                bool oldCtrl = _isCtrlPressed, oldShift = _isShiftPressed, oldAlt = _isAltPressed;
//                _isCtrlPressed = ctrlState.HasFlag(CoreVirtualKeyStates.Down);
//                _isShiftPressed = shiftState.HasFlag(CoreVirtualKeyStates.Down);
//                _isAltPressed = altState.HasFlag(CoreVirtualKeyStates.Down);

//                if (oldCtrl != _isCtrlPressed || oldShift != _isShiftPressed || oldAlt != _isAltPressed)
//                {
//                    Debug.WriteLine($"[{PanelId}] [UpdateModifierKeyStateFromCore] Ctrl={_isCtrlPressed}, Shift={_isShiftPressed}, Alt={_isAltPressed}");
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[{PanelId}] [UpdateModifierKeyStateFromCore] Error: {ex}");

//                var coreWindow = CoreWindow.GetForCurrentThread();
//                if (coreWindow != null)
//                {
//                    _isCtrlPressed = coreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
//                    _isShiftPressed = coreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
//                    _isAltPressed = coreWindow.GetKeyState(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down);
//                    Debug.WriteLine($"[{PanelId}] [UpdateModifierKeyStateFromCore] Fallback: Ctrl={_isCtrlPressed}, Shift={_isShiftPressed}, Alt={_isAltPressed}");
//                }
//            }
//        }

//        #endregion

//        #region Обработка событий мыши для выделения при наведении

//        private void ItemsControl_PointerEntered(object sender, PointerRoutedEventArgs e)
//        {
//            Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerEntered] Entering");
//            UpdateModifierKeyStateFromCore();
//            if (!SingleClickOpenItem) return;

//            var itemsControl = sender as ListViewBase;
//            if (itemsControl != _currentItemsControl) return;

//            var element = e.OriginalSource as FrameworkElement;
//            var item = FindParentDataContext<ExplorerItemViewModel>(element);

//            if (item != null)
//            {
//                Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerEntered] Hovered item: {item.Name} (Hash: {item.GetHashCode()})");
//                _hoveredItem = item;
//                StartHoverTimer();
//            }
//        }

//        private void ItemsControl_PointerExited(object sender, PointerRoutedEventArgs e)
//        {
//            Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerExited] Entering");
//            UpdateModifierKeyStateFromCore();
//            if (!SingleClickOpenItem) return;

//            var itemsControl = sender as ListViewBase;
//            if (itemsControl != _currentItemsControl) return;

//            StopHoverTimer();
//            if (_hoveredItem != null)
//            {
//                Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerExited] Hover cleared (was {_hoveredItem.Name})");
//                _hoveredItem = null;
//            }
//        }

//        private void ItemsControl_PointerMoved(object sender, PointerRoutedEventArgs e)
//        {
//            UpdateModifierKeyStateFromCore();
//            if (!SingleClickOpenItem) return;

//            var itemsControl = sender as ListViewBase;
//            if (itemsControl != _currentItemsControl) return;

//            var element = e.OriginalSource as FrameworkElement;
//            var item = FindParentDataContext<ExplorerItemViewModel>(element);

//            if (item != null)
//            {
//                if (item != _hoveredItem)
//                {
//                    Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerMoved] New hover item: {item.Name} (Hash: {item.GetHashCode()})");
//                    _hoveredItem = item;
//                    RestartHoverTimer();
//                }
//            }
//            else if (_hoveredItem != null)
//            {
//                Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerMoved] Hover lost (was {_hoveredItem.Name})");
//                StopHoverTimer();
//                _hoveredItem = null;
//            }
//        }

//        private T FindParentDataContext<T>(FrameworkElement element) where T : class
//        {
//            while (element != null)
//            {
//                if (element.DataContext is T dataContext)
//                    return dataContext;

//                element = VisualTreeHelper.GetParent(element) as FrameworkElement;
//            }
//            return null;
//        }

//        #endregion

//        #region Выделение областью мышью

//        // ИСПРАВЛЕННЫЙ МЕТОД: Определение клика на элемент (только по контейнеру)
//        private bool IsClickOnItem(Windows.Foundation.Point point, ListViewBase itemsControl)
//        {
//            var elements = VisualTreeHelper.FindElementsInHostCoordinates(point, itemsControl);

//            foreach (var element in elements)
//            {
//                var current = element as DependencyObject;
//                while (current != null)
//                {
//                    // Проверяем, является ли текущий элемент контейнером элемента
//                    if (current is ListViewItem || current is GridViewItem)
//                    {
//                        // Убедимся, что контейнер действительно содержит элемент данных
//                        var container = current as FrameworkElement;
//                        if (container?.DataContext is ExplorerItemViewModel)
//                        {
//                            Debug.WriteLine($"[{PanelId}] [IsClickOnItem] Found item container with DataContext at point ({point.X}, {point.Y})");
//                            return true;
//                        }
//                        // Если контейнер есть, но DataContext не тот – возможно, пустой контейнер, тогда не считаем кликом на элементе
//                        // (такого быть не должно, но на всякий случай игнорируем)
//                    }

//                    current = VisualTreeHelper.GetParent(current);
//                }
//            }

//            Debug.WriteLine($"[{PanelId}] [IsClickOnItem] No item container found at point ({point.X}, {point.Y}) - empty space");
//            return false;
//        }

//        // ИСПРАВЛЕННЫЙ МЕТОД: Получение элемента под курсором через контейнер
//        private ExplorerItemViewModel GetItemAtPoint(Windows.Foundation.Point point, ListViewBase itemsControl)
//        {
//            var elements = VisualTreeHelper.FindElementsInHostCoordinates(point, itemsControl);

//            foreach (var element in elements)
//            {
//                var current = element as FrameworkElement;
//                while (current != null)
//                {
//                    // Если это контейнер элемента, берем его DataContext
//                    if (current is ListViewItem || current is GridViewItem)
//                    {
//                        if (current.DataContext is ExplorerItemViewModel item)
//                        {
//                            Debug.WriteLine($"[{PanelId}] [GetItemAtPoint] Found item '{item.Name}' at point ({point.X}, {point.Y}) via container");
//                            return item;
//                        }
//                    }
//                    current = VisualTreeHelper.GetParent(current) as FrameworkElement;
//                }
//            }

//            return null;
//        }

//        private void ItemsControl_PointerPressed(object sender, PointerRoutedEventArgs e)
//        {
//            Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerPressed] Entering");
//            UpdateModifierKeyStateFromCore();

//            var itemsControl = sender as ListViewBase;
//            if (itemsControl != _currentItemsControl) return;

//            var point = e.GetCurrentPoint(itemsControl);

//            _dragStartPoint = new Vector2((float)point.Position.X, (float)point.Position.Y);
//            _wasClickHandled = false;
//            Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerPressed] Start point: {_dragStartPoint.X}, {_dragStartPoint.Y}");

//            if (point.Properties.IsLeftButtonPressed)
//            {
//                _isLeftMouseButtonPressed = true;
//                _isMouseMovingWithButton = false;

//                // Проверяем, кликнули ли на контейнере элемента
//                bool isClickOnItem = IsClickOnItem(point.Position, itemsControl);

//                if (isClickOnItem)
//                {
//                    Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerPressed] Clicked on an item container");

//                    // Получаем элемент под курсором для hover-выделения
//                    var clickedItem = GetItemAtPoint(point.Position, itemsControl);
//                    if (clickedItem != null && SingleClickOpenItem)
//                    {
//                        _hoveredItem = clickedItem;
//                        RestartHoverTimer();
//                    }

//                    e.Handled = false;
//                }
//                else
//                {
//                    Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerPressed] Clicked on empty space");

//                    // Запоминаем время клика на пустое место для защиты от восстановления выделения
//                    _lastEmptySpaceClickTime = DateTime.Now;
//                    Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerPressed] _lastEmptySpaceClickTime set to: {_lastEmptySpaceClickTime:HH:mm:ss.fff}");

//                    // ПОЛНЫЙ СБРОС ВСЕГО СОСТОЯНИЯ НАВЕДЕНИЯ
//                    StopHoverTimer();
//                    _hoveredItem = null;
//                    _lastEditEndTime = DateTime.MinValue;

//                    _isDragSelecting = true;
//                    itemsControl.CapturePointer(e.Pointer);
//                    CreateSelectionRectangle();
//                    UpdateSelectionRectangle(_dragStartPoint, _dragStartPoint);

//                    if (!_isCtrlPressed)
//                    {
//                        itemsControl.SelectedItems.Clear();
//                        Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerPressed] Cleared selection");

//                        // Явно устанавливаем SelectedItem в null
//                        itemsControl.SelectedItem = null;
//                    }

//                    e.Handled = true;

//                    // ВАЖНО: Возвращаем фокус на ItemsControl после клика на пустое место
//                    _ = DispatcherQueue.TryEnqueue(() =>
//                    {
//                        Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerPressed] Returning focus to items control");
//                        itemsControl.Focus(FocusState.Programmatic);
//                    });
//                }
//            }
//        }

//        private void ItemsControl_PointerReleased(object sender, PointerRoutedEventArgs e)
//        {
//            Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerReleased] Entering");
//            UpdateModifierKeyStateFromCore();

//            var itemsControl = sender as ListViewBase;
//            if (itemsControl != _currentItemsControl) return;

//            var point = e.GetCurrentPoint(itemsControl);

//            if (!point.Properties.IsLeftButtonPressed)
//            {
//                _isLeftMouseButtonPressed = false;
//                _isMouseMovingWithButton = false;

//                if (SingleClickOpenItem)
//                {
//                    _wasClickHandled = false;
//                }

//                if (itemsControl.PointerCaptures != null && itemsControl.PointerCaptures.Count > 0)
//                {
//                    itemsControl.ReleasePointerCapture(e.Pointer);
//                    Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerReleased] Pointer released");
//                }
//            }

//            if (_isDragSelecting)
//            {
//                Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerReleased] Ending drag selection");
//                _isDragSelecting = false;
//                _wasClickHandled = false;

//                if (itemsControl.PointerCaptures != null && itemsControl.PointerCaptures.Count > 0)
//                {
//                    itemsControl.ReleasePointerCapture(e.Pointer);
//                }

//                RemoveSelectionRectangle();
//                e.Handled = true;

//                // ВАЖНО: Возвращаем фокус на ItemsControl после завершения drag selection
//                _ = DispatcherQueue.TryEnqueue(() =>
//                {
//                    Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerReleased] Returning focus to items control");
//                    itemsControl.Focus(FocusState.Programmatic);
//                });
//            }
//        }

//        private void ItemsControl_PointerMovedForDrag(object sender, PointerRoutedEventArgs e)
//        {
//            UpdateModifierKeyStateFromCore();

//            var itemsControl = sender as ListViewBase;
//            if (itemsControl != _currentItemsControl) return;

//            var point = e.GetCurrentPoint(itemsControl);

//            if (_isLeftMouseButtonPressed && point.Properties.IsLeftButtonPressed)
//            {
//                var currentPosition = point.Position;

//                float distance = Vector2.Distance(_dragStartPoint,
//                    new Vector2((float)currentPosition.X, (float)currentPosition.Y));

//                if (distance > 3.0f && !_isMouseMovingWithButton)
//                {
//                    _isMouseMovingWithButton = true;
//                    Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerMovedForDrag] Mouse moved with button, distance={distance}");
//                }

//                if (distance > 10.0f && !_isDragSelecting)
//                {
//                    Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerMovedForDrag] Starting drag selection, distance={distance}");
//                    _isDragSelecting = true;
//                    itemsControl.CapturePointer(e.Pointer);
//                    CreateSelectionRectangle();
//                    UpdateSelectionRectangle(_dragStartPoint,
//                        new Vector2((float)currentPosition.X, (float)currentPosition.Y));

//                    if (!_isCtrlPressed)
//                    {
//                        itemsControl.SelectedItems.Clear();
//                        // Явно устанавливаем SelectedItem в null
//                        itemsControl.SelectedItem = null;
//                    }
//                }

//                if (_isDragSelecting)
//                {
//                    var currentPoint = new Vector2((float)point.Position.X, (float)point.Position.Y);
//                    UpdateSelectionRectangle(_dragStartPoint, currentPoint);
//                    PerformRectangleSelection(_dragStartPoint, currentPoint, itemsControl, true);
//                    e.Handled = true;
//                }
//            }
//        }

//        private void CreateSelectionRectangle()
//        {
//            Debug.WriteLine($"[{PanelId}] [CreateSelectionRectangle] Creating selection rectangle");
//            if (_selectionRectangle != null) return;

//            if (_selectionCanvas == null)
//            {
//                InitializeSelectionCanvas();
//            }

//            _selectionRectangle = new Rectangle
//            {
//                Stroke = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue),
//                StrokeThickness = 1,
//                Fill = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue) { Opacity = 0.3 },
//                StrokeDashArray = new DoubleCollection() { 2, 2 },
//                Width = 0,
//                Height = 0
//            };

//            if (_selectionCanvas != null)
//            {
//                _selectionCanvas.Children.Clear();
//                _selectionCanvas.Children.Add(_selectionRectangle);
//                Canvas.SetLeft(_selectionRectangle, 0);
//                Canvas.SetTop(_selectionRectangle, 0);
//                Debug.WriteLine($"[{PanelId}] [CreateSelectionRectangle] Rectangle added to canvas");
//            }
//        }

//        private void UpdateSelectionRectangle(Vector2 startPoint, Vector2 endPoint)
//        {
//            if (_selectionRectangle == null || _selectionCanvas == null) return;

//            float left = Math.Min(startPoint.X, endPoint.X);
//            float top = Math.Min(startPoint.Y, endPoint.Y);
//            float width = Math.Abs(endPoint.X - startPoint.X);
//            float height = Math.Abs(endPoint.Y - startPoint.Y);

//            Canvas.SetLeft(_selectionRectangle, left);
//            Canvas.SetTop(_selectionRectangle, top);
//            _selectionRectangle.Width = width;
//            _selectionRectangle.Height = height;

//            Debug.WriteLine($"[{PanelId}] [UpdateSelectionRectangle] Rect: left={left}, top={top}, width={width}, height={height}");
//        }

//        private void RemoveSelectionRectangle()
//        {
//            Debug.WriteLine($"[{PanelId}] [RemoveSelectionRectangle] Removing selection rectangle");
//            if (_selectionRectangle != null && _selectionCanvas != null)
//            {
//                _selectionCanvas.Children.Remove(_selectionRectangle);
//                _selectionRectangle = null;
//            }
//        }

//        private void PerformRectangleSelection(Vector2 startPoint, Vector2 endPoint, ListViewBase itemsControl, bool applyImmediately = false)
//        {
//            if (Items.Count == 0) return;

//            float left = Math.Min(startPoint.X, endPoint.X);
//            float right = Math.Max(startPoint.X, endPoint.X);
//            float top = Math.Min(startPoint.Y, endPoint.Y);
//            float bottom = Math.Max(startPoint.Y, endPoint.Y);

//            var newSelectedIndices = new HashSet<int>();

//            var panel = itemsControl.ItemsPanelRoot as ItemsWrapGrid;
//            if (panel != null)
//            {
//                foreach (var child in panel.Children)
//                {
//                    if (child is FrameworkElement container && container.Visibility == Visibility.Visible)
//                    {
//                        int index = itemsControl.IndexFromContainer(container);
//                        if (index >= 0 && index < Items.Count)
//                        {
//                            var transform = container.TransformToVisual(itemsControl);
//                            var position = transform.TransformPoint(new Windows.Foundation.Point(0, 0));

//                            float itemLeft = (float)position.X;
//                            float itemTop = (float)position.Y;
//                            float itemRight = itemLeft + (float)container.ActualWidth;
//                            float itemBottom = itemTop + (float)container.ActualHeight;

//                            bool intersects = itemRight > left && itemLeft < right &&
//                                              itemBottom > top && itemTop < bottom;

//                            if (intersects)
//                            {
//                                newSelectedIndices.Add(index);
//                            }
//                            else if (!_isCtrlPressed && applyImmediately)
//                            {
//                                var item = Items[index];
//                                if (itemsControl.SelectedItems.Contains(item))
//                                {
//                                    itemsControl.SelectedItems.Remove(item);
//                                }
//                            }
//                        }
//                    }
//                }
//            }

//            if (applyImmediately)
//            {
//                int added = 0;
//                foreach (int index in newSelectedIndices)
//                {
//                    if (index >= 0 && index < Items.Count)
//                    {
//                        var item = Items[index];
//                        if (!itemsControl.SelectedItems.Contains(item))
//                        {
//                            itemsControl.SelectedItems.Add(item);
//                            added++;
//                        }
//                    }
//                }
//                Debug.WriteLine($"[{PanelId}] [PerformRectangleSelection] Added {added} items to selection");
//                UpdateSelectionVisual(itemsControl);
//            }
//            else
//            {
//                _tempSelectedIndices = newSelectedIndices;
//                Debug.WriteLine($"[{PanelId}] [PerformRectangleSelection] Stored {newSelectedIndices.Count} temporary indices");
//            }
//        }

//        #endregion

//        #region PanelManager и навигация

//        public void SetPanelManager(PanelManager panelManager)
//        {
//            Debug.WriteLine($"[{PanelId}] [SetPanelManager] Setting PanelManager, CurrentPath={panelManager?.CurrentPath}, IconSize={panelManager?.State?.IconSize}");
//            if (PanelManager != null)
//            {
//                PanelManager.NavigationChanged -= OnPanelNavigationChanged;
//            }

//            _fileSystemService.ClearPanelCache(PanelId);
//            PanelManager = panelManager;

//            if (PanelManager != null)
//            {
//                PanelManager.NavigationChanged += OnPanelNavigationChanged;
//                SetIconSize(PanelManager.State.IconSize);
//            }
//        }

//        private async void OnPanelNavigationChanged(object sender, EventArgs e)
//        {
//            Debug.WriteLine($"[{PanelId}] [OnPanelNavigationChanged] Entering, IsLoading={_isLoading}, CurrentPath={PanelManager?.CurrentPath}, _currentLoadedPath={_currentLoadedPath}");
//            if (_isLoading) return;

//            if (PanelManager != null && PanelManager.CurrentPath != _currentLoadedPath)
//            {
//                await Task.Delay(100);

//                if (PanelManager.CurrentPath != _currentLoadedPath)
//                {
//                    Debug.WriteLine($"[{PanelId}] [OnPanelNavigationChanged] Loading path: {PanelManager.CurrentPath}");
//                    await LoadPathContents(PanelManager.CurrentPath);
//                }
//            }
//        }

//        private void OnNavigationChanged()
//        {
//            Debug.WriteLine($"[{PanelId}] [OnNavigationChanged] Raising NavigationChanged event");
//            NavigationChanged?.Invoke(this, EventArgs.Empty);
//        }

//        #endregion

//        #region Загрузка и инициализация

//        private async void OnLoaded(object sender, RoutedEventArgs e)
//        {
//            Debug.WriteLine($"[{PanelId}] [OnLoaded] Entering, SelectedSize={_selectedSize}, PanelManager.CurrentPath={PanelManager?.CurrentPath}");
//            if (string.IsNullOrEmpty(_selectedSize))
//            {
//                _selectedSize = "Medium";
//                Debug.WriteLine($"[{PanelId}] [OnLoaded] Using default size: Medium");
//            }

//            CalculateItemDimensions();

//            await Task.Delay(50);

//            UpdateAllTiles();
//            UpdateItemsControlLayout();

//            if (PanelManager != null && !string.IsNullOrEmpty(PanelManager.CurrentPath))
//            {
//                Debug.WriteLine($"[{PanelId}] [OnLoaded] Loading contents from PanelManager path: {PanelManager.CurrentPath}");
//                await LoadPathContents(PanelManager.CurrentPath);
//                _isInitialized = true;
//            }
//            else if (!_isInitialized)
//            {
//                Debug.WriteLine($"[{PanelId}] [OnLoaded] Loading initial content (MyComputer)");
//                LoadInitialContent();
//                _isInitialized = true;
//            }
//            Debug.WriteLine($"[{PanelId}] [OnLoaded] Completed");
//        }

//        #endregion

//        #region Размеры иконок

//        public void SetIconSize(string size)
//        {
//            Debug.WriteLine($"[{PanelId}] [SetIconSize] Setting size from '{_selectedSize}' to '{size}'");
//            _selectedSize = size;

//            if (PanelManager != null)
//            {
//                PanelManager.UpdateState(state => state.IconSize = size);
//                Debug.WriteLine($"[{PanelId}] [SetIconSize] Updated PanelManager state");
//            }

//            CalculateItemDimensions();
//            UpdateAllTiles();
//            UpdateItemsControlLayout();
//            ItemsListView.UpdateLayout();
//            ItemsGridView.UpdateLayout();
//            Debug.WriteLine($"[{PanelId}] [SetIconSize] Completed, ItemWidth={ItemWidth}, ItemHeight={ItemHeight}");
//        }

//        private void CalculateItemDimensions()
//        {
//            Debug.WriteLine($"[{PanelId}] [CalculateItemDimensions] Calculating for size '{_selectedSize}'");
//            var sizeParams = SizeManagerTile.GetSize(_selectedSize);

//            string viewType = _selectedSize.Split(' ').FirstOrDefault()?.ToLower() ?? "";

//            switch (viewType)
//            {
//                case "icons":
//                    ItemWidth = Math.Max(sizeParams.Width + 20, MinimumItemWidth);
//                    ItemHeight = Math.Max(sizeParams.Height + 25, MinimumItemHeight);
//                    break;

//                case "list":
//                case "compactlist":
//                case "table":
//                case "tiles":
//                    ItemWidth = Math.Max(sizeParams.Width + 20, MinimumItemWidth);
//                    ItemHeight = Math.Max(sizeParams.Height + 20, MinimumItemHeight);
//                    break;

//                default:
//                    ItemWidth = Math.Max(sizeParams.Width + 20, MinimumItemWidth);
//                    ItemHeight = Math.Max(sizeParams.Height + 25, MinimumItemHeight);
//                    break;
//            }
//            Debug.WriteLine($"[{PanelId}] [CalculateItemDimensions] Result: Width={ItemWidth}, Height={ItemHeight}");
//        }
//        #endregion

//        #region Обновление отображения элементов

//        private void UpdateAllTiles()
//        {
//            Debug.WriteLine($"[{PanelId}] [UpdateAllTiles] Updating all tiles");
//            UpdateTilesInControl(ItemsListView);
//            UpdateTilesInControl(ItemsGridView);
//        }

//        private void UpdateTilesInControl(ListViewBase itemsControl)
//        {
//            foreach (var item in itemsControl.Items)
//            {
//                var container = GetContainerFromItem(itemsControl, item);
//                if (container != null)
//                {
//                    var tile = GetContentTemplateRootFromContainer(container);
//                    if (tile is BaseTileControl baseTile)
//                    {
//                        baseTile.UpdateSize(_selectedSize);
//                    }
//                }
//            }
//        }

//        private FrameworkElement GetContainerFromItem(ListViewBase itemsControl, object item)
//        {
//            if (itemsControl is ListView listView)
//            {
//                return listView.ContainerFromItem(item) as FrameworkElement;
//            }
//            else if (itemsControl is GridView gridView)
//            {
//                return gridView.ContainerFromItem(item) as FrameworkElement;
//            }
//            return null;
//        }

//        private FrameworkElement GetContentTemplateRootFromContainer(FrameworkElement container)
//        {
//            if (container is ListViewItem listViewItem)
//            {
//                return listViewItem.ContentTemplateRoot as FrameworkElement;
//            }
//            else if (container is GridViewItem gridViewItem)
//            {
//                return gridViewItem.ContentTemplateRoot as FrameworkElement;
//            }
//            return null;
//        }

//        private void ItemsControl_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
//        {
//            if (args.Phase != 0) return;

//            BaseTileControl tile = null;
//            if (args.ItemContainer is ListViewItem listViewItem)
//                tile = listViewItem.ContentTemplateRoot as BaseTileControl;
//            else if (args.ItemContainer is GridViewItem gridViewItem)
//                tile = gridViewItem.ContentTemplateRoot as BaseTileControl;

//            if (tile != null)
//            {
//                tile.UpdateSize(_selectedSize);
//                tile.EditStateChanged -= OnTileEditStateChanged;
//                tile.EditStateChanged += OnTileEditStateChanged;

//                // Подписываемся на новое событие EditCompleted
//                tile.EditCompleted -= OnTileEditCompleted;
//                tile.EditCompleted += OnTileEditCompleted;

//                Debug.WriteLine($"[{PanelId}] [ContainerContentChanging] Tile updated and events subscribed");
//            }
//        }

//        private async void OnTileEditStateChanged(object sender, bool isEditing)
//        {
//            Debug.WriteLine($"[{PanelId}] [OnTileEditStateChanged] isEditing={isEditing}, sender={sender.GetType().Name}");

//            if (!isEditing && sender is BaseTileControl tile)
//            {
//                // Если мы в режиме множественного переименования – не восстанавливаем выделение
//                if (_isMultiRenameMode)
//                {
//                    Debug.WriteLine($"[{PanelId}] [OnTileEditStateChanged] In multi-rename mode, skipping selection restore");
//                    return;
//                }

//                // Если недавно был клик на пустое место – не восстанавливаем выделение
//                if ((DateTime.Now - _lastEmptySpaceClickTime).TotalMilliseconds < EmptySpaceClickCooldownMs)
//                {
//                    Debug.WriteLine($"[{PanelId}] [OnTileEditStateChanged] Ignoring selection restore due to recent empty space click ({(DateTime.Now - _lastEmptySpaceClickTime).TotalMilliseconds}ms < {EmptySpaceClickCooldownMs}ms)");
//                    return;
//                }

//                _lastEditEndTime = DateTime.Now;
//                StopHoverTimer();
//                _hoveredItem = null;

//                // Находим старую ViewModel
//                var oldEditedItem = tile.DataContext as ExplorerItemViewModel;

//                if (oldEditedItem != null && _currentItemsControl != null)
//                {
//                    // Ждём завершения перезагрузки коллекции
//                    await Task.Delay(100);

//                    // Ищем элемент в новой коллекции по пути файла
//                    var newEditedItem = Items.FirstOrDefault(item =>
//                        item.FilePath == oldEditedItem.FilePath ||
//                        item.Name == oldEditedItem.Name);

//                    if (newEditedItem != null)
//                    {
//                        Debug.WriteLine($"[{PanelId}] [OnTileEditStateChanged] Found new ViewModel for {newEditedItem.Name}");

//                        // Восстанавливаем выделение с новым экземпляром
//                        if (!_currentItemsControl.SelectedItems.Contains(newEditedItem))
//                        {
//                            _currentItemsControl.SelectedItems.Clear();
//                            _currentItemsControl.SelectedItems.Add(newEditedItem);
//                            _currentItemsControl.SelectedItem = newEditedItem;
//                            Debug.WriteLine($"[{PanelId}] [OnTileEditStateChanged] Restored selection for {newEditedItem.Name}");
//                        }

//                        // Принудительно прокручиваем к элементу
//                        _currentItemsControl.ScrollIntoView(newEditedItem, ScrollIntoViewAlignment.Default);

//                        // Даём время на создание контейнера
//                        await Task.Delay(50);

//                        // Фокусируем ItemsControl
//                        Debug.WriteLine($"[{PanelId}] [OnTileEditStateChanged] Focusing items control");
//                        _currentItemsControl.Focus(FocusState.Programmatic);
//                    }
//                    else
//                    {
//                        Debug.WriteLine($"[{PanelId}] [OnTileEditStateChanged] Could not find new ViewModel for {oldEditedItem.Name}");
//                    }
//                }
//            }
//        }

//        // НОВЫЙ МЕТОД: Обработка завершения редактирования для множественного переименования
//        private void OnTileEditCompleted(object sender, EditResult result)
//        {
//            Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} OnTileEditCompleted: Result={result}, IsMultiRenameMode={_isMultiRenameMode}");

//            if (!_isMultiRenameMode)
//            {
//                Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Not in multi-rename mode, ignoring");
//                return;
//            }

//            var tile = sender as BaseTileControl;
//            if (tile == null)
//            {
//                Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} ERROR: Sender is not BaseTileControl");
//                return;
//            }

//            var editedItem = tile.DataContext as ExplorerItemViewModel;
//            if (editedItem == null)
//            {
//                Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} ERROR: Could not get ViewModel from tile");
//                return;
//            }

//            // Текущий обрабатываемый путь
//            if (_multiRenamePaths == null || _multiRenameCurrentIndex < 0 || _multiRenameCurrentIndex >= _multiRenamePaths.Count)
//            {
//                Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} ERROR: Invalid multi-rename state - paths={_multiRenamePaths?.Count}, currentIndex={_multiRenameCurrentIndex}");
//                FinishMultiRename();
//                return;
//            }

//            string currentPath = _multiRenamePaths[_multiRenameCurrentIndex];
//            Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Current item: Path='{currentPath}', Name='{editedItem.Name}', Index={_multiRenameCurrentIndex}");

//            if (result == EditResult.Saved)
//            {
//                Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Item saved successfully, removing from list");

//                // Успешное сохранение – удаляем текущий путь из списка
//                _multiRenamePaths.RemoveAt(_multiRenameCurrentIndex);
//                // Индекс не увеличиваем, так как следующий элемент теперь на том же индексе

//                Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Remaining items: {_multiRenamePaths.Count}");

//                // Переходим к следующему элементу
//                BeginRenameForCurrentMultiItem();
//            }
//            else // Cancelled или Error
//            {
//                Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Item cancelled or error, finishing multi-rename sequence");
//                // Отмена или ошибка – прерываем всю последовательность
//                FinishMultiRename();
//            }
//        }

//        #endregion

//        #region Обновление выделения

//        private void UpdateSelectionVisual(ListViewBase itemsControl)
//        {
//            foreach (var item in itemsControl.SelectedItems)
//            {
//                var container = itemsControl.ContainerFromItem(item) as Control;
//                if (container != null)
//                {
//                    VisualStateManager.GoToState(container, "Selected", false);
//                }
//            }
//        }

//        private void SelectItemOnHover(ExplorerItemViewModel item)
//        {
//            Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] ========== START ==========");
//            Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Entering with item={(item != null ? $"'{item.Name}' (Hash={item.GetHashCode()})" : "null")}");
//            Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] AnyEditing={Items.Any(i => i.IsEditing)}");
//            Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Current time: {DateTime.Now:HH:mm:ss.fff}");
//            Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] _lastEditEndTime: {_lastEditEndTime:HH:mm:ss.fff}");
//            Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Time since last edit: {(DateTime.Now - _lastEditEndTime).TotalMilliseconds}ms");
//            Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] EditCooldownMs: {EditCooldownMs}ms");

//            // Защитный интервал после редактирования
//            if ((DateTime.Now - _lastEditEndTime).TotalMilliseconds < EditCooldownMs)
//            {
//                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] IGNORING - edit cooldown active");
//                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] ========== END (cooldown) ==========");
//                return;
//            }

//            if (Items.Any(i => i.IsEditing))
//            {
//                var editingItem = Items.FirstOrDefault(i => i.IsEditing);
//                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] IGNORING - another item is editing: {(editingItem != null ? editingItem.Name : "unknown")}");
//                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] ========== END (editing) ==========");
//                return;
//            }

//            if (Interlocked.CompareExchange(ref _hoverSelectionLock, 1, 0) != 0)
//            {
//                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] IGNORING - lock already taken");
//                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] ========== END (lock) ==========");
//                return;
//            }

//            Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Lock acquired successfully");

//            try
//            {
//                UpdateModifierKeyStateFromCore();
//                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Modifier keys after update: Ctrl={_isCtrlPressed}, Shift={_isShiftPressed}, Alt={_isAltPressed}");

//                if (item == null)
//                {
//                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] ABORT - item is null");
//                    return;
//                }

//                if (_isDragSelecting)
//                {
//                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] ABORT - drag selecting in progress");
//                    return;
//                }

//                if (_currentItemsControl == null)
//                {
//                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] ABORT - _currentItemsControl is null");
//                    return;
//                }

//                if (item.Name == "..")
//                {
//                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] SKIP - item is '..'");
//                    return;
//                }

//                // Проверяем, существует ли элемент в коллекции
//                int itemIndex = Items.IndexOf(item);
//                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Item index in collection: {itemIndex}");

//                if (itemIndex < 0)
//                {
//                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] WARNING - item not found in collection!");
//                    // Попробуем найти по пути или имени
//                    var foundItem = Items.FirstOrDefault(i => i.FilePath == item.FilePath || i.Name == item.Name);
//                    if (foundItem != null)
//                    {
//                        Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Found alternative item: '{foundItem.Name}' (Hash={foundItem.GetHashCode()})");
//                        item = foundItem;
//                        Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Using alternative item");
//                    }
//                    else
//                    {
//                        Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] No alternative found, aborting");
//                        return;
//                    }
//                }

//                // Проверка на зажатые клавиши-модификаторы
//                bool isCtrlPressed = _isCtrlPressed;
//                bool isShiftPressed = _isShiftPressed;

//                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Processing selection for '{item.Name}' (Hash={item.GetHashCode()})");
//                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover]   Shift={isShiftPressed}, Ctrl={isCtrlPressed}");
//                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover]   _shiftSelectionStartItem={(_shiftSelectionStartItem != null ? _shiftSelectionStartItem.Name : "null")}");

//                // Диагностика текущего выделения
//                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Current selection before change:");
//                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover]   SelectedItems.Count={_currentItemsControl.SelectedItems.Count}");
//                if (_currentItemsControl.SelectedItem is ExplorerItemViewModel currentSel)
//                {
//                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover]   SelectedItem: '{currentSel.Name}' (Hash={currentSel.GetHashCode()})");
//                }
//                else
//                {
//                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover]   SelectedItem: null");
//                }

//                // Обработка Shift+клик (выделение диапазона)
//                if (isShiftPressed)
//                {
//                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Shift+click mode");

//                    if (_shiftSelectionStartItem == null)
//                    {
//                        _shiftSelectionStartItem = _currentItemsControl.SelectedItem as ExplorerItemViewModel
//                                                    ?? Items.FirstOrDefault();
//                        Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Shift start item set to {_shiftSelectionStartItem?.Name}");
//                    }

//                    if (_shiftSelectionStartItem != null)
//                    {
//                        int startIndex = Items.IndexOf(_shiftSelectionStartItem);
//                        int endIndex = Items.IndexOf(item);
//                        Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Shift range: start={startIndex}, end={endIndex}");

//                        if (startIndex >= 0 && endIndex >= 0)
//                        {
//                            Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Calling SelectRange({startIndex}, {endIndex})");
//                            SelectRange(startIndex, endIndex);
//                        }
//                        else
//                        {
//                            Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Invalid indices, skipping");
//                        }
//                    }
//                }
//                // Обработка Ctrl+клик (добавление/удаление из выделения)
//                else if (isCtrlPressed)
//                {
//                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Ctrl+click mode");

//                    bool currentlySelected = _currentItemsControl.SelectedItems.Contains(item);
//                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Item currently selected: {currentlySelected}");

//                    if (currentlySelected)
//                    {
//                        _currentItemsControl.SelectedItems.Remove(item);
//                        Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Removed {item.Name} from selection");
//                    }
//                    else
//                    {
//                        _currentItemsControl.SelectedItems.Add(item);
//                        Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Added {item.Name} to selection");
//                    }

//                    // При Ctrl+клике не сбрасываем _shiftSelectionStartItem
//                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] _shiftSelectionStartItem unchanged: {(_shiftSelectionStartItem != null ? _shiftSelectionStartItem.Name : "null")}");
//                }
//                // Обычный клик (сброс выделения и выбор одного элемента)
//                else
//                {
//                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Normal hover selection mode");

//                    // Сохраняем старые значения для диагностики
//                    var oldSelectedItems = _currentItemsControl.SelectedItems.Cast<ExplorerItemViewModel>().ToList();
//                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Clearing {oldSelectedItems.Count} previously selected items");
//                    foreach (var oldItem in oldSelectedItems)
//                    {
//                        Debug.WriteLine($"[{PanelId}] [SelectItemOnHover]   - {oldItem.Name} (Hash={oldItem.GetHashCode()})");
//                    }

//                    _currentItemsControl.SelectedItems.Clear();
//                    _currentItemsControl.SelectedItems.Add(item);
//                    _currentItemsControl.SelectedItem = item;
//                    _shiftSelectionStartItem = item;

//                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Single selection set to {item.Name} (Hash={item.GetHashCode()})");
//                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] _shiftSelectionStartItem updated to {item.Name}");

//                    // Диагностика после изменения
//                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] New selection state:");
//                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover]   SelectedItems.Count={_currentItemsControl.SelectedItems.Count}");
//                    if (_currentItemsControl.SelectedItem is ExplorerItemViewModel newSel)
//                    {
//                        Debug.WriteLine($"[{PanelId}] [SelectItemOnHover]   SelectedItem: '{newSel.Name}' (Hash={newSel.GetHashCode()})");
//                        Debug.WriteLine($"[{PanelId}] [SelectItemOnHover]   SelectedItem matches hovered: {ReferenceEquals(newSel, item)}");
//                    }

//                    // Проверяем возможность редактирования
//                    bool canEdit = !item.IsEditing &&
//                                   !string.IsNullOrEmpty(item.FilePath) &&
//                                   !item.IsMyComputer &&
//                                   !item.IsTreeViewNode &&
//                                   !item.IsSpecialFolderNode &&
//                                   (File.Exists(item.FilePath) || Directory.Exists(item.FilePath));

//                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover]   CanEdit: {canEdit}");
//                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover]   SaveEditCommand can execute: {item.SaveEditCommand?.CanExecute(null)}");

//                    // ВАЖНО: Возвращаем фокус на ItemsControl после выделения
//                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Scheduling focus return to items control");
//                    _ = DispatcherQueue.TryEnqueue(() =>
//                    {
//                        Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Returning focus to items control (executing)");
//                        bool focusResult = _currentItemsControl.Focus(FocusState.Programmatic);
//                        Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Focus result: {focusResult}");

//                        // Проверяем, кто теперь имеет фокус
//                        var focusedElement = FocusManager.GetFocusedElement();
//                        Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Focused element: {focusedElement?.GetType().Name}");
//                    });
//                }

//                // Итоговая диагностика
//                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Final selection state:");
//                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover]   SelectedItems.Count={_currentItemsControl.SelectedItems.Count}");
//                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover]   SelectedItem: {(_currentItemsControl.SelectedItem != null ? ((ExplorerItemViewModel)_currentItemsControl.SelectedItem).Name : "null")}");
//                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover]   _shiftSelectionStartItem={(_shiftSelectionStartItem != null ? _shiftSelectionStartItem.Name : "null")}");
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] EXCEPTION: {ex.Message}");
//                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] StackTrace: {ex.StackTrace}");
//            }
//            finally
//            {
//                Interlocked.Exchange(ref _hoverSelectionLock, 0);
//                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Lock released");
//                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] ========== END ==========");
//            }
//        }
//        #endregion

//        #region Layout и обновление UI

//        private void UpdateItemsControlLayout()
//        {
//            Debug.WriteLine($"[{PanelId}] [UpdateItemsControlLayout] Updating layout for both controls");
//            UpdateControlLayout(ItemsListView);
//            UpdateControlLayout(ItemsGridView);
//        }

//        private void UpdateControlLayout(ListViewBase itemsControl)
//        {
//            if (itemsControl?.ItemsPanelRoot is ItemsWrapGrid wrapGrid)
//            {
//                double oldWidth = wrapGrid.ItemWidth, oldHeight = wrapGrid.ItemHeight;
//                wrapGrid.ItemWidth = ItemWidth;
//                wrapGrid.ItemHeight = ItemHeight;
//                Debug.WriteLine($"[{PanelId}] [UpdateControlLayout] {itemsControl.GetType().Name}: ItemWidth {oldWidth}->{ItemWidth}, ItemHeight {oldHeight}->{ItemHeight}");

//                if (itemsControl == ItemsListView)
//                {
//                    UpdateMaxRowsOrColumns();
//                    int oldMax = wrapGrid.MaximumRowsOrColumns;
//                    wrapGrid.MaximumRowsOrColumns = MaxRowsOrColumns;
//                    Debug.WriteLine($"[{PanelId}] [UpdateControlLayout] ListView MaxRowsOrColumns: {oldMax}->{MaxRowsOrColumns}");
//                }
//                else if (itemsControl == ItemsGridView)
//                {
//                    wrapGrid.MaximumRowsOrColumns = 24;
//                }

//                UpdateSelectionVisual(itemsControl);
//            }
//            else
//            {
//                Debug.WriteLine($"[{PanelId}] [UpdateControlLayout] ItemsPanelRoot is not ItemsWrapGrid or null");
//            }
//        }

//        private void UpdateMaxRowsOrColumns()
//        {
//            if (ItemHeight <= 0) return;

//            var actualHeight = ItemsListView.ActualHeight;
//            if (actualHeight > 0 && ItemHeight > 0)
//            {
//                int maxRows = Math.Max(1, (int)((actualHeight - 20) / ItemHeight));
//                MaxRowsOrColumns = maxRows;
//                Debug.WriteLine($"[{PanelId}] [UpdateMaxRowsOrColumns] Calculated: {maxRows} rows (Height={actualHeight}, ItemHeight={ItemHeight})");
//            }
//        }

//        private void ItemsControl_SizeChanged(object sender, SizeChangedEventArgs e)
//        {
//            var itemsControl = sender as ListViewBase;
//            Debug.WriteLine($"[{PanelId}] [ItemsControl_SizeChanged] {itemsControl.GetType().Name}: NewSize={e.NewSize.Width}x{e.NewSize.Height}");

//            if (itemsControl == ItemsListView)
//            {
//                UpdateMaxRowsOrColumns();
//            }
//            UpdateControlLayout(itemsControl);
//        }

//        private void UpdateUIForSelection()
//        {
//            int selectedCount = _currentItemsControl?.SelectedItems.Count ?? 0;
//            Debug.WriteLine($"[{PanelId}] [UpdateUIForSelection] {selectedCount} items selected");
//        }

//        #endregion

//        #region Обработка кликов

//        private async void ItemsControl_OnItemClick(object sender, ItemClickEventArgs e)
//        {
//            Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] ENTER");
//            var itemsControl = sender as ListViewBase;
//            if (itemsControl != _currentItemsControl)
//            {
//                Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Sender is not current control, ignoring");
//                return;
//            }

//            UpdateModifierKeyStateFromCore();

//            var now = DateTime.Now;
//            if ((now - _lastClickTime).TotalMilliseconds < 300)
//            {
//                Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Click throttled (too fast)");
//                return;
//            }
//            _lastClickTime = now;

//            Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] SingleClickOpenItem={SingleClickOpenItem}, Ctrl={_isCtrlPressed}, Shift={_isShiftPressed}");

//            if (e.ClickedItem is not ExplorerItemViewModel item)
//            {
//                Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] ClickedItem is not ExplorerItemViewModel");
//                return;
//            }

//            Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Clicked item: {item.Name}, Path={item.FilePath}");

//            int clickedIndex = Items.IndexOf(item);
//            if (clickedIndex < 0)
//            {
//                Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Clicked index not found in Items");
//                return;
//            }

//            bool isSingleClickMode = SingleClickOpenItem;
//            bool isCtrlPressed = _isCtrlPressed;
//            bool isShiftPressed = _isShiftPressed;

//            if (!isSingleClickMode)
//            {
//                if (_wasClickHandled)
//                {
//                    Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Click already handled, skipping");
//                    return;
//                }
//                _wasClickHandled = true;
//                Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] _wasClickHandled set to true");
//            }

//            if (isSingleClickMode)
//            {
//                if (isShiftPressed)
//                {
//                    Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Shift+click");
//                    if (_shiftSelectionStartItem == null)
//                    {
//                        _shiftSelectionStartItem = _currentItemsControl.SelectedItem as ExplorerItemViewModel;
//                        if (_shiftSelectionStartItem == null && Items.Count > 0)
//                        {
//                            _shiftSelectionStartItem = Items[0];
//                        }
//                        Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Shift start item set to {_shiftSelectionStartItem?.Name}");
//                    }

//                    if (_shiftSelectionStartItem != null)
//                    {
//                        int startIndex = Items.IndexOf(_shiftSelectionStartItem);
//                        int endIndex = clickedIndex;
//                        Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Selecting range {startIndex}-{endIndex}");
//                        SelectRange(startIndex, endIndex);
//                    }
//                }
//                else if (isCtrlPressed)
//                {
//                    Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Ctrl+click");
//                    if (_currentItemsControl.SelectedItems.Contains(item))
//                    {
//                        _currentItemsControl.SelectedItems.Remove(item);
//                        Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Removed from selection");
//                    }
//                    else
//                    {
//                        _currentItemsControl.SelectedItems.Add(item);
//                        Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Added to selection");
//                    }
//                }
//                else
//                {
//                    Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Single click open mode, selecting and opening");
//                    _currentItemsControl.SelectedItems.Clear();
//                    _currentItemsControl.SelectedItem = item;
//                    _shiftSelectionStartItem = item;

//                    await OpenItemByIndex(clickedIndex);
//                }
//            }
//            else
//            {
//                // Double-click mode (single click for selection only)
//                if (isShiftPressed)
//                {
//                    Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Shift+click (selection mode)");
//                    if (_shiftSelectionStartItem == null)
//                    {
//                        _shiftSelectionStartItem = _currentItemsControl.SelectedItem as ExplorerItemViewModel;
//                        if (_shiftSelectionStartItem == null && Items.Count > 0)
//                        {
//                            _shiftSelectionStartItem = Items[0];
//                        }
//                    }

//                    if (_shiftSelectionStartItem != null)
//                    {
//                        int startIndex = Items.IndexOf(_shiftSelectionStartItem);
//                        int endIndex = clickedIndex;
//                        SelectRange(startIndex, endIndex);
//                    }
//                }
//                else if (isCtrlPressed)
//                {
//                    Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Ctrl+click (selection mode)");
//                    if (_currentItemsControl.SelectedItems.Contains(item))
//                    {
//                        _currentItemsControl.SelectedItems.Remove(item);
//                    }
//                    else
//                    {
//                        _currentItemsControl.SelectedItems.Add(item);
//                    }
//                }
//                else
//                {
//                    Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Normal click (selection mode)");
//                    _currentItemsControl.SelectedItems.Clear();
//                    _currentItemsControl.SelectedItem = item;
//                    _shiftSelectionStartItem = item;

//                    var currentTime = DateTime.Now;
//                    bool isDoubleClick = (_lastClickedItem == item &&
//                                         (currentTime - _lastClickTime).TotalMilliseconds < 500);

//                    if (isDoubleClick)
//                    {
//                        Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Double-click detected, opening");
//                        await OpenItemByIndex(clickedIndex);
//                        _lastClickedItem = null;
//                        _lastClickTime = DateTime.MinValue;
//                    }
//                    else
//                    {
//                        Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Single click, setting up for possible double-click");
//                        _lastClickedItem = item;
//                        _lastClickTime = currentTime;

//                        _ = this.DispatcherQueue.EnqueueAsync(async () =>
//                        {
//                            await Task.Delay(500);
//                            _wasClickHandled = false;
//                            Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Delayed: _wasClickHandled reset to false");
//                        }, Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
//                    }
//                }
//            }

//            Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] EXIT");
//        }

//        #endregion

//        #region Загрузка содержимого

//        private async void LoadInitialContent()
//        {
//            Debug.WriteLine($"[{PanelId}] [LoadInitialContent] ENTER");

//            if (_currentLoadedPath == "MyComputer" && Items.Count > 0)
//            {
//                Debug.WriteLine($"[{PanelId}] [LoadInitialContent] MyComputer already loaded, skipping");
//                return;
//            }

//            CancelCurrentOperation();
//            Items.Clear();
//            UpdateItemsControlLayout();

//            try
//            {
//                Debug.WriteLine($"[{PanelId}] [LoadInitialContent] Calling _fileSystemService.LoadMyComputerAsync");
//                var items = await _fileSystemService.LoadMyComputerAsync(PanelId, _dummyHistory);
//                Debug.WriteLine($"[{PanelId}] [LoadInitialContent] Loaded {items.Count} items");
//                foreach (var item in items)
//                {
//                    Items.Add(item);
//                }
//                _currentLoadedPath = "MyComputer";
//                OnNavigationChanged();
//                Debug.WriteLine($"[{PanelId}] [LoadInitialContent] MyComputer loaded, items count: {Items.Count}");
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[{PanelId}] [LoadInitialContent] Error: {ex}");
//            }

//            Debug.WriteLine($"[{PanelId}] [LoadInitialContent] EXIT");
//        }

//        internal async Task LoadPathContents(string path)
//        {
//            Debug.WriteLine($"[{PanelId}] [LoadPathContents] ENTER, path='{path}'");
//            await _navigationSemaphore.WaitAsync();
//            try
//            {
//                Debug.WriteLine($"[{PanelId}] [LoadPathContents] Acquired semaphore, CurrentLoadedPath='{_currentLoadedPath}', IsLoading={_isLoading}");

//                if (_isLoading || _currentLoadedPath == path)
//                {
//                    Debug.WriteLine($"[{PanelId}] [LoadPathContents] Skipping - already loading or same path");
//                    return;
//                }

//                try
//                {
//                    _isLoading = true;
//                    switch (path)
//                    {
//                        case "MyComputer":
//                            Debug.WriteLine($"[{PanelId}] [LoadPathContents] Case: MyComputer");
//                            LoadInitialContent();
//                            _currentLoadedPath = path;
//                            break;

//                        case "Drives":
//                            Debug.WriteLine($"[{PanelId}] [LoadPathContents] Case: Drives");
//                            await LoadDrives();
//                            _currentLoadedPath = path;
//                            break;

//                        case string p when Directory.Exists(p):
//                            Debug.WriteLine($"[{PanelId}] [LoadPathContents] Case: Directory exists");
//                            await LoadFolderContents(path);
//                            _currentLoadedPath = path;

//                            if (PanelManager != null && PanelManager.CurrentPath != path)
//                            {
//                                PanelManager.NavigateTo(path);
//                                Debug.WriteLine($"[{PanelId}] [LoadPathContents] Called PanelManager.NavigateTo({path})");
//                            }
//                            break;

//                        default:
//                            Debug.WriteLine($"[{PanelId}] [LoadPathContents] Case: Default (path does not exist or is unknown)");
//                            if (_currentLoadedPath != "MyComputer")
//                            {
//                                LoadInitialContent();
//                                _currentLoadedPath = "MyComputer";
//                            }
//                            break;
//                    }
//                }
//                finally
//                {
//                    _isLoading = false;
//                }

//                OnNavigationChanged();
//            }
//            finally
//            {
//                _navigationSemaphore.Release();
//                Debug.WriteLine($"[{PanelId}] [LoadPathContents] Released semaphore");
//            }
//            Debug.WriteLine($"[{PanelId}] [LoadPathContents] EXIT");
//        }

//        private async Task LoadDrives()
//        {
//            Debug.WriteLine($"[{PanelId}] [LoadDrives] ENTER, CurrentLoadedPath='{_currentLoadedPath}', Items.Count={Items.Count}");

//            if (_currentLoadedPath == "Drives" && Items.Count > 1)
//            {
//                Debug.WriteLine($"[{PanelId}] [LoadDrives] Drives already loaded, skipping");
//                return;
//            }

//            CancelCurrentOperation();
//            Items.Clear();
//            UpdateItemsControlLayout();

//            try
//            {
//                Debug.WriteLine($"[{PanelId}] [LoadDrives] Calling _fileSystemService.LoadDrivesAsync");
//                var driveItems = await _fileSystemService.LoadDrivesAsync(_dummyHistory);
//                Debug.WriteLine($"[{PanelId}] [LoadDrives] Loaded {driveItems.Count} drives");

//                await this.DispatcherQueue.EnqueueAsync(() =>
//                {
//                    Items.Clear();
//                    foreach (var item in driveItems)
//                    {
//                        Items.Add(item);
//                    }
//                    UpdateItemsControlLayout();
//                    Debug.WriteLine($"[{PanelId}] [LoadDrives] Drives added to Items, count: {Items.Count}");
//                });
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[{PanelId}] [LoadDrives] ERROR: {ex}");

//                await this.DispatcherQueue.EnqueueAsync(() =>
//                {
//                    Items.Clear();
//                    UpdateItemsControlLayout();
//                });
//            }

//            OnNavigationChanged();
//            Debug.WriteLine($"[{PanelId}] [LoadDrives] EXIT");
//        }

//        private async Task LoadFolderContents(string folderPath)
//        {
//            Debug.WriteLine($"[{PanelId}] [LoadFolderContents] ENTER, folderPath='{folderPath}'");

//            if (string.IsNullOrEmpty(folderPath))
//            {
//                Debug.WriteLine($"[{PanelId}] [LoadFolderContents] Path is empty, returning");
//                return;
//            }

//            if (!Directory.Exists(folderPath))
//            {
//                Debug.WriteLine($"[{PanelId}] [LoadFolderContents] Directory does not exist: {folderPath}");
//                PanelManager?.GoBack();
//                return;
//            }

//            if (_currentLoadedPath == folderPath && Items.Count > 0)
//            {
//                Debug.WriteLine($"[{PanelId}] [LoadFolderContents] Folder already loaded, skipping");
//                return;
//            }

//            CancelCurrentOperation();

//            try
//            {
//                Debug.WriteLine($"[{PanelId}] [LoadFolderContents] Calling _fileSystemService.LoadFolderContentsAsync");
//                var folderItems = await _fileSystemService.LoadFolderContentsAsync(folderPath, _dummyHistory);
//                Debug.WriteLine($"[{PanelId}] [LoadFolderContents] Loaded {folderItems.Count} items");

//                await this.DispatcherQueue.EnqueueAsync(() =>
//                {
//                    Items.Clear();
//                    foreach (var item in folderItems)
//                    {
//                        Items.Add(item);
//                    }
//                    UpdateItemsControlLayout();
//                    Debug.WriteLine($"[{PanelId}] [LoadFolderContents] Items added, count: {Items.Count}");
//                });
//            }
//            catch (OperationCanceledException)
//            {
//                Debug.WriteLine($"[{PanelId}] [LoadFolderContents] Operation canceled");
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[{PanelId}] [LoadFolderContents] Error: {ex}");
//                PanelManager?.GoBack();
//            }

//            Debug.WriteLine($"[{PanelId}] [LoadFolderContents] EXIT");
//        }

//        private void CancelCurrentOperation()
//        {
//            Debug.WriteLine($"[{PanelId}] [CancelCurrentOperation] Cancelling current operations");
//            _currentOperationCts?.Cancel();
//            _currentOperationCts?.Dispose();
//            _currentOperationCts = new CancellationTokenSource();
//            _fileSystemService.CancelAllOperations();
//        }

//        #endregion

//        #region Обработка клавиатуры

//        private void ItemsControl_KeyDown(object sender, KeyRoutedEventArgs e)
//        {
//            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] ========== START ==========");
//            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] Key={e.Key}, OriginalKey={e.OriginalKey}");

//            var itemsControl = sender as ListViewBase;
//            if (itemsControl != _currentItemsControl)
//            {
//                Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] Sender is not current control, ignoring");
//                Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] ========== END (wrong control) ==========");
//                return;
//            }

//            UpdateModifierKeyState(e.Key, true);

//            bool isCtrlPressed = _isCtrlPressed;
//            bool isShiftPressed = _isShiftPressed;

//            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] Ctrl={isCtrlPressed}, Shift={isShiftPressed}, Alt={_isAltPressed}");
//            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] Items.Count={Items.Count}");
//            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] SelectedItems.Count={_currentItemsControl.SelectedItems.Count}");

//            if (_currentItemsControl.SelectedItem is ExplorerItemViewModel selectedItem)
//            {
//                Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] SelectedItem: '{selectedItem.Name}' (Index={_currentItemsControl.SelectedIndex}, Hash={selectedItem.GetHashCode()})");
//                Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] SelectedItem.IsEditing={selectedItem.IsEditing}");
//            }
//            else
//            {
//                Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] SelectedItem: null");
//            }

//            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] _hoveredItem={(_hoveredItem != null ? $"'{_hoveredItem.Name}' (Hash={_hoveredItem.GetHashCode()})" : "null")}");
//            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] _isDragSelecting={_isDragSelecting}");
//            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] _lastEditEndTime={_lastEditEndTime:HH:mm:ss.fff}, cooldown={EditCooldownMs}ms");
//            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] _shiftSelectionStartItem={(_shiftSelectionStartItem != null ? _shiftSelectionStartItem.Name : "null")}");
//            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] _isMultiRenameMode={_isMultiRenameMode}");

//            // Если мы в режиме множественного переименования, большинство клавиш игнорируем
//            if (_isMultiRenameMode)
//            {
//                Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] In multi-rename mode, most keys are ignored");
//                // Разрешаем только Escape для выхода из режима (обрабатывается в TextBox)
//                e.Handled = true;
//                Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] ========== END (multi-rename mode) ==========");
//                return;
//            }

//            switch (e.Key)
//            {
//                case VirtualKey.A when isCtrlPressed && !isShiftPressed:
//                    Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] Ctrl+A detected - selecting all");
//                    _currentItemsControl.SelectAll();
//                    e.Handled = true;
//                    Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] Ctrl+A executed, new SelectedItems.Count={_currentItemsControl.SelectedItems.Count}");
//                    break;

//                case VirtualKey.Space when isCtrlPressed:
//                    Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] Ctrl+Space detected - toggling selection");
//                    ToggleCurrentSelection();
//                    e.Handled = true;
//                    break;

//                case VirtualKey.Enter:
//                    Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] Enter detected");
//                    if (_currentItemsControl.SelectedItem != null)
//                    {
//                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] Enter: Opening selected item");
//                        OpenSelectedItem();
//                        e.Handled = true;
//                    }
//                    else
//                    {
//                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] Enter ignored - no selected item");
//                    }
//                    break;

//                case VirtualKey.F2:
//                    Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] F2 detected - checking conditions");

//                    // Если уже в режиме множественного переименования – игнорируем повторное нажатие
//                    if (_isMultiRenameMode)
//                    {
//                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] Already in multi-rename mode, ignoring F2");
//                        e.Handled = true;
//                        break;
//                    }

//                    // Диагностика перед принятием решения
//                    Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] F2 Decision Tree:");
//                    Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]   Condition 1: SelectedItems.Count == 1 ? {_currentItemsControl.SelectedItems.Count == 1}");
//                    Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]   Condition 2: _hoveredItem != null ? {_hoveredItem != null}");
//                    Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]   Condition 3: !_isDragSelecting ? {!_isDragSelecting}");

//                    if (_hoveredItem != null)
//                    {
//                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]   Hovered item exists in Items? {Items.Contains(_hoveredItem)}");
//                        int hoveredIndex = Items.IndexOf(_hoveredItem);
//                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]   Hovered item index: {hoveredIndex}");

//                        // Проверяем, совпадает ли хэш с элементом в коллекции
//                        if (hoveredIndex >= 0)
//                        {
//                            var itemFromCollection = Items[hoveredIndex];
//                            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]   Hovered hash: {_hoveredItem.GetHashCode()}, Collection item hash: {itemFromCollection.GetHashCode()}");
//                            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]   Same instance: {ReferenceEquals(_hoveredItem, itemFromCollection)}");
//                        }
//                    }

//                    // Если выделено более одного элемента
//                    if (_currentItemsControl.SelectedItems.Count > 1)
//                    {
//                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] F2: Multiple selection ({_currentItemsControl.SelectedItems.Count} items) - starting multi-rename");
//                        StartMultiRename();
//                        e.Handled = true;
//                        break;
//                    }

//                    if (_currentItemsControl.SelectedItems.Count == 1)
//                    {
//                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] F2: Case 1 - Rename selected item");
//                        if (_currentItemsControl.SelectedItem is ExplorerItemViewModel selItem)
//                        {
//                            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]   Selected item details:");
//                            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]     Name: '{selItem.Name}'");
//                            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]     Path: '{selItem.FilePath}'");
//                            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]     Index: {_currentItemsControl.SelectedIndex}");
//                            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]     Hash: {selItem.GetHashCode()}");
//                            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]     IsEditing: {selItem.IsEditing}");
//                            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]     CanEdit: {selItem.SaveEditCommand?.CanExecute(null)}");
//                        }
//                        RenameSelectedItem();
//                        e.Handled = true;
//                    }
//                    else if (_hoveredItem != null && !_isDragSelecting)
//                    {
//                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] F2: Case 2 - No selection, using hovered item");
//                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]   Hovered item details:");
//                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]     Name: '{_hoveredItem.Name}'");
//                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]     Path: '{_hoveredItem.FilePath}'");
//                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]     Hash: {_hoveredItem.GetHashCode()}");
//                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]     IsEditing: {_hoveredItem.IsEditing}");

//                        int hoveredIndex = Items.IndexOf(_hoveredItem);
//                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]     Index in collection: {hoveredIndex}");

//                        // Проверяем возможность редактирования
//                        bool canEdit = !_hoveredItem.IsEditing &&
//                                       !string.IsNullOrEmpty(_hoveredItem.FilePath) &&
//                                       !_hoveredItem.IsMyComputer &&
//                                       !_hoveredItem.IsTreeViewNode &&
//                                       !_hoveredItem.IsSpecialFolderNode &&
//                                       (File.Exists(_hoveredItem.FilePath) || Directory.Exists(_hoveredItem.FilePath));

//                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]     CanEdit: {canEdit}");
//                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]     SaveEditCommand can execute: {_hoveredItem.SaveEditCommand?.CanExecute(null)}");

//                        // Сохраняем старые значения для диагностики
//                        var oldSelectedItems = _currentItemsControl.SelectedItems.Cast<ExplorerItemViewModel>().ToList();
//                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]   Old selection count: {oldSelectedItems.Count}");
//                        foreach (var oldItem in oldSelectedItems)
//                        {
//                            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]     Old selected: '{oldItem.Name}' (Hash={oldItem.GetHashCode()})");
//                        }

//                        // Выделяем элемент под курсором
//                        _currentItemsControl.SelectedItems.Clear();
//                        _currentItemsControl.SelectedItems.Add(_hoveredItem);
//                        _currentItemsControl.SelectedItem = _hoveredItem;
//                        _shiftSelectionStartItem = _hoveredItem;

//                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]   New selection set to hovered item");
//                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]   New SelectedItems.Count: {_currentItemsControl.SelectedItems.Count}");

//                        // Проверяем, что выделение установилось правильно
//                        if (_currentItemsControl.SelectedItem is ExplorerItemViewModel newSelected)
//                        {
//                            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]   New SelectedItem: '{newSelected.Name}' (Hash={newSelected.GetHashCode()})");
//                            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]   Same as hovered: {ReferenceEquals(newSelected, _hoveredItem)}");
//                        }

//                        RenameSelectedItem();
//                        e.Handled = true;
//                    }
//                    else
//                    {
//                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] F2 ignored - conditions not met");
//                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]   SelectedItems.Count: {_currentItemsControl.SelectedItems.Count}");
//                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]   _hoveredItem: {(_hoveredItem != null ? _hoveredItem.Name : "null")}");
//                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]   _isDragSelecting: {_isDragSelecting}");
//                    }
//                    break;

//                case VirtualKey.Delete:
//                    Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] Delete detected");
//                    if (_currentItemsControl.SelectedItems.Count > 0)
//                    {
//                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] Delete: Deleting {_currentItemsControl.SelectedItems.Count} selected items");
//                        DeleteSelectedItems();
//                        e.Handled = true;
//                    }
//                    else
//                    {
//                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] Delete ignored - no selected items");
//                    }
//                    break;

//                case VirtualKey.Up:
//                case VirtualKey.Down:
//                case VirtualKey.Left:
//                case VirtualKey.Right:
//                    Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] Arrow key detected: {e.Key}");
//                    HandleArrowKeyNavigation(e.Key, isCtrlPressed, isShiftPressed);
//                    e.Handled = true;
//                    break;

//                case VirtualKey.Home:
//                case VirtualKey.End:
//                    Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] Home/End key detected: {e.Key}");
//                    HandleHomeEndNavigation(e.Key, isCtrlPressed, isShiftPressed);
//                    e.Handled = true;
//                    break;

//                case VirtualKey.PageUp:
//                case VirtualKey.PageDown:
//                    Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] PageUp/PageDown key detected: {e.Key}");
//                    HandlePageNavigation(e.Key, isCtrlPressed, isShiftPressed);
//                    e.Handled = true;
//                    break;

//                default:
//                    Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] Unhandled key: {e.Key}");
//                    break;
//            }

//            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] ========== END ==========");
//        }

//        private void ItemsControl_KeyUp(object sender, KeyRoutedEventArgs e)
//        {
//            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyUp] Key={e.Key}");
//            var itemsControl = sender as ListViewBase;
//            if (itemsControl != _currentItemsControl) return;

//            UpdateModifierKeyState(e.Key, false);
//        }

//        private void ItemsControl_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
//        {
//            Debug.WriteLine($"[{PanelId}] [ItemsControl_PreviewKeyDown] Key={e.Key}");
//        }

//        private void UpdateModifierKeyState(VirtualKey key, bool isPressed)
//        {
//            bool oldCtrl = _isCtrlPressed, oldShift = _isShiftPressed, oldAlt = _isAltPressed;
//            switch (key)
//            {
//                case VirtualKey.Control:
//                case VirtualKey.LeftControl:
//                case VirtualKey.RightControl:
//                    _isCtrlPressed = isPressed;
//                    break;
//                case VirtualKey.Shift:
//                case VirtualKey.LeftShift:
//                case VirtualKey.RightShift:
//                    _isShiftPressed = isPressed;
//                    break;
//                case VirtualKey.Menu:
//                case VirtualKey.LeftMenu:
//                case VirtualKey.RightMenu:
//                    _isAltPressed = isPressed;
//                    break;
//            }
//            if (oldCtrl != _isCtrlPressed || oldShift != _isShiftPressed || oldAlt != _isAltPressed)
//            {
//                Debug.WriteLine($"[{PanelId}] [UpdateModifierKeyState] Key={key}, Pressed={isPressed} => Ctrl={_isCtrlPressed}, Shift={_isShiftPressed}, Alt={_isAltPressed}");
//            }
//        }

//        #endregion

//        #region Навигация клавишами

//        private void HandleArrowKeyNavigation(VirtualKey key, bool isCtrlPressed, bool isShiftPressed)
//        {
//            Debug.WriteLine($"[{PanelId}] [HandleArrowKeyNavigation] Key={key}, Ctrl={isCtrlPressed}, Shift={isShiftPressed}");
//            if (_currentItemsControl == null) return;

//            int currentIndex = _currentItemsControl.SelectedIndex;
//            int newIndex = currentIndex;

//            if (_currentItemsControl == ItemsListView)
//            {
//                int itemsPerColumn = CalculateItemsPerColumnForListView();
//                Debug.WriteLine($"[{PanelId}] [HandleArrowKeyNavigation] ListView mode, itemsPerColumn={itemsPerColumn}");

//                switch (key)
//                {
//                    case VirtualKey.Up:
//                        newIndex = Math.Max(0, currentIndex - 1);
//                        break;
//                    case VirtualKey.Down:
//                        newIndex = Math.Min(Items.Count - 1, currentIndex + 1);
//                        break;
//                    case VirtualKey.Left:
//                        newIndex = Math.Max(0, currentIndex - itemsPerColumn);
//                        break;
//                    case VirtualKey.Right:
//                        newIndex = Math.Min(Items.Count - 1, currentIndex + itemsPerColumn);
//                        break;
//                }
//            }
//            else
//            {
//                int itemsPerRow = CalculateItemsPerRowForGridView();
//                Debug.WriteLine($"[{PanelId}] [HandleArrowKeyNavigation] GridView mode, itemsPerRow={itemsPerRow}");

//                switch (key)
//                {
//                    case VirtualKey.Up:
//                        newIndex = Math.Max(0, currentIndex - itemsPerRow);
//                        break;
//                    case VirtualKey.Down:
//                        newIndex = Math.Min(Items.Count - 1, currentIndex + itemsPerRow);
//                        break;
//                    case VirtualKey.Left:
//                        newIndex = Math.Max(0, currentIndex - 1);
//                        break;
//                    case VirtualKey.Right:
//                        newIndex = Math.Min(Items.Count - 1, currentIndex + 1);
//                        break;
//                }
//            }

//            if (newIndex != currentIndex && newIndex >= 0 && newIndex < Items.Count)
//            {
//                var newItem = Items[newIndex];
//                Debug.WriteLine($"[{PanelId}] [HandleArrowKeyNavigation] Moving from {currentIndex} to {newIndex}, item={newItem.Name}");

//                if (isShiftPressed)
//                {
//                    HandleShiftArrowSelection(newIndex);
//                }
//                else if (isCtrlPressed)
//                {
//                    _currentItemsControl.SelectedItem = newItem;
//                    _currentItemsControl.ScrollIntoView(newItem);
//                    Debug.WriteLine($"[{PanelId}] [HandleArrowKeyNavigation] Ctrl+arrow: set selected item");
//                }
//                else
//                {
//                    _currentItemsControl.SelectedItems.Clear();
//                    _currentItemsControl.SelectedItem = newItem;
//                    _currentItemsControl.ScrollIntoView(newItem);
//                    _shiftSelectionStartItem = newItem;
//                    Debug.WriteLine($"[{PanelId}] [HandleArrowKeyNavigation] Normal arrow: cleared and set selected item");
//                }
//            }
//            else
//            {
//                Debug.WriteLine($"[{PanelId}] [HandleArrowKeyNavigation] No movement (index unchanged or out of range)");
//            }
//        }

//        private void HandleHomeEndNavigation(VirtualKey key, bool isCtrlPressed, bool isShiftPressed)
//        {
//            Debug.WriteLine($"[{PanelId}] [HandleHomeEndNavigation] Key={key}, Ctrl={isCtrlPressed}, Shift={isShiftPressed}");
//            if (_currentItemsControl == null) return;

//            int newIndex = -1;

//            switch (key)
//            {
//                case VirtualKey.Home:
//                    newIndex = 0;
//                    break;
//                case VirtualKey.End:
//                    newIndex = Items.Count - 1;
//                    break;
//            }

//            if (newIndex >= 0 && newIndex < Items.Count)
//            {
//                var newItem = Items[newIndex];
//                Debug.WriteLine($"[{PanelId}] [HandleHomeEndNavigation] Moving to index {newIndex}, item={newItem.Name}");

//                if (isShiftPressed)
//                {
//                    HandleShiftRangeSelection(newIndex);
//                }
//                else if (isCtrlPressed)
//                {
//                    _currentItemsControl.SelectedItem = newItem;
//                    _currentItemsControl.ScrollIntoView(newItem);
//                }
//                else
//                {
//                    _currentItemsControl.SelectedItems.Clear();
//                    _currentItemsControl.SelectedItem = newItem;
//                    _currentItemsControl.ScrollIntoView(newItem);
//                }
//            }
//        }

//        private void HandlePageNavigation(VirtualKey key, bool isCtrlPressed, bool isShiftPressed)
//        {
//            Debug.WriteLine($"[{PanelId}] [HandlePageNavigation] Key={key}, Ctrl={isCtrlPressed}, Shift={isShiftPressed}");
//            if (_currentItemsControl == null) return;

//            int currentIndex = _currentItemsControl.SelectedIndex;
//            int itemsPerPage = CalculateItemsPerPage();
//            int newIndex = currentIndex;

//            switch (key)
//            {
//                case VirtualKey.PageUp:
//                    newIndex = Math.Max(0, currentIndex - itemsPerPage);
//                    break;
//                case VirtualKey.PageDown:
//                    newIndex = Math.Min(Items.Count - 1, currentIndex + itemsPerPage);
//                    break;
//            }

//            if (newIndex != currentIndex)
//            {
//                var newItem = Items[newIndex];
//                Debug.WriteLine($"[{PanelId}] [HandlePageNavigation] From {currentIndex} to {newIndex}, item={newItem.Name}");

//                if (isShiftPressed)
//                {
//                    HandleShiftRangeSelection(newIndex);
//                }
//                else
//                {
//                    _currentItemsControl.SelectedItems.Clear();
//                    _currentItemsControl.SelectedItem = newItem;
//                    _currentItemsControl.ScrollIntoView(newItem);
//                }
//            }
//        }

//        private int CalculateItemsPerColumnForListView()
//        {
//            return MaxRowsOrColumns;
//        }

//        private int CalculateItemsPerRowForGridView()
//        {
//            if (ItemWidth <= 0) return 1;

//            var grid = ItemsGridView.ItemsPanelRoot as ItemsWrapGrid;
//            if (grid != null && grid.ActualWidth > 0)
//            {
//                int perRow = (int)Math.Floor(grid.ActualWidth / ItemWidth);
//                Debug.WriteLine($"[{PanelId}] [CalculateItemsPerRowForGridView] Calculated: {perRow} (ActualWidth={grid.ActualWidth}, ItemWidth={ItemWidth})");
//                return perRow;
//            }

//            return 6;
//        }

//        private int CalculateItemsPerPage()
//        {
//            if (ItemHeight <= 0) return 1;

//            var itemsControl = _currentItemsControl;
//            var panel = itemsControl?.ItemsPanelRoot as FrameworkElement;
//            if (panel != null && panel.ActualHeight > 0)
//            {
//                int rowsPerPage = (int)Math.Floor(panel.ActualHeight / ItemHeight);
//                int perPage;
//                if (itemsControl == ItemsListView)
//                {
//                    perPage = rowsPerPage * CalculateItemsPerColumnForListView();
//                }
//                else
//                {
//                    perPage = rowsPerPage * CalculateItemsPerRowForGridView();
//                }
//                Debug.WriteLine($"[{PanelId}] [CalculateItemsPerPage] rowsPerPage={rowsPerPage}, perPage={perPage}");
//                return perPage;
//            }

//            return 20;
//        }

//        #endregion

//        #region Выделение элементов

//        private void HandleShiftArrowSelection(int newIndex)
//        {
//            Debug.WriteLine($"[{PanelId}] [HandleShiftArrowSelection] newIndex={newIndex}");
//            if (_shiftSelectionStartItem == null)
//            {
//                _shiftSelectionStartItem = _currentItemsControl.SelectedItem as ExplorerItemViewModel;
//                if (_shiftSelectionStartItem == null && Items.Count > 0)
//                {
//                    _shiftSelectionStartItem = Items[0];
//                }
//                Debug.WriteLine($"[{PanelId}] [HandleShiftArrowSelection] Shift start set to {_shiftSelectionStartItem?.Name}");
//            }

//            if (_shiftSelectionStartItem != null)
//            {
//                int startIndex = Items.IndexOf(_shiftSelectionStartItem);
//                int endIndex = newIndex;
//                Debug.WriteLine($"[{PanelId}] [HandleShiftArrowSelection] Selecting range {startIndex}-{endIndex}");
//                SelectRange(startIndex, endIndex);
//            }
//        }

//        private void HandleShiftRangeSelection(int newIndex)
//        {
//            int currentIndex = _currentItemsControl.SelectedIndex;
//            Debug.WriteLine($"[{PanelId}] [HandleShiftRangeSelection] currentIndex={currentIndex}, newIndex={newIndex}");
//            if (currentIndex >= 0)
//            {
//                SelectRange(currentIndex, newIndex);
//            }
//        }

//        private void SelectRange(int startIndex, int endIndex)
//        {
//            Debug.WriteLine($"[{PanelId}] [SelectRange] start={startIndex}, end={endIndex}");
//            if (startIndex < 0 || endIndex < 0 || startIndex >= Items.Count || endIndex >= Items.Count)
//            {
//                Debug.WriteLine($"[{PanelId}] [SelectRange] Invalid indices, returning");
//                return;
//            }

//            int minIndex = Math.Min(startIndex, endIndex);
//            int maxIndex = Math.Max(startIndex, endIndex);

//            if (!_isCtrlPressed)
//            {
//                _currentItemsControl.SelectedItems.Clear();
//                Debug.WriteLine($"[{PanelId}] [SelectRange] Cleared selection (Ctrl not pressed)");
//            }

//            int added = 0;
//            for (int i = minIndex; i <= maxIndex; i++)
//            {
//                if (!_currentItemsControl.SelectedItems.Contains(Items[i]))
//                {
//                    _currentItemsControl.SelectedItems.Add(Items[i]);
//                    added++;
//                }
//            }
//            Debug.WriteLine($"[{PanelId}] [SelectRange] Added {added} items");

//            _currentItemsControl.ScrollIntoView(Items[endIndex]);
//        }

//        private void ToggleCurrentSelection()
//        {
//            Debug.WriteLine($"[{PanelId}] [ToggleCurrentSelection] Entering");
//            if (_currentItemsControl.SelectedItem is ExplorerItemViewModel currentItem)
//            {
//                if (_currentItemsControl.SelectedItems.Contains(currentItem))
//                {
//                    _currentItemsControl.SelectedItems.Remove(currentItem);
//                    Debug.WriteLine($"[{PanelId}] [ToggleCurrentSelection] Removed {currentItem.Name} from selection");
//                }
//                else
//                {
//                    _currentItemsControl.SelectedItems.Add(currentItem);
//                    Debug.WriteLine($"[{PanelId}] [ToggleCurrentSelection] Added {currentItem.Name} to selection");
//                }
//            }
//        }

//        #endregion

//        #region Обработка двойного клика

//        private async void ItemsControl_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
//        {
//            Debug.WriteLine($"[{PanelId}] [ItemsControl_DoubleTapped] Entering");
//            var itemsControl = sender as ListViewBase;
//            if (itemsControl != _currentItemsControl) return;

//            if (SingleClickOpenItem)
//            {
//                Debug.WriteLine($"[{PanelId}] [ItemsControl_DoubleTapped] SingleClickOpenItem=true, ignoring double-tap");
//                return;
//            }

//            var element = e.OriginalSource as FrameworkElement;
//            while (element != null && element.DataContext as ExplorerItemViewModel == null)
//            {
//                element = VisualTreeHelper.GetParent(element) as FrameworkElement;
//            }

//            if (element?.DataContext is ExplorerItemViewModel item)
//            {
//                int index = Items.IndexOf(item);
//                Debug.WriteLine($"[{PanelId}] [ItemsControl_DoubleTapped] Double-tapped on {item.Name}, index={index}");
//                if (index >= 0)
//                {
//                    await OpenItemByIndex(index);
//                }
//            }
//        }

//        #endregion

//        #region Обработка выделения

//        private void ItemsControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            var itemsControl = sender as ListViewBase;
//            if (itemsControl != _currentItemsControl) return;

//            Debug.WriteLine($"[{PanelId}] [ItemsControl_SelectionChanged] Selection changed. New count: {itemsControl.SelectedItems.Count}");

//            foreach (ExplorerItemViewModel addedItem in e.AddedItems)
//            {
//                Debug.WriteLine($"[{PanelId}] [ItemsControl_SelectionChanged] [+] Added: {addedItem?.Name} (Hash={addedItem?.GetHashCode()})");
//            }

//            foreach (ExplorerItemViewModel removedItem in e.RemovedItems)
//            {
//                Debug.WriteLine($"[{PanelId}] [ItemsControl_SelectionChanged] [-] Removed: {removedItem?.Name} (Hash={removedItem?.GetHashCode()})");
//                if (removedItem?.Name == "..")
//                {
//                    StopHoverTimer();
//                    _hoveredItem = null;
//                }
//            }

//            UpdateUIForSelection();
//        }

//        #endregion

//        #region Операции с элементами

//        private async Task OpenItem(ExplorerItemViewModel item)
//        {
//            Debug.WriteLine($"[{PanelId}] [OpenItem] Entering with item={item?.Name}, Path={item?.FilePath}");
//            try
//            {
//                _shiftSelectionStartItem = null;
//                _lastClickedItem = null;
//                _lastClickTime = DateTime.MinValue;
//                _wasClickHandled = false;

//                if (item.Name == "..")
//                {
//                    Debug.WriteLine($"[{PanelId}] [OpenItem] Item is '..', going back");
//                    PanelManager?.GoBack();
//                    return;
//                }

//                if (item.FilePath == "Drives" ||
//                    item.FilePath == "MyComputer" ||
//                    Directory.Exists(item.FilePath))
//                {
//                    Debug.WriteLine($"[{PanelId}] [OpenItem] Navigating to folder: {item.FilePath}");
//                    await LoadPathContents(item.FilePath);
//                    PanelManager?.NavigateTo(item.FilePath);
//                }
//                else if (File.Exists(item.FilePath))
//                {
//                    Debug.WriteLine($"[{PanelId}] [OpenItem] Opening file: {item.FilePath}");
//                    // Здесь можно добавить логику открытия файла
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[{PanelId}] [OpenItem] Error: {ex}");
//            }
//        }

//        private async Task OpenItemByIndex(int index)
//        {
//            Debug.WriteLine($"[{PanelId}] [OpenItemByIndex] ENTER, index={index}, SingleClickOpenItem={SingleClickOpenItem}");

//            if (_isProcessingBackNavigation)
//            {
//                Debug.WriteLine($"[{PanelId}] [OpenItemByIndex] Already processing back navigation, skipping");
//                return;
//            }

//            if (index < 0 || index >= Items.Count)
//            {
//                Debug.WriteLine($"[{PanelId}] [OpenItemByIndex] Invalid index, returning");
//                return;
//            }

//            var item = Items[index];
//            Debug.WriteLine($"[{PanelId}] [OpenItemByIndex] Item: {item.Name}, Path: {item.FilePath}, IsEditing={item.IsEditing}");

//            if (_currentItemsControl.SelectedItem is ExplorerItemViewModel selectedItem && selectedItem.IsEditing)
//            {
//                Debug.WriteLine($"[{PanelId}] [OpenItemByIndex] Cancelling edit mode before opening");
//                var container = GetContainerFromItem(_currentItemsControl, selectedItem);
//                var tile = GetContentTemplateRootFromContainer(container) as BaseTileControl;
//                if (tile != null && tile.IsEditing)
//                {
//                    tile.CancelEditing();
//                    Debug.WriteLine($"[{PanelId}] [OpenItemByIndex] CancelEditing called");
//                }
//            }

//            if (item.Name == "..")
//            {
//                _isProcessingBackNavigation = true;
//                try
//                {
//                    Debug.WriteLine($"[{PanelId}] [OpenItemByIndex] Processing '..' navigation, PanelManager.CurrentPath={PanelManager?.CurrentPath}");
//                    PanelManager?.GoBack();

//                    _shiftSelectionStartItem = null;
//                    _lastClickedItem = null;
//                    _lastClickTime = DateTime.MinValue;
//                    _wasClickHandled = false;
//                    StopHoverTimer();
//                    _hoveredItem = null;

//                    await Task.Delay(50);

//                    Debug.WriteLine($"[{PanelId}] [OpenItemByIndex] After back navigation, PanelManager.CurrentPath={PanelManager?.CurrentPath}");
//                }
//                finally
//                {
//                    _isProcessingBackNavigation = false;
//                }
//                return;
//            }

//            _shiftSelectionStartItem = null;
//            _lastClickedItem = null;
//            _lastClickTime = DateTime.MinValue;
//            _wasClickHandled = false;

//            string path = item.FilePath;

//            if (path == "Drives" || path == "MyComputer" || Directory.Exists(path))
//            {
//                Debug.WriteLine($"[{PanelId}] [OpenItemByIndex] Loading path: {path}");
//                await LoadPathContents(path);
//                PanelManager?.NavigateTo(path);
//            }
//            else if (File.Exists(path))
//            {
//                Debug.WriteLine($"[{PanelId}] [OpenItemByIndex] Opening file: {path}");
//            }

//            Debug.WriteLine($"[{PanelId}] [OpenItemByIndex] EXIT");
//        }

//        private async void OpenSelectedItem()
//        {
//            Debug.WriteLine($"[{PanelId}] [OpenSelectedItem] Entering");
//            if (_currentItemsControl.SelectedItem is ExplorerItemViewModel selectedItem)
//            {
//                await OpenItem(selectedItem);
//            }
//        }

//        private void RenameSelectedItem()
//        {
//            Debug.WriteLine($"[{PanelId}] [RenameSelectedItem] ENTER");

//            try
//            {
//                if (_currentItemsControl.SelectedItem is ExplorerItemViewModel selectedItem)
//                {
//                    int selectedIndex = _currentItemsControl.SelectedIndex;
//                    Debug.WriteLine($"[{PanelId}] [RenameSelectedItem] Selected item: '{selectedItem.Name}' (Index={selectedIndex}, Hash={selectedItem.GetHashCode()})");
//                    Debug.WriteLine($"[{PanelId}] [RenameSelectedItem] IsEditing={selectedItem.IsEditing}, EditRequested={selectedItem.EditRequested}");

//                    if (selectedItem.IsEditing)
//                    {
//                        Debug.WriteLine($"[{PanelId}] [RenameSelectedItem] Item is already in edit mode, returning");
//                        return;
//                    }

//                    // Получаем контейнер для выбранного элемента
//                    var container = GetContainerFromItem(_currentItemsControl, selectedItem);
//                    Debug.WriteLine($"[{PanelId}] [RenameSelectedItem] Container found immediately: {container != null}");

//                    if (container != null)
//                    {
//                        var tile = GetContentTemplateRootFromContainer(container) as BaseTileControl;
//                        if (tile != null)
//                        {
//                            Debug.WriteLine($"[{PanelId}] [RenameSelectedItem] Tile found, CanEdit={tile.CanEdit}");
//                            if (tile.CanEdit)
//                            {
//                                Debug.WriteLine($"[{PanelId}] [RenameSelectedItem] Calling StartEditing on tile");
//                                tile.StartEditing();
//                            }
//                            else
//                            {
//                                Debug.WriteLine($"[{PanelId}] [RenameSelectedItem] Tile does not support editing");
//                            }
//                        }
//                        else
//                        {
//                            Debug.WriteLine($"[{PanelId}] [RenameSelectedItem] ContentTemplateRoot is not BaseTileControl");
//                        }
//                    }
//                    else
//                    {
//                        Debug.WriteLine($"[{PanelId}] [RenameSelectedItem] Container not found, scrolling into view and retrying...");
//                        _currentItemsControl.ScrollIntoView(selectedItem);

//                        _ = this.DispatcherQueue.EnqueueAsync(async () =>
//                        {
//                            await Task.Delay(100);
//                            container = GetContainerFromItem(_currentItemsControl, selectedItem);
//                            Debug.WriteLine($"[{PanelId}] [RenameSelectedItem] Retry: container found = {container != null}");
//                            if (container != null)
//                            {
//                                var tile = GetContentTemplateRootFromContainer(container) as BaseTileControl;
//                                if (tile != null && tile.CanEdit)
//                                {
//                                    Debug.WriteLine($"[{PanelId}] [RenameSelectedItem] Retry successful, calling StartEditing");
//                                    tile.StartEditing();
//                                }
//                                else
//                                {
//                                    Debug.WriteLine($"[{PanelId}] [RenameSelectedItem] Retry: tile not found or cannot edit");
//                                }
//                            }
//                        }, Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal);
//                    }
//                }
//                else
//                {
//                    Debug.WriteLine($"[{PanelId}] [RenameSelectedItem] No item selected");
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[{PanelId}] [RenameSelectedItem] Exception: {ex}");
//            }

//            Debug.WriteLine($"[{PanelId}] [RenameSelectedItem] EXIT");
//        }

//        // НОВЫЙ МЕТОД: Начать множественное переименование
//        private void StartMultiRename()
//        {
//            Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} ========== START MULTI-RENAME ==========");
//            Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Selected items count: {_currentItemsControl.SelectedItems.Count}");

//            // Сохраняем исходные пути выделенных элементов в порядке их следования в коллекции
//            _multiRenamePaths = _currentItemsControl.SelectedItems
//                .Cast<ExplorerItemViewModel>()
//                .OrderBy(item => Items.IndexOf(item))
//                .Select(item => item.FilePath)
//                .ToList();

//            Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Items in queue:");
//            for (int i = 0; i < _multiRenamePaths.Count; i++)
//            {
//                var item = Items.FirstOrDefault(x => x.FilePath == _multiRenamePaths[i]);
//                Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix}   [{i}] {item?.Name} (Path: {_multiRenamePaths[i]})");
//            }

//            _multiRenameCurrentIndex = 0;
//            _isMultiRenameMode = true;

//            // Отключаем таймер наведения, чтобы не мешал
//            StopHoverTimer();
//            _hoveredItem = null;

//            Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Starting with first item");
//            BeginRenameForCurrentMultiItem();
//            Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} ========== END START MULTI-RENAME ==========");
//        }

//        // НОВЫЙ МЕТОД: Начать редактирование для текущего элемента в очереди
//        private void BeginRenameForCurrentMultiItem()
//        {
//            Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} BeginRenameForCurrentMultiItem: Index={_multiRenameCurrentIndex}, Total={_multiRenamePaths?.Count}");

//            if (!_isMultiRenameMode || _multiRenamePaths == null)
//            {
//                Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Not in multi-rename mode or paths null, finishing");
//                FinishMultiRename();
//                return;
//            }

//            if (_multiRenameCurrentIndex >= _multiRenamePaths.Count)
//            {
//                Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Reached end of list, finishing multi-rename");
//                FinishMultiRename();
//                return;
//            }

//            string targetPath = _multiRenamePaths[_multiRenameCurrentIndex];
//            Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Looking for item with path: '{targetPath}'");

//            var item = Items.FirstOrDefault(x => x.FilePath == targetPath);
//            if (item == null)
//            {
//                Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} WARNING: Item with path '{targetPath}' not found in collection, skipping");
//                _multiRenameCurrentIndex++;
//                BeginRenameForCurrentMultiItem();
//                return;
//            }

//            Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Found item: '{item.Name}' (Hash={item.GetHashCode()}), Index in collection: {Items.IndexOf(item)}");

//            // Делаем этот элемент текущим (для визуального выделения и прокрутки)
//            _currentItemsControl.SelectedItems.Clear();
//            _currentItemsControl.SelectedItems.Add(item);
//            _currentItemsControl.SelectedItem = item;

//            // Пытаемся запустить редактирование
//            var container = GetContainerFromItem(_currentItemsControl, item);
//            if (container != null)
//            {
//                Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Container found immediately");
//                var tile = GetContentTemplateRootFromContainer(container) as BaseTileControl;
//                if (tile != null)
//                {
//                    Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Tile found, starting edit");
//                    tile.StartEditing();
//                }
//                else
//                {
//                    Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} ERROR: Tile is null");
//                    _multiRenameCurrentIndex++;
//                    BeginRenameForCurrentMultiItem();
//                }
//            }
//            else
//            {
//                Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Container not found, scrolling into view and retrying...");
//                _currentItemsControl.ScrollIntoView(item);

//                _ = this.DispatcherQueue.EnqueueAsync(async () =>
//                {
//                    await Task.Delay(100);
//                    container = GetContainerFromItem(_currentItemsControl, item);
//                    Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Retry: container found = {container != null}");
//                    if (container != null)
//                    {
//                        var tile = GetContentTemplateRootFromContainer(container) as BaseTileControl;
//                        if (tile != null)
//                        {
//                            Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Retry successful, starting edit");
//                            tile.StartEditing();
//                        }
//                        else
//                        {
//                            Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Retry: tile is null, skipping");
//                            _multiRenameCurrentIndex++;
//                            BeginRenameForCurrentMultiItem();
//                        }
//                    }
//                    else
//                    {
//                        Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Retry: container still not found, skipping");
//                        _multiRenameCurrentIndex++;
//                        BeginRenameForCurrentMultiItem();
//                    }
//                }, Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal);
//            }
//        }

//        // НОВЫЙ МЕТОД: Завершить множественное переименование
//        private void FinishMultiRename()
//        {
//            Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} ========== FINISH MULTI-RENAME ==========");
//            Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Current state: Index={_multiRenameCurrentIndex}, Paths count={_multiRenamePaths?.Count}");

//            _isMultiRenameMode = false;
//            _multiRenamePaths = null;
//            _multiRenameCurrentIndex = 0;

//            // Возвращаем фокус на ItemsControl
//            _ = DispatcherQueue.TryEnqueue(() =>
//            {
//                Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Returning focus to items control");
//                _currentItemsControl?.Focus(FocusState.Programmatic);
//            });

//            Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} ========== END FINISH MULTI-RENAME ==========");
//        }

//        private async void DeleteSelectedItems()
//        {
//            var selectedItems = _currentItemsControl.SelectedItems.Cast<ExplorerItemViewModel>().ToList();
//            if (selectedItems.Count > 0)
//            {
//                Debug.WriteLine($"[{PanelId}] [DeleteSelectedItems] Deleting {selectedItems.Count} items");
//                // Здесь будет логика удаления
//            }
//        }

//        #endregion

//        #region Обновление и Refresh

//        public void RefreshNavigation()
//        {
//            Debug.WriteLine($"[{PanelId}] [RefreshNavigation] Refreshing navigation via mediator");

//            _ = this.DispatcherQueue.EnqueueAsync(() =>
//            {
//                Debug.WriteLine($"[{PanelId}] [RefreshNavigation] Executing on UI thread");
//                if (_currentItemsControl != null)
//                {
//                    _currentItemsControl.SelectedItem = null;
//                    _currentItemsControl.SelectedItems.Clear();
//                }
//                _lastClickedItem = null;
//                _lastClickTime = DateTime.MinValue;
//                _shiftSelectionStartItem = null;
//                _isCtrlPressed = false;
//                _isShiftPressed = false;
//                _isAltPressed = false;
//                _hoveredItem = null;
//                _isDragSelecting = false;
//                _isLeftMouseButtonPressed = false;
//                _isMouseMovingWithButton = false;
//                _isProcessingBackNavigation = false;
//                _isMultiRenameMode = false;
//                _multiRenamePaths = null;
//                _multiRenameCurrentIndex = 0;

//                StopHoverTimer();

//                RemoveSelectionRectangle();
//                _tempSelectedIndices.Clear();

//                Task task = RefreshContent();
//            }, Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal);
//        }

//        public async Task RefreshContent()
//        {
//            Debug.WriteLine($"[{PanelId}] [RefreshContent] ENTER");

//            try
//            {
//                string pathToReload = await DispatcherQueue.EnqueueAsync(() =>
//                {
//                    string path = _currentLoadedPath;
//                    if (string.IsNullOrEmpty(path) && PanelManager != null)
//                        path = PanelManager.CurrentPath;

//                    _currentLoadedPath = null;
//                    _isInitialized = false;
//                    Items.Clear();
//                    if (_currentItemsControl != null)
//                    {
//                        _currentItemsControl.SelectedItems.Clear();
//                        _currentItemsControl.SelectedItem = null;
//                    }
//                    _shiftSelectionStartItem = null;
//                    _isCtrlPressed = false;
//                    _isShiftPressed = false;
//                    _isAltPressed = false;
//                    _hoveredItem = null;
//                    _isDragSelecting = false;
//                    _isLeftMouseButtonPressed = false;
//                    _isMouseMovingWithButton = false;
//                    _isProcessingBackNavigation = false;
//                    _isMultiRenameMode = false;
//                    _multiRenamePaths = null;
//                    _multiRenameCurrentIndex = 0;

//                    StopHoverTimer();

//                    RemoveSelectionRectangle();
//                    _tempSelectedIndices.Clear();
//                    UpdateItemsControlLayout();

//                    Debug.WriteLine($"[{PanelId}] [RefreshContent] State reset, path to reload: '{path}'");
//                    return path;
//                });

//                _fileSystemService.ClearPanelCache(PanelId);
//                CancelCurrentOperation();

//                if (!string.IsNullOrEmpty(pathToReload))
//                {
//                    Debug.WriteLine($"[{PanelId}] [RefreshContent] Reloading path: {pathToReload}");
//                    await LoadPathContents(pathToReload);
//                }
//                else
//                {
//                    Debug.WriteLine($"[{PanelId}] [RefreshContent] No path, loading initial content");
//                    await Task.Run(() => LoadInitialContent());
//                }

//                Debug.WriteLine($"[{PanelId}] [RefreshContent] Completed");
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[{PanelId}] [RefreshContent] Error: {ex}");

//                try
//                {
//                    await DispatcherQueue.EnqueueAsync(() => LoadInitialContent());
//                }
//                catch (Exception fallbackEx)
//                {
//                    Debug.WriteLine($"[{PanelId}] [RefreshContent] Fallback error: {fallbackEx}");
//                }
//            }

//            Debug.WriteLine($"[{PanelId}] [RefreshContent] EXIT");
//        }

//        #endregion
//    }
//}


using CommunityToolkit.WinUI;
using Core_FileManagement;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
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
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input;

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
        private Grid _parentGrid;

        private DispatcherTimer _hoverTimer;
        private ExplorerItemViewModel _hoveredItem = null;
        private const int HOVER_DELAY_MS = 50;

        private bool _wasClickHandled = false;

        private HashSet<int> _tempSelectedIndices = new HashSet<int>();

        private readonly SemaphoreSlim _navigationSemaphore = new SemaphoreSlim(1, 1);
        private bool _isProcessingBackNavigation = false;

        private int _maxRowsOrColumns = 1;
        public int MaxRowsOrColumns
        {
            get => _maxRowsOrColumns;
            private set
            {
                if (_maxRowsOrColumns != value)
                {
                    Debug.WriteLine($"[{PanelId}] [MaxRowsOrColumns] Changing from {_maxRowsOrColumns} to {value}");
                    _maxRowsOrColumns = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _displayMode = "Horizontal";
        public string DisplayMode
        {
            get => _displayMode;
            set
            {
                if (_displayMode != value)
                {
                    Debug.WriteLine($"[{PanelId}] [DisplayMode] Changing from '{_displayMode}' to '{value}'");
                    _displayMode = value;
                    UpdateDisplayMode();
                    OnPropertyChanged();
                }
            }
        }

        private ListViewBase _currentItemsControl;

        public double ItemWidth
        {
            get => _itemWidth;
            private set
            {
                if (_itemWidth != value)
                {
                    Debug.WriteLine($"[{PanelId}] [ItemWidth] Changing from {_itemWidth} to {value}");
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
                    Debug.WriteLine($"[{PanelId}] [ItemHeight] Changing from {_itemHeight} to {value}");
                    _itemHeight = value;
                    UpdateItemsControlLayout();
                }
            }
        }

        private string _selectedSize = "Icons Medium";
        private int _hoverSelectionLock = 0;

        private const int HorizontalPadding = 10;
        private const int VerticalPadding = 8;
        private const int TextBlockHeight = 40;
        private const int MinimumItemWidth = 100;
        private const int MinimumItemHeight = 40;

        // Защитный интервал после редактирования
        private DateTime _lastEditEndTime = DateTime.MinValue;
        private const int EditCooldownMs = 300;

        // Защита от восстановления выделения после клика на пустое место
        private DateTime _lastEmptySpaceClickTime = DateTime.MinValue;
        private const int EmptySpaceClickCooldownMs = 300;

        // Для множественного переименования
        private bool _isMultiRenameMode = false;
        private List<string> _multiRenamePaths;  // Список исходных путей выделенных элементов
        private int _multiRenameCurrentIndex;    // Индекс текущего обрабатываемого пути
        private const string MultiRenameLogPrefix = "[MultiRename]";

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
                    Debug.WriteLine($"[{PanelId}] [SingleClickOpen] Error: {ex}");
                    return false;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Конструктор, инициализация и Dispose

        public TileViewerContent()
        {
            Debug.WriteLine($"[{PanelId}] [Constructor] Entering TileViewerContent constructor");

            InitializeComponent();

            NavigationSettingsMediator.RegisterPanel(this);

            _dummyHistory = new DirectoryHistory("MyComputer", "Мой Компьютер");

            _fileSystemService = new FileSystemService();

            _currentItemsControl = ItemsListView;
            ItemsListView.ItemsSource = Items;
            ItemsGridView.ItemsSource = Items;

            SubscribeToEvents(_currentItemsControl);
            if (_currentItemsControl is ListView listView)
                listView.ItemClick += ItemsControl_OnItemClick;

            Loaded += OnLoaded;

            InitializeHoverTimer();

            this.Loaded += (s, e) => InitializeSelectionCanvas();

            CalculateItemDimensions();

            Debug.WriteLine($"[{PanelId}] [Constructor] Exiting, DisplayMode={_displayMode}, SelectedSize={_selectedSize}");
        }

        private void SubscribeToEvents(ListViewBase itemsControl)
        {
            Debug.WriteLine($"[{PanelId}] [SubscribeToEvents] Subscribing to events for {itemsControl.GetType().Name}");
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
            Debug.WriteLine($"[{PanelId}] [UnsubscribeFromEvents] Unsubscribing from events for {itemsControl.GetType().Name}");
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
            Debug.WriteLine($"[{PanelId}] [Dispose] Entering Dispose");
            NavigationSettingsMediator.UnregisterPanel(this);
            _currentOperationCts?.Cancel();
            _currentOperationCts?.Dispose();
            _dummyHistory?.Dispose();

            Loaded -= OnLoaded;

            UnsubscribeFromEvents(ItemsListView);
            UnsubscribeFromEvents(ItemsGridView);

            ItemsListView.ItemClick -= ItemsControl_OnItemClick;
            ItemsGridView.ItemClick -= ItemsControl_OnItemClick;

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

            RemoveSelectionCanvas();

            foreach (var item in Items)
            {
                item?.Dispose();
            }

            _tempSelectedIndices.Clear();
            _navigationSemaphore?.Dispose();
            Debug.WriteLine($"[{PanelId}] [Dispose] Exiting");
        }

        private void RemoveSelectionCanvas()
        {
            if (_selectionCanvas != null && _parentGrid != null)
            {
                _parentGrid.Children.Remove(_selectionCanvas);
                _selectionCanvas = null;
                Debug.WriteLine($"[{PanelId}] [RemoveSelectionCanvas] Canvas removed");
            }
        }

        private void InitializeHoverTimer()
        {
            Debug.WriteLine($"[{PanelId}] [InitializeHoverTimer] Creating hover timer");
            _hoverTimer = new DispatcherTimer();
            _hoverTimer.Interval = TimeSpan.FromMilliseconds(HOVER_DELAY_MS);
            _hoverTimer.Tick += HoverTimer_Tick;
        }

        private void InitializeSelectionCanvas()
        {
            Debug.WriteLine($"[{PanelId}] [InitializeSelectionCanvas] Entering");
            if (_selectionCanvas != null)
            {
                Debug.WriteLine($"[{PanelId}] [InitializeSelectionCanvas] Canvas already exists, returning");
                return;
            }

            _selectionCanvas = new Canvas
            {
                IsHitTestVisible = false,
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent)
            };

            // Получаем родительский Grid
            _parentGrid = this.Content as Grid;
            if (_parentGrid != null)
            {
                _parentGrid.Children.Add(_selectionCanvas);
                Canvas.SetZIndex(_selectionCanvas, 1000);
                Debug.WriteLine($"[{PanelId}] [InitializeSelectionCanvas] Canvas added to root grid");
            }
            else
            {
                Debug.WriteLine($"[{PanelId}] [InitializeSelectionCanvas] Root grid not found");
            }
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"[{PanelId}] [OnLoaded] Entering, SelectedSize={_selectedSize}, PanelManager.CurrentPath={PanelManager?.CurrentPath}");
            if (string.IsNullOrEmpty(_selectedSize))
            {
                _selectedSize = "Medium";
                Debug.WriteLine($"[{PanelId}] [OnLoaded] Using default size: Medium");
            }

            CalculateItemDimensions();

            await Task.Delay(50);

            UpdateAllTiles();
            UpdateItemsControlLayout();

            if (PanelManager != null && !string.IsNullOrEmpty(PanelManager.CurrentPath))
            {
                Debug.WriteLine($"[{PanelId}] [OnLoaded] Loading contents from PanelManager path: {PanelManager.CurrentPath}");
                await LoadPathContents(PanelManager.CurrentPath);
                _isInitialized = true;
            }
            else if (!_isInitialized)
            {
                Debug.WriteLine($"[{PanelId}] [OnLoaded] Loading initial content (MyComputer)");
                LoadInitialContent();
                _isInitialized = true;
            }
            Debug.WriteLine($"[{PanelId}] [OnLoaded] Completed");
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            Debug.WriteLine($"[{PanelId}] [OnPropertyChanged] Property '{propertyName}' changed");
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Управление PanelManager и навигация

        public void SetPanelManager(PanelManager panelManager)
        {
            Debug.WriteLine($"[{PanelId}] [SetPanelManager] Setting PanelManager, CurrentPath={panelManager?.CurrentPath}, IconSize={panelManager?.State?.IconSize}");
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
            Debug.WriteLine($"[{PanelId}] [OnPanelNavigationChanged] Entering, IsLoading={_isLoading}, CurrentPath={PanelManager?.CurrentPath}, _currentLoadedPath={_currentLoadedPath}");
            if (_isLoading) return;

            if (PanelManager != null && PanelManager.CurrentPath != _currentLoadedPath)
            {
                await Task.Delay(100);

                if (PanelManager.CurrentPath != _currentLoadedPath)
                {
                    Debug.WriteLine($"[{PanelId}] [OnPanelNavigationChanged] Loading path: {PanelManager.CurrentPath}");
                    await LoadPathContents(PanelManager.CurrentPath);
                }
            }
        }

        private void OnNavigationChanged()
        {
            Debug.WriteLine($"[{PanelId}] [OnNavigationChanged] Raising NavigationChanged event");
            NavigationChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Загрузка содержимого (Load, Refresh)

        private async void LoadInitialContent()
        {
            Debug.WriteLine($"[{PanelId}] [LoadInitialContent] ENTER");

            if (_currentLoadedPath == "MyComputer" && Items.Count > 0)
            {
                Debug.WriteLine($"[{PanelId}] [LoadInitialContent] MyComputer already loaded, skipping");
                return;
            }

            CancelCurrentOperation();
            Items.Clear();
            UpdateItemsControlLayout();

            try
            {
                Debug.WriteLine($"[{PanelId}] [LoadInitialContent] Calling _fileSystemService.LoadMyComputerAsync");
                var items = await _fileSystemService.LoadMyComputerAsync(PanelId, _dummyHistory);
                Debug.WriteLine($"[{PanelId}] [LoadInitialContent] Loaded {items.Count} items");
                foreach (var item in items)
                {
                    Items.Add(item);
                }
                _currentLoadedPath = "MyComputer";
                OnNavigationChanged();
                Debug.WriteLine($"[{PanelId}] [LoadInitialContent] MyComputer loaded, items count: {Items.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{PanelId}] [LoadInitialContent] Error: {ex}");
            }

            Debug.WriteLine($"[{PanelId}] [LoadInitialContent] EXIT");
        }

        internal async Task LoadPathContents(string path)
        {
            Debug.WriteLine($"[{PanelId}] [LoadPathContents] ENTER, path='{path}'");
            await _navigationSemaphore.WaitAsync();
            try
            {
                Debug.WriteLine($"[{PanelId}] [LoadPathContents] Acquired semaphore, CurrentLoadedPath='{_currentLoadedPath}', IsLoading={_isLoading}");

                if (_isLoading || _currentLoadedPath == path)
                {
                    Debug.WriteLine($"[{PanelId}] [LoadPathContents] Skipping - already loading or same path");
                    return;
                }

                try
                {
                    _isLoading = true;
                    switch (path)
                    {
                        case "MyComputer":
                            Debug.WriteLine($"[{PanelId}] [LoadPathContents] Case: MyComputer");
                            LoadInitialContent();
                            _currentLoadedPath = path;
                            break;

                        case "Drives":
                            Debug.WriteLine($"[{PanelId}] [LoadPathContents] Case: Drives");
                            await LoadDrives();
                            _currentLoadedPath = path;
                            break;

                        case string p when Directory.Exists(p):
                            Debug.WriteLine($"[{PanelId}] [LoadPathContents] Case: Directory exists");
                            await LoadFolderContents(path);
                            _currentLoadedPath = path;

                            if (PanelManager != null && PanelManager.CurrentPath != path)
                            {
                                PanelManager.NavigateTo(path);
                                Debug.WriteLine($"[{PanelId}] [LoadPathContents] Called PanelManager.NavigateTo({path})");
                            }
                            break;

                        default:
                            Debug.WriteLine($"[{PanelId}] [LoadPathContents] Case: Default (path does not exist or is unknown)");
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
            finally
            {
                _navigationSemaphore.Release();
                Debug.WriteLine($"[{PanelId}] [LoadPathContents] Released semaphore");
            }
            Debug.WriteLine($"[{PanelId}] [LoadPathContents] EXIT");
        }

        private async Task LoadDrives()
        {
            Debug.WriteLine($"[{PanelId}] [LoadDrives] ENTER, CurrentLoadedPath='{_currentLoadedPath}', Items.Count={Items.Count}");

            if (_currentLoadedPath == "Drives" && Items.Count > 1)
            {
                Debug.WriteLine($"[{PanelId}] [LoadDrives] Drives already loaded, skipping");
                return;
            }

            CancelCurrentOperation();
            Items.Clear();
            UpdateItemsControlLayout();

            try
            {
                Debug.WriteLine($"[{PanelId}] [LoadDrives] Calling _fileSystemService.LoadDrivesAsync");
                var driveItems = await _fileSystemService.LoadDrivesAsync(_dummyHistory);
                Debug.WriteLine($"[{PanelId}] [LoadDrives] Loaded {driveItems.Count} drives");

                await this.DispatcherQueue.EnqueueAsync(() =>
                {
                    Items.Clear();
                    foreach (var item in driveItems)
                    {
                        Items.Add(item);
                    }
                    UpdateItemsControlLayout();
                    Debug.WriteLine($"[{PanelId}] [LoadDrives] Drives added to Items, count: {Items.Count}");
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{PanelId}] [LoadDrives] ERROR: {ex}");

                await this.DispatcherQueue.EnqueueAsync(() =>
                {
                    Items.Clear();
                    UpdateItemsControlLayout();
                });
            }

            OnNavigationChanged();
            Debug.WriteLine($"[{PanelId}] [LoadDrives] EXIT");
        }

        private async Task LoadFolderContents(string folderPath)
        {
            Debug.WriteLine($"[{PanelId}] [LoadFolderContents] ENTER, folderPath='{folderPath}'");

            if (string.IsNullOrEmpty(folderPath))
            {
                Debug.WriteLine($"[{PanelId}] [LoadFolderContents] Path is empty, returning");
                return;
            }

            if (!Directory.Exists(folderPath))
            {
                Debug.WriteLine($"[{PanelId}] [LoadFolderContents] Directory does not exist: {folderPath}");
                PanelManager?.GoBack();
                return;
            }

            if (_currentLoadedPath == folderPath && Items.Count > 0)
            {
                Debug.WriteLine($"[{PanelId}] [LoadFolderContents] Folder already loaded, skipping");
                return;
            }

            CancelCurrentOperation();

            try
            {
                Debug.WriteLine($"[{PanelId}] [LoadFolderContents] Calling _fileSystemService.LoadFolderContentsAsync");
                var folderItems = await _fileSystemService.LoadFolderContentsAsync(folderPath, _dummyHistory);
                Debug.WriteLine($"[{PanelId}] [LoadFolderContents] Loaded {folderItems.Count} items");

                await this.DispatcherQueue.EnqueueAsync(() =>
                {
                    Items.Clear();
                    foreach (var item in folderItems)
                    {
                        Items.Add(item);
                    }
                    UpdateItemsControlLayout();
                    Debug.WriteLine($"[{PanelId}] [LoadFolderContents] Items added, count: {Items.Count}");
                });
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"[{PanelId}] [LoadFolderContents] Operation canceled");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{PanelId}] [LoadFolderContents] Error: {ex}");
                PanelManager?.GoBack();
            }

            Debug.WriteLine($"[{PanelId}] [LoadFolderContents] EXIT");
        }

        private void CancelCurrentOperation()
        {
            Debug.WriteLine($"[{PanelId}] [CancelCurrentOperation] Cancelling current operations");
            _currentOperationCts?.Cancel();
            _currentOperationCts?.Dispose();
            _currentOperationCts = new CancellationTokenSource();
            _fileSystemService.CancelAllOperations();
        }

        public async Task RefreshContent()
        {
            Debug.WriteLine($"[{PanelId}] [RefreshContent] ENTER");

            try
            {
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
                        _currentItemsControl.SelectedItem = null;
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
                    _isMultiRenameMode = false;
                    _multiRenamePaths = null;
                    _multiRenameCurrentIndex = 0;

                    StopHoverTimer();

                    RemoveSelectionRectangle();
                    _tempSelectedIndices.Clear();
                    UpdateItemsControlLayout();

                    Debug.WriteLine($"[{PanelId}] [RefreshContent] State reset, path to reload: '{path}'");
                    return path;
                });

                _fileSystemService.ClearPanelCache(PanelId);
                CancelCurrentOperation();

                if (!string.IsNullOrEmpty(pathToReload))
                {
                    Debug.WriteLine($"[{PanelId}] [RefreshContent] Reloading path: {pathToReload}");
                    await LoadPathContents(pathToReload);
                }
                else
                {
                    Debug.WriteLine($"[{PanelId}] [RefreshContent] No path, loading initial content");
                    await Task.Run(() => LoadInitialContent());
                }

                Debug.WriteLine($"[{PanelId}] [RefreshContent] Completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{PanelId}] [RefreshContent] Error: {ex}");

                try
                {
                    await DispatcherQueue.EnqueueAsync(() => LoadInitialContent());
                }
                catch (Exception fallbackEx)
                {
                    Debug.WriteLine($"[{PanelId}] [RefreshContent] Fallback error: {fallbackEx}");
                }
            }

            Debug.WriteLine($"[{PanelId}] [RefreshContent] EXIT");
        }

        public void RefreshNavigation()
        {
            Debug.WriteLine($"[{PanelId}] [RefreshNavigation] Refreshing navigation via mediator");

            _ = this.DispatcherQueue.EnqueueAsync(() =>
            {
                Debug.WriteLine($"[{PanelId}] [RefreshNavigation] Executing on UI thread");
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
                _isMultiRenameMode = false;
                _multiRenamePaths = null;
                _multiRenameCurrentIndex = 0;

                StopHoverTimer();

                RemoveSelectionRectangle();
                _tempSelectedIndices.Clear();

                Task task = RefreshContent();
            }, Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal);
        }

        #endregion

        #region Управление режимами отображения (DisplayMode, IconSize)

        private void UpdateDisplayMode()
        {
            Debug.WriteLine($"[{PanelId}] [UpdateDisplayMode] Switching to DisplayMode: {DisplayMode}");

            var selectedItems = _currentItemsControl?.SelectedItems?.Cast<ExplorerItemViewModel>().ToList();
            Debug.WriteLine($"[{PanelId}] [UpdateDisplayMode] Saved {selectedItems?.Count ?? 0} selected items");

            var oldControl = _currentItemsControl;

            switch (DisplayMode.ToLower())
            {
                case "horizontal":
                case "list":
                    ItemsListView.Visibility = Visibility.Visible;
                    ItemsGridView.Visibility = Visibility.Collapsed;
                    _currentItemsControl = ItemsListView;
                    Debug.WriteLine($"[{PanelId}] [UpdateDisplayMode] Activated ListView");
                    break;

                case "vertical":
                case "icons":
                    ItemsListView.Visibility = Visibility.Collapsed;
                    ItemsGridView.Visibility = Visibility.Visible;
                    _currentItemsControl = ItemsGridView;
                    Debug.WriteLine($"[{PanelId}] [UpdateDisplayMode] Activated GridView");
                    break;

                default:
                    ItemsListView.Visibility = Visibility.Visible;
                    ItemsGridView.Visibility = Visibility.Collapsed;
                    _currentItemsControl = ItemsListView;
                    Debug.WriteLine($"[{PanelId}] [UpdateDisplayMode] Default to ListView");
                    break;
            }

            if (oldControl != _currentItemsControl)
            {
                Debug.WriteLine($"[{PanelId}] [UpdateDisplayMode] Control changed, re-subscribing events");
                UnsubscribeFromEvents(oldControl);
                if (oldControl is ListView oldListView)
                    oldListView.ItemClick -= ItemsControl_OnItemClick;
                else if (oldControl is GridView oldGridView)
                    oldGridView.ItemClick -= ItemsControl_OnItemClick;

                SubscribeToEvents(_currentItemsControl);
                if (_currentItemsControl is ListView newListView)
                    newListView.ItemClick += ItemsControl_OnItemClick;
                else if (_currentItemsControl is GridView newGridView)
                    newGridView.ItemClick += ItemsControl_OnItemClick;
            }

            // Пересоздаем Canvas при смене режима
            RecreateSelectionCanvas();

            if (selectedItems != null && _currentItemsControl != null)
            {
                _currentItemsControl.SelectedItems.Clear();
                foreach (var item in selectedItems)
                {
                    _currentItemsControl.SelectedItems.Add(item);
                }
                Debug.WriteLine($"[{PanelId}] [UpdateDisplayMode] Restored {selectedItems.Count} selected items");
            }

            CalculateItemDimensions();
            UpdateItemsControlLayout();
            UpdateAllTiles();

            Debug.WriteLine($"[{PanelId}] [UpdateDisplayMode] Completed");
        }

        // НОВЫЙ МЕТОД: Пересоздание Canvas для текущего режима
        private void RecreateSelectionCanvas()
        {
            RemoveSelectionCanvas();
            _selectionCanvas = null;
            InitializeSelectionCanvas();
        }

        public void SetIconSize(string size)
        {
            Debug.WriteLine($"[{PanelId}] [SetIconSize] Setting size from '{_selectedSize}' to '{size}'");
            _selectedSize = size;

            if (PanelManager != null)
            {
                PanelManager.UpdateState(state => state.IconSize = size);
                Debug.WriteLine($"[{PanelId}] [SetIconSize] Updated PanelManager state");
            }

            CalculateItemDimensions();
            UpdateAllTiles();
            UpdateItemsControlLayout();
            ItemsListView.UpdateLayout();
            ItemsGridView.UpdateLayout();
            Debug.WriteLine($"[{PanelId}] [SetIconSize] Completed, ItemWidth={ItemWidth}, ItemHeight={ItemHeight}");
        }

        private void CalculateItemDimensions()
        {
            Debug.WriteLine($"[{PanelId}] [CalculateItemDimensions] Calculating for size '{_selectedSize}'");
            var sizeParams = SizeManagerTile.GetSize(_selectedSize);

            string viewType = _selectedSize.Split(' ').FirstOrDefault()?.ToLower() ?? "";

            switch (viewType)
            {
                case "icons":
                    ItemWidth = Math.Max(sizeParams.Width + 20, MinimumItemWidth);
                    ItemHeight = Math.Max(sizeParams.Height + 25, MinimumItemHeight);
                    break;

                case "list":
                case "compactlist":
                case "table":
                case "tiles":
                    ItemWidth = Math.Max(sizeParams.Width + 20, MinimumItemWidth);
                    ItemHeight = Math.Max(sizeParams.Height + 20, MinimumItemHeight);
                    break;

                default:
                    ItemWidth = Math.Max(sizeParams.Width + 20, MinimumItemWidth);
                    ItemHeight = Math.Max(sizeParams.Height + 25, MinimumItemHeight);
                    break;
            }
            Debug.WriteLine($"[{PanelId}] [CalculateItemDimensions] Result: Width={ItemWidth}, Height={ItemHeight}");
        }

        #endregion

        #region Обновление UI и Layout

        private void UpdateAllTiles()
        {
            Debug.WriteLine($"[{PanelId}] [UpdateAllTiles] Updating all tiles");
            UpdateTilesInControl(ItemsListView);
            UpdateTilesInControl(ItemsGridView);
        }

        private void UpdateTilesInControl(ListViewBase itemsControl)
        {
            foreach (var item in itemsControl.Items)
            {
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

            BaseTileControl tile = null;
            if (args.ItemContainer is ListViewItem listViewItem)
                tile = listViewItem.ContentTemplateRoot as BaseTileControl;
            else if (args.ItemContainer is GridViewItem gridViewItem)
                tile = gridViewItem.ContentTemplateRoot as BaseTileControl;

            if (tile != null)
            {
                tile.UpdateSize(_selectedSize);
                tile.EditStateChanged -= OnTileEditStateChanged;
                tile.EditStateChanged += OnTileEditStateChanged;

                // Подписываемся на новое событие EditCompleted
                tile.EditCompleted -= OnTileEditCompleted;
                tile.EditCompleted += OnTileEditCompleted;

                Debug.WriteLine($"[{PanelId}] [ContainerContentChanging] Tile updated and events subscribed");
            }
        }

        private void UpdateItemsControlLayout()
        {
            Debug.WriteLine($"[{PanelId}] [UpdateItemsControlLayout] Updating layout for both controls");
            UpdateControlLayout(ItemsListView);
            UpdateControlLayout(ItemsGridView);
        }

        private void UpdateControlLayout(ListViewBase itemsControl)
        {
            if (itemsControl?.ItemsPanelRoot is ItemsWrapGrid wrapGrid)
            {
                double oldWidth = wrapGrid.ItemWidth, oldHeight = wrapGrid.ItemHeight;
                wrapGrid.ItemWidth = ItemWidth;
                wrapGrid.ItemHeight = ItemHeight;
                Debug.WriteLine($"[{PanelId}] [UpdateControlLayout] {itemsControl.GetType().Name}: ItemWidth {oldWidth}->{ItemWidth}, ItemHeight {oldHeight}->{ItemHeight}");

                if (itemsControl == ItemsListView)
                {
                    UpdateMaxRowsOrColumns();
                    int oldMax = wrapGrid.MaximumRowsOrColumns;
                    wrapGrid.MaximumRowsOrColumns = MaxRowsOrColumns;
                    Debug.WriteLine($"[{PanelId}] [UpdateControlLayout] ListView MaxRowsOrColumns: {oldMax}->{MaxRowsOrColumns}");
                }
                else if (itemsControl == ItemsGridView)
                {
                    wrapGrid.MaximumRowsOrColumns = 24;
                }

                UpdateSelectionVisual(itemsControl);
            }
            else
            {
                Debug.WriteLine($"[{PanelId}] [UpdateControlLayout] ItemsPanelRoot is not ItemsWrapGrid or null");
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
                Debug.WriteLine($"[{PanelId}] [UpdateMaxRowsOrColumns] Calculated: {maxRows} rows (Height={actualHeight}, ItemHeight={ItemHeight})");
            }
        }

        private void ItemsControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var itemsControl = sender as ListViewBase;
            Debug.WriteLine($"[{PanelId}] [ItemsControl_SizeChanged] {itemsControl.GetType().Name}: NewSize={e.NewSize.Width}x{e.NewSize.Height}");

            if (itemsControl == ItemsListView)
            {
                UpdateMaxRowsOrColumns();
            }
            UpdateControlLayout(itemsControl);
        }

        private void UpdateUIForSelection()
        {
            int selectedCount = _currentItemsControl?.SelectedItems.Count ?? 0;
            Debug.WriteLine($"[{PanelId}] [UpdateUIForSelection] {selectedCount} items selected");
        }

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

        #endregion

        #region Обработка событий мыши и выделение

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

        private void UpdateModifierKeyStateFromCore()
        {
            try
            {
                var ctrlState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
                var shiftState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift);
                var altState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu);

                bool oldCtrl = _isCtrlPressed, oldShift = _isShiftPressed, oldAlt = _isAltPressed;
                _isCtrlPressed = ctrlState.HasFlag(CoreVirtualKeyStates.Down);
                _isShiftPressed = shiftState.HasFlag(CoreVirtualKeyStates.Down);
                _isAltPressed = altState.HasFlag(CoreVirtualKeyStates.Down);

                if (oldCtrl != _isCtrlPressed || oldShift != _isShiftPressed || oldAlt != _isAltPressed)
                {
                    Debug.WriteLine($"[{PanelId}] [UpdateModifierKeyStateFromCore] Ctrl={_isCtrlPressed}, Shift={_isShiftPressed}, Alt={_isAltPressed}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{PanelId}] [UpdateModifierKeyStateFromCore] Error: {ex}");

                var coreWindow = CoreWindow.GetForCurrentThread();
                if (coreWindow != null)
                {
                    _isCtrlPressed = coreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
                    _isShiftPressed = coreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
                    _isAltPressed = coreWindow.GetKeyState(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down);
                    Debug.WriteLine($"[{PanelId}] [UpdateModifierKeyStateFromCore] Fallback: Ctrl={_isCtrlPressed}, Shift={_isShiftPressed}, Alt={_isAltPressed}");
                }
            }
        }

        private void HoverTimer_Tick(object sender, object e)
        {
            Debug.WriteLine($"[{PanelId}] [HoverTimer_Tick] Timer tick");
            _hoverTimer.Stop();

            UpdateModifierKeyStateFromCore();

            if (_hoveredItem != null && SingleClickOpenItem && !_isDragSelecting)
            {
                Debug.WriteLine($"[{PanelId}] [HoverTimer_Tick] Selecting item on hover: {_hoveredItem.Name}");

                // Проверяем, что элемент все еще существует в коллекции
                if (Items.Contains(_hoveredItem))
                {
                    SelectItemOnHover(_hoveredItem);
                }
                else
                {
                    Debug.WriteLine($"[{PanelId}] [HoverTimer_Tick] Hovered item no longer in collection, resetting");
                    _hoveredItem = null;
                }
            }
            else
            {
                Debug.WriteLine($"[{PanelId}] [HoverTimer_Tick] No action: HoveredItem={_hoveredItem?.Name}, SingleClickOpenItem={SingleClickOpenItem}, IsDragSelecting={_isDragSelecting}");
            }
        }

        private void StartHoverTimer()
        {
            if (_hoverTimer != null && !_hoverTimer.IsEnabled)
            {
                Debug.WriteLine($"[{PanelId}] [StartHoverTimer] Starting timer");
                _hoverTimer.Start();
            }
        }

        private void StopHoverTimer()
        {
            if (_hoverTimer != null && _hoverTimer.IsEnabled)
            {
                Debug.WriteLine($"[{PanelId}] [StopHoverTimer] Stopping timer");
                _hoverTimer.Stop();
            }
        }

        private void RestartHoverTimer()
        {
            Debug.WriteLine($"[{PanelId}] [RestartHoverTimer] Restarting timer");
            StopHoverTimer();
            StartHoverTimer();
        }

        private void ItemsControl_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerEntered] Entering");
            UpdateModifierKeyStateFromCore();
            if (!SingleClickOpenItem) return;

            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl) return;

            var element = e.OriginalSource as FrameworkElement;
            var item = FindParentDataContext<ExplorerItemViewModel>(element);

            if (item != null)
            {
                Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerEntered] Hovered item: {item.Name} (Hash: {item.GetHashCode()})");
                _hoveredItem = item;
                StartHoverTimer();
            }
        }

        private void ItemsControl_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerExited] Entering");
            UpdateModifierKeyStateFromCore();
            if (!SingleClickOpenItem) return;

            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl) return;

            StopHoverTimer();
            if (_hoveredItem != null)
            {
                Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerExited] Hover cleared (was {_hoveredItem.Name})");
                _hoveredItem = null;
            }
        }

        private void ItemsControl_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            UpdateModifierKeyStateFromCore();
            if (!SingleClickOpenItem) return;

            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl) return;

            var element = e.OriginalSource as FrameworkElement;
            var item = FindParentDataContext<ExplorerItemViewModel>(element);

            if (item != null)
            {
                if (item != _hoveredItem)
                {
                    Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerMoved] New hover item: {item.Name} (Hash: {item.GetHashCode()})");
                    _hoveredItem = item;
                    RestartHoverTimer();
                }
            }
            else if (_hoveredItem != null)
            {
                Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerMoved] Hover lost (was {_hoveredItem.Name})");
                StopHoverTimer();
                _hoveredItem = null;
            }
        }

        // ИСПРАВЛЕННЫЙ МЕТОД: Определение клика на элемент (только по контейнеру)
        private bool IsClickOnItem(Windows.Foundation.Point point, ListViewBase itemsControl)
        {
            var elements = VisualTreeHelper.FindElementsInHostCoordinates(point, itemsControl);

            foreach (var element in elements)
            {
                var current = element as DependencyObject;
                while (current != null)
                {
                    // Проверяем, является ли текущий элемент контейнером элемента
                    if (current is ListViewItem || current is GridViewItem)
                    {
                        // Убедимся, что контейнер действительно содержит элемент данных
                        var container = current as FrameworkElement;
                        if (container?.DataContext is ExplorerItemViewModel)
                        {
                            Debug.WriteLine($"[{PanelId}] [IsClickOnItem] Found item container with DataContext at point ({point.X}, {point.Y})");
                            return true;
                        }
                        // Если контейнер есть, но DataContext не тот – возможно, пустой контейнер, тогда не считаем кликом на элементе
                        // (такого быть не должно, но на всякий случай игнорируем)
                    }

                    current = VisualTreeHelper.GetParent(current);
                }
            }

            Debug.WriteLine($"[{PanelId}] [IsClickOnItem] No item container found at point ({point.X}, {point.Y}) - empty space");
            return false;
        }

        // ИСПРАВЛЕННЫЙ МЕТОД: Получение элемента под курсором через контейнер
        private ExplorerItemViewModel GetItemAtPoint(Windows.Foundation.Point point, ListViewBase itemsControl)
        {
            var elements = VisualTreeHelper.FindElementsInHostCoordinates(point, itemsControl);

            foreach (var element in elements)
            {
                var current = element as FrameworkElement;
                while (current != null)
                {
                    // Если это контейнер элемента, берем его DataContext
                    if (current is ListViewItem || current is GridViewItem)
                    {
                        if (current.DataContext is ExplorerItemViewModel item)
                        {
                            Debug.WriteLine($"[{PanelId}] [GetItemAtPoint] Found item '{item.Name}' at point ({point.X}, {point.Y}) via container");
                            return item;
                        }
                    }
                    current = VisualTreeHelper.GetParent(current) as FrameworkElement;
                }
            }

            return null;
        }

        private void ItemsControl_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerPressed] Entering");
            UpdateModifierKeyStateFromCore();

            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl) return;

            var point = e.GetCurrentPoint(itemsControl);

            _dragStartPoint = new Vector2((float)point.Position.X, (float)point.Position.Y);
            _wasClickHandled = false;
            Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerPressed] Start point: {_dragStartPoint.X}, {_dragStartPoint.Y}");

            if (point.Properties.IsLeftButtonPressed)
            {
                _isLeftMouseButtonPressed = true;
                _isMouseMovingWithButton = false;

                // Проверяем, кликнули ли на контейнере элемента
                bool isClickOnItem = IsClickOnItem(point.Position, itemsControl);

                if (isClickOnItem)
                {
                    Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerPressed] Clicked on an item container");

                    // Получаем элемент под курсором для hover-выделения
                    var clickedItem = GetItemAtPoint(point.Position, itemsControl);
                    if (clickedItem != null && SingleClickOpenItem)
                    {
                        _hoveredItem = clickedItem;
                        RestartHoverTimer();
                    }

                    e.Handled = false;
                }
                else
                {
                    Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerPressed] Clicked on empty space");

                    // Запоминаем время клика на пустое место для защиты от восстановления выделения
                    _lastEmptySpaceClickTime = DateTime.Now;
                    Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerPressed] _lastEmptySpaceClickTime set to: {_lastEmptySpaceClickTime:HH:mm:ss.fff}");

                    // ПОЛНЫЙ СБРОС ВСЕГО СОСТОЯНИЯ НАВЕДЕНИЯ
                    StopHoverTimer();
                    _hoveredItem = null;
                    _lastEditEndTime = DateTime.MinValue;

                    _isDragSelecting = true;
                    itemsControl.CapturePointer(e.Pointer);
                    CreateSelectionRectangle();
                    UpdateSelectionRectangle(_dragStartPoint, _dragStartPoint);

                    if (!_isCtrlPressed)
                    {
                        itemsControl.SelectedItems.Clear();
                        Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerPressed] Cleared selection");

                        // Явно устанавливаем SelectedItem в null
                        itemsControl.SelectedItem = null;
                    }

                    e.Handled = true;

                    // ВАЖНО: Возвращаем фокус на ItemsControl после клика на пустое место
                    _ = DispatcherQueue.TryEnqueue(() =>
                    {
                        Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerPressed] Returning focus to items control");
                        itemsControl.Focus(FocusState.Programmatic);
                    });
                }
            }
        }

        private void ItemsControl_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerReleased] Entering");
            UpdateModifierKeyStateFromCore();

            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl) return;

            var point = e.GetCurrentPoint(itemsControl);

            if (!point.Properties.IsLeftButtonPressed)
            {
                _isLeftMouseButtonPressed = false;
                _isMouseMovingWithButton = false;

                if (SingleClickOpenItem)
                {
                    _wasClickHandled = false;
                }

                if (itemsControl.PointerCaptures != null && itemsControl.PointerCaptures.Count > 0)
                {
                    itemsControl.ReleasePointerCapture(e.Pointer);
                    Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerReleased] Pointer released");
                }
            }

            if (_isDragSelecting)
            {
                Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerReleased] Ending drag selection");
                _isDragSelecting = false;
                _wasClickHandled = false;

                if (itemsControl.PointerCaptures != null && itemsControl.PointerCaptures.Count > 0)
                {
                    itemsControl.ReleasePointerCapture(e.Pointer);
                }

                RemoveSelectionRectangle();
                e.Handled = true;

                // ВАЖНО: Возвращаем фокус на ItemsControl после завершения drag selection
                _ = DispatcherQueue.TryEnqueue(() =>
                {
                    Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerReleased] Returning focus to items control");
                    itemsControl.Focus(FocusState.Programmatic);
                });
            }
        }

        private void ItemsControl_PointerMovedForDrag(object sender, PointerRoutedEventArgs e)
        {
            UpdateModifierKeyStateFromCore();

            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl) return;

            var point = e.GetCurrentPoint(itemsControl);

            if (_isLeftMouseButtonPressed && point.Properties.IsLeftButtonPressed)
            {
                var currentPosition = point.Position;

                float distance = Vector2.Distance(_dragStartPoint,
                    new Vector2((float)currentPosition.X, (float)currentPosition.Y));

                if (distance > 3.0f && !_isMouseMovingWithButton)
                {
                    _isMouseMovingWithButton = true;
                    Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerMovedForDrag] Mouse moved with button, distance={distance}");
                }

                if (distance > 10.0f && !_isDragSelecting)
                {
                    Debug.WriteLine($"[{PanelId}] [ItemsControl_PointerMovedForDrag] Starting drag selection, distance={distance}");
                    _isDragSelecting = true;
                    itemsControl.CapturePointer(e.Pointer);
                    CreateSelectionRectangle();
                    UpdateSelectionRectangle(_dragStartPoint,
                        new Vector2((float)currentPosition.X, (float)currentPosition.Y));

                    if (!_isCtrlPressed)
                    {
                        itemsControl.SelectedItems.Clear();
                        // Явно устанавливаем SelectedItem в null
                        itemsControl.SelectedItem = null;
                    }
                }

                if (_isDragSelecting)
                {
                    var currentPoint = new Vector2((float)point.Position.X, (float)point.Position.Y);
                    UpdateSelectionRectangle(_dragStartPoint, currentPoint);
                    PerformRectangleSelection(_dragStartPoint, currentPoint, itemsControl, true);
                    e.Handled = true;
                }
            }
        }

        private void CreateSelectionRectangle()
        {
            Debug.WriteLine($"[{PanelId}] [CreateSelectionRectangle] Creating selection rectangle");
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
                Debug.WriteLine($"[{PanelId}] [CreateSelectionRectangle] Rectangle added to canvas");
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

            Debug.WriteLine($"[{PanelId}] [UpdateSelectionRectangle] Rect: left={left}, top={top}, width={width}, height={height}");
        }

        private void RemoveSelectionRectangle()
        {
            Debug.WriteLine($"[{PanelId}] [RemoveSelectionRectangle] Removing selection rectangle");
            if (_selectionRectangle != null && _selectionCanvas != null)
            {
                _selectionCanvas.Children.Remove(_selectionRectangle);
                _selectionRectangle = null;
            }
        }

        private void PerformRectangleSelection(Vector2 startPoint, Vector2 endPoint, ListViewBase itemsControl, bool applyImmediately = false)
        {
            if (Items.Count == 0) return;

            float left = Math.Min(startPoint.X, endPoint.X);
            float right = Math.Max(startPoint.X, endPoint.X);
            float top = Math.Min(startPoint.Y, endPoint.Y);
            float bottom = Math.Max(startPoint.Y, endPoint.Y);

            var newSelectedIndices = new HashSet<int>();

            var panel = itemsControl.ItemsPanelRoot as ItemsWrapGrid;
            if (panel != null)
            {
                foreach (var child in panel.Children)
                {
                    if (child is FrameworkElement container && container.Visibility == Visibility.Visible)
                    {
                        int index = itemsControl.IndexFromContainer(container);
                        if (index >= 0 && index < Items.Count)
                        {
                            var transform = container.TransformToVisual(itemsControl);
                            var position = transform.TransformPoint(new Windows.Foundation.Point(0, 0));

                            float itemLeft = (float)position.X;
                            float itemTop = (float)position.Y;
                            float itemRight = itemLeft + (float)container.ActualWidth;
                            float itemBottom = itemTop + (float)container.ActualHeight;

                            bool intersects = itemRight > left && itemLeft < right &&
                                              itemBottom > top && itemTop < bottom;

                            if (intersects)
                            {
                                newSelectedIndices.Add(index);
                            }
                            else if (!_isCtrlPressed && applyImmediately)
                            {
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

            if (applyImmediately)
            {
                int added = 0;
                foreach (int index in newSelectedIndices)
                {
                    if (index >= 0 && index < Items.Count)
                    {
                        var item = Items[index];
                        if (!itemsControl.SelectedItems.Contains(item))
                        {
                            itemsControl.SelectedItems.Add(item);
                            added++;
                        }
                    }
                }
                Debug.WriteLine($"[{PanelId}] [PerformRectangleSelection] Added {added} items to selection");
                UpdateSelectionVisual(itemsControl);
            }
            else
            {
                _tempSelectedIndices = newSelectedIndices;
                Debug.WriteLine($"[{PanelId}] [PerformRectangleSelection] Stored {newSelectedIndices.Count} temporary indices");
            }
        }

        private void ItemsControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl) return;

            Debug.WriteLine($"[{PanelId}] [ItemsControl_SelectionChanged] Selection changed. New count: {itemsControl.SelectedItems.Count}");

            foreach (ExplorerItemViewModel addedItem in e.AddedItems)
            {
                Debug.WriteLine($"[{PanelId}] [ItemsControl_SelectionChanged] [+] Added: {addedItem?.Name} (Hash={addedItem?.GetHashCode()})");
            }

            foreach (ExplorerItemViewModel removedItem in e.RemovedItems)
            {
                Debug.WriteLine($"[{PanelId}] [ItemsControl_SelectionChanged] [-] Removed: {removedItem?.Name} (Hash={removedItem?.GetHashCode()})");
                if (removedItem?.Name == "..")
                {
                    StopHoverTimer();
                    _hoveredItem = null;
                }
            }

            UpdateUIForSelection();
        }

        #endregion

        #region Выделение элементов (логика)

        private void SelectItemOnHover(ExplorerItemViewModel item)
        {
            Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] ========== START ==========");
            Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Entering with item={(item != null ? $"'{item.Name}' (Hash={item.GetHashCode()})" : "null")}");
            Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] AnyEditing={Items.Any(i => i.IsEditing)}");
            Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Current time: {DateTime.Now:HH:mm:ss.fff}");
            Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] _lastEditEndTime: {_lastEditEndTime:HH:mm:ss.fff}");
            Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Time since last edit: {(DateTime.Now - _lastEditEndTime).TotalMilliseconds}ms");
            Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] EditCooldownMs: {EditCooldownMs}ms");

            // Защитный интервал после редактирования
            if ((DateTime.Now - _lastEditEndTime).TotalMilliseconds < EditCooldownMs)
            {
                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] IGNORING - edit cooldown active");
                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] ========== END (cooldown) ==========");
                return;
            }

            if (Items.Any(i => i.IsEditing))
            {
                var editingItem = Items.FirstOrDefault(i => i.IsEditing);
                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] IGNORING - another item is editing: {(editingItem != null ? editingItem.Name : "unknown")}");
                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] ========== END (editing) ==========");
                return;
            }

            if (Interlocked.CompareExchange(ref _hoverSelectionLock, 1, 0) != 0)
            {
                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] IGNORING - lock already taken");
                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] ========== END (lock) ==========");
                return;
            }

            Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Lock acquired successfully");

            try
            {
                UpdateModifierKeyStateFromCore();
                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Modifier keys after update: Ctrl={_isCtrlPressed}, Shift={_isShiftPressed}, Alt={_isAltPressed}");

                if (item == null)
                {
                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] ABORT - item is null");
                    return;
                }

                if (_isDragSelecting)
                {
                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] ABORT - drag selecting in progress");
                    return;
                }

                if (_currentItemsControl == null)
                {
                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] ABORT - _currentItemsControl is null");
                    return;
                }

                if (item.Name == "..")
                {
                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] SKIP - item is '..'");
                    return;
                }

                // Проверяем, существует ли элемент в коллекции
                int itemIndex = Items.IndexOf(item);
                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Item index in collection: {itemIndex}");

                if (itemIndex < 0)
                {
                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] WARNING - item not found in collection!");
                    // Попробуем найти по пути или имени
                    var foundItem = Items.FirstOrDefault(i => i.FilePath == item.FilePath || i.Name == item.Name);
                    if (foundItem != null)
                    {
                        Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Found alternative item: '{foundItem.Name}' (Hash={foundItem.GetHashCode()})");
                        item = foundItem;
                        Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Using alternative item");
                    }
                    else
                    {
                        Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] No alternative found, aborting");
                        return;
                    }
                }

                // Проверка на зажатые клавиши-модификаторы
                bool isCtrlPressed = _isCtrlPressed;
                bool isShiftPressed = _isShiftPressed;

                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Processing selection for '{item.Name}' (Hash={item.GetHashCode()})");
                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover]   Shift={isShiftPressed}, Ctrl={isCtrlPressed}");
                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover]   _shiftSelectionStartItem={(_shiftSelectionStartItem != null ? _shiftSelectionStartItem.Name : "null")}");

                // Диагностика текущего выделения
                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Current selection before change:");
                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover]   SelectedItems.Count={_currentItemsControl.SelectedItems.Count}");
                if (_currentItemsControl.SelectedItem is ExplorerItemViewModel currentSel)
                {
                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover]   SelectedItem: '{currentSel.Name}' (Hash={currentSel.GetHashCode()})");
                }
                else
                {
                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover]   SelectedItem: null");
                }

                // Обработка Shift+клик (выделение диапазона)
                if (isShiftPressed)
                {
                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Shift+click mode");

                    if (_shiftSelectionStartItem == null)
                    {
                        _shiftSelectionStartItem = _currentItemsControl.SelectedItem as ExplorerItemViewModel
                                                    ?? Items.FirstOrDefault();
                        Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Shift start item set to {_shiftSelectionStartItem?.Name}");
                    }

                    if (_shiftSelectionStartItem != null)
                    {
                        int startIndex = Items.IndexOf(_shiftSelectionStartItem);
                        int endIndex = Items.IndexOf(item);
                        Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Shift range: start={startIndex}, end={endIndex}");

                        if (startIndex >= 0 && endIndex >= 0)
                        {
                            Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Calling SelectRange({startIndex}, {endIndex})");
                            SelectRange(startIndex, endIndex);
                        }
                        else
                        {
                            Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Invalid indices, skipping");
                        }
                    }
                }
                // Обработка Ctrl+клик (добавление/удаление из выделения)
                else if (isCtrlPressed)
                {
                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Ctrl+click mode");

                    bool currentlySelected = _currentItemsControl.SelectedItems.Contains(item);
                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Item currently selected: {currentlySelected}");

                    if (currentlySelected)
                    {
                        _currentItemsControl.SelectedItems.Remove(item);
                        Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Removed {item.Name} from selection");
                    }
                    else
                    {
                        _currentItemsControl.SelectedItems.Add(item);
                        Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Added {item.Name} to selection");
                    }

                    // При Ctrl+клике не сбрасываем _shiftSelectionStartItem
                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] _shiftSelectionStartItem unchanged: {(_shiftSelectionStartItem != null ? _shiftSelectionStartItem.Name : "null")}");
                }
                // Обычный клик (сброс выделения и выбор одного элемента)
                else
                {
                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Normal hover selection mode");

                    // Сохраняем старые значения для диагностики
                    var oldSelectedItems = _currentItemsControl.SelectedItems.Cast<ExplorerItemViewModel>().ToList();
                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Clearing {oldSelectedItems.Count} previously selected items");
                    foreach (var oldItem in oldSelectedItems)
                    {
                        Debug.WriteLine($"[{PanelId}] [SelectItemOnHover]   - {oldItem.Name} (Hash={oldItem.GetHashCode()})");
                    }

                    _currentItemsControl.SelectedItems.Clear();
                    _currentItemsControl.SelectedItems.Add(item);
                    _currentItemsControl.SelectedItem = item;
                    _shiftSelectionStartItem = item;

                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Single selection set to {item.Name} (Hash={item.GetHashCode()})");
                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] _shiftSelectionStartItem updated to {item.Name}");

                    // Диагностика после изменения
                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] New selection state:");
                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover]   SelectedItems.Count={_currentItemsControl.SelectedItems.Count}");
                    if (_currentItemsControl.SelectedItem is ExplorerItemViewModel newSel)
                    {
                        Debug.WriteLine($"[{PanelId}] [SelectItemOnHover]   SelectedItem: '{newSel.Name}' (Hash={newSel.GetHashCode()})");
                        Debug.WriteLine($"[{PanelId}] [SelectItemOnHover]   SelectedItem matches hovered: {ReferenceEquals(newSel, item)}");
                    }

                    // Проверяем возможность редактирования
                    bool canEdit = !item.IsEditing &&
                                   !string.IsNullOrEmpty(item.FilePath) &&
                                   !item.IsMyComputer &&
                                   !item.IsTreeViewNode &&
                                   !item.IsSpecialFolderNode &&
                                   (File.Exists(item.FilePath) || Directory.Exists(item.FilePath));

                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover]   CanEdit: {canEdit}");
                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover]   SaveEditCommand can execute: {item.SaveEditCommand?.CanExecute(null)}");

                    // ВАЖНО: Возвращаем фокус на ItemsControl после выделения
                    Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Scheduling focus return to items control");
                    _ = DispatcherQueue.TryEnqueue(() =>
                    {
                        Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Returning focus to items control (executing)");
                        bool focusResult = _currentItemsControl.Focus(FocusState.Programmatic);
                        Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Focus result: {focusResult}");

                        // Проверяем, кто теперь имеет фокус
                        var focusedElement = FocusManager.GetFocusedElement();
                        Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Focused element: {focusedElement?.GetType().Name}");
                    });
                }

                // Итоговая диагностика
                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Final selection state:");
                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover]   SelectedItems.Count={_currentItemsControl.SelectedItems.Count}");
                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover]   SelectedItem: {(_currentItemsControl.SelectedItem != null ? ((ExplorerItemViewModel)_currentItemsControl.SelectedItem).Name : "null")}");
                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover]   _shiftSelectionStartItem={(_shiftSelectionStartItem != null ? _shiftSelectionStartItem.Name : "null")}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] EXCEPTION: {ex.Message}");
                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] StackTrace: {ex.StackTrace}");
            }
            finally
            {
                Interlocked.Exchange(ref _hoverSelectionLock, 0);
                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] Lock released");
                Debug.WriteLine($"[{PanelId}] [SelectItemOnHover] ========== END ==========");
            }
        }

        private void SelectRange(int startIndex, int endIndex)
        {
            Debug.WriteLine($"[{PanelId}] [SelectRange] start={startIndex}, end={endIndex}");
            if (startIndex < 0 || endIndex < 0 || startIndex >= Items.Count || endIndex >= Items.Count)
            {
                Debug.WriteLine($"[{PanelId}] [SelectRange] Invalid indices, returning");
                return;
            }

            int minIndex = Math.Min(startIndex, endIndex);
            int maxIndex = Math.Max(startIndex, endIndex);

            if (!_isCtrlPressed)
            {
                _currentItemsControl.SelectedItems.Clear();
                Debug.WriteLine($"[{PanelId}] [SelectRange] Cleared selection (Ctrl not pressed)");
            }

            int added = 0;
            for (int i = minIndex; i <= maxIndex; i++)
            {
                if (!_currentItemsControl.SelectedItems.Contains(Items[i]))
                {
                    _currentItemsControl.SelectedItems.Add(Items[i]);
                    added++;
                }
            }
            Debug.WriteLine($"[{PanelId}] [SelectRange] Added {added} items");

            _currentItemsControl.ScrollIntoView(Items[endIndex]);
        }

        private void ToggleCurrentSelection()
        {
            Debug.WriteLine($"[{PanelId}] [ToggleCurrentSelection] Entering");
            if (_currentItemsControl.SelectedItem is ExplorerItemViewModel currentItem)
            {
                if (_currentItemsControl.SelectedItems.Contains(currentItem))
                {
                    _currentItemsControl.SelectedItems.Remove(currentItem);
                    Debug.WriteLine($"[{PanelId}] [ToggleCurrentSelection] Removed {currentItem.Name} from selection");
                }
                else
                {
                    _currentItemsControl.SelectedItems.Add(currentItem);
                    Debug.WriteLine($"[{PanelId}] [ToggleCurrentSelection] Added {currentItem.Name} to selection");
                }
            }
        }

        #endregion

        #region Обработка кликов и двойных кликов

        private async void ItemsControl_OnItemClick(object sender, ItemClickEventArgs e)
        {
            Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] ENTER");
            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl)
            {
                Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Sender is not current control, ignoring");
                return;
            }

            UpdateModifierKeyStateFromCore();

            var now = DateTime.Now;
            if ((now - _lastClickTime).TotalMilliseconds < 300)
            {
                Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Click throttled (too fast)");
                return;
            }
            _lastClickTime = now;

            Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] SingleClickOpenItem={SingleClickOpenItem}, Ctrl={_isCtrlPressed}, Shift={_isShiftPressed}");

            if (e.ClickedItem is not ExplorerItemViewModel item)
            {
                Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] ClickedItem is not ExplorerItemViewModel");
                return;
            }

            Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Clicked item: {item.Name}, Path={item.FilePath}");

            int clickedIndex = Items.IndexOf(item);
            if (clickedIndex < 0)
            {
                Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Clicked index not found in Items");
                return;
            }

            bool isSingleClickMode = SingleClickOpenItem;
            bool isCtrlPressed = _isCtrlPressed;
            bool isShiftPressed = _isShiftPressed;

            if (!isSingleClickMode)
            {
                if (_wasClickHandled)
                {
                    Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Click already handled, skipping");
                    return;
                }
                _wasClickHandled = true;
                Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] _wasClickHandled set to true");
            }

            if (isSingleClickMode)
            {
                if (isShiftPressed)
                {
                    Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Shift+click");
                    if (_shiftSelectionStartItem == null)
                    {
                        _shiftSelectionStartItem = _currentItemsControl.SelectedItem as ExplorerItemViewModel;
                        if (_shiftSelectionStartItem == null && Items.Count > 0)
                        {
                            _shiftSelectionStartItem = Items[0];
                        }
                        Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Shift start item set to {_shiftSelectionStartItem?.Name}");
                    }

                    if (_shiftSelectionStartItem != null)
                    {
                        int startIndex = Items.IndexOf(_shiftSelectionStartItem);
                        int endIndex = clickedIndex;
                        Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Selecting range {startIndex}-{endIndex}");
                        SelectRange(startIndex, endIndex);
                    }
                }
                else if (isCtrlPressed)
                {
                    Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Ctrl+click");
                    if (_currentItemsControl.SelectedItems.Contains(item))
                    {
                        _currentItemsControl.SelectedItems.Remove(item);
                        Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Removed from selection");
                    }
                    else
                    {
                        _currentItemsControl.SelectedItems.Add(item);
                        Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Added to selection");
                    }
                }
                else
                {
                    Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Single click open mode, selecting and opening");
                    _currentItemsControl.SelectedItems.Clear();
                    _currentItemsControl.SelectedItem = item;
                    _shiftSelectionStartItem = item;

                    await OpenItemByIndex(clickedIndex);
                }
            }
            else
            {
                // Double-click mode (single click for selection only)
                if (isShiftPressed)
                {
                    Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Shift+click (selection mode)");
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
                    Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Ctrl+click (selection mode)");
                    if (_currentItemsControl.SelectedItems.Contains(item))
                    {
                        _currentItemsControl.SelectedItems.Remove(item);
                    }
                    else
                    {
                        _currentItemsControl.SelectedItems.Add(item);
                    }
                }
                else
                {
                    Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Normal click (selection mode)");
                    _currentItemsControl.SelectedItems.Clear();
                    _currentItemsControl.SelectedItem = item;
                    _shiftSelectionStartItem = item;

                    var currentTime = DateTime.Now;
                    bool isDoubleClick = (_lastClickedItem == item &&
                                         (currentTime - _lastClickTime).TotalMilliseconds < 500);

                    if (isDoubleClick)
                    {
                        Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Double-click detected, opening");
                        await OpenItemByIndex(clickedIndex);
                        _lastClickedItem = null;
                        _lastClickTime = DateTime.MinValue;
                    }
                    else
                    {
                        Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Single click, setting up for possible double-click");
                        _lastClickedItem = item;
                        _lastClickTime = currentTime;

                        _ = this.DispatcherQueue.EnqueueAsync(async () =>
                        {
                            await Task.Delay(500);
                            _wasClickHandled = false;
                            Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] Delayed: _wasClickHandled reset to false");
                        }, Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
                    }
                }
            }

            Debug.WriteLine($"[{PanelId}] [ItemsControl_OnItemClick] EXIT");
        }

        private async void ItemsControl_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            Debug.WriteLine($"[{PanelId}] [ItemsControl_DoubleTapped] Entering");
            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl) return;

            if (SingleClickOpenItem)
            {
                Debug.WriteLine($"[{PanelId}] [ItemsControl_DoubleTapped] SingleClickOpenItem=true, ignoring double-tap");
                return;
            }

            var element = e.OriginalSource as FrameworkElement;
            while (element != null && element.DataContext as ExplorerItemViewModel == null)
            {
                element = VisualTreeHelper.GetParent(element) as FrameworkElement;
            }

            if (element?.DataContext is ExplorerItemViewModel item)
            {
                int index = Items.IndexOf(item);
                Debug.WriteLine($"[{PanelId}] [ItemsControl_DoubleTapped] Double-tapped on {item.Name}, index={index}");
                if (index >= 0)
                {
                    await OpenItemByIndex(index);
                }
            }
        }

        #endregion

        #region Обработка клавиатуры и навигация клавишами

        private void ItemsControl_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] ========== START ==========");
            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] Key={e.Key}, OriginalKey={e.OriginalKey}");

            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl)
            {
                Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] Sender is not current control, ignoring");
                Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] ========== END (wrong control) ==========");
                return;
            }

            UpdateModifierKeyState(e.Key, true);

            bool isCtrlPressed = _isCtrlPressed;
            bool isShiftPressed = _isShiftPressed;

            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] Ctrl={isCtrlPressed}, Shift={isShiftPressed}, Alt={_isAltPressed}");
            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] Items.Count={Items.Count}");
            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] SelectedItems.Count={_currentItemsControl.SelectedItems.Count}");

            if (_currentItemsControl.SelectedItem is ExplorerItemViewModel selectedItem)
            {
                Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] SelectedItem: '{selectedItem.Name}' (Index={_currentItemsControl.SelectedIndex}, Hash={selectedItem.GetHashCode()})");
                Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] SelectedItem.IsEditing={selectedItem.IsEditing}");
            }
            else
            {
                Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] SelectedItem: null");
            }

            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] _hoveredItem={(_hoveredItem != null ? $"'{_hoveredItem.Name}' (Hash={_hoveredItem.GetHashCode()})" : "null")}");
            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] _isDragSelecting={_isDragSelecting}");
            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] _lastEditEndTime={_lastEditEndTime:HH:mm:ss.fff}, cooldown={EditCooldownMs}ms");
            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] _shiftSelectionStartItem={(_shiftSelectionStartItem != null ? _shiftSelectionStartItem.Name : "null")}");
            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] _isMultiRenameMode={_isMultiRenameMode}");

            // Если мы в режиме множественного переименования, большинство клавиш игнорируем
            if (_isMultiRenameMode)
            {
                Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] In multi-rename mode, most keys are ignored");
                // Разрешаем только Escape для выхода из режима (обрабатывается в TextBox)
                e.Handled = true;
                Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] ========== END (multi-rename mode) ==========");
                return;
            }

            switch (e.Key)
            {
                case VirtualKey.A when isCtrlPressed && !isShiftPressed:
                    Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] Ctrl+A detected - selecting all");
                    _currentItemsControl.SelectAll();
                    e.Handled = true;
                    Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] Ctrl+A executed, new SelectedItems.Count={_currentItemsControl.SelectedItems.Count}");
                    break;

                case VirtualKey.Space when isCtrlPressed:
                    Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] Ctrl+Space detected - toggling selection");
                    ToggleCurrentSelection();
                    e.Handled = true;
                    break;

                case VirtualKey.Enter:
                    Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] Enter detected");
                    if (_currentItemsControl.SelectedItem != null)
                    {
                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] Enter: Opening selected item");
                        OpenSelectedItem();
                        e.Handled = true;
                    }
                    else
                    {
                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] Enter ignored - no selected item");
                    }
                    break;

                case VirtualKey.F2:
                    Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] F2 detected - checking conditions");

                    // Если уже в режиме множественного переименования – игнорируем повторное нажатие
                    if (_isMultiRenameMode)
                    {
                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] Already in multi-rename mode, ignoring F2");
                        e.Handled = true;
                        break;
                    }

                    // Диагностика перед принятием решения
                    Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] F2 Decision Tree:");
                    Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]   Condition 1: SelectedItems.Count == 1 ? {_currentItemsControl.SelectedItems.Count == 1}");
                    Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]   Condition 2: _hoveredItem != null ? {_hoveredItem != null}");
                    Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]   Condition 3: !_isDragSelecting ? {!_isDragSelecting}");

                    if (_hoveredItem != null)
                    {
                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]   Hovered item exists in Items? {Items.Contains(_hoveredItem)}");
                        int hoveredIndex = Items.IndexOf(_hoveredItem);
                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]   Hovered item index: {hoveredIndex}");

                        // Проверяем, совпадает ли хэш с элементом в коллекции
                        if (hoveredIndex >= 0)
                        {
                            var itemFromCollection = Items[hoveredIndex];
                            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]   Hovered hash: {_hoveredItem.GetHashCode()}, Collection item hash: {itemFromCollection.GetHashCode()}");
                            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]   Same instance: {ReferenceEquals(_hoveredItem, itemFromCollection)}");
                        }
                    }

                    // Если выделено более одного элемента
                    if (_currentItemsControl.SelectedItems.Count > 1)
                    {
                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] F2: Multiple selection ({_currentItemsControl.SelectedItems.Count} items) - starting multi-rename");
                        StartMultiRename();
                        e.Handled = true;
                        break;
                    }

                    if (_currentItemsControl.SelectedItems.Count == 1)
                    {
                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] F2: Case 1 - Rename selected item");
                        if (_currentItemsControl.SelectedItem is ExplorerItemViewModel selItem)
                        {
                            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]   Selected item details:");
                            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]     Name: '{selItem.Name}'");
                            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]     Path: '{selItem.FilePath}'");
                            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]     Index: {_currentItemsControl.SelectedIndex}");
                            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]     Hash: {selItem.GetHashCode()}");
                            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]     IsEditing: {selItem.IsEditing}");
                            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]     CanEdit: {selItem.SaveEditCommand?.CanExecute(null)}");
                        }
                        RenameSelectedItem();
                        e.Handled = true;
                    }
                    else if (_hoveredItem != null && !_isDragSelecting)
                    {
                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] F2: Case 2 - No selection, using hovered item");
                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]   Hovered item details:");
                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]     Name: '{_hoveredItem.Name}'");
                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]     Path: '{_hoveredItem.FilePath}'");
                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]     Hash: {_hoveredItem.GetHashCode()}");
                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]     IsEditing: {_hoveredItem.IsEditing}");

                        int hoveredIndex = Items.IndexOf(_hoveredItem);
                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]     Index in collection: {hoveredIndex}");

                        // Проверяем возможность редактирования
                        bool canEdit = !_hoveredItem.IsEditing &&
                                       !string.IsNullOrEmpty(_hoveredItem.FilePath) &&
                                       !_hoveredItem.IsMyComputer &&
                                       !_hoveredItem.IsTreeViewNode &&
                                       !_hoveredItem.IsSpecialFolderNode &&
                                       (File.Exists(_hoveredItem.FilePath) || Directory.Exists(_hoveredItem.FilePath));

                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]     CanEdit: {canEdit}");
                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]     SaveEditCommand can execute: {_hoveredItem.SaveEditCommand?.CanExecute(null)}");

                        // Сохраняем старые значения для диагностики
                        var oldSelectedItems = _currentItemsControl.SelectedItems.Cast<ExplorerItemViewModel>().ToList();
                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]   Old selection count: {oldSelectedItems.Count}");
                        foreach (var oldItem in oldSelectedItems)
                        {
                            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]     Old selected: '{oldItem.Name}' (Hash={oldItem.GetHashCode()})");
                        }

                        // Выделяем элемент под курсором
                        _currentItemsControl.SelectedItems.Clear();
                        _currentItemsControl.SelectedItems.Add(_hoveredItem);
                        _currentItemsControl.SelectedItem = _hoveredItem;
                        _shiftSelectionStartItem = _hoveredItem;

                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]   New selection set to hovered item");
                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]   New SelectedItems.Count: {_currentItemsControl.SelectedItems.Count}");

                        // Проверяем, что выделение установилось правильно
                        if (_currentItemsControl.SelectedItem is ExplorerItemViewModel newSelected)
                        {
                            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]   New SelectedItem: '{newSelected.Name}' (Hash={newSelected.GetHashCode()})");
                            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]   Same as hovered: {ReferenceEquals(newSelected, _hoveredItem)}");
                        }

                        RenameSelectedItem();
                        e.Handled = true;
                    }
                    else
                    {
                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] F2 ignored - conditions not met");
                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]   SelectedItems.Count: {_currentItemsControl.SelectedItems.Count}");
                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]   _hoveredItem: {(_hoveredItem != null ? _hoveredItem.Name : "null")}");
                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown]   _isDragSelecting: {_isDragSelecting}");
                    }
                    break;

                case VirtualKey.Delete:
                    Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] Delete detected");
                    if (_currentItemsControl.SelectedItems.Count > 0)
                    {
                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] Delete: Deleting {_currentItemsControl.SelectedItems.Count} selected items");
                        DeleteSelectedItems();
                        e.Handled = true;
                    }
                    else
                    {
                        Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] Delete ignored - no selected items");
                    }
                    break;

                case VirtualKey.Up:
                case VirtualKey.Down:
                case VirtualKey.Left:
                case VirtualKey.Right:
                    Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] Arrow key detected: {e.Key}");
                    HandleArrowKeyNavigation(e.Key, isCtrlPressed, isShiftPressed);
                    e.Handled = true;
                    break;

                case VirtualKey.Home:
                case VirtualKey.End:
                    Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] Home/End key detected: {e.Key}");
                    HandleHomeEndNavigation(e.Key, isCtrlPressed, isShiftPressed);
                    e.Handled = true;
                    break;

                case VirtualKey.PageUp:
                case VirtualKey.PageDown:
                    Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] PageUp/PageDown key detected: {e.Key}");
                    HandlePageNavigation(e.Key, isCtrlPressed, isShiftPressed);
                    e.Handled = true;
                    break;

                default:
                    Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] Unhandled key: {e.Key}");
                    break;
            }

            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyDown] ========== END ==========");
        }

        private void ItemsControl_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            Debug.WriteLine($"[{PanelId}] [ItemsControl_KeyUp] Key={e.Key}");
            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl) return;

            UpdateModifierKeyState(e.Key, false);
        }

        private void ItemsControl_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            Debug.WriteLine($"[{PanelId}] [ItemsControl_PreviewKeyDown] Key={e.Key}");
        }

        private void UpdateModifierKeyState(VirtualKey key, bool isPressed)
        {
            bool oldCtrl = _isCtrlPressed, oldShift = _isShiftPressed, oldAlt = _isAltPressed;
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
            if (oldCtrl != _isCtrlPressed || oldShift != _isShiftPressed || oldAlt != _isAltPressed)
            {
                Debug.WriteLine($"[{PanelId}] [UpdateModifierKeyState] Key={key}, Pressed={isPressed} => Ctrl={_isCtrlPressed}, Shift={_isShiftPressed}, Alt={_isAltPressed}");
            }
        }

        private void HandleArrowKeyNavigation(VirtualKey key, bool isCtrlPressed, bool isShiftPressed)
        {
            Debug.WriteLine($"[{PanelId}] [HandleArrowKeyNavigation] Key={key}, Ctrl={isCtrlPressed}, Shift={isShiftPressed}");
            if (_currentItemsControl == null) return;

            int currentIndex = _currentItemsControl.SelectedIndex;
            int newIndex = currentIndex;

            if (_currentItemsControl == ItemsListView)
            {
                int itemsPerColumn = CalculateItemsPerColumnForListView();
                Debug.WriteLine($"[{PanelId}] [HandleArrowKeyNavigation] ListView mode, itemsPerColumn={itemsPerColumn}");

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
                int itemsPerRow = CalculateItemsPerRowForGridView();
                Debug.WriteLine($"[{PanelId}] [HandleArrowKeyNavigation] GridView mode, itemsPerRow={itemsPerRow}");

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
                Debug.WriteLine($"[{PanelId}] [HandleArrowKeyNavigation] Moving from {currentIndex} to {newIndex}, item={newItem.Name}");

                if (isShiftPressed)
                {
                    HandleShiftArrowSelection(newIndex);
                }
                else if (isCtrlPressed)
                {
                    _currentItemsControl.SelectedItem = newItem;
                    _currentItemsControl.ScrollIntoView(newItem);
                    Debug.WriteLine($"[{PanelId}] [HandleArrowKeyNavigation] Ctrl+arrow: set selected item");
                }
                else
                {
                    _currentItemsControl.SelectedItems.Clear();
                    _currentItemsControl.SelectedItem = newItem;
                    _currentItemsControl.ScrollIntoView(newItem);
                    _shiftSelectionStartItem = newItem;
                    Debug.WriteLine($"[{PanelId}] [HandleArrowKeyNavigation] Normal arrow: cleared and set selected item");
                }
            }
            else
            {
                Debug.WriteLine($"[{PanelId}] [HandleArrowKeyNavigation] No movement (index unchanged or out of range)");
            }
        }

        private void HandleHomeEndNavigation(VirtualKey key, bool isCtrlPressed, bool isShiftPressed)
        {
            Debug.WriteLine($"[{PanelId}] [HandleHomeEndNavigation] Key={key}, Ctrl={isCtrlPressed}, Shift={isShiftPressed}");
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
                Debug.WriteLine($"[{PanelId}] [HandleHomeEndNavigation] Moving to index {newIndex}, item={newItem.Name}");

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
            }
        }

        private void HandlePageNavigation(VirtualKey key, bool isCtrlPressed, bool isShiftPressed)
        {
            Debug.WriteLine($"[{PanelId}] [HandlePageNavigation] Key={key}, Ctrl={isCtrlPressed}, Shift={isShiftPressed}");
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
                Debug.WriteLine($"[{PanelId}] [HandlePageNavigation] From {currentIndex} to {newIndex}, item={newItem.Name}");

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
            }
        }

        private int CalculateItemsPerColumnForListView()
        {
            return MaxRowsOrColumns;
        }

        private int CalculateItemsPerRowForGridView()
        {
            if (ItemWidth <= 0) return 1;

            var grid = ItemsGridView.ItemsPanelRoot as ItemsWrapGrid;
            if (grid != null && grid.ActualWidth > 0)
            {
                int perRow = (int)Math.Floor(grid.ActualWidth / ItemWidth);
                Debug.WriteLine($"[{PanelId}] [CalculateItemsPerRowForGridView] Calculated: {perRow} (ActualWidth={grid.ActualWidth}, ItemWidth={ItemWidth})");
                return perRow;
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
                int perPage;
                if (itemsControl == ItemsListView)
                {
                    perPage = rowsPerPage * CalculateItemsPerColumnForListView();
                }
                else
                {
                    perPage = rowsPerPage * CalculateItemsPerRowForGridView();
                }
                Debug.WriteLine($"[{PanelId}] [CalculateItemsPerPage] rowsPerPage={rowsPerPage}, perPage={perPage}");
                return perPage;
            }

            return 20;
        }

        private void HandleShiftArrowSelection(int newIndex)
        {
            Debug.WriteLine($"[{PanelId}] [HandleShiftArrowSelection] newIndex={newIndex}");
            if (_shiftSelectionStartItem == null)
            {
                _shiftSelectionStartItem = _currentItemsControl.SelectedItem as ExplorerItemViewModel;
                if (_shiftSelectionStartItem == null && Items.Count > 0)
                {
                    _shiftSelectionStartItem = Items[0];
                }
                Debug.WriteLine($"[{PanelId}] [HandleShiftArrowSelection] Shift start set to {_shiftSelectionStartItem?.Name}");
            }

            if (_shiftSelectionStartItem != null)
            {
                int startIndex = Items.IndexOf(_shiftSelectionStartItem);
                int endIndex = newIndex;
                Debug.WriteLine($"[{PanelId}] [HandleShiftArrowSelection] Selecting range {startIndex}-{endIndex}");
                SelectRange(startIndex, endIndex);
            }
        }

        private void HandleShiftRangeSelection(int newIndex)
        {
            int currentIndex = _currentItemsControl.SelectedIndex;
            Debug.WriteLine($"[{PanelId}] [HandleShiftRangeSelection] currentIndex={currentIndex}, newIndex={newIndex}");
            if (currentIndex >= 0)
            {
                SelectRange(currentIndex, newIndex);
            }
        }

        #endregion

        #region Операции с элементами (Open, Rename, Delete)

        private async Task OpenItem(ExplorerItemViewModel item)
        {
            Debug.WriteLine($"[{PanelId}] [OpenItem] Entering with item={item?.Name}, Path={item?.FilePath}");
            try
            {
                _shiftSelectionStartItem = null;
                _lastClickedItem = null;
                _lastClickTime = DateTime.MinValue;
                _wasClickHandled = false;

                if (item.Name == "..")
                {
                    Debug.WriteLine($"[{PanelId}] [OpenItem] Item is '..', going back");
                    PanelManager?.GoBack();
                    return;
                }

                if (item.FilePath == "Drives" ||
                    item.FilePath == "MyComputer" ||
                    Directory.Exists(item.FilePath))
                {
                    Debug.WriteLine($"[{PanelId}] [OpenItem] Navigating to folder: {item.FilePath}");
                    await LoadPathContents(item.FilePath);
                    PanelManager?.NavigateTo(item.FilePath);
                }
                else if (File.Exists(item.FilePath))
                {
                    Debug.WriteLine($"[{PanelId}] [OpenItem] Opening file: {item.FilePath}");
                    // Здесь можно добавить логику открытия файла
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{PanelId}] [OpenItem] Error: {ex}");
            }
        }

        private async Task OpenItemByIndex(int index)
        {
            Debug.WriteLine($"[{PanelId}] [OpenItemByIndex] ENTER, index={index}, SingleClickOpenItem={SingleClickOpenItem}");

            if (_isProcessingBackNavigation)
            {
                Debug.WriteLine($"[{PanelId}] [OpenItemByIndex] Already processing back navigation, skipping");
                return;
            }

            if (index < 0 || index >= Items.Count)
            {
                Debug.WriteLine($"[{PanelId}] [OpenItemByIndex] Invalid index, returning");
                return;
            }

            var item = Items[index];
            Debug.WriteLine($"[{PanelId}] [OpenItemByIndex] Item: {item.Name}, Path: {item.FilePath}, IsEditing={item.IsEditing}");

            if (_currentItemsControl.SelectedItem is ExplorerItemViewModel selectedItem && selectedItem.IsEditing)
            {
                Debug.WriteLine($"[{PanelId}] [OpenItemByIndex] Cancelling edit mode before opening");
                var container = GetContainerFromItem(_currentItemsControl, selectedItem);
                var tile = GetContentTemplateRootFromContainer(container) as BaseTileControl;
                if (tile != null && tile.IsEditing)
                {
                    tile.CancelEditing();
                    Debug.WriteLine($"[{PanelId}] [OpenItemByIndex] CancelEditing called");
                }
            }

            if (item.Name == "..")
            {
                _isProcessingBackNavigation = true;
                try
                {
                    Debug.WriteLine($"[{PanelId}] [OpenItemByIndex] Processing '..' navigation, PanelManager.CurrentPath={PanelManager?.CurrentPath}");
                    PanelManager?.GoBack();

                    _shiftSelectionStartItem = null;
                    _lastClickedItem = null;
                    _lastClickTime = DateTime.MinValue;
                    _wasClickHandled = false;
                    StopHoverTimer();
                    _hoveredItem = null;

                    await Task.Delay(50);

                    Debug.WriteLine($"[{PanelId}] [OpenItemByIndex] After back navigation, PanelManager.CurrentPath={PanelManager?.CurrentPath}");
                }
                finally
                {
                    _isProcessingBackNavigation = false;
                }
                return;
            }

            _shiftSelectionStartItem = null;
            _lastClickedItem = null;
            _lastClickTime = DateTime.MinValue;
            _wasClickHandled = false;

            string path = item.FilePath;

            if (path == "Drives" || path == "MyComputer" || Directory.Exists(path))
            {
                Debug.WriteLine($"[{PanelId}] [OpenItemByIndex] Loading path: {path}");
                await LoadPathContents(path);
                PanelManager?.NavigateTo(path);
            }
            else if (File.Exists(path))
            {
                Debug.WriteLine($"[{PanelId}] [OpenItemByIndex] Opening file: {path}");
            }

            Debug.WriteLine($"[{PanelId}] [OpenItemByIndex] EXIT");
        }

        private async void OpenSelectedItem()
        {
            Debug.WriteLine($"[{PanelId}] [OpenSelectedItem] Entering");
            if (_currentItemsControl.SelectedItem is ExplorerItemViewModel selectedItem)
            {
                await OpenItem(selectedItem);
            }
        }

        private void RenameSelectedItem()
        {
            Debug.WriteLine($"[{PanelId}] [RenameSelectedItem] ENTER");

            try
            {
                if (_currentItemsControl.SelectedItem is ExplorerItemViewModel selectedItem)
                {
                    int selectedIndex = _currentItemsControl.SelectedIndex;
                    Debug.WriteLine($"[{PanelId}] [RenameSelectedItem] Selected item: '{selectedItem.Name}' (Index={selectedIndex}, Hash={selectedItem.GetHashCode()})");
                    Debug.WriteLine($"[{PanelId}] [RenameSelectedItem] IsEditing={selectedItem.IsEditing}, EditRequested={selectedItem.EditRequested}");

                    if (selectedItem.IsEditing)
                    {
                        Debug.WriteLine($"[{PanelId}] [RenameSelectedItem] Item is already in edit mode, returning");
                        return;
                    }

                    // Получаем контейнер для выбранного элемента
                    var container = GetContainerFromItem(_currentItemsControl, selectedItem);
                    Debug.WriteLine($"[{PanelId}] [RenameSelectedItem] Container found immediately: {container != null}");

                    if (container != null)
                    {
                        var tile = GetContentTemplateRootFromContainer(container) as BaseTileControl;
                        if (tile != null)
                        {
                            Debug.WriteLine($"[{PanelId}] [RenameSelectedItem] Tile found, CanEdit={tile.CanEdit}");
                            if (tile.CanEdit)
                            {
                                Debug.WriteLine($"[{PanelId}] [RenameSelectedItem] Calling StartEditing on tile");
                                tile.StartEditing();
                            }
                            else
                            {
                                Debug.WriteLine($"[{PanelId}] [RenameSelectedItem] Tile does not support editing");
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"[{PanelId}] [RenameSelectedItem] ContentTemplateRoot is not BaseTileControl");
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"[{PanelId}] [RenameSelectedItem] Container not found, scrolling into view and retrying...");
                        _currentItemsControl.ScrollIntoView(selectedItem);

                        _ = this.DispatcherQueue.EnqueueAsync(async () =>
                        {
                            await Task.Delay(100);
                            container = GetContainerFromItem(_currentItemsControl, selectedItem);
                            Debug.WriteLine($"[{PanelId}] [RenameSelectedItem] Retry: container found = {container != null}");
                            if (container != null)
                            {
                                var tile = GetContentTemplateRootFromContainer(container) as BaseTileControl;
                                if (tile != null && tile.CanEdit)
                                {
                                    Debug.WriteLine($"[{PanelId}] [RenameSelectedItem] Retry successful, calling StartEditing");
                                    tile.StartEditing();
                                }
                                else
                                {
                                    Debug.WriteLine($"[{PanelId}] [RenameSelectedItem] Retry: tile not found or cannot edit");
                                }
                            }
                        }, Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal);
                    }
                }
                else
                {
                    Debug.WriteLine($"[{PanelId}] [RenameSelectedItem] No item selected");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{PanelId}] [RenameSelectedItem] Exception: {ex}");
            }

            Debug.WriteLine($"[{PanelId}] [RenameSelectedItem] EXIT");
        }

        private async void DeleteSelectedItems()
        {
            var selectedItems = _currentItemsControl.SelectedItems.Cast<ExplorerItemViewModel>().ToList();
            if (selectedItems.Count > 0)
            {
                Debug.WriteLine($"[{PanelId}] [DeleteSelectedItems] Deleting {selectedItems.Count} items");
                // Здесь будет логика удаления
            }
        }

        #endregion

        #region Множественное переименование (MultiRename)

        // НОВЫЙ МЕТОД: Начать множественное переименование
        private void StartMultiRename()
        {
            Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} ========== START MULTI-RENAME ==========");
            Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Selected items count: {_currentItemsControl.SelectedItems.Count}");

            // Сохраняем исходные пути выделенных элементов в порядке их следования в коллекции
            _multiRenamePaths = _currentItemsControl.SelectedItems
                .Cast<ExplorerItemViewModel>()
                .OrderBy(item => Items.IndexOf(item))
                .Select(item => item.FilePath)
                .ToList();

            Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Items in queue:");
            for (int i = 0; i < _multiRenamePaths.Count; i++)
            {
                var item = Items.FirstOrDefault(x => x.FilePath == _multiRenamePaths[i]);
                Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix}   [{i}] {item?.Name} (Path: {_multiRenamePaths[i]})");
            }

            _multiRenameCurrentIndex = 0;
            _isMultiRenameMode = true;

            // Отключаем таймер наведения, чтобы не мешал
            StopHoverTimer();
            _hoveredItem = null;

            Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Starting with first item");
            BeginRenameForCurrentMultiItem();
            Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} ========== END START MULTI-RENAME ==========");
        }

        // НОВЫЙ МЕТОД: Начать редактирование для текущего элемента в очереди
        private void BeginRenameForCurrentMultiItem()
        {
            Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} BeginRenameForCurrentMultiItem: Index={_multiRenameCurrentIndex}, Total={_multiRenamePaths?.Count}");

            if (!_isMultiRenameMode || _multiRenamePaths == null)
            {
                Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Not in multi-rename mode or paths null, finishing");
                FinishMultiRename();
                return;
            }

            if (_multiRenameCurrentIndex >= _multiRenamePaths.Count)
            {
                Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Reached end of list, finishing multi-rename");
                FinishMultiRename();
                return;
            }

            string targetPath = _multiRenamePaths[_multiRenameCurrentIndex];
            Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Looking for item with path: '{targetPath}'");

            var item = Items.FirstOrDefault(x => x.FilePath == targetPath);
            if (item == null)
            {
                Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} WARNING: Item with path '{targetPath}' not found in collection, skipping");
                _multiRenameCurrentIndex++;
                BeginRenameForCurrentMultiItem();
                return;
            }

            Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Found item: '{item.Name}' (Hash={item.GetHashCode()}), Index in collection: {Items.IndexOf(item)}");

            // Делаем этот элемент текущим (для визуального выделения и прокрутки)
            _currentItemsControl.SelectedItems.Clear();
            _currentItemsControl.SelectedItems.Add(item);
            _currentItemsControl.SelectedItem = item;

            // Пытаемся запустить редактирование
            var container = GetContainerFromItem(_currentItemsControl, item);
            if (container != null)
            {
                Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Container found immediately");
                var tile = GetContentTemplateRootFromContainer(container) as BaseTileControl;
                if (tile != null)
                {
                    Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Tile found, starting edit");
                    tile.StartEditing();
                }
                else
                {
                    Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} ERROR: Tile is null");
                    _multiRenameCurrentIndex++;
                    BeginRenameForCurrentMultiItem();
                }
            }
            else
            {
                Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Container not found, scrolling into view and retrying...");
                _currentItemsControl.ScrollIntoView(item);

                _ = this.DispatcherQueue.EnqueueAsync(async () =>
                {
                    await Task.Delay(100);
                    container = GetContainerFromItem(_currentItemsControl, item);
                    Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Retry: container found = {container != null}");
                    if (container != null)
                    {
                        var tile = GetContentTemplateRootFromContainer(container) as BaseTileControl;
                        if (tile != null)
                        {
                            Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Retry successful, starting edit");
                            tile.StartEditing();
                        }
                        else
                        {
                            Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Retry: tile is null, skipping");
                            _multiRenameCurrentIndex++;
                            BeginRenameForCurrentMultiItem();
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Retry: container still not found, skipping");
                        _multiRenameCurrentIndex++;
                        BeginRenameForCurrentMultiItem();
                    }
                }, Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal);
            }
        }

        // НОВЫЙ МЕТОД: Завершить множественное переименование
        private void FinishMultiRename()
        {
            Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} ========== FINISH MULTI-RENAME ==========");
            Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Current state: Index={_multiRenameCurrentIndex}, Paths count={_multiRenamePaths?.Count}");

            _isMultiRenameMode = false;
            _multiRenamePaths = null;
            _multiRenameCurrentIndex = 0;

            // Возвращаем фокус на ItemsControl
            _ = DispatcherQueue.TryEnqueue(() =>
            {
                Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Returning focus to items control");
                _currentItemsControl?.Focus(FocusState.Programmatic);
            });

            Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} ========== END FINISH MULTI-RENAME ==========");
        }

        private async void OnTileEditStateChanged(object sender, bool isEditing)
        {
            Debug.WriteLine($"[{PanelId}] [OnTileEditStateChanged] isEditing={isEditing}, sender={sender.GetType().Name}");

            if (!isEditing && sender is BaseTileControl tile)
            {
                // Если мы в режиме множественного переименования – не восстанавливаем выделение
                if (_isMultiRenameMode)
                {
                    Debug.WriteLine($"[{PanelId}] [OnTileEditStateChanged] In multi-rename mode, skipping selection restore");
                    return;
                }

                // Если недавно был клик на пустое место – не восстанавливаем выделение
                if ((DateTime.Now - _lastEmptySpaceClickTime).TotalMilliseconds < EmptySpaceClickCooldownMs)
                {
                    Debug.WriteLine($"[{PanelId}] [OnTileEditStateChanged] Ignoring selection restore due to recent empty space click ({(DateTime.Now - _lastEmptySpaceClickTime).TotalMilliseconds}ms < {EmptySpaceClickCooldownMs}ms)");
                    return;
                }

                _lastEditEndTime = DateTime.Now;
                StopHoverTimer();
                _hoveredItem = null;

                // Находим старую ViewModel
                var oldEditedItem = tile.DataContext as ExplorerItemViewModel;

                if (oldEditedItem != null && _currentItemsControl != null)
                {
                    // Ждём завершения перезагрузки коллекции
                    await Task.Delay(100);

                    // Ищем элемент в новой коллекции по пути файла
                    var newEditedItem = Items.FirstOrDefault(item =>
                        item.FilePath == oldEditedItem.FilePath ||
                        item.Name == oldEditedItem.Name);

                    if (newEditedItem != null)
                    {
                        Debug.WriteLine($"[{PanelId}] [OnTileEditStateChanged] Found new ViewModel for {newEditedItem.Name}");

                        // Восстанавливаем выделение с новым экземпляром
                        if (!_currentItemsControl.SelectedItems.Contains(newEditedItem))
                        {
                            _currentItemsControl.SelectedItems.Clear();
                            _currentItemsControl.SelectedItems.Add(newEditedItem);
                            _currentItemsControl.SelectedItem = newEditedItem;
                            Debug.WriteLine($"[{PanelId}] [OnTileEditStateChanged] Restored selection for {newEditedItem.Name}");
                        }

                        // Принудительно прокручиваем к элементу
                        _currentItemsControl.ScrollIntoView(newEditedItem, ScrollIntoViewAlignment.Default);

                        // Даём время на создание контейнера
                        await Task.Delay(50);

                        // Фокусируем ItemsControl
                        Debug.WriteLine($"[{PanelId}] [OnTileEditStateChanged] Focusing items control");
                        _currentItemsControl.Focus(FocusState.Programmatic);
                    }
                    else
                    {
                        Debug.WriteLine($"[{PanelId}] [OnTileEditStateChanged] Could not find new ViewModel for {oldEditedItem.Name}");
                    }
                }
            }
        }

        // НОВЫЙ МЕТОД: Обработка завершения редактирования для множественного переименования
        private void OnTileEditCompleted(object sender, EditResult result)
        {
            Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} OnTileEditCompleted: Result={result}, IsMultiRenameMode={_isMultiRenameMode}");

            if (!_isMultiRenameMode)
            {
                Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Not in multi-rename mode, ignoring");
                return;
            }

            var tile = sender as BaseTileControl;
            if (tile == null)
            {
                Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} ERROR: Sender is not BaseTileControl");
                return;
            }

            var editedItem = tile.DataContext as ExplorerItemViewModel;
            if (editedItem == null)
            {
                Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} ERROR: Could not get ViewModel from tile");
                return;
            }

            // Текущий обрабатываемый путь
            if (_multiRenamePaths == null || _multiRenameCurrentIndex < 0 || _multiRenameCurrentIndex >= _multiRenamePaths.Count)
            {
                Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} ERROR: Invalid multi-rename state - paths={_multiRenamePaths?.Count}, currentIndex={_multiRenameCurrentIndex}");
                FinishMultiRename();
                return;
            }

            string currentPath = _multiRenamePaths[_multiRenameCurrentIndex];
            Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Current item: Path='{currentPath}', Name='{editedItem.Name}', Index={_multiRenameCurrentIndex}");

            if (result == EditResult.Saved)
            {
                Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Item saved successfully, removing from list");

                // Успешное сохранение – удаляем текущий путь из списка
                _multiRenamePaths.RemoveAt(_multiRenameCurrentIndex);
                // Индекс не увеличиваем, так как следующий элемент теперь на том же индексе

                Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Remaining items: {_multiRenamePaths.Count}");

                // Переходим к следующему элементу
                BeginRenameForCurrentMultiItem();
            }
            else // Cancelled или Error
            {
                Debug.WriteLine($"[{PanelId}] {MultiRenameLogPrefix} Item cancelled or error, finishing multi-rename sequence");
                // Отмена или ошибка – прерываем всю последовательность
                FinishMultiRename();
            }
        }

        #endregion
    }
}