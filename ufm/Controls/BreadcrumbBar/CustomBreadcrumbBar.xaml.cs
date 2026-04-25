//using Microsoft.UI.Xaml;
//using Microsoft.UI.Xaml.Controls;
//using Microsoft.UI.Xaml.Controls.Primitives;
//using Microsoft.UI.Xaml.Data;
//using Microsoft.UI.Xaml.Input;
//using Microsoft.UI.Xaml.Media;
//using Microsoft.UI.Xaml.Media.Imaging;
//using Microsoft.UI.Xaml.Navigation;
//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.Collections.Specialized;
//using System.IO;
//using System.Linq;
//using System.Runtime.InteropServices.WindowsRuntime;
//using ufm.Models;
//using Windows.Foundation;
//using Windows.Foundation.Collections;

//// To learn more about WinUI, the WinUI project structure,
//// and more about our project templates, see: http://aka.ms/winui-project-info.

//namespace ufm.Controls.BreadcrumbBar
//{
//    public sealed partial class CustomBreadcrumbBar : UserControl
//    {
//        public static readonly DependencyProperty ItemsSourceProperty =
//            DependencyProperty.Register(
//                nameof(ItemsSource),
//                typeof(ObservableCollection<CustomBreadcrumbItem>),
//                typeof(CustomBreadcrumbBar),
//                new PropertyMetadata(null, OnItemsSourceChanged));

//        public ObservableCollection<CustomBreadcrumbItem> ItemsSource
//        {
//            get => (ObservableCollection<CustomBreadcrumbItem>)GetValue(ItemsSourceProperty);
//            set => SetValue(ItemsSourceProperty, value);
//        }

//        public event EventHandler<CustomBreadcrumbItem> ItemClicked;

//        public CustomBreadcrumbBar()
//        {
//            this.InitializeComponent();
//        }

//        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
//        {
//            var control = (CustomBreadcrumbBar)d;
//            control.OnItemsSourceChanged(
//                e.OldValue as ObservableCollection<CustomBreadcrumbItem>,
//                e.NewValue as ObservableCollection<CustomBreadcrumbItem>);
//        }

//        private void OnItemsSourceChanged(
//            ObservableCollection<CustomBreadcrumbItem> oldValue,
//            ObservableCollection<CustomBreadcrumbItem> newValue)
//        {
//            if (oldValue != null)
//                oldValue.CollectionChanged -= OnCollectionChanged;

//            if (newValue != null)
//            {
//                newValue.CollectionChanged += OnCollectionChanged;
//                RebuildUI();
//            }
//            else
//            {
//                ItemsPanel.Children.Clear();
//            }
//        }

//        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
//        {
//            RebuildUI();
//        }

//        private void RebuildUI()
//        {
//            ItemsPanel.Children.Clear();

//            if (ItemsSource == null || ItemsSource.Count == 0)
//                return;

//            for (int i = 0; i < ItemsSource.Count; i++)
//            {
//                var item = ItemsSource[i];
//                bool isLast = i == ItemsSource.Count - 1;

//                // Кнопка самого элемента (путь)
//                var itemButton = CreateItemButton(item);
//                ItemsPanel.Children.Add(itemButton);

//                // Если не последний, добавляем разделитель-шеврон с выпадающим меню
//                if (!isLast)
//                {
//                    var chevronButton = CreateChevronButton(item);
//                    ItemsPanel.Children.Add(chevronButton);
//                }
//            }
//        }

//        private Button CreateItemButton(CustomBreadcrumbItem item)
//        {
//            var button = new Button
//            {
//                Style = (Style)Resources["BreadcrumbButtonStyle"],
//                Content = CreateItemContent(item),
//                Tag = item
//            };

//            button.Click += (s, e) =>
//            {
//                var clickedItem = ((Button)s).Tag as CustomBreadcrumbItem;
//                ItemClicked?.Invoke(this, clickedItem);
//            };

//            return button;
//        }

//        private UIElement CreateItemContent(CustomBreadcrumbItem item)
//        {
//            var stack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };

//            // Иконка (если есть)
//            if (item.Icon != null)
//            {
//                var image = new Microsoft.UI.Xaml.Controls.Image
//                {
//                    Source = item.Icon,
//                    Width = 16,
//                    Height = 16,
//                    Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform,
//                    VerticalAlignment = VerticalAlignment.Center
//                };
//                stack.Children.Add(image);
//            }
//            else
//            {
//                // Заглушка на время загрузки
//                var placeholder = new Microsoft.UI.Xaml.Controls.Image
//                {
//                    Source = new BitmapImage(new Uri("ms-appx:///Assets/folder1.png")),
//                    Width = 16,
//                    Height = 16,
//                    VerticalAlignment = VerticalAlignment.Center
//                };
//                stack.Children.Add(placeholder);
//            }

//            // Текст
//            var textBlock = new TextBlock
//            {
//                Text = item.Text,
//                VerticalAlignment = VerticalAlignment.Center
//            };
//            stack.Children.Add(textBlock);

//            return stack;
//        }

//        private Button CreateChevronButton(CustomBreadcrumbItem parentItem)
//        {
//            var button = new Button
//            {
//                Style = (Style)Resources["ChevronButtonStyle"],
//                Tag = parentItem
//            };

//            // Создаём выпадающее меню, которое будет открываться по клику на шеврон
//            button.Click += (s, e) =>
//            {
//                var item = ((Button)s).Tag as CustomBreadcrumbItem;
//                if (item?.Children != null && item.Children.Any())
//                {
//                    var flyout = new MenuFlyout { Placement = FlyoutPlacementMode.Bottom };

//                    foreach (var child in item.Children)
//                    {
//                        var menuItem = new MenuFlyoutItem { Text = child.Text, Tag = child };

//                        // Добавляем иконку в меню
//                        if (child.Icon != null)
//                        {
//                            menuItem.Icon = new Microsoft.UI.Xaml.Controls.ImageIcon
//                            {
//                                Source = child.Icon
//                            };
//                        }
//                        else
//                        {
//                            menuItem.Icon = new Microsoft.UI.Xaml.Controls.ImageIcon
//                            {
//                                Source = new BitmapImage(new Uri("ms-appx:///Assets/folder1.png"))
//                            };
//                        }

//                        menuItem.Click += (menuSender, menuArgs) =>
//                        {
//                            var clickedChild = ((MenuFlyoutItem)menuSender).Tag as CustomBreadcrumbItem;
//                            ItemClicked?.Invoke(this, clickedChild);
//                        };

//                        flyout.Items.Add(menuItem);
//                    }

//                    flyout.ShowAt(button);
//                }
//            };

//            return button;
//        }
//    }
//}



using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using ufm.Models;

namespace ufm.Controls.BreadcrumbBar
{
    public sealed partial class CustomBreadcrumbBar : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource),
                typeof(ObservableCollection<CustomBreadcrumbItem>),
                typeof(CustomBreadcrumbBar),
                new PropertyMetadata(null, OnItemsSourceChanged));

        public ObservableCollection<CustomBreadcrumbItem> ItemsSource
        {
            get => (ObservableCollection<CustomBreadcrumbItem>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public event EventHandler<CustomBreadcrumbItem> ItemClicked;

        public CustomBreadcrumbBar()
        {
            this.InitializeComponent();
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (CustomBreadcrumbBar)d;
            control.OnItemsSourceChanged(
                e.OldValue as ObservableCollection<CustomBreadcrumbItem>,
                e.NewValue as ObservableCollection<CustomBreadcrumbItem>);
        }

        private void OnItemsSourceChanged(
            ObservableCollection<CustomBreadcrumbItem> oldValue,
            ObservableCollection<CustomBreadcrumbItem> newValue)
        {
            if (oldValue != null)
                oldValue.CollectionChanged -= OnCollectionChanged;

            if (newValue != null)
            {
                newValue.CollectionChanged += OnCollectionChanged;
                RebuildUI();
            }
            else
            {
                ItemsPanel.Children.Clear();
            }
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RebuildUI();
        }

        public void RefreshChevrons()
        {
            // Перестраиваем только шевроны, не трогая кнопки элементов (сохраняем иконки)
            // Проще всё же пересобрать полностью, но теперь это будет вызвано только при готовности детей,
            // а иконки уже загружены и не должны мерцать.
            RebuildUI();
        }

        private void RebuildUI()
        {
            ItemsPanel.Children.Clear();

            if (ItemsSource == null || ItemsSource.Count == 0)
                return;

            for (int i = 0; i < ItemsSource.Count; i++)
            {
                var item = ItemsSource[i];
                bool isLast = i == ItemsSource.Count - 1;

                var itemButton = CreateItemButton(item);
                ItemsPanel.Children.Add(itemButton);

                if (!isLast)
                {
                    var chevronButton = CreateChevronButton(item);
                    ItemsPanel.Children.Add(chevronButton);
                }
            }

            // Прокручиваем к последнему элементу (правому краю)
            ScrollToEnd();
        }

        private async void ScrollToEnd()
        {
            // Даём UI обновиться, затем прокручиваем
            await Task.Delay(10);
            var scrollViewer = this.Content as ScrollViewer;
            if (scrollViewer != null)
            {
                scrollViewer.ChangeView(scrollViewer.ScrollableWidth, null, null, true);
            }
        }

        private Button CreateItemButton(CustomBreadcrumbItem item)
        {
            var button = new Button
            {
                Style = (Style)Resources["BreadcrumbButtonStyle"],
                Content = CreateItemContent(item),
                Tag = item
            };

            button.Click += (s, e) =>
            {
                var clickedItem = ((Button)s).Tag as CustomBreadcrumbItem;
                ItemClicked?.Invoke(this, clickedItem);
            };

            return button;
        }

        private UIElement CreateItemContent(CustomBreadcrumbItem item)
        {
            var stack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };

            // Иконка (загружена заранее, поэтому мерцать не будет)
            if (item.Icon != null)
            {
                var image = new Microsoft.UI.Xaml.Controls.Image
                {
                    Source = item.Icon,
                    Width = 16,
                    Height = 16,
                    Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform,
                    VerticalAlignment = VerticalAlignment.Center
                };
                stack.Children.Add(image);
            }

            var textBlock = new TextBlock
            {
                Text = item.Text,
                VerticalAlignment = VerticalAlignment.Center
            };
            stack.Children.Add(textBlock);

            return stack;
        }

        private Button CreateChevronButton(CustomBreadcrumbItem parentItem)
        {
            var button = new Button
            {
                Style = (Style)Resources["ChevronButtonStyle"],
                Tag = parentItem
            };

            button.Click += (s, e) =>
            {
                var item = ((Button)s).Tag as CustomBreadcrumbItem;
                if (item?.Children != null && item.Children.Any())
                {
                    var flyout = new MenuFlyout { Placement = FlyoutPlacementMode.Bottom };

                    foreach (var child in item.Children)
                    {
                        var menuItem = new MenuFlyoutItem { Text = child.Text, Tag = child };

                        if (child.Icon != null)
                        {
                            menuItem.Icon = new Microsoft.UI.Xaml.Controls.ImageIcon
                            {
                                Source = child.Icon
                            };
                        }

                        menuItem.Click += (menuSender, menuArgs) =>
                        {
                            var clickedChild = ((MenuFlyoutItem)menuSender).Tag as CustomBreadcrumbItem;
                            ItemClicked?.Invoke(this, clickedChild);
                        };

                        flyout.Items.Add(menuItem);
                    }

                    flyout.ShowAt(button);
                }
            };

            return button;
        }
    }
}