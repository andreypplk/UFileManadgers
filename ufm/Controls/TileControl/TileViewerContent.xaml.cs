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
//using Windows.ApplicationModel.DataTransfer;
//using Windows.Storage;
//using Windows.System;
//using Windows.UI.Core;
//using Windows.UI.Input;

//namespace ufm
//{
//    public sealed partial class TileViewerContent : UserControl, IDisposable, ISupportsIconSize, IRefreshablePanel, INotifyPropertyChanged, IDropTarget
//    {
//        #region Поля и свойства

//        private string _panelId = "DefaultPanel";
//        public string PanelId
//        {
//            get => _panelId;
//            set
//            {
//                if (_panelId != value)
//                {
//                    if (_instances.ContainsKey(_panelId))
//                        _instances.Remove(_panelId);
//                    _panelId = value;
//                    _instances[_panelId] = this;
//                }
//            }
//        }
//        public PanelManager PanelManager { get; private set; }
//        public event EventHandler NavigationChanged;
//        public event EventHandler<bool> SelectionStateChanged;
//        public event EventHandler ClipboardChanged;
//        public event EventHandler DeleteRequested;
//        private bool _isPasting = false;

//        private CancellationTokenSource _currentOperationCts;
//        private readonly IDirectoryHistory _dummyHistory;
//        private string _currentLoadedPath;

//        private bool _isInitialized = false;
//        private bool _isLoading = false;
//        private int _refreshInProgress = 0;

//        public ObservableCollection<ExplorerItemViewModel> Items { get; } = new ObservableCollection<ExplorerItemViewModel>();

//        private readonly FileSystemService _fileSystemService;
//        private IFileOperationService _fileOperationService;

//        private Grid _parentGrid;
//        private Canvas _selectionCanvas;

//        private bool _isProcessingBackNavigation = false;

//        private string _displayMode = "Horizontal";
//        public string DisplayMode
//        {
//            get => _displayMode;
//            set
//            {
//                if (_displayMode != value)
//                {
//                    _displayMode = value;
//                    UpdateDisplayMode();
//                    OnPropertyChanged();
//                }
//            }
//        }

//        private ListViewBase _currentItemsControl;

//        public double ItemWidth => _layoutManager.ItemWidth;
//        public double ItemHeight => _layoutManager.ItemHeight;
//        public int MaxRowsOrColumns => _layoutManager.MaxRowsOrColumns;

//        private string _selectedSize = "Icons Medium";

//        private DateTime _lastEmptySpaceClickTime = DateTime.MinValue;
//        private const int EMPTY_SPACE_CLICK_COOLDOWN_MS = 300;

//        private readonly SemaphoreSlim _navigationSemaphore = new SemaphoreSlim(1, 1);

//        private readonly ILayoutTileViewerContentMng _layoutManager;
//        private readonly IModifierKeyService _modifierKeyService;
//        private readonly IKeyboardSelectionService _keyboardSelectionService;
//        private readonly IMouseDragSelectionService _mouseDragSelectionService;
//        private readonly IRenameService _renameService;
//        private readonly IClickService _clickService;

//        private DispatcherTimer _hoverTimer;
//        private ExplorerItemViewModel _hoveredItem;
//        private DateTime _lastEditEndTime;

//        private DragDropService _dragDropService;

//        private bool _isDropProcessing;
//        private Control _dragHighlightedTile;
//        private ExplorerItemViewModel _lastDragTargetItem;

//        public bool SingleClickOpenItem
//        {
//            get
//            {
//                try { return App.SettingsManager?.GetSetting<bool>("SingleClickOpen", false) ?? false; }
//                catch { return false; }
//            }
//        }

//        public bool HasSelection => _currentItemsControl?.SelectedItems.Count > 0;
//        public bool CanPaste => _fileOperationService?.CanPaste ?? false;

//        public event PropertyChangedEventHandler PropertyChanged;
//        private static readonly Dictionary<string, TileViewerContent> _instances = new();

//        #endregion

//        #region Конструктор, инициализация и Dispose

//        public TileViewerContent()
//        {
//            InitializeComponent();

//            _layoutManager = new LayoutTileViewerContentMng();
//            _layoutManager.PropertyChanged += LayoutManager_PropertyChanged;

//            _modifierKeyService = new ModifierKeyService();
//            _keyboardSelectionService = new KeyboardSelectionService();
//            _mouseDragSelectionService = new MouseDragSelectionService();
//            _renameService = new RenameService();
//            _clickService = new ClickService();

//            NavigationSettingsMediator.RegisterPanel(this);

//            _dummyHistory = new DirectoryHistory("MyComputer", "Мой Компьютер");

//            _fileSystemService = new FileSystemService();

//            _currentItemsControl = ItemsListView;
//            ItemsListView.ItemsSource = Items;
//            ItemsGridView.ItemsSource = Items;

//            InitializeHoverTimer();

//            SubscribeToEvents(_currentItemsControl);

//            Loaded += OnLoaded;
//            this.Loaded += (s, e) => InitializeSelectionCanvas();

//            CalculateItemDimensions();

//            _dragDropService = null;
//        }

//        public void SetFileOperationService(IFileOperationService service)
//        {
//            if (_fileOperationService != null)
//                _fileOperationService.ClipboardChanged -= OnFileOperationClipboardChanged;

//            _fileOperationService = service ?? throw new ArgumentNullException(nameof(service));
//            _fileOperationService.ClipboardChanged += OnFileOperationClipboardChanged;
//            ClipboardChanged?.Invoke(this, EventArgs.Empty);

//            _dragDropService = new DragDropService(_fileOperationService, _modifierKeyService);
//        }

//        private void OnFileOperationClipboardChanged(object sender, EventArgs e) =>
//            ClipboardChanged?.Invoke(this, EventArgs.Empty);

//        private void LayoutManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
//        {
//            if (e.PropertyName == nameof(ILayoutTileViewerContentMng.ItemWidth) ||
//                e.PropertyName == nameof(ILayoutTileViewerContentMng.ItemHeight) ||
//                e.PropertyName == nameof(ILayoutTileViewerContentMng.MaxRowsOrColumns))
//            {
//                OnPropertyChanged(e.PropertyName);
//                UpdateItemsControlLayout();
//            }
//        }

//        private void InitializeHoverTimer()
//        {
//            _hoverTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
//            _hoverTimer.Tick += HoverTimer_Tick;
//        }

//        private void SubscribeToEvents(ListViewBase itemsControl)
//        {
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

//            itemsControl.DragItemsStarting += ItemsControl_DragItemsStarting;
//            itemsControl.DragOver += ItemsControl_DragOver;
//            itemsControl.Drop += ItemsControl_Drop;
//            itemsControl.DragEnter += ItemsControl_DragEnter;
//            itemsControl.DragLeave += ItemsControl_DragLeave;
//        }

//        private void UnsubscribeFromEvents(ListViewBase itemsControl)
//        {
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

//            itemsControl.DragItemsStarting -= ItemsControl_DragItemsStarting;
//            itemsControl.DragOver -= ItemsControl_DragOver;
//            itemsControl.Drop -= ItemsControl_Drop;
//            itemsControl.DragEnter -= ItemsControl_DragEnter;
//            itemsControl.DragLeave -= ItemsControl_DragLeave;
//        }

//        public void Dispose()
//        {
//            NavigationSettingsMediator.UnregisterPanel(this);
//            _currentOperationCts?.Cancel();
//            _currentOperationCts?.Dispose();
//            _dummyHistory?.Dispose();

//            Loaded -= OnLoaded;

//            UnsubscribeFromEvents(ItemsListView);
//            UnsubscribeFromEvents(ItemsGridView);

//            _hoverTimer?.Stop();
//            _hoverTimer = null;

//            _fileSystemService.ClearPanelCache(PanelId);
//            _fileSystemService?.Dispose();

//            if (PanelManager != null)
//                PanelManager.NavigationChanged -= OnPanelNavigationChanged;

//            _mouseDragSelectionService.RemoveSelectionRectangle();

//            foreach (var item in Items)
//                item?.Dispose();

//            _navigationSemaphore?.Dispose();

//            if (_layoutManager != null)
//                _layoutManager.PropertyChanged -= LayoutManager_PropertyChanged;

//            if (_instances.ContainsKey(_panelId))
//                _instances.Remove(_panelId);
//        }

//        private void RemoveSelectionCanvas()
//        {
//            if (_selectionCanvas != null && _parentGrid != null)
//            {
//                _parentGrid.Children.Remove(_selectionCanvas);
//                _selectionCanvas = null;
//            }
//        }

//        private void InitializeSelectionCanvas()
//        {
//            if (_selectionCanvas != null) return;

//            _selectionCanvas = new Canvas
//            {
//                IsHitTestVisible = false,
//                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent)
//            };

//            _parentGrid = this.Content as Grid;
//            _parentGrid?.Children.Add(_selectionCanvas);
//            Canvas.SetZIndex(_selectionCanvas, 1000);
//        }

//        private async void OnLoaded(object sender, RoutedEventArgs e)
//        {
//            if (string.IsNullOrEmpty(_selectedSize))
//                _selectedSize = "Medium";

//            CalculateItemDimensions();
//            await Task.Delay(50);
//            UpdateAllTiles();
//            UpdateItemsControlLayout();

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

//        private void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

//        #endregion

//        #region Управление PanelManager и навигация

//        public void SetPanelManager(PanelManager panelManager)
//        {
//            if (PanelManager != null)
//                PanelManager.NavigationChanged -= OnPanelNavigationChanged;

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
//            if (_isLoading) return;

//            if (PanelManager != null && PanelManager.CurrentPath != _currentLoadedPath)
//            {
//                await Task.Delay(100);
//                if (PanelManager.CurrentPath != _currentLoadedPath)
//                    await LoadPathContents(PanelManager.CurrentPath);
//            }
//        }

//        private void OnNavigationChanged() => NavigationChanged?.Invoke(this, EventArgs.Empty);

//        #endregion

//        #region Загрузка содержимого

//        private async void LoadInitialContent()
//        {
//            if (_currentLoadedPath == "MyComputer" && Items.Count > 0) return;

//            CancelCurrentOperation();
//            _keyboardSelectionService.ClearSelection(_currentItemsControl);
//            _clickService.ResetClickState();
//            Items.Clear();
//            UpdateItemsControlLayout();

//            try
//            {
//                var items = await _fileSystemService.LoadMyComputerAsync(PanelId, _dummyHistory);
//                foreach (var item in items)
//                    Items.Add(item);
//                _currentLoadedPath = "MyComputer";
//                OnNavigationChanged();
//            }
//            catch { }
//        }

//        internal async Task LoadPathContents(string path)
//        {
//            await _navigationSemaphore.WaitAsync();
//            try
//            {
//                if (_isLoading || _currentLoadedPath == path) return;

//                try
//                {
//                    _isLoading = true;
//                    switch (path)
//                    {
//                        case "MyComputer":
//                            LoadInitialContent();
//                            _currentLoadedPath = path;
//                            break;
//                        case "Drives":
//                            await LoadDrives();
//                            _currentLoadedPath = path;
//                            break;
//                        case "SpecialFolders":
//                            await LoadSpecialFolders();
//                            _currentLoadedPath = path;
//                            break;
//                        case string p when Directory.Exists(p):
//                            await LoadFolderContents(path);
//                            _currentLoadedPath = path;
//                            if (PanelManager != null && PanelManager.CurrentPath != path)
//                                PanelManager.NavigateTo(path);
//                            break;
//                        default:
//                            if (_currentLoadedPath != "MyComputer")
//                            {
//                                LoadInitialContent();
//                                _currentLoadedPath = "MyComputer";
//                            }
//                            break;
//                    }
//                }
//                finally { _isLoading = false; }
//                OnNavigationChanged();
//            }
//            finally { _navigationSemaphore.Release(); }
//        }

//        private async Task LoadDrives()
//        {
//            if (_currentLoadedPath == "Drives" && Items.Count > 1) return;
//            CancelCurrentOperation();
//            _keyboardSelectionService.ClearSelection(_currentItemsControl);
//            _clickService.ResetClickState();
//            Items.Clear();
//            UpdateItemsControlLayout();

//            try
//            {
//                var driveItems = await _fileSystemService.LoadDrivesAsync(_dummyHistory);
//                await DispatcherQueue.EnqueueAsync(() =>
//                {
//                    Items.Clear();
//                    foreach (var item in driveItems) Items.Add(item);
//                    UpdateItemsControlLayout();
//                });
//            }
//            catch
//            {
//                await DispatcherQueue.EnqueueAsync(() => { Items.Clear(); UpdateItemsControlLayout(); });
//            }
//            OnNavigationChanged();
//        }

//        private async Task LoadFolderContents(string folderPath)
//        {
//            if (string.IsNullOrEmpty(folderPath)) return;
//            if (!Directory.Exists(folderPath))
//            {
//                PanelManager?.GoBack();
//                return;
//            }
//            if (_currentLoadedPath == folderPath && Items.Count > 0) return;

//            CancelCurrentOperation();
//            _keyboardSelectionService.ClearSelection(_currentItemsControl);
//            _clickService.ResetClickState();

//            try
//            {
//                var folderItems = await _fileSystemService.LoadFolderContentsAsync(folderPath, _dummyHistory);
//                await DispatcherQueue.EnqueueAsync(() =>
//                {
//                    Items.Clear();
//                    foreach (var item in folderItems) Items.Add(item);
//                    UpdateItemsControlLayout();
//                });
//            }
//            catch (OperationCanceledException) { }
//            catch { PanelManager?.GoBack(); }
//        }

//        private async Task LoadSpecialFolders()
//        {
//            if (_currentLoadedPath == "SpecialFolders" && Items.Count > 1) return;
//            CancelCurrentOperation();
//            _keyboardSelectionService.ClearSelection(_currentItemsControl);
//            _clickService.ResetClickState();
//            Items.Clear();
//            UpdateItemsControlLayout();

//            try
//            {
//                var homeItems = await _fileSystemService.LoadHomeAsync(PanelId, _dummyHistory);
//                await DispatcherQueue.EnqueueAsync(() =>
//                {
//                    Items.Clear();
//                    foreach (var item in homeItems) Items.Add(item);
//                    UpdateItemsControlLayout();
//                });
//            }
//            catch { }
//            OnNavigationChanged();
//        }

//        private void CancelCurrentOperation()
//        {
//            _currentOperationCts?.Cancel();
//            _currentOperationCts?.Dispose();
//            _currentOperationCts = new CancellationTokenSource();
//            _fileSystemService.CancelAllOperations();
//        }

//        public async Task RefreshContent()
//        {
//            if (Interlocked.CompareExchange(ref _refreshInProgress, 1, 0) != 0) return;

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
//                    _isProcessingBackNavigation = false;
//                    _hoverTimer?.Stop();
//                    _mouseDragSelectionService.RemoveSelectionRectangle();
//                    UpdateItemsControlLayout();
//                    return path;
//                }, Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal);

//                _fileSystemService.ClearPanelCache(PanelId);
//                CancelCurrentOperation();

//                if (!string.IsNullOrEmpty(pathToReload))
//                    await LoadPathContents(pathToReload);
//                else
//                    LoadInitialContent();
//            }
//            catch
//            {
//                try { await DispatcherQueue.EnqueueAsync(() => LoadInitialContent()); }
//                catch { }
//            }
//            finally { Interlocked.Exchange(ref _refreshInProgress, 0); }
//        }

//        public void RefreshNavigation()
//        {
//            _ = DispatcherQueue.EnqueueAsync(async () =>
//            {
//                if (_currentItemsControl != null)
//                {
//                    _currentItemsControl.SelectedItem = null;
//                    _currentItemsControl.SelectedItems.Clear();
//                }
//                _keyboardSelectionService.ShiftSelectionStartItem = null;
//                _clickService.ResetClickState();
//                _isProcessingBackNavigation = false;
//                _hoverTimer?.Stop();
//                _mouseDragSelectionService.RemoveSelectionRectangle();
//                await RefreshContent();
//            }, Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal);
//        }

//        #endregion

//        #region Управление режимами отображения

//        private void UpdateDisplayMode()
//        {
//            var selectedItems = _currentItemsControl?.SelectedItems?.Cast<ExplorerItemViewModel>().ToList();
//            var oldControl = _currentItemsControl;

//            switch (DisplayMode.ToLower())
//            {
//                case "horizontal":
//                case "list":
//                    ItemsListView.Visibility = Visibility.Visible;
//                    ItemsGridView.Visibility = Visibility.Collapsed;
//                    _currentItemsControl = ItemsListView;
//                    break;
//                case "vertical":
//                case "icons":
//                    ItemsListView.Visibility = Visibility.Collapsed;
//                    ItemsGridView.Visibility = Visibility.Visible;
//                    _currentItemsControl = ItemsGridView;
//                    break;
//                default:
//                    ItemsListView.Visibility = Visibility.Visible;
//                    ItemsGridView.Visibility = Visibility.Collapsed;
//                    _currentItemsControl = ItemsListView;
//                    break;
//            }

//            if (oldControl != _currentItemsControl)
//            {
//                UnsubscribeFromEvents(oldControl);
//                SubscribeToEvents(_currentItemsControl);
//            }

//            RemoveSelectionCanvas();
//            _selectionCanvas = null;
//            InitializeSelectionCanvas();

//            if (selectedItems != null && _currentItemsControl != null)
//            {
//                _currentItemsControl.SelectedItems.Clear();
//                foreach (var item in selectedItems)
//                    _currentItemsControl.SelectedItems.Add(item);
//            }

//            CalculateItemDimensions();
//            UpdateItemsControlLayout();
//            UpdateAllTiles();
//        }

//        public void SetIconSize(string size)
//        {
//            _selectedSize = size;
//            PanelManager?.UpdateState(state => state.IconSize = size);
//            _layoutManager.CalculateItemDimensions(_selectedSize);
//            UpdateAllTiles();
//            UpdateItemsControlLayout();
//            ItemsListView.UpdateLayout();
//            ItemsGridView.UpdateLayout();
//        }

//        private void CalculateItemDimensions() => _layoutManager.CalculateItemDimensions(_selectedSize);

//        #endregion

//        #region Обновление UI и Layout

//        private void UpdateAllTiles() => _layoutManager.UpdateAllTiles(ItemsListView, ItemsGridView, _selectedSize);

//        private void ItemsControl_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
//        {
//            if (args.Phase != 0) return;

//            BaseTileControl tile = null;
//            if (args.ItemContainer is ListViewItem lvi) tile = lvi.ContentTemplateRoot as BaseTileControl;
//            else if (args.ItemContainer is GridViewItem gvi) tile = gvi.ContentTemplateRoot as BaseTileControl;

//            if (tile != null)
//            {
//                tile.UpdateSize(_selectedSize);
//                tile.EditStateChanged -= OnTileEditStateChanged;
//                tile.EditStateChanged += OnTileEditStateChanged;
//                tile.EditCompleted -= OnTileEditCompleted;
//                tile.EditCompleted += OnTileEditCompleted;
//            }
//        }

//        private void UpdateItemsControlLayout()
//        {
//            _layoutManager.UpdateItemsControlLayout(ItemsListView, true);
//            _layoutManager.UpdateItemsControlLayout(ItemsGridView, false);
//            UpdateSelectionVisual(_currentItemsControl);
//        }

//        private void UpdateSelectionVisual(ListViewBase itemsControl)
//        {
//            foreach (var item in itemsControl.SelectedItems)
//            {
//                var container = itemsControl.ContainerFromItem(item) as Control;
//                if (container != null)
//                    VisualStateManager.GoToState(container, "Selected", false);
//            }
//        }

//        private void ItemsControl_SizeChanged(object sender, SizeChangedEventArgs e)
//        {
//            var itemsControl = sender as ListViewBase;
//            if (itemsControl == null) return;
//            bool isListView = itemsControl == ItemsListView;
//            double newSize = isListView ? e.NewSize.Height : e.NewSize.Width;
//            _layoutManager.OnItemsControlSizeChanged(itemsControl, newSize, isListView);
//            UpdateItemsControlLayout();
//        }

//        #endregion

//        #region Обработка событий мыши и выделение

//        private T FindParentDataContext<T>(FrameworkElement element) where T : class
//        {
//            while (element != null)
//            {
//                if (element.DataContext is T dc) return dc;
//                element = VisualTreeHelper.GetParent(element) as FrameworkElement;
//            }
//            return null;
//        }

//        private void HoverTimer_Tick(object sender, object e)
//        {
//            _hoverTimer.Stop();
//            _modifierKeyService.UpdateKeyStateFromCore();

//            if (_hoveredItem != null && SingleClickOpenItem && !_mouseDragSelectionService.IsDragSelecting)
//            {
//                if (Items.Contains(_hoveredItem))
//                {
//                    var (isCtrlPressed, isShiftPressed, _) = _modifierKeyService.GetCurrentState();
//                    if (isShiftPressed)
//                    {
//                        if (_keyboardSelectionService.ShiftSelectionStartItem == null)
//                            _keyboardSelectionService.ShiftSelectionStartItem =
//                                _currentItemsControl.SelectedItem as ExplorerItemViewModel ?? Items.FirstOrDefault();

//                        if (_keyboardSelectionService.ShiftSelectionStartItem != null)
//                        {
//                            int start = Items.IndexOf(_keyboardSelectionService.ShiftSelectionStartItem);
//                            int end = Items.IndexOf(_hoveredItem);
//                            if (start >= 0 && end >= 0)
//                                _keyboardSelectionService.SelectRange(start, end, _currentItemsControl, Items, isCtrlPressed);
//                        }
//                    }
//                    else if (isCtrlPressed)
//                    {
//                        _keyboardSelectionService.ToggleSelection(_hoveredItem, _currentItemsControl);
//                    }
//                    else
//                    {
//                        _keyboardSelectionService.SetSingleSelection(_hoveredItem, _currentItemsControl);
//                    }
//                }
//                else _hoveredItem = null;
//            }
//        }

//        private void ItemsControl_PointerEntered(object sender, PointerRoutedEventArgs e)
//        {
//            _modifierKeyService.UpdateKeyStateFromCore();
//            if (!SingleClickOpenItem) return;
//            var itemsControl = sender as ListViewBase;
//            if (itemsControl != _currentItemsControl) return;

//            var item = FindParentDataContext<ExplorerItemViewModel>(e.OriginalSource as FrameworkElement);
//            if (item != null)
//            {
//                _hoveredItem = item;
//                _hoverTimer.Start();
//            }
//        }

//        private void ItemsControl_PointerExited(object sender, PointerRoutedEventArgs e)
//        {
//            _modifierKeyService.UpdateKeyStateFromCore();
//            if (!SingleClickOpenItem) return;
//            if ((sender as ListViewBase) != _currentItemsControl) return;
//            _hoverTimer.Stop();
//            _hoveredItem = null;
//        }

//        private void ItemsControl_PointerMoved(object sender, PointerRoutedEventArgs e)
//        {
//            _modifierKeyService.UpdateKeyStateFromCore();
//            if (!SingleClickOpenItem) return;
//            var itemsControl = sender as ListViewBase;
//            if (itemsControl != _currentItemsControl) return;

//            var item = FindParentDataContext<ExplorerItemViewModel>(e.OriginalSource as FrameworkElement);
//            if (item != null && item != _hoveredItem)
//            {
//                _hoveredItem = item;
//                _hoverTimer.Stop();
//                _hoverTimer.Start();
//            }
//            else if (item == null)
//            {
//                _hoverTimer.Stop();
//                _hoveredItem = null;
//            }
//        }

//        private bool IsClickOnItem(Windows.Foundation.Point point, ListViewBase itemsControl)
//        {
//            foreach (var element in VisualTreeHelper.FindElementsInHostCoordinates(point, itemsControl))
//            {
//                var current = element as DependencyObject;
//                while (current != null)
//                {
//                    if (current is ListViewItem || current is GridViewItem)
//                    {
//                        if ((current as FrameworkElement)?.DataContext is ExplorerItemViewModel)
//                            return true;
//                    }
//                    current = VisualTreeHelper.GetParent(current);
//                }
//            }
//            return false;
//        }

//        // Старый GetItemAtPoint для мыши
//        private ExplorerItemViewModel GetItemAtPoint(Windows.Foundation.Point point, ListViewBase itemsControl)
//        {
//            foreach (var element in VisualTreeHelper.FindElementsInHostCoordinates(point, itemsControl))
//            {
//                var current = element as FrameworkElement;
//                while (current != null)
//                {
//                    if (current is ListViewItem || current is GridViewItem)
//                        if (current.DataContext is ExplorerItemViewModel item) return item;
//                    current = VisualTreeHelper.GetParent(current) as FrameworkElement;
//                }
//            }
//            return null;
//        }

//        private void ItemsControl_PointerPressed(object sender, PointerRoutedEventArgs e)
//        {
//            _modifierKeyService.UpdateKeyStateFromCore();
//            var itemsControl = sender as ListViewBase;
//            if (itemsControl != _currentItemsControl) return;

//            var point = e.GetCurrentPoint(itemsControl);
//            if (point.Properties.IsLeftButtonPressed)
//            {
//                if (IsClickOnItem(point.Position, itemsControl))
//                {
//                    var clickedItem = GetItemAtPoint(point.Position, itemsControl);
//                    if (clickedItem != null && SingleClickOpenItem)
//                    {
//                        _hoveredItem = clickedItem;
//                        _hoverTimer.Stop();
//                        _hoverTimer.Start();
//                    }
//                    e.Handled = false;
//                }
//                else
//                {
//                    _lastEmptySpaceClickTime = DateTime.Now;
//                    _hoverTimer.Stop();
//                    _hoveredItem = null;
//                    _lastEditEndTime = DateTime.MinValue;

//                    _mouseDragSelectionService.StartDrag(point.Position, itemsControl);
//                    _mouseDragSelectionService.CreateSelectionRectangle(_selectionCanvas);
//                    itemsControl.CapturePointer(e.Pointer);

//                    if (!_modifierKeyService.IsCtrlPressed)
//                    {
//                        itemsControl.SelectedItems.Clear();
//                        itemsControl.SelectedItem = null;
//                    }
//                    e.Handled = true;
//                    _ = DispatcherQueue.TryEnqueue(() => itemsControl.Focus(FocusState.Programmatic));
//                }
//            }
//        }

//        private void ItemsControl_PointerReleased(object sender, PointerRoutedEventArgs e)
//        {
//            _modifierKeyService.UpdateKeyStateFromCore();
//            var itemsControl = sender as ListViewBase;
//            if (itemsControl != _currentItemsControl) return;

//            var point = e.GetCurrentPoint(itemsControl);
//            if (!point.Properties.IsLeftButtonPressed)
//            {
//                if (itemsControl.PointerCaptures?.Count > 0)
//                    itemsControl.ReleasePointerCapture(e.Pointer);
//            }

//            _mouseDragSelectionService.EndDrag(itemsControl);
//            _mouseDragSelectionService.RemoveSelectionRectangle();
//            _ = DispatcherQueue.TryEnqueue(() => itemsControl.Focus(FocusState.Programmatic));
//            e.Handled = true;
//        }

//        private void ItemsControl_PointerMovedForDrag(object sender, PointerRoutedEventArgs e)
//        {
//            _modifierKeyService.UpdateKeyStateFromCore();
//            var itemsControl = sender as ListViewBase;
//            if (itemsControl != _currentItemsControl) return;

//            var point = e.GetCurrentPoint(itemsControl);
//            if (_mouseDragSelectionService.IsLeftMouseButtonPressed && point.Properties.IsLeftButtonPressed)
//            {
//                _mouseDragSelectionService.UpdateDrag(point.Position, itemsControl, _modifierKeyService.IsCtrlPressed);
//                if (_mouseDragSelectionService.IsDragSelecting)
//                {
//                    var start = _mouseDragSelectionService.DragStartPoint;
//                    var curr = new Vector2((float)point.Position.X, (float)point.Position.Y);
//                    _mouseDragSelectionService.UpdateSelectionRectangle(start, curr);
//                    _mouseDragSelectionService.ApplyDragSelection(start, curr, itemsControl, Items, _modifierKeyService.IsCtrlPressed);
//                    e.Handled = true;
//                }
//            }
//        }

//        private void ItemsControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            if ((sender as ListViewBase) != _currentItemsControl) return;
//            SelectionStateChanged?.Invoke(this, _currentItemsControl.SelectedItems.Count > 0);
//            ClipboardChanged?.Invoke(this, EventArgs.Empty);
//        }

//        #endregion

//        #region Обработка кликов и двойных кликов

//        public async void ItemsControl_OnItemClick(object sender, ItemClickEventArgs e)
//        {
//            if ((sender as ListViewBase) != _currentItemsControl) return;
//            _modifierKeyService.UpdateKeyStateFromCore();

//            if (e.ClickedItem is ExplorerItemViewModel item)
//            {
//                int idx = Items.IndexOf(item);
//                if (idx >= 0)
//                {
//                    var (isCtrl, isShift, _) = _modifierKeyService.GetCurrentState();
//                    bool shouldOpen = _clickService.HandleItemClick(item, idx, _currentItemsControl, SingleClickOpenItem, isCtrl, isShift, _keyboardSelectionService);
//                    if (shouldOpen) await OpenItemByIndex(idx);
//                }
//            }
//        }

//        private async void ItemsControl_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
//        {
//            if ((sender as ListViewBase) != _currentItemsControl) return;
//            if (SingleClickOpenItem) return;

//            var element = e.OriginalSource as FrameworkElement;
//            while (element != null && !(element.DataContext is ExplorerItemViewModel))
//                element = VisualTreeHelper.GetParent(element) as FrameworkElement;

//            if (element?.DataContext is ExplorerItemViewModel item)
//            {
//                int idx = Items.IndexOf(item);
//                if (idx >= 0 && _clickService.IsDoubleClick(item))
//                    await OpenItemByIndex(idx);
//            }
//        }

//        #endregion

//        #region Обработка клавиатуры

//        private void ItemsControl_KeyDown(object sender, KeyRoutedEventArgs e)
//        {
//            var itemsControl = sender as ListViewBase;
//            if (itemsControl != _currentItemsControl) return;

//            _modifierKeyService.UpdateKeyState(e.Key, true);
//            var (isCtrl, isShift, _) = _modifierKeyService.GetCurrentState();

//            if (_renameService.IsMultiRenameMode) { e.Handled = true; return; }

//            if (isCtrl && !isShift)
//            {
//                switch (e.Key)
//                {
//                    case VirtualKey.C: CopySelectedItems(); e.Handled = true; return;
//                    case VirtualKey.X: CutSelectedItems(); e.Handled = true; return;
//                    case VirtualKey.V: _ = PasteItemsAsync(); e.Handled = true; return;
//                }
//            }

//            switch (e.Key)
//            {
//                case VirtualKey.A when isCtrl && !isShift:
//                    _currentItemsControl.SelectAll(); e.Handled = true; break;
//                case VirtualKey.Space when isCtrl:
//                    if (_currentItemsControl.SelectedItem is ExplorerItemViewModel ci)
//                        _keyboardSelectionService.ToggleSelection(ci, _currentItemsControl);
//                    e.Handled = true; break;
//                case VirtualKey.Enter:
//                    if (_currentItemsControl.SelectedItem != null) { OpenSelectedItem(); e.Handled = true; }
//                    break;
//                case VirtualKey.F2:
//                    if (_renameService.IsMultiRenameMode) { e.Handled = true; break; }
//                    if (_currentItemsControl.SelectedItems.Count > 1)
//                    {
//                        _renameService.StartMultiRename(_currentItemsControl.SelectedItems.Cast<ExplorerItemViewModel>(), Items, _currentItemsControl);
//                        e.Handled = true;
//                    }
//                    else if (_currentItemsControl.SelectedItems.Count == 1)
//                    {
//                        if (_currentItemsControl.SelectedItem is ExplorerItemViewModel si)
//                            _renameService.StartSingleRename(si, _currentItemsControl);
//                        e.Handled = true;
//                    }
//                    break;
//                case VirtualKey.Delete:
//                    if (_currentItemsControl.SelectedItems.Count > 0) { DeleteRequested?.Invoke(this, EventArgs.Empty); e.Handled = true; }
//                    break;
//                case VirtualKey.Up:
//                case VirtualKey.Down:
//                case VirtualKey.Left:
//                case VirtualKey.Right:
//                    HandleArrowKeyNavigation(e.Key, isCtrl, isShift); e.Handled = true; break;
//                case VirtualKey.Home:
//                case VirtualKey.End:
//                    HandleHomeEndNavigation(e.Key, isCtrl, isShift); e.Handled = true; break;
//                case VirtualKey.PageUp:
//                case VirtualKey.PageDown:
//                    HandlePageNavigation(e.Key, isCtrl, isShift); e.Handled = true; break;
//            }
//        }

//        private void ItemsControl_KeyUp(object sender, KeyRoutedEventArgs e)
//        {
//            if ((sender as ListViewBase) != _currentItemsControl) return;
//            _modifierKeyService.UpdateKeyState(e.Key, false);
//        }

//        private void ItemsControl_PreviewKeyDown(object sender, KeyRoutedEventArgs e) { }

//        private void HandleArrowKeyNavigation(VirtualKey key, bool ctrl, bool shift)
//        {
//            if (_currentItemsControl == null) return;
//            int cur = _currentItemsControl.SelectedIndex;
//            int nxt = cur;
//            if (_currentItemsControl == ItemsListView)
//            {
//                int perCol = CalculateItemsPerColumnForListView();
//                switch (key)
//                {
//                    case VirtualKey.Up: nxt = Math.Max(0, cur - 1); break;
//                    case VirtualKey.Down: nxt = Math.Min(Items.Count - 1, cur + 1); break;
//                    case VirtualKey.Left: nxt = Math.Max(0, cur - perCol); break;
//                    case VirtualKey.Right: nxt = Math.Min(Items.Count - 1, cur + perCol); break;
//                }
//            }
//            else
//            {
//                int perRow = CalculateItemsPerRowForGridView();
//                switch (key)
//                {
//                    case VirtualKey.Up: nxt = Math.Max(0, cur - perRow); break;
//                    case VirtualKey.Down: nxt = Math.Min(Items.Count - 1, cur + perRow); break;
//                    case VirtualKey.Left: nxt = Math.Max(0, cur - 1); break;
//                    case VirtualKey.Right: nxt = Math.Min(Items.Count - 1, cur + 1); break;
//                }
//            }

//            if (nxt != cur && nxt >= 0 && nxt < Items.Count)
//            {
//                var newItem = Items[nxt];
//                if (shift)
//                    _keyboardSelectionService.HandleShiftArrow(nxt, _currentItemsControl, Items, _keyboardSelectionService.ShiftSelectionStartItem, ctrl);
//                else if (ctrl)
//                {
//                    _currentItemsControl.SelectedItem = newItem;
//                    _currentItemsControl.ScrollIntoView(newItem);
//                }
//                else
//                {
//                    _currentItemsControl.SelectedItems.Clear();
//                    _currentItemsControl.SelectedItem = newItem;
//                    _currentItemsControl.ScrollIntoView(newItem);
//                    _keyboardSelectionService.ShiftSelectionStartItem = newItem;
//                }
//            }
//        }

//        private void HandleHomeEndNavigation(VirtualKey key, bool ctrl, bool shift)
//        {
//            int nxt = key == VirtualKey.Home ? 0 : Items.Count - 1;
//            if (nxt >= 0 && nxt < Items.Count)
//            {
//                var newItem = Items[nxt];
//                if (shift)
//                    _keyboardSelectionService.HandleShiftRange(_currentItemsControl.SelectedIndex, nxt, _currentItemsControl, Items, ctrl);
//                else if (ctrl)
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

//        private void HandlePageNavigation(VirtualKey key, bool ctrl, bool shift)
//        {
//            int cur = _currentItemsControl.SelectedIndex;
//            int perPage = CalculateItemsPerPage();
//            int nxt = key == VirtualKey.PageUp ? Math.Max(0, cur - perPage) : Math.Min(Items.Count - 1, cur + perPage);
//            if (nxt != cur)
//            {
//                var newItem = Items[nxt];
//                if (shift)
//                    _keyboardSelectionService.HandleShiftRange(cur, nxt, _currentItemsControl, Items, ctrl);
//                else
//                {
//                    _currentItemsControl.SelectedItems.Clear();
//                    _currentItemsControl.SelectedItem = newItem;
//                    _currentItemsControl.ScrollIntoView(newItem);
//                }
//            }
//        }

//        private int CalculateItemsPerColumnForListView() => _layoutManager.CalculateItemsPerColumnForListView(ItemsListView.ActualHeight);
//        private int CalculateItemsPerRowForGridView() => _layoutManager.CalculateItemsPerRowForGridView(ItemsGridView.ActualWidth);
//        private int CalculateItemsPerPage() => _layoutManager.CalculateItemsPerPage(_currentItemsControl, _currentItemsControl?.ActualHeight ?? 0);

//        #endregion

//        #region Операции с элементами

//        private async Task OpenItem(ExplorerItemViewModel item)
//        {
//            try
//            {
//                _keyboardSelectionService.ShiftSelectionStartItem = null;
//                _clickService.ResetClickState();
//                if (item.Name == "..") { PanelManager?.GoBack(); return; }
//                if (item.FilePath == "Drives" || item.FilePath == "MyComputer" || Directory.Exists(item.FilePath))
//                {
//                    await LoadPathContents(item.FilePath);
//                    PanelManager?.NavigateTo(item.FilePath);
//                }
//            }
//            catch (Exception ex) { Debug.WriteLine($"[{PanelId}] Error opening item: {ex}"); }
//        }

//        private async Task OpenItemByIndex(int index)
//        {
//            if (_isProcessingBackNavigation || index < 0 || index >= Items.Count) return;
//            var item = Items[index];

//            if (_currentItemsControl.SelectedItem is ExplorerItemViewModel selectedItem && selectedItem.IsEditing)
//            {
//                var container = GetContainerFromItem(_currentItemsControl, selectedItem);
//                var tile = GetContentTemplateRootFromContainer(container) as BaseTileControl;
//                tile?.CancelEditing();
//            }

//            if (item.Name == "..")
//            {
//                _isProcessingBackNavigation = true;
//                try
//                {
//                    PanelManager?.GoBack();
//                    _keyboardSelectionService.ShiftSelectionStartItem = null;
//                    _clickService.ResetClickState();
//                    _hoverTimer?.Stop();
//                    await Task.Delay(50);
//                }
//                finally { _isProcessingBackNavigation = false; }
//                return;
//            }

//            _keyboardSelectionService.ShiftSelectionStartItem = null;
//            _clickService.ResetClickState();
//            string path = item.FilePath;
//            if (path == "Drives" || path == "MyComputer" || Directory.Exists(path))
//            {
//                await LoadPathContents(path);
//                PanelManager?.NavigateTo(path);
//            }
//            else if (File.Exists(path))
//                await OpenFileAsync(path);
//        }

//        private async Task OpenFileAsync(string filePath)
//        {
//            try
//            {
//                await Task.Run(() =>
//                {
//                    using var process = new Process { StartInfo = new ProcessStartInfo(filePath) { UseShellExecute = true, Verb = "open" } };
//                    process.Start();
//                });
//            }
//            catch { }
//        }

//        private async void OpenSelectedItem()
//        {
//            if (_currentItemsControl.SelectedItem is ExplorerItemViewModel item)
//                await OpenItem(item);
//        }

//        public async Task DeleteSelectedItemsAsync()
//        {
//            var selected = _currentItemsControl.SelectedItems.Cast<ExplorerItemViewModel>().ToList();
//            if (selected.Count == 0) return;

//            var parentPaths = selected
//                .Select(item => System.IO.Path.GetDirectoryName(item.FilePath))
//                .Where(p => !string.IsNullOrEmpty(p))
//                .Distinct(StringComparer.OrdinalIgnoreCase)
//                .ToList();

//            await _fileOperationService.DeleteAsync(selected);
//            DirectoryCacheService.InvalidateCache(_currentLoadedPath);

//            foreach (var parentPath in parentPaths)
//                TreePanelPg01.Instance?.RefreshAfterDrop(Enumerable.Empty<string>(), parentPath);

//            await DispatcherQueue.EnqueueAsync(() =>
//            {
//                foreach (var item in selected)
//                    Items.Remove(item);
//            });
//        }

//        public async Task PasteItemsAsync()
//        {
//            if (_isPasting) return;
//            _isPasting = true;
//            try
//            {
//                string dest = PanelManager?.CurrentPath ?? _currentLoadedPath;
//                if (string.IsNullOrEmpty(dest) || !Directory.Exists(dest)) return;

//                await _fileOperationService.PasteAsync(dest);
//                DirectoryCacheService.InvalidateCache(dest);

//                TreePanelPg01.Instance?.RefreshAfterDrop(Enumerable.Empty<string>(), dest);

//                RefreshNavigation();
//            }
//            finally { _isPasting = false; }
//        }

//        public void RenameSelectedItem()
//        {
//            if (_renameService.IsEditing) return;
//            if (_currentItemsControl.SelectedItems.Count > 1)
//                _renameService.StartMultiRename(_currentItemsControl.SelectedItems.Cast<ExplorerItemViewModel>(), Items, _currentItemsControl);
//            else if (_currentItemsControl.SelectedItems.Count == 1 && _currentItemsControl.SelectedItem is ExplorerItemViewModel item)
//                _renameService.StartSingleRename(item, _currentItemsControl);
//        }

//        public void CopySelectedItems()
//        {
//            var selected = _currentItemsControl?.SelectedItems.Cast<ExplorerItemViewModel>();
//            if (selected != null && selected.Any()) _fileOperationService.Copy(selected);
//        }

//        public void CutSelectedItems()
//        {
//            var selected = _currentItemsControl?.SelectedItems.Cast<ExplorerItemViewModel>();
//            if (selected != null && selected.Any()) _fileOperationService.Cut(selected);
//        }

//        private FrameworkElement GetContainerFromItem(ListViewBase itemsControl, object item)
//        {
//            if (itemsControl is ListView lv) return lv.ContainerFromItem(item) as FrameworkElement;
//            if (itemsControl is GridView gv) return gv.ContainerFromItem(item) as FrameworkElement;
//            return null;
//        }

//        private FrameworkElement GetContentTemplateRootFromContainer(FrameworkElement container)
//        {
//            if (container is ListViewItem lvi) return lvi.ContentTemplateRoot as FrameworkElement;
//            if (container is GridViewItem gvi) return gvi.ContentTemplateRoot as FrameworkElement;
//            return null;
//        }

//        #endregion

//        #region Обработка событий редактирования

//        private void OnTileEditStateChanged(object sender, bool isEditing)
//        {
//            if (!isEditing && sender is BaseTileControl tile)
//            {
//                if (_renameService.IsMultiRenameMode) return;
//                if ((DateTime.Now - _lastEmptySpaceClickTime).TotalMilliseconds < EMPTY_SPACE_CLICK_COOLDOWN_MS) return;
//                _lastEditEndTime = DateTime.Now;
//                _hoverTimer?.Stop();

//                var oldItem = tile.DataContext as ExplorerItemViewModel;
//                if (oldItem != null && _currentItemsControl != null)
//                {
//                    _ = Task.Delay(100).ContinueWith(_ => DispatcherQueue.TryEnqueue(async () =>
//                    {
//                        var newItem = Items.FirstOrDefault(x => x.FilePath == oldItem.FilePath || x.Name == oldItem.Name);
//                        if (newItem != null)
//                        {
//                            if (!_currentItemsControl.SelectedItems.Contains(newItem))
//                            {
//                                _currentItemsControl.SelectedItems.Clear();
//                                _currentItemsControl.SelectedItems.Add(newItem);
//                                _currentItemsControl.SelectedItem = newItem;
//                            }
//                            _currentItemsControl.ScrollIntoView(newItem, ScrollIntoViewAlignment.Default);
//                            await Task.Delay(50);
//                            _currentItemsControl.Focus(FocusState.Programmatic);
//                        }
//                    }));
//                }
//            }
//        }

//        private void OnTileEditCompleted(object sender, EditResult result)
//        {
//            var tile = sender as BaseTileControl;
//            var editedItem = tile?.DataContext as ExplorerItemViewModel;
//            if (editedItem != null) _renameService.HandleEditCompleted(result, editedItem);
//        }

//        #endregion

//        #region Drag-and-drop handlers

//        private void ItemsControl_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
//        {
//            if ((sender as ListViewBase) != _currentItemsControl) return;
//            if (_dragDropService == null) return;

//            var paths = e.Items.OfType<ExplorerItemViewModel>()
//                              .Select(i => i.FilePath)
//                              .Where(p => !string.IsNullOrEmpty(p))
//                              .ToList();
//            if (paths.Count > 0)
//                _dragDropService.OnDragItemsStarting(paths, e);
//        }

//        private void ItemsControl_DragOver(object sender, DragEventArgs e)
//        {
//            if ((sender as ListViewBase) != _currentItemsControl) return;
//            if (_dragDropService == null) return;

//            _modifierKeyService.UpdateKeyStateFromCore();

//            var itemsControl = (ListViewBase)sender;
//            var point = e.GetPosition(itemsControl);
//            var itemUnderPointer = GetItemAtPointForDrag(point, itemsControl);

//            // Сохраняем цель для Drop
//            _lastDragTargetItem = itemUnderPointer;

//            // Пытаемся подсветить плитку (чисто визуально, не влияет на цель)
//            UpdateDragHighlight(itemUnderPointer, itemsControl);

//            string targetFolder;
//            if (itemUnderPointer != null && Directory.Exists(itemUnderPointer.FilePath))
//                targetFolder = itemUnderPointer.FilePath;
//            else
//                targetFolder = GetTargetFolder();

//            _dragDropService.OnDragOver(e, targetFolder, true);
//            e.Handled = true;
//        }

//        private void UpdateDragHighlight(ExplorerItemViewModel item, ListViewBase itemsControl)
//        {
//            // Сбрасываем предыдущую плитку — плавно возвращаем масштаб к 1.0
//            if (_dragHighlightedTile != null)
//            {
//                ScaleAnimator.AnimateScale(_dragHighlightedTile, 1.0f);
//                _dragHighlightedTile = null;
//            }

//            if (item != null && Directory.Exists(item.FilePath))
//            {
//                int index = Items.IndexOf(item);
//                if (index >= 0)
//                {
//                    var container = itemsControl.ContainerFromIndex(index) as FrameworkElement;
//                    if (container != null)
//                    {
//                        var tile = GetContentTemplateRootFromContainer(container) as Control;
//                        if (tile != null)
//                        {
//                            // Плавно увеличиваем до 1.10
//                            ScaleAnimator.AnimateScale(tile, 1.20f);
//                            _dragHighlightedTile = tile;
//                        }
//                    }
//                }
//            }
//        }

//        private async void ItemsControl_Drop(object sender, DragEventArgs e)
//        {
//            if ((sender as ListViewBase) != _currentItemsControl) return;
//            if (_isDropProcessing) return;
//            if (_dragDropService == null) return;

//            _isDropProcessing = true;
//            try
//            {
//                var storageItems = await e.DataView.GetStorageItemsAsync();
//                var sourcePaths = storageItems.Select(i => i.Path)
//                                             .Where(p => !string.IsNullOrEmpty(p))
//                                             .ToList();

//                _modifierKeyService.UpdateKeyStateFromCore();

//                // Цель берём из сохранённого элемента (из DragOver)
//                string target = null;
//                if (_lastDragTargetItem != null && Directory.Exists(_lastDragTargetItem.FilePath))
//                    target = _lastDragTargetItem.FilePath;
//                else
//                    target = GetTargetFolder();

//                if (target != null)
//                {
//                    await _dragDropService.OnDropAsync(e, target);

//                    TreePanelPg01.Instance?.RefreshAfterDrop(sourcePaths, target);
//                    RefreshAfterDrop(sourcePaths);
//                    await ForceReloadCurrentPath();
//                }

//                e.Handled = true;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[DragDrop] Drop error: {ex.Message}");
//            }
//            finally
//            {
//                // Сброс подсветки
//                if (_dragHighlightedTile != null)
//                {
//                    ScaleAnimator.AnimateScale(_dragHighlightedTile, 1.0f);
//                    _dragHighlightedTile = null;
//                }
//                _isDropProcessing = false;
//            }
//        }

//        private async Task ForceReloadCurrentPath()
//        {
//            _isLoading = false;
//            _currentLoadedPath = null;
//            _isProcessingBackNavigation = false;

//            await DispatcherQueue.EnqueueAsync(() =>
//            {
//                _currentItemsControl?.SelectedItems.Clear();
//                Items.Clear();
//                if (_currentItemsControl != null)
//                {
//                    _currentItemsControl.ItemsSource = null;
//                    _currentItemsControl.ItemsSource = Items;
//                }
//            });

//            string pathToLoad = PanelManager?.CurrentPath ?? _currentLoadedPath;
//            if (string.IsNullOrEmpty(pathToLoad))
//                pathToLoad = "MyComputer";

//            await LoadPathContents(pathToLoad);
//        }

//        private void ItemsControl_DragEnter(object sender, DragEventArgs e)
//        {
//            _modifierKeyService.UpdateKeyStateFromCore();
//            e.Handled = true;
//        }

//        private void ItemsControl_DragLeave(object sender, DragEventArgs e)
//        {
//            _modifierKeyService.UpdateKeyStateFromCore();

//            // Сброс подсветки при уходе за пределы панели
//            if (_dragHighlightedTile != null)
//            {
//                ScaleAnimator.AnimateScale(_dragHighlightedTile, 1.0f);
//                _dragHighlightedTile = null;
//            }
//            _lastDragTargetItem = null;
//            e.Handled = true;
//        }

//        // Метод для Drag&Drop – определение элемента по координатам
//        private ExplorerItemViewModel GetItemAtPointForDrag(Windows.Foundation.Point point, ListViewBase itemsControl)
//        {
//            double cellWidth = ItemWidth;
//            double cellHeight = ItemHeight;
//            double offsetX = 0, offsetY = 0;

//            // Ищем первый визуализированный контейнер, чтобы получить реальные размеры ячейки и сдвиг сетки
//            for (int i = 0; i < Items.Count; i++)
//            {
//                var container = itemsControl.ContainerFromIndex(i) as FrameworkElement;
//                if (container != null)
//                {
//                    // Размеры с учётом Margin
//                    cellWidth = container.ActualWidth + container.Margin.Left + container.Margin.Right;
//                    cellHeight = container.ActualHeight + container.Margin.Top + container.Margin.Bottom;

//                    // Вычисляем положение верхнего левого угла первого контейнера относительно itemsControl
//                    // и вычитаем его Margin, чтобы получить начало координат для сетки элементов
//                    try
//                    {
//                        var transform = container.TransformToVisual(itemsControl);
//                        var topLeft = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
//                        offsetX = topLeft.X - container.Margin.Left;
//                        offsetY = topLeft.Y - container.Margin.Top;
//                    }
//                    catch { /* Игнорируем, если контейнер ещё не полностью загружен */ }

//                    break; // Достаточно одного контейнера
//                }
//            }

//            // Корректируем координаты указателя с учётом смещения начала сетки
//            double adjustedX = point.X - offsetX;
//            double adjustedY = point.Y - offsetY;

//            if (itemsControl == ItemsGridView)
//            {
//                int columns = Math.Max(1, (int)(itemsControl.ActualWidth / cellWidth));
//                int row = Math.Max(0, (int)Math.Floor(adjustedY / cellHeight));
//                int col = Math.Max(0, (int)Math.Floor(adjustedX / cellWidth));
//                int index = row * columns + col;
//                if (index >= 0 && index < Items.Count)
//                    return Items[index];
//            }
//            else // ItemsListView
//            {
//                int index = Math.Max(0, (int)Math.Floor(adjustedY / cellHeight));
//                if (index >= 0 && index < Items.Count)
//                    return Items[index];
//            }
//            return null;
//        }

//        #endregion

//        #region IDropTarget implementation

//        public string GetTargetFolder()
//        {
//            string path = _currentLoadedPath;
//            if (path == "MyComputer" || path == "Drives" || !Directory.Exists(path))
//                return null;
//            return path;
//        }

//        public static void RefreshAfterDrop(IEnumerable<string> sourcePaths)
//        {
//            if (sourcePaths == null) return;

//            var folders = sourcePaths
//                .Select(p => System.IO.Path.GetDirectoryName(p))
//                .Where(d => !string.IsNullOrEmpty(d))
//                .Distinct(StringComparer.OrdinalIgnoreCase)
//                .ToList();

//            foreach (var panel in _instances.Values)
//            {
//                if (panel.PanelManager?.CurrentPath is string currentPath &&
//                    folders.Contains(currentPath, StringComparer.OrdinalIgnoreCase))
//                {
//                    _ = panel.DispatcherQueue.TryEnqueue(() => panel.RefreshContent());
//                }
//            }
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
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input;

namespace ufm
{
    public sealed partial class TileViewerContent : UserControl, IDisposable, ISupportsIconSize, IRefreshablePanel, INotifyPropertyChanged, IDropTarget
    {
        #region Поля и свойства

        private string _panelId = "DefaultPanel";
        public string PanelId
        {
            get => _panelId;
            set
            {
                if (_panelId != value)
                {
                    if (_instances.ContainsKey(_panelId))
                        _instances.Remove(_panelId);
                    _panelId = value;
                    _instances[_panelId] = this;
                }
            }
        }
        public PanelManager PanelManager { get; private set; }
        public event EventHandler NavigationChanged;
        public event EventHandler<bool> SelectionStateChanged;
        public event EventHandler ClipboardChanged;
        public event EventHandler DeleteRequested;
        private bool _isPasting = false;

        private CancellationTokenSource _currentOperationCts;
        private readonly IDirectoryHistory _dummyHistory;
        private string _currentLoadedPath;

        private bool _isInitialized = false;
        private bool _isLoading = false;
        private int _refreshInProgress = 0;

        public ObservableCollection<ExplorerItemViewModel> Items { get; } = new ObservableCollection<ExplorerItemViewModel>();

        private readonly FileSystemService _fileSystemService;
        private IFileOperationService _fileOperationService;

        private Grid _parentGrid;
        private Canvas _selectionCanvas;

        private bool _isProcessingBackNavigation = false;

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

        private ListViewBase _currentItemsControl;

        public double ItemWidth => _layoutManager.ItemWidth;
        public double ItemHeight => _layoutManager.ItemHeight;
        public int MaxRowsOrColumns => _layoutManager.MaxRowsOrColumns;

        private string _selectedSize = "Icons Medium";

        private DateTime _lastEmptySpaceClickTime = DateTime.MinValue;
        private const int EMPTY_SPACE_CLICK_COOLDOWN_MS = 300;

        private readonly SemaphoreSlim _navigationSemaphore = new SemaphoreSlim(1, 1);

        private readonly ILayoutTileViewerContentMng _layoutManager;
        private readonly IModifierKeyService _modifierKeyService;
        private readonly IKeyboardSelectionService _keyboardSelectionService;
        private readonly IMouseDragSelectionService _mouseDragSelectionService;
        private readonly IRenameService _renameService;
        private readonly IClickService _clickService;

        private DispatcherTimer _hoverTimer;
        private ExplorerItemViewModel _hoveredItem;
        private DateTime _lastEditEndTime;

        private DragDropService _dragDropService;

        private bool _isDropProcessing;
        private Control _dragHighlightedTile;
        private ExplorerItemViewModel _lastDragTargetItem;

        public bool SingleClickOpenItem
        {
            get
            {
                try { return App.SettingsManager?.GetSetting<bool>("SingleClickOpen", false) ?? false; }
                catch { return false; }
            }
        }

        public bool HasSelection => _currentItemsControl?.SelectedItems.Count > 0;
        public bool CanPaste => _fileOperationService?.CanPaste ?? false;

        public event PropertyChangedEventHandler PropertyChanged;
        private static readonly Dictionary<string, TileViewerContent> _instances = new();

        #endregion

        #region Конструктор, инициализация и Dispose

        public TileViewerContent()
        {
            InitializeComponent();

            _layoutManager = new LayoutTileViewerContentMng();
            _layoutManager.PropertyChanged += LayoutManager_PropertyChanged;

            _modifierKeyService = new ModifierKeyService();
            _keyboardSelectionService = new KeyboardSelectionService();
            _mouseDragSelectionService = new MouseDragSelectionService();
            _renameService = new RenameService();
            _clickService = new ClickService();

            NavigationSettingsMediator.RegisterPanel(this);

            _dummyHistory = new DirectoryHistory("MyComputer", "Мой Компьютер");

            _fileSystemService = new FileSystemService();

            _currentItemsControl = ItemsListView;
            ItemsListView.ItemsSource = Items;
            ItemsGridView.ItemsSource = Items;

            InitializeHoverTimer();

            SubscribeToEvents(_currentItemsControl);

            Loaded += OnLoaded;
            this.Loaded += (s, e) => InitializeSelectionCanvas();

            CalculateItemDimensions();

            _dragDropService = null;
        }

        public void SetFileOperationService(IFileOperationService service)
        {
            if (_fileOperationService != null)
                _fileOperationService.ClipboardChanged -= OnFileOperationClipboardChanged;

            _fileOperationService = service ?? throw new ArgumentNullException(nameof(service));
            _fileOperationService.ClipboardChanged += OnFileOperationClipboardChanged;
            ClipboardChanged?.Invoke(this, EventArgs.Empty);

            _dragDropService = new DragDropService(_fileOperationService, _modifierKeyService);
        }

        private void OnFileOperationClipboardChanged(object sender, EventArgs e) =>
            ClipboardChanged?.Invoke(this, EventArgs.Empty);

        private void LayoutManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ILayoutTileViewerContentMng.ItemWidth) ||
                e.PropertyName == nameof(ILayoutTileViewerContentMng.ItemHeight) ||
                e.PropertyName == nameof(ILayoutTileViewerContentMng.MaxRowsOrColumns))
            {
                OnPropertyChanged(e.PropertyName);
                UpdateItemsControlLayout();
            }
        }

        private void InitializeHoverTimer()
        {
            _hoverTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
            _hoverTimer.Tick += HoverTimer_Tick;
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

            itemsControl.DragItemsStarting += ItemsControl_DragItemsStarting;
            itemsControl.DragOver += ItemsControl_DragOver;
            itemsControl.Drop += ItemsControl_Drop;
            itemsControl.DragEnter += ItemsControl_DragEnter;
            itemsControl.DragLeave += ItemsControl_DragLeave;
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

            itemsControl.DragItemsStarting -= ItemsControl_DragItemsStarting;
            itemsControl.DragOver -= ItemsControl_DragOver;
            itemsControl.Drop -= ItemsControl_Drop;
            itemsControl.DragEnter -= ItemsControl_DragEnter;
            itemsControl.DragLeave -= ItemsControl_DragLeave;
        }

        public void Dispose()
        {
            NavigationSettingsMediator.UnregisterPanel(this);
            _currentOperationCts?.Cancel();
            _currentOperationCts?.Dispose();
            _dummyHistory?.Dispose();

            Loaded -= OnLoaded;

            UnsubscribeFromEvents(ItemsListView);
            UnsubscribeFromEvents(ItemsGridView);

            _hoverTimer?.Stop();
            _hoverTimer = null;

            _fileSystemService.ClearPanelCache(PanelId);
            _fileSystemService?.Dispose();

            if (PanelManager != null)
                PanelManager.NavigationChanged -= OnPanelNavigationChanged;

            _mouseDragSelectionService.RemoveSelectionRectangle();

            foreach (var item in Items)
                item?.Dispose();

            _navigationSemaphore?.Dispose();

            if (_layoutManager != null)
                _layoutManager.PropertyChanged -= LayoutManager_PropertyChanged;

            if (_instances.ContainsKey(_panelId))
                _instances.Remove(_panelId);
        }

        private void RemoveSelectionCanvas()
        {
            if (_selectionCanvas != null && _parentGrid != null)
            {
                _parentGrid.Children.Remove(_selectionCanvas);
                _selectionCanvas = null;
            }
        }

        private void InitializeSelectionCanvas()
        {
            if (_selectionCanvas != null) return;

            _selectionCanvas = new Canvas
            {
                IsHitTestVisible = false,
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent)
            };

            _parentGrid = this.Content as Grid;
            _parentGrid?.Children.Add(_selectionCanvas);
            Canvas.SetZIndex(_selectionCanvas, 1000);
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedSize))
                _selectedSize = "Medium";

            CalculateItemDimensions();
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

        private void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion

        #region Управление PanelManager и навигация

        public void SetPanelManager(PanelManager panelManager)
        {
            if (PanelManager != null)
                PanelManager.NavigationChanged -= OnPanelNavigationChanged;

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
                await Task.Delay(100);
                if (PanelManager.CurrentPath != _currentLoadedPath)
                    await LoadPathContents(PanelManager.CurrentPath);
            }
        }

        private void OnNavigationChanged() => NavigationChanged?.Invoke(this, EventArgs.Empty);

        #endregion

        #region Загрузка содержимого

        private async void LoadInitialContent()
        {
            if (_currentLoadedPath == "MyComputer" && Items.Count > 0) return;

            CancelCurrentOperation();
            _keyboardSelectionService.ClearSelection(_currentItemsControl);
            _clickService.ResetClickState();
            Items.Clear();
            UpdateItemsControlLayout();

            try
            {
                var items = await _fileSystemService.LoadMyComputerAsync(PanelId, _dummyHistory);
                foreach (var item in items)
                    Items.Add(item);
                _currentLoadedPath = "MyComputer";
                OnNavigationChanged();
            }
            catch { }
        }

        internal async Task LoadPathContents(string path)
        {
            await _navigationSemaphore.WaitAsync();
            try
            {
                if (_isLoading || _currentLoadedPath == path) return;

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
                        case "SpecialFolders":
                            await LoadSpecialFolders();
                            _currentLoadedPath = path;
                            break;
                        case string p when Directory.Exists(p):
                            await LoadFolderContents(path);
                            _currentLoadedPath = path;
                            if (PanelManager != null && PanelManager.CurrentPath != path)
                                PanelManager.NavigateTo(path);
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
                finally { _isLoading = false; }
                OnNavigationChanged();
            }
            finally { _navigationSemaphore.Release(); }
        }

        private async Task LoadDrives()
        {
            if (_currentLoadedPath == "Drives" && Items.Count > 1) return;
            CancelCurrentOperation();
            _keyboardSelectionService.ClearSelection(_currentItemsControl);
            _clickService.ResetClickState();
            Items.Clear();
            UpdateItemsControlLayout();

            try
            {
                var driveItems = await _fileSystemService.LoadDrivesAsync(_dummyHistory);
                await DispatcherQueue.EnqueueAsync(() =>
                {
                    Items.Clear();
                    foreach (var item in driveItems) Items.Add(item);
                    UpdateItemsControlLayout();
                });
            }
            catch
            {
                await DispatcherQueue.EnqueueAsync(() => { Items.Clear(); UpdateItemsControlLayout(); });
            }
            OnNavigationChanged();
        }

        private async Task LoadFolderContents(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) return;
            if (!Directory.Exists(folderPath))
            {
                PanelManager?.GoBack();
                return;
            }
            if (_currentLoadedPath == folderPath && Items.Count > 0) return;

            CancelCurrentOperation();
            _keyboardSelectionService.ClearSelection(_currentItemsControl);
            _clickService.ResetClickState();

            try
            {
                var folderItems = await _fileSystemService.LoadFolderContentsAsync(folderPath, _dummyHistory);
                await DispatcherQueue.EnqueueAsync(() =>
                {
                    Items.Clear();
                    foreach (var item in folderItems) Items.Add(item);
                    UpdateItemsControlLayout();
                });
            }
            catch (OperationCanceledException) { }
            catch { PanelManager?.GoBack(); }
        }

        private async Task LoadSpecialFolders()
        {
            if (_currentLoadedPath == "SpecialFolders" && Items.Count > 1) return;
            CancelCurrentOperation();
            _keyboardSelectionService.ClearSelection(_currentItemsControl);
            _clickService.ResetClickState();
            Items.Clear();
            UpdateItemsControlLayout();

            try
            {
                var homeItems = await _fileSystemService.LoadHomeAsync(PanelId, _dummyHistory);
                await DispatcherQueue.EnqueueAsync(() =>
                {
                    Items.Clear();
                    foreach (var item in homeItems) Items.Add(item);
                    UpdateItemsControlLayout();
                });
            }
            catch { }
            OnNavigationChanged();
        }

        private void CancelCurrentOperation()
        {
            _currentOperationCts?.Cancel();
            _currentOperationCts?.Dispose();
            _currentOperationCts = new CancellationTokenSource();
            _fileSystemService.CancelAllOperations();
        }

        public async Task RefreshContent()
        {
            if (Interlocked.CompareExchange(ref _refreshInProgress, 1, 0) != 0) return;

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
                    _isProcessingBackNavigation = false;
                    _hoverTimer?.Stop();
                    _mouseDragSelectionService.RemoveSelectionRectangle();
                    UpdateItemsControlLayout();
                    return path;
                }, Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal);

                _fileSystemService.ClearPanelCache(PanelId);
                CancelCurrentOperation();

                if (!string.IsNullOrEmpty(pathToReload))
                    await LoadPathContents(pathToReload);
                else
                    LoadInitialContent();
            }
            catch
            {
                try { await DispatcherQueue.EnqueueAsync(() => LoadInitialContent()); }
                catch { }
            }
            finally { Interlocked.Exchange(ref _refreshInProgress, 0); }
        }

        public void RefreshNavigation()
        {
            _ = DispatcherQueue.EnqueueAsync(async () =>
            {
                if (_currentItemsControl != null)
                {
                    _currentItemsControl.SelectedItem = null;
                    _currentItemsControl.SelectedItems.Clear();
                }
                _keyboardSelectionService.ShiftSelectionStartItem = null;
                _clickService.ResetClickState();
                _isProcessingBackNavigation = false;
                _hoverTimer?.Stop();
                _mouseDragSelectionService.RemoveSelectionRectangle();
                await RefreshContent();
            }, Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal);
        }

        #endregion

        #region Управление режимами отображения

        private void UpdateDisplayMode()
        {
            var selectedItems = _currentItemsControl?.SelectedItems?.Cast<ExplorerItemViewModel>().ToList();
            var oldControl = _currentItemsControl;

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

            if (oldControl != _currentItemsControl)
            {
                UnsubscribeFromEvents(oldControl);
                SubscribeToEvents(_currentItemsControl);
            }

            RemoveSelectionCanvas();
            _selectionCanvas = null;
            InitializeSelectionCanvas();

            if (selectedItems != null && _currentItemsControl != null)
            {
                _currentItemsControl.SelectedItems.Clear();
                foreach (var item in selectedItems)
                    _currentItemsControl.SelectedItems.Add(item);
            }

            CalculateItemDimensions();
            UpdateItemsControlLayout();
            UpdateAllTiles();
        }

        public void SetIconSize(string size)
        {
            _selectedSize = size;
            PanelManager?.UpdateState(state => state.IconSize = size);
            _layoutManager.CalculateItemDimensions(_selectedSize);
            UpdateAllTiles();
            UpdateItemsControlLayout();
            ItemsListView.UpdateLayout();
            ItemsGridView.UpdateLayout();
        }

        private void CalculateItemDimensions() => _layoutManager.CalculateItemDimensions(_selectedSize);

        #endregion

        #region Обновление UI и Layout

        private void UpdateAllTiles() => _layoutManager.UpdateAllTiles(ItemsListView, ItemsGridView, _selectedSize);

        private void ItemsControl_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.Phase != 0) return;

            BaseTileControl tile = null;
            if (args.ItemContainer is ListViewItem lvi) tile = lvi.ContentTemplateRoot as BaseTileControl;
            else if (args.ItemContainer is GridViewItem gvi) tile = gvi.ContentTemplateRoot as BaseTileControl;

            if (tile != null)
            {
                tile.UpdateSize(_selectedSize);
                tile.EditStateChanged -= OnTileEditStateChanged;
                tile.EditStateChanged += OnTileEditStateChanged;
                tile.EditCompleted -= OnTileEditCompleted;
                tile.EditCompleted += OnTileEditCompleted;
            }
        }

        private void UpdateItemsControlLayout()
        {
            _layoutManager.UpdateItemsControlLayout(ItemsListView, true);
            _layoutManager.UpdateItemsControlLayout(ItemsGridView, false);
            UpdateSelectionVisual(_currentItemsControl);
        }

        private void UpdateSelectionVisual(ListViewBase itemsControl)
        {
            foreach (var item in itemsControl.SelectedItems)
            {
                var container = itemsControl.ContainerFromItem(item) as Control;
                if (container != null)
                    VisualStateManager.GoToState(container, "Selected", false);
            }
        }

        private void ItemsControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var itemsControl = sender as ListViewBase;
            if (itemsControl == null) return;
            bool isListView = itemsControl == ItemsListView;
            double newSize = isListView ? e.NewSize.Height : e.NewSize.Width;
            _layoutManager.OnItemsControlSizeChanged(itemsControl, newSize, isListView);
            UpdateItemsControlLayout();
        }

        #endregion

        #region Обработка событий мыши и выделение

        private T FindParentDataContext<T>(FrameworkElement element) where T : class
        {
            while (element != null)
            {
                if (element.DataContext is T dc) return dc;
                element = VisualTreeHelper.GetParent(element) as FrameworkElement;
            }
            return null;
        }

        private void HoverTimer_Tick(object sender, object e)
        {
            _hoverTimer.Stop();
            _modifierKeyService.UpdateKeyStateFromCore();

            if (_hoveredItem != null && SingleClickOpenItem && !_mouseDragSelectionService.IsDragSelecting)
            {
                if (Items.Contains(_hoveredItem))
                {
                    var (isCtrlPressed, isShiftPressed, _) = _modifierKeyService.GetCurrentState();
                    if (isShiftPressed)
                    {
                        if (_keyboardSelectionService.ShiftSelectionStartItem == null)
                            _keyboardSelectionService.ShiftSelectionStartItem =
                                _currentItemsControl.SelectedItem as ExplorerItemViewModel ?? Items.FirstOrDefault();

                        if (_keyboardSelectionService.ShiftSelectionStartItem != null)
                        {
                            int start = Items.IndexOf(_keyboardSelectionService.ShiftSelectionStartItem);
                            int end = Items.IndexOf(_hoveredItem);
                            if (start >= 0 && end >= 0)
                                _keyboardSelectionService.SelectRange(start, end, _currentItemsControl, Items, isCtrlPressed);
                        }
                    }
                    else if (isCtrlPressed)
                    {
                        _keyboardSelectionService.ToggleSelection(_hoveredItem, _currentItemsControl);
                    }
                    else
                    {
                        _keyboardSelectionService.SetSingleSelection(_hoveredItem, _currentItemsControl);
                    }
                }
                else _hoveredItem = null;
            }
        }

        private void ItemsControl_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            _modifierKeyService.UpdateKeyStateFromCore();
            if (!SingleClickOpenItem) return;
            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl) return;

            var item = FindParentDataContext<ExplorerItemViewModel>(e.OriginalSource as FrameworkElement);
            if (item != null)
            {
                _hoveredItem = item;
                _hoverTimer.Start();
            }
        }

        private void ItemsControl_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            _modifierKeyService.UpdateKeyStateFromCore();
            if (!SingleClickOpenItem) return;
            if ((sender as ListViewBase) != _currentItemsControl) return;
            _hoverTimer.Stop();
            _hoveredItem = null;
        }

        private void ItemsControl_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            _modifierKeyService.UpdateKeyStateFromCore();
            if (!SingleClickOpenItem) return;
            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl) return;

            var item = FindParentDataContext<ExplorerItemViewModel>(e.OriginalSource as FrameworkElement);
            if (item != null && item != _hoveredItem)
            {
                _hoveredItem = item;
                _hoverTimer.Stop();
                _hoverTimer.Start();
            }
            else if (item == null)
            {
                _hoverTimer.Stop();
                _hoveredItem = null;
            }
        }

        private bool IsClickOnItem(Windows.Foundation.Point point, ListViewBase itemsControl)
        {
            foreach (var element in VisualTreeHelper.FindElementsInHostCoordinates(point, itemsControl))
            {
                var current = element as DependencyObject;
                while (current != null)
                {
                    if (current is ListViewItem || current is GridViewItem)
                    {
                        if ((current as FrameworkElement)?.DataContext is ExplorerItemViewModel)
                            return true;
                    }
                    current = VisualTreeHelper.GetParent(current);
                }
            }
            return false;
        }

        // Старый GetItemAtPoint для мыши
        private ExplorerItemViewModel GetItemAtPoint(Windows.Foundation.Point point, ListViewBase itemsControl)
        {
            foreach (var element in VisualTreeHelper.FindElementsInHostCoordinates(point, itemsControl))
            {
                var current = element as FrameworkElement;
                while (current != null)
                {
                    if (current is ListViewItem || current is GridViewItem)
                        if (current.DataContext is ExplorerItemViewModel item) return item;
                    current = VisualTreeHelper.GetParent(current) as FrameworkElement;
                }
            }
            return null;
        }

        private void ItemsControl_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _modifierKeyService.UpdateKeyStateFromCore();
            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl) return;

            var point = e.GetCurrentPoint(itemsControl);
            if (point.Properties.IsLeftButtonPressed)
            {
                if (IsClickOnItem(point.Position, itemsControl))
                {
                    var clickedItem = GetItemAtPoint(point.Position, itemsControl);
                    if (clickedItem != null && SingleClickOpenItem)
                    {
                        _hoveredItem = clickedItem;
                        _hoverTimer.Stop();
                        _hoverTimer.Start();
                    }
                    e.Handled = false;
                }
                else
                {
                    _lastEmptySpaceClickTime = DateTime.Now;
                    _hoverTimer.Stop();
                    _hoveredItem = null;
                    _lastEditEndTime = DateTime.MinValue;

                    _mouseDragSelectionService.StartDrag(point.Position, itemsControl);
                    _mouseDragSelectionService.CreateSelectionRectangle(_selectionCanvas);
                    itemsControl.CapturePointer(e.Pointer);

                    if (!_modifierKeyService.IsCtrlPressed)
                    {
                        itemsControl.SelectedItems.Clear();
                        itemsControl.SelectedItem = null;
                    }
                    e.Handled = true;
                    _ = DispatcherQueue.TryEnqueue(() => itemsControl.Focus(FocusState.Programmatic));
                }
            }
        }

        private void ItemsControl_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _modifierKeyService.UpdateKeyStateFromCore();
            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl) return;

            var point = e.GetCurrentPoint(itemsControl);
            if (!point.Properties.IsLeftButtonPressed)
            {
                if (itemsControl.PointerCaptures?.Count > 0)
                    itemsControl.ReleasePointerCapture(e.Pointer);
            }

            _mouseDragSelectionService.EndDrag(itemsControl);
            _mouseDragSelectionService.RemoveSelectionRectangle();
            _ = DispatcherQueue.TryEnqueue(() => itemsControl.Focus(FocusState.Programmatic));
            e.Handled = true;
        }

        private void ItemsControl_PointerMovedForDrag(object sender, PointerRoutedEventArgs e)
        {
            _modifierKeyService.UpdateKeyStateFromCore();
            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl) return;

            var point = e.GetCurrentPoint(itemsControl);
            if (_mouseDragSelectionService.IsLeftMouseButtonPressed && point.Properties.IsLeftButtonPressed)
            {
                _mouseDragSelectionService.UpdateDrag(point.Position, itemsControl, _modifierKeyService.IsCtrlPressed);
                if (_mouseDragSelectionService.IsDragSelecting)
                {
                    var start = _mouseDragSelectionService.DragStartPoint;
                    var curr = new Vector2((float)point.Position.X, (float)point.Position.Y);
                    _mouseDragSelectionService.UpdateSelectionRectangle(start, curr);
                    _mouseDragSelectionService.ApplyDragSelection(start, curr, itemsControl, Items, _modifierKeyService.IsCtrlPressed);
                    e.Handled = true;
                }
            }
        }

        private void ItemsControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((sender as ListViewBase) != _currentItemsControl) return;
            SelectionStateChanged?.Invoke(this, _currentItemsControl.SelectedItems.Count > 0);
            ClipboardChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Обработка кликов и двойных кликов

        public async void ItemsControl_OnItemClick(object sender, ItemClickEventArgs e)
        {
            if ((sender as ListViewBase) != _currentItemsControl) return;
            _modifierKeyService.UpdateKeyStateFromCore();

            if (e.ClickedItem is ExplorerItemViewModel item)
            {
                int idx = Items.IndexOf(item);
                if (idx >= 0)
                {
                    var (isCtrl, isShift, _) = _modifierKeyService.GetCurrentState();
                    bool shouldOpen = _clickService.HandleItemClick(item, idx, _currentItemsControl, SingleClickOpenItem, isCtrl, isShift, _keyboardSelectionService);
                    if (shouldOpen) await OpenItemByIndex(idx);
                }
            }
        }

        private async void ItemsControl_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if ((sender as ListViewBase) != _currentItemsControl) return;
            if (SingleClickOpenItem) return;

            var element = e.OriginalSource as FrameworkElement;
            while (element != null && !(element.DataContext is ExplorerItemViewModel))
                element = VisualTreeHelper.GetParent(element) as FrameworkElement;

            if (element?.DataContext is ExplorerItemViewModel item)
            {
                int idx = Items.IndexOf(item);
                if (idx >= 0 && _clickService.IsDoubleClick(item))
                    await OpenItemByIndex(idx);
            }
        }

        #endregion

        #region Обработка клавиатуры

        private void ItemsControl_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl) return;

            _modifierKeyService.UpdateKeyState(e.Key, true);
            var (isCtrl, isShift, _) = _modifierKeyService.GetCurrentState();

            if (_renameService.IsMultiRenameMode) { e.Handled = true; return; }

            if (isCtrl && !isShift)
            {
                switch (e.Key)
                {
                    case VirtualKey.C: CopySelectedItems(); e.Handled = true; return;
                    case VirtualKey.X: CutSelectedItems(); e.Handled = true; return;
                    case VirtualKey.V: _ = PasteItemsAsync(); e.Handled = true; return;
                }
            }

            switch (e.Key)
            {
                case VirtualKey.A when isCtrl && !isShift:
                    _currentItemsControl.SelectAll(); e.Handled = true; break;
                case VirtualKey.Space when isCtrl:
                    if (_currentItemsControl.SelectedItem is ExplorerItemViewModel ci)
                        _keyboardSelectionService.ToggleSelection(ci, _currentItemsControl);
                    e.Handled = true; break;
                case VirtualKey.Enter:
                    if (_currentItemsControl.SelectedItem != null) { OpenSelectedItem(); e.Handled = true; }
                    break;
                case VirtualKey.F2:
                    if (_renameService.IsMultiRenameMode) { e.Handled = true; break; }
                    if (_currentItemsControl.SelectedItems.Count > 1)
                    {
                        _renameService.StartMultiRename(_currentItemsControl.SelectedItems.Cast<ExplorerItemViewModel>(), Items, _currentItemsControl);
                        e.Handled = true;
                    }
                    else if (_currentItemsControl.SelectedItems.Count == 1)
                    {
                        if (_currentItemsControl.SelectedItem is ExplorerItemViewModel si)
                            _renameService.StartSingleRename(si, _currentItemsControl);
                        e.Handled = true;
                    }
                    break;
                case VirtualKey.Delete:
                    if (_currentItemsControl.SelectedItems.Count > 0) { DeleteRequested?.Invoke(this, EventArgs.Empty); e.Handled = true; }
                    break;
                case VirtualKey.Up:
                case VirtualKey.Down:
                case VirtualKey.Left:
                case VirtualKey.Right:
                    HandleArrowKeyNavigation(e.Key, isCtrl, isShift); e.Handled = true; break;
                case VirtualKey.Home:
                case VirtualKey.End:
                    HandleHomeEndNavigation(e.Key, isCtrl, isShift); e.Handled = true; break;
                case VirtualKey.PageUp:
                case VirtualKey.PageDown:
                    HandlePageNavigation(e.Key, isCtrl, isShift); e.Handled = true; break;
            }
        }

        private void ItemsControl_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if ((sender as ListViewBase) != _currentItemsControl) return;
            _modifierKeyService.UpdateKeyState(e.Key, false);
        }

        private void ItemsControl_PreviewKeyDown(object sender, KeyRoutedEventArgs e) { }

        private void HandleArrowKeyNavigation(VirtualKey key, bool ctrl, bool shift)
        {
            if (_currentItemsControl == null) return;
            int cur = _currentItemsControl.SelectedIndex;
            int nxt = cur;
            if (_currentItemsControl == ItemsListView)
            {
                int perCol = CalculateItemsPerColumnForListView();
                switch (key)
                {
                    case VirtualKey.Up: nxt = Math.Max(0, cur - 1); break;
                    case VirtualKey.Down: nxt = Math.Min(Items.Count - 1, cur + 1); break;
                    case VirtualKey.Left: nxt = Math.Max(0, cur - perCol); break;
                    case VirtualKey.Right: nxt = Math.Min(Items.Count - 1, cur + perCol); break;
                }
            }
            else
            {
                int perRow = CalculateItemsPerRowForGridView();
                switch (key)
                {
                    case VirtualKey.Up: nxt = Math.Max(0, cur - perRow); break;
                    case VirtualKey.Down: nxt = Math.Min(Items.Count - 1, cur + perRow); break;
                    case VirtualKey.Left: nxt = Math.Max(0, cur - 1); break;
                    case VirtualKey.Right: nxt = Math.Min(Items.Count - 1, cur + 1); break;
                }
            }

            if (nxt != cur && nxt >= 0 && nxt < Items.Count)
            {
                var newItem = Items[nxt];
                if (shift)
                    _keyboardSelectionService.HandleShiftArrow(nxt, _currentItemsControl, Items, _keyboardSelectionService.ShiftSelectionStartItem, ctrl);
                else if (ctrl)
                {
                    _currentItemsControl.SelectedItem = newItem;
                    _currentItemsControl.ScrollIntoView(newItem);
                }
                else
                {
                    _currentItemsControl.SelectedItems.Clear();
                    _currentItemsControl.SelectedItem = newItem;
                    _currentItemsControl.ScrollIntoView(newItem);
                    _keyboardSelectionService.ShiftSelectionStartItem = newItem;
                }
            }
        }

        private void HandleHomeEndNavigation(VirtualKey key, bool ctrl, bool shift)
        {
            int nxt = key == VirtualKey.Home ? 0 : Items.Count - 1;
            if (nxt >= 0 && nxt < Items.Count)
            {
                var newItem = Items[nxt];
                if (shift)
                    _keyboardSelectionService.HandleShiftRange(_currentItemsControl.SelectedIndex, nxt, _currentItemsControl, Items, ctrl);
                else if (ctrl)
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

        private void HandlePageNavigation(VirtualKey key, bool ctrl, bool shift)
        {
            int cur = _currentItemsControl.SelectedIndex;
            int perPage = CalculateItemsPerPage();
            int nxt = key == VirtualKey.PageUp ? Math.Max(0, cur - perPage) : Math.Min(Items.Count - 1, cur + perPage);
            if (nxt != cur)
            {
                var newItem = Items[nxt];
                if (shift)
                    _keyboardSelectionService.HandleShiftRange(cur, nxt, _currentItemsControl, Items, ctrl);
                else
                {
                    _currentItemsControl.SelectedItems.Clear();
                    _currentItemsControl.SelectedItem = newItem;
                    _currentItemsControl.ScrollIntoView(newItem);
                }
            }
        }

        private int CalculateItemsPerColumnForListView() => _layoutManager.CalculateItemsPerColumnForListView(ItemsListView.ActualHeight);
        private int CalculateItemsPerRowForGridView() => _layoutManager.CalculateItemsPerRowForGridView(ItemsGridView.ActualWidth);
        private int CalculateItemsPerPage() => _layoutManager.CalculateItemsPerPage(_currentItemsControl, _currentItemsControl?.ActualHeight ?? 0);

        #endregion

        #region Операции с элементами

        private async Task OpenItem(ExplorerItemViewModel item)
        {
            try
            {
                _keyboardSelectionService.ShiftSelectionStartItem = null;
                _clickService.ResetClickState();
                if (item.Name == "..") { PanelManager?.GoBack(); return; }
                if (item.FilePath == "Drives" || item.FilePath == "MyComputer" || Directory.Exists(item.FilePath))
                {
                    await LoadPathContents(item.FilePath);
                    PanelManager?.NavigateTo(item.FilePath);
                }
            }
            catch { }
        }

        private async Task OpenItemByIndex(int index)
        {
            if (_isProcessingBackNavigation || index < 0 || index >= Items.Count) return;
            var item = Items[index];

            if (_currentItemsControl.SelectedItem is ExplorerItemViewModel selectedItem && selectedItem.IsEditing)
            {
                var container = GetContainerFromItem(_currentItemsControl, selectedItem);
                var tile = GetContentTemplateRootFromContainer(container) as BaseTileControl;
                tile?.CancelEditing();
            }

            if (item.Name == "..")
            {
                _isProcessingBackNavigation = true;
                try
                {
                    PanelManager?.GoBack();
                    _keyboardSelectionService.ShiftSelectionStartItem = null;
                    _clickService.ResetClickState();
                    _hoverTimer?.Stop();
                    await Task.Delay(50);
                }
                finally { _isProcessingBackNavigation = false; }
                return;
            }

            _keyboardSelectionService.ShiftSelectionStartItem = null;
            _clickService.ResetClickState();
            string path = item.FilePath;
            if (path == "Drives" || path == "MyComputer" || Directory.Exists(path))
            {
                await LoadPathContents(path);
                PanelManager?.NavigateTo(path);
            }
            else if (File.Exists(path))
                await OpenFileAsync(path);
        }

        private async Task OpenFileAsync(string filePath)
        {
            try
            {
                await Task.Run(() =>
                {
                    using var process = new Process { StartInfo = new ProcessStartInfo(filePath) { UseShellExecute = true, Verb = "open" } };
                    process.Start();
                });
            }
            catch { }
        }

        private async void OpenSelectedItem()
        {
            if (_currentItemsControl.SelectedItem is ExplorerItemViewModel item)
                await OpenItem(item);
        }

        public async Task DeleteSelectedItemsAsync()
        {
            var selected = _currentItemsControl.SelectedItems.Cast<ExplorerItemViewModel>().ToList();
            if (selected.Count == 0) return;

            var parentPaths = selected
                .Select(item => System.IO.Path.GetDirectoryName(item.FilePath))
                .Where(p => !string.IsNullOrEmpty(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            await _fileOperationService.DeleteAsync(selected);
            DirectoryCacheService.InvalidateCache(_currentLoadedPath);

            foreach (var parentPath in parentPaths)
                TreePanelPg01.Instance?.RefreshAfterDrop(Enumerable.Empty<string>(), parentPath);

            await DispatcherQueue.EnqueueAsync(() =>
            {
                foreach (var item in selected)
                    Items.Remove(item);
            });
        }

        public async Task PasteItemsAsync()
        {
            if (_isPasting) return;
            _isPasting = true;
            try
            {
                string dest = PanelManager?.CurrentPath ?? _currentLoadedPath;
                if (string.IsNullOrEmpty(dest) || !Directory.Exists(dest)) return;

                await _fileOperationService.PasteAsync(dest);
                DirectoryCacheService.InvalidateCache(dest);

                TreePanelPg01.Instance?.RefreshAfterDrop(Enumerable.Empty<string>(), dest);

                RefreshNavigation();
            }
            finally { _isPasting = false; }
        }

        public void RenameSelectedItem()
        {
            if (_renameService.IsEditing) return;
            if (_currentItemsControl.SelectedItems.Count > 1)
                _renameService.StartMultiRename(_currentItemsControl.SelectedItems.Cast<ExplorerItemViewModel>(), Items, _currentItemsControl);
            else if (_currentItemsControl.SelectedItems.Count == 1 && _currentItemsControl.SelectedItem is ExplorerItemViewModel item)
                _renameService.StartSingleRename(item, _currentItemsControl);
        }

        public void CopySelectedItems()
        {
            var selected = _currentItemsControl?.SelectedItems.Cast<ExplorerItemViewModel>();
            if (selected != null && selected.Any()) _fileOperationService.Copy(selected);
        }

        public void CutSelectedItems()
        {
            var selected = _currentItemsControl?.SelectedItems.Cast<ExplorerItemViewModel>();
            if (selected != null && selected.Any()) _fileOperationService.Cut(selected);
        }

        private FrameworkElement GetContainerFromItem(ListViewBase itemsControl, object item)
        {
            if (itemsControl is ListView lv) return lv.ContainerFromItem(item) as FrameworkElement;
            if (itemsControl is GridView gv) return gv.ContainerFromItem(item) as FrameworkElement;
            return null;
        }

        private FrameworkElement GetContentTemplateRootFromContainer(FrameworkElement container)
        {
            if (container is ListViewItem lvi) return lvi.ContentTemplateRoot as FrameworkElement;
            if (container is GridViewItem gvi) return gvi.ContentTemplateRoot as FrameworkElement;
            return null;
        }

        #endregion

        #region Обработка событий редактирования

        private void OnTileEditStateChanged(object sender, bool isEditing)
        {
            if (!isEditing && sender is BaseTileControl tile)
            {
                if (_renameService.IsMultiRenameMode) return;
                if ((DateTime.Now - _lastEmptySpaceClickTime).TotalMilliseconds < EMPTY_SPACE_CLICK_COOLDOWN_MS) return;
                _lastEditEndTime = DateTime.Now;
                _hoverTimer?.Stop();

                var oldItem = tile.DataContext as ExplorerItemViewModel;
                if (oldItem != null && _currentItemsControl != null)
                {
                    _ = Task.Delay(100).ContinueWith(_ => DispatcherQueue.TryEnqueue(async () =>
                    {
                        var newItem = Items.FirstOrDefault(x => x.FilePath == oldItem.FilePath || x.Name == oldItem.Name);
                        if (newItem != null)
                        {
                            if (!_currentItemsControl.SelectedItems.Contains(newItem))
                            {
                                _currentItemsControl.SelectedItems.Clear();
                                _currentItemsControl.SelectedItems.Add(newItem);
                                _currentItemsControl.SelectedItem = newItem;
                            }
                            _currentItemsControl.ScrollIntoView(newItem, ScrollIntoViewAlignment.Default);
                            await Task.Delay(50);
                            _currentItemsControl.Focus(FocusState.Programmatic);
                        }
                    }));
                }
            }
        }

        private void OnTileEditCompleted(object sender, EditResult result)
        {
            var tile = sender as BaseTileControl;
            var editedItem = tile?.DataContext as ExplorerItemViewModel;
            if (editedItem != null) _renameService.HandleEditCompleted(result, editedItem);
        }

        #endregion

        #region Drag-and-drop handlers

        private void ItemsControl_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            if ((sender as ListViewBase) != _currentItemsControl) return;
            if (_dragDropService == null) return;

            var paths = e.Items.OfType<ExplorerItemViewModel>()
                              .Select(i => i.FilePath)
                              .Where(p => !string.IsNullOrEmpty(p))
                              .ToList();
            if (paths.Count > 0)
                _dragDropService.OnDragItemsStarting(paths, e);
        }

        private void ItemsControl_DragOver(object sender, DragEventArgs e)
        {
            if ((sender as ListViewBase) != _currentItemsControl) return;
            if (_dragDropService == null) return;

            _modifierKeyService.UpdateKeyStateFromCore();

            var itemsControl = (ListViewBase)sender;
            var point = e.GetPosition(itemsControl);
            var itemUnderPointer = GetItemAtPointForDrag(point, itemsControl);

            // Сохраняем цель для Drop
            _lastDragTargetItem = itemUnderPointer;

            // Пытаемся подсветить плитку (чисто визуально, не влияет на цель)
            UpdateDragHighlight(itemUnderPointer, itemsControl);

            string targetFolder;
            if (itemUnderPointer != null && Directory.Exists(itemUnderPointer.FilePath))
                targetFolder = itemUnderPointer.FilePath;
            else
                targetFolder = GetTargetFolder();

            _dragDropService.OnDragOver(e, targetFolder, true);
            e.Handled = true;
        }

        private void UpdateDragHighlight(ExplorerItemViewModel item, ListViewBase itemsControl)
        {
            // Сбрасываем предыдущую плитку — плавно возвращаем масштаб к 1.0
            if (_dragHighlightedTile != null)
            {
                ScaleAnimator.AnimateScale(_dragHighlightedTile, 1.0f);
                _dragHighlightedTile = null;
            }

            if (item != null && Directory.Exists(item.FilePath))
            {
                int index = Items.IndexOf(item);
                if (index >= 0)
                {
                    var container = itemsControl.ContainerFromIndex(index) as FrameworkElement;
                    if (container != null)
                    {
                        var tile = GetContentTemplateRootFromContainer(container) as Control;
                        if (tile != null)
                        {
                            // Плавно увеличиваем до 1.10
                            ScaleAnimator.AnimateScale(tile, 1.20f);
                            _dragHighlightedTile = tile;
                        }
                    }
                }
            }
        }

        private async void ItemsControl_Drop(object sender, DragEventArgs e)
        {
            if ((sender as ListViewBase) != _currentItemsControl) return;
            if (_isDropProcessing) return;
            if (_dragDropService == null) return;

            _isDropProcessing = true;
            try
            {
                var storageItems = await e.DataView.GetStorageItemsAsync();
                var sourcePaths = storageItems.Select(i => i.Path)
                                             .Where(p => !string.IsNullOrEmpty(p))
                                             .ToList();

                _modifierKeyService.UpdateKeyStateFromCore();

                // Цель берём из сохранённого элемента (из DragOver)
                string target = null;
                if (_lastDragTargetItem != null && Directory.Exists(_lastDragTargetItem.FilePath))
                    target = _lastDragTargetItem.FilePath;
                else
                    target = GetTargetFolder();

                if (target != null)
                {
                    await _dragDropService.OnDropAsync(e, target);

                    TreePanelPg01.Instance?.RefreshAfterDrop(sourcePaths, target);
                    RefreshAfterDrop(sourcePaths);
                    await ForceReloadCurrentPath();
                }

                e.Handled = true;
            }
            catch
            {
                // Ошибка дропа обрабатывается без логирования
            }
            finally
            {
                // Сброс подсветки
                if (_dragHighlightedTile != null)
                {
                    ScaleAnimator.AnimateScale(_dragHighlightedTile, 1.0f);
                    _dragHighlightedTile = null;
                }
                _isDropProcessing = false;
            }
        }

        private async Task ForceReloadCurrentPath()
        {
            _isLoading = false;
            _currentLoadedPath = null;
            _isProcessingBackNavigation = false;

            await DispatcherQueue.EnqueueAsync(() =>
            {
                _currentItemsControl?.SelectedItems.Clear();
                Items.Clear();
                if (_currentItemsControl != null)
                {
                    _currentItemsControl.ItemsSource = null;
                    _currentItemsControl.ItemsSource = Items;
                }
            });

            string pathToLoad = PanelManager?.CurrentPath ?? _currentLoadedPath;
            if (string.IsNullOrEmpty(pathToLoad))
                pathToLoad = "MyComputer";

            await LoadPathContents(pathToLoad);
        }

        private void ItemsControl_DragEnter(object sender, DragEventArgs e)
        {
            _modifierKeyService.UpdateKeyStateFromCore();
            e.Handled = true;
        }

        private void ItemsControl_DragLeave(object sender, DragEventArgs e)
        {
            _modifierKeyService.UpdateKeyStateFromCore();

            // Сброс подсветки при уходе за пределы панели
            if (_dragHighlightedTile != null)
            {
                ScaleAnimator.AnimateScale(_dragHighlightedTile, 1.0f);
                _dragHighlightedTile = null;
            }
            _lastDragTargetItem = null;
            e.Handled = true;
        }

        // Метод для Drag&Drop – определение элемента по координатам
        private ExplorerItemViewModel GetItemAtPointForDrag(Windows.Foundation.Point point, ListViewBase itemsControl)
        {
            double cellWidth = ItemWidth;
            double cellHeight = ItemHeight;
            double offsetX = 0, offsetY = 0;

            // Ищем первый визуализированный контейнер, чтобы получить реальные размеры ячейки и сдвиг сетки
            for (int i = 0; i < Items.Count; i++)
            {
                var container = itemsControl.ContainerFromIndex(i) as FrameworkElement;
                if (container != null)
                {
                    // Размеры с учётом Margin
                    cellWidth = container.ActualWidth + container.Margin.Left + container.Margin.Right;
                    cellHeight = container.ActualHeight + container.Margin.Top + container.Margin.Bottom;

                    // Вычисляем положение верхнего левого угла первого контейнера относительно itemsControl
                    // и вычитаем его Margin, чтобы получить начало координат для сетки элементов
                    try
                    {
                        var transform = container.TransformToVisual(itemsControl);
                        var topLeft = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
                        offsetX = topLeft.X - container.Margin.Left;
                        offsetY = topLeft.Y - container.Margin.Top;
                    }
                    catch { /* Игнорируем, если контейнер ещё не полностью загружен */ }

                    break; // Достаточно одного контейнера
                }
            }

            // Корректируем координаты указателя с учётом смещения начала сетки
            double adjustedX = point.X - offsetX;
            double adjustedY = point.Y - offsetY;

            if (itemsControl == ItemsGridView)
            {
                int columns = Math.Max(1, (int)(itemsControl.ActualWidth / cellWidth));
                int row = Math.Max(0, (int)Math.Floor(adjustedY / cellHeight));
                int col = Math.Max(0, (int)Math.Floor(adjustedX / cellWidth));
                int index = row * columns + col;
                if (index >= 0 && index < Items.Count)
                    return Items[index];
            }
            else // ItemsListView
            {
                int index = Math.Max(0, (int)Math.Floor(adjustedY / cellHeight));
                if (index >= 0 && index < Items.Count)
                    return Items[index];
            }
            return null;
        }

        #endregion

        #region IDropTarget implementation

        public string GetTargetFolder()
        {
            string path = _currentLoadedPath;
            if (path == "MyComputer" || path == "Drives" || !Directory.Exists(path))
                return null;
            return path;
        }

        public static void RefreshAfterDrop(IEnumerable<string> sourcePaths)
        {
            if (sourcePaths == null) return;

            var folders = sourcePaths
                .Select(p => System.IO.Path.GetDirectoryName(p))
                .Where(d => !string.IsNullOrEmpty(d))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var panel in _instances.Values)
            {
                if (panel.PanelManager?.CurrentPath is string currentPath &&
                    folders.Contains(currentPath, StringComparer.OrdinalIgnoreCase))
                {
                    _ = panel.DispatcherQueue.TryEnqueue(() => panel.RefreshContent());
                }
            }
        }

        #endregion
    }
}