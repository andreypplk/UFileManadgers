using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Core_Language
{
    public abstract class LocalizableViewModel : INotifyPropertyChanged
    {
        protected LocalizableViewModel()
        {
            LanguageManager.Instance.LanguageChanged += OnLanguageChanged;
        }

        private bool _updating;

        private void OnLanguageChanged(object sender, LanguageChangedEventArgs e)
        {
            if (_updating) return;

            _updating = true;
            try
            {
                OnStringsUpdated();
            }
            finally
            {
                _updating = false;
            }
        }

        protected virtual void OnStringsUpdated()
        {
            // Реализация в производных классах
        }

        protected void UpdateText(string key, ref string field, params object[] args)
        {
            var newValue = LanguageManager.Instance.GetString(key, args);
            if (field == newValue) return;

            field = newValue;
            OnPropertyChanged(key);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}