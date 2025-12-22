using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Media;

namespace ufm
{
    public static class UIHelper
    {
        // Вариант 1: Поиск по имени (исправленная версия)
        public static UIElement FindElementByName(DependencyObject parent, string name)
        {
            if (parent == null || string.IsNullOrEmpty(name))
                return null;

            // Проверяем сам элемент
            if (parent is FrameworkElement fe && fe.Name == name)
                return fe;

            // Рекурсивно проверяем детей
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var result = FindElementByName(child, name);
                if (result != null)
                    return result;
            }

            return null;
        }

        // Вариант 2: Поиск всех потомков определенного типа (оптимизированная версия)
        public static IEnumerable<T> GetDescendantsOfType<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;

            var queue = new Queue<DependencyObject>();
            queue.Enqueue(parent);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var childrenCount = VisualTreeHelper.GetChildrenCount(current);

                for (int i = 0; i < childrenCount; i++)
                {
                    var child = VisualTreeHelper.GetChild(current, i);
                    if (child is T matchingChild)
                    {
                        yield return matchingChild;
                    }
                    queue.Enqueue(child);
                }
            }
        }

        // Альтернативный вариант поиска по имени с использованием GetDescendantsOfType
        public static UIElement FindElementByNameAlternative(DependencyObject parent, string name)
        {
            return GetDescendantsOfType<FrameworkElement>(parent)
                .FirstOrDefault(e => e.Name == name) as UIElement;
        }

        // Метод для доступности (оставляем ваш существующий)
        public static void AnnounceActionForAccessibility(UIElement ue, string announcement, string activityID)
        {
            if (ue == null || string.IsNullOrEmpty(announcement) || string.IsNullOrEmpty(activityID))
                return;

            if (ue is FrameworkElement frameworkElement)
            {
                var peer = FrameworkElementAutomationPeer.FromElement(frameworkElement);
                peer?.RaiseNotificationEvent(
                    AutomationNotificationKind.ActionCompleted,
                    AutomationNotificationProcessing.ImportantMostRecent,
                    announcement,
                    activityID
                );
            }
        }
    }
}