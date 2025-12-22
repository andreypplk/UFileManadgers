using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace ufm
{
    public static class ThemeHelper
    {
        private const string SelectedAppThemeKey = "SelectedAppTheme";

        public static ElementTheme ActualTheme
        {
            get
            {
                foreach (Window window in WindowHelper.ActiveWindows)
                {
                    if (window.Content is FrameworkElement rootElement)
                    {
                        if (rootElement.RequestedTheme != ElementTheme.Default)
                        {
                            return rootElement.RequestedTheme;
                        }
                    }
                }
                return ufm.App.GetEnum<ElementTheme>(App.Current.RequestedTheme.ToString());
            }
        }

        public static ElementTheme RootTheme
        {
            get
            {
                foreach (Window window in WindowHelper.ActiveWindows)
                {
                    if (window.Content is FrameworkElement rootElement)
                    {
                        return rootElement.RequestedTheme;
                    }
                }
                return ElementTheme.Default;
            }
            set
            {
                foreach (Window window in WindowHelper.ActiveWindows)
                {
                    if (window.Content is FrameworkElement rootElement)
                    {
                        rootElement.RequestedTheme = value;
                    }
                }

                // Сохраняем в SettingsManager
                bool saved = false;
                if (App.SettingsManager != null)
                {
                    try
                    {
                        App.SettingsManager.SaveSetting(SelectedAppThemeKey, value.ToString());
                        saved = true;
                    }
                    catch { /* Ignore if fails */ }
                }

                // Fallback на LocalSettings
                if (!saved && NativeHelper.IsAppPackaged)
                {
                    ApplicationData.Current.LocalSettings.Values[SelectedAppThemeKey] = value.ToString();
                }

                // При изменении темы обновляем пользовательские темы
                if (CustomThemeManager.CurrentCustomTheme != CustomThemeManager.CustomThemeType.Default)
                {
                    CustomThemeManager.ApplyCustomTheme(CustomThemeManager.CurrentCustomTheme);
                }
            }
        }

        public static void Initialize()
        {
            // Пробуем получить из SettingsManager
            string savedTheme = null;
            if (App.SettingsManager != null)
            {
                try
                {
                    savedTheme = App.SettingsManager.GetSetting<string>(SelectedAppThemeKey);
                }
                catch { /* Ignore if fails */ }
            }

            // Если не получили, пробуем LocalSettings
            if (string.IsNullOrEmpty(savedTheme) && NativeHelper.IsAppPackaged)
            {
                savedTheme = ApplicationData.Current.LocalSettings.Values[SelectedAppThemeKey]?.ToString();
            }

            if (!string.IsNullOrEmpty(savedTheme))
            {
                try
                {
                    if (Enum.TryParse(savedTheme, out ElementTheme theme))
                    {
                        RootTheme = theme;
                        return;
                    }
                }
                catch { /* Ignore parse errors */ }
            }

            // Инициализируем пользовательские темы
            CustomThemeManager.Initialize();
        }

        public static bool IsDarkTheme(Window window = null)
        {
            // Если указано конкретное окно - проверяем его тему
            if (window != null && window.Content is FrameworkElement rootElement)
            {
                if (rootElement.RequestedTheme != ElementTheme.Default)
                {
                    return rootElement.RequestedTheme == ElementTheme.Dark;
                }
            }

            // Проверяем все активные окна
            foreach (Window w in WindowHelper.ActiveWindows)
            {
                if (w.Content is FrameworkElement element &&
                    element.RequestedTheme != ElementTheme.Default)
                {
                    return element.RequestedTheme == ElementTheme.Dark;
                }
            }

            // Если ни в одном окне тема не задана явно - используем системную
            return Application.Current.RequestedTheme == ApplicationTheme.Dark;
        }
        //public static void ForceThemeUpdate()
        //{
        //    foreach (Window window in WindowHelper.ActiveWindows)
        //    {
        //        if (window.Content is FrameworkElement rootElement)
        //        {
        //            var currentTheme = rootElement.RequestedTheme;
        //            rootElement.RequestedTheme = ElementTheme.Default;
        //            rootElement.RequestedTheme = currentTheme;
        //        }
        //    }
        //}
        public static void ForceThemeUpdate()
        {
            try
            {
                Debug.WriteLine("ForceThemeUpdate called");

                // Способ 1: Через WindowHelper.ActiveWindows
                var windows = WindowHelper.ActiveWindows;
                Debug.WriteLine($"Found {windows.Count} windows via ActiveWindows");

                foreach (Window window in windows)
                {
                    if (window.Content is FrameworkElement rootElement)
                    {
                        var currentTheme = rootElement.RequestedTheme;
                        rootElement.RequestedTheme = ElementTheme.Default;
                        rootElement.RequestedTheme = currentTheme;
                        Debug.WriteLine($"Refreshed window theme: {currentTheme}");
                    }
                }

                // Способ 2: Резервный - через Application.Current.MainWindow
                if (windows.Count == 0)
                {
                    Debug.WriteLine("No windows in ActiveWindows, trying MainWindow property");
                    var mainWindow = Application.Current.GetType().GetProperty("MainWindow")?.GetValue(Application.Current) as Window;
                    if (mainWindow?.Content is FrameworkElement mainRoot)
                    {
                        var currentTheme = mainRoot.RequestedTheme;
                        mainRoot.RequestedTheme = ElementTheme.Default;
                        mainRoot.RequestedTheme = currentTheme;
                        Debug.WriteLine($"Refreshed main window theme: {currentTheme}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ForceThemeUpdate: {ex}");
            }
        }
    }
}
