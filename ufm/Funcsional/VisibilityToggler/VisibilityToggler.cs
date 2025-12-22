using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace ufm
{
    public sealed class VisibilityToggler
    {
        private readonly Frame _rootFrame;
        private readonly string _containerPageName;
        private readonly string _frameName;
        private readonly string _targetPageName;
        private readonly string _elementName;

        public VisibilityToggler(
            Frame rootFrame,
            string containerPageName,
            string frameName,
            string targetPageName,
            string elementName)
        {
            _rootFrame = rootFrame ?? throw new ArgumentNullException(nameof(rootFrame));
            _containerPageName = containerPageName ?? throw new ArgumentNullException(nameof(containerPageName));
            _frameName = frameName ?? throw new ArgumentNullException(nameof(frameName));
            _targetPageName = targetPageName ?? throw new ArgumentNullException(nameof(targetPageName));
            _elementName = elementName ?? throw new ArgumentNullException(nameof(elementName));
        }

        public bool ToggleVisibility()
        {
            try
            {
                var element = FindTargetElement();
                if (element == null) return false;

                element.Visibility = element.Visibility == Visibility.Visible
                    ? Visibility.Collapsed
                    : Visibility.Visible;

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VisibilityToggler] Ошибка: {ex}");
                return false;
            }
        }

        // Добавляем в класс VisibilityToggler
        public bool SetVisibility(Visibility visibility)
        {
            try
            {
                var element = FindTargetElement();
                if (element == null) return false;

                element.Visibility = visibility;
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SetVisibility] Ошибка: {ex}");
                return false;
            }
        }

        public bool IsCurrentlyVisible()
        {
            var visibility = GetCurrentVisibility();
            return visibility.HasValue && visibility.Value == Visibility.Visible;
        }

        public Visibility? GetCurrentVisibility()
        {
            try
            {
                return FindTargetElement()?.Visibility;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VisibilityToggler] Ошибка: {ex}");
                return null;
            }
        }

        private UIElement FindTargetElement()
        {
            // 1. Проверяем контейнерную страницу
            if (_rootFrame.Content?.GetType().Name != _containerPageName)
            {
                Debug.WriteLine($"[VisibilityToggler] Ожидалась страница '{_containerPageName}', получена '{_rootFrame.Content?.GetType().Name}'");
                return null;
            }

            if (!(_rootFrame.Content is FrameworkElement containerPage))
                return null;

            // 2. Ищем Frame (комбинированный поиск)
            var frame = containerPage.FindName(_frameName) as Frame
                       ?? FindVisualChild<Frame>(containerPage, _frameName);

            if (frame == null)
            {
                Debug.WriteLine($"[VisibilityToggler] Фрейм '{_frameName}' не найден");
                return null;
            }

            // 3. Проверяем целевую страницу
            if (frame.Content?.GetType().Name != _targetPageName)
            {
                Debug.WriteLine($"[VisibilityToggler] Ожидалась страница '{_targetPageName}', получена '{frame.Content?.GetType().Name}'");
                return null;
            }

            if (!(frame.Content is FrameworkElement targetPage))
                return null;

            // 4. Ищем целевой элемент (комбинированный поиск)
            var element = targetPage.FindName(_elementName) as UIElement
                         ?? FindVisualChild<UIElement>(targetPage, _elementName);

            if (element == null)
                Debug.WriteLine($"[VisibilityToggler] Элемент '{_elementName}' не найден");

            return element;
        }

        private static T FindVisualChild<T>(DependencyObject parent, string name)
            where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T result && (child as FrameworkElement)?.Name == name)
                    return result;

                var foundChild = FindVisualChild<T>(child, name);
                if (foundChild != null)
                    return foundChild;
            }
            return null;
        }
    }
}