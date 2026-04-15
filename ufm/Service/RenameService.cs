using Core_FileManagement;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ufm
{
    public class RenameService : IRenameService
    {
        private bool _isMultiRenameMode = false;
        private List<string> _multiRenamePaths;
        private int _multiRenameCurrentIndex;
        private ObservableCollection<ExplorerItemViewModel> _items;
        private ListViewBase _itemsControl;
        private DispatcherQueue _dispatcherQueue;

        public bool IsMultiRenameMode => _isMultiRenameMode;
        public bool IsEditing { get; private set; }

        public event EventHandler<ExplorerItemViewModel> RenameStarted;
        public event EventHandler RenameCompleted;

        public RenameService()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        }

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
                                    RenameStarted?.Invoke(this, item);
                                }
                            }
                        });
                    });
                }
            }
            catch (Exception)
            {
                IsEditing = false;
            }
        }

        public void StartMultiRename(IEnumerable<ExplorerItemViewModel> selectedItems,
                                    ObservableCollection<ExplorerItemViewModel> items,
                                    ListViewBase itemsControl)
        {
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
                FinishMultiRename();
                return;
            }

            switch (result)
            {
                case EditResult.Saved:
                    _multiRenamePaths.RemoveAt(_multiRenameCurrentIndex);
                    BeginRenameForCurrentMultiItem();
                    break;

                case EditResult.Cancelled:
                    _multiRenameCurrentIndex++;
                    BeginRenameForCurrentMultiItem();
                    break;

                case EditResult.CancelAll:
                case EditResult.Error:
                default:
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

        private void BeginRenameForCurrentMultiItem()
        {
            if (!_isMultiRenameMode || _multiRenamePaths == null)
            {
                FinishMultiRename();
                return;
            }

            if (_multiRenameCurrentIndex >= _multiRenamePaths.Count)
            {
                FinishMultiRename();
                return;
            }

            string targetPath = _multiRenamePaths[_multiRenameCurrentIndex];

            var item = _items.FirstOrDefault(x => x.FilePath == targetPath);
            if (item == null)
            {
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
                    tile.StartEditing();
                    RenameStarted?.Invoke(this, item);
                }
                else
                {
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
                                tile.StartEditing();
                                RenameStarted?.Invoke(this, item);
                            }
                            else
                            {
                                _multiRenameCurrentIndex++;
                                BeginRenameForCurrentMultiItem();
                            }
                        }
                        else
                        {
                            _multiRenameCurrentIndex++;
                            BeginRenameForCurrentMultiItem();
                        }
                    });
                });
            }
        }

        private void FinishMultiRename()
        {
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
                return listView.ContainerFromItem(item) as FrameworkElement;
            else if (itemsControl is GridView gridView)
                return gridView.ContainerFromItem(item) as FrameworkElement;
            return null;
        }

        private FrameworkElement GetContentTemplateRootFromContainer(FrameworkElement container)
        {
            if (container is ListViewItem listViewItem)
                return listViewItem.ContentTemplateRoot as FrameworkElement;
            else if (container is GridViewItem gridViewItem)
                return gridViewItem.ContentTemplateRoot as FrameworkElement;
            return null;
        }
    }
}