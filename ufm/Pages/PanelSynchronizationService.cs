using System;
using System.Collections.Generic;

namespace ufm
{
    public class PanelSynchronizationService
    {
        private readonly PanelManagerRegistry _registry;
        private readonly List<PanelManager> _subscribedPanels = new List<PanelManager>();
        private bool _isSyncing = false;

        public bool Enabled { get; set; } = true;

        public PanelSynchronizationService(PanelManagerRegistry registry)
        {
            _registry = registry;
            SubscribeToAllPanels();
        }

        private void SubscribeToAllPanels()
        {
            foreach (var panel in _registry.GetAllPanels())
            {
                SubscribeToPanel(panel);
            }
        }

        public void SubscribeToPanel(PanelManager panel)
        {
            if (!_subscribedPanels.Contains(panel))
            {
                panel.NavigationChanged += OnPanelNavigationChanged;
                _subscribedPanels.Add(panel);
            }
        }

        public void UnsubscribeFromPanel(PanelManager panel)
        {
            if (_subscribedPanels.Contains(panel))
            {
                panel.NavigationChanged -= OnPanelNavigationChanged;
                _subscribedPanels.Remove(panel);
            }
        }

        private void OnPanelNavigationChanged(object sender, EventArgs e)
        {
            if (!Enabled || _isSyncing) return;

            var sourcePanel = sender as PanelManager;
            if (sourcePanel == null) return;

            _isSyncing = true;

            try
            {
                // Синхронизируем другие панели с включенной синхронизацией
                foreach (var panel in _subscribedPanels)
                {
                    if (panel != sourcePanel &&
                        panel.State.SynchronizeWithOtherPanels &&
                        panel.State.CurrentPath != sourcePanel.CurrentPath)
                    {
                        // Используем навигацию вместо прямого присваивания
                        panel.NavigateTo(sourcePanel.CurrentPath);
                    }
                }
            }
            finally
            {
                _isSyncing = false;
            }
        }

        public void Dispose()
        {
            foreach (var panel in _subscribedPanels)
            {
                panel.NavigationChanged -= OnPanelNavigationChanged;
            }
            _subscribedPanels.Clear();
        }
    }
}