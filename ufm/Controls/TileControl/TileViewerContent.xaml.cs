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
    public sealed partial class TileViewerContent : UserControl, IDisposable, ISupportsIconSize, IRefreshablePanel, INotifyPropertyChanged
    {
        #region Поля и свойства

        public string PanelId { get; set; } = "DefaultPanel";
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
        private int _hoverSelectionLock;
        private DateTime _lastEditEndTime;

        public bool SingleClickOpenItem
        {
            get
            {
                try
                {
                    return App.SettingsManager?.GetSetting<bool>("SingleClickOpen", false) ?? false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool HasSelection => _currentItemsControl?.SelectedItems.Count > 0;
        public bool CanPaste => _fileOperationService?.CanPaste ?? false;

        public event PropertyChangedEventHandler PropertyChanged;

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
        }

        public void SetFileOperationService(IFileOperationService service)
        {
            if (_fileOperationService != null)
                _fileOperationService.ClipboardChanged -= OnFileOperationClipboardChanged;

            _fileOperationService = service ?? throw new ArgumentNullException(nameof(service));
            _fileOperationService.ClipboardChanged += OnFileOperationClipboardChanged;
            ClipboardChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnFileOperationClipboardChanged(object sender, EventArgs e)
        {
            ClipboardChanged?.Invoke(this, EventArgs.Empty);
        }

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
            _hoverTimer = new DispatcherTimer();
            _hoverTimer.Interval = TimeSpan.FromMilliseconds(150);
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

            UnsubscribeFromEvents(ItemsListView);
            UnsubscribeFromEvents(ItemsGridView);

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

            _mouseDragSelectionService.RemoveSelectionRectangle();

            foreach (var item in Items)
            {
                item?.Dispose();
            }

            _navigationSemaphore?.Dispose();

            if (_layoutManager != null)
            {
                _layoutManager.PropertyChanged -= LayoutManager_PropertyChanged;
            }
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
            if (_selectionCanvas != null)
                return;

            _selectionCanvas = new Canvas
            {
                IsHitTestVisible = false,
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent)
            };

            _parentGrid = this.Content as Grid;
            if (_parentGrid != null)
            {
                _parentGrid.Children.Add(_selectionCanvas);
                Canvas.SetZIndex(_selectionCanvas, 1000);
            }
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedSize))
            {
                _selectedSize = "Medium";
            }

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

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Управление PanelManager и навигация

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
                await Task.Delay(100);

                if (PanelManager.CurrentPath != _currentLoadedPath)
                {
                    await LoadPathContents(PanelManager.CurrentPath);
                }
            }
        }

        private void OnNavigationChanged()
        {
            NavigationChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Загрузка содержимого (Load, Refresh)

        private async void LoadInitialContent()
        {
            if (_currentLoadedPath == "MyComputer" && Items.Count > 0)
                return;

            CancelCurrentOperation();
            _keyboardSelectionService.ClearSelection(_currentItemsControl);
            _clickService.ResetClickState();
            Items.Clear();
            UpdateItemsControlLayout();

            try
            {
                var items = await _fileSystemService.LoadMyComputerAsync(PanelId, _dummyHistory);
                foreach (var item in items)
                {
                    Items.Add(item);
                }
                _currentLoadedPath = "MyComputer";
                OnNavigationChanged();
            }
            catch
            {
            }
        }

        internal async Task LoadPathContents(string path)
        {
            await _navigationSemaphore.WaitAsync();
            try
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

                        case "SpecialFolders":
                            await LoadSpecialFolders();
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
            finally
            {
                _navigationSemaphore.Release();
            }
        }

        private async Task LoadDrives()
        {
            if (_currentLoadedPath == "Drives" && Items.Count > 1)
                return;

            CancelCurrentOperation();
            _keyboardSelectionService.ClearSelection(_currentItemsControl);
            _clickService.ResetClickState();
            Items.Clear();
            UpdateItemsControlLayout();

            try
            {
                var driveItems = await _fileSystemService.LoadDrivesAsync(_dummyHistory);

                await this.DispatcherQueue.EnqueueAsync(() =>
                {
                    Items.Clear();
                    foreach (var item in driveItems)
                    {
                        Items.Add(item);
                    }
                    UpdateItemsControlLayout();
                });
            }
            catch
            {
                await this.DispatcherQueue.EnqueueAsync(() =>
                {
                    Items.Clear();
                    UpdateItemsControlLayout();
                });
            }

            OnNavigationChanged();
        }

        private async Task LoadFolderContents(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
                return;

            if (!Directory.Exists(folderPath))
            {
                PanelManager?.GoBack();
                return;
            }

            if (_currentLoadedPath == folderPath && Items.Count > 0)
                return;

            CancelCurrentOperation();
            _keyboardSelectionService.ClearSelection(_currentItemsControl);
            _clickService.ResetClickState();

            try
            {
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
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
                PanelManager?.GoBack();
            }
        }

        // НОВЫЙ МЕТОД: загрузка содержимого "Специальные папки"
        private async Task LoadSpecialFolders()
        {
            if (_currentLoadedPath == "SpecialFolders" && Items.Count > 1)
                return;

            CancelCurrentOperation();
            _keyboardSelectionService.ClearSelection(_currentItemsControl);
            _clickService.ResetClickState();
            Items.Clear();
            UpdateItemsControlLayout();

            try
            {
                // Используем LoadHomeAsync – тот же метод, что и в дереве
                var homeItems = await _fileSystemService.LoadHomeAsync(PanelId, _dummyHistory);
                await this.DispatcherQueue.EnqueueAsync(() =>
                {
                    Items.Clear();
                    foreach (var item in homeItems)
                    {
                        Items.Add(item);
                    }
                    UpdateItemsControlLayout();
                });
            }
            catch
            {
                // В случае ошибки панель останется пустой
            }

            OnNavigationChanged();
        }

        private void CancelCurrentOperation()
        {
            _currentOperationCts?.Cancel();
            _currentOperationCts?.Dispose();
            _currentOperationCts = new CancellationTokenSource();
            _fileSystemService.CancelAllOperations();
        }

        //public async Task RefreshContent()
        //{
        //    try
        //    {
        //        string pathToReload = await DispatcherQueue.EnqueueAsync(() =>
        //        {
        //            string path = _currentLoadedPath;
        //            if (string.IsNullOrEmpty(path) && PanelManager != null)
        //                path = PanelManager.CurrentPath;

        //            _currentLoadedPath = null;
        //            _isInitialized = false;
        //            _keyboardSelectionService.ClearSelection(_currentItemsControl);
        //            _clickService.ResetClickState();
        //            Items.Clear();
        //            if (_currentItemsControl != null)
        //            {
        //                _currentItemsControl.SelectedItems.Clear();
        //                _currentItemsControl.SelectedItem = null;
        //            }

        //            _isProcessingBackNavigation = false;

        //            if (_hoverTimer != null && _hoverTimer.IsEnabled)
        //            {
        //                _hoverTimer.Stop();
        //            }

        //            _mouseDragSelectionService.RemoveSelectionRectangle();
        //            UpdateItemsControlLayout();

        //            return path;
        //        });

        //        _fileSystemService.ClearPanelCache(PanelId);
        //        CancelCurrentOperation();

        //        if (!string.IsNullOrEmpty(pathToReload))
        //        {
        //            await LoadPathContents(pathToReload);
        //        }
        //        else
        //        {
        //            await Task.Run(() => LoadInitialContent());
        //        }
        //    }
        //    catch
        //    {
        //        try
        //        {
        //            await DispatcherQueue.EnqueueAsync(() => LoadInitialContent());
        //        }
        //        catch
        //        {
        //        }
        //    }
        //}
        public async Task RefreshContent()
        {
            // Сериализация: если обновление уже идёт, выходим
            if (Interlocked.CompareExchange(ref _refreshInProgress, 1, 0) != 0)
                return;

            try
            {
                string pathToReload = await DispatcherQueue.EnqueueAsync(() =>
                {
                    string path = _currentLoadedPath;
                    if (string.IsNullOrEmpty(path) && PanelManager != null)
                        path = PanelManager.CurrentPath;

                    _currentLoadedPath = null;   // сбрасываем, чтобы гарантировать перезагрузку
                    _isInitialized = false;

                    Items.Clear();
                    if (_currentItemsControl != null)
                    {
                        _currentItemsControl.SelectedItems.Clear();
                        _currentItemsControl.SelectedItem = null;
                    }

                    _isProcessingBackNavigation = false;

                    if (_hoverTimer != null && _hoverTimer.IsEnabled)
                    {
                        _hoverTimer.Stop();
                    }

                    _mouseDragSelectionService.RemoveSelectionRectangle();
                    UpdateItemsControlLayout();

                    return path;
                }, Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal);

                _fileSystemService.ClearPanelCache(PanelId);
                CancelCurrentOperation();

                if (!string.IsNullOrEmpty(pathToReload))
                {
                    await LoadPathContents(pathToReload);
                }
                else
                {
                    // Небольшая корректировка: LoadInitialContent не должна вызываться через Task.Run,
                    // она уже содержит асинхронную логику, лучше вызвать напрямую.
                    LoadInitialContent();
                }
            }
            catch
            {
                try
                {
                    await DispatcherQueue.EnqueueAsync(() => LoadInitialContent());
                }
                catch { }
            }
            finally
            {
                Interlocked.Exchange(ref _refreshInProgress, 0);
            }
        }
        //public void RefreshNavigation()
        //{
        //    _ = this.DispatcherQueue.EnqueueAsync(() =>
        //    {
        //        if (_currentItemsControl != null)
        //        {
        //            _currentItemsControl.SelectedItem = null;
        //            _currentItemsControl.SelectedItems.Clear();
        //        }
        //        _keyboardSelectionService.ShiftSelectionStartItem = null;
        //        _clickService.ResetClickState();
        //        _isProcessingBackNavigation = false;

        //        if (_hoverTimer != null && _hoverTimer.IsEnabled)
        //        {
        //            _hoverTimer.Stop();
        //        }

        //        _mouseDragSelectionService.RemoveSelectionRectangle();

        //        Task task = RefreshContent();
        //    }, Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal);
        //}
        public void RefreshNavigation()
        {
            _ = this.DispatcherQueue.EnqueueAsync(async () =>
            {
                // Сбрасываем UI-состояние перед обновлением
                if (_currentItemsControl != null)
                {
                    _currentItemsControl.SelectedItem = null;
                    _currentItemsControl.SelectedItems.Clear();
                }
                _keyboardSelectionService.ShiftSelectionStartItem = null;
                _clickService.ResetClickState();
                _isProcessingBackNavigation = false;

                if (_hoverTimer != null && _hoverTimer.IsEnabled)
                {
                    _hoverTimer.Stop();
                }

                _mouseDragSelectionService.RemoveSelectionRectangle();

                // Запускаем обновление и ожидаем его завершения
                await RefreshContent();

            }, Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal);
        }
        #endregion

        #region Управление режимами отображения (DisplayMode, IconSize)

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
                {
                    _currentItemsControl.SelectedItems.Add(item);
                }
            }

            CalculateItemDimensions();
            UpdateItemsControlLayout();
            UpdateAllTiles();
        }

        public void SetIconSize(string size)
        {
            _selectedSize = size;

            if (PanelManager != null)
            {
                PanelManager.UpdateState(state => state.IconSize = size);
            }

            _layoutManager.CalculateItemDimensions(_selectedSize);

            UpdateAllTiles();
            UpdateItemsControlLayout();
            ItemsListView.UpdateLayout();
            ItemsGridView.UpdateLayout();
        }

        private void CalculateItemDimensions()
        {
            _layoutManager.CalculateItemDimensions(_selectedSize);
        }

        #endregion

        #region Обновление UI и Layout

        private void UpdateAllTiles()
        {
            _layoutManager.UpdateAllTiles(ItemsListView, ItemsGridView, _selectedSize);
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
                {
                    VisualStateManager.GoToState(container, "Selected", false);
                }
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
                if (element.DataContext is T dataContext)
                    return dataContext;

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
                        {
                            _keyboardSelectionService.ShiftSelectionStartItem = _currentItemsControl.SelectedItem as ExplorerItemViewModel
                                        ?? Items.FirstOrDefault();
                        }

                        if (_keyboardSelectionService.ShiftSelectionStartItem != null)
                        {
                            int startIndex = Items.IndexOf(_keyboardSelectionService.ShiftSelectionStartItem);
                            int endIndex = Items.IndexOf(_hoveredItem);
                            if (startIndex >= 0 && endIndex >= 0)
                            {
                                _keyboardSelectionService.SelectRange(startIndex, endIndex, _currentItemsControl, Items, isCtrlPressed);
                            }
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
                else
                {
                    _hoveredItem = null;
                }
            }
        }

        private void ItemsControl_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            _modifierKeyService.UpdateKeyStateFromCore();
            if (!SingleClickOpenItem) return;

            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl) return;

            var element = e.OriginalSource as FrameworkElement;
            var item = FindParentDataContext<ExplorerItemViewModel>(element);

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

            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl) return;

            _hoverTimer.Stop();
            _hoveredItem = null;
        }

        private void ItemsControl_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            _modifierKeyService.UpdateKeyStateFromCore();
            if (!SingleClickOpenItem) return;

            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl) return;

            var element = e.OriginalSource as FrameworkElement;
            var item = FindParentDataContext<ExplorerItemViewModel>(element);

            if (item != null)
            {
                if (item != _hoveredItem)
                {
                    _hoveredItem = item;
                    _hoverTimer.Stop();
                    _hoverTimer.Start();
                }
            }
            else
            {
                _hoverTimer.Stop();
                _hoveredItem = null;
            }
        }

        private bool IsClickOnItem(Windows.Foundation.Point point, ListViewBase itemsControl)
        {
            var elements = VisualTreeHelper.FindElementsInHostCoordinates(point, itemsControl);

            foreach (var element in elements)
            {
                var current = element as DependencyObject;
                while (current != null)
                {
                    if (current is ListViewItem || current is GridViewItem)
                    {
                        var container = current as FrameworkElement;
                        if (container?.DataContext is ExplorerItemViewModel)
                        {
                            return true;
                        }
                    }

                    current = VisualTreeHelper.GetParent(current);
                }
            }

            return false;
        }

        private ExplorerItemViewModel GetItemAtPoint(Windows.Foundation.Point point, ListViewBase itemsControl)
        {
            var elements = VisualTreeHelper.FindElementsInHostCoordinates(point, itemsControl);

            foreach (var element in elements)
            {
                var current = element as FrameworkElement;
                while (current != null)
                {
                    if (current is ListViewItem || current is GridViewItem)
                    {
                        if (current.DataContext is ExplorerItemViewModel item)
                        {
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
            _modifierKeyService.UpdateKeyStateFromCore();

            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl) return;

            var point = e.GetCurrentPoint(itemsControl);

            if (point.Properties.IsLeftButtonPressed)
            {
                bool isClickOnItem = IsClickOnItem(point.Position, itemsControl);

                if (isClickOnItem)
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

                    _ = DispatcherQueue.TryEnqueue(() =>
                    {
                        itemsControl.Focus(FocusState.Programmatic);
                    });
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
                if (itemsControl.PointerCaptures != null && itemsControl.PointerCaptures.Count > 0)
                {
                    itemsControl.ReleasePointerCapture(e.Pointer);
                }
            }

            _mouseDragSelectionService.EndDrag(itemsControl);
            _mouseDragSelectionService.RemoveSelectionRectangle();

            _ = DispatcherQueue.TryEnqueue(() =>
            {
                itemsControl.Focus(FocusState.Programmatic);
            });

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
                    var startPoint = _mouseDragSelectionService.DragStartPoint;
                    var currentPoint = new Vector2((float)point.Position.X, (float)point.Position.Y);

                    _mouseDragSelectionService.UpdateSelectionRectangle(startPoint, currentPoint);

                    _mouseDragSelectionService.ApplyDragSelection(
                        startPoint,
                        currentPoint,
                        itemsControl,
                        Items,
                        _modifierKeyService.IsCtrlPressed
                    );

                    e.Handled = true;
                }
            }
        }

        private void ItemsControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl) return;

            SelectionStateChanged?.Invoke(this, _currentItemsControl.SelectedItems.Count > 0);
            ClipboardChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Обработка кликов и двойных кликов

        public async void ItemsControl_OnItemClick(object sender, ItemClickEventArgs e)
        {
            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl)
                return;

            _modifierKeyService.UpdateKeyStateFromCore();

            if (e.ClickedItem is ExplorerItemViewModel item)
            {
                int clickedIndex = Items.IndexOf(item);
                if (clickedIndex >= 0)
                {
                    var (isCtrlPressed, isShiftPressed, _) = _modifierKeyService.GetCurrentState();

                    bool shouldOpen = _clickService.HandleItemClick(
                        item, clickedIndex, itemsControl, SingleClickOpenItem,
                        isCtrlPressed, isShiftPressed, _keyboardSelectionService);

                    if (shouldOpen)
                    {
                        await OpenItemByIndex(clickedIndex);
                    }
                }
            }
        }

        private async void ItemsControl_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl) return;

            if (SingleClickOpenItem)
                return;

            var element = e.OriginalSource as FrameworkElement;
            while (element != null && element.DataContext as ExplorerItemViewModel == null)
            {
                element = VisualTreeHelper.GetParent(element) as FrameworkElement;
            }

            if (element?.DataContext is ExplorerItemViewModel item)
            {
                int index = Items.IndexOf(item);
                if (index >= 0 && _clickService.IsDoubleClick(item))
                {
                    await OpenItemByIndex(index);
                }
            }
        }

        #endregion

        #region Обработка клавиатуры и навигация клавишами

        private void ItemsControl_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            var itemsControl = sender as ListViewBase;
            if (itemsControl != _currentItemsControl)
                return;

            _modifierKeyService.UpdateKeyState(e.Key, true);

            var (isCtrlPressed, isShiftPressed, _) = _modifierKeyService.GetCurrentState();

            if (_renameService.IsMultiRenameMode)
            {
                e.Handled = true;
                return;
            }

            if (isCtrlPressed && !isShiftPressed)
            {
                switch (e.Key)
                {
                    case VirtualKey.C:
                        CopySelectedItems();
                        e.Handled = true;
                        return;
                    case VirtualKey.X:
                        CutSelectedItems();
                        e.Handled = true;
                        return;
                    case VirtualKey.V:
                        _ = PasteItemsAsync();
                        e.Handled = true;
                        return;
                }
            }

            switch (e.Key)
            {
                case VirtualKey.A when isCtrlPressed && !isShiftPressed:
                    _currentItemsControl.SelectAll();
                    e.Handled = true;
                    break;

                case VirtualKey.Space when isCtrlPressed:
                    if (_currentItemsControl.SelectedItem is ExplorerItemViewModel currentItem)
                    {
                        _keyboardSelectionService.ToggleSelection(currentItem, _currentItemsControl);
                    }
                    e.Handled = true;
                    break;

                case VirtualKey.Enter:
                    if (_currentItemsControl.SelectedItem != null)
                    {
                        OpenSelectedItem();
                        e.Handled = true;
                    }
                    break;

                case VirtualKey.F2:
                    if (_renameService.IsMultiRenameMode)
                    {
                        e.Handled = true;
                        break;
                    }

                    if (_currentItemsControl.SelectedItems.Count > 1)
                    {
                        _renameService.StartMultiRename(
                            _currentItemsControl.SelectedItems.Cast<ExplorerItemViewModel>(),
                            Items, _currentItemsControl);
                        e.Handled = true;
                    }
                    else if (_currentItemsControl.SelectedItems.Count == 1)
                    {
                        if (_currentItemsControl.SelectedItem is ExplorerItemViewModel selectedItem)
                        {
                            _renameService.StartSingleRename(selectedItem, _currentItemsControl);
                        }
                        e.Handled = true;
                    }
                    break;

                case VirtualKey.Delete:
                    if (_currentItemsControl.SelectedItems.Count > 0)
                    {
                        DeleteRequested?.Invoke(this, EventArgs.Empty);
                        e.Handled = true;
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

            _modifierKeyService.UpdateKeyState(e.Key, false);
        }

        private void ItemsControl_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
        }

        private void HandleArrowKeyNavigation(VirtualKey key, bool isCtrlPressed, bool isShiftPressed)
        {
            if (_currentItemsControl == null) return;

            int currentIndex = _currentItemsControl.SelectedIndex;
            int newIndex = currentIndex;

            if (_currentItemsControl == ItemsListView)
            {
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
                    _keyboardSelectionService.HandleShiftArrow(newIndex, _currentItemsControl, Items,
                        _keyboardSelectionService.ShiftSelectionStartItem, isCtrlPressed);
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
                    _keyboardSelectionService.ShiftSelectionStartItem = newItem;
                }
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
                    _keyboardSelectionService.HandleShiftRange(_currentItemsControl.SelectedIndex, newIndex,
                        _currentItemsControl, Items, isCtrlPressed);
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
                    _keyboardSelectionService.HandleShiftRange(currentIndex, newIndex,
                        _currentItemsControl, Items, isCtrlPressed);
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
            return _layoutManager.CalculateItemsPerColumnForListView(ItemsListView.ActualHeight);
        }

        private int CalculateItemsPerRowForGridView()
        {
            return _layoutManager.CalculateItemsPerRowForGridView(ItemsGridView.ActualWidth);
        }

        private int CalculateItemsPerPage()
        {
            double panelHeight = _currentItemsControl?.ActualHeight ?? 0;
            return _layoutManager.CalculateItemsPerPage(_currentItemsControl, panelHeight);
        }

        #endregion

        #region Операции с элементами (Open, Rename, Delete, Copy, Cut, Paste)

        private async Task OpenItem(ExplorerItemViewModel item)
        {
            try
            {
                _keyboardSelectionService.ShiftSelectionStartItem = null;
                _clickService.ResetClickState();

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
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{PanelId}] Critical error opening item: {ex}");
            }
        }

        private async Task OpenItemByIndex(int index)
        {
            if (_isProcessingBackNavigation)
                return;

            if (index < 0 || index >= Items.Count)
                return;

            var item = Items[index];

            if (_currentItemsControl.SelectedItem is ExplorerItemViewModel selectedItem && selectedItem.IsEditing)
            {
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
                    PanelManager?.GoBack();

                    _keyboardSelectionService.ShiftSelectionStartItem = null;
                    _clickService.ResetClickState();

                    if (_hoverTimer != null && _hoverTimer.IsEnabled)
                    {
                        _hoverTimer.Stop();
                    }

                    await Task.Delay(50);
                }
                finally
                {
                    _isProcessingBackNavigation = false;
                }
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
            {
                await OpenFileAsync(path);
            }
            else
            {
            }
        }

        private async Task OpenFileAsync(string filePath)
        {
            try
            {
                await Task.Run(() =>
                {
                    using var process = new Process();
                    process.StartInfo = new ProcessStartInfo(filePath)
                    {
                        UseShellExecute = true,
                        Verb = "open"
                    };
                    process.Start();
                });
            }
            catch
            {
            }
        }

        private async void OpenSelectedItem()
        {
            if (_currentItemsControl.SelectedItem is ExplorerItemViewModel selectedItem)
            {
                await OpenItem(selectedItem);
            }
        }

        public async Task DeleteSelectedItemsAsync()
        {
            if (_currentItemsControl.SelectedItems.Count == 0)
                return;

            var selectedItems = _currentItemsControl.SelectedItems
                .Cast<ExplorerItemViewModel>()
                .ToList();

            await _fileOperationService.DeleteAsync(selectedItems);

            DirectoryCacheService.InvalidateCache(_currentLoadedPath);

            await DispatcherQueue.EnqueueAsync(() =>
            {
                foreach (var item in selectedItems)
                {
                    Items.Remove(item);
                }
            });
        }

        public async Task PasteItemsAsync()
        {
            if (_isPasting) return;
            _isPasting = true;
            try
            {
                string destination = PanelManager?.CurrentPath ?? _currentLoadedPath;
                if (string.IsNullOrEmpty(destination) || !Directory.Exists(destination))
                    return;

                await _fileOperationService.PasteAsync(destination);
                DirectoryCacheService.InvalidateCache(destination);
                RefreshNavigation();
            }
            finally
            {
                _isPasting = false;
            }
        }

        public void RenameSelectedItem()
        {
            if (_renameService.IsEditing) return;

            if (_currentItemsControl.SelectedItems.Count > 1)
            {
                var selectedItems = _currentItemsControl.SelectedItems.Cast<ExplorerItemViewModel>();
                _renameService.StartMultiRename(selectedItems, Items, _currentItemsControl);
            }
            else if (_currentItemsControl.SelectedItems.Count == 1)
            {
                var item = _currentItemsControl.SelectedItem as ExplorerItemViewModel;
                if (item != null)
                    _renameService.StartSingleRename(item, _currentItemsControl);
            }
        }

        public void CopySelectedItems()
        {
            var selected = _currentItemsControl?.SelectedItems.Cast<ExplorerItemViewModel>();
            if (selected != null && selected.Any())
                _fileOperationService.Copy(selected);
        }

        public void CutSelectedItems()
        {
            var selected = _currentItemsControl?.SelectedItems.Cast<ExplorerItemViewModel>();
            if (selected != null && selected.Any())
                _fileOperationService.Cut(selected);
        }

        private FrameworkElement GetContainerFromItem(ListViewBase itemsControl, object item)
        {
            if (itemsControl is ListView listView)
                return listView.ContainerFromItem(item) as FrameworkElement;
            else if (itemsControl is GridView gridView)
                return gridView.ContainerFromItem(item) as FrameworkElement;
            return null;
        }

        private FrameworkElement GetContentTemplateRootFromContainer(FrameworkElement container)
        {
            if (container is ListViewItem listViewItem)
                return listViewItem.ContentTemplateRoot as FrameworkElement;
            else if (container is GridViewItem gridViewItem)
                return gridViewItem.ContentTemplateRoot as FrameworkElement;
            return null;
        }

        #endregion

        #region Обработка событий редактирования

        private void OnTileEditStateChanged(object sender, bool isEditing)
        {
            if (!isEditing && sender is BaseTileControl tile)
            {
                if (_renameService.IsMultiRenameMode)
                    return;

                if ((DateTime.Now - _lastEmptySpaceClickTime).TotalMilliseconds < EMPTY_SPACE_CLICK_COOLDOWN_MS)
                    return;

                _lastEditEndTime = DateTime.Now;

                if (_hoverTimer != null && _hoverTimer.IsEnabled)
                {
                    _hoverTimer.Stop();
                }

                var oldEditedItem = tile.DataContext as ExplorerItemViewModel;

                if (oldEditedItem != null && _currentItemsControl != null)
                {
                    _ = Task.Delay(100).ContinueWith(_ =>
                    {
                        DispatcherQueue.TryEnqueue(async () =>
                        {
                            var newEditedItem = Items.FirstOrDefault(item =>
                                item.FilePath == oldEditedItem.FilePath ||
                                item.Name == oldEditedItem.Name);

                            if (newEditedItem != null)
                            {
                                if (!_currentItemsControl.SelectedItems.Contains(newEditedItem))
                                {
                                    _currentItemsControl.SelectedItems.Clear();
                                    _currentItemsControl.SelectedItems.Add(newEditedItem);
                                    _currentItemsControl.SelectedItem = newEditedItem;
                                }

                                _currentItemsControl.ScrollIntoView(newEditedItem, ScrollIntoViewAlignment.Default);
                                await Task.Delay(50);

                                _currentItemsControl.Focus(FocusState.Programmatic);
                            }
                        });
                    });
                }
            }
        }

        private void OnTileEditCompleted(object sender, EditResult result)
        {
            var tile = sender as BaseTileControl;
            if (tile == null) return;

            var editedItem = tile.DataContext as ExplorerItemViewModel;
            if (editedItem == null) return;

            _renameService.HandleEditCompleted(result, editedItem);
        }

        #endregion

        #region Drag-and-drop handlers

        private void ItemsControl_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            var items = e.Items.Cast<ExplorerItemViewModel>().ToList();
            if (items.Count == 0) return;
            App.FileOperationService?.Copy(items);
        }

        private void ItemsControl_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = e.DataView.Contains(StandardDataFormats.StorageItems)
                ? DataPackageOperation.Copy
                : DataPackageOperation.None;
        }

        private async void ItemsControl_Drop(object sender, DragEventArgs e)
        {
            string destPath = _currentLoadedPath;
            if (string.IsNullOrEmpty(destPath) || destPath == "MyComputer" || destPath == "Drives")
                return;
            if (!Directory.Exists(destPath))
            {
                destPath = System.IO.Path.GetDirectoryName(destPath);
                if (!Directory.Exists(destPath)) return;
            }

            await App.FileOperationService.PasteAsync(destPath);
            RefreshNavigation();
        }

        #endregion
    }
}