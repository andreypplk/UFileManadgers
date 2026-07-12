using Core_FileManagement;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace ufm
{
    public sealed partial class TreePanelPg01 : Page, IDisposable, IDropTarget
    {
        #region Поля и свойства

        public static TreePanelPg01 Instance { get; private set; }
        private ModifierKeyService _modifierKeyService = new ModifierKeyService();
        private string _prefsize = "Tree";
        private string _selectedSize = "Tree Medium";
        private string _activePanelId = "MainTree";
        private readonly FileSystemService _fileSystemService;
        private readonly NavigationManager _navigationManager;
        private readonly DirectoryHistory _history;
        private readonly DirectoryHistory _historySpF;
        private bool _disposed = false;
        private bool _isTreeViewSpFInitialized = false;
        private bool _isLoadingSpF = false;
        private bool _isNavigationChangingSelection = false;
        private string _currentSelectedItemPath;
        private HashSet<string> _expandedPaths = new HashSet<string>();
        private HashSet<string> _savedExpandedPaths = new HashSet<string>();
        private string _savedSelectedPath = null;
        private bool _isFirstLoad = true;

        private volatile bool _spfContentLoaded = false;
        private HashSet<string> _specialFolderPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private DragDropService _dragDropService;
        private string _currentTargetFolder;
        private List<string> _lastDragSourcePaths = new List<string>();
        private int _isTreeDropProcessing = 0;

        private bool _isUpdatingSize = false;

        // Флаг: дерево уже восстановлено из снимка
        private bool _snapshotLoaded = false;

        public event EventHandler<string> NavigateRequested;

        public bool ExpandedTreeSelectedSetting =>
            App.SettingsManager?.GetSetting<bool>("ExpandedTreeSelected", true) ?? true;

        public bool ExpanderNodesSFStartsSetting =>
            App.SettingsManager?.GetSetting<bool>("ExpanderNodesSFStarts", true) ?? true;

        public bool ExpanderNodesMyPcStartsSetting =>
            App.SettingsManager?.GetSetting<bool>("ExpanderNodesMyPcStarts", true) ?? true;

        public string CurrentTileSize => ExtractSizePartFromFullKey(_selectedSize);

        #endregion

        #region Конструктор и инициализация

        public TreePanelPg01()
        {
            this.InitializeComponent();
            Instance = this;

            _fileSystemService = new FileSystemService();
            _navigationManager = new NavigationManager();
            _history = new DirectoryHistory("MyComputer", "Мой Компьютер");
            _historySpF = new DirectoryHistory("SpecialFolders", "Специальные папки");

            _navigationManager.RegisterPanel("MainTree", _history);
            _navigationManager.RegisterPanel("SpFTree", _historySpF);
            _navigationManager.NavigationChanged += OnNavigationChanged;

            _dragDropService = new DragDropService(App.FileOperationService, _modifierKeyService);

            this.Loaded += TreePanelPg01_Loaded;
            this.Unloaded += TreePanelPg01_Unloaded;
            this.KeyDown += (s, e) => _modifierKeyService.UpdateKeyState(e.Key, true);
            this.KeyUp += (s, e) => _modifierKeyService.UpdateKeyState(e.Key, false);
            this.KeyDown += TreePanelPg01_KeyDown;

            LoadDefaultExpandedState();

            // treeView
            treeView.Loaded += TreeView_OnLoaded;
            treeView.Expanding += TreeView_Expanding;
            treeView.Collapsed += TreeView_Collapsed;
            treeView.DoubleTapped += TreeView_DoubleTapped;
            treeView.SelectionChanged += TreeView_SelectionChanged;
            treeView.Expanding += (s, e) => AddExpandedPath(e.Node);

            treeView.DragItemsStarting += TreeView_DragItemsStarting;
            treeView.DragOver += TreeView_DragOver;
            treeView.Drop += TreeView_Drop;
            treeView.DragEnter += TreeView_DragEnter;
            treeView.DragLeave += TreeView_DragLeave;

            // treeViewSpF
            treeViewSpF.Loaded += TreeViewSpF_OnLoaded;
            treeViewSpF.Expanding += TreeView_Expanding;
            treeViewSpF.Collapsed += TreeView_Collapsed;
            treeViewSpF.DoubleTapped += TreeView_DoubleTapped;
            treeViewSpF.SelectionChanged += TreeViewSpF_SelectionChanged;
            treeViewSpF.Expanding += (s, e) => AddExpandedPath(e.Node);

            treeViewSpF.DragItemsStarting += TreeView_DragItemsStarting;
            treeViewSpF.DragOver += TreeView_DragOver;
            treeViewSpF.Drop += TreeView_Drop;
            treeViewSpF.DragEnter += TreeView_DragEnter;
            treeViewSpF.DragLeave += TreeView_DragLeave;
        }

        private void LoadDefaultExpandedState()
        {
            try
            {
                var savedPaths = App.SettingsManager?.GetSetting<List<string>>(
                    "TreePanelExpandedPaths", new List<string>());
                _savedExpandedPaths.Clear();
                if (savedPaths != null && savedPaths.Count > 0)
                    foreach (var path in savedPaths)
                        _savedExpandedPaths.Add(path);

                if (!ExpanderNodesMyPcStartsSetting)
                    _savedExpandedPaths.Remove("MyComputer");
                else if (!_savedExpandedPaths.Contains("MyComputer"))
                    _savedExpandedPaths.Add("MyComputer");

                if (!ExpanderNodesSFStartsSetting)
                    _savedExpandedPaths.Remove("SpecialFolders");
                else if (!_savedExpandedPaths.Contains("SpecialFolders"))
                    _savedExpandedPaths.Add("SpecialFolders");
            }
            catch
            {
                _savedExpandedPaths.Clear();
                if (ExpanderNodesMyPcStartsSetting) _savedExpandedPaths.Add("MyComputer");
                if (ExpanderNodesSFStartsSetting) _savedExpandedPaths.Add("SpecialFolders");
            }
        }

        private void TreePanelPg01_Unloaded(object sender, RoutedEventArgs e)
        {
            _savedExpandedPaths.Clear();
            foreach (var path in _expandedPaths)
                _savedExpandedPaths.Add(path);
            _savedSelectedPath = _currentSelectedItemPath;
            try
            {
                App.SettingsManager?.SaveSetting("TreePanelExpandedPaths", _savedExpandedPaths.ToList());
                SaveTreeSnapshot();
            }
            catch { }
            DriveService.DrivesUpdated -= OnDriveServiceUpdated;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    this.Loaded -= TreePanelPg01_Loaded;
                    this.Unloaded -= TreePanelPg01_Unloaded;
                    if (_navigationManager != null)
                        _navigationManager.NavigationChanged -= OnNavigationChanged;

                    if (treeView != null)
                    {
                        treeView.Loaded -= TreeView_OnLoaded;
                        treeView.Expanding -= TreeView_Expanding;
                        treeView.Collapsed -= TreeView_Collapsed;
                        treeView.DoubleTapped -= TreeView_DoubleTapped;
                        treeView.SelectionChanged -= TreeView_SelectionChanged;
                        treeView.DragItemsStarting -= TreeView_DragItemsStarting;
                        treeView.DragOver -= TreeView_DragOver;
                        treeView.Drop -= TreeView_Drop;
                        treeView.DragEnter -= TreeView_DragEnter;
                        treeView.DragLeave -= TreeView_DragLeave;
                    }

                    if (treeViewSpF != null)
                    {
                        treeViewSpF.Loaded -= TreeViewSpF_OnLoaded;
                        treeViewSpF.Expanding -= TreeView_Expanding;
                        treeViewSpF.Collapsed -= TreeView_Collapsed;
                        treeViewSpF.DoubleTapped -= TreeView_DoubleTapped;
                        treeViewSpF.SelectionChanged -= TreeViewSpF_SelectionChanged;
                        treeViewSpF.DragItemsStarting -= TreeView_DragItemsStarting;
                        treeViewSpF.DragOver -= TreeView_DragOver;
                        treeViewSpF.Drop -= TreeView_Drop;
                        treeViewSpF.DragEnter -= TreeView_DragEnter;
                        treeViewSpF.DragLeave -= TreeView_DragLeave;
                    }

                    _fileSystemService?.Dispose();
                    _history?.Dispose();
                    _historySpF?.Dispose();
                }
                catch { }
                finally
                {
                    Instance = null;
                    GC.SuppressFinalize(this);
                    _disposed = true;
                }
            }
        }

        #endregion

        #region IDropTarget implementation

        public string GetTargetFolder() => _currentTargetFolder;

        #endregion

        #region Сохранение и восстановление состояния дерева

        private void AddExpandedPath(TreeViewNode node)
        {
            if (node.Content is ExplorerItemViewModel item && !string.IsNullOrEmpty(item.FilePath))
                _expandedPaths.Add(item.FilePath);
        }

        private void RestoreExpandedState(TreeView tree, IList<TreeViewNode> nodes, string treeName)
        {
            if (nodes == null) return;
            foreach (var node in nodes)
            {
                if (node.Content is ExplorerItemViewModel item)
                {
                    bool canExpand = true;
                    if (item.FilePath == "MyComputer")
                        canExpand = ExpanderNodesMyPcStartsSetting;
                    else if (item.FilePath == "SpecialFolders")
                        canExpand = ExpanderNodesSFStartsSetting;

                    if (canExpand && _expandedPaths.Contains(item.FilePath) && !node.IsExpanded && (node.Children.Count == 0 || node.HasUnrealizedChildren))
                        tree.Expand(node);
                }
                RestoreExpandedState(tree, node.Children, treeName + "->children");
            }
        }

        #endregion

        #region Обработчики событий страницы

        private async void TreePanelPg01_Loaded(object sender, RoutedEventArgs e)
        {
            _expandedPaths.Clear();
            foreach (var path in _savedExpandedPaths)
                _expandedPaths.Add(path);

            if (!ExpanderNodesMyPcStartsSetting)
                _expandedPaths.Remove("MyComputer");
            else if (!_expandedPaths.Contains("MyComputer"))
                _expandedPaths.Add("MyComputer");

            if (!ExpanderNodesSFStartsSetting)
                _expandedPaths.Remove("SpecialFolders");
            else if (!_expandedPaths.Contains("SpecialFolders"))
                _expandedPaths.Add("SpecialFolders");

            string savedSize = App.SettingsManager?.GetSetting<string>("SelectedSizeIconTreeView");
            if (string.IsNullOrEmpty(savedSize))
            {
                var localSettings = ApplicationData.Current.LocalSettings.Values;
                if (localSettings?.ContainsKey("SelectedSizeIconTreeView") == true)
                    savedSize = localSettings["SelectedSizeIconTreeView"]?.ToString();
            }

            string sizePart = SizeHelper.ExtractSizePartFromFullKey(savedSize ?? "");
            _prefsize = "Tree";
            _selectedSize = $"{_prefsize} {sizePart}";

            try
            {
                App.SettingsManager?.SaveSetting("SelectedSizeIconTreeView", _selectedSize);
            }
            catch
            {
                ApplicationData.Current.LocalSettings.Values["SelectedSizeIconTreeView"] = _selectedSize;
            }

            SetSelectedRadioButton(_selectedSize);
            UpdateAllTiles();

            // Попытка восстановить дерево из снимка
            if (!_isFirstLoad || !TryLoadTreeSnapshot())
            {
                if (!_isFirstLoad)
                {
                    _ = Task.Run(async () =>
                    {
                        while (!_spfContentLoaded) await Task.Delay(50);
                        await DispatcherQueue.EnqueueAsync(async () =>
                        {
                            await Task.Delay(100);
                            RestoreExpandedState(treeView, treeView.RootNodes, "treeView");
                            RestoreExpandedState(treeViewSpF, treeViewSpF.RootNodes, "treeViewSpF");
                            if (!string.IsNullOrEmpty(_savedSelectedPath))
                                UpdateTreeViewSelection(_activePanelId, _savedSelectedPath);
                            ScheduleUpdateAllTiles();
                        });
                    });
                }
                else
                {
                    _isFirstLoad = false;
                    DriveService.DrivesUpdated += OnDriveServiceUpdated;
                }
            }
            else
            {
                _snapshotLoaded = true;
                _isFirstLoad = false;
                DriveService.DrivesUpdated += OnDriveServiceUpdated;
                if (!string.IsNullOrEmpty(_savedSelectedPath))
                    UpdateTreeViewSelection(_activePanelId, _savedSelectedPath);
                ScheduleUpdateAllTiles();
            }
        }

        #endregion

        #region Управление размерами элементов

        private string ExtractSizePartFromFullKey(string fullSizeKey)
        {
            if (string.IsNullOrEmpty(fullSizeKey)) return "Medium";
            var parts = fullSizeKey.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
            {
                string potential = $"{parts[parts.Length - 3]} {parts[parts.Length - 2]} {parts[parts.Length - 1]}";
                if (potential == "Below Medium" || potential == "Above Medium") return potential;
            }
            if (parts.Length >= 2)
            {
                string potential = $"{parts[parts.Length - 2]} {parts[parts.Length - 1]}";
                if (potential == "Extra Small" || potential == "Extra Large") return potential;
            }
            return parts[^1];
        }

        private void SetSelectedRadioButton(string fullSizeKey)
        {
            string sizePart = ExtractSizePartFromFullKey(fullSizeKey);
            _isUpdatingSize = true;
            try
            {
                TinySizeRadioButton.IsChecked = false;
                ExtraSmallSizeRadioButton.IsChecked = false;
                SmallSizeRadioButton.IsChecked = false;
                BelowMediumSizeRadioButton.IsChecked = false;
                MediumSizeRadioButton.IsChecked = false;
                AboveMediumSizeRadioButton.IsChecked = false;
                LargeSizeRadioButton.IsChecked = false;
                ExtraLargeSizeRadioButton.IsChecked = false;
                HugeSizeRadioButton.IsChecked = false;

                switch (sizePart)
                {
                    case "Tiny": TinySizeRadioButton.IsChecked = true; break;
                    case "Extra Small": ExtraSmallSizeRadioButton.IsChecked = true; break;
                    case "Small": SmallSizeRadioButton.IsChecked = true; break;
                    case "Below Medium": BelowMediumSizeRadioButton.IsChecked = true; break;
                    case "Medium": MediumSizeRadioButton.IsChecked = true; break;
                    case "Above Medium": AboveMediumSizeRadioButton.IsChecked = true; break;
                    case "Large": LargeSizeRadioButton.IsChecked = true; break;
                    case "Extra Large": ExtraLargeSizeRadioButton.IsChecked = true; break;
                    case "Huge": HugeSizeRadioButton.IsChecked = true; break;
                    default: MediumSizeRadioButton.IsChecked = true; break;
                }
            }
            finally
            {
                _isUpdatingSize = false;
            }
        }

        private void SizeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingSize) return;
            if (sender is RadioButton radioButton && radioButton.Tag is string sizeTag && radioButton.IsChecked == true)
            {
                _selectedSize = $"{_prefsize} {sizeTag}";
                try
                {
                    App.SettingsManager?.SaveSetting("SelectedSizeIconTreeView", _selectedSize);
                }
                catch
                {
                    ApplicationData.Current.LocalSettings.Values["SelectedSizeIconTreeView"] = _selectedSize;
                }
                UpdateAllTiles();
            }
        }

        #endregion

        #region Работа с основным TreeView

        private async void TreeView_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_snapshotLoaded)
                return;

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
                IsExpanded = ExpanderNodesMyPcStartsSetting,
                HasUnrealizedChildren = true
            };
            treeView.RootNodes.Add(myComputerNode);

            DispatcherQueue.TryEnqueue(async () =>
            {
                UpdateTileSize(myComputerNode);
                if (ExpanderNodesMyPcStartsSetting)
                {
                    await LoadDrivesSync(myComputerNode);
                    await PreloadFirstLevelAsync(myComputerNode);
                    myComputerNode.IsExpanded = true;
                    treeView.Expand(myComputerNode);
                }
                else
                {
                    myComputerNode.IsExpanded = false;
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
                        var items = await _fileSystemService.LoadFoldersOnlyAsync(driveItem.FilePath, _history);
                        foreach (var item in items.Take(3))
                        {
                            item.IsTreeViewNode = true;
                            driveNode.Children.Add(new TreeViewNode { Content = item, HasUnrealizedChildren = true });
                        }
                    }
                    catch { }
                }
            }
        }

        #endregion

        #region Работа с TreeView специальных папок

        private void TreeViewSpF_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_snapshotLoaded || _isTreeViewSpFInitialized)
                return;

            treeViewSpF.ItemsSource = null;
            treeViewSpF.RootNodes.Clear();
            var specialFoldersNode = new TreeViewNode
            {
                Content = _fileSystemService.CreateSpecialFoldersItem(_historySpF),
                IsExpanded = ExpanderNodesSFStartsSetting,
                HasUnrealizedChildren = true
            };
            treeViewSpF.RootNodes.Add(specialFoldersNode);
            _isTreeViewSpFInitialized = true;
            UpdateAllTiles();

            DispatcherQueue.TryEnqueue(async () =>
            {
                await LoadHomeContentsAsync(specialFoldersNode);
                _specialFolderPaths.Clear();
                foreach (var child in specialFoldersNode.Children)
                    if (child.Content is ExplorerItemViewModel childItem && !string.IsNullOrEmpty(childItem.FilePath))
                        _specialFolderPaths.Add(childItem.FilePath);

                await PreloadFirstLevelForSpFAsync(specialFoldersNode);
                if (ExpanderNodesSFStartsSetting)
                {
                    specialFoldersNode.IsExpanded = true;
                    treeViewSpF.Expand(specialFoldersNode);
                }
                else
                    specialFoldersNode.IsExpanded = false;
                _spfContentLoaded = true;
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
                        var items = await _fileSystemService.LoadFoldersOnlyAsync(folderItem.FilePath, _historySpF);
                        foreach (var item in items.Take(3))
                        {
                            item.IsTreeViewNode = true;
                            var node = new TreeViewNode { Content = item, HasUnrealizedChildren = true };
                            systemFolderNode.Children.Add(node);
                            UpdateTileSizeSpF(node);
                        }
                    }
                    catch { }
                }
            }
        }

        private async Task LoadHomeContentsAsync(TreeViewNode parentNode)
        {
            if (parentNode.Children.Count > 0) return;
            try
            {
                parentNode.Children.Clear();
                var homeItems = await _fileSystemService.LoadHomeAsync("TreeViewSpF", _historySpF);
                foreach (var item in homeItems)
                {
                    item.IsTreeViewNode = true;
                    var node = new TreeViewNode { Content = item, HasUnrealizedChildren = Directory.Exists(item.FilePath) };
                    parentNode.Children.Add(node);
                    UpdateTileSizeSpF(node);
                }
            }
            catch { }
        }

        #endregion

        #region Общие обработчики для обоих деревьев

        private async void TreeView_Expanding(TreeView sender, TreeViewExpandingEventArgs args)
        {
            if (args.Node.Content is not ExplorerItemViewModel treeItem) return;
            if (!args.Node.HasUnrealizedChildren) return;

            try
            {
                if (sender == treeView)
                {
                    if (treeItem.Name == "Мой Компьютер")
                    {
                        args.Node.HasUnrealizedChildren = false;
                        await LoadDrivesSync(args.Node);
                    }
                    else
                    {
                        args.Node.HasUnrealizedChildren = false;
                        await LoadSubfoldersAsync(args.Node, treeItem.FilePath);
                    }
                    UpdateAllTiles();
                }
                else if (sender == treeViewSpF)
                {
                    if (_isLoadingSpF) return;
                    _isLoadingSpF = true;
                    try
                    {
                        if (treeItem.FilePath == "SpecialFolders")
                        {
                            args.Node.HasUnrealizedChildren = false;
                            await LoadHomeContentsAsync(args.Node);
                            _specialFolderPaths.Clear();
                            foreach (var child in args.Node.Children)
                                if (child.Content is ExplorerItemViewModel childItem && !string.IsNullOrEmpty(childItem.FilePath))
                                    _specialFolderPaths.Add(childItem.FilePath);
                        }
                        else
                        {
                            args.Node.HasUnrealizedChildren = false;
                            await LoadSubfoldersForSpFAsync(args.Node, treeItem.FilePath);
                        }
                    }
                    finally
                    {
                        _isLoadingSpF = false;
                        UpdateAllTiles();
                    }
                }
            }
            catch { }
        }

        private void TreeView_Collapsed(TreeView sender, TreeViewCollapsedEventArgs args)
        {
        }

        private void TreeView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            TreeView tree = sender as TreeView;
            if (tree == null || tree.SelectedNode == null) return;

            if (!tree.SelectedNode.IsExpanded)
                tree.Expand(tree.SelectedNode);
            else
                tree.Collapse(tree.SelectedNode);

            if (tree.SelectedNode?.Content is ExplorerItemViewModel item)
                OnNavigateRequested(item.FilePath);
        }

        #endregion

        #region Методы работы с узлами TreeView

        private async Task LoadDrivesSync(TreeViewNode parentNode)
        {
            try
            {
                parentNode.Children.Clear();
                var driveItems = await _fileSystemService.LoadDrivesTree(_history);
                foreach (var driveItem in driveItems)
                {
                    driveItem.IsTreeViewNode = true;
                    parentNode.Children.Add(new TreeViewNode { Content = driveItem, HasUnrealizedChildren = true });
                }
            }
            catch { }
        }

        private async Task LoadSubfoldersAsync(TreeViewNode parentNode, string folderPath)
        {
            try
            {
                parentNode.Children.Clear();
                var folderItems = await _fileSystemService.LoadSubfoldersForTreeViewAsync(folderPath, _history);
                foreach (var item in folderItems)
                {
                    item.IsTreeViewNode = true;
                    var node = new TreeViewNode { Content = item, HasUnrealizedChildren = true };
                    parentNode.Children.Add(node);
                    UpdateTileSize(node);
                }
            }
            catch { }
        }

        private async Task LoadSubfoldersForSpFAsync(TreeViewNode parentNode, string folderPath)
        {
            try
            {
                parentNode.Children.Clear();
                var folderItems = await _fileSystemService.LoadFoldersOnlyAsync(folderPath, _historySpF);
                foreach (var item in folderItems)
                {
                    item.IsTreeViewNode = true;
                    var node = new TreeViewNode { Content = item, HasUnrealizedChildren = true };
                    parentNode.Children.Add(node);
                    UpdateTileSizeSpF(node);
                }
            }
            catch { }
        }

        #endregion

        #region Обновление отображения элементов

        private void UpdateAllTiles()
        {
            if (treeView?.RootNodes?.Count > 0)
                foreach (var node in treeView.RootNodes) UpdateTileSize(node);
            if (treeViewSpF?.RootNodes?.Count > 0)
                foreach (var node in treeViewSpF.RootNodes) UpdateTileSizeSpF(node);
        }

        private void UpdateTileSize(TreeViewNode node)
        {
            if (node == null) return;
            var container = treeView?.ContainerFromNode(node) as TreeViewItem;
            var tile = container?.ContentTemplateRoot as BaseTileControl;
            if (tile != null) tile.UpdateSize(_selectedSize);
            if (node.IsExpanded)
                foreach (var childNode in node.Children) UpdateTileSize(childNode);
        }

        private void UpdateTileSizeSpF(TreeViewNode node)
        {
            if (node == null) return;
            var container = treeViewSpF?.ContainerFromNode(node) as TreeViewItem;
            var tile = container?.ContentTemplateRoot as BaseTileControl;
            if (tile != null) tile.UpdateSize(_selectedSize);
            if (node.IsExpanded)
                foreach (var childNode in node.Children) UpdateTileSizeSpF(childNode);
        }

        private void ScheduleUpdateAllTiles()
        {
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => UpdateAllTiles());
        }

        #endregion

        #region Управление поиском

        private ExplorerItemViewModel FindItemByPath(IList<TreeViewNode> nodes, string path)
        {
            if (nodes == null) return null;
            foreach (var node in nodes)
            {
                if (node?.Content is ExplorerItemViewModel item && NormalizePath(item.FilePath) == NormalizePath(path))
                    return item;
                if (node?.Children?.Count > 0)
                {
                    var found = FindItemByPath(node.Children, path);
                    if (found != null) return found;
                }
            }
            return null;
        }

        private bool IsVirtualPath(string path) => path == ".." || path == "Drives";

        private string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path) || IsVirtualPath(path) || path == "SpecialFolders" || path == "MyComputer")
                return path;
            try
            {
                if (path.Length == 2 && path[1] == ':') return path + System.IO.Path.DirectorySeparatorChar;
                if (path.Length == 3 && path[1] == ':' && path[2] == '\\') return path;
                string fullPath = System.IO.Path.GetFullPath(path);
                return fullPath.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
            }
            catch { return path; }
        }

        #endregion

        #region Управление навигацией

        private void OnNavigationChanged(object sender, NavigationEventArgs e)
        {
            if (e.PanelId == "MainTree" || e.PanelId == "SpFTree")
                _activePanelId = e.PanelId;
            else
                _activePanelId ??= "MainTree";

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

        private async void UpdateTreeViewSelection(string panelId, string path)
        {
            try
            {
                TreeView targetTreeView = panelId == "MainTree" ? treeView : treeViewSpF;
                if (targetTreeView == null || string.IsNullOrEmpty(path))
                    return;

                _isNavigationChangingSelection = true;

                if (!IsVirtualPath(path))
                {
                    foreach (var rootNode in targetTreeView.RootNodes)
                    {
                        if (rootNode?.Content is ExplorerItemViewModel rootItem)
                        {
                            if (rootItem.FilePath == "MyComputer")
                                await EnsureMyComputerExpanded(rootNode);
                            else if (rootItem.FilePath == "SpecialFolders")
                                await EnsureSpecialFoldersExpanded(rootNode);
                        }
                    }
                }

                if (IsVirtualPath(path))
                {
                    var targetItem = FindItemByPath(targetTreeView.RootNodes, path);
                    if (targetItem != null)
                    {
                        targetTreeView.SelectedItem = targetItem;
                        _currentSelectedItemPath = targetItem.FilePath;
                        _activePanelId = panelId;
                        ScrollToSelectedItem(targetTreeView, targetItem);
                    }
                    return;
                }

                if (panelId == "SpFTree")
                {
                    var targetItem = FindItemByPath(targetTreeView.RootNodes, path);
                    if (targetItem != null)
                    {
                        targetTreeView.SelectedItem = targetItem;
                        _currentSelectedItemPath = targetItem.FilePath;
                        _activePanelId = panelId;

                        var node = FindNodeByItem(targetTreeView.RootNodes, targetItem);
                        if (node != null)
                        {
                            if (node.HasUnrealizedChildren)
                                await ExpandNode(node, targetItem.FilePath, panelId);
                            if (!node.IsExpanded)
                                targetTreeView.Expand(node);
                        }
                        ScrollToSelectedItem(targetTreeView, targetItem);
                    }
                    return;
                }

                var pathSegments = SplitPath(path);
                if (pathSegments.Length == 0) return;

                bool found = await ExpandAndSelectPath(targetTreeView.RootNodes, pathSegments, 0, panelId);

                if (!found)
                {
                    var simpleItem = FindItemByPath(targetTreeView.RootNodes, path);
                    if (simpleItem != null)
                    {
                        targetTreeView.SelectedItem = simpleItem;
                        _currentSelectedItemPath = simpleItem.FilePath;
                        _activePanelId = panelId;
                        ScrollToSelectedItem(targetTreeView, simpleItem);
                    }
                }
            }
            catch { }
            finally
            {
                _isNavigationChangingSelection = false;
                UpdateNavigationButtons();
                ScheduleUpdateAllTiles();
            }
        }

        private async Task EnsureMyComputerExpanded(TreeViewNode myComputerNode)
        {
            if (myComputerNode?.Content is ExplorerItemViewModel item && item.FilePath == "MyComputer")
            {
                if (!myComputerNode.IsExpanded)
                    treeView.Expand(myComputerNode);
            }
        }

        private async Task EnsureSpecialFoldersExpanded(TreeViewNode specialFoldersNode)
        {
            if (specialFoldersNode?.Content is ExplorerItemViewModel item && item.FilePath == "SpecialFolders")
            {
                if (!specialFoldersNode.IsExpanded)
                    treeViewSpF.Expand(specialFoldersNode);
            }
        }

        private async Task ExpandNode(TreeViewNode node, string path, string panelId)
        {
            if (!node.HasUnrealizedChildren)
                return;

            try
            {
                if (panelId == "MainTree")
                {
                    node.HasUnrealizedChildren = false;
                    await LoadSubfoldersAsync(node, path);
                }
                else
                {
                    node.HasUnrealizedChildren = false;
                    await LoadSubfoldersForSpFAsync(node, path);
                }

                var targetTreeView = panelId == "MainTree" ? treeView : treeViewSpF;
                if (!node.IsExpanded)
                    targetTreeView.Expand(node);

                await Task.Delay(20);
            }
            catch { }
        }

        private async Task<bool> ExpandAndSelectPath(IList<TreeViewNode> nodes, string[] pathSegments, int currentIndex, string panelId)
        {
            if (currentIndex >= pathSegments.Length) return false;

            string currentPath = pathSegments[currentIndex];
            bool isLastSegment = currentIndex == pathSegments.Length - 1;

            foreach (var node in nodes)
            {
                if (node?.Content is not ExplorerItemViewModel item) continue;

                string normalizedItemPath = NormalizePath(item.FilePath);
                string normalizedCurrentPath = NormalizePath(currentPath);

                if (normalizedItemPath == normalizedCurrentPath)
                {
                    if (isLastSegment)
                    {
                        var targetTreeView = panelId == "MainTree" ? treeView : treeViewSpF;
                        targetTreeView.SelectedItem = item;
                        _currentSelectedItemPath = item.FilePath;
                        _activePanelId = panelId;

                        await ExpandNode(node, item.FilePath, panelId);

                        if (!node.IsExpanded)
                            targetTreeView.Expand(node);

                        ScheduleUpdateAllTiles();
                        ScrollToSelectedItem(targetTreeView, item);
                        return true;
                    }
                    else
                    {
                        if (!node.IsExpanded)
                        {
                            await ExpandNode(node, item.FilePath, panelId);
                            ScheduleUpdateAllTiles();
                        }

                        bool found = await ExpandAndSelectPath(node.Children, pathSegments, currentIndex + 1, panelId);
                        if (found) return true;
                    }
                }
                else
                {
                    if (node.Children?.Count > 0)
                    {
                        bool foundInChildren = await ExpandAndSelectPath(node.Children, pathSegments, currentIndex, panelId);
                        if (foundInChildren) return true;
                    }
                }
            }
            return false;
        }

        private TreeViewNode FindNodeByItem(IList<TreeViewNode> nodes, ExplorerItemViewModel targetItem)
        {
            if (nodes == null) return null;
            foreach (var node in nodes)
            {
                if (node.Content == targetItem) return node;
                if (node.Children.Count > 0)
                {
                    var found = FindNodeByItem(node.Children, targetItem);
                    if (found != null) return found;
                }
            }
            return null;
        }

        private void ScrollToSelectedItem(TreeView targetTreeView, object item)
        {
            if (targetTreeView == null || item == null) return;
            try
            {
                DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                {
                    var container = targetTreeView.ContainerFromItem(item) as TreeViewItem;
                    if (container != null)
                    {
                        container.StartBringIntoView(new BringIntoViewOptions
                        {
                            AnimationDesired = true,
                            VerticalAlignmentRatio = 0.5
                        });
                    }
                });
            }
            catch { }
        }

        private string[] SplitPath(string path)
        {
            if (string.IsNullOrEmpty(path) || IsVirtualPath(path))
                return new[] { path };

            try
            {
                var normalized = NormalizePath(path);
                var result = new List<string>();

                if (normalized.Length == 3 && normalized[1] == ':' && normalized[2] == '\\')
                {
                    result.Add(normalized);
                    return result.ToArray();
                }

                string root = System.IO.Path.GetPathRoot(normalized);
                string[] parts = normalized.Substring(root.Length)
                    .Split(new[] { System.IO.Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

                string currentPath = root.TrimEnd(System.IO.Path.DirectorySeparatorChar);
                if (!string.IsNullOrEmpty(root))
                {
                    string rootPath = root.TrimEnd(System.IO.Path.DirectorySeparatorChar);
                    if (rootPath.Length == 2 && rootPath[1] == ':')
                        rootPath += System.IO.Path.DirectorySeparatorChar;
                    result.Add(rootPath);
                    currentPath = rootPath;
                }

                foreach (var part in parts)
                {
                    currentPath = System.IO.Path.Combine(currentPath, part);
                    result.Add(currentPath);
                }

                return result.ToArray();
            }
            catch
            {
                return new[] { path };
            }
        }

        private void UpdateNavigationButtons()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    var activeHistory = GetActiveHistory();
                    bool upButtonEnabled = false;
                    if (!string.IsNullOrEmpty(_currentSelectedItemPath) && Directory.Exists(_currentSelectedItemPath))
                    {
                        var parent = Directory.GetParent(_currentSelectedItemPath);
                        upButtonEnabled = parent != null && Directory.Exists(parent.FullName);
                    }
                    BackButton.IsEnabled = activeHistory?.CanMoveBack ?? false;
                    ForwardButton.IsEnabled = activeHistory?.CanMoveForward ?? false;
                    UpButton.IsEnabled = upButtonEnabled;
                }
                catch { }
            });
        }

        private void TreeViewSpF_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
        {
            if (_isNavigationChangingSelection) return;
            if (treeViewSpF.SelectedNode?.Content is ExplorerItemViewModel selectedItem)
            {
                if (_currentSelectedItemPath == selectedItem.FilePath) return;
                _currentSelectedItemPath = selectedItem.FilePath;
                _activePanelId = "SpFTree";
                if (treeView.SelectedItem != null) treeView.SelectedItem = null;
                _navigationManager.NavigateTo(selectedItem.FilePath, "SpFTree");
                UpdateNavigationButtons();
                if (!IsVirtualPath(selectedItem.FilePath))
                    OnNavigateRequested(selectedItem.FilePath);
            }
        }

        private void TreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
        {
            if (_isNavigationChangingSelection) return;
            if (treeView.SelectedNode?.Content is ExplorerItemViewModel selectedItem)
            {
                if (_currentSelectedItemPath == selectedItem.FilePath) return;
                _currentSelectedItemPath = selectedItem.FilePath;
                _activePanelId = "MainTree";
                if (treeViewSpF.SelectedItem != null) treeViewSpF.SelectedItem = null;
                _navigationManager.NavigateTo(selectedItem.FilePath, "MainTree");
                UpdateNavigationButtons();
                if (!IsVirtualPath(selectedItem.FilePath))
                    OnNavigateRequested(selectedItem.FilePath);
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
                var newPath = GetActiveHistory()?.Current?.DirectoryPath;
                if (!string.IsNullOrEmpty(newPath)) OnNavigateRequested(newPath);
            }
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            string activePanel = GetActivePanel();
            if (_navigationManager.CanGoForward(activePanel))
            {
                _navigationManager.GoForward(activePanel);
                var newPath = GetActiveHistory()?.Current?.DirectoryPath;
                if (!string.IsNullOrEmpty(newPath)) OnNavigateRequested(newPath);
            }
        }

        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(_currentSelectedItemPath) && Directory.Exists(_currentSelectedItemPath))
                {
                    var parentDir = Directory.GetParent(_currentSelectedItemPath);
                    if (parentDir != null)
                    {
                        string activePanel = GetActivePanel();
                        _navigationManager.NavigateTo(parentDir.FullName, activePanel);
                        OnNavigateRequested(parentDir.FullName);
                    }
                }
            }
            catch { }
        }

        private string GetActivePanel() => _activePanelId ?? "MainTree";
        private DirectoryHistory GetActiveHistory() => GetActivePanel() == "SpFTree" ? _historySpF : _history;

        #endregion

        #region Публичные методы для внешнего взаимодействия

        private void OnNavigateRequested(string path) => NavigateRequested?.Invoke(this, path);

        public void SelectPath(string path)
        {
            string panelId;
            string treePath = path;

            if (path == "Drives" || path == "MyComputer")
            {
                panelId = "MainTree";
                treePath = "MyComputer";
                if (treeViewSpF.SelectedNodes.Count > 0 || treeViewSpF.SelectedItem != null)
                {
                    treeViewSpF.SelectedNodes.Clear();
                    treeViewSpF.SelectedItem = null;
                    treeViewSpF.UpdateLayout();
                }
            }
            else if (_specialFolderPaths.Contains(path) || path == "SpecialFolders" || path.StartsWith("SpecialFolders"))
            {
                panelId = "SpFTree";
                treePath = path;
                if (treeView.SelectedNodes.Count > 0 || treeView.SelectedItem != null)
                {
                    treeView.SelectedNodes.Clear();
                    treeView.SelectedItem = null;
                    treeView.UpdateLayout();
                }
            }
            else
            {
                panelId = "MainTree";
                if (treeViewSpF.SelectedNodes.Count > 0 || treeViewSpF.SelectedItem != null)
                {
                    treeViewSpF.SelectedNodes.Clear();
                    treeViewSpF.SelectedItem = null;
                    treeViewSpF.UpdateLayout();
                }
            }

            UpdateTreeViewSelection(panelId, treePath);
            ScheduleUpdateAllTiles();
        }

        public void NavigateToPath(string path)
        {
            string panelId;
            if (_specialFolderPaths.Contains(path) || path == "SpecialFolders" || path.StartsWith("SpecialFolders"))
                panelId = "SpFTree";
            else
                panelId = "MainTree";

            _isNavigationChangingSelection = true;
            try
            {
                _navigationManager.NavigateTo(path, panelId);
            }
            finally
            {
                _isNavigationChangingSelection = false;
            }
        }

        public async void RefreshAfterDrop(IEnumerable<string> sourcePaths, string targetPath)
        {
            var paths = sourcePaths?.ToList() ?? new List<string>();

            if (paths.Count > 0)
                await RefreshSourceNodesAsync(paths);

            if (!string.IsNullOrEmpty(targetPath))
            {
                _fileSystemService.InvalidateIconCache(targetPath, true);

                string addedPath = await SyncTargetNodeAsync(targetPath);
                if (!string.IsNullOrEmpty(addedPath))
                {
                    SelectPath(addedPath);
                    NavigateToPath(addedPath);
                }
            }
        }

        public void RefreshNodeAfterFileOperation(string folderPath)
        {
            _ = SyncTargetNodeAsync(folderPath);
        }

        private IEnumerable<ExplorerItemViewModel> GetSelectedItems(TreeView tree)
        {
            var list = new List<ExplorerItemViewModel>();
            foreach (var node in tree.SelectedNodes)
                if (node.Content is ExplorerItemViewModel vm) list.Add(vm);
            return list;
        }

        private async void TreePanelPg01_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            bool isCtrl = _modifierKeyService.IsCtrlPressed;
            if (!isCtrl) return;

            if (e.Key == Windows.System.VirtualKey.C)
            {
                var items = GetSelectedItems(treeView).Concat(GetSelectedItems(treeViewSpF));
                if (items.Any()) App.FileOperationService?.Copy(items);
                e.Handled = true;
            }
            else if (e.Key == Windows.System.VirtualKey.X)
            {
                var items = GetSelectedItems(treeView).Concat(GetSelectedItems(treeViewSpF));
                if (items.Any()) App.FileOperationService?.Cut(items);
                e.Handled = true;
            }
            else if (e.Key == Windows.System.VirtualKey.V)
            {
                TreeView activeTree = treeView.FocusState != FocusState.Unfocused ? treeView : treeViewSpF;
                if (activeTree.SelectedNode?.Content is ExplorerItemViewModel targetVm)
                {
                    string destPath = targetVm.FilePath;
                    if (Directory.Exists(destPath))
                    {
                        await App.FileOperationService.PasteAsync(destPath);
                        if (activeTree.SelectedNode.Children.Count > 0 || activeTree.SelectedNode.HasUnrealizedChildren)
                        {
                            activeTree.SelectedNode.Children.Clear();
                            activeTree.SelectedNode.HasUnrealizedChildren = true;
                            activeTree.Expand(activeTree.SelectedNode);
                        }
                    }
                }
                e.Handled = true;
            }
        }

        #endregion

        #region Drag-and-drop handlers (общие для обоих деревьев)

        private void TreeView_DragItemsStarting(TreeView sender, TreeViewDragItemsStartingEventArgs args)
        {
            var paths = args.Items.OfType<ExplorerItemViewModel>()
                                 .Select(vm => vm.FilePath)
                                 .Where(p => !string.IsNullOrEmpty(p))
                                 .ToList();
            if (paths.Count == 0) return;
            _lastDragSourcePaths = new List<string>(paths);
            _dragDropService.OnDragItemsStarting(paths, args);
        }

        private void TreeView_DragOver(object sender, DragEventArgs e)
        {
            TreeView tree = sender as TreeView;
            if (tree == null) return;

            _modifierKeyService.UpdateKeyStateFromCore();

            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var target = GetTargetFolderUnderPointer(e, tree);

                if (!string.IsNullOrEmpty(target) && Directory.Exists(target))
                {
                    _currentTargetFolder = target;
                    _currentSelectedItemPath = target;
                }
                else if (!string.IsNullOrEmpty(_currentSelectedItemPath) && Directory.Exists(_currentSelectedItemPath))
                {
                    _currentTargetFolder = _currentSelectedItemPath;
                }
                else
                {
                    e.AcceptedOperation = DataPackageOperation.None;
                    e.Handled = true;
                    return;
                }
            }

            _dragDropService.OnDragOver(e, _currentTargetFolder ?? _currentSelectedItemPath, true);
            e.Handled = true;
        }

        private async void TreeView_Drop(object sender, DragEventArgs e)
        {
            TreeView tree = sender as TreeView;
            if (tree == null) return;

            if (Interlocked.CompareExchange(ref _isTreeDropProcessing, 1, 0) != 0)
                return;

            try
            {
                _modifierKeyService.UpdateKeyStateFromCore();

                string targetFolder = null;
                if (e.DataView.Contains(StandardDataFormats.StorageItems))
                    targetFolder = GetTargetFolderUnderPointer(e, tree);

                if (string.IsNullOrEmpty(targetFolder))
                    targetFolder = _currentTargetFolder ?? _currentSelectedItemPath;

                if (!Directory.Exists(targetFolder))
                {
                    e.Handled = true;
                    return;
                }

                var sourcePaths = new List<string>();
                if (e.DataView.Contains(StandardDataFormats.StorageItems))
                {
                    var storageItems = await e.DataView.GetStorageItemsAsync();
                    sourcePaths = storageItems.Select(i => i.Path).Where(p => !string.IsNullOrEmpty(p)).ToList();
                }

                if (sourcePaths.Count > 0)
                {
                    await _dragDropService.OnDropAsync(e, targetFolder);
                }

                _fileSystemService.InvalidateIconCache(targetFolder, true);

                e.Handled = true;

                await RefreshSourceNodesAsync(sourcePaths);
                TileViewerContent.RefreshAfterDrop(sourcePaths);

                string addedPath = await SyncTargetNodeAsync(targetFolder);
                if (!string.IsNullOrEmpty(addedPath))
                {
                    SelectPath(addedPath);
                    NavigateToPath(addedPath);
                }
            }
            catch { }
            finally
            {
                Interlocked.Exchange(ref _isTreeDropProcessing, 0);
            }
        }

        private string GetTargetFolderUnderPointer(DragEventArgs e, TreeView tree)
        {
            var point = e.GetPosition(tree);

            string FindInNodes(IList<TreeViewNode> nodes)
            {
                foreach (var node in nodes)
                {
                    var container = tree.ContainerFromNode(node) as TreeViewItem;
                    if (container != null)
                    {
                        try
                        {
                            var transform = container.TransformToVisual(tree);
                            var rect = transform.TransformBounds(new Windows.Foundation.Rect(0, 0, container.ActualWidth, container.ActualHeight));
                            if (rect.Contains(point))
                            {
                                if (node.Content is ExplorerItemViewModel item &&
                                    !string.IsNullOrEmpty(item.FilePath) &&
                                    Directory.Exists(item.FilePath))
                                {
                                    return item.FilePath;
                                }
                            }
                        }
                        catch { }
                    }

                    if (node.Children.Count > 0)
                    {
                        var found = FindInNodes(node.Children);
                        if (found != null)
                            return found;
                    }
                }
                return null;
            }

            return FindInNodes(tree.RootNodes);
        }

        private void TreeView_DragEnter(object sender, DragEventArgs e)
        {
            _modifierKeyService.UpdateKeyStateFromCore();
            e.Handled = true;
        }

        private void TreeView_DragLeave(object sender, DragEventArgs e)
        {
            _modifierKeyService.UpdateKeyStateFromCore();
            e.Handled = true;
        }

        #endregion

        #region Приватные методы синхронизации узлов

        private async Task RefreshSourceNodesAsync(List<string> sourcePaths)
        {
            if (sourcePaths == null || sourcePaths.Count == 0)
                return;

            foreach (var sourcePath in sourcePaths)
            {
                if (!Directory.Exists(sourcePath) && !File.Exists(sourcePath))
                {
                    try
                    {
                        var parentPath = System.IO.Path.GetDirectoryName(sourcePath);
                        if (string.IsNullOrEmpty(parentPath))
                            continue;

                        var parentItem = FindItemByPath(treeView.RootNodes, parentPath);
                        TreeViewNode parentNode = null;
                        IList<TreeViewNode> childrenCollection = null;

                        if (parentItem != null)
                        {
                            parentNode = FindNodeByItem(treeView.RootNodes, parentItem);
                            childrenCollection = parentNode?.Children;
                        }
                        else
                        {
                            parentItem = FindItemByPath(treeViewSpF.RootNodes, parentPath);
                            if (parentItem != null)
                            {
                                parentNode = FindNodeByItem(treeViewSpF.RootNodes, parentItem);
                                childrenCollection = parentNode?.Children;
                            }
                        }

                        if (childrenCollection != null)
                        {
                            TreeViewNode nodeToRemove = null;
                            foreach (var child in childrenCollection)
                            {
                                if (child.Content is ExplorerItemViewModel childItem &&
                                    NormalizePath(childItem.FilePath) == NormalizePath(sourcePath))
                                {
                                    nodeToRemove = child;
                                    break;
                                }
                            }

                            if (nodeToRemove != null)
                                childrenCollection.Remove(nodeToRemove);
                        }
                    }
                    catch { }
                }
            }
        }

        private async Task<string> SyncTargetNodeAsync(string targetPath)
        {
            if (string.IsNullOrEmpty(targetPath) || !Directory.Exists(targetPath))
                return null;

            try
            {
                var item = FindItemByPath(treeView.RootNodes, targetPath)
                           ?? FindItemByPath(treeViewSpF.RootNodes, targetPath);
                if (item == null)
                    return null;

                var node = FindNodeByItem(treeView.RootNodes, item);
                TreeView targetTree = null;
                if (node != null)
                {
                    targetTree = treeView;
                }
                else
                {
                    node = FindNodeByItem(treeViewSpF.RootNodes, item);
                    if (node != null)
                        targetTree = treeViewSpF;
                }

                if (node == null || targetTree == null)
                    return null;

                List<ExplorerItemViewModel> actualItems;
                if (targetTree == treeView)
                    actualItems = await _fileSystemService.LoadSubfoldersForTreeViewAsync(targetPath, _history);
                else
                    actualItems = await _fileSystemService.LoadFoldersOnlyAsync(targetPath, _historySpF);

                var actualPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var vm in actualItems)
                    actualPaths.Add(NormalizePath(vm.FilePath));

                for (int i = node.Children.Count - 1; i >= 0; i--)
                {
                    if (node.Children[i].Content is ExplorerItemViewModel childItem &&
                        !actualPaths.Contains(NormalizePath(childItem.FilePath)))
                    {
                        node.Children.RemoveAt(i);
                    }
                }

                string firstAddedPath = null;

                var existingPaths = new HashSet<string>(
                    node.Children
                        .Where(c => c.Content is ExplorerItemViewModel)
                        .Select(c => NormalizePath(((ExplorerItemViewModel)c.Content).FilePath)),
                    StringComparer.OrdinalIgnoreCase);

                foreach (var vm in actualItems.OrderBy(vm => vm.Name, StringComparer.OrdinalIgnoreCase))
                {
                    if (!existingPaths.Contains(NormalizePath(vm.FilePath)))
                    {
                        vm.IsTreeViewNode = true;
                        var newNode = new TreeViewNode
                        {
                            Content = vm,
                            HasUnrealizedChildren = Directory.Exists(vm.FilePath)
                        };

                        int insertIndex = 0;
                        for (int i = 0; i < node.Children.Count; i++)
                        {
                            if (node.Children[i].Content is ExplorerItemViewModel existingItem)
                            {
                                if (string.Compare(vm.Name, existingItem.Name, StringComparison.OrdinalIgnoreCase) < 0)
                                    break;
                                insertIndex = i + 1;
                            }
                            else
                            {
                                insertIndex = i + 1;
                            }
                        }

                        if (insertIndex >= node.Children.Count)
                            node.Children.Add(newNode);
                        else
                            node.Children.Insert(insertIndex, newNode);

                        if (firstAddedPath == null)
                            firstAddedPath = vm.FilePath;
                    }
                }

                node.HasUnrealizedChildren = Directory.Exists(targetPath) &&
                                            (Directory.GetDirectories(targetPath).Length > 0 ||
                                             Directory.GetFiles(targetPath).Length > 0);

                targetTree.UpdateLayout();
                return firstAddedPath;
            }
            catch
            {
                return null;
            }
        }

        private async void OnDriveServiceUpdated(object sender, EventArgs e)
        {
            await DispatcherQueue.EnqueueAsync(() =>
            {
                if (treeView.RootNodes.FirstOrDefault()?.Content is ExplorerItemViewModel rootItem
                    && rootItem.FilePath == "MyComputer")
                {
                    var myComputerNode = treeView.RootNodes[0];
                    myComputerNode.Children.Clear();
                    myComputerNode.HasUnrealizedChildren = true;

                    if (myComputerNode.IsExpanded)
                    {
                        treeView.Expand(myComputerNode);
                    }
                }
            });
        }
        #endregion

        #region --- Снимок: методы сериализации/десериализации дерева ---

        private void SaveTreeSnapshot()
        {
            try
            {
                var list = new List<Dictionary<string, object>>();
                foreach (var root in treeView.RootNodes)
                {
                    if (root.IsExpanded || root.Children.Count > 0)
                        list.Add(SerializeNodeToDict(root, "MainTree"));
                }
                foreach (var root in treeViewSpF.RootNodes)
                {
                    if (root.IsExpanded || root.Children.Count > 0)
                        list.Add(SerializeNodeToDict(root, "SpFTree"));
                }
                App.SettingsManager?.SaveSetting("TreeSnapshot", list);
            }
            catch { }
        }

        private Dictionary<string, object> SerializeNodeToDict(TreeViewNode node, string treeId)
        {
            var snap = new SnapshotNode
            {
                Path = (node.Content as ExplorerItemViewModel)?.FilePath ?? "",
                Name = (node.Content as ExplorerItemViewModel)?.Name ?? "",
                IsExpanded = node.IsExpanded,
                HasUnrealizedChildren = node.HasUnrealizedChildren,
                TreeId = treeId,
                Children = node.Children.Select(child => SnapshotNodeFromTreeViewNode(child, treeId)).ToList()
            };
            return SerializeSnapshotNode(snap);
        }

        private SnapshotNode SnapshotNodeFromTreeViewNode(TreeViewNode node, string treeId)
        {
            var item = node.Content as ExplorerItemViewModel;
            return new SnapshotNode
            {
                Path = item?.FilePath ?? "",
                Name = item?.Name ?? "",
                IsExpanded = node.IsExpanded,
                HasUnrealizedChildren = node.HasUnrealizedChildren,
                TreeId = treeId,
                Children = node.Children.Select(child => SnapshotNodeFromTreeViewNode(child, treeId)).ToList()
            };
        }

        private Dictionary<string, object> SerializeSnapshotNode(SnapshotNode snap)
        {
            var dict = new Dictionary<string, object>
            {
                ["Path"] = snap.Path,
                ["Name"] = snap.Name,
                ["IsExpanded"] = snap.IsExpanded,
                ["HasUnrealizedChildren"] = snap.HasUnrealizedChildren,
                ["TreeId"] = snap.TreeId,
                ["IsDirectory"] = snap.IsDirectory,
                ["Length"] = snap.Length,
                ["Children"] = snap.Children.Select(SerializeSnapshotNode).ToList()
            };
            return dict;
        }

        private SnapshotNode DeserializeSnapshotNode(Dictionary<string, object> dict)
        {
            var snap = new SnapshotNode
            {
                Path = dict["Path"]?.ToString(),
                Name = dict["Name"]?.ToString(),
                IsExpanded = (bool)dict["IsExpanded"],
                HasUnrealizedChildren = (bool)dict["HasUnrealizedChildren"],
                TreeId = dict["TreeId"]?.ToString(),
                IsDirectory = dict.ContainsKey("IsDirectory") && (bool)dict["IsDirectory"],
                Length = dict.ContainsKey("Length") ? Convert.ToInt64(dict["Length"]) : 0
            };
            if (dict.TryGetValue("Children", out var childrenObj) && childrenObj is List<Dictionary<string, object>> childrenList)
            {
                snap.Children = childrenList.Select(DeserializeSnapshotNode).ToList();
            }
            return snap;
        }

        private bool TryLoadTreeSnapshot()
        {
            var savedSnapshot = App.SettingsManager?.GetSetting<List<Dictionary<string, object>>>("TreeSnapshot");
            if (savedSnapshot == null || savedSnapshot.Count == 0)
                return false;

            try
            {
                foreach (var dict in savedSnapshot)
                {
                    var snap = DeserializeSnapshotNode(dict);
                    var tree = snap.TreeId == "SpFTree" ? treeViewSpF : treeView;
                    var history = snap.TreeId == "SpFTree" ? _historySpF : _history;
                    tree.RootNodes.Add(CreateNodeFromSnapshot(snap, history));
                }
                if (!savedSnapshot.Any(d => d.TryGetValue("TreeId", out var id) && id?.ToString() == "SpFTree"))
                {
                    TreeViewSpF_OnLoaded(treeViewSpF, null);
                }
                return true;
            }
            catch
            {
                treeView.RootNodes.Clear();
                treeViewSpF.RootNodes.Clear();
                return false;
            }
        }

        private TreeViewNode CreateNodeFromSnapshot(SnapshotNode snap, IDirectoryHistory history)
        {
            var item = new ExplorerItemViewModel(history)
            {
                FilePath = snap.Path,
                Name = snap.Name,
                IsTreeViewNode = true
            };
            var node = new TreeViewNode
            {
                Content = item,
                IsExpanded = snap.IsExpanded,
                HasUnrealizedChildren = snap.HasUnrealizedChildren
            };
            foreach (var childSnap in snap.Children)
            {
                node.Children.Add(CreateNodeFromSnapshot(childSnap, history));
            }
            return node;
        }

        #endregion
    }
}