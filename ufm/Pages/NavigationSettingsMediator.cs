using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ufm
{
    public static class NavigationSettingsMediator
    {
        private static readonly List<WeakReference<IRefreshablePanel>> _panels = new();
        private static readonly object _lock = new object();

        public static void RegisterPanel(IRefreshablePanel panel)
        {
            lock (_lock)
            {
                _panels.Add(new WeakReference<IRefreshablePanel>(panel));
                CleanupDeadReferences();
                Debug.WriteLine($"[Mediator] Panel registered: {panel.PanelId}, total: {_panels.Count}");
            }
        }

        public static void UnregisterPanel(IRefreshablePanel panel)
        {
            lock (_lock)
            {
                _panels.RemoveAll(wr =>
                    wr.TryGetTarget(out var target) && target == panel);
                CleanupDeadReferences();
                Debug.WriteLine($"[Mediator] Panel unregistered: {panel.PanelId}, total: {_panels.Count}");
            }
        }

        public static void NotifySettingsChanged(bool showBackNavigation)
        {
            Debug.WriteLine($"[Mediator] Notifying {_panels.Count} panels about settings change: {showBackNavigation}");

            lock (_lock)
            {
                foreach (var weakRef in _panels.ToList())
                {
                    if (weakRef.TryGetTarget(out var panel))
                    {
                        try
                        {
                            panel.RefreshNavigation();
                            Debug.WriteLine($"[Mediator] Notified panel: {panel.PanelId}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[Mediator] Error refreshing panel {panel.PanelId}: {ex}");
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
