using Core_FileManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.System;

namespace ufm
{
    public enum EditResult
    {
        Saved,
        Cancelled,
        Error,
        CancelAll
    }

    public abstract partial class BaseTileControl : UserControl
    {
        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(string),
                typeof(BaseTileControl),
                new PropertyMetadata(null, OnSizeChanged));

        public static readonly DependencyProperty DisplayModeProperty =
            DependencyProperty.Register(
                nameof(DisplayMode),
                typeof(string),
                typeof(BaseTileControl),
                new PropertyMetadata("Horizontal", OnDisplayModeChanged));

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

        public static readonly DependencyProperty MaxIconHeightProperty =
            DependencyProperty.Register(
                nameof(MaxIconHeight),
                typeof(double),
                typeof(BaseTileControl),
                new PropertyMetadata(100.0));

        public static readonly DependencyProperty IsEditingProperty =
            DependencyProperty.Register(
                nameof(IsEditing),
                typeof(bool),
                typeof(BaseTileControl),
                new PropertyMetadata(false, OnIsEditingChanged));

        public event EventHandler<bool> EditStateChanged;
        public event EventHandler<EditResult> EditCompleted;

        private string _originalText = "";
        private ExplorerItemViewModel _viewModel;
        private bool _isInEditMode = false;

        private DateTime _escapePressTime = DateTime.MinValue;
        private bool _escapeHoldCheckActive = false;
        private const int ESCAPE_HOLD_MS = 500;

        public bool IsInMultiRenameMode { get; set; } = false;

        public string Size
        {
            get => (string)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public string DisplayMode
        {
            get => (string)GetValue(DisplayModeProperty);
            set => SetValue(DisplayModeProperty, value);
        }

        public int IconWidth
        {
            get => (int)GetValue(IconWidthProperty);
            set => SetValue(IconWidthProperty, value);
        }

        public int IconHeight
        {
            get => (int)GetValue(IconHeightProperty);
            set => SetValue(IconHeightProperty, value);
        }

        public new int FontSize
        {
            get => (int)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public new FontFamily FontFamily
        {
            get => (FontFamily)GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public double MaxIconHeight
        {
            get => (double)GetValue(MaxIconHeightProperty);
            set => SetValue(MaxIconHeightProperty, value);
        }

        public bool IsEditing
        {
            get => (bool)GetValue(IsEditingProperty);
            set => SetValue(IsEditingProperty, value);
        }

        private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BaseTileControl control)
            {
                control.UpdateSize();
            }
        }

        private static void OnDisplayModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BaseTileControl control)
            {
                control.OnDisplayModeChanged();
            }
        }

        protected virtual void OnDisplayModeChanged()
        {
        }

        private static void OnIsEditingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BaseTileControl control)
            {
                var oldValue = (bool)e.OldValue;
                var newValue = (bool)e.NewValue;
                control.HandleIsEditingChanged(oldValue, newValue);
                control.EditStateChanged?.Invoke(control, newValue);
            }
        }

        protected void HandleIsEditingChanged(bool oldValue, bool newValue)
        {
            var viewModel = _viewModel ?? DataContext as ExplorerItemViewModel;
            if (viewModel != null && viewModel.IsEditing != newValue)
            {
                viewModel.IsEditing = newValue;
                viewModel.EditRequested = newValue;
                if (newValue && string.IsNullOrEmpty(viewModel.NewNameForEdit))
                    viewModel.NewNameForEdit = viewModel.Name;
            }
        }

        protected virtual void OnIsEditingChanged(bool oldValue, bool newValue)
        {
        }

        protected abstract TextBlock GetHorizontalTextBlock();
        protected abstract TextBlock GetVerticalTextBlock();
        protected abstract TextBlock GetListTextBlock();
        protected abstract TextBox GetHorizontalEditBox();
        protected abstract TextBox GetVerticalEditBox();
        protected abstract TextBox GetListEditBox();
        protected abstract FrameworkElement GetHorizontalLayout();
        protected abstract FrameworkElement GetVerticalLayout();
        protected abstract FrameworkElement GetListLayout();

        protected virtual void OnStartEditing() { }
        protected virtual void OnFinishEditing() { }
        protected virtual void OnSaveChanges(string newText) { }
        protected virtual void OnCancelChanges() { }

        public virtual void StartEditing()
        {
            try
            {
                if (IsEditing) return;
                _viewModel = DataContext as ExplorerItemViewModel;
                if (_viewModel == null) return;
                if (_viewModel.IsEditing) _viewModel.IsEditing = false;

                _originalText = _viewModel.Name;
                _viewModel.NewNameForEdit = _originalText;

                GetHorizontalTextBlock().Visibility = Visibility.Collapsed;
                GetVerticalTextBlock().Visibility = Visibility.Collapsed;
                GetListTextBlock().Visibility = Visibility.Collapsed;
                GetHorizontalEditBox().Visibility = Visibility.Visible;
                GetVerticalEditBox().Visibility = Visibility.Visible;
                GetListEditBox().Visibility = Visibility.Visible;

                OnStartEditing();

                string currentText = _viewModel.NewNameForEdit;
                GetHorizontalEditBox().Text = currentText;
                GetVerticalEditBox().Text = currentText;
                GetListEditBox().Text = currentText;

                GetHorizontalEditBox().KeyUp -= EditTextBox_KeyUp;
                GetHorizontalEditBox().KeyUp += EditTextBox_KeyUp;
                GetVerticalEditBox().KeyUp -= EditTextBox_KeyUp;
                GetVerticalEditBox().KeyUp += EditTextBox_KeyUp;
                GetListEditBox().KeyUp -= EditTextBox_KeyUp;
                GetListEditBox().KeyUp += EditTextBox_KeyUp;

                IsEditing = true;

                DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
                {
                    TextBox editBox = GetCurrentEditBox();
                    if (editBox != null)
                    {
                        editBox.Focus(FocusState.Programmatic);
                        editBox.SelectAll();
                    }
                });
            }
            catch (Exception)
            {
                IsEditing = false;
            }
        }

        public virtual void StopEditing()
        {
            try
            {
                if (!IsEditing) return;
                string newText = GetCurrentEditBox()?.Text?.Trim() ?? "";
                if (!string.IsNullOrEmpty(newText) && newText != _originalText)
                    SaveChanges(newText);
                else
                    FinishEditing(EditResult.Saved);
            }
            catch (Exception)
            {
                FinishEditing(EditResult.Error);
            }
        }

        public virtual void CancelEditing()
        {
            try
            {
                if (!IsEditing) return;
                if (_viewModel != null)
                {
                    _viewModel.Name = _originalText;
                    _viewModel.NewNameForEdit = _originalText;
                    _viewModel.CancelEdit();
                    OnCancelChanges();
                }
                FinishEditing(EditResult.Cancelled);
            }
            catch (Exception)
            {
                FinishEditing(EditResult.Error);
            }
        }

        public void CancelAllMultiRename()
        {
            if (!IsEditing) return;
            if (_viewModel != null)
            {
                _viewModel.Name = _originalText;
                _viewModel.NewNameForEdit = _originalText;
                _viewModel.CancelEdit();
            }
            FinishEditing(EditResult.CancelAll);
        }

        public virtual bool CanEdit => false;

        protected TextBox GetCurrentEditBox()
        {
            return DisplayMode switch
            {
                "Horizontal" => GetHorizontalEditBox(),
                "Vertical" => GetVerticalEditBox(),
                "List" => GetListEditBox(),
                _ => GetHorizontalEditBox()
            };
        }

        private void SaveChanges(string newText)
        {
            try
            {
                if (_viewModel != null)
                {
                    _viewModel.NewNameForEdit = newText;
                    OnSaveChanges(newText);
                    if (_viewModel.SaveEditCommand?.CanExecute(null) == true)
                    {
                        _viewModel.SaveEditCommand.Execute(null);
                        _ = DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, async () =>
                        {
                            await Task.Delay(10);
                            FinishEditing(EditResult.Saved);
                        });
                    }
                    else FinishEditing(EditResult.Error);
                }
                else FinishEditing(EditResult.Error);
            }
            catch (Exception)
            {
                FinishEditing(EditResult.Error);
            }
        }

        private void FinishEditing(EditResult result)
        {
            try
            {
                _escapeHoldCheckActive = false;

                GetHorizontalTextBlock().Visibility = Visibility.Visible;
                GetVerticalTextBlock().Visibility = Visibility.Visible;
                GetListTextBlock().Visibility = Visibility.Visible;
                GetHorizontalEditBox().Visibility = Visibility.Collapsed;
                GetVerticalEditBox().Visibility = Visibility.Collapsed;
                GetListEditBox().Visibility = Visibility.Collapsed;

                OnFinishEditing();

                var viewModel = _viewModel;
                _originalText = "";
                _viewModel = null;
                IsEditing = false;

                if (viewModel != null)
                {
                    viewModel.IsEditing = false;
                    viewModel.EditRequested = false;
                }

                EditCompleted?.Invoke(this, result);
            }
            catch (Exception)
            {
                IsEditing = false;
                EditCompleted?.Invoke(this, EditResult.Error);
            }
        }

        public void UpdateSize(string selectedSize)
        {
            Size = selectedSize;
            InvalidateArrange();
            InvalidateMeasure();
            UpdateLayout();
        }

        protected virtual void UpdateSize()
        {
            string targetSize = Size;
            if (string.IsNullOrEmpty(targetSize)) targetSize = "Medium";

            (Width, Height, IconWidth, IconHeight, FontSize, var fontFamilyString) = SizeManagerTile.GetSize(targetSize);
            FontFamily = new FontFamily(fontFamilyString);

            if (DisplayMode == "Vertical")
                MaxIconHeight = Height * 0.6;
            else
                MaxIconHeight = double.PositiveInfinity;

            InvalidateMeasure();
            InvalidateArrange();
            UpdateLayout();
        }

        protected BaseTileControl()
        {
            DefaultStyleKey = typeof(BaseTileControl);
            UpdateSize();
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdateSize();
        }

        protected void EditTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (!IsEditing) return;

            switch (e.Key)
            {
                case VirtualKey.Enter:
                    e.Handled = true;
                    if (sender is TextBox textBox && _viewModel != null)
                        _viewModel.NewNameForEdit = textBox.Text;
                    StopEditing();
                    break;

                case VirtualKey.Escape:
                    e.Handled = true;

                    if (e.KeyStatus.WasKeyDown)
                        return;

                    if (IsInMultiRenameMode)
                    {
                        _escapePressTime = DateTime.Now;
                        _escapeHoldCheckActive = true;

                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(ESCAPE_HOLD_MS);

                            if (_escapeHoldCheckActive)
                            {
                                bool escapeStillPressed = (GetKeyState((int)VirtualKey.Escape) & 0x8000) != 0;

                                DispatcherQueue.TryEnqueue(() =>
                                {
                                    if (_escapeHoldCheckActive && IsEditing && IsInMultiRenameMode)
                                    {
                                        if (escapeStillPressed)
                                        {
                                            _escapeHoldCheckActive = false;
                                            CancelAllMultiRename();
                                        }
                                    }
                                });
                            }
                        });
                    }
                    else
                    {
                        CancelEditing();
                    }
                    break;

                case VirtualKey.Up:
                case VirtualKey.Down:
                case VirtualKey.Left:
                case VirtualKey.Right:
                    e.Handled = true;
                    break;
            }
        }

        protected void EditTextBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Escape && _escapeHoldCheckActive)
            {
                _escapeHoldCheckActive = false;
                var holdDuration = (DateTime.Now - _escapePressTime).TotalMilliseconds;

                if (IsEditing && IsInMultiRenameMode)
                {
                    if (holdDuration < ESCAPE_HOLD_MS)
                    {
                        CancelEditing();
                    }
                    else
                    {
                        CancelAllMultiRename();
                    }
                }
            }
        }

        protected void EditTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsEditing && _viewModel != null && sender is TextBox textBox)
            {
                _viewModel.NewNameForEdit = textBox.Text;
            }
        }

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);
    }
}