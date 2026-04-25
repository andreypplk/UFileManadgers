using Core_FileManagement;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace ufm
{
    public class ClickService : IClickService
    {
        #region Поля

        private DateTime _lastClickTime = DateTime.MinValue;
        private ExplorerItemViewModel _lastClickedItem = null;
        private bool _wasClickHandled = false;

        private const int CLICK_THRESHOLD_MS = 300;
        private const int DOUBLE_CLICK_THRESHOLD_MS = 500;

        private DispatcherQueue _dispatcherQueue;

        #endregion

        #region События

        // Событие без аргументов - просто уведомление
        public event EventHandler ItemOpenRequested;

        #endregion

        #region Конструктор

        public ClickService()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        }

        #endregion

        #region Публичные методы

        public bool HandleItemClick(ExplorerItemViewModel clickedItem, int clickedIndex,
                                   ListViewBase itemsControl, bool isSingleClickMode,
                                   bool isCtrlPressed, bool isShiftPressed,
                                   IKeyboardSelectionService keyboardSelection)
        {
            var now = DateTime.Now;
            if ((now - _lastClickTime).TotalMilliseconds < CLICK_THRESHOLD_MS)
                return false;

            _lastClickTime = now;

            if (clickedItem == null || clickedIndex < 0)
                return false;

            if (!isSingleClickMode)
            {
                if (_wasClickHandled)
                    return false;
                _wasClickHandled = true;
            }

            if (isSingleClickMode)
            {
                if (isShiftPressed)
                {
                    keyboardSelection.HandleShiftClick(clickedIndex, itemsControl,
                        (ObservableCollection<ExplorerItemViewModel>)itemsControl.ItemsSource,
                        keyboardSelection.ShiftSelectionStartItem, isCtrlPressed);
                    return false;
                }
                else if (isCtrlPressed)
                {
                    keyboardSelection.ToggleSelection(clickedItem, itemsControl);
                    return false;
                }
                else
                {
                    keyboardSelection.SetSingleSelection(clickedItem, itemsControl);

                    // Просто уведомляем, что нужно открыть элемент
                    // Индекс уже передан через параметр clickedIndex в вызывающем методе
                    ItemOpenRequested?.Invoke(this, EventArgs.Empty);
                    return true;
                }
            }
            else
            {
                if (isShiftPressed)
                {
                    keyboardSelection.HandleShiftClick(clickedIndex, itemsControl,
                        (ObservableCollection<ExplorerItemViewModel>)itemsControl.ItemsSource,
                        keyboardSelection.ShiftSelectionStartItem, isCtrlPressed);
                    return false;
                }
                else if (isCtrlPressed)
                {
                    keyboardSelection.ToggleSelection(clickedItem, itemsControl);
                    return false;
                }
                else
                {
                    keyboardSelection.SetSingleSelection(clickedItem, itemsControl);

                    bool isDoubleClick = (_lastClickedItem == clickedItem &&
                                         (now - _lastClickTime).TotalMilliseconds < DOUBLE_CLICK_THRESHOLD_MS);

                    if (isDoubleClick)
                    {
                        _lastClickedItem = null;
                        _lastClickTime = DateTime.MinValue;

                        // Просто уведомляем, что нужно открыть элемент
                        ItemOpenRequested?.Invoke(this, EventArgs.Empty);
                        return true;
                    }
                    else
                    {
                        _lastClickedItem = clickedItem;
                        _lastClickTime = now;

                        _ = Task.Delay(300).ContinueWith(_ =>
                        {
                            _dispatcherQueue.TryEnqueue(() =>
                            {
                                _wasClickHandled = false;
                            });
                        });

                        return false;
                    }
                }
            }
        }

        public bool IsDoubleClick(ExplorerItemViewModel item)
        {
            var now = DateTime.Now;
            bool isDoubleClick = (_lastClickedItem == item &&
                                 (now - _lastClickTime).TotalMilliseconds < DOUBLE_CLICK_THRESHOLD_MS);

            if (isDoubleClick)
            {
                _lastClickedItem = null;
                _lastClickTime = DateTime.MinValue;
            }

            return isDoubleClick;
        }

        public void ResetClickState()
        {
            _lastClickedItem = null;
            _lastClickTime = DateTime.MinValue;
            _wasClickHandled = false;
        }

        #endregion
    }
}