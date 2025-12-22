using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using ufm.Pages;
using Windows.System;
using Windows.UI.Core;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI;
using WinRT.Interop;
using DispatcherQueueHandler = Microsoft.UI.Dispatching.DispatcherQueueHandler;

namespace ufm
{
    public class TabViewManager
    {
        private TabView _tabsView;
        private Frame _contentFrame;
        private const string DataIdentifier = "MyTabItem";

        public TabViewManager(TabView tabsView, Frame contentFrame)
        {
            _tabsView = tabsView;
            _contentFrame = contentFrame;

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
        }

        private void TabsView_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
        {
            // Мы можем перетаскивать только одну вкладку за раз, поэтому берем первую...
            var firstItem = args.Tab;

            // ... устанавливаем данные для перетаскивания в эту вкладку...
            args.Data.Properties.Add(DataIdentifier, firstItem);

            // ... и указываем, что мы можем перемещать ее
            args.Data.RequestedOperation = DataPackageOperation.Move;
        }

        private void TabsView_TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
        {
            // Создаем новое окно
            var newWindow = new MainWindow();
            newWindow.ExtendsContentIntoTitleBar = true;
            // Создаем новую страницу
            var newPage = new rootPage();

            // Удаляем вкладку из текущего TabView и добавляем в новое окно
            sender.TabItems.Remove(args.Tab);

            // Добавляем вкладку в новый TabView на странице
            newPage.AddTabToTabs(args.Tab);

            // Активируем новое окно
            newWindow.Activate();
        }
        private void TabsView_TabStripDragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties.ContainsKey(DataIdentifier))
            {
                e.AcceptedOperation = DataPackageOperation.Move;
            }
        }
        private async void TabsView_TabStripDrop(object sender, DragEventArgs e)
        {
            // Это событие вызывается, когда мы перетаскиваем вкладки между разными TabView
            // Оно отвечает за обработку переноса элемента во второй TabView

            if (e.DataView.Properties.TryGetValue(DataIdentifier, out object obj))
            {
                // Убедимся, что свойство obj установлено перед продолжением.
                if (obj == null)
                {
                    return;
                }

                var destinationTabView = sender as TabView;
                var destinationItems = destinationTabView.TabItems;

                if (destinationItems != null)
                {
                    // Сначала нужно получить позицию в списке, куда мы будем вставлять элемент
                    var index = -1;

                    // Определяем, между какими элементами списка находится наш указатель.
                    for (int i = 0; i < destinationTabView.TabItems.Count; i++)
                    {
                        var item = destinationTabView.ContainerFromIndex(i) as TabViewItem;

                        if (e.GetPosition(item).X - item.ActualWidth < 0)
                        {
                            index = i;
                            break;
                        }
                    }

                    // TabViewItem может быть только в одном дереве одновременно. Прежде чем перемещать его в новый TabView, удаляем его из старого.
                    // Обратите внимание, что этот вызов может происходить в другом потоке, если перемещаем между окнами. Поэтому убедитесь, что методы вызываются в
                    // том же потоке, где были созданы элементы UI.

                    object header = null;
                    object dataContext = null;
                    var element = (obj as UIElement);

                    var taskCompletionSource = new TaskCompletionSource();

                    element.DispatcherQueue.TryEnqueue(
                        Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal,
                        new DispatcherQueueHandler(() =>
                        {
                            var tabItem = obj as TabViewItem;
                            var destinationTabViewListView = (tabItem.Parent as TabViewListView);
                            destinationTabViewListView.Items.Remove(obj);
                            header = tabItem.Header;
                            dataContext = (tabItem.Content as rootPage).DataContext;

                            taskCompletionSource.SetResult();
                        }));

                    await taskCompletionSource.Task;

                    var insertedItem = CreateNewTVI(header.ToString(), dataContext.ToString());
                    if (index < 0)
                    {
                        // Мы не нашли точку перехода, так что добавляем в конец списка
                        destinationItems.Add(insertedItem);
                    }
                    else if (index < destinationTabView.TabItems.Count)
                    {
                        // В противном случае вставляем по указанному индексу
                        destinationItems.Insert(index, insertedItem);
                    }

                    // Выбираем вновь перемещенную вкладку
                    destinationTabView.SelectedItem = insertedItem;
                }
            }
        }

        private void TabsView_Loaded(object sender, RoutedEventArgs e)
        {
            var initialTab = CreateNewTab("Root Page", typeof(rootPage));
            _tabsView.TabItems.Add(initialTab);
            _tabsView.SelectedIndex = 0;

            Debug.WriteLine($"Initial tab with RootPage added. TabsView.TabItems.Count: {_tabsView.TabItems.Count}");
        }


        public TabViewItem CreateNewTab(string header, Type pageType)
        {
            var frame = new Frame();
            frame.Navigate(pageType, null); // Навигируем на страницу

            var newTab = new TabViewItem
            {
                Header = header,
                IconSource = new Microsoft.UI.Xaml.Controls.SymbolIconSource
                {
                    Symbol = Symbol.Placeholder
                },
                Content = frame
            };

            return newTab;
        }
        private TabViewItem CreateNewTVI(string header, string dataContext)
        {
            var newTab = new TabViewItem()
            {
                IconSource = new Microsoft.UI.Xaml.Controls.SymbolIconSource()
                {
                    Symbol = Symbol.Placeholder
                },
                Header = header,
                Content = new rootPage()
                {
                    DataContext = dataContext
                }
            };

            return newTab;
        }

        public void TabsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedTab = _tabsView.SelectedItem as TabViewItem;
            if (selectedTab != null)
            {
                if (selectedTab.Content is Frame frame)
                {
                    var pageType = frame.SourcePageType;
                    if (pageType != null)
                    {
                        _contentFrame.Navigate(pageType, frame.DataContext);
                        Debug.WriteLine($"Content set to {pageType.Name}");
                    }
                }
                else
                {
                    _contentFrame.Content = selectedTab.Content;
                    Debug.WriteLine($"Content set to {selectedTab.Content.GetType().Name}");
                }
            }
            else
            {
                _contentFrame.Content = null; // Скрываем ContentFrame, если вкладка не выбрана
            }
        }

        public void TabsView_OnAddTabButtonClick(TabView sender, object args)
        {
            var newTab = CreateNewTab("New Tab", typeof(rootPage));
            _tabsView.TabItems.Add(newTab);
            _tabsView.SelectedItem = newTab;
            Debug.WriteLine("New tab added");
        }

        public void TabsView_OnTabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            sender.TabItems.Remove(args.Tab);
            Debug.WriteLine("Tab closed");
            if (sender.TabItems.Count == 0)
            {
                _contentFrame.Content = null; // Проверка на отсутствие вкладок и скрытие ContentFrame
                Debug.WriteLine("All tabs closed, ContentFrame hidden");
            }
        }
        
    }
}
