using Microsoft.UI.Xaml;
using System;
using System.Runtime.CompilerServices;

namespace Core_Language
{
    public class LocalizedText : DependencyObject
    {
        private static readonly ConditionalWeakTable<LocalizedText, EventHandler<LanguageChangedEventArgs>> _subscriptions =
            new ConditionalWeakTable<LocalizedText, EventHandler<LanguageChangedEventArgs>>();

        public static readonly DependencyProperty KeyProperty = DependencyProperty.Register(
            "Key", typeof(string), typeof(LocalizedText),
            new PropertyMetadata(null, OnKeyChanged));

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value", typeof(string), typeof(LocalizedText),
            new PropertyMetadata(null));

        public string Key
        {
            get => (string)GetValue(KeyProperty);
            set => SetValue(KeyProperty, value);
        }

        public string Value
        {
            get => (string)GetValue(ValueProperty);
            private set => SetValue(ValueProperty, value);
        }

        private static void OnKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not LocalizedText text) return;

            if (_subscriptions.TryGetValue(text, out var handler))
            {
                LanguageManager.Instance.LanguageChanged -= handler;
                _subscriptions.Remove(text);
            }

            if (e.NewValue is string newKey)
            {
                handler = (s, args) => UpdateTextValue(text);
                LanguageManager.Instance.LanguageChanged += handler;
                _subscriptions.Add(text, handler);
                UpdateTextValue(text);
            }
        }

        private static void UpdateTextValue(LocalizedText text)
        {
            text.DispatcherQueue.TryEnqueue(() =>
            {
                text.Value = LanguageManager.Instance.GetString(text.Key);
            });
        }
    }
}