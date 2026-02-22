//using Core_FileManagement;
//using Microsoft.UI.Xaml;
//using Microsoft.UI.Xaml.Controls;
//using Microsoft.UI.Xaml.Input;
//using Microsoft.UI.Xaml.Media;
//using System;
//using System.Diagnostics;
//using System.Threading.Tasks;
//using Windows.Storage;
//using Windows.System;

//namespace ufm
//{
//    public abstract partial class BaseTileControl : UserControl
//    {
//        // Свойство зависимости для размера
//        public static readonly DependencyProperty SizeProperty =
//            DependencyProperty.Register(
//                nameof(Size),
//                typeof(string),
//                typeof(BaseTileControl),
//                new PropertyMetadata(null, OnSizeChanged));

//        // Свойство зависимости для режима отображения
//        public static readonly DependencyProperty DisplayModeProperty =
//            DependencyProperty.Register(
//                nameof(DisplayMode),
//                typeof(string),
//                typeof(BaseTileControl),
//                new PropertyMetadata("Horizontal", OnDisplayModeChanged));

//        // Свойства зависимости для размера иконки
//        public static readonly DependencyProperty IconWidthProperty =
//            DependencyProperty.Register(
//                nameof(IconWidth),
//                typeof(double),
//                typeof(BaseTileControl),
//                new PropertyMetadata(0));

//        public static readonly DependencyProperty IconHeightProperty =
//            DependencyProperty.Register(
//                nameof(IconHeight),
//                typeof(double),
//                typeof(BaseTileControl),
//                new PropertyMetadata(0));

//        // Свойства зависимости для размера шрифта и выбора шрифта
//        public new static readonly DependencyProperty FontSizeProperty =
//            DependencyProperty.Register(
//                nameof(FontSize),
//                typeof(double),
//                typeof(BaseTileControl),
//                new PropertyMetadata(0));

//        public new static readonly DependencyProperty FontFamilyProperty =
//            DependencyProperty.Register(
//                nameof(FontFamily),
//                typeof(FontFamily),
//                typeof(BaseTileControl),
//                new PropertyMetadata(null));

//        //04 11 2025
//        public static readonly DependencyProperty MaxIconHeightProperty =
//            DependencyProperty.Register(
//                nameof(MaxIconHeight),
//                typeof(double),
//                typeof(BaseTileControl),
//                new PropertyMetadata(100.0));

//        //15 01 2026
//        // Свойство зависимости для режима редактирования
//        public static readonly DependencyProperty IsEditingProperty =
//            DependencyProperty.Register(
//                nameof(IsEditing),
//                typeof(bool),
//                typeof(BaseTileControl),
//                new PropertyMetadata(false, OnIsEditingChanged));

//        // Событие для уведомления о начале/окончании редактирования
//        public event EventHandler<bool> EditStateChanged;

//        // Поля для редактирования
//        private string _originalText = "";
//        private ExplorerItemViewModel _viewModel;
//        private bool _isInEditMode = false;

//        // Свойство Size
//        public string Size
//        {
//            get => (string)GetValue(SizeProperty);
//            set => SetValue(SizeProperty, value);
//        }

//        // Свойство DisplayMode
//        public string DisplayMode
//        {
//            get => (string)GetValue(DisplayModeProperty);
//            set => SetValue(DisplayModeProperty, value);
//        }

//        // Свойство IconWidth
//        public int IconWidth
//        {
//            get => (int)GetValue(IconWidthProperty);
//            set => SetValue(IconWidthProperty, value);
//        }

//        // Свойство IconHeight
//        public int IconHeight
//        {
//            get => (int)GetValue(IconHeightProperty);
//            set => SetValue(IconHeightProperty, value);
//        }

//        // Свойство FontSize
//        public new int FontSize
//        {
//            get => (int)GetValue(FontSizeProperty);
//            set => SetValue(FontSizeProperty, value);
//        }

//        // Свойство FontFamily
//        public new FontFamily FontFamily
//        {
//            get => (FontFamily)GetValue(FontFamilyProperty);
//            set => SetValue(FontFamilyProperty, value);
//        }

//        //04 11 2025
//        public double MaxIconHeight
//        {
//            get => (double)GetValue(MaxIconHeightProperty);
//            set => SetValue(MaxIconHeightProperty, value);
//        }

//        // Свойство IsEditing
//        public bool IsEditing
//        {
//            get => (bool)GetValue(IsEditingProperty);
//            set => SetValue(IsEditingProperty, value);
//        }

//        // Обработчик изменения свойства Size
//        private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
//        {
//            if (d is BaseTileControl control)
//            {
//                control.UpdateSize();
//            }
//        }

//        // Обработчик изменения режима отображения
//        private static void OnDisplayModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
//        {
//            if (d is BaseTileControl control)
//            {
//                control.OnDisplayModeChanged();
//            }
//        }

//        protected virtual void OnDisplayModeChanged()
//        {
//            // Переопределяется в наследниках
//        }

//        // Обработчик изменения режима редактирования
//        private static void OnIsEditingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
//        {
//            if (d is BaseTileControl control)
//            {
//                var oldValue = (bool)e.OldValue;
//                var newValue = (bool)e.NewValue;

//                // Вызываем метод обработки изменения состояния
//                control.HandleIsEditingChanged(oldValue, newValue);
//                control.EditStateChanged?.Invoke(control, newValue);
//            }
//        }

//        // Метод обработки изменения состояния редактирования
//        protected void HandleIsEditingChanged(bool oldValue, bool newValue)
//        {
//            Debug.WriteLine($"[BaseTileControl] HandleIsEditingChanged: {oldValue} -> {newValue}");

//            if (_viewModel != null && _viewModel.IsEditing != newValue)
//            {
//                _viewModel.IsEditing = newValue;
//                _viewModel.EditRequested = newValue;

//                if (newValue && string.IsNullOrEmpty(_viewModel.NewNameForEdit))
//                {
//                    _viewModel.NewNameForEdit = _viewModel.Name;
//                }
//            }
//        }

//        // Виртуальный метод для обработки изменения режима редактирования
//        protected virtual void OnIsEditingChanged(bool oldValue, bool newValue)
//        {
//            // Переопределяется в наследниках
//        }

//        // Абстрактные свойства для получения элементов управления (должны быть реализованы в наследниках)
//        protected abstract TextBlock GetHorizontalTextBlock();
//        protected abstract TextBlock GetVerticalTextBlock();
//        protected abstract TextBlock GetListTextBlock();

//        protected abstract TextBox GetHorizontalEditBox();
//        protected abstract TextBox GetVerticalEditBox();
//        protected abstract TextBox GetListEditBox();

//        protected abstract FrameworkElement GetHorizontalLayout();
//        protected abstract FrameworkElement GetVerticalLayout();
//        protected abstract FrameworkElement GetListLayout();

//        // Виртуальные методы для дополнительных действий
//        protected virtual void OnStartEditing() { }
//        protected virtual void OnFinishEditing() { }
//        protected virtual void OnSaveChanges(string newText) { }
//        protected virtual void OnCancelChanges() { }

//        public virtual void StartEditing()
//        {
//            try
//            {
//                Debug.WriteLine($"[BaseTileControl] StartEditing called in {GetType().Name}");

//                if (IsEditing)
//                {
//                    Debug.WriteLine($"[BaseTileControl] Already in edit mode (IsEditing=true), skipping");
//                    return;
//                }

//                // Получаем ViewModel из DataContext
//                _viewModel = this.DataContext as ExplorerItemViewModel;
//                if (_viewModel == null)
//                {
//                    Debug.WriteLine($"[BaseTileControl] ViewModel is null");
//                    return;
//                }

//                _originalText = _viewModel.Name;

//                Debug.WriteLine($"[BaseTileControl] Starting edit for: {_originalText}");

//                // ВАЖНО: Устанавливаем IsEditing в true через DependencyProperty
//                // Это вызовет OnIsEditingChanged через статический обработчик
//                IsEditing = true;

//                // Инициализируем временное свойство в ViewModel
//                _viewModel.NewNameForEdit = _originalText;

//                // Скрываем обычные текстовые блоки
//                GetHorizontalTextBlock().Visibility = Visibility.Collapsed;
//                GetVerticalTextBlock().Visibility = Visibility.Collapsed;
//                GetListTextBlock().Visibility = Visibility.Collapsed;

//                // Показываем поля редактирования
//                GetHorizontalEditBox().Visibility = Visibility.Visible;
//                GetVerticalEditBox().Visibility = Visibility.Visible;
//                GetListEditBox().Visibility = Visibility.Visible;

//                // Вызываем метод для скрытия дополнительных элементов
//                OnStartEditing();

//                // Устанавливаем текст в TextBox
//                string currentText = _viewModel.NewNameForEdit;
//                GetHorizontalEditBox().Text = currentText;
//                GetVerticalEditBox().Text = currentText;
//                GetListEditBox().Text = currentText;

//                // Устанавливаем фокус
//                this.DispatcherQueue.TryEnqueue(() =>
//                {
//                    TextBox editBox = GetCurrentEditBox();
//                    if (editBox != null)
//                    {
//                        editBox.Focus(FocusState.Programmatic);
//                        editBox.SelectAll();
//                        Debug.WriteLine($"[BaseTileControl] Focus set to edit box");
//                    }
//                });
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[BaseTileControl] Error in StartEditing: {ex.Message}");
//                IsEditing = false;
//            }
//        }

//        public virtual void StopEditing()
//        {
//            try
//            {
//                Debug.WriteLine($"[BaseTileControl] StopEditing called in {GetType().Name}");

//                if (!IsEditing) return;

//                string newText = GetCurrentEditBox()?.Text?.Trim() ?? "";

//                if (!string.IsNullOrEmpty(newText) && newText != _originalText)
//                {
//                    Debug.WriteLine($"[BaseTileControl] Saving changes: {_originalText} -> {newText}");
//                    SaveChanges(newText);
//                }
//                else
//                {
//                    Debug.WriteLine($"[BaseTileControl] No changes to save");
//                    FinishEditing();
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[BaseTileControl] Error in StopEditing: {ex.Message}");
//                FinishEditing();
//            }
//        }

//        public virtual void CancelEditing()
//        {
//            try
//            {
//                Debug.WriteLine($"[BaseTileControl] CancelEditing called in {GetType().Name}");

//                if (!IsEditing) return;

//                Debug.WriteLine($"[BaseTileControl] Cancelling changes, restoring: {_originalText}");

//                if (_viewModel != null)
//                {
//                    // Восстанавливаем оригинальное имя в ViewModel
//                    _viewModel.Name = _originalText;
//                    _viewModel.NewNameForEdit = _originalText;
//                    _viewModel.CancelEdit();

//                    // Вызываем метод для отмены изменений
//                    OnCancelChanges();
//                }

//                FinishEditing();
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[BaseTileControl] Error in CancelEditing: {ex.Message}");
//                FinishEditing();
//            }
//        }

//        public virtual bool CanEdit => false;

//        // Получение текущего TextBox в зависимости от DisplayMode
//        protected TextBox GetCurrentEditBox()
//        {
//            switch (DisplayMode)
//            {
//                case "Horizontal":
//                    return GetHorizontalEditBox();
//                case "Vertical":
//                    return GetVerticalEditBox();
//                case "List":
//                    return GetListEditBox();
//                default:
//                    return GetHorizontalEditBox();
//            }
//        }

//        private void SaveChanges(string newText)
//        {
//            try
//            {
//                if (_viewModel != null)
//                {
//                    Debug.WriteLine($"[BaseTileControl] SaveChanges called with: '{newText}'");

//                    // Устанавливаем новое имя во временное свойство ViewModel
//                    _viewModel.NewNameForEdit = newText;

//                    // Вызываем метод для сохранения изменений
//                    OnSaveChanges(newText);

//                    Debug.WriteLine($"[BaseTileControl] NewNameForEdit set to: '{_viewModel.NewNameForEdit}'");
//                    Debug.WriteLine($"[BaseTileControl] CanSaveEdit: {_viewModel.SaveEditCommand?.CanExecute(null)}");

//                    // Вызываем команду сохранения
//                    if (_viewModel.SaveEditCommand?.CanExecute(null) == true)
//                    {
//                        Debug.WriteLine($"[BaseTileControl] SaveEditCommand can execute, calling Execute");
//                        _viewModel.SaveEditCommand.Execute(null);
//                        _ = DispatcherQueue.TryEnqueue(async () =>
//                        {
//                            await Task.Delay(10);
//                            FinishEditing();
//                        });
//                    }
//                    else
//                    {
//                        Debug.WriteLine($"[BaseTileControl] SaveEditCommand cannot execute.");
//                        FinishEditing();
//                    }
//                }
//                else
//                {
//                    Debug.WriteLine($"[BaseTileControl] ViewModel is null, cannot save changes");
//                    FinishEditing();
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[BaseTileControl] Error in SaveChanges: {ex.Message}");
//                FinishEditing();
//            }
//        }

//        private void FinishEditing()
//        {
//            try
//            {
//                Debug.WriteLine($"[BaseTileControl] FinishEditing called");

//                // ВАЖНО: Сбрасываем через DependencyProperty
//                IsEditing = false;

//                // Сбрасываем только внутренние флаги
//                _originalText = "";
//                _viewModel = null;

//                // Показываем обычные текстовые блоки
//                GetHorizontalTextBlock().Visibility = Visibility.Visible;
//                GetVerticalTextBlock().Visibility = Visibility.Visible;
//                GetListTextBlock().Visibility = Visibility.Visible;

//                // Скрываем поля редактирования
//                GetHorizontalEditBox().Visibility = Visibility.Collapsed;
//                GetVerticalEditBox().Visibility = Visibility.Collapsed;
//                GetListEditBox().Visibility = Visibility.Collapsed;

//                // Вызываем метод для восстановления дополнительных элементов
//                OnFinishEditing();
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[BaseTileControl] Error in FinishEditing: {ex.Message}");
//                IsEditing = false;
//            }
//        }

//        // Метод для обновления размеров и параметров
//        public void UpdateSize(string selectedSize)
//        {
//            Size = selectedSize;

//            InvalidateArrange();
//            InvalidateMeasure();
//            UpdateLayout();
//        }

//        // Метод для обновления размеров на основе текущих значений
//        protected virtual void UpdateSize()
//        {
//            string targetSize = Size;

//            // Если нигде не нашли, используем по умолчанию
//            if (string.IsNullOrEmpty(targetSize))
//            {
//                targetSize = "Medium";
//            }
//            else
//            {
//            }

//            // Получаем размеры из менеджера и ПРЯМОЕ ПРИСВОЕНИЕ в свойства
//            (Width, Height, IconWidth, IconHeight, FontSize, var fontFamilyString) = SizeManagerTile.GetSize(targetSize);

//            // Устанавливаем FontFamily из строки
//            FontFamily = new FontFamily(fontFamilyString);

//            //04 11 2025
//            if (DisplayMode == "Vertical")
//            {
//                MaxIconHeight = Height * 0.6; // 60% от высоты элемента
//            }
//            else
//            {
//                MaxIconHeight = double.PositiveInfinity; // Без ограничений
//            }
//            // Принудительно обновляем макет
//            InvalidateMeasure();
//            InvalidateArrange();
//            UpdateLayout();
//        }

//        // Конструктор
//        protected BaseTileControl()
//        {
//            this.DefaultStyleKey = typeof(BaseTileControl);
//            UpdateSize(); // Инициализация размеров при создании
//        }

//        // Переопределение метода применения шаблона
//        protected override void OnApplyTemplate()
//        {
//            base.OnApplyTemplate();
//            UpdateSize(); // Гарантируем обновление после применения шаблона
//        }

//        // Обработчики событий для TextBox (должны быть подключены в наследниках)
//        protected void EditTextBox_LostFocus(object sender, RoutedEventArgs e)
//        {
//            if (IsEditing)
//            {
//                Debug.WriteLine($"[BaseTileControl] EditTextBox_LostFocus - saving changes");
//                StopEditing();
//            }
//        }

//        protected void EditTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
//        {
//            if (!IsEditing) return;

//            Debug.WriteLine($"[BaseTileControl] EditTextBox_KeyDown: {e.Key}");

//            switch (e.Key)
//            {
//                case VirtualKey.Enter:
//                    e.Handled = true;
//                    Debug.WriteLine($"[BaseTileControl] Enter pressed - saving");

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
//                    Debug.WriteLine($"[BaseTileControl] Escape pressed - cancelling");
//                    CancelEditing();
//                    break;

//                case VirtualKey.Tab:
//                    e.Handled = true;
//                    Debug.WriteLine($"[BaseTileControl] Tab pressed - saving");

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

//        protected void EditTextBox_TextChanged(object sender, TextChangedEventArgs e)
//        {
//            if (IsEditing && _viewModel != null)
//            {
//                TextBox textBox = sender as TextBox;
//                if (textBox != null)
//                {
//                    _viewModel.NewNameForEdit = textBox.Text;
//                    Debug.WriteLine($"[BaseTileControl] TextChanged: NewNameForEdit = '{textBox.Text}'");
//                }
//            }
//        }
//    }
//}




//using Core_FileManagement;
//using Microsoft.UI.Xaml;
//using Microsoft.UI.Xaml.Controls;
//using Microsoft.UI.Xaml.Input;
//using Microsoft.UI.Xaml.Media;
//using System;
//using System.Diagnostics;
//using System.Threading.Tasks;
//using Windows.Storage;
//using Windows.System;

//namespace ufm
//{
//    public abstract partial class BaseTileControl : UserControl
//    {
//        // Свойство зависимости для размера
//        public static readonly DependencyProperty SizeProperty =
//            DependencyProperty.Register(
//                nameof(Size),
//                typeof(string),
//                typeof(BaseTileControl),
//                new PropertyMetadata(null, OnSizeChanged));

//        // Свойство зависимости для режима отображения
//        public static readonly DependencyProperty DisplayModeProperty =
//            DependencyProperty.Register(
//                nameof(DisplayMode),
//                typeof(string),
//                typeof(BaseTileControl),
//                new PropertyMetadata("Horizontal", OnDisplayModeChanged));

//        // Свойства зависимости для размера иконки
//        public static readonly DependencyProperty IconWidthProperty =
//            DependencyProperty.Register(
//                nameof(IconWidth),
//                typeof(double),
//                typeof(BaseTileControl),
//                new PropertyMetadata(0));

//        public static readonly DependencyProperty IconHeightProperty =
//            DependencyProperty.Register(
//                nameof(IconHeight),
//                typeof(double),
//                typeof(BaseTileControl),
//                new PropertyMetadata(0));

//        // Свойства зависимости для размера шрифта и выбора шрифта
//        public new static readonly DependencyProperty FontSizeProperty =
//            DependencyProperty.Register(
//                nameof(FontSize),
//                typeof(double),
//                typeof(BaseTileControl),
//                new PropertyMetadata(0));

//        public new static readonly DependencyProperty FontFamilyProperty =
//            DependencyProperty.Register(
//                nameof(FontFamily),
//                typeof(FontFamily),
//                typeof(BaseTileControl),
//                new PropertyMetadata(null));

//        //04 11 2025
//        public static readonly DependencyProperty MaxIconHeightProperty =
//            DependencyProperty.Register(
//                nameof(MaxIconHeight),
//                typeof(double),
//                typeof(BaseTileControl),
//                new PropertyMetadata(100.0));

//        //15 01 2026
//        // Свойство зависимости для режима редактирования
//        public static readonly DependencyProperty IsEditingProperty =
//            DependencyProperty.Register(
//                nameof(IsEditing),
//                typeof(bool),
//                typeof(BaseTileControl),
//                new PropertyMetadata(false, OnIsEditingChanged));

//        // Событие для уведомления о начале/окончании редактирования
//        public event EventHandler<bool> EditStateChanged;

//        // Поля для редактирования
//        private string _originalText = "";
//        private ExplorerItemViewModel _viewModel;
//        private bool _isInEditMode = false;

//        // Свойство Size
//        public string Size
//        {
//            get => (string)GetValue(SizeProperty);
//            set => SetValue(SizeProperty, value);
//        }

//        // Свойство DisplayMode
//        public string DisplayMode
//        {
//            get => (string)GetValue(DisplayModeProperty);
//            set => SetValue(DisplayModeProperty, value);
//        }

//        // Свойство IconWidth
//        public int IconWidth
//        {
//            get => (int)GetValue(IconWidthProperty);
//            set => SetValue(IconWidthProperty, value);
//        }

//        // Свойство IconHeight
//        public int IconHeight
//        {
//            get => (int)GetValue(IconHeightProperty);
//            set => SetValue(IconHeightProperty, value);
//        }

//        // Свойство FontSize
//        public new int FontSize
//        {
//            get => (int)GetValue(FontSizeProperty);
//            set => SetValue(FontSizeProperty, value);
//        }

//        // Свойство FontFamily
//        public new FontFamily FontFamily
//        {
//            get => (FontFamily)GetValue(FontFamilyProperty);
//            set => SetValue(FontFamilyProperty, value);
//        }

//        //04 11 2025
//        public double MaxIconHeight
//        {
//            get => (double)GetValue(MaxIconHeightProperty);
//            set => SetValue(MaxIconHeightProperty, value);
//        }

//        // Свойство IsEditing
//        public bool IsEditing
//        {
//            get => (bool)GetValue(IsEditingProperty);
//            set => SetValue(IsEditingProperty, value);
//        }

//        // Обработчик изменения свойства Size
//        private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
//        {
//            if (d is BaseTileControl control)
//            {
//                control.UpdateSize();
//            }
//        }

//        // Обработчик изменения режима отображения
//        private static void OnDisplayModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
//        {
//            if (d is BaseTileControl control)
//            {
//                control.OnDisplayModeChanged();
//            }
//        }

//        protected virtual void OnDisplayModeChanged()
//        {
//            // Переопределяется в наследниках
//        }

//        // Обработчик изменения режима редактирования
//        private static void OnIsEditingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
//        {
//            if (d is BaseTileControl control)
//            {
//                var oldValue = (bool)e.OldValue;
//                var newValue = (bool)e.NewValue;

//                // Вызываем метод обработки изменения состояния
//                control.HandleIsEditingChanged(oldValue, newValue);
//                control.EditStateChanged?.Invoke(control, newValue);
//            }
//        }

//        // Метод обработки изменения состояния редактирования (ИСПРАВЛЕНО)
//        protected void HandleIsEditingChanged(bool oldValue, bool newValue)
//        {
//            Debug.WriteLine($"[BaseTileControl] HandleIsEditingChanged: {oldValue} -> {newValue}");

//            // Получаем ViewModel из DataContext, если _viewModel уже null
//            var viewModel = _viewModel ?? this.DataContext as ExplorerItemViewModel;

//            if (viewModel != null && viewModel.IsEditing != newValue)
//            {
//                viewModel.IsEditing = newValue;
//                viewModel.EditRequested = newValue;

//                if (newValue && string.IsNullOrEmpty(viewModel.NewNameForEdit))
//                {
//                    viewModel.NewNameForEdit = viewModel.Name;
//                }
//            }
//        }

//        // Виртуальный метод для обработки изменения режима редактирования
//        protected virtual void OnIsEditingChanged(bool oldValue, bool newValue)
//        {
//            // Переопределяется в наследниках
//        }

//        // Абстрактные свойства для получения элементов управления (должны быть реализованы в наследниках)
//        protected abstract TextBlock GetHorizontalTextBlock();
//        protected abstract TextBlock GetVerticalTextBlock();
//        protected abstract TextBlock GetListTextBlock();

//        protected abstract TextBox GetHorizontalEditBox();
//        protected abstract TextBox GetVerticalEditBox();
//        protected abstract TextBox GetListEditBox();

//        protected abstract FrameworkElement GetHorizontalLayout();
//        protected abstract FrameworkElement GetVerticalLayout();
//        protected abstract FrameworkElement GetListLayout();

//        // Виртуальные методы для дополнительных действий
//        protected virtual void OnStartEditing() { }
//        protected virtual void OnFinishEditing() { }
//        protected virtual void OnSaveChanges(string newText) { }
//        protected virtual void OnCancelChanges() { }

//        public virtual void StartEditing()
//        {
//            try
//            {
//                Debug.WriteLine($"[BaseTileControl] StartEditing called in {GetType().Name}");

//                if (IsEditing)
//                {
//                    Debug.WriteLine($"[BaseTileControl] Already in edit mode (IsEditing=true), skipping");
//                    return;
//                }

//                // Получаем ViewModel из DataContext
//                _viewModel = this.DataContext as ExplorerItemViewModel;
//                if (_viewModel == null)
//                {
//                    Debug.WriteLine($"[BaseTileControl] ViewModel is null");
//                    return;
//                }

//                // Если ViewModel уже в режиме редактирования, но контрол нет - синхронизируем
//                if (_viewModel.IsEditing)
//                {
//                    Debug.WriteLine($"[BaseTileControl] ViewModel.IsEditing=true but control not, synchronizing");
//                    _viewModel.IsEditing = false;
//                }

//                _originalText = _viewModel.Name;

//                Debug.WriteLine($"[BaseTileControl] Starting edit for: {_originalText}");

//                // Инициализируем временное свойство в ViewModel
//                _viewModel.NewNameForEdit = _originalText;

//                // Скрываем обычные текстовые блоки
//                GetHorizontalTextBlock().Visibility = Visibility.Collapsed;
//                GetVerticalTextBlock().Visibility = Visibility.Collapsed;
//                GetListTextBlock().Visibility = Visibility.Collapsed;

//                // Показываем поля редактирования
//                GetHorizontalEditBox().Visibility = Visibility.Visible;
//                GetVerticalEditBox().Visibility = Visibility.Visible;
//                GetListEditBox().Visibility = Visibility.Visible;

//                // Вызываем метод для скрытия дополнительных элементов
//                OnStartEditing();

//                // Устанавливаем текст в TextBox
//                string currentText = _viewModel.NewNameForEdit;
//                GetHorizontalEditBox().Text = currentText;
//                GetVerticalEditBox().Text = currentText;
//                GetListEditBox().Text = currentText;

//                // ВАЖНО: Устанавливаем IsEditing в true через DependencyProperty ПОСЛЕ всей подготовки
//                IsEditing = true;

//                // Устанавливаем фокус
//                this.DispatcherQueue.TryEnqueue(() =>
//                {
//                    TextBox editBox = GetCurrentEditBox();
//                    if (editBox != null)
//                    {
//                        editBox.Focus(FocusState.Programmatic);
//                        editBox.SelectAll();
//                        Debug.WriteLine($"[BaseTileControl] Focus set to edit box");
//                    }
//                });
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[BaseTileControl] Error in StartEditing: {ex.Message}");
//                IsEditing = false;
//            }
//        }

//        public virtual void StopEditing()
//        {
//            try
//            {
//                Debug.WriteLine($"[BaseTileControl] StopEditing called in {GetType().Name}");

//                if (!IsEditing) return;

//                string newText = GetCurrentEditBox()?.Text?.Trim() ?? "";

//                if (!string.IsNullOrEmpty(newText) && newText != _originalText)
//                {
//                    Debug.WriteLine($"[BaseTileControl] Saving changes: {_originalText} -> {newText}");
//                    SaveChanges(newText);
//                }
//                else
//                {
//                    Debug.WriteLine($"[BaseTileControl] No changes to save");
//                    FinishEditing();
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[BaseTileControl] Error in StopEditing: {ex.Message}");
//                FinishEditing();
//            }
//        }

//        public virtual void CancelEditing()
//        {
//            try
//            {
//                Debug.WriteLine($"[BaseTileControl] CancelEditing called in {GetType().Name}");

//                if (!IsEditing) return;

//                Debug.WriteLine($"[BaseTileControl] Cancelling changes, restoring: {_originalText}");

//                if (_viewModel != null)
//                {
//                    // Восстанавливаем оригинальное имя в ViewModel
//                    _viewModel.Name = _originalText;
//                    _viewModel.NewNameForEdit = _originalText;
//                    _viewModel.CancelEdit();

//                    // Вызываем метод для отмены изменений
//                    OnCancelChanges();
//                }

//                FinishEditing();
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[BaseTileControl] Error in CancelEditing: {ex.Message}");
//                FinishEditing();
//            }
//        }

//        public virtual bool CanEdit => false;

//        // Получение текущего TextBox в зависимости от DisplayMode
//        protected TextBox GetCurrentEditBox()
//        {
//            switch (DisplayMode)
//            {
//                case "Horizontal":
//                    return GetHorizontalEditBox();
//                case "Vertical":
//                    return GetVerticalEditBox();
//                case "List":
//                    return GetListEditBox();
//                default:
//                    return GetHorizontalEditBox();
//            }
//        }

//        private void SaveChanges(string newText)
//        {
//            try
//            {
//                if (_viewModel != null)
//                {
//                    Debug.WriteLine($"[BaseTileControl] SaveChanges called with: '{newText}'");

//                    // Устанавливаем новое имя во временное свойство ViewModel
//                    _viewModel.NewNameForEdit = newText;

//                    // Вызываем метод для сохранения изменений
//                    OnSaveChanges(newText);

//                    Debug.WriteLine($"[BaseTileControl] NewNameForEdit set to: '{_viewModel.NewNameForEdit}'");
//                    Debug.WriteLine($"[BaseTileControl] CanSaveEdit: {_viewModel.SaveEditCommand?.CanExecute(null)}");

//                    // Вызываем команду сохранения
//                    if (_viewModel.SaveEditCommand?.CanExecute(null) == true)
//                    {
//                        Debug.WriteLine($"[BaseTileControl] SaveEditCommand can execute, calling Execute");
//                        _viewModel.SaveEditCommand.Execute(null);
//                        _ = DispatcherQueue.TryEnqueue(async () =>
//                        {
//                            await Task.Delay(10);
//                            FinishEditing();
//                        });
//                    }
//                    else
//                    {
//                        Debug.WriteLine($"[BaseTileControl] SaveEditCommand cannot execute.");
//                        FinishEditing();
//                    }
//                }
//                else
//                {
//                    Debug.WriteLine($"[BaseTileControl] ViewModel is null, cannot save changes");
//                    FinishEditing();
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[BaseTileControl] Error in SaveChanges: {ex.Message}");
//                FinishEditing();
//            }
//        }

//        // ИСПРАВЛЕННЫЙ МЕТОД FinishEditing
//        private void FinishEditing()
//        {
//            try
//            {
//                Debug.WriteLine($"[BaseTileControl] FinishEditing called");

//                // Показываем обычные текстовые блоки
//                GetHorizontalTextBlock().Visibility = Visibility.Visible;
//                GetVerticalTextBlock().Visibility = Visibility.Visible;
//                GetListTextBlock().Visibility = Visibility.Visible;

//                // Скрываем поля редактирования
//                GetHorizontalEditBox().Visibility = Visibility.Collapsed;
//                GetVerticalEditBox().Visibility = Visibility.Collapsed;
//                GetListEditBox().Visibility = Visibility.Collapsed;

//                // Вызываем метод для восстановления дополнительных элементов
//                OnFinishEditing();

//                // ВАЖНО: Сначала сохраняем ссылку на ViewModel
//                var viewModel = _viewModel;

//                // Сбрасываем внутренние флаги ДО изменения IsEditing
//                _originalText = "";
//                _viewModel = null;

//                // Теперь меняем состояние редактирования
//                IsEditing = false;

//                // Явно обновляем ViewModel, если она существует
//                if (viewModel != null)
//                {
//                    viewModel.IsEditing = false;
//                    viewModel.EditRequested = false;
//                    // Не сбрасываем NewNameForEdit, так как он может понадобиться для следующего редактирования
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[BaseTileControl] Error in FinishEditing: {ex.Message}");
//                IsEditing = false;
//            }
//        }

//        // Метод для обновления размеров и параметров
//        public void UpdateSize(string selectedSize)
//        {
//            Size = selectedSize;

//            InvalidateArrange();
//            InvalidateMeasure();
//            UpdateLayout();
//        }

//        // Метод для обновления размеров на основе текущих значений
//        protected virtual void UpdateSize()
//        {
//            string targetSize = Size;

//            // Если нигде не нашли, используем по умолчанию
//            if (string.IsNullOrEmpty(targetSize))
//            {
//                targetSize = "Medium";
//            }
//            else
//            {
//            }

//            // Получаем размеры из менеджера и ПРЯМОЕ ПРИСВОЕНИЕ в свойства
//            (Width, Height, IconWidth, IconHeight, FontSize, var fontFamilyString) = SizeManagerTile.GetSize(targetSize);

//            // Устанавливаем FontFamily из строки
//            FontFamily = new FontFamily(fontFamilyString);

//            //04 11 2025
//            if (DisplayMode == "Vertical")
//            {
//                MaxIconHeight = Height * 0.6; // 60% от высоты элемента
//            }
//            else
//            {
//                MaxIconHeight = double.PositiveInfinity; // Без ограничений
//            }
//            // Принудительно обновляем макет
//            InvalidateMeasure();
//            InvalidateArrange();
//            UpdateLayout();
//        }

//        // Конструктор
//        protected BaseTileControl()
//        {
//            this.DefaultStyleKey = typeof(BaseTileControl);
//            UpdateSize(); // Инициализация размеров при создании
//        }

//        // Переопределение метода применения шаблона
//        protected override void OnApplyTemplate()
//        {
//            base.OnApplyTemplate();
//            UpdateSize(); // Гарантируем обновление после применения шаблона
//        }

//        // Обработчики событий для TextBox (должны быть подключены в наследниках)
//        protected void EditTextBox_LostFocus(object sender, RoutedEventArgs e)
//        {
//            if (IsEditing)
//            {
//                Debug.WriteLine($"[BaseTileControl] EditTextBox_LostFocus - saving changes");
//                StopEditing();
//            }
//        }

//        protected void EditTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
//        {
//            if (!IsEditing) return;

//            Debug.WriteLine($"[BaseTileControl] EditTextBox_KeyDown: {e.Key}");

//            switch (e.Key)
//            {
//                case VirtualKey.Enter:
//                    e.Handled = true;
//                    Debug.WriteLine($"[BaseTileControl] Enter pressed - saving");

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
//                    Debug.WriteLine($"[BaseTileControl] Escape pressed - cancelling");
//                    CancelEditing();
//                    break;

//                case VirtualKey.Tab:
//                    e.Handled = true;
//                    Debug.WriteLine($"[BaseTileControl] Tab pressed - saving");

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

//        protected void EditTextBox_TextChanged(object sender, TextChangedEventArgs e)
//        {
//            if (IsEditing && _viewModel != null)
//            {
//                TextBox textBox = sender as TextBox;
//                if (textBox != null)
//                {
//                    _viewModel.NewNameForEdit = textBox.Text;
//                    Debug.WriteLine($"[BaseTileControl] TextChanged: NewNameForEdit = '{textBox.Text}'");
//                }
//            }
//        }
//    }
//}


using Core_FileManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

namespace ufm
{
    // Результат редактирования для множественного переименования
    public enum EditResult { Saved, Cancelled, Error }

    public abstract partial class BaseTileControl : UserControl
    {
        // Свойство зависимости для размера
        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(string),
                typeof(BaseTileControl),
                new PropertyMetadata(null, OnSizeChanged));

        // Свойство зависимости для режима отображения
        public static readonly DependencyProperty DisplayModeProperty =
            DependencyProperty.Register(
                nameof(DisplayMode),
                typeof(string),
                typeof(BaseTileControl),
                new PropertyMetadata("Horizontal", OnDisplayModeChanged));

        // Свойства зависимости для размера иконки
        public static readonly DependencyProperty IconWidthProperty =
            DependencyProperty.Register(
                nameof(IconWidth),
                typeof(double),
                typeof(BaseTileControl),
                new PropertyMetadata(0));

        public static readonly DependencyProperty IconHeightProperty =
            DependencyProperty.Register(
                nameof(IconHeight),
                typeof(double),
                typeof(BaseTileControl),
                new PropertyMetadata(0));

        // Свойства зависимости для размера шрифта и выбора шрифта
        public new static readonly DependencyProperty FontSizeProperty =
            DependencyProperty.Register(
                nameof(FontSize),
                typeof(double),
                typeof(BaseTileControl),
                new PropertyMetadata(0));

        public new static readonly DependencyProperty FontFamilyProperty =
            DependencyProperty.Register(
                nameof(FontFamily),
                typeof(FontFamily),
                typeof(BaseTileControl),
                new PropertyMetadata(null));

        //04 11 2025
        public static readonly DependencyProperty MaxIconHeightProperty =
            DependencyProperty.Register(
                nameof(MaxIconHeight),
                typeof(double),
                typeof(BaseTileControl),
                new PropertyMetadata(100.0));

        //15 01 2026
        // Свойство зависимости для режима редактирования
        public static readonly DependencyProperty IsEditingProperty =
            DependencyProperty.Register(
                nameof(IsEditing),
                typeof(bool),
                typeof(BaseTileControl),
                new PropertyMetadata(false, OnIsEditingChanged));

        // Событие для уведомления о начале/окончании редактирования
        public event EventHandler<bool> EditStateChanged;

        // НОВОЕ: Событие для уведомления о результате редактирования (для множественного переименования)
        public event EventHandler<EditResult> EditCompleted;

        // Поля для редактирования
        private string _originalText = "";
        private ExplorerItemViewModel _viewModel;
        private bool _isInEditMode = false;

        // Свойство Size
        public string Size
        {
            get => (string)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        // Свойство DisplayMode
        public string DisplayMode
        {
            get => (string)GetValue(DisplayModeProperty);
            set => SetValue(DisplayModeProperty, value);
        }

        // Свойство IconWidth
        public int IconWidth
        {
            get => (int)GetValue(IconWidthProperty);
            set => SetValue(IconWidthProperty, value);
        }

        // Свойство IconHeight
        public int IconHeight
        {
            get => (int)GetValue(IconHeightProperty);
            set => SetValue(IconHeightProperty, value);
        }

        // Свойство FontSize
        public new int FontSize
        {
            get => (int)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        // Свойство FontFamily
        public new FontFamily FontFamily
        {
            get => (FontFamily)GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        //04 11 2025
        public double MaxIconHeight
        {
            get => (double)GetValue(MaxIconHeightProperty);
            set => SetValue(MaxIconHeightProperty, value);
        }

        // Свойство IsEditing
        public bool IsEditing
        {
            get => (bool)GetValue(IsEditingProperty);
            set => SetValue(IsEditingProperty, value);
        }

        // Обработчик изменения свойства Size
        private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BaseTileControl control)
            {
                control.UpdateSize();
            }
        }

        // Обработчик изменения режима отображения
        private static void OnDisplayModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BaseTileControl control)
            {
                control.OnDisplayModeChanged();
            }
        }

        protected virtual void OnDisplayModeChanged()
        {
            // Переопределяется в наследниках
        }

        // Обработчик изменения режима редактирования
        private static void OnIsEditingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BaseTileControl control)
            {
                var oldValue = (bool)e.OldValue;
                var newValue = (bool)e.NewValue;

                // Вызываем метод обработки изменения состояния
                control.HandleIsEditingChanged(oldValue, newValue);
                control.EditStateChanged?.Invoke(control, newValue);
            }
        }

        // Метод обработки изменения состояния редактирования (ИСПРАВЛЕНО)
        protected void HandleIsEditingChanged(bool oldValue, bool newValue)
        {
            Debug.WriteLine($"[BaseTileControl] HandleIsEditingChanged: {oldValue} -> {newValue}");

            // Получаем ViewModel из DataContext, если _viewModel уже null
            var viewModel = _viewModel ?? this.DataContext as ExplorerItemViewModel;

            if (viewModel != null && viewModel.IsEditing != newValue)
            {
                viewModel.IsEditing = newValue;
                viewModel.EditRequested = newValue;

                if (newValue && string.IsNullOrEmpty(viewModel.NewNameForEdit))
                {
                    viewModel.NewNameForEdit = viewModel.Name;
                }
            }
        }

        // Виртуальный метод для обработки изменения режима редактирования
        protected virtual void OnIsEditingChanged(bool oldValue, bool newValue)
        {
            // Переопределяется в наследниках
        }

        // Абстрактные свойства для получения элементов управления (должны быть реализованы в наследниках)
        protected abstract TextBlock GetHorizontalTextBlock();
        protected abstract TextBlock GetVerticalTextBlock();
        protected abstract TextBlock GetListTextBlock();

        protected abstract TextBox GetHorizontalEditBox();
        protected abstract TextBox GetVerticalEditBox();
        protected abstract TextBox GetListEditBox();

        protected abstract FrameworkElement GetHorizontalLayout();
        protected abstract FrameworkElement GetVerticalLayout();
        protected abstract FrameworkElement GetListLayout();

        // Виртуальные методы для дополнительных действий
        protected virtual void OnStartEditing() { }
        protected virtual void OnFinishEditing() { }
        protected virtual void OnSaveChanges(string newText) { }
        protected virtual void OnCancelChanges() { }

        public virtual void StartEditing()
        {
            try
            {
                Debug.WriteLine($"[BaseTileControl] StartEditing called in {GetType().Name}");

                if (IsEditing)
                {
                    Debug.WriteLine($"[BaseTileControl] Already in edit mode (IsEditing=true), skipping");
                    return;
                }

                // Получаем ViewModel из DataContext
                _viewModel = this.DataContext as ExplorerItemViewModel;
                if (_viewModel == null)
                {
                    Debug.WriteLine($"[BaseTileControl] ViewModel is null");
                    return;
                }

                // Если ViewModel уже в режиме редактирования, но контрол нет - синхронизируем
                if (_viewModel.IsEditing)
                {
                    Debug.WriteLine($"[BaseTileControl] ViewModel.IsEditing=true but control not, synchronizing");
                    _viewModel.IsEditing = false;
                }

                _originalText = _viewModel.Name;

                Debug.WriteLine($"[BaseTileControl] Starting edit for: {_originalText}");

                // Инициализируем временное свойство в ViewModel
                _viewModel.NewNameForEdit = _originalText;

                // Скрываем обычные текстовые блоки
                GetHorizontalTextBlock().Visibility = Visibility.Collapsed;
                GetVerticalTextBlock().Visibility = Visibility.Collapsed;
                GetListTextBlock().Visibility = Visibility.Collapsed;

                // Показываем поля редактирования
                GetHorizontalEditBox().Visibility = Visibility.Visible;
                GetVerticalEditBox().Visibility = Visibility.Visible;
                GetListEditBox().Visibility = Visibility.Visible;

                // Вызываем метод для скрытия дополнительных элементов
                OnStartEditing();

                // Устанавливаем текст в TextBox
                string currentText = _viewModel.NewNameForEdit;
                GetHorizontalEditBox().Text = currentText;
                GetVerticalEditBox().Text = currentText;
                GetListEditBox().Text = currentText;

                // ВАЖНО: Устанавливаем IsEditing в true через DependencyProperty ПОСЛЕ всей подготовки
                IsEditing = true;

                // Устанавливаем фокус
                this.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
                {
                    TextBox editBox = GetCurrentEditBox();
                    if (editBox != null)
                    {
                        editBox.Focus(FocusState.Programmatic);
                        editBox.SelectAll();
                        Debug.WriteLine($"[BaseTileControl] Focus set to edit box");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BaseTileControl] Error in StartEditing: {ex.Message}");
                IsEditing = false;
            }
        }

        public virtual void StopEditing()
        {
            try
            {
                Debug.WriteLine($"[BaseTileControl] StopEditing called in {GetType().Name}");

                if (!IsEditing) return;

                string newText = GetCurrentEditBox()?.Text?.Trim() ?? "";

                if (!string.IsNullOrEmpty(newText) && newText != _originalText)
                {
                    Debug.WriteLine($"[BaseTileControl] Saving changes: {_originalText} -> {newText}");
                    SaveChanges(newText);
                }
                else
                {
                    Debug.WriteLine($"[BaseTileControl] No changes to save");
                    FinishEditing(EditResult.Saved); // Передаем результат Saved, хотя изменений не было
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BaseTileControl] Error in StopEditing: {ex.Message}");
                FinishEditing(EditResult.Error);
            }
        }

        public virtual void CancelEditing()
        {
            try
            {
                Debug.WriteLine($"[BaseTileControl] CancelEditing called in {GetType().Name}");

                if (!IsEditing) return;

                Debug.WriteLine($"[BaseTileControl] Cancelling changes, restoring: {_originalText}");

                if (_viewModel != null)
                {
                    // Восстанавливаем оригинальное имя в ViewModel
                    _viewModel.Name = _originalText;
                    _viewModel.NewNameForEdit = _originalText;
                    _viewModel.CancelEdit();

                    // Вызываем метод для отмены изменений
                    OnCancelChanges();
                }

                FinishEditing(EditResult.Cancelled);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BaseTileControl] Error in CancelEditing: {ex.Message}");
                FinishEditing(EditResult.Error);
            }
        }

        public virtual bool CanEdit => false;

        // Получение текущего TextBox в зависимости от DisplayMode
        protected TextBox GetCurrentEditBox()
        {
            switch (DisplayMode)
            {
                case "Horizontal":
                    return GetHorizontalEditBox();
                case "Vertical":
                    return GetVerticalEditBox();
                case "List":
                    return GetListEditBox();
                default:
                    return GetHorizontalEditBox();
            }
        }

        private void SaveChanges(string newText)
        {
            try
            {
                if (_viewModel != null)
                {
                    Debug.WriteLine($"[BaseTileControl] SaveChanges called with: '{newText}'");

                    // Устанавливаем новое имя во временное свойство ViewModel
                    _viewModel.NewNameForEdit = newText;

                    // Вызываем метод для сохранения изменений
                    OnSaveChanges(newText);

                    Debug.WriteLine($"[BaseTileControl] NewNameForEdit set to: '{_viewModel.NewNameForEdit}'");
                    Debug.WriteLine($"[BaseTileControl] CanSaveEdit: {_viewModel.SaveEditCommand?.CanExecute(null)}");

                    // Вызываем команду сохранения
                    if (_viewModel.SaveEditCommand?.CanExecute(null) == true)
                    {
                        Debug.WriteLine($"[BaseTileControl] SaveEditCommand can execute, calling Execute");
                        _viewModel.SaveEditCommand.Execute(null);
                        _ = DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, async () =>
                        {
                            await Task.Delay(10);
                            FinishEditing(EditResult.Saved);
                        });
                    }
                    else
                    {
                        Debug.WriteLine($"[BaseTileControl] SaveEditCommand cannot execute.");
                        FinishEditing(EditResult.Error);
                    }
                }
                else
                {
                    Debug.WriteLine($"[BaseTileControl] ViewModel is null, cannot save changes");
                    FinishEditing(EditResult.Error);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BaseTileControl] Error in SaveChanges: {ex.Message}");
                FinishEditing(EditResult.Error);
            }
        }

        // ИСПРАВЛЕННЫЙ МЕТОД FinishEditing с параметром результата
        private void FinishEditing(EditResult result)
        {
            try
            {
                Debug.WriteLine($"[BaseTileControl] FinishEditing called with result: {result}");

                // Показываем обычные текстовые блоки
                GetHorizontalTextBlock().Visibility = Visibility.Visible;
                GetVerticalTextBlock().Visibility = Visibility.Visible;
                GetListTextBlock().Visibility = Visibility.Visible;

                // Скрываем поля редактирования
                GetHorizontalEditBox().Visibility = Visibility.Collapsed;
                GetVerticalEditBox().Visibility = Visibility.Collapsed;
                GetListEditBox().Visibility = Visibility.Collapsed;

                // Вызываем метод для восстановления дополнительных элементов
                OnFinishEditing();

                // ВАЖНО: Сначала сохраняем ссылку на ViewModel
                var viewModel = _viewModel;

                // Сбрасываем внутренние флаги ДО изменения IsEditing
                _originalText = "";
                _viewModel = null;

                // Теперь меняем состояние редактирования
                IsEditing = false;

                // Явно обновляем ViewModel, если она существует
                if (viewModel != null)
                {
                    viewModel.IsEditing = false;
                    viewModel.EditRequested = false;
                    // Не сбрасываем NewNameForEdit, так как он может понадобиться для следующего редактирования
                }

                // Вызываем событие о завершении редактирования
                EditCompleted?.Invoke(this, result);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BaseTileControl] Error in FinishEditing: {ex.Message}");
                IsEditing = false;
                EditCompleted?.Invoke(this, EditResult.Error);
            }
        }

        // Метод для обновления размеров и параметров
        public void UpdateSize(string selectedSize)
        {
            Size = selectedSize;

            InvalidateArrange();
            InvalidateMeasure();
            UpdateLayout();
        }

        // Метод для обновления размеров на основе текущих значений
        protected virtual void UpdateSize()
        {
            string targetSize = Size;

            // Если нигде не нашли, используем по умолчанию
            if (string.IsNullOrEmpty(targetSize))
            {
                targetSize = "Medium";
            }
            else
            {
            }

            // Получаем размеры из менеджера и ПРЯМОЕ ПРИСВОЕНИЕ в свойства
            (Width, Height, IconWidth, IconHeight, FontSize, var fontFamilyString) = SizeManagerTile.GetSize(targetSize);

            // Устанавливаем FontFamily из строки
            FontFamily = new FontFamily(fontFamilyString);

            //04 11 2025
            if (DisplayMode == "Vertical")
            {
                MaxIconHeight = Height * 0.6; // 60% от высоты элемента
            }
            else
            {
                MaxIconHeight = double.PositiveInfinity; // Без ограничений
            }
            // Принудительно обновляем макет
            InvalidateMeasure();
            InvalidateArrange();
            UpdateLayout();
        }

        // Конструктор
        protected BaseTileControl()
        {
            this.DefaultStyleKey = typeof(BaseTileControl);
            UpdateSize(); // Инициализация размеров при создании
        }

        // Переопределение метода применения шаблона
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdateSize(); // Гарантируем обновление после применения шаблона
        }

        // Обработчики событий для TextBox (должны быть подключены в наследниках)
        protected void EditTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (IsEditing)
            {
                Debug.WriteLine($"[BaseTileControl] EditTextBox_LostFocus - saving changes");
                StopEditing();
            }
        }

        protected void EditTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (!IsEditing) return;

            Debug.WriteLine($"[BaseTileControl] EditTextBox_KeyDown: {e.Key}");

            switch (e.Key)
            {
                case VirtualKey.Enter:
                    e.Handled = true;
                    Debug.WriteLine($"[BaseTileControl] Enter pressed - saving");

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
                    Debug.WriteLine($"[BaseTileControl] Escape pressed - cancelling");
                    CancelEditing();
                    break;

                case VirtualKey.Tab:
                    e.Handled = true;
                    Debug.WriteLine($"[BaseTileControl] Tab pressed - saving");

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

        protected void EditTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsEditing && _viewModel != null)
            {
                TextBox textBox = sender as TextBox;
                if (textBox != null)
                {
                    _viewModel.NewNameForEdit = textBox.Text;
                    Debug.WriteLine($"[BaseTileControl] TextChanged: NewNameForEdit = '{textBox.Text}'");
                }
            }
        }
    }
}