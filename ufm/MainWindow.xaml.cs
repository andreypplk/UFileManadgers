using Core_FileManagement;
using Core_Language;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using ufm.Pages;
using Windows.Foundation;
using Windows.Storage;
using WinRT.Interop;

namespace ufm
{
    public sealed partial class MainWindow : Window
    {
        // Поля окна
        public AppWindow m_AppWindow;
        private OverlappedPresenter _presenter;
        private IntPtr hWnd;
        private BackdropManager _backdropManager;
        public new AppWindow AppWindow { get; private set; }
        public TabViewManager TabViewManager { get; private set; }
        public TabView MainTabsView => TabsView;
        internal bool IsCreatedByDragAndDrop { get; set; } = false;

        private Win32WindowHelper win32WindowHelper;
        private readonly LocalizationViewModel _locVM;
        private readonly MainViewModel _mainVM;
        private readonly VisibilityToggler _menuFirstVisibilityToggler;
        private readonly VisibilityToggler _menuSecondVisibilityToggler;
        private StatusBarPerformanceMetricsUC _statusBar;

        // Для синхронизации доступа к настройкам
        private readonly object _settingsLock = new object();

        // Для дебаунсинга изменения размера
        private DispatcherTimer _resizeDebounceTimer;

        // Загруженные значения настроек
        private string _loadedBackdropType;
        private string _loadedCustomTheme;
        private string _loadedStandardTheme;

        // Обработчик изменения свойства ViewModel
        private PropertyChangedEventHandler _mainVMPropertyChangedHandler;

        public BackdropManager.BackdropType CurrentBackdropType =>
            _backdropManager?.CurrentBackdropType ?? BackdropManager.BackdropType.DefaultColor;

        // Публичный конструктор для обычного запуска
        public MainWindow() : this(false)
        {
        }

        // Внутренний конструктор для создания окна через перетаскивание
        internal MainWindow(bool isCreatedByDragAndDrop)
        {
            IsCreatedByDragAndDrop = isCreatedByDragAndDrop;

            try
            {
                this.InitializeComponent();
                WindowHelper.TrackWindow(this);

                _locVM = new LocalizationViewModel();
                _mainVM = new MainViewModel();
                GridGlobal.DataContext = _locVM;

                CustomThemeManager.Initialize();

                // Статусбар
                _statusBar = new StatusBarPerformanceMetricsUC();
                _statusBar.ViewModel = _mainVM.CurrentExplorerItem;
                StatusBarContentRight.Content = _statusBar;
                _mainVMPropertyChangedHandler = (s, e) =>
                {
                    if (e.PropertyName == nameof(MainViewModel.CurrentExplorerItem))
                    {
                        _statusBar.ViewModel = _mainVM.CurrentExplorerItem;
                    }
                };
                _mainVM.PropertyChanged += _mainVMPropertyChangedHandler;

                // Окно
                m_AppWindow = GetAppWindowForCurrentWindow();
                hWnd = WindowNative.GetWindowHandle(this);
                m_AppWindow.Resize(new Windows.Graphics.SizeInt32(1440, 900));
                ((FrameworkElement)this.Content).RequestedTheme = ThemeHelper.RootTheme;
                _presenter = m_AppWindow.Presenter as OverlappedPresenter;
                m_AppWindow.IsShownInSwitchers = true;
                _presenter.SetBorderAndTitleBar(true, true);
                m_AppWindow.SetIcon("folder1.ico");
                SetTitleBar(TitleBarGrid);

                // BackdropManager
                _backdropManager = new BackdropManager(this);
                _backdropManager.BackdropChanged += OnBackdropChanged;
                _backdropManager.BackdropChangeFailed += OnBackdropChangeFailed;

                // TabViewManager с флагом пропуска начальной вкладки
                TabViewManager = new TabViewManager(TabsView, ContentFrame, isCreatedByDragAndDrop);

                // VisibilityTogglers
                _menuFirstVisibilityToggler = new VisibilityToggler(
                    rootFrame: ContentFrame,
                    containerPageName: "rootPage",
                    frameName: "FrameViewDataPanel",
                    targetPageName: "ViewPage",
                    elementName: "GridMenuFirst");
                _menuSecondVisibilityToggler = new VisibilityToggler(
                    rootFrame: ContentFrame,
                    containerPageName: "rootPage",
                    frameName: "FrameViewDataPanel",
                    targetPageName: "ViewPage",
                    elementName: "GridMenuSecond");

                // Подписки
                TitleBarGrid.Loaded += TitleBarGrid_Loaded;
                TitleBarGrid.SizeChanged += TitleBarGrid_SizeChanged;
                this.Closed += MainWindow_Closed;

                // Асинхронная загрузка настроек
#pragma warning disable CS4014
                _ = LoadSettingsAsync();
#pragma warning restore CS4014
            }
            catch
            {
                throw;
            }
        }

        // ------------------------------------------------------------------------
        // Получение AppWindow
        // ------------------------------------------------------------------------
        private AppWindow GetAppWindowForCurrentWindow()
        {
            try
            {
                IntPtr hWnd = WindowNative.GetWindowHandle(this);
                WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
                return AppWindow.GetFromWindowId(wndId);
            }
            catch
            {
                throw;
            }
        }

        // ------------------------------------------------------------------------
        // Настройка минимального размера окна
        // ------------------------------------------------------------------------
        public void SetupWindowMinSize(Window window)
        {
            try
            {
                win32WindowHelper = new Win32WindowHelper(window);
                win32WindowHelper.SetWindowMinMaxSize(new Win32.POINT() { x = 800, y = 600 });
            }
            catch
            {
            }
        }

        // ------------------------------------------------------------------------
        // Обработчики строки заголовка
        // ------------------------------------------------------------------------
        private void TitleBarGrid_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ExtendsContentIntoTitleBar == true)
                {
                    SetRegionsForCustomTitleBar();
                }
            }
            catch
            {
            }
        }

        private void TitleBarGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                if (ExtendsContentIntoTitleBar != true) return;

                _resizeDebounceTimer?.Stop();
                if (_resizeDebounceTimer == null)
                {
                    _resizeDebounceTimer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(100)
                    };
                    _resizeDebounceTimer.Tick += (s, args) =>
                    {
                        _resizeDebounceTimer.Stop();
                        SetRegionsForCustomTitleBar();
                    };
                }
                _resizeDebounceTimer.Start();
            }
            catch
            {
            }
        }

        private void SetRegionsForCustomTitleBar()
        {
            try
            {
                if (TitleBarGrid == null || TitleBarGrid.XamlRoot == null || m_AppWindow == null)
                    return;

                double scaleAdjustment = TitleBarGrid.XamlRoot.RasterizationScale;
                RightPaddingColumn.Width = new GridLength(m_AppWindow.TitleBar.RightInset / scaleAdjustment);
                LeftPaddingColumn.Width = new GridLength(m_AppWindow.TitleBar.LeftInset / scaleAdjustment);

                if (TabsView != null && StackPanelSettings != null)
                {
                    GeneralTransform transform = TabsView.TransformToVisual(null);
                    Rect bounds = transform.TransformBounds(new Rect(0, 0, TabsView.ActualWidth, TabsView.ActualHeight));
                    Windows.Graphics.RectInt32 searchBoxRect = GetRect(bounds, scaleAdjustment);

                    transform = StackPanelSettings.TransformToVisual(null);
                    bounds = transform.TransformBounds(new Rect(0, 0, StackPanelSettings.ActualWidth, StackPanelSettings.ActualHeight));
                    Windows.Graphics.RectInt32 personPicRect = GetRect(bounds, scaleAdjustment);

                    var rectArray = new[] { searchBoxRect, personPicRect };
                    InputNonClientPointerSource nonClientInputSrc = InputNonClientPointerSource.GetForWindowId(m_AppWindow.Id);
                    nonClientInputSrc.SetRegionRects(NonClientRegionKind.Passthrough, rectArray);
                }
            }
            catch
            {
            }
        }

        private Windows.Graphics.RectInt32 GetRect(Rect bounds, double scale)
        {
            try
            {
                return new Windows.Graphics.RectInt32(
                    _X: (int)Math.Round(bounds.X * scale),
                    _Y: (int)Math.Round(bounds.Y * scale),
                    _Width: (int)Math.Round(bounds.Width * scale),
                    _Height: (int)Math.Round(bounds.Height * scale)
                );
            }
            catch
            {
                return new Windows.Graphics.RectInt32(0, 0, 0, 0);
            }
        }

        // ------------------------------------------------------------------------
        // BackdropManager
        // ------------------------------------------------------------------------
        private void OnBackdropChanged(string backdropName)
        {
            try
            {
                if (tbCurrentBackdrop != null)
                    tbCurrentBackdrop.Text = backdropName;
            }
            catch
            {
            }
        }

        private void OnBackdropChangeFailed(string errorMessage)
        {
            try
            {
                if (tbChangeStatus != null)
                    tbChangeStatus.Text = errorMessage;
            }
            catch
            {
            }
        }

        public void SetBackdrop(BackdropManager.BackdropType type)
        {
            try
            {
                _backdropManager?.SetBackdrop(type);
                UIHelper.AnnounceActionForAccessibility(ButSetting,
                    $"Background changed to {_backdropManager?.CurrentBackdropType}",
                    "BackgroundChangedNotificationActivityId");
            }
            catch
            {
            }
        }

        // ------------------------------------------------------------------------
        // Тема и настройки
        // ------------------------------------------------------------------------
        public void SetTheme(ElementTheme theme)
        {
            try
            {
                _backdropManager?.SetTheme(theme);
            }
            catch
            {
            }
        }

        private async Task LoadSettingsAsync()
        {
            Task<string> loadBackdropTask = Task.Run(() => LoadBackdropSettings());
            Task<(string custom, string standard)> loadThemeTask = Task.Run(() => LoadThemeSettings());

            await Task.WhenAll(loadBackdropTask, loadThemeTask);

            _loadedBackdropType = loadBackdropTask.Result;
            (_loadedCustomTheme, _loadedStandardTheme) = loadThemeTask.Result;

            DispatcherQueue.TryEnqueue(() =>
            {
                ApplyLoadedBackdrop();
                ApplyLoadedTheme();
            });
        }

        private string LoadBackdropSettings()
        {
            try
            {
                return GetSettingWithFallback("BackdropType");
            }
            catch
            {
                return null;
            }
        }

        private (string customTheme, string standardTheme) LoadThemeSettings()
        {
            try
            {
                string custom = GetSettingWithFallback("SelectedCustomTheme");
                string standard = GetSettingWithFallback("SelectedTheme");
                return (custom, standard);
            }
            catch
            {
                return (null, null);
            }
        }

        private string GetSettingWithFallback(string key)
        {
            lock (_settingsLock)
            {
                try
                {
                    string value = null;
                    if (App.SettingsManager != null)
                    {
                        value = App.SettingsManager.GetSetting<string>(key);
                    }

                    if (string.IsNullOrEmpty(value))
                    {
                        var localSettings = ApplicationData.Current.LocalSettings.Values;
                        if (localSettings?.ContainsKey(key) == true)
                        {
                            value = localSettings[key]?.ToString();
                        }
                    }

                    return value;
                }
                catch
                {
                    return null;
                }
            }
        }

        private void ApplyLoadedBackdrop()
        {
            try
            {
                if (!string.IsNullOrEmpty(_loadedBackdropType) &&
                    Enum.TryParse(_loadedBackdropType, out BackdropManager.BackdropType type))
                {
                    _backdropManager.SetBackdrop(type);
                }
                else
                {
                    _backdropManager.LoadSavedBackdrop();
                }
            }
            catch
            {
            }
        }

        private void ApplyLoadedTheme()
        {
            try
            {
                if (!string.IsNullOrEmpty(_loadedCustomTheme) &&
                    CustomThemeManager.DynamicThemeExists(_loadedCustomTheme))
                {
                    CustomThemeManager.ApplyDynamicTheme(_loadedCustomTheme);
                    bool isDark = _loadedCustomTheme.Contains("Dark", StringComparison.OrdinalIgnoreCase);
                    SetTheme(isDark ? ElementTheme.Dark : ElementTheme.Light);
                    return;
                }

                if (Enum.TryParse(_loadedCustomTheme, out CustomThemeManager.CustomThemeType customType))
                {
                    CustomThemeManager.ApplyCustomTheme(customType);
                    ElementTheme baseTheme = customType switch
                    {
                        CustomThemeManager.CustomThemeType.Light => ElementTheme.Light,
                        CustomThemeManager.CustomThemeType.Dark => ElementTheme.Dark,
                        CustomThemeManager.CustomThemeType.DarkRed => ElementTheme.Dark,
                        CustomThemeManager.CustomThemeType.Lemon => ElementTheme.Light,
                        CustomThemeManager.CustomThemeType.DarkLemon => ElementTheme.Dark,
                        CustomThemeManager.CustomThemeType.Gold => ElementTheme.Light,
                        CustomThemeManager.CustomThemeType.DarkGold => ElementTheme.Dark,
                        CustomThemeManager.CustomThemeType.Green => ElementTheme.Light,
                        CustomThemeManager.CustomThemeType.DarkGreen => ElementTheme.Dark,
                        CustomThemeManager.CustomThemeType.Blue => ElementTheme.Light,
                        CustomThemeManager.CustomThemeType.DarkBlue => ElementTheme.Dark,
                        _ => ElementTheme.Default
                    };
                    SetTheme(baseTheme);
                    return;
                }

                if (!string.IsNullOrEmpty(_loadedStandardTheme) &&
                    Enum.TryParse(_loadedStandardTheme, out ElementTheme stdTheme))
                {
                    SetTheme(stdTheme);
                    CustomThemeManager.CustomThemeType fallbackType = stdTheme switch
                    {
                        ElementTheme.Light => CustomThemeManager.CustomThemeType.Light,
                        ElementTheme.Dark => CustomThemeManager.CustomThemeType.Dark,
                        _ => CustomThemeManager.CustomThemeType.Default
                    };
                    CustomThemeManager.ApplyCustomTheme(fallbackType);
                    return;
                }

                SetTheme(ElementTheme.Default);
                CustomThemeManager.ApplyCustomTheme(CustomThemeManager.CustomThemeType.Default);
            }
            catch
            {
                SetTheme(ElementTheme.Default);
                CustomThemeManager.ApplyCustomTheme(CustomThemeManager.CustomThemeType.Default);
            }
        }

        // ------------------------------------------------------------------------
        // Обработчики меню
        // ------------------------------------------------------------------------
        private void ButVisibleFirstToolBar_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var toggleItem = (ToggleMenuFlyoutItem)sender;
                bool newVisibility = !_menuFirstVisibilityToggler.IsCurrentlyVisible();
                _menuFirstVisibilityToggler.SetVisibility(newVisibility ? Visibility.Visible : Visibility.Collapsed);
                toggleItem.IsChecked = newVisibility;
            }
            catch
            {
            }
        }

        private void ButVisibleSecondToolBar_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var toggleItem = (ToggleMenuFlyoutItem)sender;
                bool newVisibility = !_menuSecondVisibilityToggler.IsCurrentlyVisible();
                _menuSecondVisibilityToggler.SetVisibility(newVisibility ? Visibility.Visible : Visibility.Collapsed);
                toggleItem.IsChecked = newVisibility;
            }
            catch
            {
            }
        }

        // ------------------------------------------------------------------------
        // Модальное окно настроек
        // ------------------------------------------------------------------------
        private void MenuFlyoutSettings_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var newWindow = new SettingWindow(this);
                newWindow.Activate();

                var hWnd = WindowNative.GetWindowHandle(newWindow);
                var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
                var appWindow = AppWindow.GetFromWindowId(windowId);
                var presenter = (OverlappedPresenter)appWindow.Presenter;

                var newWindowSize = new Windows.Graphics.SizeInt32(800, 600);
                appWindow.Resize(newWindowSize);
                presenter.IsResizable = false;
                presenter.IsMinimizable = false;
                presenter.IsMaximizable = false;

                var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
                var centerX = (displayArea.WorkArea.Width - newWindowSize.Width) / 2;
                var centerY = (displayArea.WorkArea.Height - newWindowSize.Height) / 2;
                appWindow.MoveAndResize(new Windows.Graphics.RectInt32(centerX, centerY, newWindowSize.Width, newWindowSize.Height));

                DisableParentWindow();
            }
            catch
            {
            }
        }

        private void DisableParentWindow()
        {
            try
            {
                var parentWindowHandle = WindowNative.GetWindowHandle(this);
                EnableWindow(parentWindowHandle, false);
            }
            catch
            {
            }
        }

        internal void EnableParentWindow()
        {
            try
            {
                var parentWindowHandle = WindowNative.GetWindowHandle(this);
                EnableWindow(parentWindowHandle, true);
            }
            catch
            {
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool EnableWindow(System.IntPtr hWnd, bool enable);

        // ------------------------------------------------------------------------
        // Закрытие окна и очистка ресурсов
        // ------------------------------------------------------------------------
        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            try
            {
                TitleBarGrid.Loaded -= TitleBarGrid_Loaded;
                TitleBarGrid.SizeChanged -= TitleBarGrid_SizeChanged;
                this.Closed -= MainWindow_Closed;

                _resizeDebounceTimer?.Stop();

                if (_mainVM != null && _mainVMPropertyChangedHandler != null)
                {
                    _mainVM.PropertyChanged -= _mainVMPropertyChangedHandler;
                }

                if (_backdropManager != null)
                {
                    _backdropManager.BackdropChanged -= OnBackdropChanged;
                    _backdropManager.BackdropChangeFailed -= OnBackdropChangeFailed;
                    _backdropManager.Dispose();
                    _backdropManager = null;
                }
            }
            catch
            {
            }
        }
    }
}