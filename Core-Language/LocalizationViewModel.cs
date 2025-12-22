using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace Core_Language
{
    public class LocalizationViewModel : LocalizableViewModel
    {
        private string _selectedLanguage;
        private string _transmittedText;

        public LocalizationViewModel()
        {
            InitializeLanguages();
        }

        public ObservableCollection<LanguageItem> Languages => LanguageManager.Instance.AvailableLanguages;

        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (_selectedLanguage != value)
                {
                    _selectedLanguage = value;
                    OnPropertyChanged();
                    // Используем внутренний метод с двумя параметрами
                    ((LanguageManager)LanguageManager.Instance).SetLanguageInternal(value, true);
                }
            }
        }

        public string TransmittedText
        {
            get => _transmittedText;
            set => UpdateText(value, ref _transmittedText);
        }

        public CultureInfo CurrentCulture => CultureInfo.GetCultureInfo(LanguageManager.Instance.CurrentLanguageCode);

        private void InitializeLanguages()
        {
            if (Languages.Any())
            {
                SelectedLanguage = LanguageManager.Instance.GetSavedLanguage() ?? LanguageManager.DefaultLanguage;
            }
        }

        protected override void OnStringsUpdated()
        {
            OnPropertyChanged(nameof(TransmittedText));
            OnPropertyChanged(nameof(CurrentCulture));
        }
    }
}