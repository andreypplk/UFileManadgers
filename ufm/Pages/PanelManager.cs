using Core_FileManagement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            // Регистрируем панель в навигационном менеджере
            _navigationManager.RegisterPanel(_panelId, new DirectoryHistory(initialPath, GetDisplayName(initialPath)));

            // Устанавливаем эту панель как активную в NavigationManager
            (_navigationManager as NavigationManager)?.SetActivePanel(_panelId);

            // Инициализируем состояние
            State.CurrentPath = initialPath;
            State.NavigationHistory.Add(initialPath);
            State.HistoryIndex = 0;

            // Подписываемся на события навигации
            _navigationManager.NavigationChanged += OnGlobalNavigationChanged;
        }

        public void NavigateTo(string path)
        {
            if (CurrentPath != path && !string.IsNullOrEmpty(path))
            {
                // Устанавливаем активную панель перед навигацией
                (_navigationManager as NavigationManager)?.SetActivePanel(_panelId);

                _navigationManager.NavigateTo(path, _panelId);
                UpdateLocalNavigationState();
                Debug.WriteLine($"Navigated to: {path}");
            }
        }

        public string GoBack()
        {
            if (CanGoBack)
            {
                (_navigationManager as NavigationManager)?.SetActivePanel(_panelId);
                _navigationManager.GoBack(_panelId);
                UpdateLocalNavigationState();
                Debug.WriteLine($"Went back to: {CurrentPath}");
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
                Debug.WriteLine($"Went forward to: {CurrentPath}");
                return CurrentPath;
            }
            return null;
        }

        public void UpdateState(Action<PanelState> updateAction)
        {
            updateAction?.Invoke(State);
            OnStateChanged();

            // ДОБАВЛЯЕМ: Автосохранение при любом обновлении состояния
            AutoSavePanelState();
        }

        public void LoadState(PanelState state = null)
        {
            try
            {
                if (state != null)
                {
                    // Загружаем из переданного состояния
                    State = state.Clone();
                }
                else if (App.SettingsManager != null)
                {
                    // Загружаем из SettingsManager
                    State.ViewMode = Enum.TryParse(App.SettingsManager.GetSetting<string>($"{_panelId}_ViewMode"), out ViewMode viewMode)
                        ? viewMode : ViewMode.Icons;

                    State.IconSize = App.SettingsManager.GetSetting<string>($"{_panelId}_IconSize");
                    State.CurrentPath = App.SettingsManager.GetSetting<string>($"{_panelId}_CurrentPath", "MyComputer");
                    State.HistoryIndex = App.SettingsManager.GetSetting<int>($"{_panelId}_HistoryIndex", -1);

                    // Загружаем навигационную историю
                    State.NavigationHistory = App.SettingsManager.GetSetting<List<string>>($"{_panelId}_NavigationHistory", new List<string>());

                    // Загружаем сортировку
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

                    // Загружаем фильтры
                    State.SearchFilter = App.SettingsManager.GetSetting<string>($"{_panelId}_SearchFilter", "");
                    State.FileTypeFilter = App.SettingsManager.GetSetting<string>($"{_panelId}_FileTypeFilter", "All");

                    // Загружаем настройки отображения
                    State.ShowHiddenFiles = App.SettingsManager.GetSetting<bool>($"{_panelId}_ShowHiddenFiles", false);
                    State.ShowFileExtensions = App.SettingsManager.GetSetting<bool>($"{_panelId}_ShowFileExtensions", true);
                    State.ColumnWidth = App.SettingsManager.GetSetting<double>($"{_panelId}_ColumnWidth", 200);

                    // Загружаем видимые колонки
                    State.VisibleColumns = App.SettingsManager.GetSetting<List<string>>($"{_panelId}_VisibleColumns", new List<string>());

                    // Загружаем выделенные элементы
                    State.SelectedItems = App.SettingsManager.GetSetting<List<string>>($"{_panelId}_SelectedItems", new List<string>());

                    // Загружаем сфокусированный элемент
                    State.FocusedItem = App.SettingsManager.GetSetting<string>($"{_panelId}_FocusedItem", "");

                    // Загружаем позицию скролла
                    State.ScrollPosition = App.SettingsManager.GetSetting<double>($"{_panelId}_ScrollPosition", 0);

                    Debug.WriteLine($"Loaded state for panel: {_panelId}");
                }

                // Восстанавливаем навигационную историю в NavigationManager
                if (State.NavigationHistory.Count > 0 && !string.IsNullOrEmpty(State.CurrentPath))
                {
                    var newHistory = new DirectoryHistory(State.CurrentPath, GetDisplayName(State.CurrentPath));

                    // Добавляем всю историю (кроме текущего пути)
                    foreach (var path in State.NavigationHistory)
                    {
                        if (path != State.CurrentPath)
                        {
                            newHistory.Add(path, GetDisplayName(path));
                        }
                    }

                    // Перерегистрируем панель с восстановленной историей
                    _navigationManager.RegisterPanel(_panelId, newHistory);
                }

                OnStateChanged();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading state for panel {_panelId}: {ex.Message}");
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

            // Обновляем локальную историю для сериализации
            if (!State.NavigationHistory.Contains(CurrentPath))
            {
                State.NavigationHistory.Add(CurrentPath);
            }
            State.HistoryIndex = State.NavigationHistory.IndexOf(CurrentPath);
        }

        private void OnGlobalNavigationChanged(object sender, NavigationEventArgs e)
        {
            // Обрабатываем только события, относящиеся к этой панели
            if (e.PanelId == _panelId)
            {
                try
                {
                    // Фильтрация быстрых событий
                    var now = DateTime.Now;
                    if ((now - _lastNavigationTime).TotalMilliseconds < 50)
                        return;

                    _lastNavigationTime = now;

                    // Обновляем локальное состояние
                    UpdateLocalNavigationState();

                    // Авто-сохранение состояния
                    if (++_navigationEventCount >= 3)
                    {
                        _navigationEventCount = 0;
                        AutoSavePanelState();
                    }

                    // Уведомляем подписчиков
                    NavigationChanged?.Invoke(this, EventArgs.Empty);

                    Debug.WriteLine($"Navigation: {_panelId} -> {CurrentPath}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Navigation event error: {ex.Message}");
                }
            }
        }

        private void AutoSavePanelState()
        {
            try
            {
                // Используем SettingsManager из ViewPage для сохранения состояния
                if (App.SettingsManager != null)
                {
                    // Сохраняем основные настройки панели
                    App.SettingsManager.SaveSetting($"{_panelId}_ViewMode", State.ViewMode.ToString());
                    App.SettingsManager.SaveSetting($"{_panelId}_IconSize", State.IconSize);
                    App.SettingsManager.SaveSetting($"{_panelId}_CurrentPath", State.CurrentPath);
                    App.SettingsManager.SaveSetting($"{_panelId}_HistoryIndex", State.HistoryIndex);

                    // Сохраняем навигационную историю
                    App.SettingsManager.SaveSetting($"{_panelId}_NavigationHistory", State.NavigationHistory);

                    // Сохраняем состояние сортировки
                    if (State.CurrentSort != null)
                    {
                        App.SettingsManager.SaveSetting($"{_panelId}_SortProperty", State.CurrentSort.PropertyName);
                        App.SettingsManager.SaveSetting($"{_panelId}_SortAscending", State.IsAscendingSort);
                    }

                    // Сохраняем фильтры
                    App.SettingsManager.SaveSetting($"{_panelId}_SearchFilter", State.SearchFilter);
                    App.SettingsManager.SaveSetting($"{_panelId}_FileTypeFilter", State.FileTypeFilter);

                    // Сохраняем настройки отображения
                    App.SettingsManager.SaveSetting($"{_panelId}_ShowHiddenFiles", State.ShowHiddenFiles);
                    App.SettingsManager.SaveSetting($"{_panelId}_ShowFileExtensions", State.ShowFileExtensions);
                    App.SettingsManager.SaveSetting($"{_panelId}_ColumnWidth", State.ColumnWidth);

                    // Сохраняем видимые колонки
                    App.SettingsManager.SaveSetting($"{_panelId}_VisibleColumns", State.VisibleColumns);

                    // Сохраняем выделенные элементы (осторожно с большими списками)
                    if (State.SelectedItems.Count > 0 && State.SelectedItems.Count < 1000)
                    {
                        App.SettingsManager.SaveSetting($"{_panelId}_SelectedItems", State.SelectedItems);
                    }

                    // Сохраняем сфокусированный элемент
                    App.SettingsManager.SaveSetting($"{_panelId}_FocusedItem", State.FocusedItem);

                    // Сохраняем позицию скролла
                    App.SettingsManager.SaveSetting($"{_panelId}_ScrollPosition", State.ScrollPosition);

                    Debug.WriteLine($"Auto-saved state for panel: {_panelId}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error auto-saving panel state for {_panelId}: {ex.Message}");
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