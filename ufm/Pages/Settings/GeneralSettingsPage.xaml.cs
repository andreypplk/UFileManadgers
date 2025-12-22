using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Diagnostics;
using System.Linq;
using Windows.Storage;
using Core_Language;
using static ufm.MainWindow;
using Windows.UI;

namespace ufm
{
    public sealed partial class GeneralSettingsPage : Page
    {
        private MainWindow _mainWindow;

        public LocalizationViewModel ViewModel { get; } = new LocalizationViewModel();

        public GeneralSettingsPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.WriteLine("GeneralSettingsPage loaded");

                // Добавляем динамические темы в комбобокс
                AddDynamicThemesToComboBox();

                // Инициализация языков
                InitializeLanguages();

                // Подписка на событие изменения языка
                LanguageComboBox.SelectionChanged += LanguageComboBox_SelectionChanged;

                // Загружаем состояние анимации
                bool isAnimationEnabled = LoadAnimationSetting();
                AnimationManager.Instance.IsAnimationEnabled = isAnimationEnabled;
                AnimationToggleSwitch.IsOn = isAnimationEnabled;

                // Загружаем состояние переключателя скрытых файлов
                ShowHiddenFilesAndFolderToggleSwitch.IsOn = App.SettingsManager.GetSetting<bool>("ShowHiddenFilesAndFolders", false);

                // Загружаем текущую тему
                LoadCurrentTheme();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OnLoaded error: {ex}");
            }
        }

        // Простой метод для добавления динамических тем
        private void AddDynamicThemesToComboBox()
        {
            try
            {
                // Добавляем динамические темы после встроенных
                foreach (var theme in CustomThemeManager.DynamicThemes)
                {
                    var item = new ComboBoxItem
                    {
                        Content = theme.Value,
                        Tag = theme.Key // Сохраняем имя темы в Tag
                    };
                    ThemeComboBox.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AddDynamicThemesToComboBox error: {ex}");
            }
        }

        private bool LoadAnimationSetting()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("IsAnimationEnabled"))
            {
                return (bool)localSettings.Values["IsAnimationEnabled"];
            }
            return true; // По умолчанию анимация включена
        }

        private void AnimationToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            bool isAnimationEnabled = AnimationToggleSwitch.IsOn;

            // Обновляем состояния анимации в AnimationManager
            AnimationManager.Instance.IsAnimationEnabled = isAnimationEnabled;

            // Сохраняем состояние анимации
            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["IsAnimationEnabled"] = isAnimationEnabled;
        }

        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            try
            {
                _mainWindow = e.Parameter as MainWindow ??
                              throw new ArgumentNullException(nameof(e.Parameter), "MainWindow parameter is required");

                // Загрузка текущих настроек
                LoadCurrentTheme();
                LoadCurrentBackdropType();

                base.OnNavigatedTo(e);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OnNavigatedTo error: {ex}");
                throw;
            }
        }

        private void InitializeLanguages()
        {
            try
            {
                // Устанавливаем выбранный язык
                LanguageComboBox.SelectedValue = ViewModel.SelectedLanguage;
                Debug.WriteLine($"Initialized language: {ViewModel.SelectedLanguage}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"InitializeLanguages error: {ex}");
            }
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (LanguageComboBox.SelectedValue is string selectedLanguage)
                {
                    Debug.WriteLine($"Language changed to: {selectedLanguage}");

                    // Устанавливаем новый язык
                    ViewModel.SelectedLanguage = selectedLanguage;

                    // Обновляем только комбобоксы
                    RefreshComboBoxText(ThemeComboBox);
                    RefreshComboBoxText(BackdropTypeComboBox);

                    // Принудительное обновление свойства выбранного языка
                    LanguageComboBox.SelectedValue = ViewModel.SelectedLanguage;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LanguageComboBox_SelectionChanged error: {ex}");
            }
        }

        private void RefreshComboBoxText(ComboBox comboBox)
        {
            try
            {
                // Запоминаем текущий выбранный индекс
                int selectedIndex = comboBox.SelectedIndex;

                // Создаем временную копию элементов
                var items = comboBox.Items.ToList();

                // Очищаем и снова добавляем элементы для принудительного обновления
                comboBox.Items.Clear();

                foreach (var item in items)
                {
                    if (item is ComboBoxItem comboBoxItem)
                    {
                        var uid = Uids.GetUid(comboBoxItem);
                        if (!string.IsNullOrEmpty(uid))
                        {
                            // Обновляем текст перед добавлением
                            Uids.UpdateElementText(comboBoxItem, uid);
                        }
                    }
                    comboBox.Items.Add(item);
                }

                // Восстанавливаем выбранный элемент
                comboBox.SelectedIndex = selectedIndex;

                // Принудительное обновление визуального состояния
                comboBox.UpdateLayout();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RefreshComboBoxText error: {ex}");
            }
        }

        private void LoadCurrentBackdropType()
        {
            try
            {
                if (_mainWindow == null || BackdropTypeComboBox.Items.Count == 0)
                    return;

                // Получаем текущий BackdropType
                BackdropType currentBackdrop = _mainWindow.CurrentBackdropType;

                // Проверяем, что значение есть в enum
                if (!Enum.IsDefined(typeof(BackdropType), currentBackdrop))
                {
                    Debug.WriteLine($"Unknown backdrop type: {currentBackdrop}");
                    return;
                }

                // Устанавливаем индекс, защищаясь от выхода за границы
                int index = (int)currentBackdrop;
                if (index >= 0 && index < BackdropTypeComboBox.Items.Count)
                {
                    BackdropTypeComboBox.SelectedIndex = index;
                    Debug.WriteLine($"Loaded backdrop: {currentBackdrop} (index: {index})");
                }
                else
                {
                    Debug.WriteLine($"Backdrop index out of range: {index}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadCurrentBackdropType error: {ex}");
            }
        }

        private void BackdropTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_mainWindow == null || BackdropTypeComboBox.SelectedIndex == -1)
                    return;

                // Получаем выбранный индекс
                int selectedIndex = BackdropTypeComboBox.SelectedIndex;

                // Проверяем, что индекс соответствует enum
                if (!Enum.IsDefined(typeof(BackdropType), selectedIndex))
                {
                    Debug.WriteLine($"Invalid backdrop index: {selectedIndex}");
                    return;
                }

                // Конвертируем индекс в BackdropType
                BackdropType backdropType = (BackdropType)selectedIndex;

                Debug.WriteLine($"Applying backdrop: {backdropType}");
                _mainWindow.SetBackdrop(backdropType);

                // Сохраняем настройку типа фона
                bool saved = false;
                if (App.SettingsManager != null)
                {
                    try
                    {
                        App.SettingsManager.SaveSetting("SelectedBackdropType", backdropType.ToString());
                        saved = true;
                        Debug.WriteLine($"Сохранено через SettingsManager: {backdropType}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Ошибка SettingsManager: {ex.Message}");
                    }
                }

                // Fallback на LocalSettings если SettingsManager не сработал
                if (!saved)
                {
                    try
                    {
                        var localSettings = ApplicationData.Current.LocalSettings.Values;
                        localSettings["SelectedBackdropType"] = backdropType.ToString();
                        Debug.WriteLine($"Сохранено через LocalSettings: {backdropType}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Ошибка LocalSettings: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BackdropTypeComboBox_SelectionChanged error: {ex}");
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            try
            {
                // Отписываемся от событий
                LanguageComboBox.SelectionChanged -= LanguageComboBox_SelectionChanged;
                base.OnNavigatingFrom(e);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OnNavigatingFrom error: {ex}");
            }
        }

        private void ShowHiddenFilesAndFolderToggleSwitch_OnToggled(object sender, RoutedEventArgs e)
        {
            bool isShowHiddenEnabled = ShowHiddenFilesAndFolderToggleSwitch.IsOn;
            App.SettingsManager.SaveSetting("ShowHiddenFilesAndFolders", isShowHiddenEnabled);
        }

        //private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    try
        //    {
        //        if (_mainWindow == null || ThemeComboBox.SelectedIndex < 0) return;

        //        int index = ThemeComboBox.SelectedIndex;
        //        bool isCurrentDark = ThemeHelper.IsDarkTheme(_mainWindow);

        //        // Динамические темы (индексы >= 13)
        //        if (index >= 13)
        //        {
        //            if (ThemeComboBox.Items[index] is ComboBoxItem item && item.Tag is string themeName)
        //            {
        //                HandleDynamicTheme(themeName, isCurrentDark);
        //            }
        //        }
        //        // Встроенные темы (индексы 0-12)
        //        else
        //        {
        //            // Особый случай для Default темы
        //            if (index == 2)
        //            {
        //                bool isSystemDark = Application.Current.RequestedTheme == ApplicationTheme.Dark;
        //                _mainWindow.SetTheme(isSystemDark ? ElementTheme.Light : ElementTheme.Dark);
        //                ThemeHelper.ForceThemeUpdate();
        //                CustomThemeManager.ApplyCustomTheme(CustomThemeManager.CustomThemeType.Default);
        //                _mainWindow.SetTheme(ElementTheme.Default);
        //            }
        //            else
        //            {
        //                // Все остальные темы обрабатываем одинаково
        //                for (int i = 0; i <= 12; i++)
        //                {
        //                    if (index == i)
        //                    {
        //                        // Определяем параметры темы
        //                        bool isDarkTheme = i == 1 || i >= 4 && i % 2 == 0; // Темные темы: 1, 4, 6, 8, 10, 12
        //                        var themeType = (CustomThemeManager.CustomThemeType)Enum.GetValues(typeof(CustomThemeManager.CustomThemeType)).GetValue(i);
        //                        var baseTheme = isDarkTheme ? ElementTheme.Dark : ElementTheme.Light;

        //                        // Принудительное переключение
        //                        if (isDarkTheme)
        //                        {
        //                            if (isCurrentDark)
        //                            {
        //                                _mainWindow.SetTheme(ElementTheme.Light);
        //                                ThemeHelper.ForceThemeUpdate();
        //                            }
        //                        }
        //                        else
        //                        {
        //                            if (!isCurrentDark)
        //                            {
        //                                _mainWindow.SetTheme(ElementTheme.Dark);
        //                                ThemeHelper.ForceThemeUpdate();
        //                            }
        //                        }

        //                        // Применяем тему
        //                        CustomThemeManager.ApplyCustomTheme(themeType);
        //                        _mainWindow.SetTheme(baseTheme);
        //                        break;
        //                    }
        //                }
        //            }
        //        }

        //        ThemeHelper.ForceThemeUpdate();
        //        SaveThemeSettings();
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"Theme change error: {ex}");
        //    }
        //}
        //private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    try
        //    {
        //        if (_mainWindow == null || ThemeComboBox.SelectedIndex < 0) return;

        //        int index = ThemeComboBox.SelectedIndex;
        //        bool isCurrentDark = ThemeHelper.IsDarkTheme(_mainWindow);

        //        // Динамические темы (индексы >= 13)
        //        if (index >= 13)
        //        {
        //            if (ThemeComboBox.Items[index] is ComboBoxItem item && item.Tag is string themeName)
        //            {
        //                HandleDynamicTheme(themeName, isCurrentDark);
        //            }
        //        }
        //        // Встроенные темы (индексы 0-12)
        //        else
        //        {
        //            // Особый случай для Default темы
        //            if (index == 2)
        //            {
        //                bool isSystemDark = Application.Current.RequestedTheme == ApplicationTheme.Dark;

        //                // Принудительный сброс для обновления ресурсов
        //                _mainWindow.SetTheme(isSystemDark ? ElementTheme.Light : ElementTheme.Dark);
        //                ThemeHelper.ForceThemeUpdate();

        //                // Применяем тему
        //                CustomThemeManager.ApplyCustomTheme(CustomThemeManager.CustomThemeType.Default);
        //                _mainWindow.SetTheme(ElementTheme.Default);
        //            }
        //            else
        //            {
        //                // Все остальные темы обрабатываем одинаково
        //                for (int i = 0; i <= 12; i++)
        //                {
        //                    if (index == i)
        //                    {
        //                        // Определяем параметры темы
        //                        bool isDarkTheme = i == 1 || i >= 4 && i % 2 == 0; // Темные темы: 1, 4, 6, 8, 10, 12
        //                        var themeType = (CustomThemeManager.CustomThemeType)Enum.GetValues(typeof(CustomThemeManager.CustomThemeType)).GetValue(i);
        //                        var baseTheme = isDarkTheme ? ElementTheme.Dark : ElementTheme.Light;

        //                        // ВСЕГДА делаем принудительный сброс для обновления ресурсов
        //                        // независимо от текущей темы
        //                        if (isDarkTheme)
        //                        {
        //                            // Временный переход на светлую тему
        //                            _mainWindow.SetTheme(ElementTheme.Light);
        //                        }
        //                        else
        //                        {
        //                            // Временный переход на темную тему  
        //                            _mainWindow.SetTheme(ElementTheme.Dark);
        //                        }
        //                        ThemeHelper.ForceThemeUpdate();

        //                        // Применяем финальную тему
        //                        CustomThemeManager.ApplyCustomTheme(themeType);
        //                        _mainWindow.SetTheme(baseTheme);
        //                        break;
        //                    }
        //                }
        //            }
        //        }

        //        ThemeHelper.ForceThemeUpdate();
        //        SaveThemeSettings();
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"Theme change error: {ex}");
        //    }
        //}


        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_mainWindow == null || ThemeComboBox.SelectedIndex < 0) return;

                int index = ThemeComboBox.SelectedIndex;
                bool isCurrentDark = ThemeHelper.IsDarkTheme(_mainWindow);

                // Динамические темы (индексы >= 13)
                if (index >= 13)
                {
                    if (ThemeComboBox.Items[index] is ComboBoxItem item && item.Tag is string themeName)
                    {
                        HandleDynamicTheme(themeName, isCurrentDark);
                    }
                }
                // Встроенные темы (индексы 0-12)
                else
                {
                    // Особый случай для Default темы
                    if (index == 2)
                    {
                        bool isSystemDark = Application.Current.RequestedTheme == ApplicationTheme.Dark;
                        _mainWindow.SetTheme(isSystemDark ? ElementTheme.Light : ElementTheme.Dark);
                        ThemeHelper.ForceThemeUpdate();
                        CustomThemeManager.ApplyCustomTheme(CustomThemeManager.CustomThemeType.Default);
                        _mainWindow.SetTheme(ElementTheme.Default);
                    }
                    //if (index == 2)
                    //{
                    //    bool isSystemDark = Application.Current.RequestedTheme == ApplicationTheme.Dark;
                    //    if (isSystemDark)
                    //    {
                    //        _mainWindow.SetTheme(ElementTheme.Light);
                    //        ThemeHelper.ForceThemeUpdate();
                    //    }
                    //    else
                    //    {
                    //        _mainWindow.SetTheme(ElementTheme.Dark);
                    //        ThemeHelper.ForceThemeUpdate();
                    //    }


                    //    CustomThemeManager.ApplyCustomTheme(CustomThemeManager.CustomThemeType.Default);
                    //    _mainWindow.SetTheme(ElementTheme.Default);

                    //    Debug.WriteLine($"Applied Default theme with double reset. System dark: {isSystemDark}");
                    //}
                    //if (index == 2)
                    //{
                    //    bool isSystemDark = Application.Current.RequestedTheme == ApplicationTheme.Dark;

                    //    // Двойной сброс для гарантии
                    //    _mainWindow.SetTheme(ElementTheme.Light);
                    //    ThemeHelper.ForceThemeUpdate();
                    //    _mainWindow.SetTheme(ElementTheme.Dark);
                    //    ThemeHelper.ForceThemeUpdate();

                    //    // Применяем кастомную тему
                    //    CustomThemeManager.ApplyCustomTheme(CustomThemeManager.CustomThemeType.Default);

                    //    // Устанавливаем Default и принудительно обновляем
                    //    _mainWindow.SetTheme(ElementTheme.Default);
                    //    ThemeHelper.ForceThemeUpdate();

                    //    // Принудительно обновляем корневой элемент
                    //    if (_mainWindow.Content is FrameworkElement root)
                    //    {
                    //        var currentTheme = root.RequestedTheme;
                    //        root.RequestedTheme = ElementTheme.Dark;
                    //        root.RequestedTheme = ElementTheme.Default;
                    //    }

                    //    Debug.WriteLine($"Applied Default theme. System dark: {isSystemDark}");
                    //}
                    else 
                    {
                        // Все остальные темы обрабатываем одинаково
                        for (int i = 0; i <= 12; i++)
                        {
                            if (index == i)
                            {
                                // Определяем параметры темы
                                bool isDarkTheme = i == 1 || i >= 4 && i % 2 == 0; // Темные темы: 1, 4, 6, 8, 10, 12
                                var themeType = (CustomThemeManager.CustomThemeType)Enum.GetValues(typeof(CustomThemeManager.CustomThemeType)).GetValue(i);
                                var baseTheme = isDarkTheme ? ElementTheme.Dark : ElementTheme.Light;

                                // Принудительное переключение (ИНВЕРТИРОВАННАЯ ЛОГИКА как в оригинале)
                                if (isDarkTheme)
                                {
                                    // Для темных тем: если сейчас темная, переключаем на светлую
                                    if (isCurrentDark)
                                    {
                                        _mainWindow.SetTheme(ElementTheme.Light);
                                        ThemeHelper.ForceThemeUpdate();
                                    }
                                }
                                else
                                {
                                    // Для светлых тем: если сейчас светлая, переключаем на темную
                                    if (!isCurrentDark)
                                    {
                                        _mainWindow.SetTheme(ElementTheme.Dark);
                                        ThemeHelper.ForceThemeUpdate();
                                    }
                                }

                                // Применяем тему
                                CustomThemeManager.ApplyCustomTheme(themeType);
                                _mainWindow.SetTheme(baseTheme);
                                break;
                            }
                        }
                    }
                }

                ThemeHelper.ForceThemeUpdate();
                SaveThemeSettings();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Theme change error: {ex}");
            }
        }
        // Простой метод для обработки динамических тем
        //private void HandleDynamicTheme(string themeName, bool isCurrentDark)
        //{
        //    try
        //    {
        //        Debug.WriteLine($"Handling dynamic theme: {themeName}");

        //        // Для динамических тем определяем базовую тему
        //        bool isDarkTheme = IsDynamicThemeDark(themeName);
        //        var baseTheme = isDarkTheme ? ElementTheme.Dark : ElementTheme.Light;

        //        Debug.WriteLine($"Dynamic theme is dark: {isDarkTheme}, base theme: {baseTheme}");

        //        // Принудительное переключение для обновления интерфейса
        //        if (isDarkTheme && isCurrentDark)
        //        {
        //            _mainWindow.SetTheme(ElementTheme.Light);
        //            ThemeHelper.ForceThemeUpdate();
        //        }
        //        else if (!isDarkTheme && !isCurrentDark)
        //        {
        //            _mainWindow.SetTheme(ElementTheme.Dark);
        //            ThemeHelper.ForceThemeUpdate();
        //        }

        //        // Применяем динамическую тему
        //        CustomThemeManager.ApplyDynamicTheme(themeName);
        //        _mainWindow.SetTheme(baseTheme);

        //        Debug.WriteLine($"Successfully applied dynamic theme: {themeName}");
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"Error in HandleDynamicTheme: {ex}");
        //    }
        //}
        //private void HandleDynamicTheme(string themeName, bool isCurrentDark)
        //{
        //    try
        //    {
        //        Debug.WriteLine($"Handling dynamic theme: {themeName}");

        //        // Для динамических тем определяем базовую тему
        //        bool isDarkTheme = IsDynamicThemeDark(themeName);
        //        var baseTheme = isDarkTheme ? ElementTheme.Dark : ElementTheme.Light;

        //        Debug.WriteLine($"Dynamic theme is dark: {isDarkTheme}, base theme: {baseTheme}");

        //        // ВСЕГДА делаем принудительный сброс для обновления ресурсов
        //        if (isDarkTheme)
        //        {
        //            // Временный переход на светлую тему
        //            _mainWindow.SetTheme(ElementTheme.Light);
        //        }
        //        else
        //        {
        //            // Временный переход на темную тему
        //            _mainWindow.SetTheme(ElementTheme.Dark);
        //        }
        //        ThemeHelper.ForceThemeUpdate();

        //        // Применяем динамическую тему
        //        CustomThemeManager.ApplyDynamicTheme(themeName);
        //        _mainWindow.SetTheme(baseTheme);

        //        Debug.WriteLine($"Successfully applied dynamic theme: {themeName}");
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"Error in HandleDynamicTheme: {ex}");
        //    }
        //}

        private void HandleDynamicTheme(string themeName, bool isCurrentDark)
        {
            try
            {
                Debug.WriteLine($"Handling dynamic theme: {themeName}");

                // Для динамических тем определяем базовую тему
                bool isDarkTheme = IsDynamicThemeDark(themeName);
                var baseTheme = isDarkTheme ? ElementTheme.Dark : ElementTheme.Light;

                Debug.WriteLine($"Dynamic theme is dark: {isDarkTheme}, base theme: {baseTheme}");

                // Принудительное переключение (ИНВЕРТИРОВАННАЯ ЛОГИКА как в оригинале)
                if (isDarkTheme)
                {
                    // Для темных тем: если сейчас темная, переключаем на светлую
                    if (isCurrentDark)
                    {
                        _mainWindow.SetTheme(ElementTheme.Light);
                        ThemeHelper.ForceThemeUpdate();
                    }
                }
                else
                {
                    // Для светлых тем: если сейчас светлая, переключаем на темную
                    if (!isCurrentDark)
                    {
                        _mainWindow.SetTheme(ElementTheme.Dark);
                        ThemeHelper.ForceThemeUpdate();
                    }
                }

                // Применяем динамическую тему
                CustomThemeManager.ApplyDynamicTheme(themeName);
                _mainWindow.SetTheme(baseTheme);

                Debug.WriteLine($"Successfully applied dynamic theme: {themeName}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in HandleDynamicTheme: {ex}");
            }
        }
        // Метод для определения, является ли динамическая тема темной
        private bool IsDynamicThemeDark(string themeName)
        {
            try
            {
                // Получаем ResourceDictionary темы
                if (Application.Current.Resources.ThemeDictionaries[themeName] is ResourceDictionary themeDict)
                {
                    // Проверяем наличие темных цветов в теме
                    if (themeDict.TryGetValue("AppBackgroundColor", out var bgColorObj) && bgColorObj is Color bgColor)
                    {
                        // Если цвет фона темный (низкая яркость), считаем тему темной
                        return IsColorDark(bgColor);
                    }

                    // Альтернативная проверка по SystemAccentColor или другим ключевым цветам
                    if (themeDict.TryGetValue("SystemAccentColor", out var accentColorObj) && accentColorObj is Color accentColor)
                    {
                        return IsColorDark(accentColor);
                    }
                }

                // Fallback: определяем по имени темы
                return themeName.Contains("Dark", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error determining if theme {themeName} is dark: {ex}");
                return themeName.Contains("Dark", StringComparison.OrdinalIgnoreCase);
            }
        }

        // Метод для определения, является ли цвет темным
        private bool IsColorDark(Color color)
        {
            // Формула для расчета яркости цвета
            double luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;
            return luminance < 0.5; // Если яркость меньше 0.5, считаем цвет темным
        }

        private void SaveThemeSettings()
        {
            try
            {
                int index = ThemeComboBox.SelectedIndex;

                // Динамические темы
                if (index >= 13)
                {
                    if (ThemeComboBox.Items[index] is ComboBoxItem item && item.Tag is string themeName)
                    {
                        SaveThemeSetting("SelectedCustomTheme", themeName);
                    }
                }
                // Встроенные темы
                else
                {
                    switch (index)
                    {
                        case 0: // Light
                            SaveThemeSetting("SelectedTheme", ElementTheme.Light.ToString());
                            break;
                        case 1: // Dark
                            SaveThemeSetting("SelectedTheme", ElementTheme.Dark.ToString());
                            break;
                        case 2: // Default
                            SaveThemeSetting("SelectedTheme", ElementTheme.Default.ToString());
                            break;
                        case 3: // Red
                            SaveThemeSetting("SelectedCustomTheme", CustomThemeManager.CustomThemeType.Red.ToString());
                            break;
                        case 4: // DarkRed
                            SaveThemeSetting("SelectedCustomTheme", CustomThemeManager.CustomThemeType.DarkRed.ToString());
                            break;
                        case 5: // Lemon
                            SaveThemeSetting("SelectedCustomTheme", CustomThemeManager.CustomThemeType.Lemon.ToString());
                            break;
                        case 6: // DarkLemon
                            SaveThemeSetting("SelectedCustomTheme", CustomThemeManager.CustomThemeType.DarkLemon.ToString());
                            break;
                        case 7: // Gold
                            SaveThemeSetting("SelectedCustomTheme", CustomThemeManager.CustomThemeType.Gold.ToString());
                            break;
                        case 8: // DarkGold
                            SaveThemeSetting("SelectedCustomTheme", CustomThemeManager.CustomThemeType.DarkGold.ToString());
                            break;
                        case 9: // Green
                            SaveThemeSetting("SelectedCustomTheme", CustomThemeManager.CustomThemeType.Green.ToString());
                            break;
                        case 10: // DarkGreen
                            SaveThemeSetting("SelectedCustomTheme", CustomThemeManager.CustomThemeType.DarkGreen.ToString());
                            break;
                        case 11: // Blue
                            SaveThemeSetting("SelectedCustomTheme", CustomThemeManager.CustomThemeType.Blue.ToString());
                            break;
                        case 12: // DarkBlue
                            SaveThemeSetting("SelectedCustomTheme", CustomThemeManager.CustomThemeType.DarkBlue.ToString());
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SaveThemeSettings error: {ex}");
            }
        }

        private void SaveThemeSetting(string settingName, string value)
        {
            bool saved = false;
            if (App.SettingsManager != null)
            {
                try
                {
                    App.SettingsManager.SaveSetting(settingName, value);
                    saved = true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error saving to SettingsManager: {ex}");
                }
            }

            if (!saved)
            {
                try
                {
                    ApplicationData.Current.LocalSettings.Values[settingName] = value;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error saving to LocalSettings: {ex}");
                }
            }
        }

        //private void LoadCurrentTheme()
        //{
        //    try
        //    {
        //        if (_mainWindow?.Content is not FrameworkElement rootElement) return;

        //        // Сначала проверяем пользовательские темы
        //        string customThemeStr = null;

        //        // Пробуем получить из SettingsManager
        //        if (App.SettingsManager != null)
        //        {
        //            try
        //            {
        //                customThemeStr = App.SettingsManager.GetSetting<string>("SelectedCustomTheme");
        //            }
        //            catch { /* Ignore if fails */ }
        //        }

        //        // Если не получили, пробуем LocalSettings
        //        if (string.IsNullOrEmpty(customThemeStr))
        //        {
        //            customThemeStr = ApplicationData.Current.LocalSettings.Values["SelectedCustomTheme"]?.ToString();
        //        }

        //        if (!string.IsNullOrEmpty(customThemeStr))
        //        {
        //            // Сначала проверяем встроенные темы
        //            if (Enum.TryParse(customThemeStr, out CustomThemeManager.CustomThemeType customTheme))
        //            {
        //                ThemeComboBox.SelectedIndex = customTheme switch
        //                {
        //                    CustomThemeManager.CustomThemeType.Light => 0,
        //                    CustomThemeManager.CustomThemeType.Dark => 1,
        //                    CustomThemeManager.CustomThemeType.Red => 3,
        //                    CustomThemeManager.CustomThemeType.DarkRed => 4,
        //                    CustomThemeManager.CustomThemeType.Lemon => 5,
        //                    CustomThemeManager.CustomThemeType.DarkLemon => 6,
        //                    CustomThemeManager.CustomThemeType.Gold => 7,
        //                    CustomThemeManager.CustomThemeType.DarkGold => 8,
        //                    CustomThemeManager.CustomThemeType.Green => 9,
        //                    CustomThemeManager.CustomThemeType.DarkGreen => 10,
        //                    CustomThemeManager.CustomThemeType.Blue => 11,
        //                    CustomThemeManager.CustomThemeType.DarkBlue => 12,
        //                    _ => 2 // Default
        //                };
        //                return;
        //            }
        //            // Затем проверяем динамические темы
        //            else if (CustomThemeManager.DynamicThemeExists(customThemeStr))
        //            {
        //                // Ищем динамическую тему в комбобоксе
        //                for (int i = 13; i < ThemeComboBox.Items.Count; i++)
        //                {
        //                    if (ThemeComboBox.Items[i] is ComboBoxItem existingItem && existingItem.Tag?.ToString() == customThemeStr)
        //                    {
        //                        ThemeComboBox.SelectedIndex = i;
        //                        return;
        //                    }
        //                }

        //                // Если не нашли в комбобоксе, но тема существует, добавляем её
        //                var displayName = CustomThemeManager.GetDynamicThemeDisplayName(customThemeStr);
        //                var newItem = new ComboBoxItem
        //                {
        //                    Content = displayName,
        //                    Tag = customThemeStr
        //                };
        //                ThemeComboBox.Items.Add(newItem);
        //                ThemeComboBox.SelectedIndex = ThemeComboBox.Items.Count - 1;
        //                return;
        //            }
        //        }

        //        // Если пользовательская тема не задана, загружаем стандартную
        //        string themeStr = null;

        //        // Пробуем получить из SettingsManager
        //        if (App.SettingsManager != null)
        //        {
        //            try
        //            {
        //                themeStr = App.SettingsManager.GetSetting<string>("SelectedTheme");
        //            }
        //            catch { /* Ignore if fails */ }
        //        }

        //        // Если не получили, пробуем LocalSettings
        //        if (string.IsNullOrEmpty(themeStr))
        //        {
        //            themeStr = ApplicationData.Current.LocalSettings.Values["SelectedTheme"]?.ToString();
        //        }

        //        var currentTheme = rootElement.RequestedTheme;
        //        if (!string.IsNullOrEmpty(themeStr) && Enum.TryParse(themeStr, out ElementTheme parsedTheme))
        //        {
        //            currentTheme = parsedTheme;
        //        }

        //        ThemeComboBox.SelectedIndex = currentTheme switch
        //        {
        //            ElementTheme.Light => 0,
        //            ElementTheme.Dark => 1,
        //            _ => 2 // Default
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"LoadCurrentTheme error: {ex}");
        //    }
        //}

        private void LoadCurrentTheme()
        {
            try
            {
                if (_mainWindow == null) return;

                // Сначала проверяем пользовательские темы
                string customThemeStr = null;

                // Пробуем получить из SettingsManager
                if (App.SettingsManager != null)
                {
                    try
                    {
                        customThemeStr = App.SettingsManager.GetSetting<string>("SelectedCustomTheme");
                    }
                    catch { /* Ignore if fails */ }
                }

                // Если не получили, пробуем LocalSettings
                if (string.IsNullOrEmpty(customThemeStr))
                {
                    customThemeStr = ApplicationData.Current.LocalSettings.Values["SelectedCustomTheme"]?.ToString();
                }

                if (!string.IsNullOrEmpty(customThemeStr))
                {
                    // Сначала проверяем встроенные темы
                    if (Enum.TryParse(customThemeStr, out CustomThemeManager.CustomThemeType customTheme))
                    {
                        ThemeComboBox.SelectedIndex = customTheme switch
                        {
                            CustomThemeManager.CustomThemeType.Light => 0,
                            CustomThemeManager.CustomThemeType.Dark => 1,
                            CustomThemeManager.CustomThemeType.Default => 2,
                            CustomThemeManager.CustomThemeType.Red => 3,
                            CustomThemeManager.CustomThemeType.DarkRed => 4,
                            CustomThemeManager.CustomThemeType.Lemon => 5,
                            CustomThemeManager.CustomThemeType.DarkLemon => 6,
                            CustomThemeManager.CustomThemeType.Gold => 7,
                            CustomThemeManager.CustomThemeType.DarkGold => 8,
                            CustomThemeManager.CustomThemeType.Green => 9,
                            CustomThemeManager.CustomThemeType.DarkGreen => 10,
                            CustomThemeManager.CustomThemeType.Blue => 11,
                            CustomThemeManager.CustomThemeType.DarkBlue => 12,
                            _ => 2 // Default fallback
                        };
                        Debug.WriteLine($"Loaded custom theme: {customTheme} (index: {ThemeComboBox.SelectedIndex})");
                        return;
                    }
                    // Затем проверяем динамические темы
                    else if (CustomThemeManager.DynamicThemeExists(customThemeStr))
                    {
                        // Ищем динамическую тему в комбобоксе
                        for (int i = 13; i < ThemeComboBox.Items.Count; i++)
                        {
                            if (ThemeComboBox.Items[i] is ComboBoxItem existingItem && existingItem.Tag?.ToString() == customThemeStr)
                            {
                                ThemeComboBox.SelectedIndex = i;
                                Debug.WriteLine($"Loaded dynamic theme: {customThemeStr} (index: {i})");
                                return;
                            }
                        }

                        // Если не нашли в комбобоксе, но тема существует, добавляем её
                        var displayName = CustomThemeManager.GetDynamicThemeDisplayName(customThemeStr);
                        var newItem = new ComboBoxItem
                        {
                            Content = displayName,
                            Tag = customThemeStr
                        };
                        ThemeComboBox.Items.Add(newItem);
                        ThemeComboBox.SelectedIndex = ThemeComboBox.Items.Count - 1;
                        Debug.WriteLine($"Added and loaded dynamic theme: {customThemeStr}");
                        return;
                    }
                }

                // Если пользовательская тема не задана, загружаем стандартную тему Windows
                string themeStr = null;

                // Пробуем получить из SettingsManager
                if (App.SettingsManager != null)
                {
                    try
                    {
                        themeStr = App.SettingsManager.GetSetting<string>("SelectedTheme");
                    }
                    catch { /* Ignore if fails */ }
                }

                // Если не получили, пробуем LocalSettings
                if (string.IsNullOrEmpty(themeStr))
                {
                    themeStr = ApplicationData.Current.LocalSettings.Values["SelectedTheme"]?.ToString();
                }

                // Определяем текущую тему через существующий хелпер
                bool isDarkTheme = ThemeHelper.IsDarkTheme(_mainWindow);

                // Если в настройках есть сохраненная тема, используем её
                if (!string.IsNullOrEmpty(themeStr) && Enum.TryParse(themeStr, out ElementTheme parsedTheme))
                {
                    ThemeComboBox.SelectedIndex = parsedTheme switch
                    {
                        ElementTheme.Light => 0,
                        ElementTheme.Dark => 1,
                        _ => 2 // Default
                    };
                }
                else
                {
                    // Если тема не сохранена, определяем по текущему состоянию
                    ThemeComboBox.SelectedIndex = isDarkTheme ? 1 : 0;
                }

                Debug.WriteLine($"Loaded theme - IsDark: {isDarkTheme}, SelectedIndex: {ThemeComboBox.SelectedIndex}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadCurrentTheme error: {ex}");
            }
        }
    }
}