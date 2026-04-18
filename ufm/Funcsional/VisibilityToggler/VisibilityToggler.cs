using System;
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
            catch
            {
                return false;
            }
        }

        public bool SetVisibility(Visibility visibility)
        {
            try
            {
                var element = FindTargetElement();
                if (element == null) return false;

                element.Visibility = visibility;
                return true;
            }
            catch
            {
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
            catch
            {
                return null;
            }
        }

        private UIElement FindTargetElement()
        {
            if (_rootFrame.Content?.GetType().Name != _containerPageName)
            {
                return null;
            }

            if (!(_rootFrame.Content is FrameworkElement containerPage))
                return null;

            var frame = containerPage.FindName(_frameName) as Frame
                       ?? FindVisualChild<Frame>(containerPage, _frameName);

            if (frame == null)
            {
                return null;
            }

            if (frame.Content?.GetType().Name != _targetPageName)
            {
                return null;
            }

            if (!(frame.Content is FrameworkElement targetPage))
                return null;

            var element = targetPage.FindName(_elementName) as UIElement
                         ?? FindVisualChild<UIElement>(targetPage, _elementName);

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