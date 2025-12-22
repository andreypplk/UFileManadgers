using System;
using System.Collections.Generic;

namespace Core_Language
{
    internal interface ILocalization
    {
        event EventHandler<LanguageChangedEventArgs> LanguageChanged;
        IEnumerable<string> GetAvailableLanguages();
        string CurrentLanguageCode { get; }
        void SetLanguage(string languageCode);
        string GetString(string resourceId, params object[] formatArgs);
    }
}