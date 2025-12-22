using System;
using System.Globalization;
using System.Linq;

namespace Core_Language
{
    public sealed class LanguageChangedEventArgs : EventArgs
    {
        public LanguageChangedEventArgs(string previousLanguageCode, string currentLanguageCode)
        {
            if (string.IsNullOrWhiteSpace(currentLanguageCode))
                throw new ArgumentException("Invalid language code", nameof(currentLanguageCode));

            if (!CultureExists(currentLanguageCode))
                throw new CultureNotFoundException($"Culture '{currentLanguageCode}' not found");

            PreviousLanguageCode = previousLanguageCode;
            CurrentLanguageCode = currentLanguageCode;
            Culture = CultureInfo.GetCultureInfo(currentLanguageCode);
        }

        public string PreviousLanguageCode { get; }
        public string CurrentLanguageCode { get; }
        public CultureInfo Culture { get; }

        private static bool CultureExists(string cultureName)
        {
            return CultureInfo.GetCultures(CultureTypes.AllCultures)
                .Any(c => c.Name.Equals(cultureName, StringComparison.OrdinalIgnoreCase));
        }
    }
}