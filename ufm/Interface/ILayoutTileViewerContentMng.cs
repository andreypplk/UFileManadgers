using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;

namespace ufm
{
    public interface ILayoutTileViewerContentMng : INotifyPropertyChanged
    {
        double ItemWidth { get; }
        double ItemHeight { get; }
        int MaxRowsOrColumns { get; }

        void CalculateItemDimensions(string selectedSize);
        void UpdateItemsControlLayout(ListViewBase itemsControl, bool isListView);
        int CalculateItemsPerColumnForListView(double actualHeight);
        int CalculateItemsPerRowForGridView(double actualWidth);
        int CalculateItemsPerPage(ListViewBase itemsControl, double panelHeight);
        void OnItemsControlSizeChanged(ListViewBase itemsControl, double newSize, bool isListView);
        void UpdateAllTiles(ListViewBase listView, ListViewBase gridView, string selectedSize);
    }
}