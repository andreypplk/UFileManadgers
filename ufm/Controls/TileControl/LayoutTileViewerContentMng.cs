using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ufm
{
    public class LayoutTileViewerContentMng : ILayoutTileViewerContentMng
    {
        #region Константы

        private const int HorizontalPadding = 10;
        private const int VerticalPadding = 8;
        private const int TextBlockHeight = 40;
        private const int MinimumItemWidth = 100;
        private const int MinimumItemHeight = 40;
        private const int DefaultGridColumns = 6;

        #endregion

        #region Приватные поля

        private double _itemWidth;
        private double _itemHeight;
        private int _maxRowsOrColumns = 1;

        #endregion

        #region Свойства

        public double ItemWidth
        {
            get => _itemWidth;
            private set
            {
                if (Math.Abs(_itemWidth - value) > 0.01)
                {
                    _itemWidth = value;
                    OnPropertyChanged();
                }
            }
        }

        public double ItemHeight
        {
            get => _itemHeight;
            private set
            {
                if (Math.Abs(_itemHeight - value) > 0.01)
                {
                    _itemHeight = value;
                    OnPropertyChanged();
                }
            }
        }

        public int MaxRowsOrColumns
        {
            get => _maxRowsOrColumns;
            private set
            {
                if (_maxRowsOrColumns != value)
                {
                    _maxRowsOrColumns = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Публичные методы

        public void CalculateItemDimensions(string selectedSize)
        {
            if (string.IsNullOrEmpty(selectedSize))
                selectedSize = "Icons Medium";

            var sizeParams = SizeManagerTile.GetSize(selectedSize);
            string viewType = selectedSize.Split(' ').FirstOrDefault()?.ToLower() ?? "";

            switch (viewType)
            {
                case "icons":
                    ItemWidth = Math.Max(sizeParams.Width + 20, MinimumItemWidth);
                    ItemHeight = Math.Max(sizeParams.Height + 25, MinimumItemHeight);
                    break;

                case "list":
                case "compactlist":
                case "table":
                case "tiles":
                    ItemWidth = Math.Max(sizeParams.Width + 20, MinimumItemWidth);
                    ItemHeight = Math.Max(sizeParams.Height + 20, MinimumItemHeight);
                    break;

                default:
                    ItemWidth = Math.Max(sizeParams.Width + 20, MinimumItemWidth);
                    ItemHeight = Math.Max(sizeParams.Height + 25, MinimumItemHeight);
                    break;
            }

            Debug.WriteLine($"[LayoutManager] Dimensions calculated: {ItemWidth}x{ItemHeight} for size {selectedSize}");
        }

        public void UpdateItemsControlLayout(ListViewBase itemsControl, bool isListView)
        {
            if (itemsControl?.ItemsPanelRoot is not ItemsWrapGrid wrapGrid)
                return;

            wrapGrid.ItemWidth = ItemWidth;
            wrapGrid.ItemHeight = ItemHeight;

            if (isListView)
            {
                wrapGrid.MaximumRowsOrColumns = MaxRowsOrColumns;
            }
            else
            {
                wrapGrid.MaximumRowsOrColumns = 24;
            }
        }

        public int CalculateItemsPerColumnForListView(double actualHeight)
        {
            if (ItemHeight <= 0 || actualHeight <= 0)
                return 1;

            MaxRowsOrColumns = Math.Max(1, (int)((actualHeight - 20) / ItemHeight));
            return MaxRowsOrColumns;
        }

        public int CalculateItemsPerRowForGridView(double actualWidth)
        {
            if (ItemWidth <= 0 || actualWidth <= 0)
                return 1;

            return Math.Max(1, (int)Math.Floor(actualWidth / ItemWidth));
        }

        public int CalculateItemsPerPage(ListViewBase itemsControl, double panelHeight)
        {
            if (ItemHeight <= 0 || panelHeight <= 0)
                return 20;

            int rowsPerPage = Math.Max(1, (int)Math.Floor(panelHeight / ItemHeight));

            if (itemsControl is ListView)
            {
                return rowsPerPage * MaxRowsOrColumns;
            }
            else
            {
                double actualWidth = GetActualWidth(itemsControl);
                int itemsPerRow = CalculateItemsPerRowForGridView(actualWidth);
                return rowsPerPage * itemsPerRow;
            }
        }

        public void OnItemsControlSizeChanged(ListViewBase itemsControl, double newSize, bool isListView)
        {
            if (itemsControl == null)
                return;

            if (isListView)
            {
                CalculateItemsPerColumnForListView(newSize);
            }

            UpdateItemsControlLayout(itemsControl, isListView);
        }

        public void UpdateAllTiles(ListViewBase listView, ListViewBase gridView, string selectedSize)
        {
            UpdateTilesInControl(listView, selectedSize);
            UpdateTilesInControl(gridView, selectedSize);
        }

        #endregion

        #region Приватные методы

        private double GetActualWidth(ListViewBase itemsControl)
        {
            var panel = itemsControl?.ItemsPanelRoot as ItemsWrapGrid;
            return panel?.ActualWidth ?? 0;
        }

        private void UpdateTilesInControl(ListViewBase itemsControl, string selectedSize)
        {
            if (itemsControl == null)
                return;

            foreach (var item in itemsControl.Items)
            {
                var container = GetContainerFromItem(itemsControl, item);
                if (container != null)
                {
                    var tile = GetContentTemplateRootFromContainer(container);
                    if (tile is BaseTileControl baseTile)
                    {
                        baseTile.UpdateSize(selectedSize);
                    }
                }
            }
        }

        private FrameworkElement GetContainerFromItem(ListViewBase itemsControl, object item)
        {
            if (itemsControl is ListView listView)
            {
                return listView.ContainerFromItem(item) as FrameworkElement;
            }
            else if (itemsControl is GridView gridView)
            {
                return gridView.ContainerFromItem(item) as FrameworkElement;
            }
            return null;
        }

        private FrameworkElement GetContentTemplateRootFromContainer(FrameworkElement container)
        {
            if (container is ListViewItem listViewItem)
            {
                return listViewItem.ContentTemplateRoot as FrameworkElement;
            }
            else if (container is GridViewItem gridViewItem)
            {
                return gridViewItem.ContentTemplateRoot as FrameworkElement;
            }
            return null;
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}