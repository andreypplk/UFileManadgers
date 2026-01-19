//using Microsoft.UI.Xaml;
//using Microsoft.UI.Xaml.Controls;
//using Microsoft.UI.Xaml.Input;

//namespace ufm
//{
//    public sealed partial class TileMyPcUC : BaseTileControl
//    {
//        private ScaleAnimator _scaleAnimator;

//        public TileMyPcUC()
//        {
//            this.InitializeComponent();

//            // Инициализируем аниматор
//            _scaleAnimator = new ScaleAnimator(BorderTileMyPcUC);
//        }

//        protected override void OnDisplayModeChanged()
//        {
//            base.OnDisplayModeChanged();

//            if (DisplayMode == "Vertical")
//            {
//                HorizontalLayout.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
//                VerticalLayout.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
//            }
//            else
//            {
//                HorizontalLayout.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
//                VerticalLayout.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
//            }
//        }
//    }
//}


//using Core_FileManagement;
//using Microsoft.UI.Xaml;
//using Microsoft.UI.Xaml.Controls;
//using Microsoft.UI.Xaml.Input;
//using System;
//using System.Diagnostics;
//using Windows.System;

//namespace ufm
//{
//    public sealed partial class TileMyPcUC : BaseTileControl
//    {
//        private ScaleAnimator _scaleAnimator;
//        private string _originalText = "";
//        private ExplorerItemViewModel _viewModel;
//        private bool _isInEditMode = false;

//        public TileMyPcUC()
//        {
//            this.InitializeComponent();

//            // Инициализируем аниматор
//            _scaleAnimator = new ScaleAnimator(BorderTileMyPcUC);
//        }

//        // Переопределяем StartEditing
//        public override void StartEditing()
//        {
//            try
//            {
//                Debug.WriteLine($"[TileMyPcUC] StartEditing called");

//                if (_isInEditMode)
//                {
//                    Debug.WriteLine($"[TileMyPcUC] Already in edit mode, skipping");
//                    return;
//                }

//                // Получаем ViewModel из DataContext
//                _viewModel = this.DataContext as ExplorerItemViewModel;
//                if (_viewModel == null)
//                {
//                    Debug.WriteLine($"[TileMyPcUC] ViewModel is null");
//                    return;
//                }

//                _isInEditMode = true;
//                _originalText = _viewModel.Name;

//                Debug.WriteLine($"[TileMyPcUC] Starting edit for: {_originalText}");

//                // Скрываем обычные текстовые блоки
//                HorizontalText.Visibility = Visibility.Collapsed;
//                VerticalText.Visibility = Visibility.Collapsed;
//                ListTextBlock.Visibility = Visibility.Collapsed;

//                // Показываем поля редактирования
//                HorizontalEditBox.Visibility = Visibility.Visible;
//                VerticalEditBox.Visibility = Visibility.Visible;
//                ListEditBox.Visibility = Visibility.Visible;

//                // Устанавливаем текст в TextBox
//                string currentText = _viewModel.Name;
//                HorizontalEditBox.Text = currentText;
//                VerticalEditBox.Text = currentText;
//                ListEditBox.Text = currentText;

//                // Устанавливаем фокус
//                this.DispatcherQueue.TryEnqueue(() =>
//                {
//                    TextBox editBox = GetCurrentEditBox();
//                    if (editBox != null)
//                    {
//                        editBox.Focus(FocusState.Programmatic);
//                        editBox.SelectAll();
//                        Debug.WriteLine($"[TileMyPcUC] Focus set to edit box");
//                    }
//                });
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyPcUC] Error in StartEditing: {ex.Message}");
//                _isInEditMode = false;
//            }
//        }

//        // Переопределяем StopEditing
//        public override void StopEditing()
//        {
//            try
//            {
//                Debug.WriteLine($"[TileMyPcUC] StopEditing called");

//                if (!_isInEditMode) return;

//                string newText = GetCurrentEditBox()?.Text?.Trim() ?? "";

//                if (!string.IsNullOrEmpty(newText) && newText != _originalText)
//                {
//                    Debug.WriteLine($"[TileMyPcUC] Saving changes: {_originalText} -> {newText}");
//                    SaveChanges(newText);
//                }
//                else
//                {
//                    Debug.WriteLine($"[TileMyPcUC] No changes to save");
//                }

//                FinishEditing();
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyPcUC] Error in StopEditing: {ex.Message}");
//                FinishEditing();
//            }
//        }

//        // Переопределяем CancelEditing
//        public override void CancelEditing()
//        {
//            try
//            {
//                Debug.WriteLine($"[TileMyPcUC] CancelEditing called");

//                if (!_isInEditMode) return;

//                Debug.WriteLine($"[TileMyPcUC] Cancelling changes, restoring: {_originalText}");

//                if (_viewModel != null && !string.IsNullOrEmpty(_originalText))
//                {
//                    _viewModel.Name = _originalText;
//                }

//                FinishEditing();
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyPcUC] Error in CancelEditing: {ex.Message}");
//                FinishEditing();
//            }
//        }

//        // Переопределяем CanEdit
//        public override bool CanEdit => true;

//        private void SaveChanges(string newText)
//        {
//            try
//            {
//                if (_viewModel != null)
//                {
//                    _viewModel.Name = newText;

//                    // Вызываем команду сохранения если она доступна
//                    if (_viewModel.SaveEditCommand?.CanExecute(null) == true)
//                    {
//                        _viewModel.SaveEditCommand.Execute(null);
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyPcUC] Error saving changes: {ex.Message}");
//            }
//        }

//        private void FinishEditing()
//        {
//            try
//            {
//                Debug.WriteLine($"[TileMyPcUC] FinishEditing called");

//                _isInEditMode = false;

//                // Показываем обычные текстовые блоки
//                HorizontalText.Visibility = Visibility.Visible;
//                VerticalText.Visibility = Visibility.Visible;
//                ListTextBlock.Visibility = Visibility.Visible;

//                // Скрываем поля редактирования
//                HorizontalEditBox.Visibility = Visibility.Collapsed;
//                VerticalEditBox.Visibility = Visibility.Collapsed;
//                ListEditBox.Visibility = Visibility.Collapsed;

//                _viewModel = null;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyPcUC] Error in FinishEditing: {ex.Message}");
//                _isInEditMode = false;
//                _viewModel = null;
//            }
//        }

//        // Обработчики событий для TextBox
//        public void EditTextBox_Loaded(object sender, RoutedEventArgs e)
//        {
//            // Автоматическая фокусировка не нужна - она делается в StartEditing
//        }

//        public void EditTextBox_LostFocus(object sender, RoutedEventArgs e)
//        {
//            if (_isInEditMode)
//            {
//                Debug.WriteLine($"[TileMyPcUC] EditTextBox_LostFocus - saving changes");
//                StopEditing();
//            }
//        }

//        public void EditTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
//        {
//            if (!_isInEditMode) return;

//            Debug.WriteLine($"[TileMyPcUC] EditTextBox_KeyDown: {e.Key}");

//            switch (e.Key)
//            {
//                case VirtualKey.Enter:
//                    e.Handled = true;
//                    Debug.WriteLine($"[TileMyPcUC] Enter pressed - saving");
//                    StopEditing();
//                    break;

//                case VirtualKey.Escape:
//                    e.Handled = true;
//                    Debug.WriteLine($"[TileMyPcUC] Escape pressed - cancelling");
//                    CancelEditing();
//                    break;

//                case VirtualKey.Tab:
//                    e.Handled = true;
//                    Debug.WriteLine($"[TileMyPcUC] Tab pressed - saving");
//                    StopEditing();
//                    break;
//            }
//        }

//        protected override void OnDisplayModeChanged()
//        {
//            base.OnDisplayModeChanged();

//            if (DisplayMode == "Vertical")
//            {
//                HorizontalLayout.Visibility = Visibility.Collapsed;
//                VerticalLayout.Visibility = Visibility.Visible;
//                ListLayout.Visibility = Visibility.Collapsed;
//            }
//            else if (DisplayMode == "List")
//            {
//                HorizontalLayout.Visibility = Visibility.Collapsed;
//                VerticalLayout.Visibility = Visibility.Collapsed;
//                ListLayout.Visibility = Visibility.Visible;
//            }
//            else
//            {
//                HorizontalLayout.Visibility = Visibility.Visible;
//                VerticalLayout.Visibility = Visibility.Collapsed;
//                ListLayout.Visibility = Visibility.Collapsed;
//            }
//        }

//        private TextBox GetCurrentEditBox()
//        {
//            switch (DisplayMode)
//            {
//                case "Horizontal":
//                    return HorizontalEditBox;
//                case "Vertical":
//                    return VerticalEditBox;
//                case "List":
//                    return ListEditBox;
//                default:
//                    return HorizontalEditBox;
//            }
//        }
//    }
//}


//using Core_FileManagement;
//using Microsoft.UI.Xaml;
//using Microsoft.UI.Xaml.Controls;
//using Microsoft.UI.Xaml.Input;
//using System;
//using System.Diagnostics;
//using Windows.System;

//namespace ufm
//{
//    public sealed partial class TileMyPcUC : BaseTileControl
//    {
//        private ScaleAnimator _scaleAnimator;
//        private string _originalText = "";
//        private ExplorerItemViewModel _viewModel;
//        private bool _isInEditMode = false;

//        public TileMyPcUC()
//        {
//            this.InitializeComponent();

//            // Инициализируем аниматор
//            _scaleAnimator = new ScaleAnimator(BorderTileMyPcUC);
//        }

//        // Переопределяем StartEditing
//        public override void StartEditing()
//        {
//            try
//            {
//                Debug.WriteLine($"[TileMyPcUC] StartEditing called");

//                if (_isInEditMode)
//                {
//                    Debug.WriteLine($"[TileMyPcUC] Already in edit mode, skipping");
//                    return;
//                }

//                // Получаем ViewModel из DataContext
//                _viewModel = this.DataContext as ExplorerItemViewModel;
//                if (_viewModel == null)
//                {
//                    Debug.WriteLine($"[TileMyPcUC] ViewModel is null");
//                    return;
//                }

//                _isInEditMode = true;
//                _originalText = _viewModel.Name;

//                Debug.WriteLine($"[TileMyPcUC] Starting edit for: {_originalText}");

//                // Инициализируем временное свойство в ViewModel
//                _viewModel.NewNameForEdit = _originalText;

//                // Скрываем обычные текстовые блоки
//                HorizontalText.Visibility = Visibility.Collapsed;
//                VerticalText.Visibility = Visibility.Collapsed;
//                ListTextBlock.Visibility = Visibility.Collapsed;

//                // Показываем поля редактирования
//                HorizontalEditBox.Visibility = Visibility.Visible;
//                VerticalEditBox.Visibility = Visibility.Visible;
//                ListEditBox.Visibility = Visibility.Visible;

//                // Устанавливаем текст в TextBox
//                string currentText = _viewModel.NewNameForEdit; // Используем временное свойство
//                HorizontalEditBox.Text = currentText;
//                VerticalEditBox.Text = currentText;
//                ListEditBox.Text = currentText;

//                // Устанавливаем фокус
//                this.DispatcherQueue.TryEnqueue(() =>
//                {
//                    TextBox editBox = GetCurrentEditBox();
//                    if (editBox != null)
//                    {
//                        editBox.Focus(FocusState.Programmatic);
//                        editBox.SelectAll();
//                        Debug.WriteLine($"[TileMyPcUC] Focus set to edit box");
//                    }
//                });
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyPcUC] Error in StartEditing: {ex.Message}");
//                _isInEditMode = false;
//            }
//        }

//        // Переопределяем StopEditing
//        public override void StopEditing()
//        {
//            try
//            {
//                Debug.WriteLine($"[TileMyPcUC] StopEditing called");

//                if (!_isInEditMode) return;

//                string newText = GetCurrentEditBox()?.Text?.Trim() ?? "";

//                if (!string.IsNullOrEmpty(newText) && newText != _originalText)
//                {
//                    Debug.WriteLine($"[TileMyPcUC] Saving changes: {_originalText} -> {newText}");
//                    SaveChanges(newText);
//                }
//                else
//                {
//                    Debug.WriteLine($"[TileMyPcUC] No changes to save");
//                }

//                FinishEditing();
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyPcUC] Error in StopEditing: {ex.Message}");
//                FinishEditing();
//            }
//        }

//        // Переопределяем CancelEditing
//        public override void CancelEditing()
//        {
//            try
//            {
//                Debug.WriteLine($"[TileMyPcUC] CancelEditing called");

//                if (!_isInEditMode) return;

//                Debug.WriteLine($"[TileMyPcUC] Cancelling changes, restoring: {_originalText}");

//                if (_viewModel != null && !string.IsNullOrEmpty(_originalText))
//                {
//                    _viewModel.Name = _originalText;
//                }

//                FinishEditing();
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyPcUC] Error in CancelEditing: {ex.Message}");
//                FinishEditing();
//            }
//        }

//        // Переопределяем CanEdit
//        public override bool CanEdit => true;

//        private void SaveChanges(string newText)
//        {
//            try
//            {
//                if (_viewModel != null)
//                {
//                    Debug.WriteLine($"[TileMyPcUC] SaveChanges called with: '{newText}'");

//                    // Устанавливаем новое имя во временное свойство ViewModel
//                    _viewModel.NewNameForEdit = newText;

//                    Debug.WriteLine($"[TileMyPcUC] NewNameForEdit set to: '{_viewModel.NewNameForEdit}'");
//                    Debug.WriteLine($"[TileMyPcUC] Original name: '{_originalText}'");
//                    Debug.WriteLine($"[TileMyPcUC] IsEditing: {_viewModel.IsEditing}");

//                    // Вызываем команду сохранения
//                    if (_viewModel.SaveEditCommand?.CanExecute(null) == true)
//                    {
//                        Debug.WriteLine($"[TileMyPcUC] SaveEditCommand can execute, calling Execute");
//                        _viewModel.SaveEditCommand.Execute(null);
//                    }
//                    else
//                    {
//                        Debug.WriteLine($"[TileMyPcUC] SaveEditCommand cannot execute. Reasons:");
//                        Debug.WriteLine($"[TileMyPcUC]   IsEditing: {_viewModel.IsEditing}");
//                        Debug.WriteLine($"[TileMyPcUC]   NewNameForEdit is null/empty: {string.IsNullOrEmpty(_viewModel.NewNameForEdit?.Trim())}");
//                        Debug.WriteLine($"[TileMyPcUC]   NewNameForEdit == Original: {_viewModel.NewNameForEdit?.Trim() == _originalText}");

//                        // Альтернативный способ: все равно обновляем имя
//                        if (!string.IsNullOrEmpty(newText) && newText != _originalText)
//                        {
//                            Debug.WriteLine($"[TileMyPcUC] Falling back to direct name update");
//                            _viewModel.Name = newText;
//                            FinishEditing();
//                        }
//                    }
//                }
//                else
//                {
//                    Debug.WriteLine($"[TileMyPcUC] ViewModel is null, cannot save changes");
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyPcUC] Error in SaveChanges: {ex.Message}");
//            }
//        }

//        private void FinishEditing()
//        {
//            try
//            {
//                Debug.WriteLine($"[TileMyPcUC] FinishEditing called");

//                _isInEditMode = false;

//                // Показываем обычные текстовые блоки
//                HorizontalText.Visibility = Visibility.Visible;
//                VerticalText.Visibility = Visibility.Visible;
//                ListTextBlock.Visibility = Visibility.Visible;

//                // Скрываем поля редактирования
//                HorizontalEditBox.Visibility = Visibility.Collapsed;
//                VerticalEditBox.Visibility = Visibility.Collapsed;
//                ListEditBox.Visibility = Visibility.Collapsed;

//                _viewModel = null;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyPcUC] Error in FinishEditing: {ex.Message}");
//                _isInEditMode = false;
//                _viewModel = null;
//            }
//        }

//        // Обработчики событий для TextBox
//        public void EditTextBox_Loaded(object sender, RoutedEventArgs e)
//        {
//            // Автоматическая фокусировка не нужна - она делается в StartEditing
//        }

//        public void EditTextBox_LostFocus(object sender, RoutedEventArgs e)
//        {
//            if (_isInEditMode)
//            {
//                Debug.WriteLine($"[TileMyPcUC] EditTextBox_LostFocus - saving changes");
//                StopEditing();
//            }
//        }

//        public void EditTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
//        {
//            if (!_isInEditMode) return;

//            Debug.WriteLine($"[TileMyPcUC] EditTextBox_KeyDown: {e.Key}");

//            switch (e.Key)
//            {
//                case VirtualKey.Enter:
//                    e.Handled = true;
//                    Debug.WriteLine($"[TileMyPcUC] Enter pressed - saving");

//                    // Обновляем NewNameForEdit перед сохранением
//                    TextBox textBox = sender as TextBox;
//                    if (textBox != null && _viewModel != null)
//                    {
//                        _viewModel.NewNameForEdit = textBox.Text;
//                    }

//                    StopEditing();
//                    break;

//                case VirtualKey.Escape:
//                    e.Handled = true;
//                    Debug.WriteLine($"[TileMyPcUC] Escape pressed - cancelling");
//                    CancelEditing();
//                    break;

//                case VirtualKey.Tab:
//                    e.Handled = true;
//                    Debug.WriteLine($"[TileMyPcUC] Tab pressed - saving");

//                    // Аналогично для Tab
//                    TextBox tabTextBox = sender as TextBox;
//                    if (tabTextBox != null && _viewModel != null)
//                    {
//                        _viewModel.NewNameForEdit = tabTextBox.Text;
//                    }

//                    StopEditing();
//                    break;
//            }
//        }

//        public void EditTextBox_TextChanged(object sender, TextChangedEventArgs e)
//        {
//            if (_isInEditMode && _viewModel != null)
//            {
//                TextBox textBox = sender as TextBox;
//                if (textBox != null)
//                {
//                    _viewModel.NewNameForEdit = textBox.Text;
//                    Debug.WriteLine($"[TileMyPcUC] TextChanged: NewNameForEdit = '{textBox.Text}'");
//                }
//            }
//        }

//        protected override void OnDisplayModeChanged()
//        {
//            base.OnDisplayModeChanged();

//            if (DisplayMode == "Vertical")
//            {
//                HorizontalLayout.Visibility = Visibility.Collapsed;
//                VerticalLayout.Visibility = Visibility.Visible;
//                ListLayout.Visibility = Visibility.Collapsed;
//            }
//            else if (DisplayMode == "List")
//            {
//                HorizontalLayout.Visibility = Visibility.Collapsed;
//                VerticalLayout.Visibility = Visibility.Collapsed;
//                ListLayout.Visibility = Visibility.Visible;
//            }
//            else
//            {
//                HorizontalLayout.Visibility = Visibility.Visible;
//                VerticalLayout.Visibility = Visibility.Collapsed;
//                ListLayout.Visibility = Visibility.Collapsed;
//            }
//        }

//        private TextBox GetCurrentEditBox()
//        {
//            switch (DisplayMode)
//            {
//                case "Horizontal":
//                    return HorizontalEditBox;
//                case "Vertical":
//                    return VerticalEditBox;
//                case "List":
//                    return ListEditBox;
//                default:
//                    return HorizontalEditBox;
//            }
//        }
//    }
//}


using Core_FileManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.System;

namespace ufm
{
    public sealed partial class TileMyPcUC : BaseTileControl
    {
        private ScaleAnimator _scaleAnimator;
        private string _originalText = "";
        private ExplorerItemViewModel _viewModel;
        private bool _isInEditMode = false;

        public TileMyPcUC()
        {
            this.InitializeComponent();

            // Инициализируем аниматор
            _scaleAnimator = new ScaleAnimator(BorderTileMyPcUC);
        }

        // Переопределяем StartEditing
        public override void StartEditing()
        {
            try
            {
                Debug.WriteLine($"[TileMyPcUC] StartEditing called");

                if (_isInEditMode)
                {
                    Debug.WriteLine($"[TileMyPcUC] Already in edit mode, skipping");
                    return;
                }

                // Получаем ViewModel из DataContext
                _viewModel = this.DataContext as ExplorerItemViewModel;
                if (_viewModel == null)
                {
                    Debug.WriteLine($"[TileMyPcUC] ViewModel is null");
                    return;
                }

                _isInEditMode = true;
                _originalText = _viewModel.Name;

                Debug.WriteLine($"[TileMyPcUC] Starting edit for: {_originalText}");

                // ВАЖНО: Устанавливаем IsEditing в ViewModel!
                _viewModel.IsEditing = true;
                _viewModel.EditRequested = true;

                // Инициализируем временное свойство в ViewModel
                _viewModel.NewNameForEdit = _originalText;

                // Скрываем обычные текстовые блоки
                HorizontalText.Visibility = Visibility.Collapsed;
                VerticalText.Visibility = Visibility.Collapsed;
                ListTextBlock.Visibility = Visibility.Collapsed;

                // Показываем поля редактирования
                HorizontalEditBox.Visibility = Visibility.Visible;
                VerticalEditBox.Visibility = Visibility.Visible;
                ListEditBox.Visibility = Visibility.Visible;

                // Устанавливаем текст в TextBox
                string currentText = _viewModel.NewNameForEdit;
                HorizontalEditBox.Text = currentText;
                VerticalEditBox.Text = currentText;
                ListEditBox.Text = currentText;

                // Устанавливаем фокус
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    TextBox editBox = GetCurrentEditBox();
                    if (editBox != null)
                    {
                        editBox.Focus(FocusState.Programmatic);
                        editBox.SelectAll();
                        Debug.WriteLine($"[TileMyPcUC] Focus set to edit box");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileMyPcUC] Error in StartEditing: {ex.Message}");
                _isInEditMode = false;
                if (_viewModel != null)
                {
                    _viewModel.IsEditing = false;
                    _viewModel.EditRequested = false;
                }
            }
        }

        // Переопределяем StopEditing
        public override void StopEditing()
        {
            try
            {
                Debug.WriteLine($"[TileMyPcUC] StopEditing called");

                if (!_isInEditMode) return;

                string newText = GetCurrentEditBox()?.Text?.Trim() ?? "";

                if (!string.IsNullOrEmpty(newText) && newText != _originalText)
                {
                    Debug.WriteLine($"[TileMyPcUC] Saving changes: {_originalText} -> {newText}");
                    SaveChanges(newText);
                }
                else
                {
                    Debug.WriteLine($"[TileMyPcUC] No changes to save");
                    FinishEditing();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileMyPcUC] Error in StopEditing: {ex.Message}");
                FinishEditing();
            }
        }

        // Переопределяем CancelEditing
        public override void CancelEditing()
        {
            try
            {
                Debug.WriteLine($"[TileMyPcUC] CancelEditing called");

                if (!_isInEditMode) return;

                Debug.WriteLine($"[TileMyPcUC] Cancelling changes, restoring: {_originalText}");

                if (_viewModel != null)
                {
                    // Восстанавливаем оригинальное имя в ViewModel
                    _viewModel.Name = _originalText;
                    _viewModel.NewNameForEdit = _originalText;
                    _viewModel.CancelEdit();
                }

                FinishEditing();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileMyPcUC] Error in CancelEditing: {ex.Message}");
                FinishEditing();
            }
        }

        // Переопределяем CanEdit
        public override bool CanEdit => true;

        private void SaveChanges(string newText)
        {
            try
            {
                if (_viewModel != null)
                {
                    Debug.WriteLine($"[TileMyPcUC] SaveChanges called with: '{newText}'");

                    // Устанавливаем новое имя во временное свойство ViewModel
                    _viewModel.NewNameForEdit = newText;

                    Debug.WriteLine($"[TileMyPcUC] NewNameForEdit set to: '{_viewModel.NewNameForEdit}'");
                    Debug.WriteLine($"[TileMyPcUC] Original name: '{_originalText}'");
                    Debug.WriteLine($"[TileMyPcUC] IsEditing: {_viewModel.IsEditing}");
                    Debug.WriteLine($"[TileMyPcUC] CanSaveEdit: {_viewModel.SaveEditCommand?.CanExecute(null)}");

                    // Вызываем команду сохранения
                    if (_viewModel.SaveEditCommand?.CanExecute(null) == true)
                    {
                        Debug.WriteLine($"[TileMyPcUC] SaveEditCommand can execute, calling Execute");
                        _viewModel.SaveEditCommand.Execute(null);
                        _ = DispatcherQueue.TryEnqueue(async () =>
                        {
                            await Task.Delay(10);
                            FinishEditing();
                        });
                    }
                    else
                    {
                        Debug.WriteLine($"[TileMyPcUC] SaveEditCommand cannot execute. Reasons:");
                        Debug.WriteLine($"[TileMyPcUC]   IsEditing: {_viewModel.IsEditing}");
                        Debug.WriteLine($"[TileMyPcUC]   NewNameForEdit is null/empty: {string.IsNullOrEmpty(_viewModel.NewNameForEdit?.Trim())}");
                        Debug.WriteLine($"[TileMyPcUC]   NewNameForEdit == Original: {_viewModel.NewNameForEdit?.Trim() == _originalText}");

                        // Если команда не может выполниться, завершаем редактирование
                        FinishEditing();
                    }
                }
                else
                {
                    Debug.WriteLine($"[TileMyPcUC] ViewModel is null, cannot save changes");
                    FinishEditing();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileMyPcUC] Error in SaveChanges: {ex.Message}");
                FinishEditing();
            }
        }

        private void FinishEditing()
        {
            try
            {
                Debug.WriteLine($"[TileMyPcUC] FinishEditing called");

                _isInEditMode = false;

                // Показываем обычные текстовые блоки
                HorizontalText.Visibility = Visibility.Visible;
                VerticalText.Visibility = Visibility.Visible;
                ListTextBlock.Visibility = Visibility.Visible;

                // Скрываем поля редактирования
                HorizontalEditBox.Visibility = Visibility.Collapsed;
                VerticalEditBox.Visibility = Visibility.Collapsed;
                ListEditBox.Visibility = Visibility.Collapsed;

                // Сбрасываем ViewModel
                if (_viewModel != null)
                {
                    _viewModel.EditRequested = false;
                    _viewModel = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileMyPcUC] Error in FinishEditing: {ex.Message}");
                _isInEditMode = false;
                _viewModel = null;
            }
        }

        // Обработчики событий для TextBox
        public void EditTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            // Автоматическая фокусировка не нужна - она делается в StartEditing
        }

        public void EditTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_isInEditMode)
            {
                Debug.WriteLine($"[TileMyPcUC] EditTextBox_LostFocus - saving changes");
                StopEditing();
            }
        }

        public void EditTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (!_isInEditMode) return;

            Debug.WriteLine($"[TileMyPcUC] EditTextBox_KeyDown: {e.Key}");

            switch (e.Key)
            {
                case VirtualKey.Enter:
                    e.Handled = true;
                    Debug.WriteLine($"[TileMyPcUC] Enter pressed - saving");

                    // Обновляем NewNameForEdit перед сохранением
                    TextBox textBox = sender as TextBox;
                    if (textBox != null && _viewModel != null)
                    {
                        _viewModel.NewNameForEdit = textBox.Text;
                    }

                    StopEditing();
                    break;

                case VirtualKey.Escape:
                    e.Handled = true;
                    Debug.WriteLine($"[TileMyPcUC] Escape pressed - cancelling");
                    CancelEditing();
                    break;

                case VirtualKey.Tab:
                    e.Handled = true;
                    Debug.WriteLine($"[TileMyPcUC] Tab pressed - saving");

                    // Аналогично для Tab
                    TextBox tabTextBox = sender as TextBox;
                    if (tabTextBox != null && _viewModel != null)
                    {
                        _viewModel.NewNameForEdit = tabTextBox.Text;
                    }

                    StopEditing();
                    break;
            }
        }

        public void EditTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInEditMode && _viewModel != null)
            {
                TextBox textBox = sender as TextBox;
                if (textBox != null)
                {
                    _viewModel.NewNameForEdit = textBox.Text;
                    Debug.WriteLine($"[TileMyPcUC] TextChanged: NewNameForEdit = '{textBox.Text}'");
                }
            }
        }

        protected override void OnDisplayModeChanged()
        {
            base.OnDisplayModeChanged();

            if (DisplayMode == "Vertical")
            {
                HorizontalLayout.Visibility = Visibility.Collapsed;
                VerticalLayout.Visibility = Visibility.Visible;
                ListLayout.Visibility = Visibility.Collapsed;
            }
            else if (DisplayMode == "List")
            {
                HorizontalLayout.Visibility = Visibility.Collapsed;
                VerticalLayout.Visibility = Visibility.Collapsed;
                ListLayout.Visibility = Visibility.Visible;
            }
            else
            {
                HorizontalLayout.Visibility = Visibility.Visible;
                VerticalLayout.Visibility = Visibility.Collapsed;
                ListLayout.Visibility = Visibility.Collapsed;
            }
        }

        private TextBox GetCurrentEditBox()
        {
            switch (DisplayMode)
            {
                case "Horizontal":
                    return HorizontalEditBox;
                case "Vertical":
                    return VerticalEditBox;
                case "List":
                    return ListEditBox;
                default:
                    return HorizontalEditBox;
            }
        }
    }
}