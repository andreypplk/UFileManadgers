using Core_FileManagement;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ufm
{
    public interface IRenameService
    {
        bool IsMultiRenameMode { get; }
        bool IsEditing { get; }

        void StartSingleRename(ExplorerItemViewModel item, ListViewBase itemsControl);
        void StartMultiRename(IEnumerable<ExplorerItemViewModel> selectedItems,
                             ObservableCollection<ExplorerItemViewModel> items,
                             ListViewBase itemsControl);
        void HandleEditCompleted(EditResult result, ExplorerItemViewModel editedItem);
        void CancelEditing();

        event EventHandler<ExplorerItemViewModel> RenameStarted;
        event EventHandler RenameCompleted;
    }
}