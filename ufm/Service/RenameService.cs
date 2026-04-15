//using Core_FileManagement;
//using Microsoft.UI.Dispatching;
//using Microsoft.UI.Xaml;
//using Microsoft.UI.Xaml.Controls;
//using System;
//using System.Collections.Generic;
//using System.Collections.ObjectModel;
//using System.Diagnostics;
//using System.Linq;
//using System.Threading.Tasks;

//namespace ufm
//{
//    public class RenameService : IRenameService
//    {
//        #region Поля

//        private bool _isMultiRenameMode = false;
//        private List<string> _multiRenamePaths;
//        private int _multiRenameCurrentIndex;
//        private ObservableCollection<ExplorerItemViewModel> _items;
//        private ListViewBase _itemsControl;
//        private const string MultiRenameLogPrefix = "[MultiRename]";
//        private DispatcherQueue _dispatcherQueue;

//        #endregion

//        #region Свойства

//        public bool IsMultiRenameMode => _isMultiRenameMode;
//        public bool IsEditing { get; private set; }

//        #endregion

//        #region События

//        public event EventHandler<ExplorerItemViewModel> RenameStarted;
//        public event EventHandler RenameCompleted;

//        #endregion

//        #region Конструктор

//        public RenameService()
//        {
//            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
//        }

//        #endregion

//        #region Публичные методы

//        public void StartSingleRename(ExplorerItemViewModel item, ListViewBase itemsControl)
//        {
//            try
//            {
//                if (item == null || item.IsEditing)
//                    return;

//                _itemsControl = itemsControl;
//                IsEditing = true;

//                var container = GetContainerFromItem(itemsControl, item);

//                if (container != null)
//                {
//                    var tile = GetContentTemplateRootFromContainer(container) as BaseTileControl;
//                    if (tile != null && tile.CanEdit)
//                    {
//                        tile.StartEditing();
//                        Debug.WriteLine($"[RenameService] Rename started for: {item.Name}");
//                        RenameStarted?.Invoke(this, item);
//                    }
//                }
//                else
//                {
//                    itemsControl.ScrollIntoView(item);

//                    _ = Task.Delay(100).ContinueWith(_ =>
//                    {
//                        _dispatcherQueue.TryEnqueue(() =>
//                        {
//                            container = GetContainerFromItem(itemsControl, item);
//                            if (container != null)
//                            {
//                                var tile = GetContentTemplateRootFromContainer(container) as BaseTileControl;
//                                if (tile != null && tile.CanEdit)
//                                {
//                                    tile.StartEditing();
//                                    Debug.WriteLine($"[RenameService] Rename started (delayed) for: {item.Name}");
//                                    RenameStarted?.Invoke(this, item);
//                                }
//                            }
//                        });
//                    });
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[RenameService] Error in StartSingleRename: {ex}");
//                IsEditing = false;
//            }
//        }

//        public void StartMultiRename(IEnumerable<ExplorerItemViewModel> selectedItems,
//                                    ObservableCollection<ExplorerItemViewModel> items,
//                                    ListViewBase itemsControl)
//        {
//            Debug.WriteLine($"[RenameService] {MultiRenameLogPrefix} STARTING multi-rename for {selectedItems.Count()} items");

//            _items = items;
//            _itemsControl = itemsControl;
//            _multiRenamePaths = selectedItems
//                .OrderBy(item => items.IndexOf(item))
//                .Select(item => item.FilePath)
//                .ToList();

//            _multiRenameCurrentIndex = 0;
//            _isMultiRenameMode = true;
//            IsEditing = true;

//            BeginRenameForCurrentMultiItem();
//        }

//        public void HandleEditCompleted(EditResult result, ExplorerItemViewModel editedItem)
//        {
//            if (!_isMultiRenameMode)
//            {
//                IsEditing = false;
//                RenameCompleted?.Invoke(this, EventArgs.Empty);
//                return;
//            }

//            if (_multiRenamePaths == null || _multiRenameCurrentIndex < 0 || _multiRenameCurrentIndex >= _multiRenamePaths.Count)
//            {
//                Debug.WriteLine($"[RenameService] {MultiRenameLogPrefix} ERROR: Invalid state, finishing");
//                FinishMultiRename();
//                return;
//            }

//            string currentPath = _multiRenamePaths[_multiRenameCurrentIndex];

//            if (result == EditResult.Saved)
//            {
//                Debug.WriteLine($"[RenameService] {MultiRenameLogPrefix} Item saved, moving to next");
//                _multiRenamePaths.RemoveAt(_multiRenameCurrentIndex);
//                BeginRenameForCurrentMultiItem();
//            }
//            else
//            {
//                Debug.WriteLine($"[RenameService] {MultiRenameLogPrefix} Cancelled or error, finishing sequence");
//                FinishMultiRename();
//            }
//        }

//        public void CancelEditing()
//        {
//            if (_isMultiRenameMode)
//            {
//                FinishMultiRename();
//            }
//            else
//            {
//                IsEditing = false;
//            }
//        }

//        #endregion

//        #region Приватные методы

//        private void BeginRenameForCurrentMultiItem()
//        {
//            if (!_isMultiRenameMode || _multiRenamePaths == null)
//            {
//                FinishMultiRename();
//                return;
//            }

//            if (_multiRenameCurrentIndex >= _multiRenamePaths.Count)
//            {
//                Debug.WriteLine($"[RenameService] {MultiRenameLogPrefix} Completed all {_multiRenamePaths.Count} items");
//                FinishMultiRename();
//                return;
//            }

//            string targetPath = _multiRenamePaths[_multiRenameCurrentIndex];

//            var item = _items.FirstOrDefault(x => x.FilePath == targetPath);
//            if (item == null)
//            {
//                Debug.WriteLine($"[RenameService] {MultiRenameLogPrefix} WARNING: Item not found, skipping");
//                _multiRenameCurrentIndex++;
//                BeginRenameForCurrentMultiItem();
//                return;
//            }

//            _itemsControl.SelectedItems.Clear();
//            _itemsControl.SelectedItems.Add(item);
//            _itemsControl.SelectedItem = item;

//            var container = GetContainerFromItem(_itemsControl, item);
//            if (container != null)
//            {
//                var tile = GetContentTemplateRootFromContainer(container) as BaseTileControl;
//                if (tile != null)
//                {
//                    Debug.WriteLine($"[RenameService] {MultiRenameLogPrefix} Starting rename for [{_multiRenameCurrentIndex}]: {item.Name}");
//                    tile.StartEditing();
//                    RenameStarted?.Invoke(this, item);
//                }
//                else
//                {
//                    Debug.WriteLine($"[RenameService] {MultiRenameLogPrefix} ERROR: Tile is null");
//                    _multiRenameCurrentIndex++;
//                    BeginRenameForCurrentMultiItem();
//                }
//            }
//            else
//            {
//                _itemsControl.ScrollIntoView(item);

//                _ = Task.Delay(100).ContinueWith(_ =>
//                {
//                    _dispatcherQueue.TryEnqueue(() =>
//                    {
//                        container = GetContainerFromItem(_itemsControl, item);
//                        if (container != null)
//                        {
//                            var tile = GetContentTemplateRootFromContainer(container) as BaseTileControl;
//                            if (tile != null)
//                            {
//                                Debug.WriteLine($"[RenameService] {MultiRenameLogPrefix} Starting rename (delayed) for [{_multiRenameCurrentIndex}]: {item.Name}");
//                                tile.StartEditing();
//                                RenameStarted?.Invoke(this, item);
//                            }
//                            else
//                            {
//                                Debug.WriteLine($"[RenameService] {MultiRenameLogPrefix} Retry: tile is null, skipping");
//                                _multiRenameCurrentIndex++;
//                                BeginRenameForCurrentMultiItem();
//                            }
//                        }
//                        else
//                        {
//                            Debug.WriteLine($"[RenameService] {MultiRenameLogPrefix} Retry: container not found, skipping");
//                            _multiRenameCurrentIndex++;
//                            BeginRenameForCurrentMultiItem();
//                        }
//                    });
//                });
//            }
//        }

//        private void FinishMultiRename()
//        {
//            Debug.WriteLine($"[RenameService] {MultiRenameLogPrefix} FINISHING multi-rename");

//            _isMultiRenameMode = false;
//            _multiRenamePaths = null;
//            _multiRenameCurrentIndex = 0;
//            IsEditing = false;

//            _dispatcherQueue.TryEnqueue(() =>
//            {
//                _itemsControl?.Focus(FocusState.Programmatic);
//            });

//            RenameCompleted?.Invoke(this, EventArgs.Empty);
//        }

//        private FrameworkElement GetContainerFromItem(ListViewBase itemsControl, object item)
//        {
//            if (itemsControl is ListView listView)
//            {
//                return listView.ContainerFromItem(item) as FrameworkElement;
//            }
//            else if (itemsControl is GridView gridView)
//            {
//                return gridView.ContainerFromItem(item) as FrameworkElement;
//            }
//            return null;
//        }

//        private FrameworkElement GetContentTemplateRootFromContainer(FrameworkElement container)
//        {
//            if (container is ListViewItem listViewItem)
//            {
//                return listViewItem.ContentTemplateRoot as FrameworkElement;
//            }
//            else if (container is GridViewItem gridViewItem)
//            {
//                return gridViewItem.ContentTemplateRoot as FrameworkElement;
//            }
//            return null;
//        }

//        #endregion
//    }
//}

using Core_FileManagement;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ufm
{
    public class RenameService : IRenameService
    {
        #region Поля

        private bool _isMultiRenameMode = false;
        private List<string> _multiRenamePaths;
        private int _multiRenameCurrentIndex;
        private ObservableCollection<ExplorerItemViewModel> _items;
        private ListViewBase _itemsControl;
        private const string MultiRenameLogPrefix = "[MultiRename]";
        private DispatcherQueue _dispatcherQueue;

        #endregion

        #region Свойства

        public bool IsMultiRenameMode => _isMultiRenameMode;
        public bool IsEditing { get; private set; }

        #endregion

        #region События

        public event EventHandler<ExplorerItemViewModel> RenameStarted;
        public event EventHandler RenameCompleted;

        #endregion

        #region Конструктор

        public RenameService()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        }

        #endregion

        #region Публичные методы

        public void StartSingleRename(ExplorerItemViewModel item, ListViewBase itemsControl)
        {
            try
            {
                if (item == null || item.IsEditing)
                    return;

                _itemsControl = itemsControl;
                IsEditing = true;

                var container = GetContainerFromItem(itemsControl, item);

                if (container != null)
                {
                    var tile = GetContentTemplateRootFromContainer(container) as BaseTileControl;
                    if (tile != null && tile.CanEdit)
                    {
                        tile.IsInMultiRenameMode = false;
                        tile.StartEditing();
                        Debug.WriteLine($"[RenameService] Rename started for: {item.Name}");
                        RenameStarted?.Invoke(this, item);
                    }
                }
                else
                {
                    itemsControl.ScrollIntoView(item);

                    _ = Task.Delay(100).ContinueWith(_ =>
                    {
                        _dispatcherQueue.TryEnqueue(() =>
                        {
                            container = GetContainerFromItem(itemsControl, item);
                            if (container != null)
                            {
                                var tile = GetContentTemplateRootFromContainer(container) as BaseTileControl;
                                if (tile != null && tile.CanEdit)
                                {
                                    tile.IsInMultiRenameMode = false;
                                    tile.StartEditing();
                                    Debug.WriteLine($"[RenameService] Rename started (delayed) for: {item.Name}");
                                    RenameStarted?.Invoke(this, item);
                                }
                            }
                        });
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RenameService] Error in StartSingleRename: {ex}");
                IsEditing = false;
            }
        }

        public void StartMultiRename(IEnumerable<ExplorerItemViewModel> selectedItems,
                                    ObservableCollection<ExplorerItemViewModel> items,
                                    ListViewBase itemsControl)
        {
            Debug.WriteLine($"[RenameService] {MultiRenameLogPrefix} STARTING multi-rename for {selectedItems.Count()} items");

            _items = items;
            _itemsControl = itemsControl;
            _multiRenamePaths = selectedItems
                .OrderBy(item => items.IndexOf(item))
                .Select(item => item.FilePath)
                .ToList();

            _multiRenameCurrentIndex = 0;
            _isMultiRenameMode = true;
            IsEditing = true;

            BeginRenameForCurrentMultiItem();
        }

        public void HandleEditCompleted(EditResult result, ExplorerItemViewModel editedItem)
        {
            if (!_isMultiRenameMode)
            {
                IsEditing = false;
                RenameCompleted?.Invoke(this, EventArgs.Empty);
                return;
            }

            if (_multiRenamePaths == null || _multiRenameCurrentIndex < 0 || _multiRenameCurrentIndex >= _multiRenamePaths.Count)
            {
                Debug.WriteLine($"[RenameService] {MultiRenameLogPrefix} ERROR: Invalid state, finishing");
                FinishMultiRename();
                return;
            }

            switch (result)
            {
                case EditResult.Saved:
                    Debug.WriteLine($"[RenameService] {MultiRenameLogPrefix} Item saved, moving to next");
                    _multiRenamePaths.RemoveAt(_multiRenameCurrentIndex);
                    BeginRenameForCurrentMultiItem();
                    break;

                case EditResult.Cancelled:
                    // Короткое нажатие Escape - пропускаем текущий элемент
                    Debug.WriteLine($"[RenameService] {MultiRenameLogPrefix} Item skipped by Escape, moving to next");
                    _multiRenameCurrentIndex++;
                    BeginRenameForCurrentMultiItem();
                    break;

                case EditResult.CancelAll:
                    // Длительное удержание Escape - полная отмена всей последовательности
                    Debug.WriteLine($"[RenameService] {MultiRenameLogPrefix} Multi-rename cancelled by long Escape");
                    FinishMultiRename();
                    break;

                case EditResult.Error:
                default:
                    Debug.WriteLine($"[RenameService] {MultiRenameLogPrefix} Error, finishing sequence");
                    FinishMultiRename();
                    break;
            }
        }

        public void CancelEditing()
        {
            if (_isMultiRenameMode)
            {
                FinishMultiRename();
            }
            else
            {
                IsEditing = false;
            }
        }

        #endregion

        #region Приватные методы

        private void BeginRenameForCurrentMultiItem()
        {
            if (!_isMultiRenameMode || _multiRenamePaths == null)
            {
                FinishMultiRename();
                return;
            }

            if (_multiRenameCurrentIndex >= _multiRenamePaths.Count)
            {
                Debug.WriteLine($"[RenameService] {MultiRenameLogPrefix} Completed all items");
                FinishMultiRename();
                return;
            }

            string targetPath = _multiRenamePaths[_multiRenameCurrentIndex];

            var item = _items.FirstOrDefault(x => x.FilePath == targetPath);
            if (item == null)
            {
                Debug.WriteLine($"[RenameService] {MultiRenameLogPrefix} WARNING: Item not found, skipping");
                _multiRenameCurrentIndex++;
                BeginRenameForCurrentMultiItem();
                return;
            }

            _itemsControl.SelectedItems.Clear();
            _itemsControl.SelectedItems.Add(item);
            _itemsControl.SelectedItem = item;

            var container = GetContainerFromItem(_itemsControl, item);
            if (container != null)
            {
                var tile = GetContentTemplateRootFromContainer(container) as BaseTileControl;
                if (tile != null)
                {
                    tile.IsInMultiRenameMode = true;
                    Debug.WriteLine($"[RenameService] {MultiRenameLogPrefix} Starting rename for [{_multiRenameCurrentIndex}]: {item.Name}");
                    tile.StartEditing();
                    RenameStarted?.Invoke(this, item);
                }
                else
                {
                    Debug.WriteLine($"[RenameService] {MultiRenameLogPrefix} ERROR: Tile is null");
                    _multiRenameCurrentIndex++;
                    BeginRenameForCurrentMultiItem();
                }
            }
            else
            {
                _itemsControl.ScrollIntoView(item);

                _ = Task.Delay(100).ContinueWith(_ =>
                {
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        container = GetContainerFromItem(_itemsControl, item);
                        if (container != null)
                        {
                            var tile = GetContentTemplateRootFromContainer(container) as BaseTileControl;
                            if (tile != null)
                            {
                                tile.IsInMultiRenameMode = true;
                                Debug.WriteLine($"[RenameService] {MultiRenameLogPrefix} Starting rename (delayed) for [{_multiRenameCurrentIndex}]: {item.Name}");
                                tile.StartEditing();
                                RenameStarted?.Invoke(this, item);
                            }
                            else
                            {
                                Debug.WriteLine($"[RenameService] {MultiRenameLogPrefix} Retry: tile is null, skipping");
                                _multiRenameCurrentIndex++;
                                BeginRenameForCurrentMultiItem();
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"[RenameService] {MultiRenameLogPrefix} Retry: container not found, skipping");
                            _multiRenameCurrentIndex++;
                            BeginRenameForCurrentMultiItem();
                        }
                    });
                });
            }
        }

        private void FinishMultiRename()
        {
            Debug.WriteLine($"[RenameService] {MultiRenameLogPrefix} FINISHING multi-rename");

            _isMultiRenameMode = false;
            _multiRenamePaths = null;
            _multiRenameCurrentIndex = 0;
            IsEditing = false;

            _dispatcherQueue.TryEnqueue(() =>
            {
                _itemsControl?.Focus(FocusState.Programmatic);
            });

            RenameCompleted?.Invoke(this, EventArgs.Empty);
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
    }
}