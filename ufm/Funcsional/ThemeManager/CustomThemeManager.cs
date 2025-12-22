using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.UI;
using System.Text.RegularExpressions;

namespace ufm
{
    public static class CustomThemeManager
    {
        private const string SelectedCustomThemeKey = "SelectedCustomTheme";
        private static readonly Dictionary<string, string> _dynamicThemes = new Dictionary<string, string>();
        private static readonly Dictionary<string, ResourceDictionary> _loadedThemeDictionaries = new Dictionary<string, ResourceDictionary>();

        public enum CustomThemeType
        {
            Light,
            Dark,
            Default,
            Red,
            DarkRed,
            Lemon,
            DarkLemon,
            Gold,
            DarkGold,
            Green,
            DarkGreen,
            Blue,
            DarkBlue
        }

        public static CustomThemeType CurrentCustomTheme { get; private set; } = CustomThemeType.Default;
        public static IReadOnlyDictionary<string, string> DynamicThemes => _dynamicThemes;

        public static void Initialize()
        {
            // Загружаем динамические темы
            LoadDynamicThemesAsync().ConfigureAwait(false);

            // Пробуем получить из SettingsManager
            if (App.SettingsManager?.GetSetting<string>(SelectedCustomThemeKey) is string savedTheme)
            {
                ApplySavedTheme(savedTheme);
            }
            // Fallback на LocalSettings
            else if (ApplicationData.Current.LocalSettings.Values.TryGetValue(SelectedCustomThemeKey, out var localTheme))
            {
                ApplySavedTheme(localTheme.ToString());
            }
        }

        private static void ApplySavedTheme(string themeStr)
        {
            if (string.IsNullOrEmpty(themeStr)) return;

            // Сначала проверяем встроенные темы
            if (Enum.TryParse(themeStr, out CustomThemeType theme))
            {
                CurrentCustomTheme = theme;
                ApplyCustomTheme(theme);
            }
            // Затем проверяем динамические темы
            else if (_dynamicThemes.ContainsKey(themeStr))
            {
                ApplyDynamicTheme(themeStr);
            }
        }

        private static async Task LoadDynamicThemesAsync()
        {
            try
            {
                _dynamicThemes.Clear();
                _loadedThemeDictionaries.Clear();

                var themeFiles = await GetThemeFilesAsync();

                foreach (var filePath in themeFiles)
                {
                    try
                    {
                        string fileName = Path.GetFileName(filePath);
                        if (fileName.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase) && !IsBuiltInTheme(fileName))
                        {
                            string themeName = Path.GetFileNameWithoutExtension(filePath);
                            string displayName = await GetThemeDisplayNameAsync(filePath);
                            _dynamicThemes[themeName] = displayName;

                            // Загружаем ResourceDictionary
                            await LoadThemeDictionaryAsync(themeName, filePath);

                            Debug.WriteLine($"Found dynamic theme: {themeName} -> {displayName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error processing theme file {filePath}: {ex}");
                    }
                }

                Debug.WriteLine($"Loaded {_dynamicThemes.Count} dynamic themes");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading dynamic themes: {ex}");
            }
        }

        private static async Task LoadThemeDictionaryAsync(string themeName, string filePath)
        {
            try
            {
                // Читаем XAML содержимое
                var xamlContent = await File.ReadAllTextAsync(filePath);

                // Используем улучшенный парсинг
                var themeDictionary = ParseXamlToResourceDictionary(xamlContent);

                if (themeDictionary != null)
                {
                    _loadedThemeDictionaries[themeName] = themeDictionary;

                    // Добавляем в ThemeDictionaries приложения
                    Application.Current.Resources.ThemeDictionaries[themeName] = themeDictionary;

                    Debug.WriteLine($"Successfully loaded theme dictionary for: {themeName} with {themeDictionary.Count} resources");
                }
                else
                {
                    Debug.WriteLine($"Failed to load theme dictionary for: {themeName}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading theme dictionary {themeName}: {ex}");
            }
        }

        private static ResourceDictionary ParseXamlToResourceDictionary(string xamlContent)
        {
            try
            {
                var dictionary = new ResourceDictionary();

                // Удаляем комментарии из XAML
                xamlContent = RemoveComments(xamlContent);

                // Парсим все элементы, а не только цвета и строки
                ParseAllResourcesFromXaml(dictionary, xamlContent);

                Debug.WriteLine($"Parsed {dictionary.Count} resources from XAML");
                return dictionary;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error parsing XAML: {ex}");
                return null;
            }
        }

        private static string RemoveComments(string xamlContent)
        {
            // Удаляем XML комментарии <!-- -->
            return Regex.Replace(xamlContent, @"<!--.*?-->", "", RegexOptions.Singleline);
        }

        private static void ParseAllResourcesFromXaml(ResourceDictionary dictionary, string xamlContent)
        {
            try
            {
                // Упрощенный парсинг - ищем все элементы между тегами ResourceDictionary
                int startIndex = xamlContent.IndexOf("<ResourceDictionary");
                if (startIndex == -1) return;

                startIndex = xamlContent.IndexOf(">", startIndex) + 1;
                int endIndex = xamlContent.LastIndexOf("</ResourceDictionary>");

                if (startIndex < 0 || endIndex < 0 || startIndex >= endIndex) return;

                string content = xamlContent.Substring(startIndex, endIndex - startIndex);

                // Парсим цвета
                ParseColorsFromXaml(dictionary, content);

                // Парсим SolidColorBrush
                ParseBrushesFromXaml(dictionary, content);

                // Парсим строки
                ParseStringsFromXaml(dictionary, content);

                // Парсим другие типы ресурсов
                ParseOtherResourcesFromXaml(dictionary, content);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ParseAllResourcesFromXaml: {ex}");
            }
        }

        private static void ParseBrushesFromXaml(ResourceDictionary dictionary, string xamlContent)
        {
            try
            {
                // Паттерн для SolidColorBrush с атрибутом Color
                var pattern = @"<SolidColorBrush\s+x:Key=""([^""]+)""\s+Color=""([^""]+)""\s*/>";
                var matches = Regex.Matches(xamlContent, pattern);

                foreach (Match match in matches)
                {
                    if (match.Groups.Count == 3)
                    {
                        string key = match.Groups[1].Value;
                        string colorValue = match.Groups[2].Value;

                        if (TryParseColor(colorValue, out Color color))
                        {
                            dictionary[key] = new SolidColorBrush(color);
                            Debug.WriteLine($"Added SolidColorBrush: {key} = {colorValue}");
                        }
                    }
                }

                // Паттерн для SolidColorBrush с содержимым Color
                pattern = @"<SolidColorBrush\s+x:Key=""([^""]+)"">\s*<SolidColorBrush\.Color>\s*<Color>([^<]+)</Color>\s*</SolidColorBrush\.Color>\s*</SolidColorBrush>";
                matches = Regex.Matches(xamlContent, pattern, RegexOptions.Singleline);

                foreach (Match match in matches)
                {
                    if (match.Groups.Count == 3)
                    {
                        string key = match.Groups[1].Value;
                        string colorValue = match.Groups[2].Value.Trim();

                        if (TryParseColor(colorValue, out Color color))
                        {
                            dictionary[key] = new SolidColorBrush(color);
                            Debug.WriteLine($"Added SolidColorBrush (nested): {key} = {colorValue}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error parsing brushes: {ex}");
            }
        }

        private static void ParseColorsFromXaml(ResourceDictionary dictionary, string xamlContent)
        {
            try
            {
                // Ищем элементы Color с разными пространствами имен
                var colorPatterns = new[]
                {
                    @"<ui:Color\s+x:Key=""([^""]+)"">([^<]+)</ui:Color>",
                    @"<Color\s+x:Key=""([^""]+)"">([^<]+)</Color>",
                    @"<ui:Color\s+x:Key=""([^""]+)""\s+Color=""([^""]+)""\s*/>",
                    @"<Color\s+x:Key=""([^""]+)""\s+Color=""([^""]+)""\s*/>",
                    @"<ui:Color\s+x:Key=""([^""]+)"">\s*<ui:Color\.Color>([^<]+)</ui:Color\.Color>\s*</ui:Color>",
                    @"<Color\s+x:Key=""([^""]+)"">\s*<Color\.Color>([^<]+)</Color\.Color>\s*</Color>"
                };

                foreach (var pattern in colorPatterns)
                {
                    var matches = Regex.Matches(xamlContent, pattern, RegexOptions.Singleline);
                    foreach (Match match in matches)
                    {
                        if (match.Groups.Count >= 3)
                        {
                            string key = match.Groups[1].Value;
                            string colorValue = match.Groups[2].Value.Trim();

                            if (TryParseColor(colorValue, out Color color))
                            {
                                dictionary[key] = color;
                                Debug.WriteLine($"Added color: {key} = {colorValue}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error parsing colors: {ex}");
            }
        }

        private static void ParseStringsFromXaml(ResourceDictionary dictionary, string xamlContent)
        {
            try
            {
                var pattern = @"<x:String\s+x:Key=""([^""]+)"">([^<]+)</x:String>";
                var matches = Regex.Matches(xamlContent, pattern);

                foreach (Match match in matches)
                {
                    if (match.Groups.Count == 3)
                    {
                        string key = match.Groups[1].Value;
                        string value = match.Groups[2].Value.Trim();
                        dictionary[key] = value;
                        Debug.WriteLine($"Added string: {key} = {value}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error parsing strings: {ex}");
            }
        }

        private static void ParseOtherResourcesFromXaml(ResourceDictionary dictionary, string xamlContent)
        {
            try
            {
                // Парсим Thickness
                var thicknessPattern = @"<Thickness\s+x:Key=""([^""]+)"">([^<]+)</Thickness>";
                var matches = Regex.Matches(xamlContent, thicknessPattern);

                foreach (Match match in matches)
                {
                    if (match.Groups.Count == 3)
                    {
                        string key = match.Groups[1].Value;
                        string value = match.Groups[2].Value.Trim();

                        try
                        {
                            var thickness = ParseThickness(value);
                            dictionary[key] = thickness;
                            Debug.WriteLine($"Added Thickness: {key} = {value}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error parsing Thickness {value}: {ex}");
                        }
                    }
                }

                // Парсим CornerRadius
                var cornerRadiusPattern = @"<CornerRadius\s+x:Key=""([^""]+)"">([^<]+)</CornerRadius>";
                matches = Regex.Matches(xamlContent, cornerRadiusPattern);

                foreach (Match match in matches)
                {
                    if (match.Groups.Count == 3)
                    {
                        string key = match.Groups[1].Value;
                        string value = match.Groups[2].Value.Trim();

                        try
                        {
                            var cornerRadius = ParseCornerRadius(value);
                            dictionary[key] = cornerRadius;
                            Debug.WriteLine($"Added CornerRadius: {key} = {value}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error parsing CornerRadius {value}: {ex}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error parsing other resources: {ex}");
            }
        }

        private static Thickness ParseThickness(string value)
        {
            var parts = value.Split(',').Select(p => double.Parse(p.Trim())).ToArray();
            return parts.Length switch
            {
                1 => new Thickness(parts[0]),
                2 => new Thickness(parts[0], parts[1], parts[0], parts[1]),
                4 => new Thickness(parts[0], parts[1], parts[2], parts[3]),
                _ => new Thickness(0)
            };
        }

        private static CornerRadius ParseCornerRadius(string value)
        {
            var parts = value.Split(',').Select(p => double.Parse(p.Trim())).ToArray();
            return parts.Length switch
            {
                1 => new CornerRadius(parts[0]),
                4 => new CornerRadius(parts[0], parts[1], parts[2], parts[3]),
                _ => new CornerRadius(0)
            };
        }

        private static bool TryParseColor(string colorString, out Color color)
        {
            color = Colors.Transparent;

            try
            {
                if (string.IsNullOrEmpty(colorString)) return false;

                colorString = colorString.Trim();

                // Если цвет в формате "#AARRGGBB" или "#RRGGBB"
                if (colorString.StartsWith("#"))
                {
                    colorString = colorString.Replace("#", "");

                    if (colorString.Length == 6)
                    {
                        colorString = "FF" + colorString;
                    }

                    if (colorString.Length == 8)
                    {
                        var a = Convert.ToByte(colorString.Substring(0, 2), 16);
                        var r = Convert.ToByte(colorString.Substring(2, 2), 16);
                        var g = Convert.ToByte(colorString.Substring(4, 2), 16);
                        var b = Convert.ToByte(colorString.Substring(6, 2), 16);

                        color = Color.FromArgb(a, r, g, b);
                        return true;
                    }
                }
                // Если цвет по имени
                else
                {
                    var colorProperty = typeof(Colors).GetProperty(colorString);
                    if (colorProperty != null)
                    {
                        color = (Color)colorProperty.GetValue(null);
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        private static Task<List<string>> GetThemeFilesAsync()
        {
            var themeFiles = new List<string>();

            try
            {
                // Способ 1: Ищем в папке исполняемого файла
                string exeDir = AppContext.BaseDirectory;
                string themesPath = Path.Combine(exeDir, "Themes");

                if (Directory.Exists(themesPath))
                {
                    var files = Directory.GetFiles(themesPath, "*.xaml");
                    themeFiles.AddRange(files);
                    Debug.WriteLine($"Found {files.Length} theme files in: {themesPath}");
                }
                else
                {
                    Debug.WriteLine($"Themes directory not found: {themesPath}");
                }

                // Способ 2: Ищем в корне проекта (для разработки)
                if (themeFiles.Count == 0)
                {
                    try
                    {
                        // Поднимаемся на несколько уровней вверх для поиска в корне проекта
                        var dir = new DirectoryInfo(exeDir);
                        for (int i = 0; i < 3; i++)
                        {
                            dir = dir?.Parent;
                            if (dir == null) break;

                            themesPath = Path.Combine(dir.FullName, "Themes");
                            if (Directory.Exists(themesPath))
                            {
                                var files = Directory.GetFiles(themesPath, "*.xaml");
                                themeFiles.AddRange(files);
                                Debug.WriteLine($"Found {files.Length} theme files in project: {themesPath}");
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error searching in project root: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetThemeFilesAsync: {ex}");
            }

            return Task.FromResult(themeFiles);
        }

        private static bool IsBuiltInTheme(string fileName)
        {
            var builtInThemes = new[]
            {
                "LightTheme", "DarkTheme", "RedTheme", "DarkRedTheme",
                "LemonTheme", "DarkLemonTheme", "LightGoldTheme", "DarkGoldTheme",
                "LightGreenTheme", "DarkGreenTheme", "BlueTheme", "DarkBlueTheme"
            };

            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);

            // Проверяем, является ли файл встроенной темой
            bool isBuiltInTheme = builtInThemes.Any(theme =>
                fileNameWithoutExt.Equals(theme, StringComparison.OrdinalIgnoreCase));

            // Исключаем файлы ресурсов (содержащие "Resources" в названии)
            bool isResourceFile = fileNameWithoutExt.Contains("Resources", StringComparison.OrdinalIgnoreCase);

            return isBuiltInTheme || isResourceFile;
        }

        private static async Task<string> GetThemeDisplayNameAsync(string filePath)
        {
            try
            {
                Debug.WriteLine($"=== Reading theme file: {filePath} ===");

                // Проверяем существует ли файл
                if (!File.Exists(filePath))
                {
                    Debug.WriteLine($"File does not exist: {filePath}");
                    return GetFallbackName(filePath);
                }

                var content = await File.ReadAllTextAsync(filePath);
                Debug.WriteLine($"File content length: {content.Length} chars");

                // Логируем первые 200 символов для отладки
                if (content.Length > 0)
                {
                    string preview = content.Length > 200 ? content.Substring(0, 300) + "..." : content;
                    Debug.WriteLine($"File preview: {preview}");
                }

                // Ищем ThemeDisplayName разными способами

                // Способ 1: Регулярное выражение
                var pattern = @"<x:String\s+x:Key=""ThemeDisplayName""[^>]*>(.*?)</x:String>";
                var match = Regex.Match(content, pattern, RegexOptions.Singleline);

                if (match.Success)
                {
                    string displayName = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(displayName))
                    {
                        Debug.WriteLine($"✓ Found ThemeDisplayName via regex: '{displayName}'");
                        return displayName;
                    }
                    else
                    {
                        Debug.WriteLine("✗ ThemeDisplayName found but empty");
                    }
                }
                else
                {
                    Debug.WriteLine("✗ ThemeDisplayName not found via regex");
                }

                // Способ 2: Простой поиск подстроки
                if (content.Contains("ThemeDisplayName"))
                {
                    Debug.WriteLine("✓ Found 'ThemeDisplayName' string in content");

                    // Пытаемся извлечь значение вручную
                    int keyIndex = content.IndexOf("ThemeDisplayName", StringComparison.Ordinal);
                    if (keyIndex >= 0)
                    {
                        // Ищем следующий символ '>'
                        int valueStart = content.IndexOf('>', keyIndex);
                        if (valueStart > 0)
                        {
                            valueStart++; // Переходим после '>'
                            int valueEnd = content.IndexOf('<', valueStart);
                            if (valueEnd > valueStart)
                            {
                                string displayName = content.Substring(valueStart, valueEnd - valueStart).Trim();
                                if (!string.IsNullOrEmpty(displayName))
                                {
                                    Debug.WriteLine($"✓ Found ThemeDisplayName via manual: '{displayName}'");
                                    return displayName;
                                }
                            }
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("✗ 'ThemeDisplayName' string not found in content");
                }

                // Если ничего не нашли
                return GetFallbackName(filePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"!!! Error reading theme file {filePath}: {ex}");
                return GetFallbackName(filePath);
            }
        }

        private static string GetFallbackName(string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            if (fileName.EndsWith("Theme", StringComparison.OrdinalIgnoreCase))
            {
                fileName = fileName.Substring(0, fileName.Length - 5);
            }
            Debug.WriteLine($"Using fallback name: '{fileName}'");
            return fileName;
        }

        public static void ApplyCustomTheme(CustomThemeType themeType)
        {
            CurrentCustomTheme = themeType;
            SaveThemeSetting(themeType.ToString());
            UpdateApplicationResources(themeType);
        }

        public static void ApplyDynamicTheme(string themeName)
        {
            if (!_dynamicThemes.ContainsKey(themeName))
            {
                Debug.WriteLine($"Dynamic theme not found: {themeName}");
                return;
            }

            SaveThemeSetting(themeName);
            UpdateApplicationResources(themeName);
            Debug.WriteLine($"Applied dynamic theme: {themeName}");
        }
        
        private static void UpdateApplicationResources(CustomThemeType themeType)
        {
            if (Application.Current.Resources is not ResourceDictionary resources) return;

            // Получаем текущую тему для сравнения
            bool wasDarkTheme = ThemeHelper.IsDarkTheme();

            // Принудительно обновляем базовую тему (ВСЕГДА делаем это)
            string baseThemeKey = wasDarkTheme ? "Light" : "Dark";
            if (resources.ThemeDictionaries[baseThemeKey] is ResourceDictionary baseThemeDict)
            {
                foreach (var key in baseThemeDict.Keys)
                {
                    if (resources.ContainsKey(key))
                    {
                        resources[key] = baseThemeDict[key];
                    }
                }
            }

            // Теперь применяем выбранную тему
            string themeKey = themeType switch
            {
                CustomThemeType.Light => "Light",
                CustomThemeType.Dark => "Dark",
                CustomThemeType.Red => "Red",
                CustomThemeType.DarkRed => "DarkRed",
                CustomThemeType.Lemon => "Lemon",
                CustomThemeType.DarkLemon => "DarkLemon",
                CustomThemeType.Gold => "Gold",
                CustomThemeType.DarkGold => "DarkGold",
                CustomThemeType.Green => "Green",
                CustomThemeType.DarkGreen => "DarkGreen",
                CustomThemeType.Blue => "Blue",
                CustomThemeType.DarkBlue => "DarkBlue",
                _ => Application.Current.RequestedTheme == ApplicationTheme.Dark ? "Dark" : "Light" // Для Default используем системную тему
            };

            ApplyThemeDictionary(resources, themeKey);
        }
        private static void UpdateApplicationResources(string themeName)
        {
            if (Application.Current.Resources is not ResourceDictionary resources) return;
            ApplyThemeDictionary(resources, themeName);
        }

        private static void ApplyThemeDictionary(ResourceDictionary resources, string themeKey)
        {
            try
            {
                Debug.WriteLine($"Applying theme: {themeKey}");

                if (resources.ThemeDictionaries[themeKey] is ResourceDictionary themeDictionary)
                {
                    Debug.WriteLine($"Found theme dictionary with {themeDictionary.Count} resources");

                    // ВАЖНО: Копируем ВСЕ ресурсы из темы в основной словарь
                    foreach (var key in themeDictionary.Keys)
                    {
                        try
                        {
                            resources[key] = themeDictionary[key];
                            Debug.WriteLine($"Applied resource: {key} = {themeDictionary[key]}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error applying resource {key}: {ex}");
                        }
                    }

                    Debug.WriteLine($"Successfully applied {themeDictionary.Count} resources from {themeKey}");
                }
                else
                {
                    Debug.WriteLine($"Theme dictionary not found for key: {themeKey}");
                    Debug.WriteLine($"Available theme dictionaries: {string.Join(", ", resources.ThemeDictionaries.Keys)}");
                }

                // Принудительно обновляем все окна
                foreach (var window in WindowHelper.ActiveWindows)
                {
                    if (window.Content is FrameworkElement rootElement)
                    {
                        var currentTheme = rootElement.RequestedTheme;
                        rootElement.RequestedTheme = ElementTheme.Default;
                        rootElement.RequestedTheme = currentTheme;
                    }
                }
                //ThemeHelper.ForceThemeUpdate();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ApplyThemeDictionary: {ex}");
            }
        }
        private static void SaveThemeSetting(string value)
        {
            bool saved = false;
            if (App.SettingsManager != null)
            {
                try
                {
                    App.SettingsManager.SaveSetting(SelectedCustomThemeKey, value);
                    saved = true;
                }
                catch { /* Ignore if fails */ }
            }

            if (!saved)
            {
                ApplicationData.Current.LocalSettings.Values[SelectedCustomThemeKey] = value;
            }
        }

        // Простой метод для получения количества динамических тем
        public static int GetDynamicThemeCount()
        {
            return _dynamicThemes.Count;
        }

        // Простой метод для получения имени динамической темы по индексу
        public static string GetDynamicThemeName(int index)
        {
            return _dynamicThemes.Keys.ElementAtOrDefault(index);
        }

        // Простой метод для получения отображаемого имени динамической темы
        public static string GetDynamicThemeDisplayName(string themeName)
        {
            return _dynamicThemes.TryGetValue(themeName, out string displayName) ? displayName : themeName;
        }

        // Метод для проверки существования динамической темы
        public static bool DynamicThemeExists(string themeName)
        {
            return _dynamicThemes.ContainsKey(themeName);
        }
    }
}