using Core_FileManagement;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ufm
{
    public class NavigationEventArgs : EventArgs
    {
        public string PanelId { get; set; }
        public string Path { get; set; }
        public NavigationAction Action { get; set; }
    }

    public enum NavigationAction
    {
        Navigate,
        Back,
        Forward
    }

    public class NavigationManager : INavigationManager
    {
        private readonly Dictionary<string, DirectoryHistory> _panelHistories = new();
        private readonly DispatcherQueue _dispatcherQueue;
        private string _activePanelId;

        public event EventHandler<NavigationEventArgs> NavigationChanged;

        public NavigationManager()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        }

        public void RegisterPanel(string panelId, DirectoryHistory history = null)
        {
            if (!_panelHistories.ContainsKey(panelId))
            {
                _panelHistories[panelId] = history ?? new DirectoryHistory("MyComputer", "Мой Компьютер");
                _panelHistories[panelId].HistoryChanged += (s, e) => OnHistoryChanged(panelId);
            }
        }

        public void SetActivePanel(string panelId)
        {
            if (_panelHistories.ContainsKey(panelId))
            {
                _activePanelId = panelId;
            }
        }

        public void NavigateTo(string path, string panelId = null)
        {
            var targetPanelId = panelId ?? _activePanelId;
            if (string.IsNullOrEmpty(targetPanelId) || !_panelHistories.ContainsKey(targetPanelId))
                return;

            // ПРОВЕРКА НА ДУБЛИКАТ - не добавляем тот же путь
            var currentPath = _panelHistories[targetPanelId].Current.DirectoryPath;
            if (currentPath == path)
            {
                Debug.WriteLine($"NavigationManager.NavigateTo: путь не изменился ({path}), пропускаем");
                return;
            }

            // Получаем отображаемое имя
            string displayName = GetDisplayName(path);

            _panelHistories[targetPanelId].Add(path, displayName);
            // НЕ вызываем OnNavigationChanged здесь - он вызовется автоматически через HistoryChanged
        }

        public bool CanGoBack(string panelId)
        {
            var targetPanelId = panelId ?? _activePanelId;
            return !string.IsNullOrEmpty(targetPanelId) &&
                   _panelHistories.ContainsKey(targetPanelId) &&
                   _panelHistories[targetPanelId].CanMoveBack;
        }

        public bool CanGoForward(string panelId)
        {
            var targetPanelId = panelId ?? _activePanelId;
            return !string.IsNullOrEmpty(targetPanelId) &&
                   _panelHistories.ContainsKey(targetPanelId) &&
                   _panelHistories[targetPanelId].CanMoveForward;
        }

        public void GoBack(string panelId = null)
        {
            var targetPanelId = panelId ?? _activePanelId;
            if (string.IsNullOrEmpty(targetPanelId) || !_panelHistories.ContainsKey(targetPanelId))
                return;

            if (_panelHistories[targetPanelId].CanMoveBack)
            {
                _panelHistories[targetPanelId].MoveBack();
                // OnNavigationChanged вызовется автоматически через HistoryChanged
            }
            else
            {
                Debug.WriteLine($"NavigationManager.GoBack: невозможно вернуться назад для панели {targetPanelId}");
            }
        }

        public void GoForward(string panelId = null)
        {
            var targetPanelId = panelId ?? _activePanelId;
            if (string.IsNullOrEmpty(targetPanelId) || !_panelHistories.ContainsKey(targetPanelId))
                return;

            if (_panelHistories[targetPanelId].CanMoveForward)
            {
                _panelHistories[targetPanelId].MoveForward();
                // OnNavigationChanged вызовется автоматически через HistoryChanged
            }
            else
            {
                Debug.WriteLine($"NavigationManager.GoForward: невозможно перейти вперед для панели {targetPanelId}");
            }
        }

        public string GetCurrentPath(string panelId)
        {
            var targetPanelId = panelId ?? _activePanelId;
            return !string.IsNullOrEmpty(targetPanelId) && _panelHistories.ContainsKey(targetPanelId)
                ? _panelHistories[targetPanelId].Current.DirectoryPath
                : null;
        }

        public string GetCurrentDisplayName(string panelId)
        {
            var targetPanelId = panelId ?? _activePanelId;
            return !string.IsNullOrEmpty(targetPanelId) && _panelHistories.ContainsKey(targetPanelId)
                ? _panelHistories[targetPanelId].Current.DirectoryPathName
                : null;
        }

        public IReadOnlyList<string> GetRegisteredPanels()
        {
            return _panelHistories.Keys.ToList();
        }

        public void ClearHistory(string panelId = null)
        {
            var targetPanelId = panelId ?? _activePanelId;
            if (string.IsNullOrEmpty(targetPanelId) || !_panelHistories.ContainsKey(targetPanelId))
                return;

            // Сохраняем текущий путь перед очисткой
            var currentPath = _panelHistories[targetPanelId].Current.DirectoryPath;
            var currentName = _panelHistories[targetPanelId].Current.DirectoryPathName;

            // Создаем новую историю с текущим путем
            _panelHistories[targetPanelId].Dispose();
            _panelHistories[targetPanelId] = new DirectoryHistory(currentPath, currentName);
            _panelHistories[targetPanelId].HistoryChanged += (s, e) => OnHistoryChanged(targetPanelId);

            Debug.WriteLine($"NavigationManager.ClearHistory: история очищена для панели {targetPanelId}");
        }

        private string GetDisplayName(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "Неизвестный путь";

            // Специальные пути
            if (path == "MyComputer")
                return "Мой Компьютер";

            if (path == "SpecialFolders")
                return "Специальные папки";

            // Диски (C:\, D:\ и т.д.)
            if (path.Length == 3 && path.EndsWith(":\\") && char.IsLetter(path[0]))
                return path;

            // Папки и файлы
            try
            {
                if (Directory.Exists(path))
                {
                    var dirInfo = new DirectoryInfo(path);
                    return dirInfo.Name;
                }
                else if (File.Exists(path))
                {
                    var fileInfo = new FileInfo(path);
                    return fileInfo.Name;
                }
                else
                {
                    return Path.GetFileName(path) ?? path;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"NavigationManager.GetDisplayName: ошибка получения имени для {path}: {ex.Message}");
                return Path.GetFileName(path) ?? path;
            }
        }

        private void OnHistoryChanged(string panelId)
        {
            if (!_panelHistories.ContainsKey(panelId))
                return;

            var currentPath = _panelHistories[panelId].Current.DirectoryPath;
            OnNavigationChanged(panelId, currentPath, NavigationAction.Navigate);
        }

        private void OnNavigationChanged(string panelId, string path, NavigationAction action)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    Debug.WriteLine($"NavigationManager.OnNavigationChanged: Panel={panelId}, Path={path}, Action={action}");

                    NavigationChanged?.Invoke(this, new NavigationEventArgs
                    {
                        PanelId = panelId,
                        Path = path,
                        Action = action
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"NavigationManager.OnNavigationChanged: ошибка вызова события: {ex.Message}");
                }
            });
        }

        #region IDisposable Support

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Освобождаем управляемые ресурсы
                    foreach (var history in _panelHistories.Values)
                    {
                        history?.Dispose();
                    }
                    _panelHistories.Clear();
                    NavigationChanged = null;
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~NavigationManager()
        {
            Dispose(false);
        }

        #endregion
    }
}