using System;
using System.Collections.Generic;
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
            _tabsView = tabsView;
            _contentFrame = contentFrame;
            _skipInitialTab = skipInitialTab;
            Initialize();
        }

        private void Initialize()
        {
            _tabsView.Loaded += TabsView_Loaded;
            _tabsView.SelectionChanged += TabsView_SelectionChanged;
            _tabsView.AddTabButtonClick += TabsView_OnAddTabButtonClick;
            _tabsView.TabCloseRequested += TabsView_OnTabCloseRequested;
            _tabsView.TabDragStarting += TabsView_TabDragStarting;
            _tabsView.TabDroppedOutside += TabsView_TabDroppedOutside;
            _tabsView.TabStripDragOver += TabsView_TabStripDragOver;
            _tabsView.TabStripDrop += TabsView_TabStripDrop;
            _tabsView.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(TabsView_PointerPressed), true);
        }

        private void TabsView_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var tabView = sender as TabView;
            if (tabView == null) return;

            var pointerPoint = e.GetCurrentPoint(tabView);

            if (!pointerPoint.Properties.IsLeftButtonPressed)
                return;

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
                            {
                                tabView.SelectedItem = item;
                            }
                            break;
                        }
                    }
                    catch
                    {
                        // Игнорируем ошибки трансформации
                    }
                }
            }
        }

        private void TabsView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_skipInitialTab)
            {
                return;
            }

            if (_tabsView.TabItems.Count == 0)
            {
                var initialTab = CreateNewTab("Root Page");
                _tabsView.TabItems.Add(initialTab);
                _tabsView.SelectedIndex = 0;
            }
        }

        public TabViewItem CreateNewTab(string header)
        {
            var newTab = new TabViewItem
            {
                Header = header,
                IconSource = new SymbolIconSource { Symbol = Symbol.Placeholder },
                Tag = Guid.NewGuid().ToString()
            };
            return newTab;
        }

        private UIElement CreateTabContent(string dataContext = null)
        {
            var frame = new Frame();
            frame.Navigate(typeof(rootPage), dataContext);
            return frame;
        }

        private void TabsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
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
        }

        public void TabsView_OnAddTabButtonClick(TabView sender, object args)
        {
            var newTab = CreateNewTab("New Tab");
            _tabsView.TabItems.Add(newTab);
            _tabsView.SelectedItem = newTab;
        }

        public void TabsView_OnTabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            if (_tabContentMap.ContainsKey(args.Tab))
            {
                _tabContentMap.Remove(args.Tab);
            }

            sender.TabItems.Remove(args.Tab);

            if (sender.TabItems.Count == 0)
            {
                _contentFrame.Content = null;
            }
        }

        private void TabsView_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
        {
            var draggedTab = _tabsView.SelectedItem as TabViewItem;

            if (draggedTab == null)
            {
                args.Cancel = true;
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
                return;
            }

            args.Data.RequestedOperation = DataPackageOperation.Move;
        }

        private async void TabsView_TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
        {
            if (string.IsNullOrEmpty(_draggedTabId))
            {
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

            // Сохраняем контент для передачи
            var contentToTransfer = sourceContent;

            // Даём UI время завершить текущие операции
            await Task.Delay(10);

            // Создаём окно с низким приоритетом
            _tabsView.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
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
                catch
                {
                    // Игнорируем ошибки создания окна
                }
            });
        }

        public void TransferContentWithUIElement(UIElement content, TabViewItem targetTab)
        {
            _tabContentMap[targetTab] = content;
        }

        public int GetTabContentMapCount() => _tabContentMap.Count;

        private void TabsView_TabStripDragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties.ContainsKey(TabIdKey))
                e.AcceptedOperation = DataPackageOperation.Move;
        }

        private async void TabsView_TabStripDrop(object sender, DragEventArgs e)
        {
            if (!e.DataView.Properties.TryGetValue(TabIdKey, out object idObj))
            {
                return;
            }
            string draggedTabId = idObj.ToString();

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
        }

        public void TransferContent(TabViewItem sourceTab, TabViewItem targetTab)
        {
            if (_tabContentMap.TryGetValue(sourceTab, out var content))
            {
                _tabContentMap[targetTab] = content;
                _tabContentMap.Remove(sourceTab);
            }
        }
    }
}