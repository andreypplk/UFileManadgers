using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using ufm.Pages;

namespace ufm
{
    public class TabViewManager
    {
        private TabView _tabsView;
        private Frame _contentFrame;
        private const string DataIdentifier = "MyTabItem";
        private readonly Dictionary<TabViewItem, UIElement> _tabContentMap = new Dictionary<TabViewItem, UIElement>();
        private readonly bool _skipInitialTab;

        public TabViewManager(TabView tabsView, Frame contentFrame)
            : this(tabsView, contentFrame, false)
        {
        }

        public TabViewManager(TabView tabsView, Frame contentFrame, bool skipInitialTab)
        {
            Debug.WriteLine($"[TabViewManager] Constructor START: tabsView={tabsView != null}, contentFrame={contentFrame != null}, skipInitialTab={skipInitialTab}");
            _tabsView = tabsView;
            _contentFrame = contentFrame;
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
                IconSource = new SymbolIconSource { Symbol = Symbol.Placeholder }
            };
            Debug.WriteLine($"[TabViewManager] CreateNewTab END: newTab created, Header='{newTab.Header}'");
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

        private void TabsView_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
        {
            Debug.WriteLine($"[TabViewManager] TabsView_TabDragStarting START: Tab.Header='{args.Tab.Header}'");
            int index = sender.TabItems.IndexOf(args.Tab);
            Debug.WriteLine($"[Drag] Index={index}, Header='{args.Tab.Header}'");
            args.Data.Properties.Add(DataIdentifier, args.Tab);
            args.Data.RequestedOperation = DataPackageOperation.Move;
            Debug.WriteLine($"[TabViewManager] TabsView_TabDragStarting END: DataIdentifier added, RequestedOperation=Move");
        }

        private async void TabsView_TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
        {
            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside START");
            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: source Tab.Header='{args.Tab.Header}'");
            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: source sender.TabItems.Count={sender.TabItems.Count}");
            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: _tabContentMap.Count={_tabContentMap.Count}");

            var sourceTab = args.Tab;
            var sourceHeader = sourceTab.Header?.ToString() ?? "Tab";
            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: sourceHeader='{sourceHeader}'");

            var sourceContent = _tabContentMap.ContainsKey(sourceTab) ? _tabContentMap[sourceTab] : null;
            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: sourceContent exists={sourceContent != null}");

            int selectedIndex = _tabsView.SelectedIndex;
            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: current selected index={selectedIndex}");

            // Сохраняем заголовок и иконку
            var savedHeader = sourceTab.Header;
            var savedIconSource = sourceTab.IconSource;

            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: savedHeader='{savedHeader}', savedIconSource={savedIconSource != null}");

            // Проверяем, содержится ли вкладка в TabItems
            bool containsBefore = sender.TabItems.Contains(sourceTab);
            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: Before removal - TabItems contains sourceTab? {containsBefore}");

            // Удаляем содержимое из словаря исходного окна
            if (_tabContentMap.ContainsKey(sourceTab))
            {
                Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: removing sourceTab from _tabContentMap");
                _tabContentMap.Remove(sourceTab);
                Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: _tabContentMap.Count={_tabContentMap.Count}");
            }

            // Удаляем вкладку из исходного TabView
            sender.TabItems.Remove(sourceTab);
            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: after removal, source sender.TabItems.Count={sender.TabItems.Count}");

            bool containsAfter = sender.TabItems.Contains(sourceTab);
            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: After removal - TabItems contains sourceTab? {containsAfter}");

            // Выводим оставшиеся вкладки
            for (int i = 0; i < sender.TabItems.Count; i++)
            {
                var tab = sender.TabItems[i] as TabViewItem;
                Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: Remaining tab {i}: Header='{tab?.Header}'");
            }

            // Восстанавливаем выделение в исходном окне
            if (sender.TabItems.Count > 0)
            {
                int newIndex = selectedIndex < sender.TabItems.Count ? selectedIndex : sender.TabItems.Count - 1;
                if (newIndex >= 0)
                {
                    _tabsView.SelectedIndex = newIndex;
                    Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: restored selection to index {newIndex}");
                }
            }

            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: creating new MainWindow with skipInitialTab=true");
            var newWindow = new MainWindow(true);
            newWindow.ExtendsContentIntoTitleBar = true;
            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: newWindow created");

            // Создаём новую вкладку в новом окне
            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: creating new tab in new window");
            var newTab = newWindow.TabViewManager.CreateNewTab(sourceHeader);
            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: newTab created, Header='{newTab.Header}'");

            // Копируем иконку
            newTab.IconSource = savedIconSource;
            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: copied icon source to new tab");

            // Передаём содержимое
            if (sourceContent != null)
            {
                Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: transferring existing content to new window");
                newWindow.TabViewManager.TransferContentWithUIElement(sourceContent, newTab);
                Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: content transferred");
            }
            else
            {
                Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: sourceContent is null, creating new Frame");
                var frame = new Frame();
                frame.Navigate(typeof(rootPage), null);
                newWindow.TabViewManager.TransferContentWithUIElement(frame, newTab);
                Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: new Frame created and transferred");
            }

            // Добавляем вкладку в новое окно
            newWindow.MainTabsView.TabItems.Add(newTab);
            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: newTab added to new window, TabItems.Count={newWindow.MainTabsView.TabItems.Count}");

            // Выводим вкладки нового окна
            for (int i = 0; i < newWindow.MainTabsView.TabItems.Count; i++)
            {
                var tab = newWindow.MainTabsView.TabItems[i] as TabViewItem;
                Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: New window tab {i}: Header='{tab?.Header}'");
            }

            newWindow.TabViewManager._tabsView.SelectedItem = newTab;
            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: newTab selected in new window");

            await Task.Delay(50);
            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside: activating new window");
            newWindow.Activate();

            Debug.WriteLine($"[TabViewManager] TabsView_TabDroppedOutside END");
        }

        public void TransferContentWithUIElement(UIElement content, TabViewItem targetTab)
        {
            Debug.WriteLine($"[TabViewManager] TransferContentWithUIElement START: content={content != null}, targetTab.Header='{targetTab.Header}'");
            _tabContentMap[targetTab] = content;
            Debug.WriteLine($"[TabViewManager] TransferContentWithUIElement END: _tabContentMap.Count={_tabContentMap.Count}");
        }

        public int GetTabContentMapCount()
        {
            return _tabContentMap.Count;
        }

        private void TabsView_TabStripDragOver(object sender, DragEventArgs e)
        {
            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDragOver START");
            bool hasKey = e.DataView.Properties.ContainsKey(DataIdentifier);
            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDragOver: DataIdentifier exists={hasKey}");
            if (hasKey)
            {
                e.AcceptedOperation = DataPackageOperation.Move;
                Debug.WriteLine($"[TabViewManager] TabsView_TabStripDragOver: AcceptedOperation set to Move");
            }
            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDragOver END");
        }

        private async void TabsView_TabStripDrop(object sender, DragEventArgs e)
        {
            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop START");

            if (!e.DataView.Properties.TryGetValue(DataIdentifier, out object obj))
            {
                Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop: DataIdentifier not found, exiting");
                return;
            }
            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop: DataIdentifier found, obj={obj != null}");

            var destinationTabView = sender as TabView;
            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop: destinationTabView={destinationTabView != null}");

            var index = -1;
            for (int i = 0; i < destinationTabView.TabItems.Count; i++)
            {
                var item = destinationTabView.ContainerFromIndex(i) as TabViewItem;
                if (item != null && e.GetPosition(item).X - item.ActualWidth < 0)
                {
                    index = i;
                    Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop: index found at {i}");
                    break;
                }
            }
            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop: final index={index}");

            object header = null;
            TabViewItem sourceTab = null;
            var element = (obj as UIElement);
            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop: element={element != null}");

            var tcs = new TaskCompletionSource();

            element.DispatcherQueue.TryEnqueue(() =>
            {
                Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop: DispatcherQueue executing");
                sourceTab = obj as TabViewItem;
                Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop: sourceTab={sourceTab != null}, Header='{sourceTab?.Header}'");

                (sourceTab.Parent as TabViewListView)?.Items.Remove(obj);
                Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop: sourceTab removed from parent");

                header = sourceTab.Header;
                Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop: header='{header}'");

                tcs.SetResult();
            });
            await tcs.Task;
            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop: after DispatcherQueue");

            var newTab = new TabViewItem
            {
                Header = header?.ToString(),
                IconSource = new SymbolIconSource { Symbol = Symbol.Placeholder }
            };
            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop: newTab created, Header='{newTab.Header}'");

            if (index < 0)
            {
                destinationTabView.TabItems.Add(newTab);
                Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop: newTab added at end");
            }
            else
            {
                destinationTabView.TabItems.Insert(index, newTab);
                Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop: newTab inserted at index {index}");
            }

            if (sourceTab != null)
            {
                Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop: calling TransferContent");
                TransferContent(sourceTab, newTab);
                Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop: TransferContent completed");
            }

            destinationTabView.SelectedItem = newTab;
            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop: newTab selected");

            Debug.WriteLine($"[TabViewManager] TabsView_TabStripDrop END");
        }

        public void TransferContent(TabViewItem sourceTab, TabViewItem targetTab)
        {
            Debug.WriteLine($"[TabViewManager] TransferContent START: sourceTab.Header='{sourceTab?.Header}', targetTab.Header='{targetTab?.Header}'");
            Debug.WriteLine($"[TabViewManager] TransferContent: _tabContentMap.ContainsKey(sourceTab)={_tabContentMap.ContainsKey(sourceTab)}");

            if (_tabContentMap.TryGetValue(sourceTab, out var content))
            {
                Debug.WriteLine($"[TabViewManager] TransferContent: content found, transferring");
                _tabContentMap[targetTab] = content;
                _tabContentMap.Remove(sourceTab);
                Debug.WriteLine($"[TabViewManager] TransferContent: content transferred, _tabContentMap.Count={_tabContentMap.Count}");
            }
            else
            {
                Debug.WriteLine($"[TabViewManager] TransferContent: content NOT found for sourceTab");
            }
            Debug.WriteLine($"[TabViewManager] TransferContent END");
        }
    }
}