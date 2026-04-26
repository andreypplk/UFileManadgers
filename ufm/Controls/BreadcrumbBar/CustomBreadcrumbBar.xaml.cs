using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Windows.Foundation;
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
                ApplyOptimalView();
            }
            else
            {
                ItemsPanel.Children.Clear();
            }
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ApplyOptimalView();
        }

        /// <summary>
        /// Сразу строит цепочку с нужным числом скрытых элементов (без мигания).
        /// </summary>
        private void ApplyOptimalView()
        {
            if (ItemsSource == null || ItemsSource.Count == 0)
            {
                ItemsPanel.Children.Clear();
                return;
            }

            if (BreadcrumbScrollViewer.ActualWidth <= 0)
            {
                // Размер ещё не известен – откладываем до завершения компоновки
                _ = DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => ApplyOptimalView());
                return;
            }

            double availableWidth = BreadcrumbScrollViewer.ActualWidth;
            var allItems = ItemsSource.ToList();

            // Вычисляем количество скрываемых элементов
            int collapseCount = CalculateCollapseCount(allItems, availableWidth);
            var visibleItems = BuildVisibleItems(allItems, collapseCount);

            // Отрисовываем один раз
            RenderChain(visibleItems);
        }

        /// <summary>
        /// Определяет, сколько элементов (начиная со второго) можно заменить на "...",
        /// чтобы оставшаяся цепочка поместилась в availableWidth.
        /// </summary>
        private int CalculateCollapseCount(List<CustomBreadcrumbItem> allItems, double availableWidth)
        {
            int maxCollapse = allItems.Count - 2; // как минимум первый и последний видны
            if (maxCollapse <= 0) return 0;

            // Полный путь помещается – ничего не скрываем
            if (MeasureWidth(BuildVisibleItems(allItems, 0)) <= availableWidth)
                return 0;

            // Линейный поиск первого подходящего количества скрытых
            for (int count = 1; count <= maxCollapse; count++)
            {
                if (MeasureWidth(BuildVisibleItems(allItems, count)) <= availableWidth)
                    return count;
            }

            // Если даже после скрытия всего не влезает, возвращаем максимум
            return maxCollapse;
        }

        /// <summary>
        /// Измеряет ширину цепочки для заданного набора элементов (без добавления в визуальное дерево).
        /// </summary>
        private double MeasureWidth(List<CustomBreadcrumbItem> items)
        {
            var tempPanel = new StackPanel { Orientation = Orientation.Horizontal };
            for (int i = 0; i < items.Count; i++)
            {
                tempPanel.Children.Add(CreateItemButton(items[i]));
                if (i < items.Count - 1)
                    tempPanel.Children.Add(CreateChevronButton(items[i]));
            }
            tempPanel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return tempPanel.DesiredSize.Width;
        }

        /// <summary>
        /// Формирует список для отображения: первый элемент всегда виден,
        /// затем (при необходимости) "…" с дочерними скрытыми элементами,
        /// затем оставшаяся часть.
        /// </summary>
        private List<CustomBreadcrumbItem> BuildVisibleItems(List<CustomBreadcrumbItem> allItems, int collapseCount)
        {
            var result = new List<CustomBreadcrumbItem> { allItems[0] };

            if (collapseCount > 0)
            {
                var collapsedChildren = new ObservableCollection<CustomBreadcrumbItem>(
                    allItems.Skip(1).Take(collapseCount));
                result.Add(new CustomBreadcrumbItem
                {
                    Text = "...",
                    FullPath = null,
                    Icon = null,
                    Children = collapsedChildren
                });
            }

            int startIndex = 1 + collapseCount;
            for (int i = startIndex; i < allItems.Count; i++)
                result.Add(allItems[i]);

            return result;
        }

        /// <summary>
        /// Очищает панель и наполняет её кнопками согласно списку элементов.
        /// </summary>
        private void RenderChain(List<CustomBreadcrumbItem> items)
        {
            ItemsPanel.Children.Clear();
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                ItemsPanel.Children.Add(CreateItemButton(item));
                if (i < items.Count - 1)
                    ItemsPanel.Children.Add(CreateChevronButton(item));
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
                var clicked = ((Button)s).Tag as CustomBreadcrumbItem;
                if (clicked == null) return;

                if (clicked.Text == "..." && clicked.Children?.Count > 0)
                {
                    ShowEllipsisFlyout(button, clicked.Children);
                    return;
                }

                ItemClicked?.Invoke(this, clicked);
            };

            return button;
        }

        private UIElement CreateItemContent(CustomBreadcrumbItem item)
        {
            var stack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };

            if (item.Icon != null)
            {
                stack.Children.Add(new Image
                {
                    Source = item.Icon,
                    Width = 16,
                    Height = 16,
                    Stretch = Stretch.Uniform,
                    VerticalAlignment = VerticalAlignment.Center
                });
            }

            stack.Children.Add(new TextBlock
            {
                Text = item.Text,
                VerticalAlignment = VerticalAlignment.Center
            });

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
                    ShowFlyout(button, item.Children);
                }
            };

            return button;
        }

        private void ShowEllipsisFlyout(Button ellipsisButton, IList<CustomBreadcrumbItem> children)
        {
            ShowFlyout(ellipsisButton, children);
        }

        private void ShowFlyout(Button target, IList<CustomBreadcrumbItem> children)
        {
            var flyout = new MenuFlyout { Placement = FlyoutPlacementMode.Bottom };
            foreach (var child in children)
            {
                var menuItem = new MenuFlyoutItem { Text = child.Text, Tag = child };
                if (child.Icon != null)
                    menuItem.Icon = new ImageIcon { Source = child.Icon };

                menuItem.Click += (s, e) =>
                {
                    var item = ((MenuFlyoutItem)s).Tag as CustomBreadcrumbItem;
                    ItemClicked?.Invoke(this, item);
                };
                flyout.Items.Add(menuItem);
            }
            flyout.ShowAt(target);
        }
    }
}