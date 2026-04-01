using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Storage;
using WinRT;
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
        private BackdropType _currentBackdropType;
        public BackdropType CurrentBackdropType { get; private set; } = BackdropType.DefaultColor;

        private MicaController _micaController;
        private DesktopAcrylicController _acrylicController;
        private SystemBackdropConfiguration _configurationSource;
        private ICompositionSupportsSystemBackdrop _compositionTarget;

        // Кэширование поддержки
        private readonly bool _isMicaSupported;
        private readonly bool _isAcrylicSupported;

        // Для оптимизации SetConfigurationSourceTheme
        private SystemBackdropTheme _lastAppliedTheme;

        // События для уведомления UI
        public event Action<string> BackdropChanged;
        public event Action<string> BackdropChangeFailed;

        public BackdropManager(Window window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));

            // Инициализируем диспетчер очереди (только один раз)
            _wsdqHelper = new WindowsSystemDispatcherQueueHelper();
            _wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

            // Получаем интерфейс для системного фона один раз
            _compositionTarget = _window.As<ICompositionSupportsSystemBackdrop>();

            // Кэшируем поддержку системных эффектов
            _isMicaSupported = MicaController.IsSupported();
            _isAcrylicSupported = DesktopAcrylicController.IsSupported();

            // Создаём конфигурацию один раз
            _configurationSource = new SystemBackdropConfiguration();
            _configurationSource.IsInputActive = true;
            SetConfigurationSourceTheme();

            // Подписываемся на события окна
            _window.Activated += Window_Activated;
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
                // Если тип не изменился, ничего не делаем
                if (_currentBackdropType == type)
                    return;

                // Сохраняем выбранный тип поверхности в локальных настройках
                SaveBackdropSetting(type.ToString());

                // Очищаем старые контроллеры
                DisposeControllers();

                // Устанавливаем новый backdrop
                bool success = false;
                string backdropName = "None (default theme color)";

                // Обрабатываем типы, используя fallback без рекурсии
                switch (type)
                {
                    case BackdropType.Mica:
                        if (_isMicaSupported && TrySetMicaBackdrop(false))
                        {
                            backdropName = "Custom Mica";
                            success = true;
                        }
                        else
                        {
                            BackdropChangeFailed?.Invoke("Mica не поддерживается. Попробуем Acrylic.");
                            // Fallback без рекурсии: пытаемся установить AcrylicBase
                            if (_isAcrylicSupported && TrySetAcrylicBackdrop(false))
                            {
                                type = BackdropType.DesktopAcrylicBase;
                                backdropName = "Custom Acrylic (Base)";
                                success = true;
                            }
                            else
                            {
                                BackdropChangeFailed?.Invoke("Acrylic Base также не поддерживается. Переключаемся на цвет по умолчанию.");
                            }
                        }
                        break;

                    case BackdropType.MicaAlt:
                        if (_isMicaSupported && TrySetMicaBackdrop(true))
                        {
                            backdropName = "Custom MicaAlt";
                            success = true;
                        }
                        else
                        {
                            BackdropChangeFailed?.Invoke("MicaAlt не поддерживается. Попробуем Acrylic.");
                            if (_isAcrylicSupported && TrySetAcrylicBackdrop(false))
                            {
                                type = BackdropType.DesktopAcrylicBase;
                                backdropName = "Custom Acrylic (Base)";
                                success = true;
                            }
                            else
                            {
                                BackdropChangeFailed?.Invoke("Acrylic Base также не поддерживается. Переключаемся на цвет по умолчанию.");
                            }
                        }
                        break;

                    case BackdropType.DesktopAcrylicBase:
                        if (_isAcrylicSupported && TrySetAcrylicBackdrop(false))
                        {
                            backdropName = "Custom Acrylic (Base)";
                            success = true;
                        }
                        else
                        {
                            BackdropChangeFailed?.Invoke("Acrylic Base не поддерживается. Переключаемся на цвет по умолчанию.");
                        }
                        break;

                    case BackdropType.DesktopAcrylicThin:
                        if (_isAcrylicSupported && TrySetAcrylicBackdrop(true))
                        {
                            backdropName = "Custom Acrylic (Thin)";
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
                    _currentBackdropType = type;
                    CurrentBackdropType = type;
                    BackdropChanged?.Invoke(backdropName);
                }
                else
                {
                    // Если ничего не удалось, сбрасываем на цвет по умолчанию
                    DisposeControllers();
                    _currentBackdropType = BackdropType.DefaultColor;
                    CurrentBackdropType = BackdropType.DefaultColor;
                    BackdropChanged?.Invoke("None (default theme color)");
                }
            }
            catch (Exception ex)
            {
                BackdropChangeFailed?.Invoke($"Ошибка установки фона: {ex.Message}");
            }
        }

        private bool TrySetMicaBackdrop(bool useMicaAlt)
        {
            try
            {
                if (!_isMicaSupported)
                    return false;

                // Настраиваем конфигурацию (уже создана, просто обновляем)
                SetConfigurationSourceTheme();

                _micaController = new MicaController
                {
                    Kind = useMicaAlt ? MicaKind.BaseAlt : MicaKind.Base
                };

                _micaController.AddSystemBackdropTarget(_compositionTarget);
                _micaController.SetSystemBackdropConfiguration(_configurationSource);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool TrySetAcrylicBackdrop(bool useAcrylicThin)
        {
            try
            {
                if (!_isAcrylicSupported)
                    return false;

                SetConfigurationSourceTheme();

                _acrylicController = new DesktopAcrylicController
                {
                    Kind = useAcrylicThin ? DesktopAcrylicKind.Thin : DesktopAcrylicKind.Base
                };

                _acrylicController.AddSystemBackdropTarget(_compositionTarget);
                _acrylicController.SetSystemBackdropConfiguration(_configurationSource);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            // Обновляем состояние активности
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
                var newTheme = rootElement.ActualTheme switch
                {
                    ElementTheme.Dark => SystemBackdropTheme.Dark,
                    ElementTheme.Light => SystemBackdropTheme.Light,
                    _ => SystemBackdropTheme.Default
                };

                // Обновляем только если тема изменилась
                if (_lastAppliedTheme != newTheme)
                {
                    _configurationSource.Theme = newTheme;
                    _lastAppliedTheme = newTheme;
                }
            }
        }

        public void SetTheme(ElementTheme theme)
        {
            try
            {
                if (_window.Content is FrameworkElement rootElement)
                {
                    rootElement.RequestedTheme = theme;
                    SetConfigurationSourceTheme();
                }
            }
            catch
            {
                // Игнорируем ошибки установки темы
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

            _configurationSource = null;
        }

        private void DisposeControllers()
        {
            try
            {
                _micaController?.Dispose();
                _micaController = null;

                _acrylicController?.Dispose();
                _acrylicController = null;
            }
            catch
            {
                // Игнорируем ошибки при освобождении контроллеров
            }
        }

        private void SaveBackdropSetting(string value)
        {
            try
            {
                bool saved = false;

                if (App.SettingsManager != null)
                {
                    try
                    {
                        App.SettingsManager.SaveSetting("SelectedBackdropType", value);
                        saved = true;
                    }
                    catch
                    {
                        // Игнорируем ошибки сохранения в SettingsManager
                    }
                }

                if (!saved)
                {
                    ApplicationData.Current.LocalSettings.Values["SelectedBackdropType"] = value;
                }
            }
            catch
            {
                // Игнорируем ошибки сохранения
            }
        }

        public void LoadSavedBackdrop()
        {
            try
            {
                string savedBackdrop = null;

                if (App.SettingsManager != null)
                {
                    savedBackdrop = App.SettingsManager.GetSetting<string>("SelectedBackdropType");
                }

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
                    SetBackdrop(BackdropType.DefaultColor);
                }
            }
            catch
            {
                SetBackdrop(BackdropType.DefaultColor);
            }
        }
    }
}