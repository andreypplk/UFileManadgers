using Core_FileManagement;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;

namespace ufm
{
    public interface IMouseDragSelectionService
    {
        bool IsDragSelecting { get; }
        bool IsLeftMouseButtonPressed { get; }
        bool IsMouseMovingWithButton { get; }
        Vector2 DragStartPoint { get; }

        void StartDrag(Windows.Foundation.Point startPoint, ListViewBase itemsControl);
        void UpdateDrag(Windows.Foundation.Point currentPoint, ListViewBase itemsControl, bool isCtrlPressed);
        void EndDrag(ListViewBase itemsControl);

        void CreateSelectionRectangle(Canvas parentCanvas);
        void UpdateSelectionRectangle(Vector2 startPoint, Vector2 endPoint);
        void RemoveSelectionRectangle();

        HashSet<int> GetItemsInDragRectangle(Vector2 startPoint, Vector2 endPoint,
                                            ListViewBase itemsControl,
                                            ObservableCollection<ExplorerItemViewModel> items);

        void ApplyDragSelection(Vector2 startPoint, Vector2 endPoint,
                               ListViewBase itemsControl,
                               ObservableCollection<ExplorerItemViewModel> items,
                               bool isCtrlPressed);
    }
}