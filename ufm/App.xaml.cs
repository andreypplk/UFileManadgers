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

namespace ufm
{
    public partial class App : Application
    {
        // Список всех активных окон приложения (для навигации между окнами)
        public static List<Window> ActiveWindows { get; } = new List<Window>();

        private static Window startupWindow;
        private static Win32WindowHelper win32WindowHelper;
        private const int DefaultWindowWidth = 1280;
        private const int DefaultWindowHeight = 800;
        private static int windowWidth = -1;
        private static int windowHeight = -1;

        public static Window StartupWindow => startupWindow;
        public static MainWindow MainWindow { get; private set; }
        public static AppWindow AppWindow { get; private set; }
        public static SettingsManager SettingsManager { get; private set; }
        public static IconService IconService { get; private set; }

        private readonly List<DependencyObject> _elementsWithUid = new List<DependencyObject>();

        public App()
        {
            this.InitializeComponent();
            this.UnhandledException += OnUnhandledException;
            SettingsManager = new SettingsManager();
            SettingsManager.CleanInvalidSettings();
            InitializeLanguageManager();
            LoadWindowSize();
        }

        private void LoadWindowSize()
        {
            try
            {
                int loadedWidth = SettingsManager.GetSetting("WindowWidth", DefaultWindowWidth);
                int loadedHeight = SettingsManager.GetSetting("WindowHeight", DefaultWindowHeight);
                windowWidth = loadedWidth > 0 ? loadedWidth : DefaultWindowWidth;
                windowHeight = loadedHeight > 0 ? loadedHeight : DefaultWindowHeight;
                windowWidth = Math.Clamp(windowWidth, 800, 7680);
                windowHeight = Math.Clamp(windowHeight, 600, 4320);
            }
            catch
            {
                ResetWindowSizeToDefault();
            }
        }

        private void ResetWindowSizeToDefault()
        {
            windowWidth = DefaultWindowWidth;
            windowHeight = DefaultWindowHeight;
        }

        private void SaveWindowSize()
        {
            try
            {
                if (AppWindow != null)
                {
                    windowWidth = Math.Max(AppWindow.Size.Width, 100);
                    windowHeight = Math.Max(AppWindow.Size.Height, 100);
                }
                windowWidth = Math.Clamp(windowWidth, 800, 7680);
                windowHeight = Math.Clamp(windowHeight, 600, 4320);
                SettingsManager.SaveSetting("WindowWidth", windowWidth);
                SettingsManager.SaveSetting("WindowHeight", windowHeight);
            }
            catch
            {
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
            catch
            {
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
                            Uids.UpdateElementText(element, uid);
                    }
                    Uids.UpdateAllToolTips();
                    Uids.UpdateAllRadioButtonContents();
                }
                catch
                {
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
            e.Handled = true;
        }

        public static TEnum GetEnum<TEnum>(string text) where TEnum : struct
        {
            if (!typeof(TEnum).GetTypeInfo().IsEnum)
                throw new InvalidOperationException("Generic parameter 'TEnum' must be an enum.");
            return (TEnum)Enum.Parse(typeof(TEnum), text);
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            MainWindow = new MainWindow();
            startupWindow = MainWindow;

            // Регистрация главного окна в списке активных окон
            ActiveWindows.Add(MainWindow);
            MainWindow.Closed += (s, e) => ActiveWindows.Remove(MainWindow);

            MainWindow.Activate();
            MainWindow.ExtendsContentIntoTitleBar = true;

            var hWnd = WindowNative.GetWindowHandle(MainWindow);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            AppWindow = AppWindow.GetFromWindowId(windowId);

            if (AppWindow != null)
            {
                if (windowWidth <= 0 || windowHeight <= 0)
                {
                    windowWidth = DefaultWindowWidth;
                    windowHeight = DefaultWindowHeight;
                }
                AppWindow.Resize(new Windows.Graphics.SizeInt32(windowWidth, windowHeight));
            }

            win32WindowHelper = new Win32WindowHelper(startupWindow);
            win32WindowHelper.SetWindowMinMaxSize(
                minWindowSize: new Win32.POINT() { x = 600, y = 400 },
                maxWindowSize: new Win32.POINT() { x = 7680, y = 4320 }
            );
            MainWindow.Closed += MainWindow_Closed;
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            try
            {
                if (AppWindow != null)
                {
                    windowWidth = AppWindow.Size.Width;
                    windowHeight = AppWindow.Size.Height;
                    SaveWindowSize();
                }
            }
            catch
            {
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
    }
}