using Core_FileManagement;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace ufm
{
    public class FileSystemService : IDisposable
    {
        private static readonly Core_FileManagement.IIconService _iconService = new Core_FileManagement.IconService();
        private CancellationTokenSource _currentOperationCts;
        private readonly Dictionary<string, List<ExplorerItemViewModel>> _panelCaches = new();
        private Task _breadcrumbCacheTask;

        private static readonly SemaphoreSlim _iconLoadSemaphore = new(8, 8);
        private readonly DispatcherQueue _dispatcher;

        // Фоновые задачи иконок для ожидания при завершении
        private readonly ConcurrentBag<Task> _activeIconTasks = new();
        // Общий токен отмены для всех фоновых операций (отменяется при Dispose)
        private readonly CancellationTokenSource _disposeCts = new();

        public bool IsDisposed { get; private set; }

        public bool ShowNavigationBackItem
        {
            get
            {
                try { return App.SettingsManager?.GetSetting<bool>("ShowNavigationBackItem", true) ?? true; }
                catch { return true; }
            }
        }

        public FileSystemService()
        {
            _currentOperationCts = new CancellationTokenSource();
            FileCacheService.Initialize(_iconService);
            _dispatcher = DispatcherQueue.GetForCurrentThread()
                          ?? throw new InvalidOperationException("FileSystemService must be created on UI thread");

            _breadcrumbCacheTask = Task.Run(async () =>
            {
                try
                {
                    var history = new DirectoryHistory("SpecialFolders", "Специальные папки");
                    await LoadHomeAsync("Breadcrumb", history);
                }
                catch { }
            });
        }

        // ========================= ПУБЛИЧНЫЕ МЕТОДЫ =========================

        public async Task<List<ExplorerItemViewModel>> LoadPathContentsAsync(string path, string panelId, IDirectoryHistory history = null)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(FileSystemService));
            CancelCurrentOperation();
            var token = CancellationTokenSource.CreateLinkedTokenSource(_currentOperationCts.Token, _disposeCts.Token).Token;
            try
            {
                var localHistory = history ?? new DirectoryHistory("MyComputer", "Мой Компьютер");
                switch (path)
                {
                    case "MyComputer":
                        return await LoadMyComputerAsync(panelId, localHistory, token);
                    case "Drives":
                        return await LoadDrivesAsync(localHistory, token);
                    default:
                        if (Directory.Exists(path))
                            return await LoadFolderContentsAsync(path, localHistory, token);
                        else
                            return await LoadMyComputerAsync(panelId, localHistory, token);
                }
            }
            catch (OperationCanceledException)
            {
                return new List<ExplorerItemViewModel>();
            }
        }

        public async Task<List<ExplorerItemViewModel>> LoadMyComputerAsync(string panelId, IDirectoryHistory history, CancellationToken token = default)
        {
            if (_panelCaches.TryGetValue(panelId, out var cached) && !token.IsCancellationRequested)
                return new List<ExplorerItemViewModel>(cached);
            var items = await InitializeMyComputerCacheAsync(panelId, history, token);
            return new List<ExplorerItemViewModel>(items);
        }

        public async Task<List<ExplorerItemViewModel>> LoadDrivesAsync(IDirectoryHistory history, CancellationToken token = default)
        {
            var items = new List<ExplorerItemViewModel>();
            if (ShowNavigationBackItem) items.Add(CreateBackItem(history));
            foreach (var drive in Directory.GetLogicalDrives())
            {
                if (token.IsCancellationRequested) break;
                try
                {
                    var driveItem = await CreateDriveItemAsync(drive, history);
                    if (driveItem != null) items.Add(driveItem);
                }
                catch
                {
                    items.Add(new ExplorerItemViewModel(history)
                    {
                        Name = drive,
                        FilePath = drive,
                        ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/harddisk.png"))
                    });
                }
            }
            return items;
        }

        // ===================== ОПТИМИЗИРОВАННАЯ ЗАГРУЗКА ПАПОК =====================
        public async Task<List<ExplorerItemViewModel>> LoadFolderContentsAsync(string folderPath, IDirectoryHistory history, CancellationToken token = default)
        {
            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"Directory not found: {folderPath}");

            var items = new List<ExplorerItemViewModel>();

            if (ShowNavigationBackItem)
                items.Add(CreateBackItem(history));

            // 1. Собираем пути папок и файлов в фоне (без UI-объектов)
            List<string> folderPaths = new();
            List<string> filePaths = new();
            await CollectFolderAndFilePathsAsync(folderPath, folderPaths, filePaths, token);

            // 2. Создаём ViewModel в UI-потоке: сначала папки, потом файлы, по алфавиту
            var comparer = StringComparer.OrdinalIgnoreCase;
            foreach (var folderPathItem in folderPaths.OrderBy(p => Path.GetFileName(p), comparer))
            {
                items.Add(new ExplorerItemViewModel(history)
                {
                    Name = Path.GetFileName(folderPathItem),
                    FilePath = folderPathItem,
                    IsTreeViewNode = true,
                    ImageSource = null
                });
            }
            foreach (var filePathItem in filePaths.OrderBy(p => Path.GetFileName(p), comparer))
            {
                items.Add(new ExplorerItemViewModel(history)
                {
                    Name = Path.GetFileName(filePathItem),
                    FilePath = filePathItem,
                    IsTreeViewNode = true,
                    ImageSource = null
                });
            }

            // 3. Запускаем фоновую загрузку иконок (не ждём)
            var iconTask = LoadIconsInBackgroundAsync(items, token);
            _activeIconTasks.Add(iconTask);
            // Удаляем задачу из коллекции по завершении
            _ = iconTask.ContinueWith(_ => _activeIconTasks.TryTake(out _), TaskScheduler.Default);

            return items;
        }

        private Task CollectFolderAndFilePathsAsync(string folderPath, List<string> folderPaths, List<string> filePaths, CancellationToken token)
        {
            return Task.Run(() =>
            {
                var dirInfo = new DirectoryInfo(folderPath);
                if (!dirInfo.Exists) return;

                foreach (var fsi in dirInfo.EnumerateFileSystemInfos())
                {
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        if (fsi is DirectoryInfo)
                            folderPaths.Add(fsi.FullName);
                        else
                            filePaths.Add(fsi.FullName);
                    }
                    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                    {
                        // Пропускаем недоступные элементы
                    }
                }
            }, token);
        }

        private async Task LoadIconsInBackgroundAsync(List<ExplorerItemViewModel> items, CancellationToken token)
        {
            var tasks = items
                .Where(it => it.FilePath != "..")
                .Select(it => LoadIconForItemAsync(it, token))
                .ToArray();
            if (tasks.Length == 0) return;
            try
            {
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException) { }
            catch { }
        }

        private async Task LoadIconForItemAsync(ExplorerItemViewModel item, CancellationToken token)
        {
            if (IsDisposed || token.IsCancellationRequested)
                return;

            await _iconLoadSemaphore.WaitAsync(token);
            try
            {
                if (IsDisposed || token.IsCancellationRequested)
                    return;

                BitmapImage icon = null;
                string path = item.FilePath;

                if (path.Length == 3 && path.EndsWith(":\\") && char.IsLetter(path[0]))
                    icon = await _iconService.GetIconAsync(path, true);
                else if (Directory.Exists(path))
                    icon = await _iconService.GetIconAsync(path, true);
                else if (File.Exists(path))
                    icon = await FileCacheService.GetFileIconAsync(path);

                if (icon != null && !IsDisposed && !token.IsCancellationRequested)
                {
                    _dispatcher.TryEnqueue(() =>
                    {
                        if (!IsDisposed)
                            item.ImageSource = icon;
                    });
                }
            }
            catch (OperationCanceledException) { }
            catch { }
            finally
            {
                _iconLoadSemaphore.Release();
            }
        }

        // ===================== ОСТАЛЬНЫЕ МЕТОДЫ БЕЗ ИЗМЕНЕНИЙ =====================

        public async Task<List<ExplorerItemViewModel>> LoadDrivesTree(IDirectoryHistory history, CancellationToken token = default)
        {
            var items = new List<ExplorerItemViewModel>();
            foreach (var logicalDrive in Directory.GetLogicalDrives())
            {
                if (token.IsCancellationRequested) break;
                try
                {
                    var driveInfo = new DriveInfo(logicalDrive);
                    var icon = await _iconService.GetIconAsync(logicalDrive, true).ConfigureAwait(false);
                    var driveViewModel = new DriveViewModel(driveInfo, EntityFlags.IsDrive);
                    items.Add(new ExplorerItemViewModel(history)
                    {
                        IsProgressBarVisible = true,
                        Name = GetDriveDisplayName(driveInfo, logicalDrive),
                        FilePath = logicalDrive,
                        ImageSource = icon,
                        UsedSpaceString = driveViewModel.UsedSpaceString,
                        FreeSpaceString = driveViewModel.FreeSpaceString,
                        TotalSizeString = driveViewModel.TotalSizeString,
                        UsedProcentValue = driveViewModel.UsedProcentValue,
                        IsTreeViewNode = true
                    });
                }
                catch
                {
                    items.Add(new ExplorerItemViewModel(history)
                    {
                        Name = logicalDrive,
                        FilePath = logicalDrive,
                        ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/harddisk.png")),
                        IsTreeViewNode = true
                    });
                }
            }
            return items;
        }

        public async Task<List<ExplorerItemViewModel>> LoadSubfoldersForTreeViewAsync(string folderPath, IDirectoryHistory history, CancellationToken token = default)
        {
            var items = new List<ExplorerItemViewModel>();
            try
            {
                var subfolders = Directory.GetDirectories(folderPath);
                var defaultIcon = new BitmapImage(new Uri("ms-appx:///Assets/folder1.png"));
                var throttler = new SemaphoreSlim(10);
                var tasks = subfolders.Select(async subfolder =>
                {
                    await throttler.WaitAsync();
                    try
                    {
                        var icon = await _iconService.GetIconAsync(subfolder, true);
                        return new ExplorerItemViewModel(history)
                        {
                            IsProgressBarVisible = false,
                            Name = Path.GetFileName(subfolder),
                            FilePath = subfolder,
                            ImageSource = icon ?? defaultIcon,
                            IsTreeViewNode = true
                        };
                    }
                    finally { throttler.Release(); }
                });
                var results = await Task.WhenAll(tasks);
                items.AddRange(results);
            }
            catch { }
            return items;
        }

        public async Task<List<ExplorerItemViewModel>> LoadFoldersOnlyAsync(string folderPath, IDirectoryHistory history, CancellationToken token = default)
        {
            var items = new List<ExplorerItemViewModel>();
            try
            {
                var folders = Directory.GetDirectories(folderPath);
                var defaultIcon = new BitmapImage(new Uri("ms-appx:///Assets/folder1.png"));
                foreach (var folder in folders)
                {
                    if (token.IsCancellationRequested) break;
                    try
                    {
                        var dirInfo = new DirectoryInfo(folder);
                        var icon = await _iconService.GetIconAsync(folder, true) ?? defaultIcon;
                        items.Add(new ExplorerItemViewModel(history)
                        {
                            IsProgressBarVisible = false,
                            Name = dirInfo.Name,
                            FilePath = folder,
                            ImageSource = icon,
                            IsTreeViewNode = true
                        });
                    }
                    catch { }
                }
            }
            catch { }
            return items;
        }

        private async Task<List<ExplorerItemViewModel>> InitializeMyComputerCacheAsync(string panelId, IDirectoryHistory history, CancellationToken token)
        {
            var items = new List<ExplorerItemViewModel>
            {
                new ExplorerItemViewModel(history)
                {
                    Name = "Мой Компьютер", FilePath = "Drives",
                    ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/computer.png")),
                    IsTreeViewNode = true
                }
            };
            await LoadSystemFoldersAsync(items, history, token);
            _panelCaches[panelId] = items;
            return items;
        }

        public async Task<List<ExplorerItemViewModel>> LoadHomeAsync(string panelId, IDirectoryHistory history, CancellationToken token = default)
        {
            if (_panelCaches.TryGetValue(panelId, out var cached) && !token.IsCancellationRequested)
                return new List<ExplorerItemViewModel>(cached);
            var items = await InitializeHomeCacheAsync(panelId, history, token);
            return new List<ExplorerItemViewModel>(items);
        }

        private async Task<List<ExplorerItemViewModel>> InitializeHomeCacheAsync(string panelId, IDirectoryHistory history, CancellationToken token)
        {
            var items = new List<ExplorerItemViewModel>();
            await LoadSystemFoldersAsync(items, history, token);
            _panelCaches[panelId] = items;
            return items;
        }

        public async Task LoadSystemFoldersAsync(List<ExplorerItemViewModel> items, IDirectoryHistory history, CancellationToken token)
        {
            var systemFolders = new[]
            {
                new { Name = "Рабочий стол", Path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) },
                new { Name = "Документы", Path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) },
                new { Name = "Изображения", Path = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) },
                new { Name = "Музыка", Path = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic) },
                new { Name = "Видео", Path = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos) },
                new { Name = "Загрузки", Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads") }
            };
            foreach (var folder in systemFolders)
            {
                if (token.IsCancellationRequested) break;
                try
                {
                    if (!Directory.Exists(folder.Path)) continue;
                    var icon = await _iconService.GetSpecialFolderIconAsync(folder.Path);
                    items.Add(new ExplorerItemViewModel(history)
                    {
                        Name = folder.Name,
                        FilePath = folder.Path,
                        ImageSource = icon ?? new BitmapImage(new Uri("ms-appx:///Assets/folder1.png")),
                        IsTreeViewNode = true,
                        IsSpecialFolderNode = true
                    });
                }
                catch
                {
                    items.Add(new ExplorerItemViewModel(history)
                    {
                        Name = folder.Name,
                        FilePath = folder.Path,
                        ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/folder1.png")),
                        IsTreeViewNode = true,
                        IsSpecialFolderNode = true
                    });
                }
                await Task.Delay(1, token);
            }
        }

        public ExplorerItemViewModel CreateSpecialFoldersItem(IDirectoryHistory history)
        {
            return new ExplorerItemViewModel(history)
            {
                Name = "Специальные папки",
                FilePath = "SpecialFolders",
                ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/home.png")),
                IsTreeViewNode = true,
                IsSpecialFolderNode = true
            };
        }

        public async Task<List<ExplorerItemViewModel>> LoadHomePageAsync(CancellationToken token = default)
        {
            var history = new DirectoryHistory("MyComputer", "Домашняя страница");
            var items = new List<ExplorerItemViewModel>
            {
                new ExplorerItemViewModel(history)
                {
                    Name = "Мой Компьютер", FilePath = "Drives",
                    ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/computer.png")),
                    IsTreeViewNode = true
                }
            };
            await LoadSystemFoldersAsync(items, history, token);
            return items;
        }

        private async Task<ExplorerItemViewModel> CreateDriveItemAsync(string drivePath, IDirectoryHistory history)
        {
            var driveInfo = new DriveInfo(drivePath);
            var driveViewModel = new DriveViewModel(driveInfo, EntityFlags.IsDrive);
            BitmapImage icon;
            try { icon = await _iconService.GetIconAsync(drivePath, true); }
            catch { icon = new BitmapImage(new Uri("ms-appx:///Assets/harddisk.png")); }
            return new ExplorerItemViewModel(history)
            {
                IsProgressBarVisible = true,
                Name = GetDriveDisplayName(driveInfo, drivePath),
                FilePath = drivePath,
                ImageSource = icon,
                UsedSpaceString = driveViewModel.UsedSpaceString,
                FreeSpaceString = driveViewModel.FreeSpaceString,
                TotalSizeString = driveViewModel.TotalSizeString,
                UsedProcentValue = driveViewModel.UsedProcentValue,
                IsTreeViewNode = true
            };
        }

        private ExplorerItemViewModel CreateBackItem(IDirectoryHistory history)
        {
            return new ExplorerItemViewModel(history)
            {
                Name = "..",
                FilePath = "..",
                ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/ahead-only.png")),
                IsTreeViewNode = true
            };
        }

        private string GetDriveDisplayName(DriveInfo driveInfo, string drivePath)
        {
            string letter = drivePath.TrimEnd('\\');
            return string.IsNullOrWhiteSpace(driveInfo.VolumeLabel) ? letter : $"{driveInfo.VolumeLabel} ({letter})";
        }

        private void CancelCurrentOperation()
        {
            _currentOperationCts?.Cancel();
            _currentOperationCts?.Dispose();
            _currentOperationCts = new CancellationTokenSource();
        }

        public void CancelAllOperations() => CancelCurrentOperation();

        public void ClearPanelCache(string panelId)
        {
            if (_panelCaches.TryGetValue(panelId, out var cache))
            {
                foreach (var item in cache) item?.Dispose();
                _panelCaches.Remove(panelId);
            }
        }

        public void ClearAllCaches()
        {
            foreach (var (_, cache) in _panelCaches)
                foreach (var item in cache) item?.Dispose();
            _panelCaches.Clear();
        }

        public void RefreshNavigationSettings()
        {
            try
            {
                bool show = ShowNavigationBackItem;
                ClearAllCaches();
                NavigationSettingsMediator.NotifySettingsChanged(show);
            }
            catch { }
        }

        public bool IsNavigationPath(string path)
        {
            return path == ".." || path == "MyComputer" || path == "Drives" || Directory.Exists(path);
        }

        public string GetDisplayName(string path)
        {
            if (string.IsNullOrEmpty(path)) return "Неизвестный путь";
            if (path == "MyComputer") return "Домашняя страница";
            if (path == "SpecialFolders") return "Специальные папки";
            if (path == "Drives") return "Мой компьютер";
            if (path.Length == 3 && path.EndsWith(":\\") && char.IsLetter(path[0]))
            {
                try { return GetDriveDisplayName(new DriveInfo(path), path); }
                catch { return path; }
            }

            if (_breadcrumbCacheTask?.IsCompletedSuccessfully == true &&
                _panelCaches.TryGetValue("Breadcrumb", out var cached))
            {
                var found = cached.FirstOrDefault(it => string.Equals(it.FilePath, path, StringComparison.OrdinalIgnoreCase));
                if (found != null) return found.Name;
            }

            string special = GetSpecialFolderDisplayNameSync(path);
            if (!string.IsNullOrEmpty(special)) return special;

            if (Directory.Exists(path) || File.Exists(path)) return Path.GetFileName(path);
            try { return Path.GetFileName(path); } catch { return path; }
        }

        private string GetSpecialFolderDisplayNameSync(string path)
        {
            try
            {
                string normalized = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var dict = new Dictionary<string, string>
                {
                    { Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)).TrimEnd(Path.DirectorySeparatorChar), "Рабочий стол" },
                    { Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)).TrimEnd(Path.DirectorySeparatorChar), "Документы" },
                    { Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)).TrimEnd(Path.DirectorySeparatorChar), "Изображения" },
                    { Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)).TrimEnd(Path.DirectorySeparatorChar), "Музыка" },
                    { Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)).TrimEnd(Path.DirectorySeparatorChar), "Видео" },
                    { Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")).TrimEnd(Path.DirectorySeparatorChar), "Загрузки" }
                };
                return dict.TryGetValue(normalized, out var name) ? name : null;
            }
            catch { return null; }
        }

        public async Task<BitmapImage> GetDriveIconAsync(string drivePath) => await _iconService.GetIconAsync(drivePath, true);
        public async Task<BitmapImage> GetFolderIconAsync(string folderPath) => await _iconService.GetIconAsync(folderPath, true);
        public async Task<BitmapImage> GetFileIconAsync(string filePath) => await FileCacheService.GetFileIconAsync(filePath);

        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;

            // Отменяем все фоновые операции
            _disposeCts.Cancel();
            _currentOperationCts?.Cancel();

            // Ожидаем завершения всех фоновых задач иконок (с таймаутом)
            var tasks = _activeIconTasks.ToArray();
            if (tasks.Length > 0)
            {
                try
                {
                    Task.WaitAll(tasks, TimeSpan.FromSeconds(3));
                }
                catch (AggregateException) { }
            }

            ClearAllCaches();
            _currentOperationCts?.Dispose();
            _disposeCts?.Dispose();
        }
    }
} 