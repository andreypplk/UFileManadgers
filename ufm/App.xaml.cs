using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Reflection;
using Windows.Storage;
using ABI.Windows.ApplicationModel.Appointments.DataProvider;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.Windows.AppLifecycle;
using WinRT.Interop;
using Core_Language;
using SettingManager;
using Core_FileManagement;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ufm
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        // Переменная для хранения ссылки на стартовое окно
        private static Window startupWindow;

        // Переменная для хранения ссылки на помощника для работы с окнами Win32
        private static Win32WindowHelper win32WindowHelper;

        // Переменная для хранения идентификатора зарегистрированного хука на нажатие клавиш
        //private static int registeredKeyPressedHook = 0;
        // Дефолтные размеры окна
        private const int DefaultWindowWidth = 1280;
        private const int DefaultWindowHeight = 800;

        // Переменные для хранения размеров окна
        private static int windowWidth = -1;
        private static int windowHeight = -1;
        // Делегат для обработки событий хука клавиатуры
        //private HookProc keyEventHook;

        public static Window StartupWindow => startupWindow;
        public static MainWindow MainWindow { get; private set; }
        public static AppWindow AppWindow { get; private set; }

        // Статический экземпляр SettingsManager
        public static SettingsManager SettingsManager { get; private set; }

        //public static Window StartupWindow
        //{
        //    get
        //    {
        //        return startupWindow;
        //    }
        //}

        // Список для отслеживания элементов с Uid
        private readonly List<DependencyObject> _elementsWithUid = new List<DependencyObject>();


        public App()
        {
            this.InitializeComponent();
            this.UnhandledException += OnUnhandledException;
            // Инициализация SettingsManager
            SettingsManager = new SettingsManager();
            SettingsManager.CleanInvalidSettings(); // Очистка перед использованием
            InitializeLanguageManager();
            LoadWindowSize();
        }
        private void LoadWindowSize()
        {
            try
            {
                // Первичная загрузка
                int loadedWidth = SettingsManager.GetSetting("WindowWidth", DefaultWindowWidth);
                int loadedHeight = SettingsManager.GetSetting("WindowHeight", DefaultWindowHeight);

                // Вторичная проверка
                windowWidth = loadedWidth > 0 ? loadedWidth : DefaultWindowWidth;
                windowHeight = loadedHeight > 0 ? loadedHeight : DefaultWindowHeight;

                // Гарантируем минимальные разумные размеры
                windowWidth = Math.Clamp(windowWidth, 800, 7680);
                windowHeight = Math.Clamp(windowHeight, 600, 4320);

                Debug.WriteLine($"Valid window size loaded: {windowWidth}x{windowHeight}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Critical error loading window size: {ex.Message}");
                ResetWindowSizeToDefault();
            }
        }

        private void ResetWindowSizeToDefault()
        {
            windowWidth = DefaultWindowWidth;
            windowHeight = DefaultWindowHeight;
            Debug.WriteLine("Window size reset to defaults");
        }
        private void SaveWindowSize()
        {
            try
            {
                // Проверяем текущие размеры
                if (AppWindow != null)
                {
                    windowWidth = Math.Max(AppWindow.Size.Width, 100); // Минимум 100px
                    windowHeight = Math.Max(AppWindow.Size.Height, 100);
                }

                // Финализируем значения перед сохранением
                windowWidth = Math.Clamp(windowWidth, 800, 7680);
                windowHeight = Math.Clamp(windowHeight, 600, 4320);

                SettingsManager.SaveSetting("WindowWidth", windowWidth);
                SettingsManager.SaveSetting("WindowHeight", windowHeight);
                Debug.WriteLine($"Window size safely saved: {windowWidth}x{windowHeight}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving window size: {ex.Message}");
            }
        }

        private void InitializeLanguageManager()
        {
            try
            {
                LanguageManager.Instance.Initialize();
                LanguageManager.Instance.LanguageChanged += OnLanguageChanged;
                Uids.DependencyObjectUidSet += OnDependencyObjectUidSet;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LanguageManager init error: {ex}");
            }
        }

        private void OnLanguageChanged(object sender, LanguageChangedEventArgs e)
        {
         
            MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    foreach (var element in _elementsWithUid.ToArray())
                    {
                        var uid = Uids.GetUid(element);
                        if (!string.IsNullOrEmpty(uid))
                        {
                            Uids.UpdateElementText(element, uid);
                        }
                    }
                    Uids.UpdateAllToolTips();
                    Uids.UpdateAllRadioButtonContents();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"UI update error: {ex}");
                }
            });
        }

        private void OnDependencyObjectUidSet(object sender, DependencyObject d)
        {
            if (!_elementsWithUid.Contains(d))
            {
                _elementsWithUid.Add(d);
                Uids.UpdateElementText(d, Uids.GetUid(d));
            }
        }
        private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            // Логирование критической ошибки
            Debug.WriteLine($"Unhandled Exception: {e.Exception}");

            // Помечаем исключение как обработанное, чтобы приложение не завершалось
            e.Handled = true;
        }
        public static TEnum GetEnum<TEnum>(string text) where TEnum : struct
        {
            // Проверяет, является ли тип TEnum перечислением
            if (!typeof(TEnum).GetTypeInfo().IsEnum)
            {
                // Если TEnum не является перечислением, выбрасывает исключение
                throw new InvalidOperationException("Generic parameter 'TEnum' must be an enum.");
            }
            // Преобразует строку в значение перечисления типа TEnum и возвращает его
            return (TEnum)Enum.Parse(typeof(TEnum), text);
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            MainWindow = new MainWindow();
            startupWindow = MainWindow;

            MainWindow.Activate();
            MainWindow.ExtendsContentIntoTitleBar = true;

            // Инициализация AppWindow
            var hWnd = WindowNative.GetWindowHandle(MainWindow);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            AppWindow = AppWindow.GetFromWindowId(windowId);

            if (AppWindow != null)
            {
                if (windowWidth <= 0 || windowHeight <= 0)
                {
                    Debug.WriteLine("Invalid window size detected, resetting to defaults");
                    windowWidth = DefaultWindowWidth;
                    windowHeight = DefaultWindowHeight;
                }

                Debug.WriteLine($"Setting window size to: {windowWidth}x{windowHeight}");
                AppWindow.Resize(new Windows.Graphics.SizeInt32(windowWidth, windowHeight));
            }
            else
            {
                Debug.WriteLine("Failed to initialize AppWindow in App.");
            }

            //startupWindow = WindowHelper.CreateWindow();
            //startupWindow.Activate();
            //startupWindow.ExtendsContentIntoTitleBar = true;

            //m_window = new MainWindow();
            //m_window.Activate();
            //m_window.ExtendsContentIntoTitleBar = true;

            //Устанавливает минимальный размер окна
            win32WindowHelper = new Win32WindowHelper(startupWindow);
            win32WindowHelper.SetWindowMinMaxSize(
                minWindowSize: new Win32.POINT() { x = 600, y = 400 },    // Минимальный размер
                maxWindowSize: new Win32.POINT() { x = 7680, y = 4320 }     // Максимальный размер
            );
            // Подписываемся на событие закрытия окна для сохранения размеров
            MainWindow.Closed += MainWindow_Closed;

        }
        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            try
            {
                // Сохраняем текущие размеры окна перед закрытием
                if (AppWindow != null)
                {
                    windowWidth = AppWindow.Size.Width;
                    windowHeight = AppWindow.Size.Height;
                    SaveWindowSize();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving window size on close: {ex.Message}");
            }
        }

        #region Управление страницами приложения
        //Пока закомментирую из за сожности кода, как разберусь со NavigationView и Page сразу займусь адаптацией
        //
        //private async void EnsureWindow(IActivatedEventArgs args = null)
        //{
        //    // Независимо от нашего назначения, нам нужно загрузить данные управления - давайте сделаем это сейчас.
        //    // Нам больше никогда не придется делать это снова.
        //    //await ControlInfoDataSource.Instance.GetGroupsAsync();
        //    //await IconsDataSource.Instance.LoadIcons();

        //    Frame rootFrame = GetRootFrame();

        //    // Инициализация темы
        //    ThemeHelper.Initialize();

        //    Type targetPageType = typeof(HomePage);
        //    string targetPageArguments = string.Empty;

        //    if (args != null)
        //    {
        //        if (args.Kind == ActivationKind.Launch)
        //        {
        //            if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
        //            {
        //                try
        //                {
        //                    // Восстановление состояния приложения
        //                    await SuspensionManager.RestoreAsync();
        //                }
        //                catch (SuspensionManagerException)
        //                {
        //                    // Что-то пошло не так при восстановлении состояния.
        //                    // Предполагаем, что состояния нет, и продолжаем
        //                }
        //            }

        //            targetPageArguments = ((Windows.ApplicationModel.Activation.LaunchActivatedEventArgs)args).Arguments;
        //        }
        //    }
        //    var eventargs = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();
        //    if (eventargs != null && eventargs.Kind is ExtendedActivationKind.Protocol && eventargs.Data is ProtocolActivatedEventArgs)
        //    {
        //        ProtocolActivatedEventArgs ProtocolArgs = eventargs.Data as ProtocolActivatedEventArgs;
        //        var uri = ProtocolArgs.Uri.LocalPath.Replace("/", "");

        //        targetPageArguments = uri;
        //        string targetId = string.Empty;

        //        if (uri == "AllControls")
        //        {
        //            targetPageType = typeof(AllControlsPage);
        //        }
        //        else if (uri == "NewControls")
        //        {
        //            targetPageType = typeof(HomePage);
        //        }
        //        else if (ControlInfoDataSource.Instance.Groups.Any(g => g.UniqueId == uri))
        //        {
        //            targetPageType = typeof(SectionPage);
        //        }
        //        else if (ControlInfoDataSource.Instance.Groups.Any(g => g.Items.Any(i => i.UniqueId == uri)))
        //        {
        //            targetPageType = typeof(ItemPage);
        //        }
        //    }

        //    NavigationRootPage rootPage = StartupWindow.Content as NavigationRootPage;
        //    rootPage.Navigate(targetPageType, targetPageArguments);

        //    if (targetPageType == typeof(HomePage))
        //    {
        //        ((Microsoft.UI.Xaml.Controls.NavigationViewItem)((NavigationRootPage)App.StartupWindow.Content).NavigationView.MenuItems[0]).IsSelected = true;
        //    }

        //    // Убедитесь, что текущее окно активно
        //    StartupWindow.Activate();
        //}

        //// Получение корневого фрейма
        //public Frame GetRootFrame()
        //{
        //    Frame rootFrame;
        //    NavigationRootPage rootPage = StartupWindow.Content as NavigationRootPage;
        //    if (rootPage == null)
        //    {
        //        rootPage = new NavigationRootPage();
        //        rootFrame = (Frame)rootPage.FindName("rootFrame");
        //        if (rootFrame == null)
        //        {
        //            throw new Exception("Корневой фрейм не найден");
        //        }
        //        SuspensionManager.RegisterFrame(rootFrame, "AppFrame");
        //        rootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];
        //        rootFrame.NavigationFailed += OnNavigationFailed;

        //        StartupWindow.Content = rootPage;
        //    }
        //    else
        //    {
        //        rootFrame = (Frame)rootPage.FindName("rootFrame");
        //    }

        //    return rootFrame;
        //}

        ///// <summary>
        ///// Вызывается при неудачной навигации на определенную страницу
        ///// </summary>
        ///// <param name="sender">Фрейм, который не смог выполнить навигацию</param>
        ///// <param name="e">Детали о неудачной навигации</param>
        //void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        //{
        //    throw new Exception("Не удалось загрузить страницу " + e.SourcePageType.FullName);
        //}


        #endregion

        //private Window m_window;
    }
}
