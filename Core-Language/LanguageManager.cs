using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Windows.ApplicationModel.Resources;
using Windows.Globalization;
using Windows.Storage;

namespace Core_Language
{
    public class LanguageManager : ILocalization
    {
        private static readonly Lazy<LanguageManager> _instance =
            new Lazy<LanguageManager>(() => new LanguageManager());
        public static LanguageManager Instance => _instance.Value;

        private ResourceLoader _resourceLoader;
        private string _externalResourcesPath;
        private string _currentLanguage;
        public const string DefaultLanguage = "en-US";

        public ObservableCollection<LanguageItem> AvailableLanguages { get; } = new ObservableCollection<LanguageItem>();
        public event EventHandler<LanguageChangedEventArgs> LanguageChanged;

        private LanguageManager() { }

        public void Initialize(string externalResourcesPath = null)
        {
            _externalResourcesPath = externalResourcesPath ?? ApplicationData.Current.LocalFolder.Path;
            LoadSystemLanguages();
            LoadExternalLanguages();

            var savedLanguage = GetSavedLanguage();
            var targetLanguage = ValidateLanguage(savedLanguage) ??
                               ValidateLanguage(GetSystemLanguage()) ??
                               DefaultLanguage;

            SetLanguageInternal(targetLanguage, true);
        }

        // Реализация интерфейса
        public void SetLanguage(string languageCode) => SetLanguageInternal(languageCode, true);

        // Внутренний метод с дополнительным параметром
        internal void SetLanguageInternal(string languageCode, bool saveSetting)
        {
            if (_currentLanguage == languageCode) return;

            var previous = _currentLanguage;
            try
            {
                ApplicationLanguages.PrimaryLanguageOverride = languageCode;
                _resourceLoader = ResourceLoader.GetForViewIndependentUse();
                _currentLanguage = languageCode;

                if (saveSetting)
                {
                    ApplicationData.Current.LocalSettings.Values["AppLanguage"] = languageCode;
                }

                AddLanguageIfMissing(languageCode);
                LanguageChanged?.Invoke(this, new LanguageChangedEventArgs(previous, languageCode));
            }
            catch
            {
                _currentLanguage = previous;
                throw;
            }
        }

        private void LoadSystemLanguages()
        {
            foreach (var lang in ApplicationLanguages.ManifestLanguages)
            {
                AddLanguageIfMissing(lang);
            }
        }

        private void LoadExternalLanguages()
        {
            try
            {
                var externalFiles = Directory.GetFiles(_externalResourcesPath, "*.resw");
                foreach (var file in externalFiles)
                {
                    var langCode = Path.GetFileNameWithoutExtension(file).Split('.').LastOrDefault();
                    if (!string.IsNullOrEmpty(langCode) && !AvailableLanguages.Any(l => l.LanguageCode == langCode))
                    {
                        AvailableLanguages.Add(new LanguageItem
                        {
                            LanguageCode = langCode,
                            DisplayName = CapitalizeLanguageName(new CultureInfo(langCode).NativeName)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading external languages: {ex}");
            }
        }
        public string GetString(string resourceId, params object[] formatArgs)
        {
            var value = GetStringInternal(resourceId);
            return formatArgs.Length > 0 ? string.Format(value, formatArgs) : value;
        }

        private string GetStringInternal(string key)
        {
            try
            {
                var value = _resourceLoader?.GetString(key);
                if (!string.IsNullOrEmpty(value) && value != key) return value;
            }
            catch { }

            try
            {
                var externalPath = Path.Combine(_externalResourcesPath, $"Resources.{_currentLanguage}.resw");
                if (File.Exists(externalPath))
                {
                    var lines = File.ReadAllLines(externalPath);
                    var entry = lines.FirstOrDefault(l => l.StartsWith($"{key}="));
                    if (entry != null) return entry.Split('=')[1].Trim();
                }
            }
            catch { }

            return $"[{key}]";
        }

        private void AddLanguageIfMissing(string language)
        {
            if (AvailableLanguages.All(l => l.LanguageCode != language))
            {
                AvailableLanguages.Add(new LanguageItem
                {
                    LanguageCode = language,
                    DisplayName = CapitalizeLanguageName(new CultureInfo(language).NativeName)
                });
            }
        }

        private string ValidateLanguage(string language)
        {
            if (string.IsNullOrEmpty(language)) return null;
            if (ApplicationLanguages.ManifestLanguages.Contains(language)) return language;

            var externalFile = Path.Combine(_externalResourcesPath, $"Resources.{language}.resw");
            if (File.Exists(externalFile)) return language;

            var baseLanguage = language.Split('-')[0];
            return AvailableLanguages
                .FirstOrDefault(l => l.LanguageCode.StartsWith(baseLanguage + "-"))?
                .LanguageCode;
        }

        private string GetSystemLanguage() => CultureInfo.CurrentUICulture.Name;
        internal string GetSavedLanguage() => ApplicationData.Current.LocalSettings.Values["AppLanguage"]?.ToString();
        public IEnumerable<string> GetAvailableLanguages() => AvailableLanguages.Select(l => l.LanguageCode);
        public string CurrentLanguageCode => _currentLanguage;
        private string CapitalizeLanguageName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;

            // Используем TextInfo для корректной капитализации с учетом культуры
            TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;

            // Разделяем строку на части, сохраняя разделители
            var parts = new List<string>();
            var currentPart = new StringBuilder();
            foreach (char ch in name)
            {
                if (ch == ' ' || ch == '(' || ch == ')')
                {
                    if (currentPart.Length > 0)
                    {
                        parts.Add(currentPart.ToString());
                        currentPart.Clear();
                    }
                    parts.Add(ch.ToString());
                }
                else
                {
                    currentPart.Append(ch);
                }
            }
            if (currentPart.Length > 0)
            {
                parts.Add(currentPart.ToString());
            }

            // Капитализируем каждую часть, кроме скобок
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i] != "(" && parts[i] != ")")
                {
                    parts[i] = textInfo.ToTitleCase(parts[i].ToLower());
                }
            }

            // Собираем строку обратно
            return string.Concat(parts);
        }
    }
}