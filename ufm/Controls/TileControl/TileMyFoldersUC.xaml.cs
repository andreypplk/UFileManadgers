//using System;
//using Microsoft.UI.Xaml;
//using Microsoft.UI.Xaml.Controls;
//using System.Diagnostics;

//namespace ufm
//{
//    public sealed partial class TileMyFoldersUc : BaseTileControl
//    {
//        private ScaleAnimator _scaleAnimator;

//        public TileMyFoldersUc()
//        {
//            Debug.WriteLine($"[TileMyFoldersUc] Конструктор вызван");
//            this.InitializeComponent();

//            if (BorderTileFolders != null)
//            {
//                Debug.WriteLine($"[TileMyFoldersUc] Инициализация ScaleAnimator");
//                _scaleAnimator = new ScaleAnimator(BorderTileFolders);
//            }
//            else
//            {
//                Debug.WriteLine($"[TileMyFoldersUc] Ошибка: BorderTileFolders не найден");
//            }
//        }

//        protected override void OnDisplayModeChanged()
//        {
//            Debug.WriteLine($"[TileMyFoldersUc] OnDisplayModeChanged: DisplayMode = {DisplayMode}");
//            base.OnDisplayModeChanged();

//            try
//            {
//                if (HorizontalLayout == null || VerticalLayout == null)
//                {
//                    Debug.WriteLine($"[TileMyFoldersUc] Ошибка: Макеты не инициализированы");
//                    return;
//                }

//                // Переключение между режимами
//                if (DisplayMode == "Vertical")
//                {
//                    Debug.WriteLine($"[TileMyFoldersUc] Установка вертикального режима");
//                    HorizontalLayout.Visibility = Visibility.Collapsed;
//                    VerticalLayout.Visibility = Visibility.Visible;
//                }
//                else
//                {
//                    Debug.WriteLine($"[TileMyFoldersUc] Установка горизонтального режима");
//                    HorizontalLayout.Visibility = Visibility.Visible;
//                    VerticalLayout.Visibility = Visibility.Collapsed;
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyFoldersUc] Ошибка в OnDisplayModeChanged: {ex.Message}");
//            }
//        }

//        protected override void UpdateSize()
//        {
//            Debug.WriteLine($"[TileMyFoldersUc] UpdateSize: Size = {Size}");
//            base.UpdateSize();

//            try
//            {
//                if (FolderNameText == null || ItemsCountText == null || LastModifiedText == null)
//                {
//                    Debug.WriteLine($"[TileMyFoldersUc] Ошибка: Основные элементы не инициализированы");
//                    return;
//                }

//                Debug.WriteLine($"[TileMyFoldersUc] Текущие параметры: FontSize={FontSize}, IconWidth={IconWidth}, IconHeight={IconHeight}");

//                // Настройка видимости и размеров в зависимости от выбранного размера
//                switch (Size?.ToLower())
//                {
//                    case "extra small":
//                        Debug.WriteLine($"[TileMyFoldersUc] Установка размера Extra Small");
//                        SetElementsVisibility(false);
//                        break;

//                    case "small":
//                        Debug.WriteLine($"[TileMyFoldersUc] Установка размера Small");
//                        SetElementsVisibility(false);
//                        FolderNameText.VerticalAlignment = VerticalAlignment.Center;
//                        FolderNameText.Margin = new Thickness(10, 0, 8, 0);
//                        break;

//                    case "medium":
//                        Debug.WriteLine($"[TileMyFoldersUc] Установка размера Medium");
//                        SetElementsVisibility(true, showDetails: true, showAttributes: true);
//                        ItemsCountText.FontSize = 10;
//                        LastModifiedText.FontSize = 10;
//                        AttributesText.FontSize = 10;
//                        break;

//                    case "large":
//                        Debug.WriteLine($"[TileMyFoldersUc] Установка размера Large");
//                        SetElementsVisibility(true, showDetails: true, showAttributes: true);
//                        ItemsCountText.FontSize = 12;
//                        LastModifiedText.FontSize = 12;
//                        AttributesText.FontSize = 12;
//                        break;

//                    case "extra large":
//                        Debug.WriteLine($"[TileMyFoldersUc] Установка размера Extra Large");
//                        SetElementsVisibility(true, showDetails: true, showAttributes: true);
//                        FolderNameText.FontSize = 16;
//                        ItemsCountText.FontSize = 14;
//                        LastModifiedText.FontSize = 14;
//                        AttributesText.FontSize = 12;
//                        break;

//                    default:
//                        Debug.WriteLine($"[TileMyFoldersUc] Неизвестный размер: {Size}");
//                        SetElementsVisibility(true, showDetails: true, showAttributes: true);
//                        break;
//                }

//                Debug.WriteLine($"[TileMyFoldersUc] Обновленные параметры: " +
//                               $"FolderNameText.FontSize={FolderNameText?.FontSize}, " +
//                               $"ItemsCountText.Visibility={ItemsCountText?.Visibility}");
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyFoldersUc] Ошибка в UpdateSize: {ex.Message}");
//            }
//        }

//        private void SetElementsVisibility(
//            bool isVisible,
//            bool showDetails = true,
//            bool showAttributes = true)
//        {
//            try
//            {
//                var visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
//                Debug.WriteLine($"[TileMyFoldersUc] SetElementsVisibility: " +
//                               $"isVisible={isVisible}, showDetails={showDetails}, showAttributes={showAttributes}");

//                if (ItemsCountText != null)
//                {
//                    ItemsCountText.Visibility = visibility;
//                    Debug.WriteLine($"[TileMyFoldersUc] ItemsCountText.Visibility = {ItemsCountText.Visibility}");
//                }

//                if (LastModifiedText != null)
//                {
//                    LastModifiedText.Visibility = visibility;
//                    Debug.WriteLine($"[TileMyFoldersUc] LastModifiedText.Visibility = {LastModifiedText.Visibility}");
//                }

//                if (AttributesText != null)
//                {
//                    AttributesText.Visibility = showAttributes ? visibility : Visibility.Collapsed;
//                    Debug.WriteLine($"[TileMyFoldersUc] AttributesText.Visibility = {AttributesText.Visibility}");
//                }

//                if (!showDetails)
//                {
//                    if (ItemsCountText != null) ItemsCountText.Visibility = Visibility.Collapsed;
//                    if (LastModifiedText != null) LastModifiedText.Visibility = Visibility.Collapsed;
//                    if (AttributesText != null) AttributesText.Visibility = Visibility.Collapsed;
//                    Debug.WriteLine($"[TileMyFoldersUc] Все детали скрыты (showDetails=false)");
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyFoldersUc] Ошибка в SetElementsVisibility: {ex.Message}");
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
//    public sealed partial class TileMyFoldersUc : BaseTileControl
//    {
//        private ScaleAnimator _scaleAnimator;
//        private string _originalText = "";
//        private ExplorerItemViewModel _viewModel;
//        private bool _isInEditMode = false;

//        public TileMyFoldersUc()
//        {
//            Debug.WriteLine($"[TileMyFoldersUc] Конструктор вызван");
//            this.InitializeComponent();

//            if (BorderTileFolders != null)
//            {
//                Debug.WriteLine($"[TileMyFoldersUc] Инициализация ScaleAnimator");
//                _scaleAnimator = new ScaleAnimator(BorderTileFolders);
//            }
//            else
//            {
//                Debug.WriteLine($"[TileMyFoldersUc] Ошибка: BorderTileFolders не найден");
//            }
//        }

//        // Переопределяем StartEditing
//        public override void StartEditing()
//        {
//            try
//            {
//                Debug.WriteLine($"[TileMyFoldersUc] StartEditing called");

//                if (_isInEditMode)
//                {
//                    Debug.WriteLine($"[TileMyFoldersUc] Already in edit mode, skipping");
//                    return;
//                }

//                // Получаем ViewModel из DataContext
//                _viewModel = this.DataContext as ExplorerItemViewModel;
//                if (_viewModel == null)
//                {
//                    Debug.WriteLine($"[TileMyFoldersUc] ViewModel is null");
//                    return;
//                }

//                _isInEditMode = true;
//                _originalText = _viewModel.Name;

//                Debug.WriteLine($"[TileMyFoldersUc] Starting edit for: {_originalText}");

//                // Скрываем обычные текстовые блоки
//                HorizontalTextBlock.Visibility = Visibility.Collapsed;
//                VerticalTextBlock.Visibility = Visibility.Collapsed;
//                ListTextBlock.Visibility = Visibility.Collapsed;

//                // Показываем поля редактирования
//                HorizontalEditBox.Visibility = Visibility.Visible;
//                VerticalEditBox.Visibility = Visibility.Visible;
//                ListEditBox.Visibility = Visibility.Visible;

//                // Скрываем дополнительные элементы при редактировании
//                ItemsCountText.Visibility = Visibility.Collapsed;
//                LastModifiedText.Visibility = Visibility.Collapsed;
//                AttributesText.Visibility = Visibility.Collapsed;

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
//                        Debug.WriteLine($"[TileMyFoldersUc] Focus set to edit box");
//                    }
//                });
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyFoldersUc] Error in StartEditing: {ex.Message}");
//                _isInEditMode = false;
//            }
//        }

//        // Переопределяем StopEditing
//        public override void StopEditing()
//        {
//            try
//            {
//                Debug.WriteLine($"[TileMyFoldersUc] StopEditing called");

//                if (!_isInEditMode) return;

//                string newText = GetCurrentEditBox()?.Text?.Trim() ?? "";

//                if (!string.IsNullOrEmpty(newText) && newText != _originalText)
//                {
//                    Debug.WriteLine($"[TileMyFoldersUc] Saving changes: {_originalText} -> {newText}");
//                    SaveChanges(newText);
//                }
//                else
//                {
//                    Debug.WriteLine($"[TileMyFoldersUc] No changes to save");
//                }

//                FinishEditing();
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyFoldersUc] Error in StopEditing: {ex.Message}");
//                FinishEditing();
//            }
//        }

//        // Переопределяем CancelEditing
//        public override void CancelEditing()
//        {
//            try
//            {
//                Debug.WriteLine($"[TileMyFoldersUc] CancelEditing called");

//                if (!_isInEditMode) return;

//                Debug.WriteLine($"[TileMyFoldersUc] Cancelling changes, restoring: {_originalText}");

//                if (_viewModel != null && !string.IsNullOrEmpty(_originalText))
//                {
//                    _viewModel.Name = _originalText;
//                }

//                FinishEditing();
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyFoldersUc] Error in CancelEditing: {ex.Message}");
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
//                Debug.WriteLine($"[TileMyFoldersUc] Error saving changes: {ex.Message}");
//            }
//        }

//        private void FinishEditing()
//        {
//            try
//            {
//                Debug.WriteLine($"[TileMyFoldersUc] FinishEditing called");

//                _isInEditMode = false;

//                // Показываем обычные текстовые блоки
//                HorizontalTextBlock.Visibility = Visibility.Visible;
//                VerticalTextBlock.Visibility = Visibility.Visible;
//                ListTextBlock.Visibility = Visibility.Visible;

//                // Скрываем поля редактирования
//                HorizontalEditBox.Visibility = Visibility.Collapsed;
//                VerticalEditBox.Visibility = Visibility.Collapsed;
//                ListEditBox.Visibility = Visibility.Collapsed;

//                // Восстанавливаем дополнительные элементы
//                UpdateDetailsVisibility();

//                _viewModel = null;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyFoldersUc] Error in FinishEditing: {ex.Message}");
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
//                Debug.WriteLine($"[TileMyFoldersUc] EditTextBox_LostFocus - saving changes");
//                StopEditing();
//            }
//        }

//        public void EditTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
//        {
//            if (!_isInEditMode) return;

//            Debug.WriteLine($"[TileMyFoldersUc] EditTextBox_KeyDown: {e.Key}");

//            switch (e.Key)
//            {
//                case VirtualKey.Enter:
//                    e.Handled = true;
//                    Debug.WriteLine($"[TileMyFoldersUc] Enter pressed - saving");
//                    StopEditing();
//                    break;

//                case VirtualKey.Escape:
//                    e.Handled = true;
//                    Debug.WriteLine($"[TileMyFoldersUc] Escape pressed - cancelling");
//                    CancelEditing();
//                    break;

//                case VirtualKey.Tab:
//                    e.Handled = true;
//                    Debug.WriteLine($"[TileMyFoldersUc] Tab pressed - saving");
//                    StopEditing();
//                    break;
//            }
//        }

//        protected override void OnDisplayModeChanged()
//        {
//            Debug.WriteLine($"[TileMyFoldersUc] OnDisplayModeChanged: DisplayMode = {DisplayMode}");

//            try
//            {
//                if (HorizontalLayout == null || VerticalLayout == null || ListLayout == null)
//                {
//                    Debug.WriteLine($"[TileMyFoldersUc] Ошибка: Макеты не инициализированы");
//                    return;
//                }

//                if (DisplayMode == "Vertical")
//                {
//                    Debug.WriteLine($"[TileMyFoldersUc] Установка вертикального режима");
//                    HorizontalLayout.Visibility = Visibility.Collapsed;
//                    VerticalLayout.Visibility = Visibility.Visible;
//                    ListLayout.Visibility = Visibility.Collapsed;
//                }
//                else if (DisplayMode == "List")
//                {
//                    Debug.WriteLine($"[TileMyFoldersUc] Установка лист режима");
//                    HorizontalLayout.Visibility = Visibility.Collapsed;
//                    VerticalLayout.Visibility = Visibility.Collapsed;
//                    ListLayout.Visibility = Visibility.Visible;
//                }
//                else
//                {
//                    Debug.WriteLine($"[TileMyFoldersUc] Установка горизонтального режима");
//                    HorizontalLayout.Visibility = Visibility.Visible;
//                    VerticalLayout.Visibility = Visibility.Collapsed;
//                    ListLayout.Visibility = Visibility.Collapsed;
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyFoldersUc] Ошибка в OnDisplayModeChanged: {ex.Message}");
//            }
//        }

//        protected override void UpdateSize()
//        {
//            Debug.WriteLine($"[TileMyFoldersUc] UpdateSize: Size = {Size}");
//            base.UpdateSize();

//            UpdateDetailsVisibility();
//        }

//        private void UpdateDetailsVisibility()
//        {
//            if (_isInEditMode) return;

//            if (DisplayMode == "List")
//            {
//                SetElementsVisibility(false);
//                return;
//            }

//            switch (Size?.ToLower())
//            {
//                case "extra small":
//                    Debug.WriteLine($"[TileMyFoldersUc] Установка размера Extra Small");
//                    SetElementsVisibility(false);
//                    break;

//                case "small":
//                    Debug.WriteLine($"[TileMyFoldersUc] Установка размера Small");
//                    SetElementsVisibility(false);
//                    if (HorizontalTextBlock != null)
//                    {
//                        HorizontalTextBlock.VerticalAlignment = VerticalAlignment.Center;
//                        HorizontalTextBlock.Margin = new Thickness(10, 0, 8, 0);
//                    }
//                    break;

//                case "medium":
//                    Debug.WriteLine($"[TileMyFoldersUc] Установка размера Medium");
//                    SetElementsVisibility(true, showDetails: true, showAttributes: true);
//                    if (ItemsCountText != null) ItemsCountText.FontSize = 10;
//                    if (LastModifiedText != null) LastModifiedText.FontSize = 10;
//                    if (AttributesText != null) AttributesText.FontSize = 10;
//                    break;

//                case "large":
//                    Debug.WriteLine($"[TileMyFoldersUc] Установка размера Large");
//                    SetElementsVisibility(true, showDetails: true, showAttributes: true);
//                    if (ItemsCountText != null) ItemsCountText.FontSize = 12;
//                    if (LastModifiedText != null) LastModifiedText.FontSize = 12;
//                    if (AttributesText != null) AttributesText.FontSize = 12;
//                    break;

//                case "extra large":
//                    Debug.WriteLine($"[TileMyFoldersUc] Установка размера Extra Large");
//                    SetElementsVisibility(true, showDetails: true, showAttributes: true);
//                    if (HorizontalTextBlock != null) HorizontalTextBlock.FontSize = 16;
//                    if (VerticalTextBlock != null) VerticalTextBlock.FontSize = 16;
//                    if (ListTextBlock != null) ListTextBlock.FontSize = 16;
//                    if (ItemsCountText != null) ItemsCountText.FontSize = 14;
//                    if (LastModifiedText != null) LastModifiedText.FontSize = 14;
//                    if (AttributesText != null) AttributesText.FontSize = 12;
//                    break;

//                default:
//                    Debug.WriteLine($"[TileMyFoldersUc] Неизвестный размер: {Size}");
//                    SetElementsVisibility(true, showDetails: true, showAttributes: true);
//                    break;
//            }
//        }

//        private void SetElementsVisibility(
//            bool isVisible,
//            bool showDetails = true,
//            bool showAttributes = true)
//        {
//            try
//            {
//                var visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;

//                if (ItemsCountText != null)
//                {
//                    ItemsCountText.Visibility = showDetails ? visibility : Visibility.Collapsed;
//                }

//                if (LastModifiedText != null)
//                {
//                    LastModifiedText.Visibility = showDetails ? visibility : Visibility.Collapsed;
//                }

//                if (AttributesText != null)
//                {
//                    AttributesText.Visibility = showAttributes ? visibility : Visibility.Collapsed;
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyFoldersUc] Ошибка в SetElementsVisibility: {ex.Message}");
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
//    public sealed partial class TileMyFoldersUc : BaseTileControl
//    {
//        private ScaleAnimator _scaleAnimator;
//        private string _originalText = "";
//        private ExplorerItemViewModel _viewModel;
//        private bool _isInEditMode = false;

//        public TileMyFoldersUc()
//        {
//            Debug.WriteLine($"[TileMyFoldersUc] Конструктор вызван");
//            this.InitializeComponent();

//            if (BorderTileFolders != null)
//            {
//                Debug.WriteLine($"[TileMyFoldersUc] Инициализация ScaleAnimator");
//                _scaleAnimator = new ScaleAnimator(BorderTileFolders);
//            }
//            else
//            {
//                Debug.WriteLine($"[TileMyFoldersUc] Ошибка: BorderTileFolders не найден");
//            }
//        }

//        // Переопределяем StartEditing
//        public override void StartEditing()
//        {
//            try
//            {
//                Debug.WriteLine($"[TileMyFoldersUc] StartEditing called");

//                if (_isInEditMode)
//                {
//                    Debug.WriteLine($"[TileMyFoldersUc] Already in edit mode, skipping");
//                    return;
//                }

//                // Получаем ViewModel из DataContext
//                _viewModel = this.DataContext as ExplorerItemViewModel;
//                if (_viewModel == null)
//                {
//                    Debug.WriteLine($"[TileMyFoldersUc] ViewModel is null");
//                    return;
//                }

//                _isInEditMode = true;
//                _originalText = _viewModel.Name;

//                Debug.WriteLine($"[TileMyFoldersUc] Starting edit for: {_originalText}");

//                // Инициализируем временное свойство в ViewModel
//                _viewModel.NewNameForEdit = _originalText;

//                // Скрываем обычные текстовые блоки
//                HorizontalTextBlock.Visibility = Visibility.Collapsed;
//                VerticalTextBlock.Visibility = Visibility.Collapsed;
//                ListTextBlock.Visibility = Visibility.Collapsed;

//                // Показываем поля редактирования
//                HorizontalEditBox.Visibility = Visibility.Visible;
//                VerticalEditBox.Visibility = Visibility.Visible;
//                ListEditBox.Visibility = Visibility.Visible;

//                // Скрываем дополнительные элементы при редактировании
//                ItemsCountText.Visibility = Visibility.Collapsed;
//                LastModifiedText.Visibility = Visibility.Collapsed;
//                AttributesText.Visibility = Visibility.Collapsed;

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
//                        Debug.WriteLine($"[TileMyFoldersUc] Focus set to edit box");
//                    }
//                });
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyFoldersUc] Error in StartEditing: {ex.Message}");
//                _isInEditMode = false;
//            }
//        }

//        // Переопределяем StopEditing
//        public override void StopEditing()
//        {
//            try
//            {
//                Debug.WriteLine($"[TileMyFoldersUc] StopEditing called");

//                if (!_isInEditMode) return;

//                string newText = GetCurrentEditBox()?.Text?.Trim() ?? "";

//                if (!string.IsNullOrEmpty(newText) && newText != _originalText)
//                {
//                    Debug.WriteLine($"[TileMyFoldersUc] Saving changes: {_originalText} -> {newText}");
//                    SaveChanges(newText);
//                }
//                else
//                {
//                    Debug.WriteLine($"[TileMyFoldersUc] No changes to save");
//                }

//                FinishEditing();
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyFoldersUc] Error in StopEditing: {ex.Message}");
//                FinishEditing();
//            }
//        }

//        // Переопределяем CancelEditing
//        public override void CancelEditing()
//        {
//            try
//            {
//                Debug.WriteLine($"[TileMyFoldersUc] CancelEditing called");

//                if (!_isInEditMode) return;

//                Debug.WriteLine($"[TileMyFoldersUc] Cancelling changes, restoring: {_originalText}");

//                if (_viewModel != null && !string.IsNullOrEmpty(_originalText))
//                {
//                    _viewModel.Name = _originalText;
//                }

//                FinishEditing();
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyFoldersUc] Error in CancelEditing: {ex.Message}");
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
//                    Debug.WriteLine($"[TileMyFoldersUc] SaveChanges called with: '{newText}'");

//                    // Устанавливаем новое имя во временное свойство ViewModel
//                    _viewModel.NewNameForEdit = newText;

//                    Debug.WriteLine($"[TileMyFoldersUc] NewNameForEdit set to: '{_viewModel.NewNameForEdit}'");
//                    Debug.WriteLine($"[TileMyFoldersUc] Original name: '{_originalText}'");
//                    Debug.WriteLine($"[TileMyFoldersUc] IsEditing: {_viewModel.IsEditing}");

//                    // Вызываем команду сохранения
//                    if (_viewModel.SaveEditCommand?.CanExecute(null) == true)
//                    {
//                        Debug.WriteLine($"[TileMyFoldersUc] SaveEditCommand can execute, calling Execute");
//                        _viewModel.SaveEditCommand.Execute(null);
//                    }
//                    else
//                    {
//                        Debug.WriteLine($"[TileMyFoldersUc] SaveEditCommand cannot execute. Reasons:");
//                        Debug.WriteLine($"[TileMyFoldersUc]   IsEditing: {_viewModel.IsEditing}");
//                        Debug.WriteLine($"[TileMyFoldersUc]   NewNameForEdit is null/empty: {string.IsNullOrEmpty(_viewModel.NewNameForEdit?.Trim())}");
//                        Debug.WriteLine($"[TileMyFoldersUc]   NewNameForEdit == Original: {_viewModel.NewNameForEdit?.Trim() == _originalText}");

//                        // Альтернативный способ: все равно обновляем имя
//                        if (!string.IsNullOrEmpty(newText) && newText != _originalText)
//                        {
//                            Debug.WriteLine($"[TileMyFoldersUc] Falling back to direct name update");
//                            _viewModel.Name = newText;
//                            FinishEditing();
//                        }
//                    }
//                }
//                else
//                {
//                    Debug.WriteLine($"[TileMyFoldersUc] ViewModel is null, cannot save changes");
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyFoldersUc] Error in SaveChanges: {ex.Message}");
//            }
//        }

//        private void FinishEditing()
//        {
//            try
//            {
//                Debug.WriteLine($"[TileMyFoldersUc] FinishEditing called");

//                _isInEditMode = false;

//                // Показываем обычные текстовые блоки
//                HorizontalTextBlock.Visibility = Visibility.Visible;
//                VerticalTextBlock.Visibility = Visibility.Visible;
//                ListTextBlock.Visibility = Visibility.Visible;

//                // Скрываем поля редактирования
//                HorizontalEditBox.Visibility = Visibility.Collapsed;
//                VerticalEditBox.Visibility = Visibility.Collapsed;
//                ListEditBox.Visibility = Visibility.Collapsed;

//                // Восстанавливаем дополнительные элементы
//                UpdateDetailsVisibility();

//                _viewModel = null;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyFoldersUc] Error in FinishEditing: {ex.Message}");
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
//                Debug.WriteLine($"[TileMyFoldersUc] EditTextBox_LostFocus - saving changes");
//                StopEditing();
//            }
//        }

//        public void EditTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
//        {
//            if (!_isInEditMode) return;

//            Debug.WriteLine($"[TileMyFoldersUc] EditTextBox_KeyDown: {e.Key}");

//            switch (e.Key)
//            {
//                case VirtualKey.Enter:
//                    e.Handled = true;
//                    Debug.WriteLine($"[TileMyFoldersUc] Enter pressed - saving");

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
//                    Debug.WriteLine($"[TileMyFoldersUc] Escape pressed - cancelling");
//                    CancelEditing();
//                    break;

//                case VirtualKey.Tab:
//                    e.Handled = true;
//                    Debug.WriteLine($"[TileMyFoldersUc] Tab pressed - saving");

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
//                    Debug.WriteLine($"[TileMyFoldersUc] TextChanged: NewNameForEdit = '{textBox.Text}'");
//                }
//            }
//        }

//        protected override void OnDisplayModeChanged()
//        {
//            Debug.WriteLine($"[TileMyFoldersUc] OnDisplayModeChanged: DisplayMode = {DisplayMode}");

//            try
//            {
//                if (HorizontalLayout == null || VerticalLayout == null || ListLayout == null)
//                {
//                    Debug.WriteLine($"[TileMyFoldersUc] Ошибка: Макеты не инициализированы");
//                    return;
//                }

//                if (DisplayMode == "Vertical")
//                {
//                    Debug.WriteLine($"[TileMyFoldersUc] Установка вертикального режима");
//                    HorizontalLayout.Visibility = Visibility.Collapsed;
//                    VerticalLayout.Visibility = Visibility.Visible;
//                    ListLayout.Visibility = Visibility.Collapsed;
//                }
//                else if (DisplayMode == "List")
//                {
//                    Debug.WriteLine($"[TileMyFoldersUc] Установка лист режима");
//                    HorizontalLayout.Visibility = Visibility.Collapsed;
//                    VerticalLayout.Visibility = Visibility.Collapsed;
//                    ListLayout.Visibility = Visibility.Visible;
//                }
//                else
//                {
//                    Debug.WriteLine($"[TileMyFoldersUc] Установка горизонтального режима");
//                    HorizontalLayout.Visibility = Visibility.Visible;
//                    VerticalLayout.Visibility = Visibility.Collapsed;
//                    ListLayout.Visibility = Visibility.Collapsed;
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyFoldersUc] Ошибка в OnDisplayModeChanged: {ex.Message}");
//            }
//        }

//        protected override void UpdateSize()
//        {
//            Debug.WriteLine($"[TileMyFoldersUc] UpdateSize: Size = {Size}");
//            base.UpdateSize();

//            UpdateDetailsVisibility();
//        }

//        private void UpdateDetailsVisibility()
//        {
//            if (_isInEditMode) return;

//            if (DisplayMode == "List")
//            {
//                SetElementsVisibility(false);
//                return;
//            }

//            switch (Size?.ToLower())
//            {
//                case "extra small":
//                    Debug.WriteLine($"[TileMyFoldersUc] Установка размера Extra Small");
//                    SetElementsVisibility(false);
//                    break;

//                case "small":
//                    Debug.WriteLine($"[TileMyFoldersUc] Установка размера Small");
//                    SetElementsVisibility(false);
//                    if (HorizontalTextBlock != null)
//                    {
//                        HorizontalTextBlock.VerticalAlignment = VerticalAlignment.Center;
//                        HorizontalTextBlock.Margin = new Thickness(10, 0, 8, 0);
//                    }
//                    break;

//                case "medium":
//                    Debug.WriteLine($"[TileMyFoldersUc] Установка размера Medium");
//                    SetElementsVisibility(true, showDetails: true, showAttributes: true);
//                    if (ItemsCountText != null) ItemsCountText.FontSize = 10;
//                    if (LastModifiedText != null) LastModifiedText.FontSize = 10;
//                    if (AttributesText != null) AttributesText.FontSize = 10;
//                    break;

//                case "large":
//                    Debug.WriteLine($"[TileMyFoldersUc] Установка размера Large");
//                    SetElementsVisibility(true, showDetails: true, showAttributes: true);
//                    if (ItemsCountText != null) ItemsCountText.FontSize = 12;
//                    if (LastModifiedText != null) LastModifiedText.FontSize = 12;
//                    if (AttributesText != null) AttributesText.FontSize = 12;
//                    break;

//                case "extra large":
//                    Debug.WriteLine($"[TileMyFoldersUc] Установка размера Extra Large");
//                    SetElementsVisibility(true, showDetails: true, showAttributes: true);
//                    if (HorizontalTextBlock != null) HorizontalTextBlock.FontSize = 16;
//                    if (VerticalTextBlock != null) VerticalTextBlock.FontSize = 16;
//                    if (ListTextBlock != null) ListTextBlock.FontSize = 16;
//                    if (ItemsCountText != null) ItemsCountText.FontSize = 14;
//                    if (LastModifiedText != null) LastModifiedText.FontSize = 14;
//                    if (AttributesText != null) AttributesText.FontSize = 12;
//                    break;

//                default:
//                    Debug.WriteLine($"[TileMyFoldersUc] Неизвестный размер: {Size}");
//                    SetElementsVisibility(true, showDetails: true, showAttributes: true);
//                    break;
//            }
//        }

//        private void SetElementsVisibility(
//            bool isVisible,
//            bool showDetails = true,
//            bool showAttributes = true)
//        {
//            try
//            {
//                var visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;

//                if (ItemsCountText != null)
//                {
//                    ItemsCountText.Visibility = showDetails ? visibility : Visibility.Collapsed;
//                }

//                if (LastModifiedText != null)
//                {
//                    LastModifiedText.Visibility = showDetails ? visibility : Visibility.Collapsed;
//                }

//                if (AttributesText != null)
//                {
//                    AttributesText.Visibility = showAttributes ? visibility : Visibility.Collapsed;
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyFoldersUc] Ошибка в SetElementsVisibility: {ex.Message}");
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
    public sealed partial class TileMyFoldersUc : BaseTileControl
    {
        private ScaleAnimator _scaleAnimator;
        private string _originalText = "";
        private ExplorerItemViewModel _viewModel;
        private bool _isInEditMode = false;

        public TileMyFoldersUc()
        {
            Debug.WriteLine($"[TileMyFoldersUc] Конструктор вызван");
            this.InitializeComponent();

            if (BorderTileFolders != null)
            {
                Debug.WriteLine($"[TileMyFoldersUc] Инициализация ScaleAnimator");
                _scaleAnimator = new ScaleAnimator(BorderTileFolders);
            }
            else
            {
                Debug.WriteLine($"[TileMyFoldersUc] Ошибка: BorderTileFolders не найден");
            }
        }

        // Переопределяем StartEditing
        public override void StartEditing()
        {
            try
            {
                Debug.WriteLine($"[TileMyFoldersUc] StartEditing called");

                if (_isInEditMode)
                {
                    Debug.WriteLine($"[TileMyFoldersUc] Already in edit mode, skipping");
                    return;
                }

                // Получаем ViewModel из DataContext
                _viewModel = this.DataContext as ExplorerItemViewModel;
                if (_viewModel == null)
                {
                    Debug.WriteLine($"[TileMyFoldersUc] ViewModel is null");
                    return;
                }

                _isInEditMode = true;
                _originalText = _viewModel.Name;

                Debug.WriteLine($"[TileMyFoldersUc] Starting edit for: {_originalText}");

                // ВАЖНО: Устанавливаем IsEditing в ViewModel!
                _viewModel.IsEditing = true;
                _viewModel.EditRequested = true;

                // Инициализируем временное свойство в ViewModel
                _viewModel.NewNameForEdit = _originalText;

                // Скрываем обычные текстовые блоки
                HorizontalTextBlock.Visibility = Visibility.Collapsed;
                VerticalTextBlock.Visibility = Visibility.Collapsed;
                ListTextBlock.Visibility = Visibility.Collapsed;

                // Показываем поля редактирования
                HorizontalEditBox.Visibility = Visibility.Visible;
                VerticalEditBox.Visibility = Visibility.Visible;
                ListEditBox.Visibility = Visibility.Visible;

                // Скрываем дополнительные элементы при редактировании
                ItemsCountText.Visibility = Visibility.Collapsed;
                LastModifiedText.Visibility = Visibility.Collapsed;
                AttributesText.Visibility = Visibility.Collapsed;

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
                        Debug.WriteLine($"[TileMyFoldersUc] Focus set to edit box");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileMyFoldersUc] Error in StartEditing: {ex.Message}");
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
                Debug.WriteLine($"[TileMyFoldersUc] StopEditing called");

                if (!_isInEditMode) return;

                string newText = GetCurrentEditBox()?.Text?.Trim() ?? "";

                if (!string.IsNullOrEmpty(newText) && newText != _originalText)
                {
                    Debug.WriteLine($"[TileMyFoldersUc] Saving changes: {_originalText} -> {newText}");
                    SaveChanges(newText);
                }
                else
                {
                    Debug.WriteLine($"[TileMyFoldersUc] No changes to save");
                    FinishEditing();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileMyFoldersUc] Error in StopEditing: {ex.Message}");
                FinishEditing();
            }
        }

        // Переопределяем CancelEditing
        public override void CancelEditing()
        {
            try
            {
                Debug.WriteLine($"[TileMyFoldersUc] CancelEditing called");

                if (!_isInEditMode) return;

                Debug.WriteLine($"[TileMyFoldersUc] Cancelling changes, restoring: {_originalText}");

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
                Debug.WriteLine($"[TileMyFoldersUc] Error in CancelEditing: {ex.Message}");
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
                    Debug.WriteLine($"[TileMyFoldersUc] SaveChanges called with: '{newText}'");

                    // Устанавливаем новое имя во временное свойство ViewModel
                    _viewModel.NewNameForEdit = newText;

                    Debug.WriteLine($"[TileMyFoldersUc] NewNameForEdit set to: '{_viewModel.NewNameForEdit}'");
                    Debug.WriteLine($"[TileMyFoldersUc] Original name: '{_originalText}'");
                    Debug.WriteLine($"[TileMyFoldersUc] IsEditing: {_viewModel.IsEditing}");
                    Debug.WriteLine($"[TileMyFoldersUc] CanSaveEdit: {_viewModel.SaveEditCommand?.CanExecute(null)}");

                    // Вызываем команду сохранения
                    if (_viewModel.SaveEditCommand?.CanExecute(null) == true)
                    {
                        Debug.WriteLine($"[TileMyFoldersUc] SaveEditCommand can execute, calling Execute");
                        _viewModel.SaveEditCommand.Execute(null);
                        _ = DispatcherQueue.TryEnqueue(async () =>
                        {
                            await Task.Delay(10);
                            FinishEditing();
                        });
                    }
                    else
                    {
                        Debug.WriteLine($"[TileMyFoldersUc] SaveEditCommand cannot execute. Reasons:");
                        Debug.WriteLine($"[TileMyFoldersUc]   IsEditing: {_viewModel.IsEditing}");
                        Debug.WriteLine($"[TileMyFoldersUc]   NewNameForEdit is null/empty: {string.IsNullOrEmpty(_viewModel.NewNameForEdit?.Trim())}");
                        Debug.WriteLine($"[TileMyFoldersUc]   NewNameForEdit == Original: {_viewModel.NewNameForEdit?.Trim() == _originalText}");

                        // Если команда не может выполниться, завершаем редактирование
                        FinishEditing();
                    }
                }
                else
                {
                    Debug.WriteLine($"[TileMyFoldersUc] ViewModel is null, cannot save changes");
                    FinishEditing();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileMyFoldersUc] Error in SaveChanges: {ex.Message}");
                FinishEditing();
            }
        }

        private void FinishEditing()
        {
            try
            {
                Debug.WriteLine($"[TileMyFoldersUc] FinishEditing called");

                _isInEditMode = false;

                // Показываем обычные текстовые блоки
                HorizontalTextBlock.Visibility = Visibility.Visible;
                VerticalTextBlock.Visibility = Visibility.Visible;
                ListTextBlock.Visibility = Visibility.Visible;

                // Скрываем поля редактирования
                HorizontalEditBox.Visibility = Visibility.Collapsed;
                VerticalEditBox.Visibility = Visibility.Collapsed;
                ListEditBox.Visibility = Visibility.Collapsed;

                // Восстанавливаем дополнительные элементы
                UpdateDetailsVisibility();

                // Сбрасываем ViewModel
                if (_viewModel != null)
                {
                    _viewModel.EditRequested = false;
                    _viewModel = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileMyFoldersUc] Error in FinishEditing: {ex.Message}");
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
                Debug.WriteLine($"[TileMyFoldersUc] EditTextBox_LostFocus - saving changes");
                StopEditing();
            }
        }

        public void EditTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (!_isInEditMode) return;

            Debug.WriteLine($"[TileMyFoldersUc] EditTextBox_KeyDown: {e.Key}");

            switch (e.Key)
            {
                case VirtualKey.Enter:
                    e.Handled = true;
                    Debug.WriteLine($"[TileMyFoldersUc] Enter pressed - saving");

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
                    Debug.WriteLine($"[TileMyFoldersUc] Escape pressed - cancelling");
                    CancelEditing();
                    break;

                case VirtualKey.Tab:
                    e.Handled = true;
                    Debug.WriteLine($"[TileMyFoldersUc] Tab pressed - saving");

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
                    Debug.WriteLine($"[TileMyFoldersUc] TextChanged: NewNameForEdit = '{textBox.Text}'");
                }
            }
        }

        protected override void OnDisplayModeChanged()
        {
            Debug.WriteLine($"[TileMyFoldersUc] OnDisplayModeChanged: DisplayMode = {DisplayMode}");

            try
            {
                if (HorizontalLayout == null || VerticalLayout == null || ListLayout == null)
                {
                    Debug.WriteLine($"[TileMyFoldersUc] Ошибка: Макеты не инициализированы");
                    return;
                }

                if (DisplayMode == "Vertical")
                {
                    Debug.WriteLine($"[TileMyFoldersUc] Установка вертикального режима");
                    HorizontalLayout.Visibility = Visibility.Collapsed;
                    VerticalLayout.Visibility = Visibility.Visible;
                    ListLayout.Visibility = Visibility.Collapsed;
                }
                else if (DisplayMode == "List")
                {
                    Debug.WriteLine($"[TileMyFoldersUc] Установка лист режима");
                    HorizontalLayout.Visibility = Visibility.Collapsed;
                    VerticalLayout.Visibility = Visibility.Collapsed;
                    ListLayout.Visibility = Visibility.Visible;
                }
                else
                {
                    Debug.WriteLine($"[TileMyFoldersUc] Установка горизонтального режима");
                    HorizontalLayout.Visibility = Visibility.Visible;
                    VerticalLayout.Visibility = Visibility.Collapsed;
                    ListLayout.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileMyFoldersUc] Ошибка в OnDisplayModeChanged: {ex.Message}");
            }
        }

        protected override void UpdateSize()
        {
            Debug.WriteLine($"[TileMyFoldersUc] UpdateSize: Size = {Size}");
            base.UpdateSize();

            UpdateDetailsVisibility();
        }

        private void UpdateDetailsVisibility()
        {
            if (_isInEditMode) return;

            if (DisplayMode == "List")
            {
                SetElementsVisibility(false);
                return;
            }

            switch (Size?.ToLower())
            {
                case "extra small":
                    Debug.WriteLine($"[TileMyFoldersUc] Установка размера Extra Small");
                    SetElementsVisibility(false);
                    break;

                case "small":
                    Debug.WriteLine($"[TileMyFoldersUc] Установка размера Small");
                    SetElementsVisibility(false);
                    if (HorizontalTextBlock != null)
                    {
                        HorizontalTextBlock.VerticalAlignment = VerticalAlignment.Center;
                        HorizontalTextBlock.Margin = new Thickness(10, 0, 8, 0);
                    }
                    break;

                case "medium":
                    Debug.WriteLine($"[TileMyFoldersUc] Установка размера Medium");
                    SetElementsVisibility(true, showDetails: true, showAttributes: true);
                    if (ItemsCountText != null) ItemsCountText.FontSize = 10;
                    if (LastModifiedText != null) LastModifiedText.FontSize = 10;
                    if (AttributesText != null) AttributesText.FontSize = 10;
                    break;

                case "large":
                    Debug.WriteLine($"[TileMyFoldersUc] Установка размера Large");
                    SetElementsVisibility(true, showDetails: true, showAttributes: true);
                    if (ItemsCountText != null) ItemsCountText.FontSize = 12;
                    if (LastModifiedText != null) LastModifiedText.FontSize = 12;
                    if (AttributesText != null) AttributesText.FontSize = 12;
                    break;

                case "extra large":
                    Debug.WriteLine($"[TileMyFoldersUc] Установка размера Extra Large");
                    SetElementsVisibility(true, showDetails: true, showAttributes: true);
                    if (HorizontalTextBlock != null) HorizontalTextBlock.FontSize = 16;
                    if (VerticalTextBlock != null) VerticalTextBlock.FontSize = 16;
                    if (ListTextBlock != null) ListTextBlock.FontSize = 16;
                    if (ItemsCountText != null) ItemsCountText.FontSize = 14;
                    if (LastModifiedText != null) LastModifiedText.FontSize = 14;
                    if (AttributesText != null) AttributesText.FontSize = 12;
                    break;

                default:
                    Debug.WriteLine($"[TileMyFoldersUc] Неизвестный размер: {Size}");
                    SetElementsVisibility(true, showDetails: true, showAttributes: true);
                    break;
            }
        }

        private void SetElementsVisibility(
            bool isVisible,
            bool showDetails = true,
            bool showAttributes = true)
        {
            try
            {
                var visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;

                if (ItemsCountText != null)
                {
                    ItemsCountText.Visibility = showDetails ? visibility : Visibility.Collapsed;
                }

                if (LastModifiedText != null)
                {
                    LastModifiedText.Visibility = showDetails ? visibility : Visibility.Collapsed;
                }

                if (AttributesText != null)
                {
                    AttributesText.Visibility = showAttributes ? visibility : Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileMyFoldersUc] Ошибка в SetElementsVisibility: {ex.Message}");
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