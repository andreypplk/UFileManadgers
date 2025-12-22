using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Diagnostics;
using Windows.Foundation;
using Windows.Storage;
using Core_Language;
using WinRT; // required to support Window.As<ICompositionSupportsSystemBackdrop>()
using WinRT.Interop;
using WindowActivatedEventArgs = Microsoft.UI.Xaml.WindowActivatedEventArgs;
using ufm.Pages;
using System.Linq;
using Microsoft.UI.Xaml.Controls.Primitives;
using Core_FileManagement;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ufm
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>

    public sealed partial class MainWindow : Window
    {
        //Объявляет приватное поле m_AppWindow типа AppWindow, которое будет использоваться для представления окна приложения.
        public AppWindow m_AppWindow;

        //Объявляет приватное поле _presenter типа OverlappedPresenter, которое будет использоваться для представления наложенного представителя (презентера).
        private OverlappedPresenter _presenter;
        private IntPtr hWnd;

        //private RefreshUI _refreshUI;

        //Перебор типов тем встроенных в систему
        public enum BackdropType
        {
            DefaultColor,
            Mica,
            MicaAlt,
            DesktopAcrylicBase,
            DesktopAcrylicThin,
        }

        public new AppWindow AppWindow { get; private set; }
        private TabViewManager _tabViewManager;
        //private Window tabTearOutWindow = null;
        private Win32WindowHelper win32WindowHelper;
        private readonly LocalizationViewModel _locVM;
        private readonly MainViewModel _mainVM;

        //Визуал меню
        private readonly VisibilityToggler _menuFirstVisibilityToggler;
        private readonly VisibilityToggler _menuSecondVisibilityToggler;

        public MainWindow()
        {
            this.InitializeComponent();

            WindowHelper.TrackWindow(this); // Добавляем это

            _locVM = new LocalizationViewModel();
            _mainVM = new MainViewModel();
            GridGlobal.DataContext = _locVM; // Установка DataContext

            // Инициализация динамических тем ДО загрузки сохраненной темы
            CustomThemeManager.Initialize();

            // Создание и настройка статусбара
            var statusBar = new StatusBarPerformanceMetricsUC();
            statusBar.ViewModel = _mainVM.CurrentExplorerItem;

            // Добавление статусбара в интерфейс (предполагая, что есть ContentControl с именем StatusBarContainer)
            StatusBarContentRight.Content = statusBar;

            // Подписка на изменения CurrentExplorerItem
            _mainVM.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.CurrentExplorerItem))
                {
                    statusBar.ViewModel = _mainVM.CurrentExplorerItem;
                }
            };
            //Получает объект AppWindow для текущего окна.
            m_AppWindow = GetAppWindowForCurrentWindow();
            hWnd = WindowNative.GetWindowHandle(this);

            //Фиксируем размер окна
            m_AppWindow.Resize(new Windows.Graphics.SizeInt32(1440, 900));
            //Устанавливает корневую тему
            ((FrameworkElement)this.Content).RequestedTheme = ThemeHelper.RootTheme;

            //Объявляем помошника диспетчера очереди
            m_wsdqHelper = new WindowsSystemDispatcherQueueHelper();
            m_wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

            //Привязывает _presenter к представителю (презентеру) m_AppWindow.
            _presenter = m_AppWindow.Presenter as OverlappedPresenter;

            //Определяет, будет ли окно отображаться в переключателях задач (Alt+Tab).
            m_AppWindow.IsShownInSwitchers = true;

            //Устанавливает границы и строку заголовка окна.
            //Если оба значения поставить в True то появятся системные кнопки 
            _presenter.SetBorderAndTitleBar(true, true);

            //Создает переменную titleBar, представляющую строку заголовка окна.
            var titleBar = m_AppWindow.TitleBar;

            //Устанавливаем собственный значёк
            m_AppWindow.SetIcon("folder1.ico");

            TitleBarGrid.Loaded += TitleBarGrid_Loaded;
            TitleBarGrid.SizeChanged += TitleBarGrid_SizeChanged;
            //Данный код убирает иконку и меню из главного окна
            //m_AppWindow.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;

            //Если системный заголовок спрятан то цвета настраиваемого нужно изменять в XAML в Grid 
            //Пока отключить так как в своём TitleBar смена цвета пока не реализована
            //SetTitleBarColors();

            //Устанавливает пользовательскую строку заголовка, используя элемент TitleBarGrid.
            SetTitleBar(TitleBarGrid);
            LoadSavedBackdropType();
            LoadSavedTheme();
            _tabViewManager = new TabViewManager(TabsView, ContentFrame);

            //передаем кде искать меню
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
            //SetTitleBarColors();
        }

        private bool SetTitleBarColors()
        {

            if (AppWindowTitleBar.IsCustomizationSupported())
            {
                AppWindowTitleBar m_TitleBar = m_AppWindow.TitleBar;

                // Set active window colors.
                // Note: No effect when app is running on Windows 10
                // because color customization is not supported.
                m_TitleBar.ForegroundColor = Colors.White;
                m_TitleBar.BackgroundColor = Colors.Green;
                m_TitleBar.ButtonForegroundColor = Colors.White;
                m_TitleBar.ButtonBackgroundColor = Colors.SeaGreen;
                m_TitleBar.ButtonHoverForegroundColor = Colors.Gainsboro;
                m_TitleBar.ButtonHoverBackgroundColor = Colors.DarkSeaGreen;
                m_TitleBar.ButtonPressedForegroundColor = Colors.Gray;
                m_TitleBar.ButtonPressedBackgroundColor = Colors.LightGreen;

                // Set inactive window colors.
                // Note: No effect when app is running on Windows 10
                // because color customization is not supported.
                m_TitleBar.InactiveForegroundColor = Colors.Gainsboro;
                m_TitleBar.InactiveBackgroundColor = Colors.SeaGreen;
                m_TitleBar.ButtonInactiveForegroundColor = Colors.Gainsboro;
                m_TitleBar.ButtonInactiveBackgroundColor = Colors.SeaGreen;
                return true;
            }
            return false;
        }

        public void SetupWindowMinSize(Window window)
        {
            win32WindowHelper = new Win32WindowHelper(window);
            win32WindowHelper.SetWindowMinMaxSize(new Win32.POINT() { x = 800, y = 600 });
        }

        private AppWindow GetAppWindowForCurrentWindow()
        {
            //Поучаем дескриптор текущего окна
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            //Получаем идентификатор окна из дескриптора
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            //Возвращаем объект AppWindow для указанного идентификатора окна
            return AppWindow.GetFromWindowId(wndId);
        }

        private void TitleBarGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (ExtendsContentIntoTitleBar == true)
            {
                // Set the initial interactive regions.
                SetRegionsForCustomTitleBar();
            }
        }

        private void TitleBarGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ExtendsContentIntoTitleBar == true)
            {
                // Update interactive regions if the size of the window changes.
                SetRegionsForCustomTitleBar();
            }
        }

        private void SetRegionsForCustomTitleBar()
        {
            // Убедитесь, что TitleBarGrid инициализирован
            if (TitleBarGrid != null)
            {
                // Убедитесь, что XamlRoot инициализирован
                if (TitleBarGrid.XamlRoot != null)
                {
                    // Specify the interactive regions of the title bar.
                    double scaleAdjustment = TitleBarGrid.XamlRoot.RasterizationScale;

                    // Убедитесь, что m_AppWindow инициализирован
                    if (m_AppWindow != null)
                    {
                        RightPaddingColumn.Width = new GridLength(m_AppWindow.TitleBar.RightInset / scaleAdjustment);
                        LeftPaddingColumn.Width = new GridLength(m_AppWindow.TitleBar.LeftInset / scaleAdjustment);
                    }
                    else
                    {
                        Debug.WriteLine("m_AppWindow is null");
                    }

                    // Убедитесь, что TitleBarTable и StackPanelSettings инициализированы
                    if (TabsView != null && StackPanelSettings != null)
                    {
                        GeneralTransform transform = TabsView.TransformToVisual(null);
                        Rect bounds = transform.TransformBounds(new Rect(0, 0, TabsView.ActualWidth,
                            TabsView.ActualHeight));
                        Windows.Graphics.RectInt32 SearchBoxRect = GetRect(bounds, scaleAdjustment);

                        transform = StackPanelSettings.TransformToVisual(null);
                        bounds = transform.TransformBounds(new Rect(0, 0, StackPanelSettings.ActualWidth,
                            StackPanelSettings.ActualHeight));
                        Windows.Graphics.RectInt32 PersonPicRect = GetRect(bounds, scaleAdjustment);

                        var rectArray = new Windows.Graphics.RectInt32[] { SearchBoxRect, PersonPicRect };

                        InputNonClientPointerSource nonClientInputSrc = InputNonClientPointerSource.GetForWindowId(m_AppWindow.Id);
                        nonClientInputSrc.SetRegionRects(NonClientRegionKind.Passthrough, rectArray);
                    }
                    else
                    {
                        if (TabsView == null)
                        {
                            Debug.WriteLine("TitleBarTable is null");
                        }

                        if (StackPanelSettings == null)
                        {
                            Debug.WriteLine("StackPanelSettings is null");
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("TitleBarGrid.XamlRoot is null");
                }
            }
            else
            {
                Debug.WriteLine("TitleBarGrid is null");
            }
        }

        private Windows.Graphics.RectInt32 GetRect(Rect bounds, double scale)
        {
            return new Windows.Graphics.RectInt32(
                _X: (int)Math.Round(bounds.X * scale),
                _Y: (int)Math.Round(bounds.Y * scale),
                _Width: (int)Math.Round(bounds.Width * scale),
                _Height: (int)Math.Round(bounds.Height * scale)
            );
        }

        #region Themes Group

        //Группа отвечает за применение тем
        WindowsSystemDispatcherQueueHelper m_wsdqHelper;
        BackdropType m_currentBackdrop;
        public BackdropType CurrentBackdropType { get; private set; } = BackdropType.DefaultColor;

        Microsoft.UI.Composition.SystemBackdrops.MicaController m_micaController;
        Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController m_acrylicController;
        Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration m_configurationSource;

        public void SetBackdrop(BackdropType type)
        {
            // Сброс до цвета по умолчанию. Если запрашиваемый тип поддерживается, мы обновим его.
            // Примечание: Этот пример полностью удаляет любой предыдущий контроллер, чтобы сбросить его в состояние по умолчанию.
            // Это сделано для того, чтобы этот пример мог показать наиболее распространенный шаблон приложения, просто выбирающего один тип контроллера, который устанавливается при запуске.
            // Если приложение хочет переключаться между Mica и Acrylic, оно может просто вызвать RemoveSystemBackdropTarget() на старом контроллере, а затем настроить новый контроллер, повторно используя любой существующий m_configurationSource и обработчики событий Activated/Closed.
            m_currentBackdrop = BackdropType.DefaultColor;
            CurrentBackdropType = type;

            // Сохраняем выбранный тип поверхности в локальных настройках
            ApplicationData.Current.LocalSettings.Values["SelectedBackdropType"] = type.ToString();

            tbCurrentBackdrop.Text = "None (default theme color)";
            tbChangeStatus.Text = "";
            if (m_micaController != null)
            {
                m_micaController.Dispose();
                m_micaController = null;
            }

            if (m_acrylicController != null)
            {
                m_acrylicController.Dispose();
                m_acrylicController = null;
            }

            this.Activated -= Window_Activated;
            this.Closed -= Window_Closed;
            ((FrameworkElement)this.Content).ActualThemeChanged -= Window_ThemeChanged;
            m_configurationSource = null;

            if (type == BackdropType.Mica)
            {
                if (TrySetMicaBackdrop(false))
                {
                    tbCurrentBackdrop.Text = "Custom Mica";
                    m_currentBackdrop = type;
                }
                else
                {
                    // Mica не поддерживается. Попробуйте Acrylic.
                    type = BackdropType.DesktopAcrylicBase;
                    tbChangeStatus.Text += "  Mica не поддерживается. Попробуем Acrylic.";
                }
            }

            if (type == BackdropType.MicaAlt)
            {
                if (TrySetMicaBackdrop(true))
                {
                    tbCurrentBackdrop.Text = "Custom MicaAlt";
                    m_currentBackdrop = type;
                }
                else
                {
                    // MicaAlt не поддерживается. Попробуйте Acrylic.
                    type = BackdropType.DesktopAcrylicBase;
                    tbChangeStatus.Text += "  MicaAlt не поддерживается. Попробуем Acrylic.";
                }
            }

            if (type == BackdropType.DesktopAcrylicBase)
            {
                if (TrySetAcrylicBackdrop(false))
                {
                    tbCurrentBackdrop.Text = "Custom Acrylic (Base)";
                    m_currentBackdrop = type;
                }
                else
                {
                    // Acrylic не поддерживается, поэтому выберите следующий вариант, который является DefaultColor, который уже установлен.
                    tbChangeStatus.Text += "  Acrylic Base не поддерживается. Переключаемся на цвет по умолчанию.";
                }
            }

            if (type == BackdropType.DesktopAcrylicThin)
            {
                if (TrySetAcrylicBackdrop(true))
                {
                    tbCurrentBackdrop.Text = "Custom Acrylic (Thin)";
                    m_currentBackdrop = type;
                }
                else
                {
                    // Acrylic не поддерживается, поэтому выберите следующий вариант, который является DefaultColor, который уже установлен.
                    tbChangeStatus.Text += "  Acrylic Thin не поддерживается. Переключаемся на цвет по умолчанию.";
                }
            }

            // объявить визуальное изменение для автоматизации
            UIHelper.AnnounceActionForAccessibility(ButSetting, $"Background changed to {tbCurrentBackdrop.Text}",
                "BackgroundChangedNotificationActivityId");
        }

        bool TrySetMicaBackdrop(bool useMicaAlt)
        {
            if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
            {
                // Подключение объекта политики.
                m_configurationSource = new Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration();
                this.Activated += Window_Activated;
                this.Closed += Window_Closed;
                ((FrameworkElement)this.Content).ActualThemeChanged += Window_ThemeChanged;

                // Начальное состояние конфигурации.
                m_configurationSource.IsInputActive = true;
                SetConfigurationSourceTheme();

                m_micaController = new Microsoft.UI.Composition.SystemBackdrops.MicaController();

                m_micaController.Kind = useMicaAlt
                    ? Microsoft.UI.Composition.SystemBackdrops.MicaKind.BaseAlt
                    : Microsoft.UI.Composition.SystemBackdrops.MicaKind.Base;

                // Включить системный фон.
                // Примечание: Убедитесь, что у вас есть "using WinRT;", чтобы поддерживать вызов Window.As<...>().
                m_micaController.AddSystemBackdropTarget(this
                    .As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                m_micaController.SetSystemBackdropConfiguration(m_configurationSource);
                return true; // Успешно.
            }

            return false; // Mica не поддерживается на этой системе.
        }

        bool TrySetAcrylicBackdrop(bool useAcrylicThin)
        {
            if (Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.IsSupported())
            {
                // Подключение объекта политики.
                m_configurationSource = new Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration();
                this.Activated += Window_Activated;
                this.Closed += Window_Closed;
                ((FrameworkElement)this.Content).ActualThemeChanged += Window_ThemeChanged;

                // Начальное состояние конфигурации.
                m_configurationSource.IsInputActive = true;
                SetConfigurationSourceTheme();

                m_acrylicController = new Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController();

                m_acrylicController.Kind = useAcrylicThin
                    ? Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicKind.Thin
                    : Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicKind.Base;

                // Включить системный фон.
                // Примечание: Убедитесь, что у вас есть "using WinRT;", чтобы поддерживать вызов Window.As<...>().
                m_acrylicController.AddSystemBackdropTarget(
                    this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                m_acrylicController.SetSystemBackdropConfiguration(m_configurationSource);
                return true; // Успешно.
            }

            return false; // Acrylic не поддерживается на этой системе.
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            m_configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            // Убедитесь, что любой контроллер Mica/Acrylic удален, чтобы он не пытался использовать это закрытое окна.
            if (m_micaController != null)
            {
                m_micaController.Dispose();
                m_micaController = null;
            }

            if (m_acrylicController != null)
            {
                m_acrylicController.Dispose();
                m_acrylicController = null;
            }

            this.Activated -= Window_Activated;
            m_configurationSource = null;
        }

        private void Window_ThemeChanged(FrameworkElement sender, object args)
        {
            if (m_configurationSource != null)
            {
                SetConfigurationSourceTheme();
            }
        }

        private void SetConfigurationSourceTheme()
        {
            if (m_configurationSource == null)
            {
                // Инициализируем m_configurationSource, если он не был инициализирован
                m_configurationSource = new Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration();
            }

            if (this.Content is FrameworkElement rootElement)
            {
                switch (rootElement.ActualTheme)
                {
                    case ElementTheme.Dark:
                        m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Dark;
                        break;
                    case ElementTheme.Light:
                        m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Light;
                        break;
                    case ElementTheme.Default:
                        m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Default;
                        break;

                }
            }
            else
            {
                throw new InvalidOperationException("Root element is not a FrameworkElement.");
            }
        }

        public void SetTheme(ElementTheme theme)
        {
            if (this.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = theme;
                SetConfigurationSourceTheme();
            }
        }

        private void LoadSavedTheme()
        {
            try
            {
                // Сначала пробуем загрузить кастомную тему (включая динамические)
                string savedCustomTheme = null;

                // Пробуем получить из SettingsManager
                if (App.SettingsManager != null)
                {
                    savedCustomTheme = App.SettingsManager.GetSetting<string>("SelectedCustomTheme");
                }

                // Если не получили, пробуем LocalSettings
                if (string.IsNullOrEmpty(savedCustomTheme))
                {
                    var localSettings = ApplicationData.Current.LocalSettings.Values;
                    if (localSettings?.ContainsKey("SelectedCustomTheme") == true)
                    {
                        savedCustomTheme = localSettings["SelectedCustomTheme"]?.ToString();
                    }
                }

                if (!string.IsNullOrEmpty(savedCustomTheme))
                {
                    // Сначала проверяем встроенные темы
                    if (Enum.TryParse(savedCustomTheme, out CustomThemeManager.CustomThemeType customTheme))
                    {
                        // Применяем кастомную тему
                        CustomThemeManager.ApplyCustomTheme(customTheme);

                        // Устанавливаем соответствующую базовую тему
                        ElementTheme baseTheme = customTheme switch
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
                    // Затем проверяем динамические темы
                    else if (CustomThemeManager.DynamicThemeExists(savedCustomTheme))
                    {
                        // Применяем динамическую тему
                        CustomThemeManager.ApplyDynamicTheme(savedCustomTheme);

                        // Определяем базовую тему на основе имени
                        bool isDarkTheme = savedCustomTheme.Contains("Dark", StringComparison.OrdinalIgnoreCase);
                        var baseTheme = isDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
                        SetTheme(baseTheme);
                        return;
                    }
                }

                // Если кастомной темы нет, загружаем стандартную тему
                string savedTheme = null;

                // Пробуем получить из SettingsManager
                if (App.SettingsManager != null)
                {
                    savedTheme = App.SettingsManager.GetSetting<string>("SelectedTheme");
                }

                // Если не получили, пробуем LocalSettings
                if (string.IsNullOrEmpty(savedTheme))
                {
                    var localSettings = ApplicationData.Current.LocalSettings.Values;
                    if (localSettings?.ContainsKey("SelectedTheme") == true)
                    {
                        savedTheme = localSettings["SelectedTheme"]?.ToString();
                    }
                }

                if (!string.IsNullOrEmpty(savedTheme) && Enum.TryParse(savedTheme, out ElementTheme theme))
                {
                    SetTheme(theme);
                    // Для стандартных тем применяем соответствующую кастомную тему
                    CustomThemeManager.CustomThemeType customThemeType = theme switch
                    {
                        ElementTheme.Light => CustomThemeManager.CustomThemeType.Light,
                        ElementTheme.Dark => CustomThemeManager.CustomThemeType.Dark,
                        _ => CustomThemeManager.CustomThemeType.Default
                    };
                    CustomThemeManager.ApplyCustomTheme(customThemeType);
                }
                else
                {
                    // Устанавливаем тему по умолчанию
                    SetTheme(ElementTheme.Default);
                    CustomThemeManager.ApplyCustomTheme(CustomThemeManager.CustomThemeType.Default);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadSavedTheme error: {ex}");
                // Устанавливаем тему по умолчанию при ошибке
                SetTheme(ElementTheme.Default);
                CustomThemeManager.ApplyCustomTheme(CustomThemeManager.CustomThemeType.Default);
            }
        }

        private void LoadSavedBackdropType()
        {
            try
            {
                string savedBackdrop = null;

                // Пробуем получить из SettingsManager
                if (App.SettingsManager != null)
                {
                    savedBackdrop = App.SettingsManager.GetSetting<string>("SelectedBackdropType");
                }

                // Если не получили из SettingsManager, пробуем LocalSettings
                if (string.IsNullOrEmpty(savedBackdrop))
                {
                    var localSettings = ApplicationData.Current.LocalSettings.Values;
                    if (localSettings?.ContainsKey("SelectedBackdropType") == true)
                    {
                        savedBackdrop = localSettings["SelectedBackdropType"]?.ToString();
                    }
                }

                if (!string.IsNullOrEmpty(savedBackdrop) &&
                    Enum.TryParse<BackdropType>(savedBackdrop, out var backdropType))
                {
                    SetBackdrop(backdropType);
                }
                else
                {
                    // Устанавливаем тип поверхности по умолчанию
                    SetBackdrop(BackdropType.DefaultColor);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadSavedBackdropType error: {ex}");
                SetBackdrop(BackdropType.DefaultColor);
            }
        }
        #endregion

        #region Menu Group

        private void ButVisibleFirstToolBar_OnClick(object sender, RoutedEventArgs e)
        {
            var toggleItem = (ToggleMenuFlyoutItem)sender;

            // Инвертируем текущее состояние
            bool newVisibility = !_menuFirstVisibilityToggler.IsCurrentlyVisible();

            // Применяем изменения
            _menuFirstVisibilityToggler.SetVisibility(newVisibility ? Visibility.Visible : Visibility.Collapsed);
            toggleItem.IsChecked = newVisibility;
        }

        private void ButVisibleSecondToolBar_OnClick(object sender, RoutedEventArgs e)
        {
            var toggleItem = (ToggleMenuFlyoutItem)sender;

            bool newVisibility = !_menuSecondVisibilityToggler.IsCurrentlyVisible();
            _menuSecondVisibilityToggler.SetVisibility(newVisibility ? Visibility.Visible : Visibility.Collapsed);
            toggleItem.IsChecked = newVisibility;
        }

        #endregion

        #region Работа с модальным окном

        private void MenuFlyoutSettings_OnClick(object sender, RoutedEventArgs e)
        {
            var newWindow = new SettingWindow(this);
            newWindow.Activate();

            var hWnd = WindowNative.GetWindowHandle(newWindow);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            var presenter = (OverlappedPresenter)appWindow.Presenter;

            // Установите размер окна
            var newWindowSize = new Windows.Graphics.SizeInt32(800, 600);
            appWindow.Resize(newWindowSize);

            // Запретить изменение размера окна
            presenter.IsResizable = false;

            // Убрать системные кнопки свернуть и развернуть
            presenter.IsMinimizable = false;
            presenter.IsMaximizable = false;

            // Получить размеры экрана
            var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
            var centerX = (displayArea.WorkArea.Width - newWindowSize.Width) / 2;
            var centerY = (displayArea.WorkArea.Height - newWindowSize.Height) / 2;

            // Переместить и изменить размер окна, чтобы оно было по центру
            appWindow.MoveAndResize(new Windows.Graphics.RectInt32(centerX, centerY, newWindowSize.Width, newWindowSize.Height));

            // Сделать окно модальным
            DisableParentWindow();
        }

        private void DisableParentWindow()
        {
            var parentWindowHandle = WindowNative.GetWindowHandle(this);
            EnableWindow(parentWindowHandle, false);
        }

        internal void EnableParentWindow()
        {
            var parentWindowHandle = WindowNative.GetWindowHandle(this);
            EnableWindow(parentWindowHandle, true);
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool EnableWindow(System.IntPtr hWnd, bool enable);

        #endregion
    }
}