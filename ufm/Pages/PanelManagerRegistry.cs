using System;
using System.Collections.Generic;

namespace ufm
{
    public class PanelManagerRegistry
    {
        private readonly Dictionary<string, PanelManager> _panels = new Dictionary<string, PanelManager>();
        private readonly INavigationManager _navigationManager;

        public PanelManager ActivePanel { get; private set; }
        public event EventHandler<PanelManager> ActivePanelChanged;

        public PanelManagerRegistry(INavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
        }

        public PanelManager GetOrCreatePanel(string panelId, string initialPath = "MyComputer")
        {
            if (!_panels.TryGetValue(panelId, out var manager))
            {
                manager = new PanelManager(panelId, _navigationManager, initialPath);
                _panels[panelId] = manager;

                // Подписываемся на события активации/деактивации
                manager.Activated += OnPanelActivated;
                manager.Deactivated += OnPanelDeactivated;
            }
            return manager;
        }

        public PanelManager GetPanel(string panelId)
        {
            return _panels.TryGetValue(panelId, out var manager) ? manager : null;
        }

        public bool SetActivePanel(string panelId)
        {
            var panel = GetPanel(panelId);
            if (panel != null)
            {
                // Деактивируем текущую активную панель
                if (ActivePanel != null && ActivePanel != panel)
                {
                    ActivePanel.Deactivate();
                }

                // Активируем новую панель
                panel.Activate();
                ActivePanel = panel;

                OnActivePanelChanged(panel);
                return true;
            }
            return false;
        }

        public IEnumerable<PanelManager> GetAllPanels()
        {
            return _panels.Values;
        }

        public Dictionary<string, PanelState> GetAllPanelStates()
        {
            var states = new Dictionary<string, PanelState>();
            foreach (var kvp in _panels)
            {
                states[kvp.Key] = kvp.Value.State.Clone();
            }
            return states;
        }

        public void LoadAllPanelStates(Dictionary<string, PanelState> states)
        {
            foreach (var kvp in states)
            {
                if (_panels.TryGetValue(kvp.Key, out var manager))
                {
                    manager.LoadState(kvp.Value);
                }
            }
        }

        private void OnPanelActivated(object sender, EventArgs e)
        {
            var activatedPanel = sender as PanelManager;
            if (activatedPanel != null && ActivePanel != activatedPanel)
            {
                ActivePanel = activatedPanel;
                OnActivePanelChanged(activatedPanel);
            }
        }

        private void OnPanelDeactivated(object sender, EventArgs e)
        {
            var deactivatedPanel = sender as PanelManager;
            if (deactivatedPanel != null && ActivePanel == deactivatedPanel)
            {
                ActivePanel = null;
            }
        }

        protected virtual void OnActivePanelChanged(PanelManager panel)
        {
            ActivePanelChanged?.Invoke(this, panel);
        }

        public void Dispose()
        {
            // Отписываемся от всех событий и dispose менеджеры
            foreach (var panel in _panels.Values)
            {
                panel.Activated -= OnPanelActivated;
                panel.Deactivated -= OnPanelDeactivated;
                panel.Dispose();
            }
            _panels.Clear();
        }
    }
}