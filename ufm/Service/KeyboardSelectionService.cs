using Core_FileManagement;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace ufm
{
    public class KeyboardSelectionService : IKeyboardSelectionService
    {
        #region Свойства

        public ExplorerItemViewModel ShiftSelectionStartItem { get; set; }

        #endregion

        #region Публичные методы

        public void SelectRange(int startIndex, int endIndex, ListViewBase itemsControl,
                                ObservableCollection<ExplorerItemViewModel> items, bool isCtrlPressed)
        {
            if (startIndex < 0 || endIndex < 0 || startIndex >= items.Count || endIndex >= items.Count)
                return;

            int minIndex = Math.Min(startIndex, endIndex);
            int maxIndex = Math.Max(startIndex, endIndex);

            if (!isCtrlPressed)
            {
                itemsControl.SelectedItems.Clear();
            }

            for (int i = minIndex; i <= maxIndex; i++)
            {
                if (!itemsControl.SelectedItems.Contains(items[i]))
                {
                    itemsControl.SelectedItems.Add(items[i]);
                }
            }

            itemsControl.ScrollIntoView(items[endIndex]);
            Debug.WriteLine($"[KeyboardSelection] Selected range {minIndex}-{maxIndex}");
        }

        public void ToggleSelection(ExplorerItemViewModel item, ListViewBase itemsControl)
        {
            if (itemsControl.SelectedItems.Contains(item))
            {
                itemsControl.SelectedItems.Remove(item);
                Debug.WriteLine($"[KeyboardSelection] Removed: {item.Name}");
            }
            else
            {
                itemsControl.SelectedItems.Add(item);
                Debug.WriteLine($"[KeyboardSelection] Added: {item.Name}");
            }
        }

        public void HandleShiftClick(int clickedIndex, ListViewBase itemsControl,
                                     ObservableCollection<ExplorerItemViewModel> items,
                                     ExplorerItemViewModel shiftStartItem, bool isCtrlPressed)
        {
            ExplorerItemViewModel startItem = shiftStartItem;

            if (startItem == null)
            {
                startItem = itemsControl.SelectedItem as ExplorerItemViewModel;
                if (startItem == null && items.Count > 0)
                {
                    startItem = items[0];
                }
            }

            if (startItem != null)
            {
                int startIndex = items.IndexOf(startItem);
                if (startIndex >= 0)
                {
                    SelectRange(startIndex, clickedIndex, itemsControl, items, isCtrlPressed);
                }
            }
        }

        public void HandleShiftArrow(int newIndex, ListViewBase itemsControl,
                                     ObservableCollection<ExplorerItemViewModel> items,
                                     ExplorerItemViewModel shiftStartItem, bool isCtrlPressed)
        {
            ExplorerItemViewModel startItem = shiftStartItem;

            if (startItem == null)
            {
                startItem = itemsControl.SelectedItem as ExplorerItemViewModel;
                if (startItem == null && items.Count > 0)
                {
                    startItem = items[0];
                }
            }

            if (startItem != null)
            {
                int startIndex = items.IndexOf(startItem);
                if (startIndex >= 0)
                {
                    SelectRange(startIndex, newIndex, itemsControl, items, isCtrlPressed);
                }
            }
        }

        public void HandleShiftRange(int currentIndex, int newIndex, ListViewBase itemsControl,
                                     ObservableCollection<ExplorerItemViewModel> items,
                                     bool isCtrlPressed)
        {
            if (currentIndex >= 0)
            {
                SelectRange(currentIndex, newIndex, itemsControl, items, isCtrlPressed);
            }
        }

        public void SetSingleSelection(ExplorerItemViewModel item, ListViewBase itemsControl)
        {
            itemsControl.SelectedItems.Clear();
            itemsControl.SelectedItems.Add(item);
            itemsControl.SelectedItem = item;
            ShiftSelectionStartItem = item;
        }

        public void ClearSelection(ListViewBase itemsControl)
        {
            itemsControl.SelectedItems.Clear();
            itemsControl.SelectedItem = null;
            ShiftSelectionStartItem = null;
        }

        #endregion
    }
}