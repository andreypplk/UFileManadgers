
//using System.Diagnostics;
//using System.Linq;
//using CommunityToolkit.WinUI;
//using Microsoft.UI.Xaml;
//using Microsoft.UI.Xaml.Controls;

//namespace ufm
//{
//    public sealed partial class TileMyDriveUc : BaseTileControl
//    {
//        private ScaleAnimator _scaleAnimator;

//        public TileMyDriveUc()
//        {
//            this.InitializeComponent();

//            if (BorderTileMyDriveUC != null)
//            {
//                _scaleAnimator = new ScaleAnimator(BorderTileMyDriveUC);
//            }
//        }

//        protected override void OnDisplayModeChanged()
//        {
//            base.OnDisplayModeChanged();

//            // Переключение между режимами
//            if (DisplayMode == "Vertical")
//            {
//                HorizontalLayout.Visibility = Visibility.Collapsed;
//                VerticalLayout.Visibility = Visibility.Visible;
//            }
//            else
//            {
//                HorizontalLayout.Visibility = Visibility.Visible;
//                VerticalLayout.Visibility = Visibility.Collapsed;
//            }
//        }

//        protected override void UpdateSize()
//        {
//            base.UpdateSize();

//            // Проверка на null элементов управления
//            if (progressBar == null || GridUsedSpaceString == null || BorderTotalSizeString == null ||
//                tbFreeSpaceString == null || tbUsedSpaceSString == null || tbTotalSizeString == null)
//            {
//                return;
//            }

//            // Приводим Size к нижнему регистру для унификации
//            switch (Size.ToLower())
//            {
//                case "tiny":
//                    BorderTotalSizeString.Height = 1;
//                    BorderTotalSizeString.Width = 1;
//                    SetElementVisibility(false, 0, 0);
//                    break;
//                case "extra small":
//                    BorderTotalSizeString.Height = 1;
//                    BorderTotalSizeString.Width = 1;
//                    SetElementVisibility(false, 0, 0);
//                    break;
//                case "small":
//                    BorderTotalSizeString.Height = 1;
//                    BorderTotalSizeString.Width = 1;
//                    SetElementVisibility(false, 0, 0);
//                    break;
//                case "medium":
//                    BorderTotalSizeString.Height = 45;
//                    BorderTotalSizeString.Width = 40;
//                    SetElementVisibility(true, 10, 18);
//                    break;
//                case "large":
//                    BorderTotalSizeString.Height = 75;
//                    BorderTotalSizeString.Width = 45;
//                    SetElementVisibility(true, 12, 20);
//                    break;
//                case "extra large":
//                    BorderTotalSizeString.Height = 85;
//                    BorderTotalSizeString.Width = 50;
//                    SetElementVisibility(true, 14, 22);
//                    break;
//                case "huge":
//                    BorderTotalSizeString.Height = 85;
//                    BorderTotalSizeString.Width = 50;
//                    SetElementVisibility(true, 14, 22);
//                    break;
//                default:
//                    // Обработка неизвестного размера
//                    Debug.WriteLine($"Неизвестный размер: {Size}");
//                    break;
//            }
//        }

//        private void SetElementVisibility(bool isVisible, double fontSize, double indHeight)
//        {
//            // Устанавливаем видимость элементов
//            progressBar.Visibility = isVisible.ToVisibility();
//            BorderTotalSizeString.Visibility = isVisible.ToVisibility();
//            GridUsedSpaceString.Visibility = isVisible.ToVisibility();
//            tbFreeSpaceString.Visibility = isVisible.ToVisibility();
//            tbUsedSpaceSString.Visibility = isVisible.ToVisibility();

//            // Устанавливаем минимальный размер шрифта 1 (вместо 0)
//            double actualFontSize = isVisible ? fontSize : 1;
//            tbFreeSpaceString.FontSize = actualFontSize;
//            tbUsedSpaceSString.FontSize = actualFontSize;
//            tbTotalSizeString.FontSize = actualFontSize;

//            var indicator = GetProgressBarIndicator();
//            if (indicator != null)
//            {
//                indicator.Height = isVisible ? indHeight : 0;
//            }
//        }

//        private Border GetProgressBarIndicator()
//        {
//            if (progressBar == null) return null;

//            // Поиск индикатора с учетом правильного регистра имени
//            return UIHelper.GetDescendantsOfType<Border>(progressBar)
//                .FirstOrDefault(b => b.Name == "Indicator");
//        }
//    }

//    public static class VisibilityExtensions
//    {
//        public static Visibility ToVisibility(this bool isVisible) =>
//            isVisible ? Visibility.Visible : Visibility.Collapsed;
//    }
//}

//using System.Diagnostics;
//using System.Linq;
//using CommunityToolkit.WinUI;
//using Microsoft.UI.Xaml;
//using Microsoft.UI.Xaml.Controls;
//using Microsoft.UI.Xaml.Input;
//using Windows.System;
//using System.IO;
//using System.Threading.Tasks;
//using System;
//using Core_FileManagement;

//namespace ufm
//{
//    public sealed partial class TileMyDriveUc : BaseTileControl
//    {
//        private ScaleAnimator _scaleAnimator;
//        private string _originalText = "";
//        private ExplorerItemViewModel _viewModel;

//        public TileMyDriveUc()
//        {
//            this.InitializeComponent();

//            if (BorderTileMyDriveUC != null)
//            {
//                _scaleAnimator = new ScaleAnimator(BorderTileMyDriveUC);
//            }

//            this.Loaded += OnLoaded;
//            this.DataContextChanged += TileMyDriveUc_DataContextChanged;
//        }

//        private void OnLoaded(object sender, RoutedEventArgs e)
//        {
//            UpdateEditState();
//        }

//        private void TileMyDriveUc_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
//        {
//            if (_viewModel != null)
//            {
//                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
//            }

//            _viewModel = args.NewValue as ExplorerItemViewModel;

//            if (_viewModel != null)
//            {
//                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
//                UpdateEditState();
//            }
//        }

//        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
//        {
//            if (e.PropertyName == nameof(ExplorerItemViewModel.EditRequested) ||
//                e.PropertyName == nameof(ExplorerItemViewModel.IsEditing))
//            {
//                UpdateEditState();
//            }
//        }

//        // Переопределяем метод OnIsEditingChanged из базового класса
//        protected override void OnIsEditingChanged(bool oldValue, bool newValue)
//        {
//            base.OnIsEditingChanged(oldValue, newValue);

//            this.DispatcherQueue.TryEnqueue(() =>
//            {
//                if (newValue && !oldValue)
//                {
//                    // Начинаем редактирование
//                    BeginEdit();
//                }
//                else if (!newValue && oldValue)
//                {
//                    // Заканчиваем редактирование
//                    EndEdit();
//                }
//            });
//        }

//        // Переопределяем StartEditing для активации редактирования
//        public override void StartEditing()
//        {
//            base.StartEditing();

//            if (_viewModel != null)
//            {
//                // Синхронизируем с ViewModel
//                _viewModel.IsEditing = true;
//                _viewModel.EditRequested = true;
//            }

//            BeginEdit();
//        }

//        // Переопределяем StopEditing для сохранения изменений
//        public override void StopEditing()
//        {
//            SaveChangesAndExit();
//            base.StopEditing();
//        }

//        // Переопределяем CancelEditing для отмены изменений
//        public override void CancelEditing()
//        {
//            CancelChangesAndExit();
//            base.CancelEditing();
//        }

//        // Переопределяем CanEdit для указания поддержки редактирования
//        public override bool CanEdit => true;

//        private void UpdateEditState()
//        {
//            if (_viewModel != null)
//            {
//                bool shouldEdit = _viewModel.EditRequested || _viewModel.IsEditing;

//                if (shouldEdit != IsEditing)
//                {
//                    IsEditing = shouldEdit;
//                }
//            }
//        }

//        private void BeginEdit()
//        {
//            if (_viewModel != null)
//            {
//                _originalText = _viewModel.Name;
//            }

//            // Переключаем видимость элементов
//            HorizontalTextBlock.Visibility = Visibility.Collapsed;
//            VerticalTextBlock.Visibility = Visibility.Collapsed;
//            ListTextBlock.Visibility = Visibility.Collapsed;

//            HorizontalEditBox.Visibility = Visibility.Visible;
//            VerticalEditBox.Visibility = Visibility.Visible;
//            ListEditBox.Visibility = Visibility.Visible;

//            // Скрываем дополнительные элементы при редактировании
//            SetElementVisibility(false, 0, 0);

//            // Устанавливаем фокус
//            this.DispatcherQueue.TryEnqueue(() =>
//            {
//                TextBox editBox = GetCurrentEditBox();
//                if (editBox != null)
//                {
//                    editBox.Focus(FocusState.Programmatic);
//                    editBox.SelectAll();
//                }
//            });

//            Debug.WriteLine($"Режим редактирования активирован для: {_originalText}");
//        }

//        private void EndEdit()
//        {
//            // Восстанавливаем видимость
//            HorizontalTextBlock.Visibility = Visibility.Visible;
//            VerticalTextBlock.Visibility = Visibility.Visible;
//            ListTextBlock.Visibility = Visibility.Visible;

//            HorizontalEditBox.Visibility = Visibility.Collapsed;
//            VerticalEditBox.Visibility = Visibility.Collapsed;
//            ListEditBox.Visibility = Visibility.Collapsed;

//            // Восстанавливаем дополнительные элементы
//            UpdateSize();

//            if (_viewModel != null)
//            {
//                _viewModel.IsEditing = false;
//                _viewModel.EditRequested = false;
//            }

//            Debug.WriteLine($"Режим редактирования деактивирован");
//        }

//        protected override void OnDisplayModeChanged()
//        {
//            base.OnDisplayModeChanged();

//            // Переключение между режимами
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
//            else // Horizontal и все остальные
//            {
//                HorizontalLayout.Visibility = Visibility.Visible;
//                VerticalLayout.Visibility = Visibility.Collapsed;
//                ListLayout.Visibility = Visibility.Collapsed;
//            }

//            // Обновляем состояние редактирования при смене режима
//            if (IsEditing)
//            {
//                UpdateEditControls();
//            }
//        }

//        protected override void UpdateSize()
//        {
//            base.UpdateSize();

//            // Проверка на null элементов управления
//            if (progressBar == null || GridUsedSpaceString == null || BorderTotalSizeString == null ||
//                tbFreeSpaceString == null || tbUsedSpaceSString == null || tbTotalSizeString == null)
//            {
//                return;
//            }

//            // Для режимов List и EditText скрываем дополнительные элементы
//            if (DisplayMode == "List" || IsEditing)
//            {
//                SetElementVisibility(false, 0, 0);
//                return;
//            }

//            // Приводим Size к нижнему регистру для унификации
//            switch (Size.ToLower())
//            {
//                case "tiny":
//                    BorderTotalSizeString.Height = 1;
//                    BorderTotalSizeString.Width = 1;
//                    SetElementVisibility(false, 0, 0);
//                    break;
//                case "extra small":
//                    BorderTotalSizeString.Height = 1;
//                    BorderTotalSizeString.Width = 1;
//                    SetElementVisibility(false, 0, 0);
//                    break;
//                case "small":
//                    BorderTotalSizeString.Height = 1;
//                    BorderTotalSizeString.Width = 1;
//                    SetElementVisibility(false, 0, 0);
//                    break;
//                case "medium":
//                    BorderTotalSizeString.Height = 45;
//                    BorderTotalSizeString.Width = 40;
//                    SetElementVisibility(true, 10, 18);
//                    break;
//                case "large":
//                    BorderTotalSizeString.Height = 75;
//                    BorderTotalSizeString.Width = 45;
//                    SetElementVisibility(true, 12, 20);
//                    break;
//                case "extra large":
//                    BorderTotalSizeString.Height = 85;
//                    BorderTotalSizeString.Width = 50;
//                    SetElementVisibility(true, 14, 22);
//                    break;
//                case "huge":
//                    BorderTotalSizeString.Height = 85;
//                    BorderTotalSizeString.Width = 50;
//                    SetElementVisibility(true, 14, 22);
//                    break;
//                default:
//                    // Обработка неизвестного размера
//                    Debug.WriteLine($"Неизвестный размер: {Size}");
//                    break;
//            }
//        }

//        private void UpdateEditControls()
//        {
//            // Показываем/скрываем текстовые поля редактирования в зависимости от текущего режима
//            if (IsEditing)
//            {
//                // Скрываем обычные текстовые блоки
//                HorizontalTextBlock.Visibility = Visibility.Collapsed;
//                VerticalTextBlock.Visibility = Visibility.Collapsed;
//                ListTextBlock.Visibility = Visibility.Collapsed;

//                // Показываем поля редактирования
//                HorizontalEditBox.Visibility = Visibility.Visible;
//                VerticalEditBox.Visibility = Visibility.Visible;
//                ListEditBox.Visibility = Visibility.Visible;

//                // При редактировании скрываем дополнительные элементы
//                SetElementVisibility(false, 0, 0);
//            }
//            else
//            {
//                // Показываем обычные текстовые блоки
//                HorizontalTextBlock.Visibility = Visibility.Visible;
//                VerticalTextBlock.Visibility = Visibility.Visible;
//                ListTextBlock.Visibility = Visibility.Visible;

//                // Скрываем поля редактирования
//                HorizontalEditBox.Visibility = Visibility.Collapsed;
//                VerticalEditBox.Visibility = Visibility.Collapsed;
//                ListEditBox.Visibility = Visibility.Collapsed;

//                // Восстанавливаем видимость дополнительных элементов
//                UpdateSize();
//            }
//        }

//        private void SetElementVisibility(bool isVisible, double fontSize, double indHeight)
//        {
//            // Устанавливаем видимость элементов
//            progressBar.Visibility = isVisible.ToVisibility();
//            BorderTotalSizeString.Visibility = isVisible.ToVisibility();
//            GridUsedSpaceString.Visibility = isVisible.ToVisibility();
//            tbFreeSpaceString.Visibility = isVisible.ToVisibility();
//            tbUsedSpaceSString.Visibility = isVisible.ToVisibility();

//            // Устанавливаем минимальный размер шрифта 1 (вместо 0)
//            double actualFontSize = isVisible ? fontSize : 1;
//            tbFreeSpaceString.FontSize = actualFontSize;
//            tbUsedSpaceSString.FontSize = actualFontSize;
//            tbTotalSizeString.FontSize = actualFontSize;

//            var indicator = GetProgressBarIndicator();
//            if (indicator != null)
//            {
//                indicator.Height = isVisible ? indHeight : 0;
//            }
//        }

//        private Border GetProgressBarIndicator()
//        {
//            if (progressBar == null) return null;

//            // Поиск индикатора с учетом правильного регистра имени
//            return UIHelper.GetDescendantsOfType<Border>(progressBar)
//                .FirstOrDefault(b => b.Name == "Indicator");
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

//        // Обработчики событий для текстовых полей редактирования
//        public void EditTextBox_Loaded(object sender, RoutedEventArgs e)
//        {
//            if (IsEditing && sender is TextBox textBox)
//            {
//                textBox.Focus(FocusState.Programmatic);
//                textBox.SelectAll();
//            }
//        }

//        public void EditTextBox_LostFocus(object sender, RoutedEventArgs e)
//        {
//            if (IsEditing)
//            {
//                SaveChangesAndExit();
//            }
//        }

//        public void EditTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
//        {
//            if (!IsEditing) return;

//            switch (e.Key)
//            {
//                case VirtualKey.Enter:
//                    e.Handled = true;
//                    SaveChangesAndExit();
//                    break;

//                case VirtualKey.Escape:
//                    e.Handled = true;
//                    CancelChangesAndExit();
//                    break;

//                case VirtualKey.Tab:
//                    e.Handled = true;
//                    SaveChangesAndExit();
//                    break;
//            }
//        }

//        private async void SaveChangesAndExit()
//        {
//            try
//            {
//                string newText = GetCurrentEditBox()?.Text?.Trim() ?? "";

//                if (!string.IsNullOrEmpty(newText) && newText != _originalText)
//                {
//                    Debug.WriteLine($"Сохранение нового имени диска: {newText}");

//                    if (_viewModel != null)
//                    {
//                        // Сохраняем новое имя в ViewModel
//                        _viewModel.Name = newText;

//                        // Вызываем команду сохранения через ViewModel
//                        if (_viewModel.SaveEditCommand.CanExecute(null))
//                        {
//                            _viewModel.SaveEditCommand.Execute(null);
//                        }
//                    }
//                }

//                StopEditing();
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Ошибка при сохранении изменений: {ex.Message}");
//                CancelEditing();
//            }
//        }

//        private void CancelChangesAndExit()
//        {
//            try
//            {
//                Debug.WriteLine("Изменения отменены");

//                if (_viewModel != null)
//                {
//                    // Восстанавливаем оригинальное имя
//                    if (!string.IsNullOrEmpty(_originalText))
//                    {
//                        _viewModel.Name = _originalText;
//                    }

//                    // Вызываем команду отмены через ViewModel
//                    if (_viewModel.CancelEditCommand.CanExecute(null))
//                    {
//                        _viewModel.CancelEditCommand.Execute(null);
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Ошибка при отмене изменений: {ex.Message}");
//            }
//            finally
//            {
//                CancelEditing();
//            }
//        }
//    }

//    public static class VisibilityExtensions
//    {
//        public static Visibility ToVisibility(this bool isVisible) =>
//            isVisible ? Visibility.Visible : Visibility.Collapsed;
//    }
//}


//using System.Diagnostics;
//using System.Linq;
//using CommunityToolkit.WinUI;
//using Microsoft.UI.Xaml;
//using Microsoft.UI.Xaml.Controls;
//using Microsoft.UI.Xaml.Input;
//using Windows.System;
//using System.IO;
//using System.Threading.Tasks;
//using System;
//using Core_FileManagement;

//namespace ufm
//{
//    public sealed partial class TileMyDriveUc : BaseTileControl
//    {
//        private ScaleAnimator _scaleAnimator;
//        private string _originalText = "";
//        private ExplorerItemViewModel _viewModel;
//        private bool _isUpdatingFromViewModel = false;

//        public TileMyDriveUc()
//        {
//            this.InitializeComponent();

//            if (BorderTileMyDriveUC != null)
//            {
//                _scaleAnimator = new ScaleAnimator(BorderTileMyDriveUC);
//            }

//            this.Loaded += OnLoaded;
//            this.DataContextChanged += TileMyDriveUc_DataContextChanged;
//        }

//        private void OnLoaded(object sender, RoutedEventArgs e)
//        {
//            UpdateEditState();
//        }

//        private void TileMyDriveUc_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
//        {
//            if (_viewModel != null)
//            {
//                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
//            }

//            _viewModel = args.NewValue as ExplorerItemViewModel;

//            if (_viewModel != null)
//            {
//                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
//                UpdateEditState();
//            }
//        }

//        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
//        {
//            if (e.PropertyName == nameof(ExplorerItemViewModel.EditRequested) ||
//                e.PropertyName == nameof(ExplorerItemViewModel.IsEditing))
//            {
//                UpdateEditState();
//            }
//        }

//        // Переопределяем метод OnIsEditingChanged из базового класса
//        protected override void OnIsEditingChanged(bool oldValue, bool newValue)
//        {
//            base.OnIsEditingChanged(oldValue, newValue);

//            this.DispatcherQueue.TryEnqueue(() =>
//            {
//                if (newValue && !oldValue)
//                {
//                    // Начинаем редактирование
//                    BeginEdit();
//                }
//                else if (!newValue && oldValue)
//                {
//                    // Заканчиваем редактирование
//                    EndEdit();
//                }
//            });
//        }

//        // Переопределяем StartEditing для активации редактирования
//        public override void StartEditing()
//        {
//            Debug.WriteLine($"[TileMyDriveUc] StartEditing called");

//            // Устанавливаем флаг, чтобы избежать рекурсии
//            _isUpdatingFromViewModel = true;

//            try
//            {
//                if (_viewModel != null)
//                {
//                    // Синхронизируем с ViewModel только если значения отличаются
//                    if (!_viewModel.IsEditing || !_viewModel.EditRequested)
//                    {
//                        _viewModel.IsEditing = true;
//                        _viewModel.EditRequested = true;
//                    }
//                }

//                // Устанавливаем свойство базового класса
//                if (!IsEditing)
//                {
//                    IsEditing = true;
//                }
//                else
//                {
//                    // Если уже в режиме редактирования, просто активируем UI
//                    BeginEdit();
//                }
//            }
//            finally
//            {
//                _isUpdatingFromViewModel = false;
//            }
//        }

//        // Переопределяем StopEditing для сохранения изменений
//        public override void StopEditing()
//        {
//            Debug.WriteLine($"[TileMyDriveUc] StopEditing called");
//            SaveChangesAndExit();
//            base.StopEditing();
//        }

//        // Переопределяем CancelEditing для отмены изменений
//        public override void CancelEditing()
//        {
//            Debug.WriteLine($"[TileMyDriveUc] CancelEditing called");
//            CancelChangesAndExit();
//            base.CancelEditing();
//        }

//        // Переопределяем CanEdit для указания поддержки редактирования
//        public override bool CanEdit => true;

//        private void UpdateEditState()
//        {
//            if (_viewModel != null && !_isUpdatingFromViewModel)
//            {
//                bool shouldEdit = _viewModel.EditRequested || _viewModel.IsEditing;

//                if (shouldEdit != IsEditing)
//                {
//                    Debug.WriteLine($"[TileMyDriveUc] UpdateEditState: shouldEdit={shouldEdit}, IsEditing={IsEditing}");
//                    IsEditing = shouldEdit;
//                }
//            }
//        }

//        private void BeginEdit()
//        {
//            try
//            {
//                Debug.WriteLine($"[TileMyDriveUc] BeginEdit called");

//                if (_viewModel != null)
//                {
//                    _originalText = _viewModel.Name;
//                    Debug.WriteLine($"[TileMyDriveUc] Original text saved: {_originalText}");
//                }

//                // Переключаем видимость элементов
//                HorizontalTextBlock.Visibility = Visibility.Collapsed;
//                VerticalTextBlock.Visibility = Visibility.Collapsed;
//                ListTextBlock.Visibility = Visibility.Collapsed;

//                HorizontalEditBox.Visibility = Visibility.Visible;
//                VerticalEditBox.Visibility = Visibility.Visible;
//                ListEditBox.Visibility = Visibility.Visible;

//                // Скрываем дополнительные элементы при редактировании
//                SetElementVisibility(false, 0, 0);

//                // Устанавливаем фокус
//                this.DispatcherQueue.TryEnqueue(() =>
//                {
//                    TextBox editBox = GetCurrentEditBox();
//                    if (editBox != null)
//                    {
//                        editBox.Focus(FocusState.Programmatic);
//                        editBox.SelectAll();
//                        Debug.WriteLine($"[TileMyDriveUc] Focus set to edit box");
//                    }
//                    else
//                    {
//                        Debug.WriteLine($"[TileMyDriveUc] Edit box not found");
//                    }
//                });

//                Debug.WriteLine($"[TileMyDriveUc] Режим редактирования активирован для: {_originalText}");
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyDriveUc] Error in BeginEdit: {ex.Message}");
//            }
//        }

//        private void EndEdit()
//        {
//            try
//            {
//                Debug.WriteLine($"[TileMyDriveUc] EndEdit called");

//                // Восстанавливаем видимость
//                HorizontalTextBlock.Visibility = Visibility.Visible;
//                VerticalTextBlock.Visibility = Visibility.Visible;
//                ListTextBlock.Visibility = Visibility.Visible;

//                HorizontalEditBox.Visibility = Visibility.Collapsed;
//                VerticalEditBox.Visibility = Visibility.Collapsed;
//                ListEditBox.Visibility = Visibility.Collapsed;

//                // Восстанавливаем дополнительные элементы
//                UpdateSize();

//                if (_viewModel != null && !_isUpdatingFromViewModel)
//                {
//                    _viewModel.IsEditing = false;
//                    _viewModel.EditRequested = false;
//                }

//                Debug.WriteLine($"[TileMyDriveUc] Режим редактирования деактивирован");
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyDriveUc] Error in EndEdit: {ex.Message}");
//            }
//        }

//        protected override void OnDisplayModeChanged()
//        {
//            base.OnDisplayModeChanged();

//            // Переключение между режимами
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
//            else // Horizontal и все остальные
//            {
//                HorizontalLayout.Visibility = Visibility.Visible;
//                VerticalLayout.Visibility = Visibility.Collapsed;
//                ListLayout.Visibility = Visibility.Collapsed;
//            }

//            // Обновляем состояние редактирования при смене режима
//            if (IsEditing)
//            {
//                UpdateEditControls();
//            }
//        }

//        protected override void UpdateSize()
//        {
//            base.UpdateSize();

//            // Проверка на null элементов управления
//            if (progressBar == null || GridUsedSpaceString == null || BorderTotalSizeString == null ||
//                tbFreeSpaceString == null || tbUsedSpaceSString == null || tbTotalSizeString == null)
//            {
//                return;
//            }

//            // Для режимов List и EditText скрываем дополнительные элементы
//            if (DisplayMode == "List" || IsEditing)
//            {
//                SetElementVisibility(false, 0, 0);
//                return;
//            }

//            // Приводим Size к нижнему регистру для унификации
//            switch (Size.ToLower())
//            {
//                case "tiny":
//                    BorderTotalSizeString.Height = 1;
//                    BorderTotalSizeString.Width = 1;
//                    SetElementVisibility(false, 0, 0);
//                    break;
//                case "extra small":
//                    BorderTotalSizeString.Height = 1;
//                    BorderTotalSizeString.Width = 1;
//                    SetElementVisibility(false, 0, 0);
//                    break;
//                case "small":
//                    BorderTotalSizeString.Height = 1;
//                    BorderTotalSizeString.Width = 1;
//                    SetElementVisibility(false, 0, 0);
//                    break;
//                case "medium":
//                    BorderTotalSizeString.Height = 45;
//                    BorderTotalSizeString.Width = 40;
//                    SetElementVisibility(true, 10, 18);
//                    break;
//                case "large":
//                    BorderTotalSizeString.Height = 75;
//                    BorderTotalSizeString.Width = 45;
//                    SetElementVisibility(true, 12, 20);
//                    break;
//                case "extra large":
//                    BorderTotalSizeString.Height = 85;
//                    BorderTotalSizeString.Width = 50;
//                    SetElementVisibility(true, 14, 22);
//                    break;
//                case "huge":
//                    BorderTotalSizeString.Height = 85;
//                    BorderTotalSizeString.Width = 50;
//                    SetElementVisibility(true, 14, 22);
//                    break;
//                default:
//                    // Обработка неизвестного размера
//                    Debug.WriteLine($"[TileMyDriveUc] Неизвестный размер: {Size}");
//                    break;
//            }
//        }

//        private void UpdateEditControls()
//        {
//            // Показываем/скрываем текстовые поля редактирования в зависимости от текущего режима
//            if (IsEditing)
//            {
//                // Скрываем обычные текстовые блоки
//                HorizontalTextBlock.Visibility = Visibility.Collapsed;
//                VerticalTextBlock.Visibility = Visibility.Collapsed;
//                ListTextBlock.Visibility = Visibility.Collapsed;

//                // Показываем поля редактирования
//                HorizontalEditBox.Visibility = Visibility.Visible;
//                VerticalEditBox.Visibility = Visibility.Visible;
//                ListEditBox.Visibility = Visibility.Visible;

//                // При редактировании скрываем дополнительные элементы
//                SetElementVisibility(false, 0, 0);
//            }
//            else
//            {
//                // Показываем обычные текстовые блоки
//                HorizontalTextBlock.Visibility = Visibility.Visible;
//                VerticalTextBlock.Visibility = Visibility.Visible;
//                ListTextBlock.Visibility = Visibility.Visible;

//                // Скрываем поля редактирования
//                HorizontalEditBox.Visibility = Visibility.Collapsed;
//                VerticalEditBox.Visibility = Visibility.Collapsed;
//                ListEditBox.Visibility = Visibility.Collapsed;

//                // Восстанавливаем видимость дополнительных элементов
//                UpdateSize();
//            }
//        }

//        private void SetElementVisibility(bool isVisible, double fontSize, double indHeight)
//        {
//            // Устанавливаем видимость элементов
//            progressBar.Visibility = isVisible.ToVisibility();
//            BorderTotalSizeString.Visibility = isVisible.ToVisibility();
//            GridUsedSpaceString.Visibility = isVisible.ToVisibility();
//            tbFreeSpaceString.Visibility = isVisible.ToVisibility();
//            tbUsedSpaceSString.Visibility = isVisible.ToVisibility();

//            // Устанавливаем минимальный размер шрифта 1 (вместо 0)
//            double actualFontSize = isVisible ? fontSize : 1;
//            tbFreeSpaceString.FontSize = actualFontSize;
//            tbUsedSpaceSString.FontSize = actualFontSize;
//            tbTotalSizeString.FontSize = actualFontSize;

//            var indicator = GetProgressBarIndicator();
//            if (indicator != null)
//            {
//                indicator.Height = isVisible ? indHeight : 0;
//            }
//        }

//        private Border GetProgressBarIndicator()
//        {
//            if (progressBar == null) return null;

//            // Поиск индикатора с учетом правильного регистра имени
//            return UIHelper.GetDescendantsOfType<Border>(progressBar)
//                .FirstOrDefault(b => b.Name == "Indicator");
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

//        // Обработчики событий для текстовых полей редактирования
//        public void EditTextBox_Loaded(object sender, RoutedEventArgs e)
//        {
//            if (IsEditing && sender is TextBox textBox)
//            {
//                textBox.Focus(FocusState.Programmatic);
//                textBox.SelectAll();
//                Debug.WriteLine($"[TileMyDriveUc] EditTextBox_Loaded: focus set");
//            }
//        }

//        public void EditTextBox_LostFocus(object sender, RoutedEventArgs e)
//        {
//            if (IsEditing)
//            {
//                Debug.WriteLine($"[TileMyDriveUc] EditTextBox_LostFocus");
//                SaveChangesAndExit();
//            }
//        }

//        public void EditTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
//        {
//            if (!IsEditing) return;

//            Debug.WriteLine($"[TileMyDriveUc] EditTextBox_KeyDown: {e.Key}");

//            switch (e.Key)
//            {
//                case VirtualKey.Enter:
//                    e.Handled = true;
//                    SaveChangesAndExit();
//                    break;

//                case VirtualKey.Escape:
//                    e.Handled = true;
//                    CancelChangesAndExit();
//                    break;

//                case VirtualKey.Tab:
//                    e.Handled = true;
//                    SaveChangesAndExit();
//                    break;
//            }
//        }

//        private async void SaveChangesAndExit()
//        {
//            try
//            {
//                Debug.WriteLine($"[TileMyDriveUc] SaveChangesAndExit called");

//                string newText = GetCurrentEditBox()?.Text?.Trim() ?? "";

//                if (!string.IsNullOrEmpty(newText) && newText != _originalText)
//                {
//                    Debug.WriteLine($"[TileMyDriveUc] Сохранение нового имени диска: {newText}");

//                    if (_viewModel != null)
//                    {
//                        // Сохраняем новое имя в ViewModel
//                        _viewModel.Name = newText;

//                        // Вызываем команду сохранения через ViewModel
//                        if (_viewModel.SaveEditCommand.CanExecute(null))
//                        {
//                            _viewModel.SaveEditCommand.Execute(null);
//                        }
//                    }
//                }
//                else
//                {
//                    Debug.WriteLine($"[TileMyDriveUc] Имя не изменилось или пустое");
//                }

//                StopEditing();
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyDriveUc] Ошибка при сохранении изменений: {ex.Message}");
//                CancelEditing();
//            }
//        }

//        private void CancelChangesAndExit()
//        {
//            try
//            {
//                Debug.WriteLine($"[TileMyDriveUc] CancelChangesAndExit called");

//                if (_viewModel != null)
//                {
//                    // Восстанавливаем оригинальное имя
//                    if (!string.IsNullOrEmpty(_originalText))
//                    {
//                        _viewModel.Name = _originalText;
//                        Debug.WriteLine($"[TileMyDriveUc] Восстановлено оригинальное имя: {_originalText}");
//                    }

//                    // Вызываем команду отмены через ViewModel
//                    if (_viewModel.CancelEditCommand.CanExecute(null))
//                    {
//                        _viewModel.CancelEditCommand.Execute(null);
//                    }
//                }

//                CancelEditing();
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyDriveUc] Ошибка при отмене изменений: {ex.Message}");
//                CancelEditing();
//            }
//        }
//    }

//    public static class VisibilityExtensions
//    {
//        public static Visibility ToVisibility(this bool isVisible) =>
//            isVisible ? Visibility.Visible : Visibility.Collapsed;
//    }
//}


//using System.Diagnostics;
//using System.Linq;
//using CommunityToolkit.WinUI;
//using Microsoft.UI.Xaml;
//using Microsoft.UI.Xaml.Controls;
//using Microsoft.UI.Xaml.Input;
//using Windows.System;
//using System.IO;
//using System.Threading.Tasks;
//using System;
//using Core_FileManagement;

//namespace ufm
//{
//    public sealed partial class TileMyDriveUc : BaseTileControl
//    {
//        private ScaleAnimator _scaleAnimator;
//        private string _originalText = "";
//        private ExplorerItemViewModel _viewModel;
//        private bool _isInEditMode = false;

//        public TileMyDriveUc()
//        {
//            this.InitializeComponent();

//            if (BorderTileMyDriveUC != null)
//            {
//                _scaleAnimator = new ScaleAnimator(BorderTileMyDriveUC);
//            }
//        }

//        // Переопределяем StartEditing
//        public override void StartEditing()
//        {
//            try
//            {
//                Debug.WriteLine($"[TileMyDriveUc] StartEditing called");

//                if (_isInEditMode)
//                {
//                    Debug.WriteLine($"[TileMyDriveUc] Already in edit mode, skipping");
//                    return;
//                }

//                // Получаем ViewModel из DataContext
//                _viewModel = this.DataContext as ExplorerItemViewModel;
//                if (_viewModel == null)
//                {
//                    Debug.WriteLine($"[TileMyDriveUc] ViewModel is null");
//                    return;
//                }

//                _isInEditMode = true;
//                _originalText = _viewModel.Name;

//                Debug.WriteLine($"[TileMyDriveUc] Starting edit for: {_originalText}");

//                // Показываем TextBox, скрываем TextBlock
//                ShowEditControls(true);

//                // Устанавливаем фокус
//                this.DispatcherQueue.TryEnqueue(() =>
//                {
//                    TextBox editBox = GetCurrentEditBox();
//                    if (editBox != null)
//                    {
//                        editBox.Focus(FocusState.Programmatic);
//                        editBox.SelectAll();
//                        Debug.WriteLine($"[TileMyDriveUc] Focus set to edit box");
//                    }
//                });
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyDriveUc] Error in StartEditing: {ex.Message}");
//                _isInEditMode = false;
//            }
//        }

//        // Переопределяем StopEditing
//        public override void StopEditing()
//        {
//            try
//            {
//                Debug.WriteLine($"[TileMyDriveUc] StopEditing called");

//                if (!_isInEditMode) return;

//                string newText = GetCurrentEditBox()?.Text?.Trim() ?? "";

//                if (!string.IsNullOrEmpty(newText) && newText != _originalText)
//                {
//                    Debug.WriteLine($"[TileMyDriveUc] Saving changes: {_originalText} -> {newText}");
//                    SaveChanges(newText);
//                }
//                else
//                {
//                    Debug.WriteLine($"[TileMyDriveUc] No changes to save");
//                }

//                FinishEditing();
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyDriveUc] Error in StopEditing: {ex.Message}");
//                FinishEditing();
//            }
//        }

//        // Переопределяем CancelEditing
//        public override void CancelEditing()
//        {
//            try
//            {
//                Debug.WriteLine($"[TileMyDriveUc] CancelEditing called");

//                if (!_isInEditMode) return;

//                Debug.WriteLine($"[TileMyDriveUc] Cancelling changes, restoring: {_originalText}");

//                if (_viewModel != null && !string.IsNullOrEmpty(_originalText))
//                {
//                    _viewModel.Name = _originalText;
//                }

//                FinishEditing();
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyDriveUc] Error in CancelEditing: {ex.Message}");
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
//                Debug.WriteLine($"[TileMyDriveUc] Error saving changes: {ex.Message}");
//            }
//        }

//        private void FinishEditing()
//        {
//            try
//            {
//                Debug.WriteLine($"[TileMyDriveUc] FinishEditing called");

//                // Скрываем TextBox, показываем TextBlock
//                ShowEditControls(false);

//                _isInEditMode = false;
//                _viewModel = null;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyDriveUc] Error in FinishEditing: {ex.Message}");
//                _isInEditMode = false;
//                _viewModel = null;
//            }
//        }

//        private void ShowEditControls(bool showEdit)
//        {
//            try
//            {
//                if (showEdit)
//                {
//                    // Скрываем обычные текстовые блоки
//                    HorizontalTextBlock.Visibility = Visibility.Collapsed;
//                    VerticalTextBlock.Visibility = Visibility.Collapsed;
//                    ListTextBlock.Visibility = Visibility.Collapsed;

//                    // Показываем поля редактирования
//                    HorizontalEditBox.Visibility = Visibility.Visible;
//                    VerticalEditBox.Visibility = Visibility.Visible;
//                    ListEditBox.Visibility = Visibility.Visible;

//                    // Скрываем дополнительные элементы при редактировании
//                    SetElementVisibility(false, 0, 0);

//                    // Устанавливаем текст в TextBox
//                    if (_viewModel != null)
//                    {
//                        string currentText = _viewModel.Name;
//                        HorizontalEditBox.Text = currentText;
//                        VerticalEditBox.Text = currentText;
//                        ListEditBox.Text = currentText;
//                    }
//                }
//                else
//                {
//                    // Показываем обычные текстовые блоки
//                    HorizontalTextBlock.Visibility = Visibility.Visible;
//                    VerticalTextBlock.Visibility = Visibility.Visible;
//                    ListTextBlock.Visibility = Visibility.Visible;

//                    // Скрываем поля редактирования
//                    HorizontalEditBox.Visibility = Visibility.Collapsed;
//                    VerticalEditBox.Visibility = Visibility.Collapsed;
//                    ListEditBox.Visibility = Visibility.Collapsed;

//                    // Восстанавливаем дополнительные элементы
//                    UpdateSize();
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyDriveUc] Error in ShowEditControls: {ex.Message}");
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
//                Debug.WriteLine($"[TileMyDriveUc] EditTextBox_LostFocus - saving changes");
//                StopEditing();
//            }
//        }

//        public void EditTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
//        {
//            if (!_isInEditMode) return;

//            Debug.WriteLine($"[TileMyDriveUc] EditTextBox_KeyDown: {e.Key}");

//            switch (e.Key)
//            {
//                case VirtualKey.Enter:
//                    e.Handled = true;
//                    Debug.WriteLine($"[TileMyDriveUc] Enter pressed - saving");
//                    StopEditing();
//                    break;

//                case VirtualKey.Escape:
//                    e.Handled = true;
//                    Debug.WriteLine($"[TileMyDriveUc] Escape pressed - cancelling");
//                    CancelEditing();
//                    break;

//                case VirtualKey.Tab:
//                    e.Handled = true;
//                    Debug.WriteLine($"[TileMyDriveUc] Tab pressed - saving");
//                    StopEditing();
//                    break;
//            }
//        }

//        // Остальные методы без изменений...
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

//        protected override void UpdateSize()
//        {
//            base.UpdateSize();

//            if (progressBar == null || GridUsedSpaceString == null || BorderTotalSizeString == null ||
//                tbFreeSpaceString == null || tbUsedSpaceSString == null || tbTotalSizeString == null)
//            {
//                return;
//            }

//            // Если в режиме редактирования - скрываем дополнительные элементы
//            if (DisplayMode == "List" || _isInEditMode)
//            {
//                SetElementVisibility(false, 0, 0);
//                return;
//            }

//            switch (Size.ToLower())
//            {
//                case "tiny":
//                case "extra small":
//                case "small":
//                    BorderTotalSizeString.Height = 1;
//                    BorderTotalSizeString.Width = 1;
//                    SetElementVisibility(false, 0, 0);
//                    break;
//                case "medium":
//                    BorderTotalSizeString.Height = 45;
//                    BorderTotalSizeString.Width = 40;
//                    SetElementVisibility(true, 10, 18);
//                    break;
//                case "large":
//                    BorderTotalSizeString.Height = 75;
//                    BorderTotalSizeString.Width = 45;
//                    SetElementVisibility(true, 12, 20);
//                    break;
//                case "extra large":
//                case "huge":
//                    BorderTotalSizeString.Height = 85;
//                    BorderTotalSizeString.Width = 50;
//                    SetElementVisibility(true, 14, 22);
//                    break;
//                default:
//                    Debug.WriteLine($"[TileMyDriveUc] Неизвестный размер: {Size}");
//                    break;
//            }
//        }

//        private void SetElementVisibility(bool isVisible, double fontSize, double indHeight)
//        {
//            progressBar.Visibility = isVisible.ToVisibility();
//            BorderTotalSizeString.Visibility = isVisible.ToVisibility();
//            GridUsedSpaceString.Visibility = isVisible.ToVisibility();
//            tbFreeSpaceString.Visibility = isVisible.ToVisibility();
//            tbUsedSpaceSString.Visibility = isVisible.ToVisibility();

//            double actualFontSize = isVisible ? fontSize : 1;
//            tbFreeSpaceString.FontSize = actualFontSize;
//            tbUsedSpaceSString.FontSize = actualFontSize;
//            tbTotalSizeString.FontSize = actualFontSize;

//            var indicator = GetProgressBarIndicator();
//            if (indicator != null)
//            {
//                indicator.Height = isVisible ? indHeight : 0;
//            }
//        }

//        private Border GetProgressBarIndicator()
//        {
//            if (progressBar == null) return null;

//            return UIHelper.GetDescendantsOfType<Border>(progressBar)
//                .FirstOrDefault(b => b.Name == "Indicator");
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

//    public static class VisibilityExtensions
//    {
//        public static Visibility ToVisibility(this bool isVisible) =>
//            isVisible ? Visibility.Visible : Visibility.Collapsed;
//    }
//}

//using System.Diagnostics;
//using System.Linq;
//using CommunityToolkit.WinUI;
//using Microsoft.UI.Xaml;
//using Microsoft.UI.Xaml.Controls;
//using Microsoft.UI.Xaml.Input;
//using Windows.System;
//using System.IO;
//using System.Threading.Tasks;
//using System;
//using Core_FileManagement;

//namespace ufm
//{
//    public sealed partial class TileMyDriveUc : BaseTileControl
//    {
//        private ScaleAnimator _scaleAnimator;
//        private string _originalText = "";
//        private ExplorerItemViewModel _viewModel;
//        private bool _isInEditMode = false;

//        public TileMyDriveUc()
//        {
//            this.InitializeComponent();

//            if (BorderTileMyDriveUC != null)
//            {
//                _scaleAnimator = new ScaleAnimator(BorderTileMyDriveUC);
//            }
//        }

//        // Переопределяем StartEditing
//        public override void StartEditing()
//        {
//            try
//            {
//                Debug.WriteLine($"[TileMyDriveUc] StartEditing called");

//                if (_isInEditMode)
//                {
//                    Debug.WriteLine($"[TileMyDriveUc] Already in edit mode, skipping");
//                    return;
//                }

//                // Получаем ViewModel из DataContext
//                _viewModel = this.DataContext as ExplorerItemViewModel;
//                if (_viewModel == null)
//                {
//                    Debug.WriteLine($"[TileMyDriveUc] ViewModel is null");
//                    return;
//                }

//                _isInEditMode = true;
//                _originalText = _viewModel.Name;

//                Debug.WriteLine($"[TileMyDriveUc] Starting edit for: {_originalText}");

//                // Показываем TextBox, скрываем TextBlock
//                ShowEditControls(true);

//                // Устанавливаем фокус
//                this.DispatcherQueue.TryEnqueue(() =>
//                {
//                    TextBox editBox = GetCurrentEditBox();
//                    if (editBox != null)
//                    {
//                        editBox.Focus(FocusState.Programmatic);
//                        editBox.SelectAll();
//                        Debug.WriteLine($"[TileMyDriveUc] Focus set to edit box");
//                    }
//                });
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyDriveUc] Error in StartEditing: {ex.Message}");
//                _isInEditMode = false;
//            }
//        }

//        // Переопределяем StopEditing
//        public override void StopEditing()
//        {
//            try
//            {
//                Debug.WriteLine($"[TileMyDriveUc] StopEditing called");

//                if (!_isInEditMode) return;

//                string newText = GetCurrentEditBox()?.Text?.Trim() ?? "";

//                if (!string.IsNullOrEmpty(newText) && newText != _originalText)
//                {
//                    Debug.WriteLine($"[TileMyDriveUc] Saving changes: {_originalText} -> {newText}");
//                    SaveChanges(newText);
//                }
//                else
//                {
//                    Debug.WriteLine($"[TileMyDriveUc] No changes to save");
//                }

//                FinishEditing();
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyDriveUc] Error in StopEditing: {ex.Message}");
//                FinishEditing();
//            }
//        }

//        // Переопределяем CancelEditing
//        public override void CancelEditing()
//        {
//            try
//            {
//                Debug.WriteLine($"[TileMyDriveUc] CancelEditing called");

//                if (!_isInEditMode) return;

//                Debug.WriteLine($"[TileMyDriveUc] Cancelling changes, restoring: {_originalText}");

//                if (_viewModel != null && !string.IsNullOrEmpty(_originalText))
//                {
//                    _viewModel.Name = _originalText;
//                }

//                FinishEditing();
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyDriveUc] Error in CancelEditing: {ex.Message}");
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
//                Debug.WriteLine($"[TileMyDriveUc] Error saving changes: {ex.Message}");
//            }
//        }

//        private void FinishEditing()
//        {
//            try
//            {
//                Debug.WriteLine($"[TileMyDriveUc] FinishEditing called");

//                // Скрываем TextBox, показываем TextBlock
//                ShowEditControls(false);

//                _isInEditMode = false;
//                _viewModel = null;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyDriveUc] Error in FinishEditing: {ex.Message}");
//                _isInEditMode = false;
//                _viewModel = null;
//            }
//        }

//        private void ShowEditControls(bool showEdit)
//        {
//            try
//            {
//                if (showEdit)
//                {
//                    Debug.WriteLine($"[TileMyDriveUc] ShowEditControls: Entering edit mode");

//                    // Скрываем обычные текстовые блоки
//                    HorizontalTextBlock.Visibility = Visibility.Collapsed;
//                    VerticalTextBlock.Visibility = Visibility.Collapsed;
//                    ListTextBlock.Visibility = Visibility.Collapsed;

//                    // Показываем поля редактирования
//                    HorizontalEditBox.Visibility = Visibility.Visible;
//                    VerticalEditBox.Visibility = Visibility.Visible;
//                    ListEditBox.Visibility = Visibility.Visible;

//                    // Скрываем дополнительные элементы при редактировании
//                    SetElementVisibility(false, 0, 0);

//                    // Устанавливаем текст в TextBox
//                    if (_viewModel != null)
//                    {
//                        string currentText = _viewModel.Name;
//                        HorizontalEditBox.Text = currentText;
//                        VerticalEditBox.Text = currentText;
//                        ListEditBox.Text = currentText;
//                    }
//                }
//                else
//                {
//                    Debug.WriteLine($"[TileMyDriveUc] ShowEditControls: Exiting edit mode");

//                    // Показываем обычные текстовые блоки
//                    HorizontalTextBlock.Visibility = Visibility.Visible;
//                    VerticalTextBlock.Visibility = Visibility.Visible;
//                    ListTextBlock.Visibility = Visibility.Visible;

//                    // Скрываем поля редактирования
//                    HorizontalEditBox.Visibility = Visibility.Collapsed;
//                    VerticalEditBox.Visibility = Visibility.Collapsed;
//                    ListEditBox.Visibility = Visibility.Collapsed;

//                    // Вызываем UpdateSize для восстановления UI
//                    UpdateSize();
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyDriveUc] Error in ShowEditControls: {ex.Message}");
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
//                Debug.WriteLine($"[TileMyDriveUc] EditTextBox_LostFocus - saving changes");
//                StopEditing();
//            }
//        }

//        public void EditTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
//        {
//            if (!_isInEditMode) return;

//            Debug.WriteLine($"[TileMyDriveUc] EditTextBox_KeyDown: {e.Key}");

//            switch (e.Key)
//            {
//                case VirtualKey.Enter:
//                    e.Handled = true;
//                    Debug.WriteLine($"[TileMyDriveUc] Enter pressed - saving");
//                    StopEditing();
//                    break;

//                case VirtualKey.Escape:
//                    e.Handled = true;
//                    Debug.WriteLine($"[TileMyDriveUc] Escape pressed - cancelling");
//                    CancelEditing();
//                    break;

//                case VirtualKey.Tab:
//                    e.Handled = true;
//                    Debug.WriteLine($"[TileMyDriveUc] Tab pressed - saving");
//                    StopEditing();
//                    break;
//            }
//        }

//        // Остальные методы без изменений...
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

//        protected override void UpdateSize()
//        {
//            base.UpdateSize();

//            if (progressBar == null || GridUsedSpaceString == null || BorderTotalSizeString == null ||
//                tbFreeSpaceString == null || tbUsedSpaceSString == null || tbTotalSizeString == null)
//            {
//                return;
//            }

//            // Скрываем для List режима
//            if (DisplayMode == "List")
//            {
//                SetElementVisibility(false, 0, 0);
//                return;
//            }

//            Debug.WriteLine($"[TileMyDriveUc] UpdateSize: Size = {Size}, DisplayMode = {DisplayMode}");

//            switch (Size.ToLower())
//            {
//                case "tiny":
//                case "extra small":
//                case "small":
//                    BorderTotalSizeString.Height = 1;
//                    BorderTotalSizeString.Width = 1;
//                    SetElementVisibility(false, 0, 0);
//                    break;
//                case "medium":
//                    BorderTotalSizeString.Height = 45;
//                    BorderTotalSizeString.Width = 40;
//                    SetElementVisibility(true, 10, 18);
//                    break;
//                case "large":
//                    BorderTotalSizeString.Height = 75;
//                    BorderTotalSizeString.Width = 45;
//                    SetElementVisibility(true, 12, 20);
//                    break;
//                case "extra large":
//                case "huge":
//                    BorderTotalSizeString.Height = 85;
//                    BorderTotalSizeString.Width = 50;
//                    SetElementVisibility(true, 14, 22);
//                    break;
//                default:
//                    Debug.WriteLine($"[TileMyDriveUc] Неизвестный размер: {Size}");
//                    break;
//            }
//        }

//        private void SetElementVisibility(bool isVisible, double fontSize, double indHeight)
//        {
//            try
//            {
//                Debug.WriteLine($"[TileMyDriveUc] SetElementVisibility: isVisible = {isVisible}, fontSize = {fontSize}");

//                // Безопасная проверка и установка видимости
//                if (progressBar != null)
//                    progressBar.Visibility = isVisible.ToVisibility();

//                if (BorderTotalSizeString != null)
//                    BorderTotalSizeString.Visibility = isVisible.ToVisibility();

//                if (GridUsedSpaceString != null)
//                    GridUsedSpaceString.Visibility = isVisible.ToVisibility();

//                if (tbFreeSpaceString != null)
//                {
//                    tbFreeSpaceString.Visibility = isVisible.ToVisibility();
//                    tbFreeSpaceString.FontSize = isVisible ? fontSize : 1;
//                }

//                if (tbUsedSpaceSString != null)
//                {
//                    tbUsedSpaceSString.Visibility = isVisible.ToVisibility();
//                    tbUsedSpaceSString.FontSize = isVisible ? fontSize : 1;
//                }

//                if (tbTotalSizeString != null)
//                {
//                    tbTotalSizeString.Visibility = isVisible.ToVisibility();
//                    tbTotalSizeString.FontSize = isVisible ? fontSize : 1;
//                }

//                var indicator = GetProgressBarIndicator();
//                if (indicator != null)
//                {
//                    indicator.Height = isVisible ? indHeight : 0;
//                    Debug.WriteLine($"[TileMyDriveUc] SetElementVisibility: Indicator height = {indicator.Height}");
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[TileMyDriveUc] Error in SetElementVisibility: {ex.Message}");
//            }
//        }

//        private Border GetProgressBarIndicator()
//        {
//            if (progressBar == null) return null;

//            return UIHelper.GetDescendantsOfType<Border>(progressBar)
//                .FirstOrDefault(b => b.Name == "Indicator");
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

//    public static class VisibilityExtensions
//    {
//        public static Visibility ToVisibility(this bool isVisible) =>
//            isVisible ? Visibility.Visible : Visibility.Collapsed;
//    }
//}

using System.Diagnostics;
using System.Linq;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using System.IO;
using System.Threading.Tasks;
using System;
using Core_FileManagement;

namespace ufm
{
    public sealed partial class TileMyDriveUc : BaseTileControl
    {
        private ScaleAnimator _scaleAnimator;
        private string _originalText = "";
        private ExplorerItemViewModel _viewModel;
        private bool _isInEditMode = false;

        public TileMyDriveUc()
        {
            this.InitializeComponent();

            if (BorderTileMyDriveUC != null)
            {
                _scaleAnimator = new ScaleAnimator(BorderTileMyDriveUC);
            }
        }

        // Переопределяем StartEditing
        public override void StartEditing()
        {
            try
            {
                Debug.WriteLine($"[TileMyDriveUc] StartEditing called");

                if (_isInEditMode)
                {
                    Debug.WriteLine($"[TileMyDriveUc] Already in edit mode, skipping");
                    return;
                }

                // Получаем ViewModel из DataContext
                _viewModel = this.DataContext as ExplorerItemViewModel;
                if (_viewModel == null)
                {
                    Debug.WriteLine($"[TileMyDriveUc] ViewModel is null");
                    return;
                }

                _isInEditMode = true;
                _originalText = _viewModel.Name;

                Debug.WriteLine($"[TileMyDriveUc] Starting edit for: {_originalText}");

                // Скрываем обычные текстовые блоки
                HorizontalTextBlock.Visibility = Visibility.Collapsed;
                VerticalTextBlock.Visibility = Visibility.Collapsed;
                ListTextBlock.Visibility = Visibility.Collapsed;

                // Показываем поля редактирования
                HorizontalEditBox.Visibility = Visibility.Visible;
                VerticalEditBox.Visibility = Visibility.Visible;
                ListEditBox.Visibility = Visibility.Visible;

                // Скрываем дополнительные элементы при редактировании
                progressBar.Visibility = Visibility.Collapsed;
                BorderTotalSizeString.Visibility = Visibility.Collapsed;
                GridUsedSpaceString.Visibility = Visibility.Collapsed;
                tbFreeSpaceString.Visibility = Visibility.Collapsed;
                tbUsedSpaceSString.Visibility = Visibility.Collapsed;

                // Устанавливаем текст в TextBox
                string currentText = _viewModel.Name;
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
                        Debug.WriteLine($"[TileMyDriveUc] Focus set to edit box");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileMyDriveUc] Error in StartEditing: {ex.Message}");
                _isInEditMode = false;
            }
        }

        // Переопределяем StopEditing
        public override void StopEditing()
        {
            try
            {
                Debug.WriteLine($"[TileMyDriveUc] StopEditing called");

                if (!_isInEditMode) return;

                string newText = GetCurrentEditBox()?.Text?.Trim() ?? "";

                if (!string.IsNullOrEmpty(newText) && newText != _originalText)
                {
                    Debug.WriteLine($"[TileMyDriveUc] Saving changes: {_originalText} -> {newText}");
                    SaveChanges(newText);
                }
                else
                {
                    Debug.WriteLine($"[TileMyDriveUc] No changes to save");
                }

                FinishEditing();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileMyDriveUc] Error in StopEditing: {ex.Message}");
                FinishEditing();
            }
        }

        // Переопределяем CancelEditing
        public override void CancelEditing()
        {
            try
            {
                Debug.WriteLine($"[TileMyDriveUc] CancelEditing called");

                if (!_isInEditMode) return;

                Debug.WriteLine($"[TileMyDriveUc] Cancelling changes, restoring: {_originalText}");

                if (_viewModel != null && !string.IsNullOrEmpty(_originalText))
                {
                    _viewModel.Name = _originalText;
                }

                FinishEditing();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileMyDriveUc] Error in CancelEditing: {ex.Message}");
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
                    _viewModel.Name = newText;

                    // Вызываем команду сохранения если она доступна
                    if (_viewModel.SaveEditCommand?.CanExecute(null) == true)
                    {
                        _viewModel.SaveEditCommand.Execute(null);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileMyDriveUc] Error saving changes: {ex.Message}");
            }
        }

        private void FinishEditing()
        {
            try
            {
                Debug.WriteLine($"[TileMyDriveUc] FinishEditing called");

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
                progressBar.Visibility = Visibility.Visible;
                BorderTotalSizeString.Visibility = Visibility.Visible;
                GridUsedSpaceString.Visibility = Visibility.Visible;
                tbFreeSpaceString.Visibility = Visibility.Visible;
                tbUsedSpaceSString.Visibility = Visibility.Visible;

                _viewModel = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileMyDriveUc] Error in FinishEditing: {ex.Message}");
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
                Debug.WriteLine($"[TileMyDriveUc] EditTextBox_LostFocus - saving changes");
                StopEditing();
            }
        }

        public void EditTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (!_isInEditMode) return;

            Debug.WriteLine($"[TileMyDriveUc] EditTextBox_KeyDown: {e.Key}");

            switch (e.Key)
            {
                case VirtualKey.Enter:
                    e.Handled = true;
                    Debug.WriteLine($"[TileMyDriveUc] Enter pressed - saving");
                    StopEditing();
                    break;

                case VirtualKey.Escape:
                    e.Handled = true;
                    Debug.WriteLine($"[TileMyDriveUc] Escape pressed - cancelling");
                    CancelEditing();
                    break;

                case VirtualKey.Tab:
                    e.Handled = true;
                    Debug.WriteLine($"[TileMyDriveUc] Tab pressed - saving");
                    StopEditing();
                    break;
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

        protected override void UpdateSize()
        {
            base.UpdateSize();

            if (progressBar == null || GridUsedSpaceString == null || BorderTotalSizeString == null ||
                tbFreeSpaceString == null || tbUsedSpaceSString == null || tbTotalSizeString == null)
            {
                return;
            }

            // Если в режиме редактирования - выходим
            if (_isInEditMode)
            {
                return;
            }

            // Скрываем для List режима
            if (DisplayMode == "List")
            {
                SetElementVisibility(false, 0, 0);
                return;
            }

            switch (Size.ToLower())
            {
                case "tiny":
                case "extra small":
                case "small":
                    BorderTotalSizeString.Height = 1;
                    BorderTotalSizeString.Width = 1;
                    SetElementVisibility(false, 0, 0);
                    break;
                case "medium":
                    BorderTotalSizeString.Height = 45;
                    BorderTotalSizeString.Width = 40;
                    SetElementVisibility(true, 10, 18);
                    break;
                case "large":
                    BorderTotalSizeString.Height = 75;
                    BorderTotalSizeString.Width = 45;
                    SetElementVisibility(true, 12, 20);
                    break;
                case "extra large":
                case "huge":
                    BorderTotalSizeString.Height = 85;
                    BorderTotalSizeString.Width = 50;
                    SetElementVisibility(true, 14, 22);
                    break;
                default:
                    Debug.WriteLine($"[TileMyDriveUc] Неизвестный размер: {Size}");
                    break;
            }
        }

        private void SetElementVisibility(bool isVisible, double fontSize, double indHeight)
        {
            progressBar.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            BorderTotalSizeString.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            GridUsedSpaceString.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            tbFreeSpaceString.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            tbUsedSpaceSString.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;

            double actualFontSize = isVisible ? fontSize : 1;
            tbFreeSpaceString.FontSize = actualFontSize;
            tbUsedSpaceSString.FontSize = actualFontSize;
            tbTotalSizeString.FontSize = actualFontSize;

            var indicator = GetProgressBarIndicator();
            if (indicator != null)
            {
                indicator.Height = isVisible ? indHeight : 0;
            }
        }

        private Border GetProgressBarIndicator()
        {
            if (progressBar == null) return null;

            return UIHelper.GetDescendantsOfType<Border>(progressBar)
                .FirstOrDefault(b => b.Name == "Indicator");
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

    public static class VisibilityExtensions
    {
        public static Visibility ToVisibility(this bool isVisible) =>
            isVisible ? Visibility.Visible : Visibility.Collapsed;
    }
}