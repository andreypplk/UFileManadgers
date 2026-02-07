using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using Windows.Storage;
using WinRT;
using Windows.UI;
using Microsoft.UI.Composition;

namespace ufm
{
    public class BackdropManager : IDisposable
    {
        public enum BackdropType
        {
            DefaultColor,
            Mica,
            MicaAlt,
            DesktopAcrylicBase,
            DesktopAcrylicThin,
        }

        private readonly Window _window;
        private readonly WindowsSystemDispatcherQueueHelper _wsdqHelper;
        private BackdropType _currentBackdrop;
        public BackdropType CurrentBackdropType { get; private set; } = BackdropType.DefaultColor;

        private MicaController _micaController;
        private DesktopAcrylicController _acrylicController;
        private SystemBackdropConfiguration _configurationSource;

        // События для уведомления UI
        public event Action<string> BackdropChanged;
        public event Action<string> BackdropChangeFailed;

        public BackdropManager(Window window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));

            // Инициализируем диспетчер очереди
            _wsdqHelper = new WindowsSystemDispatcherQueueHelper();
            _wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

            // Подписываемся на события окна
            _window.Closed += Window_Closed;

            if (_window.Content is FrameworkElement rootElement)
            {
                rootElement.ActualThemeChanged += Window_ThemeChanged;
            }
        }

        public void SetBackdrop(BackdropType type)
        {
            try
            {
                // Сохраняем выбранный тип поверхности в локальных настройках
                SaveBackdropSetting(type.ToString());

                // Сброс до цвета по умолчанию
                CurrentBackdropType = type;
                _currentBackdrop = BackdropType.DefaultColor;

                // Очищаем старые контроллеры
                DisposeControllers();

                // Отписываемся от старых событий (кроме тех, что в конструкторе)
                _window.Activated -= Window_Activated;
                _configurationSource = null;

                // Устанавливаем новый backdrop
                bool success = false;
                string backdropName = "None (default theme color)";

                switch (type)
                {
                    case BackdropType.Mica:
                        if (TrySetMicaBackdrop(false))
                        {
                            backdropName = "Custom Mica";
                            _currentBackdrop = type;
                            success = true;
                        }
                        else
                        {
                            BackdropChangeFailed?.Invoke("Mica не поддерживается. Попробуем Acrylic.");
                            SetBackdrop(BackdropType.DesktopAcrylicBase);
                            return;
                        }
                        break;

                    case BackdropType.MicaAlt:
                        if (TrySetMicaBackdrop(true))
                        {
                            backdropName = "Custom MicaAlt";
                            _currentBackdrop = type;
                            success = true;
                        }
                        else
                        {
                            BackdropChangeFailed?.Invoke("MicaAlt не поддерживается. Попробуем Acrylic.");
                            SetBackdrop(BackdropType.DesktopAcrylicBase);
                            return;
                        }
                        break;

                    case BackdropType.DesktopAcrylicBase:
                        if (TrySetAcrylicBackdrop(false))
                        {
                            backdropName = "Custom Acrylic (Base)";
                            _currentBackdrop = type;
                            success = true;
                        }
                        else
                        {
                            BackdropChangeFailed?.Invoke("Acrylic Base не поддерживается. Переключаемся на цвет по умолчанию.");
                        }
                        break;

                    case BackdropType.DesktopAcrylicThin:
                        if (TrySetAcrylicBackdrop(true))
                        {
                            backdropName = "Custom Acrylic (Thin)";
                            _currentBackdrop = type;
                            success = true;
                        }
                        else
                        {
                            BackdropChangeFailed?.Invoke("Acrylic Thin не поддерживается. Переключаемся на цвет по умолчанию.");
                        }
                        break;
                }

                if (success)
                {
                    BackdropChanged?.Invoke(backdropName);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SetBackdrop error: {ex}");
                BackdropChangeFailed?.Invoke($"Ошибка установки фона: {ex.Message}");
            }
        }

        private bool TrySetMicaBackdrop(bool useMicaAlt)
        {
            try
            {
                if (!MicaController.IsSupported())
                {
                    Debug.WriteLine("MicaController is not supported on this system");
                    return false;
                }

                _configurationSource = new SystemBackdropConfiguration();
                _window.Activated += Window_Activated;

                // Начальное состояние конфигурации
                _configurationSource.IsInputActive = true;
                SetConfigurationSourceTheme();

                _micaController = new MicaController
                {
                    Kind = useMicaAlt ? MicaKind.BaseAlt : MicaKind.Base
                };

                // Включение системного фона
                _micaController.AddSystemBackdropTarget(
                    _window.As<ICompositionSupportsSystemBackdrop>());
                _micaController.SetSystemBackdropConfiguration(_configurationSource);

                Debug.WriteLine($"Mica backdrop set successfully (Alt: {useMicaAlt})");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TrySetMicaBackdrop error: {ex}");
                return false;
            }
        }

        private bool TrySetAcrylicBackdrop(bool useAcrylicThin)
        {
            try
            {
                if (!DesktopAcrylicController.IsSupported())
                {
                    Debug.WriteLine("DesktopAcrylicController is not supported on this system");
                    return false;
                }

                _configurationSource = new SystemBackdropConfiguration();
                _window.Activated += Window_Activated;

                // Начальное состояние конфигурации
                _configurationSource.IsInputActive = true;
                SetConfigurationSourceTheme();

                _acrylicController = new DesktopAcrylicController
                {
                    Kind = useAcrylicThin ? DesktopAcrylicKind.Thin : DesktopAcrylicKind.Base
                };

                // Включение системного фона
                _acrylicController.AddSystemBackdropTarget(
                    _window.As<ICompositionSupportsSystemBackdrop>());
                _acrylicController.SetSystemBackdropConfiguration(_configurationSource);

                Debug.WriteLine($"Acrylic backdrop set successfully (Thin: {useAcrylicThin})");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TrySetAcrylicBackdrop error: {ex}");
                return false;
            }
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (_configurationSource != null)
            {
                _configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
            }
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            Dispose();
        }

        private void Window_ThemeChanged(FrameworkElement sender, object args)
        {
            SetConfigurationSourceTheme();
        }

        private void SetConfigurationSourceTheme()
        {
            if (_configurationSource == null) return;

            if (_window.Content is FrameworkElement rootElement)
            {
                _configurationSource.Theme = rootElement.ActualTheme switch
                {
                    ElementTheme.Dark => SystemBackdropTheme.Dark,
                    ElementTheme.Light => SystemBackdropTheme.Light,
                    _ => SystemBackdropTheme.Default
                };

                Debug.WriteLine($"Configuration theme updated to: {_configurationSource.Theme}");
            }
        }

        public void SetTheme(ElementTheme theme)
        {
            if (_window.Content is FrameworkElement rootElement)
            {
                try
                {
                    rootElement.RequestedTheme = theme;
                    SetConfigurationSourceTheme();
                    Debug.WriteLine($"Window theme set to: {theme}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"SetTheme error: {ex}");
                }
            }
        }

        public void Dispose()
        {
            DisposeControllers();

            if (_window != null)
            {
                _window.Activated -= Window_Activated;
                _window.Closed -= Window_Closed;

                if (_window.Content is FrameworkElement rootElement)
                {
                    rootElement.ActualThemeChanged -= Window_ThemeChanged;
                }
            }
        }

        private void DisposeControllers()
        {
            try
            {
                _micaController?.Dispose();
                _micaController = null;

                _acrylicController?.Dispose();
                _acrylicController = null;

                Debug.WriteLine("Backdrop controllers disposed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DisposeControllers error: {ex}");
            }
        }

        private void SaveBackdropSetting(string value)
        {
            try
            {
                bool saved = false;

                // Пробуем сохранить через SettingsManager
                if (App.SettingsManager != null)
                {
                    try
                    {
                        App.SettingsManager.SaveSetting("SelectedBackdropType", value);
                        saved = true;
                        Debug.WriteLine($"Backdrop saved to SettingsManager: {value}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error saving to SettingsManager: {ex}");
                    }
                }

                // Fallback на LocalSettings
                if (!saved)
                {
                    ApplicationData.Current.LocalSettings.Values["SelectedBackdropType"] = value;
                    Debug.WriteLine($"Backdrop saved to LocalSettings: {value}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SaveBackdropSetting error: {ex}");
            }
        }

        public void LoadSavedBackdrop()
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
                    Debug.WriteLine($"Loaded saved backdrop: {backdropType}");
                }
                else
                {
                    SetBackdrop(BackdropType.DefaultColor);
                    Debug.WriteLine("Using default backdrop");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadSavedBackdrop error: {ex}");
                SetBackdrop(BackdropType.DefaultColor);
            }
        }
    }
}