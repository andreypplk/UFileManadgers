using Core_FileManagement;
using System;
using System.Collections.Generic;

namespace ufm
{
    public interface INavigationManager : IDisposable
    {
        event EventHandler<NavigationEventArgs> NavigationChanged;

        void RegisterPanel(string panelId, DirectoryHistory history = null);
        void SetActivePanel(string panelId);
        void NavigateTo(string path, string panelId = null);
        bool CanGoBack(string panelId);
        bool CanGoForward(string panelId);
        void GoBack(string panelId = null);
        void GoForward(string panelId = null);
        string GetCurrentPath(string panelId);
        string GetCurrentDisplayName(string panelId);
        IReadOnlyList<string> GetRegisteredPanels();
        void ClearHistory(string panelId = null);
    }
}