//using Core_FileManagement;
//using Microsoft.UI.Xaml.Controls;
//using System;
//using System.Collections.ObjectModel;

//namespace ufm
//{
//    public interface IClickService
//    {
//        bool HandleItemClick(ExplorerItemViewModel clickedItem, int clickedIndex,
//                            ListViewBase itemsControl, bool isSingleClickMode,
//                            bool isCtrlPressed, bool isShiftPressed,
//                            IKeyboardSelectionService keyboardSelection);

//        bool IsDoubleClick(ExplorerItemViewModel item);
//        void ResetClickState();

//        event EventHandler<ItemClickEventArgs> ItemOpenRequested;
//    }

//    public class ItemClickEventArgs : EventArgs
//    {
//        public ExplorerItemViewModel Item { get; set; }
//        public int Index { get; set; }
//    }
//}

using Core_FileManagement;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;

namespace ufm
{
    public interface IClickService
    {
        bool HandleItemClick(ExplorerItemViewModel clickedItem, int clickedIndex,
                            ListViewBase itemsControl, bool isSingleClickMode,
                            bool isCtrlPressed, bool isShiftPressed,
                            IKeyboardSelectionService keyboardSelection);

        bool IsDoubleClick(ExplorerItemViewModel item);
        void ResetClickState();

        // Простое событие-уведомление
        event EventHandler ItemOpenRequested;
    }
}