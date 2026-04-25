using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Core_FileManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;

namespace ufm
{
    public sealed partial class TreePanelPg01 : Page, IDisposable
    {
        #region Поля и свойства

        public static TreePanelPg01 Instance { get; private set; }

        private string _selectedSize = "Medium";
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

        private HashSet<string> _specialFolderPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public event EventHandler<string> NavigateRequested;

        public bool ExpandedTreeSelectedSetting
        {
            get
            {
                try
                {
                    return App.SettingsManager?.GetSetting<bool>("ExpandedTreeSelected", true) ?? true;
                }
                catch
                {
                    return true;
                }
            }
        }

        public bool ExpanderNodesSFStartsSetting
        {
            get
            {
                try
                {
                    return App.SettingsManager?.GetSetting<bool>("ExpanderNodesSFStarts", true) ?? true;
                }
                catch
                {
                    return true;
                }
            }
        }

        public bool ExpanderNodesMyPcStartsSetting
        {
            get
            {
                try
                {
                    return App.SettingsManager?.GetSetting<bool>("ExpanderNodesMyPcStarts", true) ?? true;
                }
                catch
                {
                    return true;
                }
            }
        }

        public string CurrentTileSize => _selectedSize;

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

            this.Loaded += TreePanelPg01_Loaded;
            this.Unloaded += TreePanelPg01_Unloaded;

            LoadDefaultExpandedState();

            treeView.Loaded += TreeView_OnLoaded;
            treeView.Expanding += TreeView_OnExpanding;
            treeView.Collapsed += TreeView_OnCollapsed;
            treeView.DoubleTapped += TreeView_OnDoubleTapped;
            treeView.SelectionChanged += TreeView_SelectionChanged;
            treeView.Expanding += (s, e) => AddExpandedPath(e.Node);

            treeViewSpF.Loaded += TreeViewSpF_OnLoaded;
            treeViewSpF.Expanding += TreeViewSpF_OnExpanding;
            treeViewSpF.Collapsed += TreeViewSpF_OnCollapsed;
            treeViewSpF.DoubleTapped += TreeViewSpF_OnDoubleTapped;
            treeViewSpF.SelectionChanged += TreeViewSpF_SelectionChanged;
            treeViewSpF.Expanding += (s, e) => AddExpandedPath(e.Node);
        }

        private void LoadDefaultExpandedState()
        {
            try
            {
                var savedPaths = App.SettingsManager?.GetSetting<List<string>>("TreePanelExpandedPaths", new List<string>());
                if (savedPaths != null && savedPaths.Count > 0)
                {
                    _savedExpandedPaths.Clear();
                    foreach (var path in savedPaths)
                        _savedExpandedPaths.Add(path);
                }
                else
                {
                    if (ExpanderNodesMyPcStartsSetting)
                        _savedExpandedPaths.Add("MyComputer");
                }
            }
            catch
            {
                if (ExpanderNodesMyPcStartsSetting)
                    _savedExpandedPaths.Add("MyComputer");
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
                var pathsList = _savedExpandedPaths.ToList();
                App.SettingsManager?.SaveSetting("TreePanelExpandedPaths", pathsList);
            }
            catch
            {
            }
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
                catch
                {
                }
                finally
                {
                    Instance = null;
                    GC.SuppressFinalize(this);
                    _disposed = true;
                }
            }
        }

        #endregion

        #region Сохранение и восстановление состояния дерева

        private void AddExpandedPath(TreeViewNode node)
        {
            if (node.Content is ExplorerItemViewModel item && !string.IsNullOrEmpty(item.FilePath))
            {
                _expandedPaths.Add(item.FilePath);
            }
        }

        private void RestoreExpandedState(TreeView tree, IList<TreeViewNode> nodes, string treeName)
        {
            if (nodes == null) return;

            foreach (var node in nodes)
            {
                if (node.Content is ExplorerItemViewModel item)
                {
                    bool shouldExpand = _expandedPaths.Contains(item.FilePath);
                    if (shouldExpand && !node.IsExpanded)
                    {
                        tree.Expand(node);
                    }
                }
                RestoreExpandedState(tree, node.Children, treeName + "->children");
            }
        }

        #endregion

        #region Обработчики событий страницы

        private void TreePanelPg01_Loaded(object sender, RoutedEventArgs e)
        {
            _expandedPaths.Clear();
            foreach (var path in _savedExpandedPaths)
                _expandedPaths.Add(path);

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
            }

            SetSelectedRadioButton(_selectedSize);
            UpdateAllTiles();

            if (!_isFirstLoad)
            {
                DispatcherQueue.TryEnqueue(async () =>
                {
                    await Task.Delay(300);
                    RestoreExpandedState(treeView, treeView.RootNodes, "treeView");
                    RestoreExpandedState(treeViewSpF, treeViewSpF.RootNodes, "treeViewSpF");
                    if (!string.IsNullOrEmpty(_savedSelectedPath))
                    {
                        UpdateTreeViewSelection(_activePanelId, _savedSelectedPath);
                    }
                    ScheduleUpdateAllTiles();
                });
            }
            else
            {
                _isFirstLoad = false;
            }
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
                    catch
                    {
                    }
                }

                if (!saved)
                {
                    try
                    {
                        var localSettings = ApplicationData.Current.LocalSettings.Values;
                        localSettings["SelectedSizeIconTreeView"] = _selectedSize;
                    }
                    catch
                    {
                    }
                }

                UpdateAllTiles();
            }
        }

        #endregion

        #region Работа с основным TreeView

        private async void TreeView_OnLoaded(object sender, RoutedEventArgs e)
        {
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
                        var firstLevelItems = await _fileSystemService.LoadFoldersOnlyAsync(driveItem.FilePath, _history);
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
                    catch
                    {
                    }
                }
            }
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
                    await LoadDrivesSync(args.Node);
                    UpdateAllTiles();
                }
                else if (args.Node.HasUnrealizedChildren || args.Node.Children.Count == 0)
                {
                    args.Node.HasUnrealizedChildren = false;
                    await LoadSubfoldersAsync(args.Node, treeItem.FilePath);
                    UpdateAllTiles();
                }
            }
            catch
            {
            }
        }

        private void TreeView_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (treeView.SelectedNode == null) return;

            if (!treeView.SelectedNode.IsExpanded)
                treeView.Expand(treeView.SelectedNode);
            else
                treeView.Collapse(treeView.SelectedNode);

            if (treeView.SelectedNode?.Content is ExplorerItemViewModel item)
            {
                OnNavigateRequested(item.FilePath);
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
                {
                    if (child.Content is ExplorerItemViewModel childItem && !string.IsNullOrEmpty(childItem.FilePath))
                        _specialFolderPaths.Add(childItem.FilePath);
                }

                await PreloadFirstLevelForSpFAsync(specialFoldersNode);

                if (ExpanderNodesSFStartsSetting)
                {
                    specialFoldersNode.IsExpanded = true;
                    treeViewSpF.Expand(specialFoldersNode);
                }
                else
                {
                    specialFoldersNode.IsExpanded = false;
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
                        var firstLevelItems = await _fileSystemService.LoadFoldersOnlyAsync(folderItem.FilePath, _historySpF);
                        foreach (var item in firstLevelItems.Take(3))
                        {
                            item.IsTreeViewNode = true;
                            var node = new TreeViewNode
                            {
                                Content = item,
                                HasUnrealizedChildren = true
                            };
                            systemFolderNode.Children.Add(node);
                            UpdateTileSizeSpF(node);
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        private async void TreeViewSpF_OnExpanding(TreeView sender, TreeViewExpandingEventArgs args)
        {
            if (_isLoadingSpF || args.Node.Content is not ExplorerItemViewModel treeItem)
                return;

            try
            {
                _isLoadingSpF = true;

                if (treeItem.FilePath == "SpecialFolders" && args.Node.Children.Count == 0)
                {
                    args.Node.HasUnrealizedChildren = false;
                    await LoadHomeContentsAsync(args.Node);

                    _specialFolderPaths.Clear();
                    foreach (var child in args.Node.Children)
                    {
                        if (child.Content is ExplorerItemViewModel childItem && !string.IsNullOrEmpty(childItem.FilePath))
                            _specialFolderPaths.Add(childItem.FilePath);
                    }
                    UpdateAllTiles();
                }
                else if (Directory.Exists(treeItem.FilePath) && args.Node.Children.Count == 0)
                {
                    args.Node.HasUnrealizedChildren = false;
                    await LoadSubfoldersForSpFAsync(args.Node, treeItem.FilePath);
                    UpdateAllTiles();
                }
            }
            catch
            {
            }
            finally
            {
                _isLoadingSpF = false;
            }
            UpdateAllTiles();
        }

        private async Task LoadHomeContentsAsync(TreeViewNode parentNode)
        {
            try
            {
                parentNode.Children.Clear();
                var homeItems = await _fileSystemService.LoadHomeAsync("TreeViewSpF", _historySpF);

                foreach (var item in homeItems)
                {
                    item.IsTreeViewNode = true;
                    var node = new TreeViewNode
                    {
                        Content = item,
                        HasUnrealizedChildren = Directory.Exists(item.FilePath)
                    };
                    parentNode.Children.Add(node);
                    UpdateTileSizeSpF(node);
                }
            }
            catch
            {
            }
        }

        private void TreeViewSpF_OnCollapsed(TreeView sender, TreeViewCollapsedEventArgs args)
        {
            if (args.Node.HasChildren && args.Node.Children.Count > 0 &&
                args.Node.Content is ExplorerItemViewModel item &&
                item.FilePath != "SpecialFolders")
            {
                args.Node.Children.Clear();
                args.Node.HasUnrealizedChildren = true;
            }
        }

        private void TreeViewSpF_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (treeViewSpF.SelectedNode == null) return;

            if (!treeViewSpF.SelectedNode.IsExpanded)
                treeViewSpF.Expand(treeViewSpF.SelectedNode);
            else
                treeViewSpF.Collapse(treeViewSpF.SelectedNode);

            if (treeViewSpF.SelectedNode?.Content is ExplorerItemViewModel item)
            {
                OnNavigateRequested(item.FilePath);
            }
        }

        #endregion

        #region Методы работы с узлами TreeView

        private async Task LoadDrivesSync(TreeViewNode parentNode)
        {
            try
            {
                var driveItems = await _fileSystemService.LoadDrivesTree(_history);
                foreach (var driveItem in driveItems)
                {
                    driveItem.IsTreeViewNode = true;
                    var driveNode = new TreeViewNode
                    {
                        Content = driveItem,
                        HasUnrealizedChildren = true
                    };
                    parentNode.Children.Add(driveNode);
                }
            }
            catch
            {
            }
        }

        private async Task LoadSubfoldersAsync(TreeViewNode parentNode, string folderPath)
        {
            try
            {
                var folderItems = await _fileSystemService.LoadSubfoldersForTreeViewAsync(folderPath, _history);
                foreach (var item in folderItems)
                {
                    item.IsTreeViewNode = true;
                    var node = new TreeViewNode
                    {
                        Content = item,
                        HasUnrealizedChildren = true
                    };
                    parentNode.Children.Add(node);
                    UpdateTileSize(node);
                }
            }
            catch
            {
            }
        }

        private async Task LoadSubfoldersForSpFAsync(TreeViewNode parentNode, string folderPath)
        {
            try
            {
                var folderItems = await _fileSystemService.LoadFoldersOnlyAsync(folderPath, _historySpF);
                foreach (var item in folderItems)
                {
                    item.IsTreeViewNode = true;
                    var node = new TreeViewNode
                    {
                        Content = item,
                        HasUnrealizedChildren = true
                    };
                    parentNode.Children.Add(node);
                    UpdateTileSizeSpF(node);
                }
            }
            catch
            {
            }
        }

        private bool IsItemInTreeView(IList<TreeViewNode> nodes, ExplorerItemViewModel targetItem)
        {
            if (nodes == null) return false;
            foreach (var node in nodes)
            {
                if (node?.Content == targetItem)
                    return true;
                if (node?.Children?.Count > 0 && IsItemInTreeView(node.Children, targetItem))
                    return true;
            }
            return false;
        }

        #endregion

        #region Обновление отображения элементов

        private TreeViewNode FindNodeByItem(IList<TreeViewNode> nodes, ExplorerItemViewModel targetItem)
        {
            if (nodes == null) return null;
            foreach (var node in nodes)
            {
                if (node.Content == targetItem)
                    return node;
                if (node.Children.Count > 0)
                {
                    var found = FindNodeByItem(node.Children, targetItem);
                    if (found != null)
                        return found;
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
            catch
            {
            }
        }

        private async void UpdateTreeViewSelection(string panelId, string path)
        {
            try
            {
                TreeView targetTreeView = panelId == "MainTree" ? treeView : treeViewSpF;
                if (targetTreeView == null || string.IsNullOrEmpty(path))
                {
                    return;
                }

                _isNavigationChangingSelection = true;

                if (!IsVirtualPath(path))
                {
                    foreach (var rootNode in targetTreeView.RootNodes)
                    {
                        if (rootNode?.Content is ExplorerItemViewModel rootItem)
                        {
                            if (rootItem.FilePath == "MyComputer")
                            {
                                await EnsureMyComputerExpanded(rootNode);
                            }
                            else if (rootItem.FilePath == "SpecialFolders")
                            {
                                await EnsureSpecialFoldersExpanded(rootNode);
                            }
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
                            if (node.HasUnrealizedChildren || node.Children.Count == 0)
                            {
                                await ExpandNode(node, targetItem.FilePath, panelId);
                            }
                            if (!node.IsExpanded)
                            {
                                targetTreeView.Expand(node);
                            }
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
            catch
            {
            }
            finally
            {
                _isNavigationChangingSelection = false;
                UpdateNavigationButtons();
                ScheduleUpdateAllTiles();
            }
        }

        private async Task<bool> ExpandAndSelectPath(IList<TreeViewNode> nodes, string[] pathSegments, int currentIndex, string panelId)
        {
            if (currentIndex >= pathSegments.Length)
                return false;

            string currentPath = pathSegments[currentIndex];
            bool isLastSegment = currentIndex == pathSegments.Length - 1;

            foreach (var node in nodes)
            {
                if (node?.Content is not ExplorerItemViewModel item)
                    continue;

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

                        if (node.HasUnrealizedChildren || node.Children.Count == 0)
                        {
                            await ExpandNode(node, item.FilePath, panelId);
                        }

                        if (!node.IsExpanded)
                        {
                            targetTreeView.Expand(node);
                        }

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

                        if (node.Children.Count == 0)
                            await Task.Delay(10);

                        bool found = await ExpandAndSelectPath(node.Children, pathSegments, currentIndex + 1, panelId);
                        if (found)
                            return true;
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

        private async Task EnsureMyComputerExpanded(TreeViewNode myComputerNode)
        {
            if (myComputerNode?.Content is ExplorerItemViewModel item && item.FilePath == "MyComputer")
            {
                if (!myComputerNode.IsExpanded)
                {
                    treeView.Expand(myComputerNode);
                }
                if (myComputerNode.Children.Count == 0)
                {
                    await LoadDrivesSync(myComputerNode);
                }
            }
        }

        private async Task EnsureSpecialFoldersExpanded(TreeViewNode specialFoldersNode)
        {
            if (specialFoldersNode?.Content is ExplorerItemViewModel item && item.FilePath == "SpecialFolders")
            {
                if (!specialFoldersNode.IsExpanded)
                {
                    treeViewSpF.Expand(specialFoldersNode);
                }
                if (specialFoldersNode.Children.Count == 0)
                {
                    await LoadHomeContentsAsync(specialFoldersNode);
                    _specialFolderPaths.Clear();
                    foreach (var child in specialFoldersNode.Children)
                    {
                        if (child.Content is ExplorerItemViewModel childItem && !string.IsNullOrEmpty(childItem.FilePath))
                            _specialFolderPaths.Add(childItem.FilePath);
                    }
                }
            }
        }

        private async Task ExpandNode(TreeViewNode node, string path, string panelId)
        {
            try
            {
                if (panelId == "MainTree")
                {
                    if (node.HasUnrealizedChildren || node.Children.Count == 0)
                    {
                        node.HasUnrealizedChildren = false;
                        await LoadSubfoldersAsync(node, path);
                    }
                }
                else
                {
                    if (node.HasUnrealizedChildren || node.Children.Count == 0)
                    {
                        node.HasUnrealizedChildren = false;
                        await LoadSubfoldersForSpFAsync(node, path);
                    }
                }

                var targetTreeView = panelId == "MainTree" ? treeView : treeViewSpF;
                if (!node.IsExpanded)
                    targetTreeView.Expand(node);

                await Task.Delay(20);
            }
            catch
            {
            }
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

                string root = Path.GetPathRoot(normalized);
                string[] parts = normalized.Substring(root.Length)
                    .Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

                string currentPath = root.TrimEnd(Path.DirectorySeparatorChar);
                if (!string.IsNullOrEmpty(root))
                {
                    string rootPath = root.TrimEnd(Path.DirectorySeparatorChar);
                    if (rootPath.Length == 2 && rootPath[1] == ':')
                        rootPath += Path.DirectorySeparatorChar;
                    result.Add(rootPath);
                    currentPath = rootPath;
                }

                foreach (var part in parts)
                {
                    currentPath = Path.Combine(currentPath, part);
                    result.Add(currentPath);
                }

                return result.ToArray();
            }
            catch
            {
                return new[] { path };
            }
        }

        private void UpdateAllTiles()
        {
            if (treeView?.RootNodes?.Count > 0)
            {
                foreach (var node in treeView.RootNodes)
                    UpdateTileSize(node);
            }

            if (treeViewSpF?.RootNodes?.Count > 0)
            {
                foreach (var node in treeViewSpF.RootNodes)
                    UpdateTileSizeSpF(node);
            }
        }

        private void UpdateTileSize(TreeViewNode node)
        {
            if (node == null) return;

            var container = treeView?.ContainerFromNode(node) as TreeViewItem;
            var tile = container?.ContentTemplateRoot as BaseTileControl;

            if (tile != null)
                tile.UpdateSize(_selectedSize);

            if (node.IsExpanded)
            {
                foreach (var childNode in node.Children)
                    UpdateTileSize(childNode);
            }
        }

        private void UpdateTileSizeSpF(TreeViewNode node)
        {
            if (node == null) return;

            var container = treeViewSpF?.ContainerFromNode(node) as TreeViewItem;
            var tile = container?.ContentTemplateRoot as BaseTileControl;

            if (tile != null)
                tile.UpdateSize(_selectedSize);

            if (node.IsExpanded)
            {
                foreach (var childNode in node.Children)
                    UpdateTileSizeSpF(childNode);
            }
        }

        private void ScheduleUpdateAllTiles()
        {
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                UpdateAllTiles();
            });
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
                    if (NormalizePath(item.FilePath) == NormalizePath(path))
                        return item;
                }
                if (node?.Children?.Count > 0)
                {
                    var found = FindItemByPath(node.Children, path);
                    if (found != null)
                        return found;
                }
            }
            return null;
        }

        private bool IsVirtualPath(string path) => path == "MyComputer" || path == "SpecialFolders" || path == ".." || path == "Drives";

        private string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path) || IsVirtualPath(path))
                return path;

            try
            {
                if (path.Length == 2 && path[1] == ':')
                    return path + Path.DirectorySeparatorChar;
                if (path.Length == 3 && path[1] == ':' && path[2] == '\\')
                    return path;

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
                }
                catch
                {
                }
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
                _navigationManager.NavigateTo(selectedItem.FilePath, "SpFTree");
                UpdateNavigationButtons();

                if (!IsVirtualPath(selectedItem.FilePath))
                {
                    OnNavigateRequested(selectedItem.FilePath);
                }
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
                _navigationManager.NavigateTo(selectedItem.FilePath, "MainTree");
                UpdateNavigationButtons();

                if (!IsVirtualPath(selectedItem.FilePath))
                {
                    OnNavigateRequested(selectedItem.FilePath);
                }
            }
        }

        #endregion

        #region Навигационные кнопки

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            string activePanel = GetActivePanel();
            if (_navigationManager.CanGoBack(activePanel))
                _navigationManager.GoBack(activePanel);
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            string activePanel = GetActivePanel();
            if (_navigationManager.CanGoForward(activePanel))
                _navigationManager.GoForward(activePanel);
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
            catch
            {
            }
        }

        private string GetActivePanel() => _activePanelId ?? "MainTree";

        private DirectoryHistory GetActiveHistory() => GetActivePanel() == "SpFTree" ? _historySpF : _history;

        #endregion

        #region Публичные методы для внешнего взаимодействия

        private void OnNavigateRequested(string path)
        {
            NavigateRequested?.Invoke(this, path);
        }

        public void SelectPath(string path)
        {
            string panelId;

            if (_specialFolderPaths.Contains(path) || path == "SpecialFolders" || path.StartsWith("SpecialFolders"))
            {
                panelId = "SpFTree";
            }
            else
            {
                panelId = "MainTree";
            }

            UpdateTreeViewSelection(panelId, path);
            ScheduleUpdateAllTiles();
        }

        #endregion
    }
}