using Microsoft.UI.Xaml;
using System;
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

                bool saved = false;
                if (App.SettingsManager != null)
                {
                    try
                    {
                        App.SettingsManager.SaveSetting(SelectedAppThemeKey, value.ToString());
                        saved = true;
                    }
                    catch { }
                }

                if (!saved && NativeHelper.IsAppPackaged)
                {
                    ApplicationData.Current.LocalSettings.Values[SelectedAppThemeKey] = value.ToString();
                }

                if (CustomThemeManager.CurrentCustomTheme != CustomThemeManager.CustomThemeType.Default)
                {
                    CustomThemeManager.ApplyCustomTheme(CustomThemeManager.CurrentCustomTheme);
                }
            }
        }

        public static void Initialize()
        {
            string savedTheme = null;
            if (App.SettingsManager != null)
            {
                try
                {
                    savedTheme = App.SettingsManager.GetSetting<string>(SelectedAppThemeKey);
                }
                catch { }
            }

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
                catch { }
            }

            CustomThemeManager.Initialize();
        }

        public static bool IsDarkTheme(Window window = null)
        {
            if (window != null && window.Content is FrameworkElement rootElement)
            {
                if (rootElement.RequestedTheme != ElementTheme.Default)
                {
                    return rootElement.RequestedTheme == ElementTheme.Dark;
                }
            }

            foreach (Window w in WindowHelper.ActiveWindows)
            {
                if (w.Content is FrameworkElement element &&
                    element.RequestedTheme != ElementTheme.Default)
                {
                    return element.RequestedTheme == ElementTheme.Dark;
                }
            }

            return Application.Current.RequestedTheme == ApplicationTheme.Dark;
        }

        public static void ForceThemeUpdate()
        {
            try
            {
                var windows = WindowHelper.ActiveWindows;

                foreach (Window window in windows)
                {
                    if (window.Content is FrameworkElement rootElement)
                    {
                        var currentTheme = rootElement.RequestedTheme;
                        rootElement.RequestedTheme = ElementTheme.Default;
                        rootElement.RequestedTheme = currentTheme;
                    }
                }

                if (windows.Count == 0)
                {
                    var mainWindow = Application.Current.GetType().GetProperty("MainWindow")?.GetValue(Application.Current) as Window;
                    if (mainWindow?.Content is FrameworkElement mainRoot)
                    {
                        var currentTheme = mainRoot.RequestedTheme;
                        mainRoot.RequestedTheme = ElementTheme.Default;
                        mainRoot.RequestedTheme = currentTheme;
                    }
                }
            }
            catch
            {
            }
        }
    }
}