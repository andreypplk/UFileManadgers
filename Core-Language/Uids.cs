using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;

namespace Core_Language
{
    public static class Uids
    {
        public static readonly DependencyProperty UidProperty = DependencyProperty.RegisterAttached(
            "Uid",
            typeof(string),
            typeof(Uids),
            new PropertyMetadata(default(string), OnUidChanged));

        // Свойство для хранения ключа ToolTip
        public static readonly DependencyProperty ToolTipUidProperty = DependencyProperty.RegisterAttached(
            "ToolTipUid",
            typeof(string),
            typeof(Uids),
            new PropertyMetadata(default(string), OnToolTipUidChanged));

        // Новое свойство для хранения ключа RadioButton Content
        public static readonly DependencyProperty RadioButtonContentUidProperty = DependencyProperty.RegisterAttached(
            "RadioButtonContentUid",
            typeof(string),
            typeof(Uids),
            new PropertyMetadata(default(string), OnRadioButtonContentUidChanged));

        public static event EventHandler<DependencyObject> DependencyObjectUidSet;

        // Словари для отслеживания элементов и их ключей
        private static readonly Dictionary<DependencyObject, string> _toolTipUids = new Dictionary<DependencyObject, string>();
        private static readonly Dictionary<DependencyObject, string> _radioButtonContentUids = new Dictionary<DependencyObject, string>();

        public static string GetUid(DependencyObject obj) => (string)obj.GetValue(UidProperty);
        public static void SetUid(DependencyObject obj, string value) => obj.SetValue(UidProperty, value);

        public static string GetToolTipUid(DependencyObject obj) => (string)obj.GetValue(ToolTipUidProperty);
        public static void SetToolTipUid(DependencyObject obj, string value) => obj.SetValue(ToolTipUidProperty, value);

        public static string GetRadioButtonContentUid(DependencyObject obj) => (string)obj.GetValue(RadioButtonContentUidProperty);
        public static void SetRadioButtonContentUid(DependencyObject obj, string value) => obj.SetValue(RadioButtonContentUidProperty, value);

        private static void OnUidChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DependencyObjectUidSet?.Invoke(null, d);

            if (e.NewValue is string newUid && !string.IsNullOrEmpty(newUid))
            {
                UpdateElementText(d, newUid);
            }
        }

        private static void OnToolTipUidChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is string newUid && !string.IsNullOrEmpty(newUid))
            {
                _toolTipUids[d] = newUid;
                UpdateToolTip(d, newUid);
            }
            else
            {
                _toolTipUids.Remove(d);
            }
        }

        private static void OnRadioButtonContentUidChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is string newUid && !string.IsNullOrEmpty(newUid))
            {
                _radioButtonContentUids[d] = newUid;
                UpdateRadioButtonContent(d, newUid);
            }
            else
            {
                _radioButtonContentUids.Remove(d);
            }
        }

        public static void UpdateElementText(DependencyObject d, string uid)
        {
            if (d is null) return;

            try
            {
                var localizedText = LanguageManager.Instance.GetString(uid);

                // Обрабатываем элементы в порядке от конкретных к более общих
                switch (d)
                {
                    // Сначала обрабатываем самые специфические элементы
                    case TextBlock textBlock:
                        textBlock.Text = localizedText;
                        break;

                    case TextBox textBox:
                        textBox.Text = localizedText;
                        break;

                    case AppBarButton appBarButton:
                        appBarButton.Label = localizedText;
                        break;

                    case ToggleMenuFlyoutItem toggleItem:
                        toggleItem.Text = localizedText;
                        break;

                    case MenuFlyoutSubItem subMenu:
                        subMenu.Text = localizedText;
                        UpdateSubMenuItems(subMenu);
                        break;

                    case MenuFlyoutItem menuItem:
                        menuItem.Text = localizedText;
                        break;

                    case ComboBoxItem comboBoxItem:
                        comboBoxItem.Content = localizedText;
                        break;

                    case ToggleSwitch toggleSwitch:
                        toggleSwitch.Header = localizedText;
                        break;

                    case NavigationViewItem navItem:
                        navItem.Content = localizedText;
                        break;

                    case ComboBox comboBox:
                        comboBox.Header = localizedText;
                        break;

                    case Button button when button.Content is string:
                        button.Content = localizedText;
                        break;

                    // Затем обрабатываем более общие случаи
                    case ContentControl contentControl when contentControl.Content is string:
                        contentControl.Content = localizedText;
                        break;

                    case FrameworkElement frameworkElement:
                        HandleCustomElements(frameworkElement, localizedText);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Localization error: {ex}");
            }
        }

        // Метод для обновления всех ToolTip при смене языка
        public static void UpdateAllToolTips()
        {
            foreach (var item in _toolTipUids.ToList())
            {
                UpdateToolTip(item.Key, item.Value);
            }
        }

        // Метод для обновления всех RadioButton Content при смене языка
        public static void UpdateAllRadioButtonContents()
        {
            foreach (var item in _radioButtonContentUids.ToList())
            {
                UpdateRadioButtonContent(item.Key, item.Value);
            }
        }

        private static void UpdateToolTip(DependencyObject d, string uid)
        {
            try
            {
                var localizedText = LanguageManager.Instance.GetString(uid);
                ToolTipService.SetToolTip(d, localizedText);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ToolTip localization error: {ex}");
            }
        }

        private static void UpdateRadioButtonContent(DependencyObject d, string uid)
        {
            try
            {
                var localizedText = LanguageManager.Instance.GetString(uid);
                if (d is RadioButton radioButton)
                {
                    radioButton.Content = localizedText;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RadioButton content localization error: {ex}");
            }
        }

        private static void UpdateSubMenuItems(MenuFlyoutSubItem subMenu)
        {
            foreach (var item in subMenu.Items.OfType<FrameworkElement>())
            {
                var childUid = GetUid(item);
                if (!string.IsNullOrEmpty(childUid))
                {
                    UpdateElementText(item, childUid);
                }
            }
        }

        private static void HandleCustomElements(FrameworkElement element, string localizedText)
        {
            if (element is ToolTip toolTip && toolTip.Content is string)
            {
                toolTip.Content = localizedText;
                return;
            }

            // Попытка установить значение для ContentProperty, если оно существует
            var contentProp = ContentControl.ContentProperty;
            if (element.ReadLocalValue(contentProp) != DependencyProperty.UnsetValue)
            {
                element.SetValue(contentProp, localizedText);
            }
        }
    }
}