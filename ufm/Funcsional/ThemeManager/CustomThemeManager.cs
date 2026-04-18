using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
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
            LoadDynamicThemesAsync().ConfigureAwait(false);

            if (App.SettingsManager?.GetSetting<string>(SelectedCustomThemeKey) is string savedTheme)
            {
                ApplySavedTheme(savedTheme);
            }
            else if (ApplicationData.Current.LocalSettings.Values.TryGetValue(SelectedCustomThemeKey, out var localTheme))
            {
                ApplySavedTheme(localTheme.ToString());
            }
        }

        private static void ApplySavedTheme(string themeStr)
        {
            if (string.IsNullOrEmpty(themeStr)) return;

            if (Enum.TryParse(themeStr, out CustomThemeType theme))
            {
                CurrentCustomTheme = theme;
                ApplyCustomTheme(theme);
            }
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

                            await LoadThemeDictionaryAsync(themeName, filePath);
                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }

        private static async Task LoadThemeDictionaryAsync(string themeName, string filePath)
        {
            try
            {
                var xamlContent = await File.ReadAllTextAsync(filePath);

                var themeDictionary = ParseXamlToResourceDictionary(xamlContent);

                if (themeDictionary != null)
                {
                    _loadedThemeDictionaries[themeName] = themeDictionary;

                    Application.Current.Resources.ThemeDictionaries[themeName] = themeDictionary;
                }
            }
            catch
            {
            }
        }

        private static ResourceDictionary ParseXamlToResourceDictionary(string xamlContent)
        {
            try
            {
                var dictionary = new ResourceDictionary();

                xamlContent = RemoveComments(xamlContent);

                ParseAllResourcesFromXaml(dictionary, xamlContent);

                return dictionary;
            }
            catch
            {
                return null;
            }
        }

        private static string RemoveComments(string xamlContent)
        {
            return Regex.Replace(xamlContent, @"<!--.*?-->", "", RegexOptions.Singleline);
        }

        private static void ParseAllResourcesFromXaml(ResourceDictionary dictionary, string xamlContent)
        {
            try
            {
                int startIndex = xamlContent.IndexOf("<ResourceDictionary");
                if (startIndex == -1) return;

                startIndex = xamlContent.IndexOf(">", startIndex) + 1;
                int endIndex = xamlContent.LastIndexOf("</ResourceDictionary>");

                if (startIndex < 0 || endIndex < 0 || startIndex >= endIndex) return;

                string content = xamlContent.Substring(startIndex, endIndex - startIndex);

                ParseColorsFromXaml(dictionary, content);
                ParseBrushesFromXaml(dictionary, content);
                ParseStringsFromXaml(dictionary, content);
                ParseOtherResourcesFromXaml(dictionary, content);
            }
            catch
            {
            }
        }

        private static void ParseBrushesFromXaml(ResourceDictionary dictionary, string xamlContent)
        {
            try
            {
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
                        }
                    }
                }

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
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private static void ParseColorsFromXaml(ResourceDictionary dictionary, string xamlContent)
        {
            try
            {
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
                            }
                        }
                    }
                }
            }
            catch
            {
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
                    }
                }
            }
            catch
            {
            }
        }

        private static void ParseOtherResourcesFromXaml(ResourceDictionary dictionary, string xamlContent)
        {
            try
            {
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
                        }
                        catch
                        {
                        }
                    }
                }

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
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch
            {
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
                string exeDir = AppContext.BaseDirectory;
                string themesPath = Path.Combine(exeDir, "Themes");

                if (Directory.Exists(themesPath))
                {
                    var files = Directory.GetFiles(themesPath, "*.xaml");
                    themeFiles.AddRange(files);
                }

                if (themeFiles.Count == 0)
                {
                    try
                    {
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
                                break;
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
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

            bool isBuiltInTheme = builtInThemes.Any(theme =>
                fileNameWithoutExt.Equals(theme, StringComparison.OrdinalIgnoreCase));

            bool isResourceFile = fileNameWithoutExt.Contains("Resources", StringComparison.OrdinalIgnoreCase);

            return isBuiltInTheme || isResourceFile;
        }

        private static async Task<string> GetThemeDisplayNameAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return GetFallbackName(filePath);
                }

                var content = await File.ReadAllTextAsync(filePath);

                var pattern = @"<x:String\s+x:Key=""ThemeDisplayName""[^>]*>(.*?)</x:String>";
                var match = Regex.Match(content, pattern, RegexOptions.Singleline);

                if (match.Success)
                {
                    string displayName = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(displayName))
                    {
                        return displayName;
                    }
                }

                if (content.Contains("ThemeDisplayName"))
                {
                    int keyIndex = content.IndexOf("ThemeDisplayName", StringComparison.Ordinal);
                    if (keyIndex >= 0)
                    {
                        int valueStart = content.IndexOf('>', keyIndex);
                        if (valueStart > 0)
                        {
                            valueStart++;
                            int valueEnd = content.IndexOf('<', valueStart);
                            if (valueEnd > valueStart)
                            {
                                string displayName = content.Substring(valueStart, valueEnd - valueStart).Trim();
                                if (!string.IsNullOrEmpty(displayName))
                                {
                                    return displayName;
                                }
                            }
                        }
                    }
                }

                return GetFallbackName(filePath);
            }
            catch
            {
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
                return;
            }

            SaveThemeSetting(themeName);
            UpdateApplicationResources(themeName);
        }

        private static void UpdateApplicationResources(CustomThemeType themeType)
        {
            if (Application.Current.Resources is not ResourceDictionary resources) return;

            bool wasDarkTheme = ThemeHelper.IsDarkTheme();

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
                _ => Application.Current.RequestedTheme == ApplicationTheme.Dark ? "Dark" : "Light"
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
                if (resources.ThemeDictionaries[themeKey] is ResourceDictionary themeDictionary)
                {
                    foreach (var key in themeDictionary.Keys)
                    {
                        try
                        {
                            resources[key] = themeDictionary[key];
                        }
                        catch
                        {
                        }
                    }
                }

                foreach (var window in WindowHelper.ActiveWindows)
                {
                    if (window.Content is FrameworkElement rootElement)
                    {
                        var currentTheme = rootElement.RequestedTheme;
                        rootElement.RequestedTheme = ElementTheme.Default;
                        rootElement.RequestedTheme = currentTheme;
                    }
                }
            }
            catch
            {
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
                catch { }
            }

            if (!saved)
            {
                ApplicationData.Current.LocalSettings.Values[SelectedCustomThemeKey] = value;
            }
        }

        public static int GetDynamicThemeCount()
        {
            return _dynamicThemes.Count;
        }

        public static string GetDynamicThemeName(int index)
        {
            return _dynamicThemes.Keys.ElementAtOrDefault(index);
        }

        public static string GetDynamicThemeDisplayName(string themeName)
        {
            return _dynamicThemes.TryGetValue(themeName, out string displayName) ? displayName : themeName;
        }

        public static bool DynamicThemeExists(string themeName)
        {
            return _dynamicThemes.ContainsKey(themeName);
        }
    }
}