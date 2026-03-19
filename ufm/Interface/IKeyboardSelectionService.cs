using Core_FileManagement;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace ufm
{
    public interface IKeyboardSelectionService
    {
        ExplorerItemViewModel ShiftSelectionStartItem { get; set; }

        void SelectRange(int startIndex, int endIndex, ListViewBase itemsControl,
                        ObservableCollection<ExplorerItemViewModel> items, bool isCtrlPressed);

        void ToggleSelection(ExplorerItemViewModel item, ListViewBase itemsControl);

        void HandleShiftClick(int clickedIndex, ListViewBase itemsControl,
                             ObservableCollection<ExplorerItemViewModel> items,
                             ExplorerItemViewModel shiftStartItem, bool isCtrlPressed);

        void HandleShiftArrow(int newIndex, ListViewBase itemsControl,
                             ObservableCollection<ExplorerItemViewModel> items,
                             ExplorerItemViewModel shiftStartItem, bool isCtrlPressed);

        void HandleShiftRange(int currentIndex, int newIndex, ListViewBase itemsControl,
                             ObservableCollection<ExplorerItemViewModel> items,
                             bool isCtrlPressed);

        void SetSingleSelection(ExplorerItemViewModel item, ListViewBase itemsControl);
        void ClearSelection(ListViewBase itemsControl);
    }
}