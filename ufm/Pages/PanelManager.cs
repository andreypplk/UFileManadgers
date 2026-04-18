using Core_FileManagement;
using System;
using System.Collections.Generic;
using static ufm.ViewPage;

namespace ufm
{
    public class PanelManager
    {
        private readonly INavigationManager _navigationManager;
        private readonly string _panelId;
        private DateTime _lastNavigationTime = DateTime.MinValue;
        private int _navigationEventCount = 0;

        public PanelState State { get; private set; }
        public string PanelId => _panelId;
        public string CurrentPath => _navigationManager.GetCurrentPath(_panelId);
        public bool CanGoBack => _navigationManager.CanGoBack(_panelId);
        public bool CanGoForward => _navigationManager.CanGoForward(_panelId);

        public event EventHandler NavigationChanged;
        public event EventHandler StateChanged;
        public event EventHandler Activated;
        public event EventHandler Deactivated;

        public PanelManager(string panelId, INavigationManager navigationManager, string initialPath = "MyComputer")
        {
            _panelId = panelId;
            _navigationManager = navigationManager;
            State = new PanelState();

            _navigationManager.RegisterPanel(_panelId, new DirectoryHistory(initialPath, GetDisplayName(initialPath)));

            (_navigationManager as NavigationManager)?.SetActivePanel(_panelId);

            State.CurrentPath = initialPath;
            State.NavigationHistory.Add(initialPath);
            State.HistoryIndex = 0;

            _navigationManager.NavigationChanged += OnGlobalNavigationChanged;
        }

        public void NavigateTo(string path)
        {
            if (CurrentPath != path && !string.IsNullOrEmpty(path))
            {
                (_navigationManager as NavigationManager)?.SetActivePanel(_panelId);

                _navigationManager.NavigateTo(path, _panelId);
                UpdateLocalNavigationState();
            }
        }

        public string GoBack()
        {
            if (CanGoBack)
            {
                (_navigationManager as NavigationManager)?.SetActivePanel(_panelId);
                _navigationManager.GoBack(_panelId);
                UpdateLocalNavigationState();
                return CurrentPath;
            }
            return null;
        }

        public string GoForward()
        {
            if (CanGoForward)
            {
                (_navigationManager as NavigationManager)?.SetActivePanel(_panelId);
                _navigationManager.GoForward(_panelId);
                UpdateLocalNavigationState();
                return CurrentPath;
            }
            return null;
        }

        public void UpdateState(Action<PanelState> updateAction)
        {
            updateAction?.Invoke(State);
            OnStateChanged();

            AutoSavePanelState();
        }

        public void LoadState(PanelState state = null)
        {
            try
            {
                if (state != null)
                {
                    State = state.Clone();
                }
                else if (App.SettingsManager != null)
                {
                    State.ViewMode = Enum.TryParse(App.SettingsManager.GetSetting<string>($"{_panelId}_ViewMode"), out ViewMode viewMode)
                        ? viewMode : ViewMode.Icons;

                    State.IconSize = App.SettingsManager.GetSetting<string>($"{_panelId}_IconSize");
                    State.CurrentPath = App.SettingsManager.GetSetting<string>($"{_panelId}_CurrentPath", "MyComputer");
                    State.HistoryIndex = App.SettingsManager.GetSetting<int>($"{_panelId}_HistoryIndex", -1);

                    State.NavigationHistory = App.SettingsManager.GetSetting<List<string>>($"{_panelId}_NavigationHistory", new List<string>());

                    var sortProperty = App.SettingsManager.GetSetting<string>($"{_panelId}_SortProperty", null);
                    if (!string.IsNullOrEmpty(sortProperty))
                    {
                        State.CurrentSort = new SortDescription
                        {
                            PropertyName = sortProperty,
                            DisplayName = sortProperty
                        };
                        State.IsAscendingSort = App.SettingsManager.GetSetting<bool>($"{_panelId}_SortAscending", true);
                    }

                    State.SearchFilter = App.SettingsManager.GetSetting<string>($"{_panelId}_SearchFilter", "");
                    State.FileTypeFilter = App.SettingsManager.GetSetting<string>($"{_panelId}_FileTypeFilter", "All");

                    State.ShowHiddenFiles = App.SettingsManager.GetSetting<bool>($"{_panelId}_ShowHiddenFiles", false);
                    State.ShowFileExtensions = App.SettingsManager.GetSetting<bool>($"{_panelId}_ShowFileExtensions", true);
                    State.ColumnWidth = App.SettingsManager.GetSetting<double>($"{_panelId}_ColumnWidth", 200);

                    State.VisibleColumns = App.SettingsManager.GetSetting<List<string>>($"{_panelId}_VisibleColumns", new List<string>());

                    State.SelectedItems = App.SettingsManager.GetSetting<List<string>>($"{_panelId}_SelectedItems", new List<string>());

                    State.FocusedItem = App.SettingsManager.GetSetting<string>($"{_panelId}_FocusedItem", "");

                    State.ScrollPosition = App.SettingsManager.GetSetting<double>($"{_panelId}_ScrollPosition", 0);
                }

                if (State.NavigationHistory.Count > 0 && !string.IsNullOrEmpty(State.CurrentPath))
                {
                    var newHistory = new DirectoryHistory(State.CurrentPath, GetDisplayName(State.CurrentPath));

                    foreach (var path in State.NavigationHistory)
                    {
                        if (path != State.CurrentPath)
                        {
                            newHistory.Add(path, GetDisplayName(path));
                        }
                    }

                    _navigationManager.RegisterPanel(_panelId, newHistory);
                }

                OnStateChanged();
            }
            catch
            {
            }
        }

        public void Activate()
        {
            State.IsActive = true;
            (_navigationManager as NavigationManager)?.SetActivePanel(_panelId);
            Activated?.Invoke(this, EventArgs.Empty);
            OnStateChanged();
        }

        public void Deactivate()
        {
            State.IsActive = false;
            Deactivated?.Invoke(this, EventArgs.Empty);
            OnStateChanged();
        }

        private void UpdateLocalNavigationState()
        {
            State.CurrentPath = CurrentPath;

            if (!State.NavigationHistory.Contains(CurrentPath))
            {
                State.NavigationHistory.Add(CurrentPath);
            }
            State.HistoryIndex = State.NavigationHistory.IndexOf(CurrentPath);
        }

        private void OnGlobalNavigationChanged(object sender, NavigationEventArgs e)
        {
            if (e.PanelId == _panelId)
            {
                try
                {
                    var now = DateTime.Now;
                    if ((now - _lastNavigationTime).TotalMilliseconds < 50)
                        return;

                    _lastNavigationTime = now;

                    UpdateLocalNavigationState();

                    if (++_navigationEventCount >= 3)
                    {
                        _navigationEventCount = 0;
                        AutoSavePanelState();
                    }

                    NavigationChanged?.Invoke(this, EventArgs.Empty);
                }
                catch
                {
                }
            }
        }

        private void AutoSavePanelState()
        {
            try
            {
                if (App.SettingsManager != null)
                {
                    App.SettingsManager.SaveSetting($"{_panelId}_ViewMode", State.ViewMode.ToString());
                    App.SettingsManager.SaveSetting($"{_panelId}_IconSize", State.IconSize);
                    App.SettingsManager.SaveSetting($"{_panelId}_CurrentPath", State.CurrentPath);
                    App.SettingsManager.SaveSetting($"{_panelId}_HistoryIndex", State.HistoryIndex);

                    App.SettingsManager.SaveSetting($"{_panelId}_NavigationHistory", State.NavigationHistory);

                    if (State.CurrentSort != null)
                    {
                        App.SettingsManager.SaveSetting($"{_panelId}_SortProperty", State.CurrentSort.PropertyName);
                        App.SettingsManager.SaveSetting($"{_panelId}_SortAscending", State.IsAscendingSort);
                    }

                    App.SettingsManager.SaveSetting($"{_panelId}_SearchFilter", State.SearchFilter);
                    App.SettingsManager.SaveSetting($"{_panelId}_FileTypeFilter", State.FileTypeFilter);

                    App.SettingsManager.SaveSetting($"{_panelId}_ShowHiddenFiles", State.ShowHiddenFiles);
                    App.SettingsManager.SaveSetting($"{_panelId}_ShowFileExtensions", State.ShowFileExtensions);
                    App.SettingsManager.SaveSetting($"{_panelId}_ColumnWidth", State.ColumnWidth);

                    App.SettingsManager.SaveSetting($"{_panelId}_VisibleColumns", State.VisibleColumns);

                    if (State.SelectedItems.Count > 0 && State.SelectedItems.Count < 1000)
                    {
                        App.SettingsManager.SaveSetting($"{_panelId}_SelectedItems", State.SelectedItems);
                    }

                    App.SettingsManager.SaveSetting($"{_panelId}_FocusedItem", State.FocusedItem);

                    App.SettingsManager.SaveSetting($"{_panelId}_ScrollPosition", State.ScrollPosition);
                }
            }
            catch
            {
            }
        }

        private string GetDisplayName(string path)
        {
            if (path == "MyComputer")
                return "Мой Компьютер";

            return System.IO.Path.GetFileName(path);
        }

        protected virtual void OnStateChanged()
        {
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            _navigationManager.NavigationChanged -= OnGlobalNavigationChanged;
        }
    }
}