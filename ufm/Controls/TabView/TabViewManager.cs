
//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using Microsoft.UI.Dispatching;
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

//        public TabViewManager(TabView tabsView, Frame contentFrame)
//            : this(tabsView, contentFrame, false)
//        {
//        }

//        public TabViewManager(TabView tabsView, Frame contentFrame, bool skipInitialTab)
//        {
//            _tabsView = tabsView;
//            _contentFrame = contentFrame;
//            _skipInitialTab = skipInitialTab;
//            Initialize();
//        }

//        private void Initialize()
//        {
//            _tabsView.Loaded += TabsView_Loaded;
//            _tabsView.SelectionChanged += TabsView_SelectionChanged;
//            _tabsView.AddTabButtonClick += TabsView_OnAddTabButtonClick;
//            _tabsView.TabCloseRequested += TabsView_OnTabCloseRequested;
//            _tabsView.TabDragStarting += TabsView_TabDragStarting;
//            _tabsView.TabDroppedOutside += TabsView_TabDroppedOutside;
//            _tabsView.TabStripDragOver += TabsView_TabStripDragOver;
//            _tabsView.TabStripDrop += TabsView_TabStripDrop;
//            _tabsView.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(TabsView_PointerPressed), true);
//        }

//        private void TabsView_PointerPressed(object sender, PointerRoutedEventArgs e)
//        {
//            var tabView = sender as TabView;
//            if (tabView == null) return;

//            var pointerPoint = e.GetCurrentPoint(tabView);

//            if (!pointerPoint.Properties.IsLeftButtonPressed)
//                return;

//            foreach (var item in tabView.TabItems)
//            {
//                var container = tabView.ContainerFromItem(item) as TabViewItem;
//                if (container != null)
//                {
//                    try
//                    {
//                        var transform = container.TransformToVisual(tabView);
//                        var bounds = transform.TransformBounds(new Windows.Foundation.Rect(0, 0, container.ActualWidth, container.ActualHeight));

//                        if (bounds.Contains(pointerPoint.Position))
//                        {
//                            if (tabView.SelectedItem != item)
//                            {
//                                tabView.SelectedItem = item;
//                            }
//                            break;
//                        }
//                    }
//                    catch
//                    {
//                        // Игнорируем ошибки трансформации
//                    }
//                }
//            }
//        }

//        private void TabsView_Loaded(object sender, RoutedEventArgs e)
//        {
//            if (_skipInitialTab)
//            {
//                return;
//            }

//            if (_tabsView.TabItems.Count == 0)
//            {
//                var initialTab = CreateNewTab("Root Page");
//                _tabsView.TabItems.Add(initialTab);
//                _tabsView.SelectedIndex = 0;
//            }
//        }

//        public TabViewItem CreateNewTab(string header)
//        {
//            var newTab = new TabViewItem
//            {
//                Header = header,
//                IconSource = new SymbolIconSource { Symbol = Symbol.Placeholder },
//                Tag = Guid.NewGuid().ToString()
//            };
//            return newTab;
//        }

//        private UIElement CreateTabContent(string dataContext = null)
//        {
//            var frame = new Frame();
//            frame.Navigate(typeof(rootPage), dataContext);
//            return frame;
//        }

//        private void TabsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            var selectedTab = _tabsView.SelectedItem as TabViewItem;

//            if (selectedTab != null)
//            {
//                if (!_tabContentMap.ContainsKey(selectedTab))
//                {
//                    var content = CreateTabContent();
//                    _tabContentMap[selectedTab] = content;
//                }

//                _contentFrame.Content = _tabContentMap[selectedTab];
//            }
//            else
//            {
//                _contentFrame.Content = null;
//            }
//        }

//        public void TabsView_OnAddTabButtonClick(TabView sender, object args)
//        {
//            var newTab = CreateNewTab("New Tab");
//            _tabsView.TabItems.Add(newTab);
//            _tabsView.SelectedItem = newTab;
//        }

//        public void TabsView_OnTabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
//        {
//            if (_tabContentMap.ContainsKey(args.Tab))
//            {
//                _tabContentMap.Remove(args.Tab);
//            }

//            sender.TabItems.Remove(args.Tab);

//            if (sender.TabItems.Count == 0)
//            {
//                _contentFrame.Content = null;
//            }
//        }

//        private void TabsView_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
//        {
//            var draggedTab = _tabsView.SelectedItem as TabViewItem;

//            if (draggedTab == null)
//            {
//                args.Cancel = true;
//                return;
//            }

//            string tabId = draggedTab.Tag?.ToString();
//            if (!string.IsNullOrEmpty(tabId))
//            {
//                _draggedTabId = tabId;
//                args.Data.Properties.Add(TabIdKey, tabId);
//            }
//            else
//            {
//                args.Cancel = true;
//                return;
//            }

//            args.Data.RequestedOperation = DataPackageOperation.Move;
//        }

//        private async void TabsView_TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
//        {
//            if (string.IsNullOrEmpty(_draggedTabId))
//            {
//                return;
//            }

//            string draggedTabId = _draggedTabId;
//            _draggedTabId = null;

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
//                return;
//            }

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

//            // Сохраняем контент для передачи
//            var contentToTransfer = sourceContent;
//            var dispatcherQueue = _tabsView.DispatcherQueue;

//            // Запускаем создание окна параллельно, не блокируя текущий UI
//            _ = Task.Run(() =>
//            {
//                // Создаём окно в фоновом потоке
//                dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () =>
//                {
//                    try
//                    {
//                        var newWindow = new MainWindow(true);
//                        newWindow.ExtendsContentIntoTitleBar = true;

//                        var newTab = newWindow.TabViewManager.CreateNewTab(savedHeader?.ToString() ?? "Tab");
//                        newTab.IconSource = savedIconSource;

//                        if (contentToTransfer != null)
//                        {
//                            // Прямая передача существующего контента без навигации
//                            newWindow.TabViewManager.TransferContentWithUIElement(contentToTransfer, newTab);
//                        }
//                        else
//                        {
//                            // Только если контента нет - создаём новый
//                            var frame = new Frame();
//                            frame.Navigate(typeof(rootPage), null);
//                            newWindow.TabViewManager.TransferContentWithUIElement(frame, newTab);
//                        }

//                        newWindow.MainTabsView.TabItems.Add(newTab);
//                        newWindow.TabViewManager._tabsView.SelectedItem = newTab;

//                        // Активируем окно
//                        newWindow.Activate();
//                    }
//                    catch
//                    {
//                        // Игнорируем ошибки создания окна
//                    }
//                });
//            });
//        }

//        public void TransferContentWithUIElement(UIElement content, TabViewItem targetTab)
//        {
//            _tabContentMap[targetTab] = content;
//        }

//        public int GetTabContentMapCount() => _tabContentMap.Count;

//        private void TabsView_TabStripDragOver(object sender, DragEventArgs e)
//        {
//            if (e.DataView.Properties.ContainsKey(TabIdKey))
//                e.AcceptedOperation = DataPackageOperation.Move;
//        }

//        private async void TabsView_TabStripDrop(object sender, DragEventArgs e)
//        {
//            if (!e.DataView.Properties.TryGetValue(TabIdKey, out object idObj))
//            {
//                return;
//            }
//            string draggedTabId = idObj.ToString();

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
//        }

//        public void TransferContent(TabViewItem sourceTab, TabViewItem targetTab)
//        {
//            if (_tabContentMap.TryGetValue(sourceTab, out var content))
//            {
//                _tabContentMap[targetTab] = content;
//                _tabContentMap.Remove(sourceTab);
//            }
//        }
//    }
//}


//ОРИГИНАЛЬНЫЙ

//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Threading.Tasks;
//using Microsoft.UI.Dispatching;
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

//        public TabViewManager(TabView tabsView, Frame contentFrame)
//            : this(tabsView, contentFrame, false)
//        {
//        }

//        public TabViewManager(TabView tabsView, Frame contentFrame, bool skipInitialTab)
//        {
//            var sw = Stopwatch.StartNew();
//            _tabsView = tabsView;
//            _contentFrame = contentFrame;
//            _skipInitialTab = skipInitialTab;
//            Initialize();
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] Constructor: {sw.ElapsedMilliseconds} ms");
//        }

//        private void Initialize()
//        {
//            var sw = Stopwatch.StartNew();
//            _tabsView.Loaded += TabsView_Loaded;
//            _tabsView.SelectionChanged += TabsView_SelectionChanged;
//            _tabsView.AddTabButtonClick += TabsView_OnAddTabButtonClick;
//            _tabsView.TabCloseRequested += TabsView_OnTabCloseRequested;
//            _tabsView.TabDragStarting += TabsView_TabDragStarting;
//            _tabsView.TabDroppedOutside += TabsView_TabDroppedOutside;
//            _tabsView.TabStripDragOver += TabsView_TabStripDragOver;
//            _tabsView.TabStripDrop += TabsView_TabStripDrop;
//            _tabsView.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(TabsView_PointerPressed), true);
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] Initialize: {sw.ElapsedMilliseconds} ms");
//        }

//        private void TabsView_PointerPressed(object sender, PointerRoutedEventArgs e)
//        {
//            var sw = Stopwatch.StartNew();
//            var tabView = sender as TabView;
//            if (tabView == null) { sw.Stop(); return; }

//            var pointerPoint = e.GetCurrentPoint(tabView);

//            if (!pointerPoint.Properties.IsLeftButtonPressed) { sw.Stop(); return; }

//            foreach (var item in tabView.TabItems)
//            {
//                var container = tabView.ContainerFromItem(item) as TabViewItem;
//                if (container != null)
//                {
//                    try
//                    {
//                        var transform = container.TransformToVisual(tabView);
//                        var bounds = transform.TransformBounds(new Windows.Foundation.Rect(0, 0, container.ActualWidth, container.ActualHeight));

//                        if (bounds.Contains(pointerPoint.Position))
//                        {
//                            if (tabView.SelectedItem != item)
//                            {
//                                tabView.SelectedItem = item;
//                            }
//                            break;
//                        }
//                    }
//                    catch
//                    {
//                        // Игнорируем ошибки трансформации
//                    }
//                }
//            }
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_PointerPressed: {sw.ElapsedMilliseconds} ms");
//        }

//        private void TabsView_Loaded(object sender, RoutedEventArgs e)
//        {
//            var sw = Stopwatch.StartNew();
//            if (_skipInitialTab)
//            {
//                sw.Stop();
//                Debug.WriteLine($"[TabViewManager] TabsView_Loaded (skipped): {sw.ElapsedMilliseconds} ms");
//                return;
//            }

//            if (_tabsView.TabItems.Count == 0)
//            {
//                var initialTab = CreateNewTab("Root Page");
//                _tabsView.TabItems.Add(initialTab);
//                _tabsView.SelectedIndex = 0;
//            }
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_Loaded: {sw.ElapsedMilliseconds} ms");
//        }

//        public TabViewItem CreateNewTab(string header)
//        {
//            var sw = Stopwatch.StartNew();
//            var newTab = new TabViewItem
//            {
//                Header = header,
//                IconSource = new SymbolIconSource { Symbol = Symbol.Placeholder },
//                Tag = Guid.NewGuid().ToString()
//            };
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] CreateNewTab: {sw.ElapsedMilliseconds} ms");
//            return newTab;
//        }

//        private UIElement CreateTabContent(string dataContext = null)
//        {
//            var sw = Stopwatch.StartNew();
//            var frame = new Frame();
//            frame.Navigate(typeof(rootPage), dataContext);
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] CreateTabContent: {sw.ElapsedMilliseconds} ms");
//            return frame;
//        }

//        private void TabsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            var sw = Stopwatch.StartNew();
//            var selectedTab = _tabsView.SelectedItem as TabViewItem;

//            if (selectedTab != null)
//            {
//                if (!_tabContentMap.ContainsKey(selectedTab))
//                {
//                    var content = CreateTabContent();
//                    _tabContentMap[selectedTab] = content;
//                }

//                _contentFrame.Content = _tabContentMap[selectedTab];
//            }
//            else
//            {
//                _contentFrame.Content = null;
//            }
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: {sw.ElapsedMilliseconds} ms");
//        }

//        public void TabsView_OnAddTabButtonClick(TabView sender, object args)
//        {
//            var sw = Stopwatch.StartNew();
//            var newTab = CreateNewTab("New Tab");
//            _tabsView.TabItems.Add(newTab);
//            _tabsView.SelectedItem = newTab;
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_OnAddTabButtonClick: {sw.ElapsedMilliseconds} ms");
//        }

//        public void TabsView_OnTabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
//        {
//            var sw = Stopwatch.StartNew();
//            if (_tabContentMap.ContainsKey(args.Tab))
//            {
//                _tabContentMap.Remove(args.Tab);
//            }

//            sender.TabItems.Remove(args.Tab);

//            if (sender.TabItems.Count == 0)
//            {
//                _contentFrame.Content = null;
//            }
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested: {sw.ElapsedMilliseconds} ms");
//        }

//        private void TabsView_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
//        {
//            var sw = Stopwatch.StartNew();
//            var draggedTab = _tabsView.SelectedItem as TabViewItem;

//            if (draggedTab == null)
//            {
//                args.Cancel = true;
//                sw.Stop();
//                Debug.WriteLine($"[TabViewManager] TabsView_TabDragStarting: {sw.ElapsedMilliseconds} ms");
//                return;
//            }

//            string tabId = draggedTab.Tag?.ToString();
//            if (!string.IsNullOrEmpty(tabId))
//            {
//                _draggedTabId = tabId;
//                args.Data.Properties.Add(TabIdKey, tabId);
//            }
//            else
//            {
//                args.Cancel = true;
//                sw.Stop();
//                Debug.WriteLine($"[TabViewManager] TabsView_TabDragStarting: {sw.ElapsedMilliseconds} ms");
//                return;
//            }

//            args.Data.RequestedOperation = DataPackageOperation.Move;
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_TabDragStarting: {sw.ElapsedMilliseconds} ms");
//        }

//        private async void TabsView_TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
//        {
//            var sw = Stopwatch.StartNew();
//            if (string.IsNullOrEmpty(_draggedTabId))
//            {
//                sw.Stop();
//                Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: {sw.ElapsedMilliseconds} ms");
//                return;
//            }

//            string draggedTabId = _draggedTabId;
//            _draggedTabId = null;

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
//                sw.Stop();
//                Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: {sw.ElapsedMilliseconds} ms");
//                return;
//            }

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

//            var contentToTransfer = sourceContent;
//            var dispatcherQueue = _tabsView.DispatcherQueue;

//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside (sync part): {sw.ElapsedMilliseconds} ms");

//            _ = Task.Run(() =>
//            {
//                dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () =>
//                {
//                    var sw2 = Stopwatch.StartNew();
//                    try
//                    {
//                        var newWindow = new MainWindow(true);
//                        newWindow.ExtendsContentIntoTitleBar = true;

//                        var newTab = newWindow.TabViewManager.CreateNewTab(savedHeader?.ToString() ?? "Tab");
//                        newTab.IconSource = savedIconSource;

//                        if (contentToTransfer != null)
//                        {
//                            newWindow.TabViewManager.TransferContentWithUIElement(contentToTransfer, newTab);
//                        }
//                        else
//                        {
//                            var frame = new Frame();
//                            frame.Navigate(typeof(rootPage), null);
//                            newWindow.TabViewManager.TransferContentWithUIElement(frame, newTab);
//                        }

//                        newWindow.MainTabsView.TabItems.Add(newTab);
//                        newWindow.TabViewManager._tabsView.SelectedItem = newTab;

//                        newWindow.Activate();
//                    }
//                    catch
//                    {
//                        // Игнорируем ошибки создания окна
//                    }
//                    finally
//                    {
//                        sw2.Stop();
//                        Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside (new window): {sw2.ElapsedMilliseconds} ms");
//                    }
//                });
//            });
//        }

//        public void TransferContentWithUIElement(UIElement content, TabViewItem targetTab)
//        {
//            var sw = Stopwatch.StartNew();
//            _tabContentMap[targetTab] = content;
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TransferContentWithUIElement: {sw.ElapsedMilliseconds} ms");
//        }

//        public int GetTabContentMapCount()
//        {
//            var sw = Stopwatch.StartNew();
//            int count = _tabContentMap.Count;
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] GetTabContentMapCount: {sw.ElapsedMilliseconds} ms");
//            return count;
//        }

//        private void TabsView_TabStripDragOver(object sender, DragEventArgs e)
//        {
//            var sw = Stopwatch.StartNew();
//            if (e.DataView.Properties.ContainsKey(TabIdKey))
//                e.AcceptedOperation = DataPackageOperation.Move;
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDragOver: {sw.ElapsedMilliseconds} ms");
//        }

//        private async void TabsView_TabStripDrop(object sender, DragEventArgs e)
//        {
//            var sw = Stopwatch.StartNew();
//            if (!e.DataView.Properties.TryGetValue(TabIdKey, out object idObj))
//            {
//                sw.Stop();
//                Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop: {sw.ElapsedMilliseconds} ms");
//                return;
//            }
//            string draggedTabId = idObj.ToString();

//            var destinationTabView = sender as TabView;
//            if (destinationTabView == null) { sw.Stop(); return; }

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

//            if (sourceTab == null) { sw.Stop(); return; }

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
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop: {sw.ElapsedMilliseconds} ms");
//        }

//        public void TransferContent(TabViewItem sourceTab, TabViewItem targetTab)
//        {
//            var sw = Stopwatch.StartNew();
//            if (_tabContentMap.TryGetValue(sourceTab, out var content))
//            {
//                _tabContentMap[targetTab] = content;
//                _tabContentMap.Remove(sourceTab);
//            }
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TransferContent: {sw.ElapsedMilliseconds} ms");
//        }
//    }
//}


//ПЕРВЫЙ ВАРИАНТ
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Threading.Tasks;
//using Microsoft.UI.Dispatching;
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

//        public TabViewManager(TabView tabsView, Frame contentFrame)
//            : this(tabsView, contentFrame, false)
//        {
//        }

//        public TabViewManager(TabView tabsView, Frame contentFrame, bool skipInitialTab)
//        {
//            var sw = Stopwatch.StartNew();
//            _tabsView = tabsView;
//            _contentFrame = contentFrame;
//            _skipInitialTab = skipInitialTab;
//            Initialize();
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] Constructor: {sw.ElapsedMilliseconds} ms");
//        }

//        private void Initialize()
//        {
//            var sw = Stopwatch.StartNew();
//            _tabsView.Loaded += TabsView_Loaded;
//            _tabsView.SelectionChanged += TabsView_SelectionChanged;
//            _tabsView.AddTabButtonClick += TabsView_OnAddTabButtonClick;
//            _tabsView.TabCloseRequested += TabsView_OnTabCloseRequested;
//            _tabsView.TabDragStarting += TabsView_TabDragStarting;
//            _tabsView.TabDroppedOutside += TabsView_TabDroppedOutside;
//            _tabsView.TabStripDragOver += TabsView_TabStripDragOver;
//            _tabsView.TabStripDrop += TabsView_TabStripDrop;
//            _tabsView.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(TabsView_PointerPressed), true);
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] Initialize: {sw.ElapsedMilliseconds} ms");
//        }

//        private void TabsView_PointerPressed(object sender, PointerRoutedEventArgs e)
//        {
//            var sw = Stopwatch.StartNew();
//            var tabView = sender as TabView;
//            if (tabView == null) { sw.Stop(); return; }

//            var pointerPoint = e.GetCurrentPoint(tabView);

//            if (!pointerPoint.Properties.IsLeftButtonPressed) { sw.Stop(); return; }

//            foreach (var item in tabView.TabItems)
//            {
//                var container = tabView.ContainerFromItem(item) as TabViewItem;
//                if (container != null)
//                {
//                    try
//                    {
//                        var transform = container.TransformToVisual(tabView);
//                        var bounds = transform.TransformBounds(new Windows.Foundation.Rect(0, 0, container.ActualWidth, container.ActualHeight));

//                        if (bounds.Contains(pointerPoint.Position))
//                        {
//                            if (tabView.SelectedItem != item)
//                            {
//                                tabView.SelectedItem = item;
//                            }
//                            break;
//                        }
//                    }
//                    catch { }
//                }
//            }
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_PointerPressed: {sw.ElapsedMilliseconds} ms");
//        }

//        private void TabsView_Loaded(object sender, RoutedEventArgs e)
//        {
//            var sw = Stopwatch.StartNew();
//            if (_skipInitialTab) { sw.Stop(); return; }

//            _tabsView.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
//            {
//                if (_tabsView.TabItems.Count == 0)
//                {
//                    var initialTab = CreateNewTab("Root Page");
//                    _tabsView.TabItems.Add(initialTab);
//                    _tabsView.SelectedIndex = 0;
//                }
//            });
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_Loaded: {sw.ElapsedMilliseconds} ms");
//        }

//        public TabViewItem CreateNewTab(string header)
//        {
//            var sw = Stopwatch.StartNew();
//            var newTab = new TabViewItem
//            {
//                Header = header,
//                IconSource = new SymbolIconSource { Symbol = Symbol.Placeholder },
//                Tag = Guid.NewGuid().ToString()
//            };
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] CreateNewTab: {sw.ElapsedMilliseconds} ms");
//            return newTab;
//        }

//        private UIElement CreateTabContent(string dataContext = null)
//        {
//            var sw = Stopwatch.StartNew();
//            var frame = new Frame();
//            frame.Navigate(typeof(rootPage), dataContext);
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] CreateTabContent: {sw.ElapsedMilliseconds} ms");
//            return frame;
//        }

//        private void TabsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            var sw = Stopwatch.StartNew();
//            var selectedTab = _tabsView.SelectedItem as TabViewItem;

//            if (selectedTab != null)
//            {
//                if (!_tabContentMap.ContainsKey(selectedTab))
//                {
//                    var content = CreateTabContent();
//                    _tabContentMap[selectedTab] = content;
//                }

//                _contentFrame.Content = _tabContentMap[selectedTab];
//            }
//            else
//            {
//                _contentFrame.Content = null;
//            }
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: {sw.ElapsedMilliseconds} ms");
//        }

//        public void TabsView_OnAddTabButtonClick(TabView sender, object args)
//        {
//            var sw = Stopwatch.StartNew();
//            var newTab = CreateNewTab("New Tab");
//            _tabsView.TabItems.Add(newTab);
//            _tabsView.SelectedItem = newTab;
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_OnAddTabButtonClick: {sw.ElapsedMilliseconds} ms");
//        }

//        public void TabsView_OnTabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
//        {
//            var sw = Stopwatch.StartNew();
//            if (_tabContentMap.ContainsKey(args.Tab))
//            {
//                _tabContentMap.Remove(args.Tab);
//            }

//            sender.TabItems.Remove(args.Tab);

//            if (sender.TabItems.Count == 0)
//            {
//                _contentFrame.Content = null;
//            }
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested: {sw.ElapsedMilliseconds} ms");
//        }

//        private void TabsView_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
//        {
//            var sw = Stopwatch.StartNew();
//            var draggedTab = _tabsView.SelectedItem as TabViewItem;

//            if (draggedTab == null)
//            {
//                args.Cancel = true;
//                sw.Stop();
//                Debug.WriteLine($"[TabViewManager] TabsView_TabDragStarting: {sw.ElapsedMilliseconds} ms");
//                return;
//            }

//            string tabId = draggedTab.Tag?.ToString();
//            if (!string.IsNullOrEmpty(tabId))
//            {
//                _draggedTabId = tabId;
//                args.Data.Properties.Add(TabIdKey, tabId);
//            }
//            else
//            {
//                args.Cancel = true;
//                sw.Stop();
//                Debug.WriteLine($"[TabViewManager] TabsView_TabDragStarting: {sw.ElapsedMilliseconds} ms");
//                return;
//            }

//            args.Data.RequestedOperation = DataPackageOperation.Move;
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_TabDragStarting: {sw.ElapsedMilliseconds} ms");
//        }

//        private async void TabsView_TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
//        {
//            var sw = Stopwatch.StartNew();
//            if (string.IsNullOrEmpty(_draggedTabId))
//            {
//                sw.Stop();
//                Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: {sw.ElapsedMilliseconds} ms");
//                return;
//            }

//            string draggedTabId = _draggedTabId;
//            _draggedTabId = null;

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
//                sw.Stop();
//                Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: {sw.ElapsedMilliseconds} ms");
//                return;
//            }

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

//            var contentToTransfer = sourceContent;
//            var dispatcherQueue = _tabsView.DispatcherQueue;

//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside (sync part): {sw.ElapsedMilliseconds} ms");

//            _ = Task.Run(() =>
//            {
//                dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () =>
//                {
//                    var sw2 = Stopwatch.StartNew();
//                    try
//                    {
//                        var newWindow = new MainWindow(true);
//                        newWindow.ExtendsContentIntoTitleBar = true;

//                        var newTab = newWindow.TabViewManager.CreateNewTab(savedHeader?.ToString() ?? "Tab");
//                        newTab.IconSource = savedIconSource;

//                        if (contentToTransfer != null)
//                        {
//                            newWindow.TabViewManager.TransferContentWithUIElement(contentToTransfer, newTab);
//                        }
//                        else
//                        {
//                            var frame = new Frame();
//                            frame.Navigate(typeof(rootPage), null);
//                            newWindow.TabViewManager.TransferContentWithUIElement(frame, newTab);
//                        }

//                        newWindow.MainTabsView.TabItems.Add(newTab);
//                        newWindow.TabViewManager._tabsView.SelectedItem = newTab;

//                        newWindow.Activate();
//                    }
//                    catch { }
//                    finally
//                    {
//                        sw2.Stop();
//                        Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside (new window): {sw2.ElapsedMilliseconds} ms");
//                    }
//                });
//            });
//        }

//        public void TransferContentWithUIElement(UIElement content, TabViewItem targetTab)
//        {
//            var sw = Stopwatch.StartNew();
//            _tabContentMap[targetTab] = content;
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TransferContentWithUIElement: {sw.ElapsedMilliseconds} ms");
//        }

//        public int GetTabContentMapCount()
//        {
//            var sw = Stopwatch.StartNew();
//            int count = _tabContentMap.Count;
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] GetTabContentMapCount: {sw.ElapsedMilliseconds} ms");
//            return count;
//        }

//        private void TabsView_TabStripDragOver(object sender, DragEventArgs e)
//        {
//            var sw = Stopwatch.StartNew();
//            if (e.DataView.Properties.ContainsKey(TabIdKey))
//                e.AcceptedOperation = DataPackageOperation.Move;
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDragOver: {sw.ElapsedMilliseconds} ms");
//        }

//        private async void TabsView_TabStripDrop(object sender, DragEventArgs e)
//        {
//            var sw = Stopwatch.StartNew();
//            if (!e.DataView.Properties.TryGetValue(TabIdKey, out object idObj))
//            {
//                sw.Stop();
//                Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop: {sw.ElapsedMilliseconds} ms");
//                return;
//            }
//            string draggedTabId = idObj.ToString();

//            var destinationTabView = sender as TabView;
//            if (destinationTabView == null) { sw.Stop(); return; }

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

//            if (sourceTab == null) { sw.Stop(); return; }

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
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop: {sw.ElapsedMilliseconds} ms");
//        }

//        public void TransferContent(TabViewItem sourceTab, TabViewItem targetTab)
//        {
//            var sw = Stopwatch.StartNew();
//            if (_tabContentMap.TryGetValue(sourceTab, out var content))
//            {
//                _tabContentMap[targetTab] = content;
//                _tabContentMap.Remove(sourceTab);
//            }
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TransferContent: {sw.ElapsedMilliseconds} ms");
//        }
//    }
//}

//ВТОРОЙ ВАРИАНТ

//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Threading.Tasks;
//using Microsoft.UI.Dispatching;
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

//        private Window _window;
//        private bool _initialTabCreated = false;

//        public TabViewManager(TabView tabsView, Frame contentFrame, Window window)
//            : this(tabsView, contentFrame, false, window)
//        {
//        }

//        public TabViewManager(TabView tabsView, Frame contentFrame, bool skipInitialTab, Window window = null)
//        {
//            var sw = Stopwatch.StartNew();
//            _tabsView = tabsView;
//            _contentFrame = contentFrame;
//            _skipInitialTab = skipInitialTab;
//            _window = window;
//            Initialize();
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] Constructor: {sw.ElapsedMilliseconds} ms");
//        }

//        private void Initialize()
//        {
//            var sw = Stopwatch.StartNew();
//            _tabsView.Loaded += TabsView_Loaded;
//            _tabsView.SelectionChanged += TabsView_SelectionChanged;
//            _tabsView.AddTabButtonClick += TabsView_OnAddTabButtonClick;
//            _tabsView.TabCloseRequested += TabsView_OnTabCloseRequested;
//            _tabsView.TabDragStarting += TabsView_TabDragStarting;
//            _tabsView.TabDroppedOutside += TabsView_TabDroppedOutside;
//            _tabsView.TabStripDragOver += TabsView_TabStripDragOver;
//            _tabsView.TabStripDrop += TabsView_TabStripDrop;
//            _tabsView.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(TabsView_PointerPressed), true);
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] Initialize: {sw.ElapsedMilliseconds} ms");
//        }

//        private void TabsView_PointerPressed(object sender, PointerRoutedEventArgs e)
//        {
//            var sw = Stopwatch.StartNew();
//            var tabView = sender as TabView;
//            if (tabView == null) { sw.Stop(); return; }

//            var pointerPoint = e.GetCurrentPoint(tabView);
//            if (!pointerPoint.Properties.IsLeftButtonPressed) { sw.Stop(); return; }

//            foreach (var item in tabView.TabItems)
//            {
//                var container = tabView.ContainerFromItem(item) as TabViewItem;
//                if (container != null)
//                {
//                    try
//                    {
//                        var transform = container.TransformToVisual(tabView);
//                        var bounds = transform.TransformBounds(new Windows.Foundation.Rect(0, 0, container.ActualWidth, container.ActualHeight));
//                        if (bounds.Contains(pointerPoint.Position))
//                        {
//                            if (tabView.SelectedItem != item)
//                                tabView.SelectedItem = item;
//                            break;
//                        }
//                    }
//                    catch { }
//                }
//            }
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_PointerPressed: {sw.ElapsedMilliseconds} ms");
//        }

//        private void TabsView_Loaded(object sender, RoutedEventArgs e)
//        {
//            var sw = Stopwatch.StartNew();
//            if (_skipInitialTab) { sw.Stop(); return; }

//            // Используем WindowHelper для получения окна, если не передали явно
//            if (_window == null)
//                _window = WindowHelper.GetWindowForElement(_tabsView) as Window;

//            if (_window != null)
//                _window.Activated += Window_Activated;
//            else
//                CreateInitialTabIfNeeded(); // fallback
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_Loaded: {sw.ElapsedMilliseconds} ms");
//        }

//        private void Window_Activated(object sender, WindowActivatedEventArgs args)
//        {
//            var sw = Stopwatch.StartNew();
//            if (!_initialTabCreated && args.WindowActivationState != WindowActivationState.Deactivated)
//            {
//                _initialTabCreated = true;
//                CreateInitialTabIfNeeded();
//                if (_window != null)
//                    _window.Activated -= Window_Activated;
//            }
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] Window_Activated: {sw.ElapsedMilliseconds} ms");
//        }

//        private void CreateInitialTabIfNeeded()
//        {
//            var sw = Stopwatch.StartNew();
//            if (_tabsView.TabItems.Count == 0)
//            {
//                var initialTab = CreateNewTab("Root Page");
//                _tabsView.TabItems.Add(initialTab);
//                _tabsView.SelectedIndex = 0;
//            }
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] CreateInitialTabIfNeeded: {sw.ElapsedMilliseconds} ms");
//        }

//        public TabViewItem CreateNewTab(string header)
//        {
//            var sw = Stopwatch.StartNew();
//            var newTab = new TabViewItem
//            {
//                Header = header,
//                IconSource = new SymbolIconSource { Symbol = Symbol.Placeholder },
//                Tag = Guid.NewGuid().ToString()
//            };
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] CreateNewTab: {sw.ElapsedMilliseconds} ms");
//            return newTab;
//        }

//        private UIElement CreateTabContent(string dataContext = null)
//        {
//            var sw = Stopwatch.StartNew();
//            var frame = new Frame();
//            frame.Navigate(typeof(rootPage), dataContext);
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] CreateTabContent: {sw.ElapsedMilliseconds} ms");
//            return frame;
//        }

//        private void TabsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            var sw = Stopwatch.StartNew();
//            var selectedTab = _tabsView.SelectedItem as TabViewItem;
//            if (selectedTab != null)
//            {
//                if (!_tabContentMap.ContainsKey(selectedTab))
//                {
//                    var content = CreateTabContent();
//                    _tabContentMap[selectedTab] = content;
//                }
//                _contentFrame.Content = _tabContentMap[selectedTab];
//            }
//            else
//            {
//                _contentFrame.Content = null;
//            }
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: {sw.ElapsedMilliseconds} ms");
//        }

//        public void TabsView_OnAddTabButtonClick(TabView sender, object args)
//        {
//            var sw = Stopwatch.StartNew();
//            var newTab = CreateNewTab("New Tab");
//            _tabsView.TabItems.Add(newTab);
//            _tabsView.SelectedItem = newTab;
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_OnAddTabButtonClick: {sw.ElapsedMilliseconds} ms");
//        }

//        public void TabsView_OnTabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
//        {
//            var sw = Stopwatch.StartNew();
//            if (_tabContentMap.ContainsKey(args.Tab))
//                _tabContentMap.Remove(args.Tab);
//            sender.TabItems.Remove(args.Tab);
//            if (sender.TabItems.Count == 0)
//                _contentFrame.Content = null;
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested: {sw.ElapsedMilliseconds} ms");
//        }

//        private void TabsView_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
//        {
//            var sw = Stopwatch.StartNew();
//            var draggedTab = _tabsView.SelectedItem as TabViewItem;
//            if (draggedTab == null)
//            {
//                args.Cancel = true;
//                sw.Stop();
//                Debug.WriteLine($"[TabViewManager] TabsView_TabDragStarting: {sw.ElapsedMilliseconds} ms");
//                return;
//            }
//            string tabId = draggedTab.Tag?.ToString();
//            if (!string.IsNullOrEmpty(tabId))
//            {
//                _draggedTabId = tabId;
//                args.Data.Properties.Add(TabIdKey, tabId);
//            }
//            else
//            {
//                args.Cancel = true;
//                sw.Stop();
//                Debug.WriteLine($"[TabViewManager] TabsView_TabDragStarting: {sw.ElapsedMilliseconds} ms");
//                return;
//            }
//            args.Data.RequestedOperation = DataPackageOperation.Move;
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_TabDragStarting: {sw.ElapsedMilliseconds} ms");
//        }

//        private async void TabsView_TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
//        {
//            var sw = Stopwatch.StartNew();
//            if (string.IsNullOrEmpty(_draggedTabId))
//            {
//                sw.Stop();
//                Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: {sw.ElapsedMilliseconds} ms");
//                return;
//            }
//            string draggedTabId = _draggedTabId;
//            _draggedTabId = null;

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
//                sw.Stop();
//                Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: {sw.ElapsedMilliseconds} ms");
//                return;
//            }

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

//            var contentToTransfer = sourceContent;
//            var dispatcherQueue = _tabsView.DispatcherQueue;

//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside (sync part): {sw.ElapsedMilliseconds} ms");

//            _ = Task.Run(() =>
//            {
//                dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () =>
//                {
//                    var sw2 = Stopwatch.StartNew();
//                    try
//                    {
//                        var newWindow = new MainWindow(true);
//                        newWindow.ExtendsContentIntoTitleBar = true;

//                        var newTab = newWindow.TabViewManager.CreateNewTab(savedHeader?.ToString() ?? "Tab");
//                        newTab.IconSource = savedIconSource;

//                        if (contentToTransfer != null)
//                            newWindow.TabViewManager.TransferContentWithUIElement(contentToTransfer, newTab);
//                        else
//                        {
//                            var frame = new Frame();
//                            frame.Navigate(typeof(rootPage), null);
//                            newWindow.TabViewManager.TransferContentWithUIElement(frame, newTab);
//                        }

//                        newWindow.MainTabsView.TabItems.Add(newTab);
//                        newWindow.TabViewManager._tabsView.SelectedItem = newTab;
//                        newWindow.Activate();
//                    }
//                    catch { }
//                    finally
//                    {
//                        sw2.Stop();
//                        Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside (new window): {sw2.ElapsedMilliseconds} ms");
//                    }
//                });
//            });
//        }

//        public void TransferContentWithUIElement(UIElement content, TabViewItem targetTab)
//        {
//            var sw = Stopwatch.StartNew();
//            _tabContentMap[targetTab] = content;
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TransferContentWithUIElement: {sw.ElapsedMilliseconds} ms");
//        }

//        public int GetTabContentMapCount()
//        {
//            var sw = Stopwatch.StartNew();
//            int count = _tabContentMap.Count;
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] GetTabContentMapCount: {sw.ElapsedMilliseconds} ms");
//            return count;
//        }

//        private void TabsView_TabStripDragOver(object sender, DragEventArgs e)
//        {
//            var sw = Stopwatch.StartNew();
//            if (e.DataView.Properties.ContainsKey(TabIdKey))
//                e.AcceptedOperation = DataPackageOperation.Move;
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDragOver: {sw.ElapsedMilliseconds} ms");
//        }

//        private async void TabsView_TabStripDrop(object sender, DragEventArgs e)
//        {
//            var sw = Stopwatch.StartNew();
//            if (!e.DataView.Properties.TryGetValue(TabIdKey, out object idObj))
//            {
//                sw.Stop();
//                Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop: {sw.ElapsedMilliseconds} ms");
//                return;
//            }
//            string draggedTabId = idObj.ToString();
//            var destinationTabView = sender as TabView;
//            if (destinationTabView == null) { sw.Stop(); return; }

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
//            if (sourceTab == null) { sw.Stop(); return; }

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
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop: {sw.ElapsedMilliseconds} ms");
//        }

//        public void TransferContent(TabViewItem sourceTab, TabViewItem targetTab)
//        {
//            var sw = Stopwatch.StartNew();
//            if (_tabContentMap.TryGetValue(sourceTab, out var content))
//            {
//                _tabContentMap[targetTab] = content;
//                _tabContentMap.Remove(sourceTab);
//            }
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TransferContent: {sw.ElapsedMilliseconds} ms");
//        }
//    }
//}

//ТРЕТИЙ ВАРИАНТ

//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Threading.Tasks;
//using Microsoft.UI.Dispatching;
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

//        private UIElement _preCreatedContent;

//        public TabViewManager(TabView tabsView, Frame contentFrame)
//            : this(tabsView, contentFrame, false)
//        {
//        }

//        public TabViewManager(TabView tabsView, Frame contentFrame, bool skipInitialTab)
//        {
//            var sw = Stopwatch.StartNew();
//            _tabsView = tabsView;
//            _contentFrame = contentFrame;
//            _skipInitialTab = skipInitialTab;
//            Initialize();
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] Constructor: {sw.ElapsedMilliseconds} ms");
//        }

//        private void Initialize()
//        {
//            var sw = Stopwatch.StartNew();
//            _tabsView.Loaded += TabsView_Loaded;
//            _tabsView.SelectionChanged += TabsView_SelectionChanged;
//            _tabsView.AddTabButtonClick += TabsView_OnAddTabButtonClick;
//            _tabsView.TabCloseRequested += TabsView_OnTabCloseRequested;
//            _tabsView.TabDragStarting += TabsView_TabDragStarting;
//            _tabsView.TabDroppedOutside += TabsView_TabDroppedOutside;
//            _tabsView.TabStripDragOver += TabsView_TabStripDragOver;
//            _tabsView.TabStripDrop += TabsView_TabStripDrop;
//            _tabsView.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(TabsView_PointerPressed), true);
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] Initialize: {sw.ElapsedMilliseconds} ms");
//        }

//        private void TabsView_PointerPressed(object sender, PointerRoutedEventArgs e)
//        {
//            var sw = Stopwatch.StartNew();
//            var tabView = sender as TabView;
//            if (tabView == null) { sw.Stop(); return; }

//            var pointerPoint = e.GetCurrentPoint(tabView);

//            if (!pointerPoint.Properties.IsLeftButtonPressed) { sw.Stop(); return; }

//            foreach (var item in tabView.TabItems)
//            {
//                var container = tabView.ContainerFromItem(item) as TabViewItem;
//                if (container != null)
//                {
//                    try
//                    {
//                        var transform = container.TransformToVisual(tabView);
//                        var bounds = transform.TransformBounds(new Windows.Foundation.Rect(0, 0, container.ActualWidth, container.ActualHeight));

//                        if (bounds.Contains(pointerPoint.Position))
//                        {
//                            if (tabView.SelectedItem != item)
//                            {
//                                tabView.SelectedItem = item;
//                            }
//                            break;
//                        }
//                    }
//                    catch { }
//                }
//            }
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_PointerPressed: {sw.ElapsedMilliseconds} ms");
//        }

//        private void TabsView_Loaded(object sender, RoutedEventArgs e)
//        {
//            var sw = Stopwatch.StartNew();
//            if (_skipInitialTab) { sw.Stop(); return; }

//            _preCreatedContent = CreateTabContent();

//            _tabsView.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
//            {
//                if (_tabsView.TabItems.Count == 0)
//                {
//                    var initialTab = CreateNewTab("Root Page");
//                    _tabsView.TabItems.Add(initialTab);
//                    _tabContentMap[initialTab] = _preCreatedContent;
//                    _tabsView.SelectedIndex = 0;
//                }
//                _preCreatedContent = null;
//            });
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_Loaded: {sw.ElapsedMilliseconds} ms");
//        }

//        public TabViewItem CreateNewTab(string header)
//        {
//            var sw = Stopwatch.StartNew();
//            var newTab = new TabViewItem
//            {
//                Header = header,
//                IconSource = new SymbolIconSource { Symbol = Symbol.Placeholder },
//                Tag = Guid.NewGuid().ToString()
//            };
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] CreateNewTab: {sw.ElapsedMilliseconds} ms");
//            return newTab;
//        }

//        private UIElement CreateTabContent(string dataContext = null)
//        {
//            var sw = Stopwatch.StartNew();
//            var frame = new Frame();
//            frame.Navigate(typeof(rootPage), dataContext);
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] CreateTabContent: {sw.ElapsedMilliseconds} ms");
//            return frame;
//        }

//        private void TabsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
//        {
//            var sw = Stopwatch.StartNew();
//            var selectedTab = _tabsView.SelectedItem as TabViewItem;

//            if (selectedTab != null)
//            {
//                if (!_tabContentMap.ContainsKey(selectedTab))
//                {
//                    var content = CreateTabContent();
//                    _tabContentMap[selectedTab] = content;
//                }

//                _contentFrame.Content = _tabContentMap[selectedTab];
//            }
//            else
//            {
//                _contentFrame.Content = null;
//            }
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: {sw.ElapsedMilliseconds} ms");
//        }

//        public void TabsView_OnAddTabButtonClick(TabView sender, object args)
//        {
//            var sw = Stopwatch.StartNew();
//            var newTab = CreateNewTab("New Tab");
//            _tabsView.TabItems.Add(newTab);
//            _tabsView.SelectedItem = newTab;
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_OnAddTabButtonClick: {sw.ElapsedMilliseconds} ms");
//        }

//        public void TabsView_OnTabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
//        {
//            var sw = Stopwatch.StartNew();
//            if (_tabContentMap.ContainsKey(args.Tab))
//            {
//                _tabContentMap.Remove(args.Tab);
//            }

//            sender.TabItems.Remove(args.Tab);

//            if (sender.TabItems.Count == 0)
//            {
//                _contentFrame.Content = null;
//            }
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested: {sw.ElapsedMilliseconds} ms");
//        }

//        private void TabsView_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
//        {
//            var sw = Stopwatch.StartNew();
//            var draggedTab = _tabsView.SelectedItem as TabViewItem;

//            if (draggedTab == null)
//            {
//                args.Cancel = true;
//                sw.Stop();
//                Debug.WriteLine($"[TabViewManager] TabsView_TabDragStarting: {sw.ElapsedMilliseconds} ms");
//                return;
//            }

//            string tabId = draggedTab.Tag?.ToString();
//            if (!string.IsNullOrEmpty(tabId))
//            {
//                _draggedTabId = tabId;
//                args.Data.Properties.Add(TabIdKey, tabId);
//            }
//            else
//            {
//                args.Cancel = true;
//                sw.Stop();
//                Debug.WriteLine($"[TabViewManager] TabsView_TabDragStarting: {sw.ElapsedMilliseconds} ms");
//                return;
//            }

//            args.Data.RequestedOperation = DataPackageOperation.Move;
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_TabDragStarting: {sw.ElapsedMilliseconds} ms");
//        }

//        private async void TabsView_TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
//        {
//            var sw = Stopwatch.StartNew();
//            if (string.IsNullOrEmpty(_draggedTabId))
//            {
//                sw.Stop();
//                Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: {sw.ElapsedMilliseconds} ms");
//                return;
//            }

//            string draggedTabId = _draggedTabId;
//            _draggedTabId = null;

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
//                sw.Stop();
//                Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: {sw.ElapsedMilliseconds} ms");
//                return;
//            }

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

//            var contentToTransfer = sourceContent;
//            var dispatcherQueue = _tabsView.DispatcherQueue;

//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside (sync part): {sw.ElapsedMilliseconds} ms");

//            _ = Task.Run(() =>
//            {
//                dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () =>
//                {
//                    var sw2 = Stopwatch.StartNew();
//                    try
//                    {
//                        var newWindow = new MainWindow(true);
//                        newWindow.ExtendsContentIntoTitleBar = true;

//                        var newTab = newWindow.TabViewManager.CreateNewTab(savedHeader?.ToString() ?? "Tab");
//                        newTab.IconSource = savedIconSource;

//                        if (contentToTransfer != null)
//                        {
//                            newWindow.TabViewManager.TransferContentWithUIElement(contentToTransfer, newTab);
//                        }
//                        else
//                        {
//                            var frame = new Frame();
//                            frame.Navigate(typeof(rootPage), null);
//                            newWindow.TabViewManager.TransferContentWithUIElement(frame, newTab);
//                        }

//                        newWindow.MainTabsView.TabItems.Add(newTab);
//                        newWindow.TabViewManager._tabsView.SelectedItem = newTab;

//                        newWindow.Activate();
//                    }
//                    catch { }
//                    finally
//                    {
//                        sw2.Stop();
//                        Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside (new window): {sw2.ElapsedMilliseconds} ms");
//                    }
//                });
//            });
//        }

//        public void TransferContentWithUIElement(UIElement content, TabViewItem targetTab)
//        {
//            var sw = Stopwatch.StartNew();
//            _tabContentMap[targetTab] = content;
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TransferContentWithUIElement: {sw.ElapsedMilliseconds} ms");
//        }

//        public int GetTabContentMapCount()
//        {
//            var sw = Stopwatch.StartNew();
//            int count = _tabContentMap.Count;
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] GetTabContentMapCount: {sw.ElapsedMilliseconds} ms");
//            return count;
//        }

//        private void TabsView_TabStripDragOver(object sender, DragEventArgs e)
//        {
//            var sw = Stopwatch.StartNew();
//            if (e.DataView.Properties.ContainsKey(TabIdKey))
//                e.AcceptedOperation = DataPackageOperation.Move;
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDragOver: {sw.ElapsedMilliseconds} ms");
//        }

//        private async void TabsView_TabStripDrop(object sender, DragEventArgs e)
//        {
//            var sw = Stopwatch.StartNew();
//            if (!e.DataView.Properties.TryGetValue(TabIdKey, out object idObj))
//            {
//                sw.Stop();
//                Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop: {sw.ElapsedMilliseconds} ms");
//                return;
//            }
//            string draggedTabId = idObj.ToString();

//            var destinationTabView = sender as TabView;
//            if (destinationTabView == null) { sw.Stop(); return; }

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

//            if (sourceTab == null) { sw.Stop(); return; }

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
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop: {sw.ElapsedMilliseconds} ms");
//        }

//        public void TransferContent(TabViewItem sourceTab, TabViewItem targetTab)
//        {
//            var sw = Stopwatch.StartNew();
//            if (_tabContentMap.TryGetValue(sourceTab, out var content))
//            {
//                _tabContentMap[targetTab] = content;
//                _tabContentMap.Remove(sourceTab);
//            }
//            sw.Stop();
//            Debug.WriteLine($"[TabViewManager] TransferContent: {sw.ElapsedMilliseconds} ms");
//        }
//    }
//}


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using ufm.Pages;

namespace ufm
{
    public class TabViewManager
    {
        private TabView _tabsView;
        private Frame _contentFrame;
        private const string TabIdKey = "TabId";
        private readonly Dictionary<TabViewItem, UIElement> _tabContentMap = new();
        private readonly bool _skipInitialTab;
        private string _draggedTabId;

        public TabViewManager(TabView tabsView, Frame contentFrame)
            : this(tabsView, contentFrame, false)
        {
        }

        public TabViewManager(TabView tabsView, Frame contentFrame, bool skipInitialTab)
        {
            var sw = Stopwatch.StartNew();
            _tabsView = tabsView;
            _contentFrame = contentFrame;
            _skipInitialTab = skipInitialTab;
            Initialize();
            sw.Stop();
            Debug.WriteLine($"[TabViewManager] Constructor: {sw.ElapsedMilliseconds} ms");
        }

        private void Initialize()
        {
            var sw = Stopwatch.StartNew();
            _tabsView.Loaded += TabsView_Loaded;
            _tabsView.SelectionChanged += TabsView_SelectionChanged;
            _tabsView.AddTabButtonClick += TabsView_OnAddTabButtonClick;
            _tabsView.TabCloseRequested += TabsView_OnTabCloseRequested;
            _tabsView.TabDragStarting += TabsView_TabDragStarting;
            _tabsView.TabDroppedOutside += TabsView_TabDroppedOutside;
            _tabsView.TabStripDragOver += TabsView_TabStripDragOver;
            _tabsView.TabStripDrop += TabsView_TabStripDrop;
            _tabsView.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(TabsView_PointerPressed), true);
            sw.Stop();
            Debug.WriteLine($"[TabViewManager] Initialize: {sw.ElapsedMilliseconds} ms");
        }

        private void TabsView_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var sw = Stopwatch.StartNew();
            var tabView = sender as TabView;
            if (tabView == null) { sw.Stop(); return; }

            var pointerPoint = e.GetCurrentPoint(tabView);
            if (!pointerPoint.Properties.IsLeftButtonPressed) { sw.Stop(); return; }

            foreach (var item in tabView.TabItems)
            {
                var container = tabView.ContainerFromItem(item) as TabViewItem;
                if (container != null)
                {
                    try
                    {
                        var transform = container.TransformToVisual(tabView);
                        var bounds = transform.TransformBounds(new Windows.Foundation.Rect(0, 0, container.ActualWidth, container.ActualHeight));
                        if (bounds.Contains(pointerPoint.Position))
                        {
                            if (tabView.SelectedItem != item)
                                tabView.SelectedItem = item;
                            break;
                        }
                    }
                    catch { }
                }
            }
            sw.Stop();
            Debug.WriteLine($"[TabViewManager] TabsView_PointerPressed: {sw.ElapsedMilliseconds} ms");
        }

        // ========== ВАРИАНТ 1 (оптимальный) ==========
        private void TabsView_Loaded(object sender, RoutedEventArgs e)
        {
            var sw = Stopwatch.StartNew();
            if (_skipInitialTab) { sw.Stop(); return; }

            // Откладываем добавление вкладки с низким приоритетом,
            // чтобы окно успело отрисоваться
            _tabsView.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                if (_tabsView.TabItems.Count == 0)
                {
                    var initialTab = CreateNewTab("Root Page");
                    _tabsView.TabItems.Add(initialTab);
                    _tabsView.SelectedIndex = 0;
                }
            });
            sw.Stop();
            Debug.WriteLine($"[TabViewManager] TabsView_Loaded: {sw.ElapsedMilliseconds} ms");
        }

        public TabViewItem CreateNewTab(string header)
        {
            var sw = Stopwatch.StartNew();
            var newTab = new TabViewItem
            {
                Header = header,
                IconSource = new SymbolIconSource { Symbol = Symbol.Placeholder },
                Tag = Guid.NewGuid().ToString()
            };
            sw.Stop();
            Debug.WriteLine($"[TabViewManager] CreateNewTab: {sw.ElapsedMilliseconds} ms");
            return newTab;
        }

        private UIElement CreateTabContent(string dataContext = null)
        {
            var sw = Stopwatch.StartNew();
            var frame = new Frame();
            frame.Navigate(typeof(rootPage), dataContext);
            sw.Stop();
            Debug.WriteLine($"[TabViewManager] CreateTabContent: {sw.ElapsedMilliseconds} ms");
            return frame;
        }

        private void TabsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var sw = Stopwatch.StartNew();
            var selectedTab = _tabsView.SelectedItem as TabViewItem;

            if (selectedTab != null)
            {
                if (!_tabContentMap.ContainsKey(selectedTab))
                {
                    var content = CreateTabContent();
                    _tabContentMap[selectedTab] = content;
                }

                _contentFrame.Content = _tabContentMap[selectedTab];
            }
            else
            {
                _contentFrame.Content = null;
            }
            sw.Stop();
            Debug.WriteLine($"[TabViewManager] TabsView_SelectionChanged: {sw.ElapsedMilliseconds} ms");
        }

        public void TabsView_OnAddTabButtonClick(TabView sender, object args)
        {
            var sw = Stopwatch.StartNew();
            var newTab = CreateNewTab("New Tab");
            _tabsView.TabItems.Add(newTab);
            _tabsView.SelectedItem = newTab;
            sw.Stop();
            Debug.WriteLine($"[TabViewManager] TabsView_OnAddTabButtonClick: {sw.ElapsedMilliseconds} ms");
        }

        public void TabsView_OnTabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            var sw = Stopwatch.StartNew();
            if (_tabContentMap.ContainsKey(args.Tab))
            {
                _tabContentMap.Remove(args.Tab);
            }

            sender.TabItems.Remove(args.Tab);

            if (sender.TabItems.Count == 0)
            {
                _contentFrame.Content = null;
            }
            sw.Stop();
            Debug.WriteLine($"[TabViewManager] TabsView_OnTabCloseRequested: {sw.ElapsedMilliseconds} ms");
        }

        private void TabsView_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
        {
            var sw = Stopwatch.StartNew();
            var draggedTab = _tabsView.SelectedItem as TabViewItem;

            if (draggedTab == null)
            {
                args.Cancel = true;
                sw.Stop();
                Debug.WriteLine($"[TabViewManager] TabsView_TabDragStarting: {sw.ElapsedMilliseconds} ms");
                return;
            }

            string tabId = draggedTab.Tag?.ToString();
            if (!string.IsNullOrEmpty(tabId))
            {
                _draggedTabId = tabId;
                args.Data.Properties.Add(TabIdKey, tabId);
            }
            else
            {
                args.Cancel = true;
                sw.Stop();
                Debug.WriteLine($"[TabViewManager] TabsView_TabDragStarting: {sw.ElapsedMilliseconds} ms");
                return;
            }

            args.Data.RequestedOperation = DataPackageOperation.Move;
            sw.Stop();
            Debug.WriteLine($"[TabViewManager] TabsView_TabDragStarting: {sw.ElapsedMilliseconds} ms");
        }

        private async void TabsView_TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
        {
            var sw = Stopwatch.StartNew();
            if (string.IsNullOrEmpty(_draggedTabId))
            {
                sw.Stop();
                Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: {sw.ElapsedMilliseconds} ms");
                return;
            }

            string draggedTabId = _draggedTabId;
            _draggedTabId = null;

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
                sw.Stop();
                Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: {sw.ElapsedMilliseconds} ms");
                return;
            }

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

            var contentToTransfer = sourceContent;
            var dispatcherQueue = _tabsView.DispatcherQueue;

            sw.Stop();
            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside (sync part): {sw.ElapsedMilliseconds} ms");

            _ = Task.Run(() =>
            {
                dispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () =>
                {
                    var sw2 = Stopwatch.StartNew();
                    try
                    {
                        var newWindow = new MainWindow(true);
                        newWindow.ExtendsContentIntoTitleBar = true;

                        var newTab = newWindow.TabViewManager.CreateNewTab(savedHeader?.ToString() ?? "Tab");
                        newTab.IconSource = savedIconSource;

                        if (contentToTransfer != null)
                        {
                            newWindow.TabViewManager.TransferContentWithUIElement(contentToTransfer, newTab);
                        }
                        else
                        {
                            var frame = new Frame();
                            frame.Navigate(typeof(rootPage), null);
                            newWindow.TabViewManager.TransferContentWithUIElement(frame, newTab);
                        }

                        newWindow.MainTabsView.TabItems.Add(newTab);
                        newWindow.TabViewManager._tabsView.SelectedItem = newTab;

                        newWindow.Activate();
                    }
                    catch { }
                    finally
                    {
                        sw2.Stop();
                        Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside (new window): {sw2.ElapsedMilliseconds} ms");
                    }
                });
            });
        }

        public void TransferContentWithUIElement(UIElement content, TabViewItem targetTab)
        {
            var sw = Stopwatch.StartNew();
            _tabContentMap[targetTab] = content;
            sw.Stop();
            Debug.WriteLine($"[TabViewManager] TransferContentWithUIElement: {sw.ElapsedMilliseconds} ms");
        }

        public int GetTabContentMapCount()
        {
            var sw = Stopwatch.StartNew();
            int count = _tabContentMap.Count;
            sw.Stop();
            Debug.WriteLine($"[TabViewManager] GetTabContentMapCount: {sw.ElapsedMilliseconds} ms");
            return count;
        }

        private void TabsView_TabStripDragOver(object sender, DragEventArgs e)
        {
            var sw = Stopwatch.StartNew();
            if (e.DataView.Properties.ContainsKey(TabIdKey))
                e.AcceptedOperation = DataPackageOperation.Move;
            sw.Stop();
            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDragOver: {sw.ElapsedMilliseconds} ms");
        }

        private async void TabsView_TabStripDrop(object sender, DragEventArgs e)
        {
            var sw = Stopwatch.StartNew();
            if (!e.DataView.Properties.TryGetValue(TabIdKey, out object idObj))
            {
                sw.Stop();
                Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop: {sw.ElapsedMilliseconds} ms");
                return;
            }
            string draggedTabId = idObj.ToString();

            var destinationTabView = sender as TabView;
            if (destinationTabView == null) { sw.Stop(); return; }

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

            if (sourceTab == null) { sw.Stop(); return; }

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
            sw.Stop();
            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop: {sw.ElapsedMilliseconds} ms");
        }

        public void TransferContent(TabViewItem sourceTab, TabViewItem targetTab)
        {
            var sw = Stopwatch.StartNew();
            if (_tabContentMap.TryGetValue(sourceTab, out var content))
            {
                _tabContentMap[targetTab] = content;
                _tabContentMap.Remove(sourceTab);
            }
            sw.Stop();
            Debug.WriteLine($"[TabViewManager] TransferContent: {sw.ElapsedMilliseconds} ms");
        }
    }
}