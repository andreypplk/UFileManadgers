using System;
using System.Collections.Generic;
using System.Linq;

namespace ufm
{
    public static class NavigationSettingsMediator
    {
        private static readonly List<WeakReference<IRefreshablePanel>> _panels = new();
        private static readonly object _lock = new object();

        // Храним последнее отправленное значение, чтобы избежать повторной рассылки
        private static bool? _lastShowBackNavigation = null;

        public static void RegisterPanel(IRefreshablePanel panel)
        {
            lock (_lock)
            {
                _panels.Add(new WeakReference<IRefreshablePanel>(panel));
                CleanupDeadReferences();
            }
        }

        public static void UnregisterPanel(IRefreshablePanel panel)
        {
            lock (_lock)
            {
                _panels.RemoveAll(wr =>
                    wr.TryGetTarget(out var target) && target == panel);
                CleanupDeadReferences();
            }
        }

        public static void NotifySettingsChanged(bool showBackNavigation)
        {
            // Если значение не изменилось, не рассылаем уведомление
            if (_lastShowBackNavigation.HasValue && _lastShowBackNavigation.Value == showBackNavigation)
                return;

            _lastShowBackNavigation = showBackNavigation;

            lock (_lock)
            {
                foreach (var weakRef in _panels.ToList())
                {
                    if (weakRef.TryGetTarget(out var panel))
                    {
                        try
                        {
                            panel.RefreshNavigation();
                        }
                        catch
                        {
                        }
                    }
                }
                CleanupDeadReferences();
            }
        }

        private static void CleanupDeadReferences()
        {
            _panels.RemoveAll(wr => !wr.TryGetTarget(out _));
        }
    }
}