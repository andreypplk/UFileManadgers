//РАБОЧИЙ ВАРИАНТ НО ТРЕБУЕТСЯ ПРЕДВАРИТЕЛЬНОЕ ВЫДЕЛЕНИЕ ВКЛАДКИ
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Threading.Tasks;
//using Microsoft.UI.Xaml;
//using Microsoft.UI.Xaml.Controls;
//using Windows.ApplicationModel.DataTransfer;
//using ufm.Pages;

//namespace ufm
//{
//    public class TabViewManager
//    {
//        private TabView _tabsView;
//        private Frame _contentFrame;
//        private const string TabIdKey = "TabId";
//        private readonly Dictionary<TabViewItem, UIElement> _tabContentMap = new();
//        private readonly bool _skipInitialTab;
//        private string _draggedTabId;

//        public TabViewManager(TabView tabsView, Frame contentFrame)
//            : this(tabsView, contentFrame, false)
//        {
//        }

//        public TabViewManager(TabView tabsView, Frame contentFrame, bool skipInitialTab)
//        {
//            Debug.WriteLine($"[TabViewManager] Constructor START: tabsView={tabsView != null}, contentFrame={contentFrame != null}, skipInitialTab={skipInitialTab}");
//            _tabsView = tabsView;
//            _contentFrame = contentFrame;
//            _skipInitialTab = skipInitialTab;
//            Initialize();
//            Debug.WriteLine($"[TabViewManager] Constructor END");
//        }

//        private void Initialize()
//        {
//            Debug.WriteLine($"[TabViewManager] Initialize START");
//            _tabsView.Loaded += TabsView_Loaded;
//            _tabsView.SelectionChanged += TabsView_SelectionChanged;
//            _tabsView.AddTabButtonClick += TabsView_OnAddTabButtonClick;
//            _tabsView.TabCloseRequested += TabsView_OnTabCloseRequested;
//            _tabsView.TabDragStarting += TabsView_TabDragStarting;
//            _tabsView.TabDroppedOutside += TabsView_TabDroppedOutside;
//            _tabsView.TabStripDragOver += TabsView_TabStripDragOver;
//            _tabsView.TabStripDrop += TabsView_TabStripDrop;
//            Debug.WriteLine($"[TabViewManager] Initialize END, events subscribed");
//        }

//        private void TabsView_Loaded(object sender, RoutedEventArgs e)
//        {
//            Debug.WriteLine($"[TabViewManager] TabsView_Loaded START, _skipInitialTab={_skipInitialTab}, TabItems.Count={_tabsView.TabItems.Count}");

//            if (_skipInitialTab)
//            {
//                Debug.WriteLine($"[TabViewManager] TabsView_Loaded: skipping initial tab creation (drag&drop window)");
//                return;
//            }

//            if (_tabsView.TabItems.Count == 0)
//            {
//                var initialTab = CreateNewTab("Root Page");
//                _tabsView.TabItems.Add(initialTab);
//                _tabsView.SelectedIndex = 0;
//                Debug.WriteLine($"[TabViewManager] TabsView_Loaded: initialTab added, TabItems.Count={_tabsView.TabItems.Count}");
//            }
//            else
//            {
//                Debug.WriteLine($"[TabViewManager] TabsView_Loaded: TabItems already has {_tabsView.TabItems.Count} tabs, skipping creation");
//            }
//            Debug.WriteLine($"[TabViewManager] TabsView_Loaded END");
//        }

//        public TabViewItem CreateNewTab(string header)
//        {
//            Debug.WriteLine($"[TabViewManager] CreateNewTab START: header='{header}'");
//            var newTab = new TabViewItem
//            {
//                Header = header,
//                IconSource = new SymbolIconSource { Symbol = Symbol.Placeholder },
//                Tag = Guid.NewGuid().ToString()
//            };
//            Debug.WriteLine($"[TabViewManager] CreateNewTab END: newTab created, Header='{newTab.Header}', Tag='{newTab.Tag}'");
//            return newTab;
//        }

//        private UIElement CreateTabContent(string dataContext = null)
//        {
//            Debug.WriteLine($"[TabViewManager] CreateTabContent START: dataContext='{dataContext}'");
//            var frame = new Frame();
//            frame.Navigate(typeof(rootPage), dataContext);
//            Debug.WriteLine($"[TabViewManager] CreateTabContent END: Frame created, Content={frame.Content?.GetType().Name}");
//            return frame;
//        }

//        private void TabsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged START");
//            var selectedTab = _tabsView.SelectedItem as TabViewItem;
//            Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: selectedTab={selectedTab != null}, Header='{selectedTab?.Header}'");

//            if (selectedTab != null)
//            {
//                Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: checking _tabContentMap.ContainsKey={_tabContentMap.ContainsKey(selectedTab)}");
//                if (!_tabContentMap.ContainsKey(selectedTab))
//                {
//                    Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: creating new content for tab");
//                    var content = CreateTabContent();
//                    _tabContentMap[selectedTab] = content;
//                    Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: content added to map, _tabContentMap.Count={_tabContentMap.Count}");
//                }

//                _contentFrame.Content = _tabContentMap[selectedTab];
//                Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: _contentFrame.Content set to tab content");
//            }
//            else
//            {
//                _contentFrame.Content = null;
//                Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: selectedTab is null, _contentFrame.Content set to null");
//            }
//            Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged END");
//        }

//        public void TabsView_OnAddTabButtonClick(TabView sender, object args)
//        {
//            Debug.WriteLine($"[TabViewManager] TabsView_OnAddTabButtonClick START: sender.TabItems.Count={sender.TabItems.Count}");
//            var newTab = CreateNewTab("New Tab");
//            _tabsView.TabItems.Add(newTab);
//            _tabsView.SelectedItem = newTab;
//            Debug.WriteLine($"[TabViewManager] TabsView_OnAddTabButtonClick END: TabItems.Count={_tabsView.TabItems.Count}, SelectedIndex={_tabsView.SelectedIndex}");
//        }

//        public void TabsView_OnTabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
//        {
//            Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested START: Tab.Header='{args.Tab.Header}', sender.TabItems.Count={sender.TabItems.Count}");

//            if (_tabContentMap.ContainsKey(args.Tab))
//            {
//                Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested: removing content from map");
//                _tabContentMap.Remove(args.Tab);
//                Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested: _tabContentMap.Count={_tabContentMap.Count}");
//            }

//            sender.TabItems.Remove(args.Tab);
//            Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested: after removal, TabItems.Count={sender.TabItems.Count}");

//            if (sender.TabItems.Count == 0)
//            {
//                _contentFrame.Content = null;
//                Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested: no tabs left, _contentFrame.Content set to null");
//            }
//            Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested END");
//        }

//        // -----------------------------------------------------------------
//        // Обработчики drag & drop (используют SelectedItem)
//        // -----------------------------------------------------------------
//        private void TabsView_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
//        {
//            Debug.WriteLine($"[TabViewManager] TabsView_TabDragStarting START");

//            // Используем текущую выбранную вкладку – это надёжно, т.к. пользователь обычно кликает на вкладку перед перетаскиванием
//            var draggedTab = _tabsView.SelectedItem as TabViewItem;
//            if (draggedTab == null)
//            {
//                Debug.WriteLine("[Drag] No selected tab, aborting drag.");
//                args.Cancel = true;
//                return;
//            }

//            string tabId = draggedTab.Tag?.ToString();
//            if (!string.IsNullOrEmpty(tabId))
//            {
//                _draggedTabId = tabId;
//                args.Data.Properties.Add(TabIdKey, tabId);
//                Debug.WriteLine($"[Drag] Using SelectedItem: Header='{draggedTab.Header}', TabId={tabId}");
//            }
//            else
//            {
//                Debug.WriteLine("[Drag] WARNING: SelectedItem Tag is null, cannot identify tab uniquely!");
//                args.Cancel = true;
//                return;
//            }
//            args.Data.RequestedOperation = DataPackageOperation.Move;
//            Debug.WriteLine($"[TabViewManager] TabsView_TabDragStarting END");
//        }

//        private async void TabsView_TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
//        {
//            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside START");

//            if (string.IsNullOrEmpty(_draggedTabId))
//            {
//                Debug.WriteLine($"[TabViewManager] _draggedTabId is empty, aborting");
//                return;
//            }

//            string draggedTabId = _draggedTabId;
//            _draggedTabId = null;
//            Debug.WriteLine($"[TabViewManager] Retrieved draggedTabId = {draggedTabId}");

//            TabViewItem sourceTab = null;
//            foreach (TabViewItem tab in sender.TabItems)
//            {
//                if (tab.Tag?.ToString() == draggedTabId)
//                {
//                    sourceTab = tab;
//                    break;
//                }
//            }

//            if (sourceTab == null)
//            {
//                Debug.WriteLine($"[TabViewManager] sourceTab not found by Tag, aborting");
//                return;
//            }

//            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: source Tab.Header='{sourceTab.Header}'");
//            var sourceContent = _tabContentMap.ContainsKey(sourceTab) ? _tabContentMap[sourceTab] : null;
//            int selectedIndex = _tabsView.SelectedIndex;
//            var savedHeader = sourceTab.Header;
//            var savedIconSource = sourceTab.IconSource;

//            if (_tabContentMap.ContainsKey(sourceTab))
//                _tabContentMap.Remove(sourceTab);
//            sender.TabItems.Remove(sourceTab);

//            if (sender.TabItems.Count > 0)
//            {
//                int newIndex = selectedIndex < sender.TabItems.Count ? selectedIndex : sender.TabItems.Count - 1;
//                if (newIndex >= 0)
//                    _tabsView.SelectedIndex = newIndex;
//            }

//            var newWindow = new MainWindow(true);
//            newWindow.ExtendsContentIntoTitleBar = true;

//            var newTab = newWindow.TabViewManager.CreateNewTab(savedHeader?.ToString() ?? "Tab");
//            newTab.IconSource = savedIconSource;

//            if (sourceContent != null)
//                newWindow.TabViewManager.TransferContentWithUIElement(sourceContent, newTab);
//            else
//            {
//                var frame = new Frame();
//                frame.Navigate(typeof(rootPage), null);
//                newWindow.TabViewManager.TransferContentWithUIElement(frame, newTab);
//            }

//            newWindow.MainTabsView.TabItems.Add(newTab);
//            newWindow.TabViewManager._tabsView.SelectedItem = newTab;

//            await Task.Delay(50);
//            newWindow.Activate();

//            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside END");
//        }

//        public void TransferContentWithUIElement(UIElement content, TabViewItem targetTab)
//        {
//            Debug.WriteLine($"[TabViewManager] TransferContentWithUIElement START: content={content != null}, targetTab.Header='{targetTab.Header}'");
//            _tabContentMap[targetTab] = content;
//            Debug.WriteLine($"[TabViewManager] TransferContentWithUIElement END: _tabContentMap.Count={_tabContentMap.Count}");
//        }

//        public int GetTabContentMapCount() => _tabContentMap.Count;

//        private void TabsView_TabStripDragOver(object sender, DragEventArgs e)
//        {
//            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDragOver START");
//            bool hasKey = e.DataView.Properties.ContainsKey(TabIdKey);
//            if (hasKey)
//                e.AcceptedOperation = DataPackageOperation.Move;
//            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDragOver END, Accepted={hasKey}");
//        }

//        private async void TabsView_TabStripDrop(object sender, DragEventArgs e)
//        {
//            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop START");

//            if (!e.DataView.Properties.TryGetValue(TabIdKey, out object idObj))
//            {
//                Debug.WriteLine($"[TabViewManager] TabId not found in DataPackage, exiting");
//                return;
//            }
//            string draggedTabId = idObj.ToString();
//            Debug.WriteLine($"[TabViewManager] TabId={draggedTabId}");

//            var destinationTabView = sender as TabView;
//            if (destinationTabView == null) return;

//            int insertIndex = -1;
//            for (int i = 0; i < destinationTabView.TabItems.Count; i++)
//            {
//                var item = destinationTabView.ContainerFromIndex(i) as TabViewItem;
//                if (item != null && e.GetPosition(item).X - item.ActualWidth < 0)
//                {
//                    insertIndex = i;
//                    break;
//                }
//            }

//            TabViewItem sourceTab = null;
//            foreach (TabViewItem tab in destinationTabView.TabItems)
//            {
//                if (tab.Tag?.ToString() == draggedTabId)
//                {
//                    sourceTab = tab;
//                    break;
//                }
//            }

//            if (sourceTab == null)
//            {
//                Debug.WriteLine($"[TabViewManager] sourceTab not found in current TabView, ignoring drop");
//                return;
//            }

//            var header = sourceTab.Header;
//            var iconSource = sourceTab.IconSource;
//            var content = _tabContentMap.ContainsKey(sourceTab) ? _tabContentMap[sourceTab] : null;

//            destinationTabView.TabItems.Remove(sourceTab);
//            _tabContentMap.Remove(sourceTab);

//            var newTab = new TabViewItem
//            {
//                Header = header,
//                IconSource = iconSource,
//                Tag = Guid.NewGuid().ToString()
//            };

//            if (insertIndex < 0)
//                destinationTabView.TabItems.Add(newTab);
//            else
//                destinationTabView.TabItems.Insert(insertIndex, newTab);

//            if (content != null)
//                _tabContentMap[newTab] = content;

//            destinationTabView.SelectedItem = newTab;
//            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop END");
//        }

//        public void TransferContent(TabViewItem sourceTab, TabViewItem targetTab)
//        {
//            Debug.WriteLine($"[TabViewManager] TransferContent START: sourceTab.Header='{sourceTab?.Header}', targetTab.Header='{targetTab?.Header}'");
//            if (_tabContentMap.TryGetValue(sourceTab, out var content))
//            {
//                _tabContentMap[targetTab] = content;
//                _tabContentMap.Remove(sourceTab);
//                Debug.WriteLine($"[TabViewManager] TransferContent: content transferred");
//            }
//            else
//            {
//                Debug.WriteLine($"[TabViewManager] TransferContent: content NOT found for sourceTab");
//            }
//            Debug.WriteLine($"[TabViewManager] TransferContent END");
//        }
//    }
//}

//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Threading.Tasks;
//using Microsoft.UI.Xaml;
//using Microsoft.UI.Xaml.Controls;
//using Microsoft.UI.Xaml.Input;
//using Windows.ApplicationModel.DataTransfer;
//using ufm.Pages;

//namespace ufm
//{
//    public class TabViewManager
//    {
//        private TabView _tabsView;
//        private Frame _contentFrame;
//        private const string TabIdKey = "TabId";
//        private readonly Dictionary<TabViewItem, UIElement> _tabContentMap = new();
//        private readonly bool _skipInitialTab;
//        private string _draggedTabId;

//        // Храним последнюю вкладку, на которой было нажатие указателя
//        private TabViewItem _lastPressedTab;

//        public TabViewManager(TabView tabsView, Frame contentFrame)
//            : this(tabsView, contentFrame, false)
//        {
//        }

//        public TabViewManager(TabView tabsView, Frame contentFrame, bool skipInitialTab)
//        {
//            Debug.WriteLine($"[TabViewManager] Constructor START: tabsView={tabsView != null}, contentFrame={contentFrame != null}, skipInitialTab={skipInitialTab}");
//            _tabsView = tabsView;
//            _contentFrame = contentFrame;
//            _skipInitialTab = skipInitialTab;
//            Initialize();
//            Debug.WriteLine($"[TabViewManager] Constructor END");
//        }

//        private void Initialize()
//        {
//            Debug.WriteLine($"[TabViewManager] Initialize START");

//            _tabsView.Loaded += TabsView_Loaded;
//            _tabsView.SelectionChanged += TabsView_SelectionChanged;
//            _tabsView.AddTabButtonClick += TabsView_OnAddTabButtonClick;
//            _tabsView.TabCloseRequested += TabsView_OnTabCloseRequested;
//            _tabsView.TabDragStarting += TabsView_TabDragStarting;
//            _tabsView.TabDroppedOutside += TabsView_TabDroppedOutside;
//            _tabsView.TabStripDragOver += TabsView_TabStripDragOver;
//            _tabsView.TabStripDrop += TabsView_TabStripDrop;

//            Debug.WriteLine($"[TabViewManager] Initialize END, events subscribed");
//        }

//        private void TabsView_Loaded(object sender, RoutedEventArgs e)
//        {
//            Debug.WriteLine($"[TabViewManager] TabsView_Loaded START, _skipInitialTab={_skipInitialTab}, TabItems.Count={_tabsView.TabItems.Count}");

//            // Подписываемся на PointerPressed для всех уже существующих вкладок
//            foreach (var item in _tabsView.TabItems)
//            {
//                var tab = item as TabViewItem;
//                if (tab != null && !tab.IsInTabStripPointerPressedSubscribed())
//                {
//                    tab.PointerPressed += TabViewItem_PointerPressed;
//                    tab.SetIsInTabStripPointerPressedSubscribed(true);
//                }
//            }

//            if (_skipInitialTab)
//            {
//                Debug.WriteLine($"[TabViewManager] TabsView_Loaded: skipping initial tab creation (drag&drop window)");
//                return;
//            }

//            if (_tabsView.TabItems.Count == 0)
//            {
//                var initialTab = CreateNewTab("Root Page");
//                _tabsView.TabItems.Add(initialTab);
//                _tabsView.SelectedIndex = 0;
//                Debug.WriteLine($"[TabViewManager] TabsView_Loaded: initialTab added, TabItems.Count={_tabsView.TabItems.Count}");
//            }
//            else
//            {
//                Debug.WriteLine($"[TabViewManager] TabsView_Loaded: TabItems already has {_tabsView.TabItems.Count} tabs, skipping creation");
//            }
//            Debug.WriteLine($"[TabViewManager] TabsView_Loaded END");
//        }

//        private void TabViewItem_PointerPressed(object sender, PointerRoutedEventArgs e)
//        {
//            var tab = sender as TabViewItem;
//            if (tab != null)
//            {
//                _lastPressedTab = tab;
//                Debug.WriteLine($"[PointerPressed] Last pressed tab: '{tab.Header}'");
//            }
//        }

//        public TabViewItem CreateNewTab(string header)
//        {
//            Debug.WriteLine($"[TabViewManager] CreateNewTab START: header='{header}'");
//            var newTab = new TabViewItem
//            {
//                Header = header,
//                IconSource = new SymbolIconSource { Symbol = Symbol.Placeholder },
//                Tag = Guid.NewGuid().ToString()
//            };
//            // Подписываемся на PointerPressed сразу при создании
//            newTab.PointerPressed += TabViewItem_PointerPressed;
//            newTab.SetIsInTabStripPointerPressedSubscribed(true);
//            Debug.WriteLine($"[TabViewManager] CreateNewTab END: newTab created, Header='{newTab.Header}', Tag='{newTab.Tag}'");
//            return newTab;
//        }

//        private UIElement CreateTabContent(string dataContext = null)
//        {
//            Debug.WriteLine($"[TabViewManager] CreateTabContent START: dataContext='{dataContext}'");
//            var frame = new Frame();
//            frame.Navigate(typeof(rootPage), dataContext);
//            Debug.WriteLine($"[TabViewManager] CreateTabContent END: Frame created, Content={frame.Content?.GetType().Name}");
//            return frame;
//        }

//        private void TabsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged START");
//            var selectedTab = _tabsView.SelectedItem as TabViewItem;
//            Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: selectedTab={selectedTab != null}, Header='{selectedTab?.Header}'");

//            if (selectedTab != null)
//            {
//                Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: checking _tabContentMap.ContainsKey={_tabContentMap.ContainsKey(selectedTab)}");
//                if (!_tabContentMap.ContainsKey(selectedTab))
//                {
//                    Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: creating new content for tab");
//                    var content = CreateTabContent();
//                    _tabContentMap[selectedTab] = content;
//                    Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: content added to map, _tabContentMap.Count={_tabContentMap.Count}");
//                }

//                _contentFrame.Content = _tabContentMap[selectedTab];
//                Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: _contentFrame.Content set to tab content");
//            }
//            else
//            {
//                _contentFrame.Content = null;
//                Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: selectedTab is null, _contentFrame.Content set to null");
//            }
//            Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged END");
//        }

//        public void TabsView_OnAddTabButtonClick(TabView sender, object args)
//        {
//            Debug.WriteLine($"[TabViewManager] TabsView_OnAddTabButtonClick START: sender.TabItems.Count={sender.TabItems.Count}");
//            var newTab = CreateNewTab("New Tab");
//            _tabsView.TabItems.Add(newTab);
//            _tabsView.SelectedItem = newTab;
//            Debug.WriteLine($"[TabViewManager] TabsView_OnAddTabButtonClick END: TabItems.Count={_tabsView.TabItems.Count}, SelectedIndex={_tabsView.SelectedIndex}");
//        }

//        public void TabsView_OnTabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
//        {
//            Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested START: Tab.Header='{args.Tab.Header}', sender.TabItems.Count={sender.TabItems.Count}");

//            if (_tabContentMap.ContainsKey(args.Tab))
//            {
//                Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested: removing content from map");
//                _tabContentMap.Remove(args.Tab);
//                Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested: _tabContentMap.Count={_tabContentMap.Count}");
//            }

//            sender.TabItems.Remove(args.Tab);
//            Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested: after removal, TabItems.Count={sender.TabItems.Count}");

//            if (sender.TabItems.Count == 0)
//            {
//                _contentFrame.Content = null;
//                Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested: no tabs left, _contentFrame.Content set to null");
//            }
//            Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested END");
//        }

//        // -----------------------------------------------------------------
//        // Drag & Drop с использованием _lastPressedTab
//        // -----------------------------------------------------------------
//        private void TabsView_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
//        {
//            Debug.WriteLine($"[TabViewManager] TabsView_TabDragStarting START");

//            // Используем последнюю нажатую вкладку, если она ещё существует в коллекции
//            var draggedTab = _lastPressedTab;
//            if (draggedTab == null || !sender.TabItems.Contains(draggedTab))
//            {
//                // Fallback на SelectedItem
//                draggedTab = _tabsView.SelectedItem as TabViewItem;
//                Debug.WriteLine("[Drag] Using SelectedItem as fallback");
//            }

//            if (draggedTab == null)
//            {
//                Debug.WriteLine("[Drag] No tab to drag, aborting.");
//                args.Cancel = true;
//                return;
//            }

//            string tabId = draggedTab.Tag?.ToString();
//            if (!string.IsNullOrEmpty(tabId))
//            {
//                _draggedTabId = tabId;
//                args.Data.Properties.Add(TabIdKey, tabId);
//                Debug.WriteLine($"[Drag] Dragging tab: Header='{draggedTab.Header}', TabId={tabId}");
//            }
//            else
//            {
//                Debug.WriteLine("[Drag] Tab.Tag is null, aborting drag.");
//                args.Cancel = true;
//                return;
//            }
//            args.Data.RequestedOperation = DataPackageOperation.Move;
//            Debug.WriteLine($"[TabViewManager] TabsView_TabDragStarting END");
//        }

//        private async void TabsView_TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
//        {
//            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside START");

//            if (string.IsNullOrEmpty(_draggedTabId))
//            {
//                Debug.WriteLine($"[TabViewManager] _draggedTabId is empty, aborting");
//                return;
//            }

//            string draggedTabId = _draggedTabId;
//            _draggedTabId = null;
//            Debug.WriteLine($"[TabViewManager] Retrieved draggedTabId = {draggedTabId}");

//            TabViewItem sourceTab = null;
//            foreach (TabViewItem tab in sender.TabItems)
//            {
//                if (tab.Tag?.ToString() == draggedTabId)
//                {
//                    sourceTab = tab;
//                    break;
//                }
//            }

//            if (sourceTab == null)
//            {
//                Debug.WriteLine($"[TabViewManager] sourceTab not found by Tag, aborting");
//                return;
//            }

//            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: source Tab.Header='{sourceTab.Header}'");
//            var sourceContent = _tabContentMap.ContainsKey(sourceTab) ? _tabContentMap[sourceTab] : null;
//            int selectedIndex = _tabsView.SelectedIndex;
//            var savedHeader = sourceTab.Header;
//            var savedIconSource = sourceTab.IconSource;

//            if (_tabContentMap.ContainsKey(sourceTab))
//                _tabContentMap.Remove(sourceTab);
//            sender.TabItems.Remove(sourceTab);

//            if (sender.TabItems.Count > 0)
//            {
//                int newIndex = selectedIndex < sender.TabItems.Count ? selectedIndex : sender.TabItems.Count - 1;
//                if (newIndex >= 0)
//                    _tabsView.SelectedIndex = newIndex;
//            }

//            var newWindow = new MainWindow(true);
//            newWindow.ExtendsContentIntoTitleBar = true;

//            var newTab = newWindow.TabViewManager.CreateNewTab(savedHeader?.ToString() ?? "Tab");
//            newTab.IconSource = savedIconSource;

//            if (sourceContent != null)
//                newWindow.TabViewManager.TransferContentWithUIElement(sourceContent, newTab);
//            else
//            {
//                var frame = new Frame();
//                frame.Navigate(typeof(rootPage), null);
//                newWindow.TabViewManager.TransferContentWithUIElement(frame, newTab);
//            }

//            newWindow.MainTabsView.TabItems.Add(newTab);
//            newWindow.TabViewManager._tabsView.SelectedItem = newTab;

//            await Task.Delay(50);
//            newWindow.Activate();

//            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside END");
//        }

//        public void TransferContentWithUIElement(UIElement content, TabViewItem targetTab)
//        {
//            Debug.WriteLine($"[TabViewManager] TransferContentWithUIElement START: content={content != null}, targetTab.Header='{targetTab.Header}'");
//            _tabContentMap[targetTab] = content;
//            Debug.WriteLine($"[TabViewManager] TransferContentWithUIElement END: _tabContentMap.Count={_tabContentMap.Count}");
//        }

//        public int GetTabContentMapCount() => _tabContentMap.Count;

//        private void TabsView_TabStripDragOver(object sender, DragEventArgs e)
//        {
//            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDragOver START");
//            bool hasKey = e.DataView.Properties.ContainsKey(TabIdKey);
//            if (hasKey)
//                e.AcceptedOperation = DataPackageOperation.Move;
//            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDragOver END, Accepted={hasKey}");
//        }

//        private async void TabsView_TabStripDrop(object sender, DragEventArgs e)
//        {
//            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop START");

//            if (!e.DataView.Properties.TryGetValue(TabIdKey, out object idObj))
//            {
//                Debug.WriteLine($"[TabViewManager] TabId not found in DataPackage, exiting");
//                return;
//            }
//            string draggedTabId = idObj.ToString();
//            Debug.WriteLine($"[TabViewManager] TabId={draggedTabId}");

//            var destinationTabView = sender as TabView;
//            if (destinationTabView == null) return;

//            int insertIndex = -1;
//            for (int i = 0; i < destinationTabView.TabItems.Count; i++)
//            {
//                var item = destinationTabView.ContainerFromIndex(i) as TabViewItem;
//                if (item != null && e.GetPosition(item).X - item.ActualWidth < 0)
//                {
//                    insertIndex = i;
//                    break;
//                }
//            }

//            TabViewItem sourceTab = null;
//            foreach (TabViewItem tab in destinationTabView.TabItems)
//            {
//                if (tab.Tag?.ToString() == draggedTabId)
//                {
//                    sourceTab = tab;
//                    break;
//                }
//            }

//            if (sourceTab == null)
//            {
//                Debug.WriteLine($"[TabViewManager] sourceTab not found in current TabView, ignoring drop");
//                return;
//            }

//            var header = sourceTab.Header;
//            var iconSource = sourceTab.IconSource;
//            var content = _tabContentMap.ContainsKey(sourceTab) ? _tabContentMap[sourceTab] : null;

//            destinationTabView.TabItems.Remove(sourceTab);
//            _tabContentMap.Remove(sourceTab);

//            var newTab = new TabViewItem
//            {
//                Header = header,
//                IconSource = iconSource,
//                Tag = Guid.NewGuid().ToString()
//            };

//            if (insertIndex < 0)
//                destinationTabView.TabItems.Add(newTab);
//            else
//                destinationTabView.TabItems.Insert(insertIndex, newTab);

//            if (content != null)
//                _tabContentMap[newTab] = content;

//            destinationTabView.SelectedItem = newTab;
//            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop END");
//        }

//        public void TransferContent(TabViewItem sourceTab, TabViewItem targetTab)
//        {
//            Debug.WriteLine($"[TabViewManager] TransferContent START: sourceTab.Header='{sourceTab?.Header}', targetTab.Header='{targetTab?.Header}'");
//            if (_tabContentMap.TryGetValue(sourceTab, out var content))
//            {
//                _tabContentMap[targetTab] = content;
//                _tabContentMap.Remove(sourceTab);
//                Debug.WriteLine($"[TabViewManager] TransferContent: content transferred");
//            }
//            else
//            {
//                Debug.WriteLine($"[TabViewManager] TransferContent: content NOT found for sourceTab");
//            }
//            Debug.WriteLine($"[TabViewManager] TransferContent END");
//        }
//    }

//    // Вспомогательные методы расширения для хранения флага подписки
//    public static class TabViewItemExtensions
//    {
//        private const string PointerPressedSubscribedKey = "IsPointerPressedSubscribed";
//        private static DependencyProperty _isPointerPressedSubscribedProperty;

//        private static DependencyProperty IsPointerPressedSubscribedProperty
//        {
//            get
//            {
//                if (_isPointerPressedSubscribedProperty == null)
//                {
//                    _isPointerPressedSubscribedProperty = DependencyProperty.RegisterAttached(
//                        "IsPointerPressedSubscribed",
//                        typeof(bool),
//                        typeof(TabViewItemExtensions),
//                        new PropertyMetadata(false));
//                }
//                return _isPointerPressedSubscribedProperty;
//            }
//        }

//        public static bool IsInTabStripPointerPressedSubscribed(this TabViewItem tab)
//        {
//            return (bool)tab.GetValue(IsPointerPressedSubscribedProperty);
//        }

//        public static void SetIsInTabStripPointerPressedSubscribed(this TabViewItem tab, bool value)
//        {
//            tab.SetValue(IsPointerPressedSubscribedProperty, value);
//        }
//    }
//}



//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Threading.Tasks;
//using Microsoft.UI.Xaml;
//using Microsoft.UI.Xaml.Controls;
//using Microsoft.UI.Xaml.Input;
//using Windows.ApplicationModel.DataTransfer;
//using ufm.Pages;

//namespace ufm
//{
//    public class TabViewManager
//    {
//        private TabView _tabsView;
//        private Frame _contentFrame;
//        private const string TabIdKey = "TabId";
//        private readonly Dictionary<TabViewItem, UIElement> _tabContentMap = new();
//        private readonly bool _skipInitialTab;
//        private string _draggedTabId;

//        // Храним последнюю вкладку, на которой было нажатие указателя
//        private TabViewItem _lastPressedTab;

//        public TabViewManager(TabView tabsView, Frame contentFrame)
//            : this(tabsView, contentFrame, false)
//        {
//        }

//        public TabViewManager(TabView tabsView, Frame contentFrame, bool skipInitialTab)
//        {
//            Debug.WriteLine($"[TabViewManager] Constructor START: tabsView={tabsView != null}, contentFrame={contentFrame != null}, skipInitialTab={skipInitialTab}");
//            _tabsView = tabsView;
//            _contentFrame = contentFrame;
//            _skipInitialTab = skipInitialTab;
//            Initialize();
//            Debug.WriteLine($"[TabViewManager] Constructor END");
//        }

//        private void Initialize()
//        {
//            Debug.WriteLine($"[TabViewManager] Initialize START");

//            _tabsView.Loaded += TabsView_Loaded;
//            _tabsView.SelectionChanged += TabsView_SelectionChanged;
//            _tabsView.AddTabButtonClick += TabsView_OnAddTabButtonClick;
//            _tabsView.TabCloseRequested += TabsView_OnTabCloseRequested;
//            _tabsView.TabDragStarting += TabsView_TabDragStarting;
//            _tabsView.TabDroppedOutside += TabsView_TabDroppedOutside;
//            _tabsView.TabStripDragOver += TabsView_TabStripDragOver;
//            _tabsView.TabStripDrop += TabsView_TabStripDrop;

//            Debug.WriteLine($"[TabViewManager] Initialize END, events subscribed");
//        }

//        private void TabsView_Loaded(object sender, RoutedEventArgs e)
//        {
//            Debug.WriteLine($"[TabViewManager] TabsView_Loaded START, _skipInitialTab={_skipInitialTab}, TabItems.Count={_tabsView.TabItems.Count}");

//            // Подписываемся на PointerPressed для всех уже существующих вкладок
//            foreach (var item in _tabsView.TabItems)
//            {
//                var tab = item as TabViewItem;
//                if (tab != null && !tab.IsInTabStripPointerPressedSubscribed())
//                {
//                    tab.PointerPressed += TabViewItem_PointerPressed;
//                    tab.SetIsInTabStripPointerPressedSubscribed(true);
//                }
//            }

//            if (_skipInitialTab)
//            {
//                Debug.WriteLine($"[TabViewManager] TabsView_Loaded: skipping initial tab creation (drag&drop window)");
//                return;
//            }

//            if (_tabsView.TabItems.Count == 0)
//            {
//                var initialTab = CreateNewTab("Root Page");
//                _tabsView.TabItems.Add(initialTab);
//                _tabsView.SelectedIndex = 0;
//                Debug.WriteLine($"[TabViewManager] TabsView_Loaded: initialTab added, TabItems.Count={_tabsView.TabItems.Count}");
//            }
//            else
//            {
//                Debug.WriteLine($"[TabViewManager] TabsView_Loaded: TabItems already has {_tabsView.TabItems.Count} tabs, skipping creation");
//            }
//            Debug.WriteLine($"[TabViewManager] TabsView_Loaded END");
//        }

//        private void TabViewItem_PointerPressed(object sender, PointerRoutedEventArgs e)
//        {
//            var tab = sender as TabViewItem;
//            if (tab != null)
//            {
//                _lastPressedTab = tab;
//                Debug.WriteLine($"[PointerPressed] Last pressed tab: '{tab.Header}'");
//            }
//        }

//        public TabViewItem CreateNewTab(string header)
//        {
//            Debug.WriteLine($"[TabViewManager] CreateNewTab START: header='{header}'");
//            var newTab = new TabViewItem
//            {
//                Header = header,
//                IconSource = new SymbolIconSource { Symbol = Symbol.Placeholder },
//                Tag = Guid.NewGuid().ToString()
//            };
//            // Подписываемся на PointerPressed сразу при создании
//            newTab.PointerPressed += TabViewItem_PointerPressed;
//            newTab.SetIsInTabStripPointerPressedSubscribed(true);
//            Debug.WriteLine($"[TabViewManager] CreateNewTab END: newTab created, Header='{newTab.Header}', Tag='{newTab.Tag}'");
//            return newTab;
//        }

//        private UIElement CreateTabContent(string dataContext = null)
//        {
//            Debug.WriteLine($"[TabViewManager] CreateTabContent START: dataContext='{dataContext}'");
//            var frame = new Frame();
//            frame.Navigate(typeof(rootPage), dataContext);
//            Debug.WriteLine($"[TabViewManager] CreateTabContent END: Frame created, Content={frame.Content?.GetType().Name}");
//            return frame;
//        }

//        private void TabsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged START");
//            var selectedTab = _tabsView.SelectedItem as TabViewItem;
//            Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: selectedTab={selectedTab != null}, Header='{selectedTab?.Header}'");

//            if (selectedTab != null)
//            {
//                Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: checking _tabContentMap.ContainsKey={_tabContentMap.ContainsKey(selectedTab)}");
//                if (!_tabContentMap.ContainsKey(selectedTab))
//                {
//                    Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: creating new content for tab");
//                    var content = CreateTabContent();
//                    _tabContentMap[selectedTab] = content;
//                    Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: content added to map, _tabContentMap.Count={_tabContentMap.Count}");
//                }

//                _contentFrame.Content = _tabContentMap[selectedTab];
//                Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: _contentFrame.Content set to tab content");
//            }
//            else
//            {
//                _contentFrame.Content = null;
//                Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: selectedTab is null, _contentFrame.Content set to null");
//            }
//            Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged END");
//        }

//        public void TabsView_OnAddTabButtonClick(TabView sender, object args)
//        {
//            Debug.WriteLine($"[TabViewManager] TabsView_OnAddTabButtonClick START: sender.TabItems.Count={sender.TabItems.Count}");
//            var newTab = CreateNewTab("New Tab");
//            _tabsView.TabItems.Add(newTab);
//            _tabsView.SelectedItem = newTab;
//            Debug.WriteLine($"[TabViewManager] TabsView_OnAddTabButtonClick END: TabItems.Count={_tabsView.TabItems.Count}, SelectedIndex={_tabsView.SelectedIndex}");
//        }

//        public void TabsView_OnTabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
//        {
//            Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested START: Tab.Header='{args.Tab.Header}', sender.TabItems.Count={sender.TabItems.Count}");

//            if (_tabContentMap.ContainsKey(args.Tab))
//            {
//                Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested: removing content from map");
//                _tabContentMap.Remove(args.Tab);
//                Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested: _tabContentMap.Count={_tabContentMap.Count}");
//            }

//            sender.TabItems.Remove(args.Tab);
//            Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested: after removal, TabItems.Count={sender.TabItems.Count}");

//            if (sender.TabItems.Count == 0)
//            {
//                _contentFrame.Content = null;
//                Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested: no tabs left, _contentFrame.Content set to null");
//            }
//            Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested END");
//        }

//        // -----------------------------------------------------------------
//        // Drag & Drop с приоритетом: args.Tab → _lastPressedTab → SelectedItem
//        // -----------------------------------------------------------------
//        private void TabsView_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
//        {
//            Debug.WriteLine($"[TabViewManager] TabsView_TabDragStarting START");

//            // 1. Пробуем взять вкладку из аргументов события (та, за которую тянут)
//            var draggedTab = args.Tab;

//            // 2. Если args.Tab не дал результат, используем последнюю нажатую вкладку
//            if (draggedTab == null || !sender.TabItems.Contains(draggedTab))
//            {
//                draggedTab = _lastPressedTab;
//                if (draggedTab != null)
//                    Debug.WriteLine("[Drag] Using _lastPressedTab");
//            }

//            // 3. Если и это не помогло, берём выделенную вкладку
//            if (draggedTab == null || !sender.TabItems.Contains(draggedTab))
//            {
//                draggedTab = sender.SelectedItem as TabViewItem;
//                if (draggedTab != null)
//                    Debug.WriteLine("[Drag] Using SelectedItem as fallback");
//            }

//            if (draggedTab == null)
//            {
//                Debug.WriteLine("[Drag] No tab to drag, aborting.");
//                args.Cancel = true;
//                return;
//            }

//            string tabId = draggedTab.Tag?.ToString();
//            if (!string.IsNullOrEmpty(tabId))
//            {
//                _draggedTabId = tabId;
//                args.Data.Properties.Add(TabIdKey, tabId);
//                Debug.WriteLine($"[Drag] Dragging tab: Header='{draggedTab.Header}', TabId={tabId}");
//            }
//            else
//            {
//                Debug.WriteLine("[Drag] Tab.Tag is null, aborting drag.");
//                args.Cancel = true;
//                return;
//            }

//            args.Data.RequestedOperation = DataPackageOperation.Move;
//            Debug.WriteLine($"[TabViewManager] TabsView_TabDragStarting END");
//        }

//        private async void TabsView_TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
//        {
//            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside START");

//            if (string.IsNullOrEmpty(_draggedTabId))
//            {
//                Debug.WriteLine($"[TabViewManager] _draggedTabId is empty, aborting");
//                return;
//            }

//            string draggedTabId = _draggedTabId;
//            _draggedTabId = null;
//            Debug.WriteLine($"[TabViewManager] Retrieved draggedTabId = {draggedTabId}");

//            TabViewItem sourceTab = null;
//            foreach (TabViewItem tab in sender.TabItems)
//            {
//                if (tab.Tag?.ToString() == draggedTabId)
//                {
//                    sourceTab = tab;
//                    break;
//                }
//            }

//            if (sourceTab == null)
//            {
//                Debug.WriteLine($"[TabViewManager] sourceTab not found by Tag, aborting");
//                return;
//            }

//            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: source Tab.Header='{sourceTab.Header}'");
//            var sourceContent = _tabContentMap.ContainsKey(sourceTab) ? _tabContentMap[sourceTab] : null;
//            int selectedIndex = _tabsView.SelectedIndex;
//            var savedHeader = sourceTab.Header;
//            var savedIconSource = sourceTab.IconSource;

//            if (_tabContentMap.ContainsKey(sourceTab))
//                _tabContentMap.Remove(sourceTab);
//            sender.TabItems.Remove(sourceTab);

//            if (sender.TabItems.Count > 0)
//            {
//                int newIndex = selectedIndex < sender.TabItems.Count ? selectedIndex : sender.TabItems.Count - 1;
//                if (newIndex >= 0)
//                    _tabsView.SelectedIndex = newIndex;
//            }

//            var newWindow = new MainWindow(true);
//            newWindow.ExtendsContentIntoTitleBar = true;

//            var newTab = newWindow.TabViewManager.CreateNewTab(savedHeader?.ToString() ?? "Tab");
//            newTab.IconSource = savedIconSource;

//            if (sourceContent != null)
//                newWindow.TabViewManager.TransferContentWithUIElement(sourceContent, newTab);
//            else
//            {
//                var frame = new Frame();
//                frame.Navigate(typeof(rootPage), null);
//                newWindow.TabViewManager.TransferContentWithUIElement(frame, newTab);
//            }

//            newWindow.MainTabsView.TabItems.Add(newTab);
//            newWindow.TabViewManager._tabsView.SelectedItem = newTab;

//            await Task.Delay(50);
//            newWindow.Activate();

//            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside END");
//        }

//        public void TransferContentWithUIElement(UIElement content, TabViewItem targetTab)
//        {
//            Debug.WriteLine($"[TabViewManager] TransferContentWithUIElement START: content={content != null}, targetTab.Header='{targetTab.Header}'");
//            _tabContentMap[targetTab] = content;
//            Debug.WriteLine($"[TabViewManager] TransferContentWithUIElement END: _tabContentMap.Count={_tabContentMap.Count}");
//        }

//        public int GetTabContentMapCount() => _tabContentMap.Count;

//        private void TabsView_TabStripDragOver(object sender, DragEventArgs e)
//        {
//            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDragOver START");
//            bool hasKey = e.DataView.Properties.ContainsKey(TabIdKey);
//            if (hasKey)
//                e.AcceptedOperation = DataPackageOperation.Move;
//            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDragOver END, Accepted={hasKey}");
//        }

//        private async void TabsView_TabStripDrop(object sender, DragEventArgs e)
//        {
//            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop START");

//            if (!e.DataView.Properties.TryGetValue(TabIdKey, out object idObj))
//            {
//                Debug.WriteLine($"[TabViewManager] TabId not found in DataPackage, exiting");
//                return;
//            }
//            string draggedTabId = idObj.ToString();
//            Debug.WriteLine($"[TabViewManager] TabId={draggedTabId}");

//            var destinationTabView = sender as TabView;
//            if (destinationTabView == null) return;

//            int insertIndex = -1;
//            for (int i = 0; i < destinationTabView.TabItems.Count; i++)
//            {
//                var item = destinationTabView.ContainerFromIndex(i) as TabViewItem;
//                if (item != null && e.GetPosition(item).X - item.ActualWidth < 0)
//                {
//                    insertIndex = i;
//                    break;
//                }
//            }

//            TabViewItem sourceTab = null;
//            foreach (TabViewItem tab in destinationTabView.TabItems)
//            {
//                if (tab.Tag?.ToString() == draggedTabId)
//                {
//                    sourceTab = tab;
//                    break;
//                }
//            }

//            if (sourceTab == null)
//            {
//                Debug.WriteLine($"[TabViewManager] sourceTab not found in current TabView, ignoring drop");
//                return;
//            }

//            var header = sourceTab.Header;
//            var iconSource = sourceTab.IconSource;
//            var content = _tabContentMap.ContainsKey(sourceTab) ? _tabContentMap[sourceTab] : null;

//            destinationTabView.TabItems.Remove(sourceTab);
//            _tabContentMap.Remove(sourceTab);

//            var newTab = new TabViewItem
//            {
//                Header = header,
//                IconSource = iconSource,
//                Tag = Guid.NewGuid().ToString()
//            };

//            if (insertIndex < 0)
//                destinationTabView.TabItems.Add(newTab);
//            else
//                destinationTabView.TabItems.Insert(insertIndex, newTab);

//            if (content != null)
//                _tabContentMap[newTab] = content;

//            destinationTabView.SelectedItem = newTab;
//            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop END");
//        }

//        public void TransferContent(TabViewItem sourceTab, TabViewItem targetTab)
//        {
//            Debug.WriteLine($"[TabViewManager] TransferContent START: sourceTab.Header='{sourceTab?.Header}', targetTab.Header='{targetTab?.Header}'");
//            if (_tabContentMap.TryGetValue(sourceTab, out var content))
//            {
//                _tabContentMap[targetTab] = content;
//                _tabContentMap.Remove(sourceTab);
//                Debug.WriteLine($"[TabViewManager] TransferContent: content transferred");
//            }
//            else
//            {
//                Debug.WriteLine($"[TabViewManager] TransferContent: content NOT found for sourceTab");
//            }
//            Debug.WriteLine($"[TabViewManager] TransferContent END");
//        }
//    }

//    // Вспомогательные методы расширения для хранения флага подписки
//    public static class TabViewItemExtensions
//    {
//        private static DependencyProperty _isPointerPressedSubscribedProperty;

//        private static DependencyProperty IsPointerPressedSubscribedProperty
//        {
//            get
//            {
//                if (_isPointerPressedSubscribedProperty == null)
//                {
//                    _isPointerPressedSubscribedProperty = DependencyProperty.RegisterAttached(
//                        "IsPointerPressedSubscribed",
//                        typeof(bool),
//                        typeof(TabViewItemExtensions),
//                        new PropertyMetadata(false));
//                }
//                return _isPointerPressedSubscribedProperty;
//            }
//        }

//        public static bool IsInTabStripPointerPressedSubscribed(this TabViewItem tab)
//        {
//            return (bool)tab.GetValue(IsPointerPressedSubscribedProperty);
//        }

//        public static void SetIsInTabStripPointerPressedSubscribed(this TabViewItem tab, bool value)
//        {
//            tab.SetValue(IsPointerPressedSubscribedProperty, value);
//        }
//    }
//}



//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Runtime.InteropServices;
//using System.Threading.Tasks;
//using Microsoft.UI.Xaml;
//using Microsoft.UI.Xaml.Controls;
//using Microsoft.UI.Xaml.Media;
//using Windows.ApplicationModel.DataTransfer;
//using Windows.Foundation;
//using WinRT.Interop;
//using ufm.Pages;

//namespace ufm
//{
//    public class TabViewManager
//    {
//        private TabView _tabsView;
//        private Frame _contentFrame;
//        private Window _mainWindow; // <-- Добавлено для получения HWND
//        private const string TabIdKey = "TabId";
//        private readonly Dictionary<TabViewItem, UIElement> _tabContentMap = new();
//        private readonly bool _skipInitialTab;
//        private string _draggedTabId;

//        // P/Invoke для получения позиции курсора
//        [DllImport("user32.dll")]
//        private static extern bool GetCursorPos(out POINT lpPoint);

//        [DllImport("user32.dll")]
//        private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

//        [StructLayout(LayoutKind.Sequential)]
//        private struct POINT
//        {
//            public int X;
//            public int Y;
//        }

//        public TabViewManager(TabView tabsView, Frame contentFrame, Window mainWindow)
//            : this(tabsView, contentFrame, mainWindow, false)
//        {
//        }

//        public TabViewManager(TabView tabsView, Frame contentFrame, Window mainWindow, bool skipInitialTab)
//        {
//            Debug.WriteLine($"[TabViewManager] Constructor START: tabsView={tabsView != null}, contentFrame={contentFrame != null}, skipInitialTab={skipInitialTab}");
//            _tabsView = tabsView;
//            _contentFrame = contentFrame;
//            _mainWindow = mainWindow; // <-- Сохраняем ссылку на окно
//            _skipInitialTab = skipInitialTab;
//            Initialize();
//            Debug.WriteLine($"[TabViewManager] Constructor END");
//        }

//        private void Initialize()
//        {
//            Debug.WriteLine($"[TabViewManager] Initialize START");

//            _tabsView.Loaded += TabsView_Loaded;
//            _tabsView.SelectionChanged += TabsView_SelectionChanged;
//            _tabsView.AddTabButtonClick += TabsView_OnAddTabButtonClick;
//            _tabsView.TabCloseRequested += TabsView_OnTabCloseRequested;
//            _tabsView.TabDragStarting += TabsView_TabDragStarting;
//            _tabsView.TabDroppedOutside += TabsView_TabDroppedOutside;
//            _tabsView.TabStripDragOver += TabsView_TabStripDragOver;
//            _tabsView.TabStripDrop += TabsView_TabStripDrop;

//            Debug.WriteLine($"[TabViewManager] Initialize END, events subscribed");
//        }

//        private void TabsView_Loaded(object sender, RoutedEventArgs e)
//        {
//            Debug.WriteLine($"[TabViewManager] TabsView_Loaded START, _skipInitialTab={_skipInitialTab}, TabItems.Count={_tabsView.TabItems.Count}");

//            if (_skipInitialTab)
//            {
//                Debug.WriteLine($"[TabViewManager] TabsView_Loaded: skipping initial tab creation (drag&drop window)");
//                return;
//            }

//            if (_tabsView.TabItems.Count == 0)
//            {
//                var initialTab = CreateNewTab("Root Page");
//                _tabsView.TabItems.Add(initialTab);
//                _tabsView.SelectedIndex = 0;
//                Debug.WriteLine($"[TabViewManager] TabsView_Loaded: initialTab added, TabItems.Count={_tabsView.TabItems.Count}");
//            }
//            else
//            {
//                Debug.WriteLine($"[TabViewManager] TabsView_Loaded: TabItems already has {_tabsView.TabItems.Count} tabs, skipping creation");
//            }
//            Debug.WriteLine($"[TabViewManager] TabsView_Loaded END");
//        }

//        public TabViewItem CreateNewTab(string header)
//        {
//            Debug.WriteLine($"[TabViewManager] CreateNewTab START: header='{header}'");
//            var newTab = new TabViewItem
//            {
//                Header = header,
//                IconSource = new SymbolIconSource { Symbol = Symbol.Placeholder },
//                Tag = Guid.NewGuid().ToString()
//            };
//            Debug.WriteLine($"[TabViewManager] CreateNewTab END: newTab created, Header='{newTab.Header}', Tag='{newTab.Tag}'");
//            return newTab;
//        }

//        private UIElement CreateTabContent(string dataContext = null)
//        {
//            Debug.WriteLine($"[TabViewManager] CreateTabContent START: dataContext='{dataContext}'");
//            var frame = new Frame();
//            frame.Navigate(typeof(rootPage), dataContext);
//            Debug.WriteLine($"[TabViewManager] CreateTabContent END: Frame created, Content={frame.Content?.GetType().Name}");
//            return frame;
//        }

//        private void TabsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged START");
//            var selectedTab = _tabsView.SelectedItem as TabViewItem;
//            Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: selectedTab={selectedTab != null}, Header='{selectedTab?.Header}'");

//            if (selectedTab != null)
//            {
//                Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: checking _tabContentMap.ContainsKey={_tabContentMap.ContainsKey(selectedTab)}");
//                if (!_tabContentMap.ContainsKey(selectedTab))
//                {
//                    Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: creating new content for tab");
//                    var content = CreateTabContent();
//                    _tabContentMap[selectedTab] = content;
//                    Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: content added to map, _tabContentMap.Count={_tabContentMap.Count}");
//                }

//                _contentFrame.Content = _tabContentMap[selectedTab];
//                Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: _contentFrame.Content set to tab content");
//            }
//            else
//            {
//                _contentFrame.Content = null;
//                Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: selectedTab is null, _contentFrame.Content set to null");
//            }
//            Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged END");
//        }

//        public void TabsView_OnAddTabButtonClick(TabView sender, object args)
//        {
//            Debug.WriteLine($"[TabViewManager] TabsView_OnAddTabButtonClick START: sender.TabItems.Count={sender.TabItems.Count}");
//            var newTab = CreateNewTab("New Tab");
//            _tabsView.TabItems.Add(newTab);
//            _tabsView.SelectedItem = newTab;
//            Debug.WriteLine($"[TabViewManager] TabsView_OnAddTabButtonClick END: TabItems.Count={_tabsView.TabItems.Count}, SelectedIndex={_tabsView.SelectedIndex}");
//        }

//        public void TabsView_OnTabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
//        {
//            Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested START: Tab.Header='{args.Tab.Header}', sender.TabItems.Count={sender.TabItems.Count}");

//            if (_tabContentMap.ContainsKey(args.Tab))
//            {
//                Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested: removing content from map");
//                _tabContentMap.Remove(args.Tab);
//                Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested: _tabContentMap.Count={_tabContentMap.Count}");
//            }

//            sender.TabItems.Remove(args.Tab);
//            Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested: after removal, TabItems.Count={sender.TabItems.Count}");

//            if (sender.TabItems.Count == 0)
//            {
//                _contentFrame.Content = null;
//                Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested: no tabs left, _contentFrame.Content set to null");
//            }
//            Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested END");
//        }

//        // -----------------------------------------------------------------
//        // Drag & Drop с hit-test через HWND окна (гарантированно работает)
//        // -----------------------------------------------------------------
//        private void TabsView_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
//        {
//            Debug.WriteLine($"[TabViewManager] TabsView_TabDragStarting START");

//            TabViewItem draggedTab = null;

//            try
//            {
//                // 1. Получаем глобальную позицию курсора
//                GetCursorPos(out POINT screenPoint);

//                // 2. Получаем HWND главного окна
//                var windowHandle = WindowNative.GetWindowHandle(_mainWindow);

//                // 3. Преобразуем в координаты клиентской области окна
//                ScreenToClient(windowHandle, ref screenPoint);

//                // 4. Корректируем Y-координату (заголовок окна обычно 32-40 пикселей)
//                int titleBarOffset = _mainWindow.ExtendsContentIntoTitleBar ? 0 : 32;
//                var localPoint = new Point(screenPoint.X, screenPoint.Y - titleBarOffset);

//                // 5. Получаем корневой элемент окна
//                var rootElement = _mainWindow.Content;
//                if (rootElement != null)
//                {
//                    // 6. Преобразуем координаты в пространство корневого элемента
//                    var transform = rootElement.TransformToVisual(null);
//                    var rootScreenPoint = transform.TransformPoint(new Point(0, 0));
//                    var finalPoint = new Point(localPoint.X - rootScreenPoint.X, localPoint.Y - rootScreenPoint.Y);

//                    // 7. Ищем элементы под курсором
//                    var elements = VisualTreeHelper.FindElementsInHostCoordinates(finalPoint, rootElement);
//                    foreach (var element in elements)
//                    {
//                        // Поднимаемся по визуальному дереву в поисках TabViewItem
//                        var tab = FindParentTabViewItem(element);
//                        if (tab != null)
//                        {
//                            draggedTab = tab;
//                            break;
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[Drag] Hit-test error: {ex.Message}");
//            }

//            // Fallback на SelectedItem, если визуальный поиск не дал результата
//            if (draggedTab == null)
//            {
//                draggedTab = sender.SelectedItem as TabViewItem;
//                Debug.WriteLine("[Drag] Using SelectedItem as fallback");
//            }

//            if (draggedTab == null)
//            {
//                Debug.WriteLine("[Drag] No tab to drag, aborting.");
//                args.Cancel = true;
//                return;
//            }

//            string tabId = draggedTab.Tag?.ToString();
//            if (!string.IsNullOrEmpty(tabId))
//            {
//                _draggedTabId = tabId;
//                args.Data.Properties.Add(TabIdKey, tabId);
//                Debug.WriteLine($"[Drag] Dragging tab: Header='{draggedTab.Header}', TabId={tabId}");
//            }
//            else
//            {
//                Debug.WriteLine("[Drag] Tab.Tag is null, aborting drag.");
//                args.Cancel = true;
//                return;
//            }

//            args.Data.RequestedOperation = DataPackageOperation.Move;
//            Debug.WriteLine($"[TabViewManager] TabsView_TabDragStarting END");
//        }

//        // Вспомогательный метод поиска TabViewItem в визуальных родителях
//        private TabViewItem FindParentTabViewItem(DependencyObject child)
//        {
//            while (child != null)
//            {
//                if (child is TabViewItem tabItem)
//                    return tabItem;
//                child = VisualTreeHelper.GetParent(child);
//            }
//            return null;
//        }

//        private async void TabsView_TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
//        {
//            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside START");

//            if (string.IsNullOrEmpty(_draggedTabId))
//            {
//                Debug.WriteLine($"[TabViewManager] _draggedTabId is empty, aborting");
//                return;
//            }

//            string draggedTabId = _draggedTabId;
//            _draggedTabId = null;
//            Debug.WriteLine($"[TabViewManager] Retrieved draggedTabId = {draggedTabId}");

//            TabViewItem sourceTab = null;
//            foreach (TabViewItem tab in sender.TabItems)
//            {
//                if (tab.Tag?.ToString() == draggedTabId)
//                {
//                    sourceTab = tab;
//                    break;
//                }
//            }

//            if (sourceTab == null)
//            {
//                Debug.WriteLine($"[TabViewManager] sourceTab not found by Tag, aborting");
//                return;
//            }

//            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: source Tab.Header='{sourceTab.Header}'");
//            var sourceContent = _tabContentMap.ContainsKey(sourceTab) ? _tabContentMap[sourceTab] : null;
//            int selectedIndex = _tabsView.SelectedIndex;
//            var savedHeader = sourceTab.Header;
//            var savedIconSource = sourceTab.IconSource;

//            if (_tabContentMap.ContainsKey(sourceTab))
//                _tabContentMap.Remove(sourceTab);
//            sender.TabItems.Remove(sourceTab);

//            if (sender.TabItems.Count > 0)
//            {
//                int newIndex = selectedIndex < sender.TabItems.Count ? selectedIndex : sender.TabItems.Count - 1;
//                if (newIndex >= 0)
//                    _tabsView.SelectedIndex = newIndex;
//            }

//            var newWindow = new MainWindow(true);
//            newWindow.ExtendsContentIntoTitleBar = true;

//            var newTab = newWindow.TabViewManager.CreateNewTab(savedHeader?.ToString() ?? "Tab");
//            newTab.IconSource = savedIconSource;

//            if (sourceContent != null)
//                newWindow.TabViewManager.TransferContentWithUIElement(sourceContent, newTab);
//            else
//            {
//                var frame = new Frame();
//                frame.Navigate(typeof(rootPage), null);
//                newWindow.TabViewManager.TransferContentWithUIElement(frame, newTab);
//            }

//            newWindow.MainTabsView.TabItems.Add(newTab);
//            newWindow.TabViewManager._tabsView.SelectedItem = newTab;

//            await Task.Delay(50);
//            newWindow.Activate();

//            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside END");
//        }

//        public void TransferContentWithUIElement(UIElement content, TabViewItem targetTab)
//        {
//            Debug.WriteLine($"[TabViewManager] TransferContentWithUIElement START: content={content != null}, targetTab.Header='{targetTab.Header}'");
//            _tabContentMap[targetTab] = content;
//            Debug.WriteLine($"[TabViewManager] TransferContentWithUIElement END: _tabContentMap.Count={_tabContentMap.Count}");
//        }

//        public int GetTabContentMapCount() => _tabContentMap.Count;

//        private void TabsView_TabStripDragOver(object sender, DragEventArgs e)
//        {
//            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDragOver START");
//            bool hasKey = e.DataView.Properties.ContainsKey(TabIdKey);
//            if (hasKey)
//                e.AcceptedOperation = DataPackageOperation.Move;
//            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDragOver END, Accepted={hasKey}");
//        }

//        private async void TabsView_TabStripDrop(object sender, DragEventArgs e)
//        {
//            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop START");

//            if (!e.DataView.Properties.TryGetValue(TabIdKey, out object idObj))
//            {
//                Debug.WriteLine($"[TabViewManager] TabId not found in DataPackage, exiting");
//                return;
//            }
//            string draggedTabId = idObj.ToString();
//            Debug.WriteLine($"[TabViewManager] TabId={draggedTabId}");

//            var destinationTabView = sender as TabView;
//            if (destinationTabView == null) return;

//            int insertIndex = -1;
//            for (int i = 0; i < destinationTabView.TabItems.Count; i++)
//            {
//                var item = destinationTabView.ContainerFromIndex(i) as TabViewItem;
//                if (item != null && e.GetPosition(item).X - item.ActualWidth < 0)
//                {
//                    insertIndex = i;
//                    break;
//                }
//            }

//            TabViewItem sourceTab = null;
//            foreach (TabViewItem tab in destinationTabView.TabItems)
//            {
//                if (tab.Tag?.ToString() == draggedTabId)
//                {
//                    sourceTab = tab;
//                    break;
//                }
//            }

//            if (sourceTab == null)
//            {
//                Debug.WriteLine($"[TabViewManager] sourceTab not found in current TabView, ignoring drop");
//                return;
//            }

//            var header = sourceTab.Header;
//            var iconSource = sourceTab.IconSource;
//            var content = _tabContentMap.ContainsKey(sourceTab) ? _tabContentMap[sourceTab] : null;

//            destinationTabView.TabItems.Remove(sourceTab);
//            _tabContentMap.Remove(sourceTab);

//            var newTab = new TabViewItem
//            {
//                Header = header,
//                IconSource = iconSource,
//                Tag = Guid.NewGuid().ToString()
//            };

//            if (insertIndex < 0)
//                destinationTabView.TabItems.Add(newTab);
//            else
//                destinationTabView.TabItems.Insert(insertIndex, newTab);

//            if (content != null)
//                _tabContentMap[newTab] = content;

//            destinationTabView.SelectedItem = newTab;
//            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop END");
//        }

//        public void TransferContent(TabViewItem sourceTab, TabViewItem targetTab)
//        {
//            Debug.WriteLine($"[TabViewManager] TransferContent START: sourceTab.Header='{sourceTab?.Header}', targetTab.Header='{targetTab?.Header}'");
//            if (_tabContentMap.TryGetValue(sourceTab, out var content))
//            {
//                _tabContentMap[targetTab] = content;
//                _tabContentMap.Remove(sourceTab);
//                Debug.WriteLine($"[TabViewManager] TransferContent: content transferred");
//            }
//            else
//            {
//                Debug.WriteLine($"[TabViewManager] TransferContent: content NOT found for sourceTab");
//            }
//            Debug.WriteLine($"[TabViewManager] TransferContent END");
//        }
//    }
//}




using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using ufm.Pages;

namespace ufm
{
    public class TabViewManager
    {
        private TabView _tabsView;
        private Frame _contentFrame;
        private Window _mainWindow;
        private const string TabIdKey = "TabId";
        private readonly Dictionary<TabViewItem, UIElement> _tabContentMap = new();
        private readonly bool _skipInitialTab;
        private string _draggedTabId;

        public TabViewManager(TabView tabsView, Frame contentFrame, Window mainWindow)
            : this(tabsView, contentFrame, mainWindow, false)
        {
        }

        public TabViewManager(TabView tabsView, Frame contentFrame, Window mainWindow, bool skipInitialTab)
        {
            Debug.WriteLine($"[TabViewManager] Constructor START: tabsView={tabsView != null}, contentFrame={contentFrame != null}, skipInitialTab={skipInitialTab}");
            _tabsView = tabsView;
            _contentFrame = contentFrame;
            _mainWindow = mainWindow;
            _skipInitialTab = skipInitialTab;
            Initialize();
            Debug.WriteLine($"[TabViewManager] Constructor END");
        }

        private void Initialize()
        {
            Debug.WriteLine($"[TabViewManager] Initialize START");

            _tabsView.Loaded += TabsView_Loaded;
            _tabsView.SelectionChanged += TabsView_SelectionChanged;
            _tabsView.AddTabButtonClick += TabsView_OnAddTabButtonClick;
            _tabsView.TabCloseRequested += TabsView_OnTabCloseRequested;
            _tabsView.TabDragStarting += TabsView_TabDragStarting;
            _tabsView.TabDroppedOutside += TabsView_TabDroppedOutside;
            _tabsView.TabStripDragOver += TabsView_TabStripDragOver;
            _tabsView.TabStripDrop += TabsView_TabStripDrop;

            Debug.WriteLine($"[TabViewManager] Initialize END, events subscribed");
        }

        private void TabsView_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine($"[TabViewManager] TabsView_Loaded START, _skipInitialTab={_skipInitialTab}, TabItems.Count={_tabsView.TabItems.Count}");

            if (_skipInitialTab)
            {
                Debug.WriteLine($"[TabViewManager] TabsView_Loaded: skipping initial tab creation (drag&drop window)");
                return;
            }

            if (_tabsView.TabItems.Count == 0)
            {
                var initialTab = CreateNewTab("Root Page");
                _tabsView.TabItems.Add(initialTab);
                _tabsView.SelectedIndex = 0;
                Debug.WriteLine($"[TabViewManager] TabsView_Loaded: initialTab added, TabItems.Count={_tabsView.TabItems.Count}");
            }
            else
            {
                Debug.WriteLine($"[TabViewManager] TabsView_Loaded: TabItems already has {_tabsView.TabItems.Count} tabs, skipping creation");
            }
            Debug.WriteLine($"[TabViewManager] TabsView_Loaded END");
        }

        public TabViewItem CreateNewTab(string header)
        {
            Debug.WriteLine($"[TabViewManager] CreateNewTab START: header='{header}'");
            var newTab = new TabViewItem
            {
                Header = header,
                IconSource = new SymbolIconSource { Symbol = Symbol.Placeholder },
                Tag = Guid.NewGuid().ToString()
            };
            Debug.WriteLine($"[TabViewManager] CreateNewTab END: newTab created, Header='{newTab.Header}', Tag='{newTab.Tag}'");
            return newTab;
        }

        private UIElement CreateTabContent(string dataContext = null)
        {
            Debug.WriteLine($"[TabViewManager] CreateTabContent START: dataContext='{dataContext}'");
            var frame = new Frame();
            frame.Navigate(typeof(rootPage), dataContext);
            Debug.WriteLine($"[TabViewManager] CreateTabContent END: Frame created, Content={frame.Content?.GetType().Name}");
            return frame;
        }

        private void TabsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged START");
            var selectedTab = _tabsView.SelectedItem as TabViewItem;
            Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: selectedTab={selectedTab != null}, Header='{selectedTab?.Header}'");

            if (selectedTab != null)
            {
                Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: checking _tabContentMap.ContainsKey={_tabContentMap.ContainsKey(selectedTab)}");
                if (!_tabContentMap.ContainsKey(selectedTab))
                {
                    Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: creating new content for tab");
                    var content = CreateTabContent();
                    _tabContentMap[selectedTab] = content;
                    Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: content added to map, _tabContentMap.Count={_tabContentMap.Count}");
                }

                _contentFrame.Content = _tabContentMap[selectedTab];
                Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: _contentFrame.Content set to tab content");
            }
            else
            {
                _contentFrame.Content = null;
                Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: selectedTab is null, _contentFrame.Content set to null");
            }
            Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged END");
        }

        public void TabsView_OnAddTabButtonClick(TabView sender, object args)
        {
            Debug.WriteLine($"[TabViewManager] TabsView_OnAddTabButtonClick START: sender.TabItems.Count={sender.TabItems.Count}");
            var newTab = CreateNewTab("New Tab");
            _tabsView.TabItems.Add(newTab);
            _tabsView.SelectedItem = newTab;
            Debug.WriteLine($"[TabViewManager] TabsView_OnAddTabButtonClick END: TabItems.Count={_tabsView.TabItems.Count}, SelectedIndex={_tabsView.SelectedIndex}");
        }

        public void TabsView_OnTabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested START: Tab.Header='{args.Tab.Header}', sender.TabItems.Count={sender.TabItems.Count}");

            if (_tabContentMap.ContainsKey(args.Tab))
            {
                Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested: removing content from map");
                _tabContentMap.Remove(args.Tab);
                Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested: _tabContentMap.Count={_tabContentMap.Count}");
            }

            sender.TabItems.Remove(args.Tab);
            Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested: after removal, TabItems.Count={sender.TabItems.Count}");

            if (sender.TabItems.Count == 0)
            {
                _contentFrame.Content = null;
                Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested: no tabs left, _contentFrame.Content set to null");
            }
            Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested END");
        }

        // -----------------------------------------------------------------
        // Исправленный Drag & Drop: используем args.Tab вместо hit‑test
        // -----------------------------------------------------------------
        private void TabsView_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
        {
            Debug.WriteLine($"[TabViewManager] TabsView_TabDragStarting START");

            var draggedTab = args.Tab;
            if (draggedTab == null)
            {
                Debug.WriteLine("[Drag] args.Tab is null, aborting.");
                args.Cancel = true;
                return;
            }

            string tabId = draggedTab.Tag?.ToString();
            if (string.IsNullOrEmpty(tabId))
            {
                Debug.WriteLine("[Drag] Tab.Tag is null, aborting.");
                args.Cancel = true;
                return;
            }

            _draggedTabId = tabId;
            args.Data.Properties.Add(TabIdKey, tabId);
            args.Data.RequestedOperation = DataPackageOperation.Move;
            Debug.WriteLine($"[Drag] Dragging tab: Header='{draggedTab.Header}', TabId={tabId}");
            Debug.WriteLine($"[TabViewManager] TabsView_TabDragStarting END");
        }

        private async void TabsView_TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
        {
            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside START");

            if (string.IsNullOrEmpty(_draggedTabId))
            {
                Debug.WriteLine($"[TabViewManager] _draggedTabId is empty, aborting");
                return;
            }

            string draggedTabId = _draggedTabId;
            _draggedTabId = null;
            Debug.WriteLine($"[TabViewManager] Retrieved draggedTabId = {draggedTabId}");

            TabViewItem sourceTab = null;
            foreach (TabViewItem tab in sender.TabItems)
            {
                if (tab.Tag?.ToString() == draggedTabId)
                {
                    sourceTab = tab;
                    break;
                }
            }

            if (sourceTab == null)
            {
                Debug.WriteLine($"[TabViewManager] sourceTab not found by Tag, aborting");
                return;
            }

            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: source Tab.Header='{sourceTab.Header}'");
            var sourceContent = _tabContentMap.ContainsKey(sourceTab) ? _tabContentMap[sourceTab] : null;
            int selectedIndex = _tabsView.SelectedIndex;
            var savedHeader = sourceTab.Header;
            var savedIconSource = sourceTab.IconSource;

            if (_tabContentMap.ContainsKey(sourceTab))
                _tabContentMap.Remove(sourceTab);
            sender.TabItems.Remove(sourceTab);

            if (sender.TabItems.Count > 0)
            {
                int newIndex = selectedIndex < sender.TabItems.Count ? selectedIndex : sender.TabItems.Count - 1;
                if (newIndex >= 0)
                    _tabsView.SelectedIndex = newIndex;
            }

            var newWindow = new MainWindow(true);
            newWindow.ExtendsContentIntoTitleBar = true;

            var newTab = newWindow.TabViewManager.CreateNewTab(savedHeader?.ToString() ?? "Tab");
            newTab.IconSource = savedIconSource;

            if (sourceContent != null)
                newWindow.TabViewManager.TransferContentWithUIElement(sourceContent, newTab);
            else
            {
                var frame = new Frame();
                frame.Navigate(typeof(rootPage), null);
                newWindow.TabViewManager.TransferContentWithUIElement(frame, newTab);
            }

            newWindow.MainTabsView.TabItems.Add(newTab);
            newWindow.TabViewManager._tabsView.SelectedItem = newTab;

            await Task.Delay(50);
            newWindow.Activate();

            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside END");
        }

        public void TransferContentWithUIElement(UIElement content, TabViewItem targetTab)
        {
            Debug.WriteLine($"[TabViewManager] TransferContentWithUIElement START: content={content != null}, targetTab.Header='{targetTab.Header}'");
            _tabContentMap[targetTab] = content;
            Debug.WriteLine($"[TabViewManager] TransferContentWithUIElement END: _tabContentMap.Count={_tabContentMap.Count}");
        }

        public int GetTabContentMapCount() => _tabContentMap.Count;

        private void TabsView_TabStripDragOver(object sender, DragEventArgs e)
        {
            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDragOver START");
            bool hasKey = e.DataView.Properties.ContainsKey(TabIdKey);
            if (hasKey)
                e.AcceptedOperation = DataPackageOperation.Move;
            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDragOver END, Accepted={hasKey}");
        }

        private async void TabsView_TabStripDrop(object sender, DragEventArgs e)
        {
            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop START");

            if (!e.DataView.Properties.TryGetValue(TabIdKey, out object idObj))
            {
                Debug.WriteLine($"[TabViewManager] TabId not found in DataPackage, exiting");
                return;
            }
            string draggedTabId = idObj.ToString();
            Debug.WriteLine($"[TabViewManager] TabId={draggedTabId}");

            var destinationTabView = sender as TabView;
            if (destinationTabView == null) return;

            int insertIndex = -1;
            for (int i = 0; i < destinationTabView.TabItems.Count; i++)
            {
                var item = destinationTabView.ContainerFromIndex(i) as TabViewItem;
                if (item != null && e.GetPosition(item).X - item.ActualWidth < 0)
                {
                    insertIndex = i;
                    break;
                }
            }

            TabViewItem sourceTab = null;
            foreach (TabViewItem tab in destinationTabView.TabItems)
            {
                if (tab.Tag?.ToString() == draggedTabId)
                {
                    sourceTab = tab;
                    break;
                }
            }

            if (sourceTab == null)
            {
                Debug.WriteLine($"[TabViewManager] sourceTab not found in current TabView, ignoring drop");
                return;
            }

            var header = sourceTab.Header;
            var iconSource = sourceTab.IconSource;
            var content = _tabContentMap.ContainsKey(sourceTab) ? _tabContentMap[sourceTab] : null;

            destinationTabView.TabItems.Remove(sourceTab);
            _tabContentMap.Remove(sourceTab);

            var newTab = new TabViewItem
            {
                Header = header,
                IconSource = iconSource,
                Tag = Guid.NewGuid().ToString()
            };

            if (insertIndex < 0)
                destinationTabView.TabItems.Add(newTab);
            else
                destinationTabView.TabItems.Insert(insertIndex, newTab);

            if (content != null)
                _tabContentMap[newTab] = content;

            destinationTabView.SelectedItem = newTab;
            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop END");
        }

        public void TransferContent(TabViewItem sourceTab, TabViewItem targetTab)
        {
            Debug.WriteLine($"[TabViewManager] TransferContent START: sourceTab.Header='{sourceTab?.Header}', targetTab.Header='{targetTab?.Header}'");
            if (_tabContentMap.TryGetValue(sourceTab, out var content))
            {
                _tabContentMap[targetTab] = content;
                _tabContentMap.Remove(sourceTab);
                Debug.WriteLine($"[TabViewManager] TransferContent: content transferred");
            }
            else
            {
                Debug.WriteLine($"[TabViewManager] TransferContent: content NOT found for sourceTab");
            }
            Debug.WriteLine($"[TabViewManager] TransferContent END");
        }
    }
}