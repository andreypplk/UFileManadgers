using Core_FileManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Numerics;
using Windows.Foundation;

namespace ufm
{
    public class MouseDragSelectionService : IMouseDragSelectionService
    {
        #region Поля

        private bool _isDragSelecting = false;
        private bool _isLeftMouseButtonPressed = false;
        private bool _isMouseMovingWithButton = false;
        private Vector2 _dragStartPoint;
        private Rectangle _selectionRectangle;
        private Canvas _selectionCanvas;
        private const float DragThreshold = 10.0f;
        private const float MoveThreshold = 3.0f;

        #endregion

        #region Свойства

        public bool IsDragSelecting => _isDragSelecting;
        public bool IsLeftMouseButtonPressed => _isLeftMouseButtonPressed;
        public bool IsMouseMovingWithButton => _isMouseMovingWithButton;
        public Vector2 DragStartPoint => _dragStartPoint;

        #endregion

        #region Публичные методы

        public void StartDrag(Point startPoint, ListViewBase itemsControl)
        {
            _dragStartPoint = new Vector2((float)startPoint.X, (float)startPoint.Y);
            _isLeftMouseButtonPressed = true;
            _isMouseMovingWithButton = false;
        }

        public void UpdateDrag(Point currentPoint, ListViewBase itemsControl, bool isCtrlPressed)
        {
            if (!_isLeftMouseButtonPressed)
                return;

            var currentPosition = new Vector2((float)currentPoint.X, (float)currentPoint.Y);

            float distance = Vector2.Distance(_dragStartPoint, currentPosition);

            if (distance > MoveThreshold && !_isMouseMovingWithButton)
            {
                _isMouseMovingWithButton = true;
            }

            if (distance > DragThreshold && !_isDragSelecting)
            {
                _isDragSelecting = true;

                if (!isCtrlPressed)
                {
                    itemsControl.SelectedItems.Clear();
                    itemsControl.SelectedItem = null;
                }
            }
        }

        public void EndDrag(ListViewBase itemsControl)
        {
            _isLeftMouseButtonPressed = false;
            _isMouseMovingWithButton = false;
            _isDragSelecting = false;

            RemoveSelectionRectangle();
        }

        public void CreateSelectionRectangle(Canvas parentCanvas)
        {
            if (_selectionRectangle != null)
                return;

            _selectionCanvas = parentCanvas;

            _selectionRectangle = new Rectangle
            {
                Stroke = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue),
                StrokeThickness = 1,
                Fill = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue) { Opacity = 0.3 },
                StrokeDashArray = new DoubleCollection() { 2, 2 },
                Width = 0,
                Height = 0,
                Visibility = Microsoft.UI.Xaml.Visibility.Visible
            };

            if (_selectionCanvas != null)
            {
                _selectionCanvas.Children.Add(_selectionRectangle);
                Canvas.SetLeft(_selectionRectangle, 0);
                Canvas.SetTop(_selectionRectangle, 0);
            }
        }

        public void UpdateSelectionRectangle(Vector2 startPoint, Vector2 endPoint)
        {
            if (_selectionRectangle == null || _selectionCanvas == null)
                return;

            float left = Math.Min(startPoint.X, endPoint.X);
            float top = Math.Min(startPoint.Y, endPoint.Y);
            float width = Math.Abs(endPoint.X - startPoint.X);
            float height = Math.Abs(endPoint.Y - startPoint.Y);

            Canvas.SetLeft(_selectionRectangle, left);
            Canvas.SetTop(_selectionRectangle, top);
            _selectionRectangle.Width = width;
            _selectionRectangle.Height = height;

            _selectionRectangle.Visibility = width > 0 && height > 0
                ? Microsoft.UI.Xaml.Visibility.Visible
                : Microsoft.UI.Xaml.Visibility.Collapsed;
        }

        public void RemoveSelectionRectangle()
        {
            if (_selectionRectangle != null && _selectionCanvas != null)
            {
                _selectionCanvas.Children.Remove(_selectionRectangle);
                _selectionRectangle = null;
            }
        }

        public HashSet<int> GetItemsInDragRectangle(Vector2 startPoint, Vector2 endPoint,
                                                    ListViewBase itemsControl,
                                                    ObservableCollection<ExplorerItemViewModel> items)
        {
            if (items.Count == 0)
                return new HashSet<int>();

            float left = Math.Min(startPoint.X, endPoint.X);
            float right = Math.Max(startPoint.X, endPoint.X);
            float top = Math.Min(startPoint.Y, endPoint.Y);
            float bottom = Math.Max(startPoint.Y, endPoint.Y);

            var selectedIndices = new HashSet<int>();

            var panel = itemsControl.ItemsPanelRoot as ItemsWrapGrid;
            if (panel != null)
            {
                foreach (var child in panel.Children)
                {
                    if (child is FrameworkElement container && container.Visibility == Microsoft.UI.Xaml.Visibility.Visible)
                    {
                        int index = itemsControl.IndexFromContainer(container);
                        if (index >= 0 && index < items.Count)
                        {
                            var transform = container.TransformToVisual(itemsControl);
                            var position = transform.TransformPoint(new Point(0, 0));

                            float itemLeft = (float)position.X;
                            float itemTop = (float)position.Y;
                            float itemRight = itemLeft + (float)container.ActualWidth;
                            float itemBottom = itemTop + (float)container.ActualHeight;

                            bool intersects = itemRight > left && itemLeft < right &&
                                              itemBottom > top && itemTop < bottom;

                            if (intersects)
                            {
                                selectedIndices.Add(index);
                            }
                        }
                    }
                }
            }

            return selectedIndices;
        }

        public void ApplyDragSelection(Vector2 startPoint, Vector2 endPoint,
                                      ListViewBase itemsControl,
                                      ObservableCollection<ExplorerItemViewModel> items,
                                      bool isCtrlPressed)
        {
            var selectedIndices = GetItemsInDragRectangle(startPoint, endPoint, itemsControl, items);

            foreach (int index in selectedIndices)
            {
                if (index >= 0 && index < items.Count)
                {
                    var item = items[index];
                    if (!itemsControl.SelectedItems.Contains(item))
                    {
                        itemsControl.SelectedItems.Add(item);
                    }
                }
            }

            if (!isCtrlPressed)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (!selectedIndices.Contains(i))
                    {
                        var item = items[i];
                        if (itemsControl.SelectedItems.Contains(item))
                        {
                            itemsControl.SelectedItems.Remove(item);
                        }
                    }
                }
            }

            UpdateSelectionVisual(itemsControl);
        }

        #endregion

        #region Приватные методы

        private void UpdateSelectionVisual(ListViewBase itemsControl)
        {
            foreach (var item in itemsControl.SelectedItems)
            {
                var container = itemsControl.ContainerFromItem(item) as Control;
                if (container != null)
                {
                    VisualStateManager.GoToState(container, "Selected", false);
                }
            }
        }

        #endregion
    }
}