//using Core_FileManagement;
//using Microsoft.UI.Xaml.Media.Imaging;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using Windows.Storage;

//namespace ufm
//{
//    public class FileSystemService : IDisposable
//    {
//        #region Поля и свойства

//        private static readonly Core_FileManagement.IIconService _iconService =
//            new Core_FileManagement.IconService();

//        private CancellationTokenSource _currentOperationCts;
//        private readonly Dictionary<string, List<ExplorerItemViewModel>> _panelCaches = new();

//        private Task _breadcrumbCacheTask; // задача для предзагрузки кэша для хлебных крошек

//        public bool IsDisposed { get; private set; }

//        public bool ShowNavigationBackItem
//        {
//            get
//            {
//                try
//                {
//                    return App.SettingsManager?.GetSetting<bool>("ShowNavigationBackItem", true) ?? true;
//                }
//                catch
//                {
//                    return true;
//                }
//            }
//        }

//        #endregion

//        #region Конструктор

//        public FileSystemService()
//        {
//            _currentOperationCts = new CancellationTokenSource();
//            FileCacheService.Initialize(_iconService);

//            // Запускаем фоновую загрузку кэша для панели "Breadcrumb"
//            _breadcrumbCacheTask = Task.Run(async () =>
//            {
//                try
//                {
//                    // Используем существующий метод LoadHomeAsync с фиктивной историей
//                    var history = new DirectoryHistory("SpecialFolders", "Специальные папки");
//                    await LoadHomeAsync("Breadcrumb", history);
//                }
//                catch
//                {
//                    // Игнорируем ошибки инициализации
//                }
//            });
//        }

//        #endregion

//        #region Основные методы для TileViewIcons

//        public async Task<List<ExplorerItemViewModel>> LoadPathContentsAsync(string path, string panelId = "DefaultPanel", IDirectoryHistory history = null)
//        {
//            if (IsDisposed) throw new ObjectDisposedException(nameof(FileSystemService));

//            CancelCurrentOperation();
//            var token = _currentOperationCts.Token;

//            try
//            {
//                var localHistory = history ?? new DirectoryHistory("MyComputer", "Мой Компьютер");

//                switch (path)
//                {
//                    case "MyComputer":
//                        return await LoadMyComputerAsync(panelId, localHistory, token);

//                    case "Drives":
//                        return await LoadDrivesAsync(localHistory, token);

//                    default:
//                        if (Directory.Exists(path))
//                            return await LoadFolderContentsAsync(path, localHistory, token);
//                        else
//                            return await LoadMyComputerAsync(panelId, localHistory, token);
//                }
//            }
//            catch (OperationCanceledException)
//            {
//                return new List<ExplorerItemViewModel>();
//            }
//        }

//        public async Task<List<ExplorerItemViewModel>> LoadMyComputerAsync(string panelId, IDirectoryHistory history, CancellationToken token = default)
//        {
//            if (_panelCaches.TryGetValue(panelId, out var cachedItems) && !token.IsCancellationRequested)
//            {
//                return new List<ExplorerItemViewModel>(cachedItems);
//            }

//            var items = await InitializeMyComputerCacheAsync(panelId, history, token);
//            return new List<ExplorerItemViewModel>(items);
//        }

//        public async Task<List<ExplorerItemViewModel>> LoadDrivesAsync(IDirectoryHistory history, CancellationToken token = default)
//        {
//            var items = new List<ExplorerItemViewModel>();

//            if (ShowNavigationBackItem)
//            {
//                items.Add(CreateBackItem(history));
//            }

//            foreach (var logicalDrive in Directory.GetLogicalDrives())
//            {
//                if (token.IsCancellationRequested) break;

//                try
//                {
//                    var driveItem = await CreateDriveItemAsync(logicalDrive, history);
//                    if (driveItem != null)
//                        items.Add(driveItem);
//                }
//                catch
//                {
//                    items.Add(new ExplorerItemViewModel(history)
//                    {
//                        Name = logicalDrive,
//                        FilePath = logicalDrive,
//                        ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/harddisk.png"))
//                    });
//                }
//            }

//            return items;
//        }

//        public async Task<List<ExplorerItemViewModel>> LoadFolderContentsAsync(string folderPath, IDirectoryHistory history, CancellationToken token = default)
//        {
//            if (!Directory.Exists(folderPath))
//                throw new DirectoryNotFoundException($"Directory not found: {folderPath}");

//            var items = new List<ExplorerItemViewModel>();

//            if (ShowNavigationBackItem)
//            {
//                items.Add(CreateBackItem(history));
//            }

//            await LoadSubfoldersAsync(items, folderPath, history, token);
//            await LoadFilesAsync(items, folderPath, history, token);

//            return items;
//        }

//        #endregion

//        #region Методы для TreeView

//        public async Task<List<ExplorerItemViewModel>> LoadDrivesTree(IDirectoryHistory history, CancellationToken token = default)
//        {
//            var items = new List<ExplorerItemViewModel>();

//            foreach (var logicalDrive in Directory.GetLogicalDrives())
//            {
//                if (token.IsCancellationRequested) break;

//                try
//                {
//                    var driveInfo = new DriveInfo(logicalDrive);
//                    var icon = await _iconService.GetIconAsync(logicalDrive, true).ConfigureAwait(false);
//                    var driveViewModel = new DriveViewModel(driveInfo, EntityFlags.IsDrive);

//                    var fileSystemItem = new ExplorerItemViewModel(history)
//                    {
//                        IsProgressBarVisible = true,
//                        Name = GetDriveDisplayName(driveInfo, logicalDrive),
//                        FilePath = logicalDrive,
//                        ImageSource = icon,
//                        UsedSpaceString = driveViewModel.UsedSpaceString,
//                        FreeSpaceString = driveViewModel.FreeSpaceString,
//                        TotalSizeString = driveViewModel.TotalSizeString,
//                        UsedProcentValue = driveViewModel.UsedProcentValue,
//                        IsTreeViewNode = true
//                    };

//                    items.Add(fileSystemItem);
//                }
//                catch
//                {
//                    items.Add(new ExplorerItemViewModel(history)
//                    {
//                        Name = logicalDrive,
//                        FilePath = logicalDrive,
//                        ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/harddisk.png")),
//                        IsTreeViewNode = true
//                    });
//                }
//            }

//            return items;
//        }

//        public async Task<List<ExplorerItemViewModel>> LoadSubfoldersForTreeViewAsync(string folderPath, IDirectoryHistory history, CancellationToken token = default)
//        {
//            var items = new List<ExplorerItemViewModel>();

//            try
//            {
//                var subfolders = Directory.GetDirectories(folderPath);
//                var defaultIcon = new BitmapImage(new Uri("ms-appx:///Assets/folder1.png"));

//                var throttler = new SemaphoreSlim(initialCount: 10);

//                var tasks = subfolders.Select(async subfolder =>
//                {
//                    await throttler.WaitAsync();
//                    try
//                    {
//                        var icon = await _iconService.GetIconAsync(subfolder, true);
//                        return new ExplorerItemViewModel(history)
//                        {
//                            IsProgressBarVisible = false,
//                            Name = Path.GetFileName(subfolder),
//                            FilePath = subfolder,
//                            ImageSource = icon ?? defaultIcon,
//                            IsTreeViewNode = true
//                        };
//                    }
//                    finally
//                    {
//                        throttler.Release();
//                    }
//                });

//                var results = await Task.WhenAll(tasks);
//                items.AddRange(results);
//            }
//            catch
//            {
//            }

//            return items;
//        }

//        public async Task<List<ExplorerItemViewModel>> LoadFoldersOnlyAsync(string folderPath, IDirectoryHistory history, CancellationToken token = default)
//        {
//            var items = new List<ExplorerItemViewModel>();

//            try
//            {
//                var folders = Directory.GetDirectories(folderPath);
//                var defaultIcon = new BitmapImage(new Uri("ms-appx:///Assets/folder1.png"));

//                foreach (var folder in folders)
//                {
//                    if (token.IsCancellationRequested) break;

//                    try
//                    {
//                        var dirInfo = new DirectoryInfo(folder);
//                        var icon = await _iconService.GetIconAsync(folder, true) ?? defaultIcon;

//                        items.Add(new ExplorerItemViewModel(history)
//                        {
//                            IsProgressBarVisible = false,
//                            Name = dirInfo.Name,
//                            FilePath = folder,
//                            ImageSource = icon,
//                            IsTreeViewNode = true
//                        });
//                    }
//                    catch
//                    {
//                    }
//                }
//            }
//            catch
//            {
//            }

//            return items;
//        }

//        #endregion

//        #region Приватные методы загрузки данных

//        private async Task<List<ExplorerItemViewModel>> InitializeMyComputerCacheAsync(string panelId, IDirectoryHistory history, CancellationToken token)
//        {
//            var items = new List<ExplorerItemViewModel>();

//            items.Add(new ExplorerItemViewModel(history)
//            {
//                Name = "Мой Компьютер",
//                FilePath = "Drives",
//                ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/computer.png")),
//                IsTreeViewNode = true
//            });

//            await LoadSystemFoldersAsync(items, history, token);
//            _panelCaches[panelId] = items;

//            return items;
//        }

//        public async Task<List<ExplorerItemViewModel>> LoadHomeAsync(string panelId, IDirectoryHistory history, CancellationToken token = default)
//        {
//            if (_panelCaches.TryGetValue(panelId, out var cachedItems) && !token.IsCancellationRequested)
//            {
//                return new List<ExplorerItemViewModel>(cachedItems);
//            }

//            var items = await InitializeHomeCacheAsync(panelId, history, token);
//            return new List<ExplorerItemViewModel>(items);
//        }

//        private async Task<List<ExplorerItemViewModel>> InitializeHomeCacheAsync(string panelId, IDirectoryHistory history, CancellationToken token)
//        {
//            var items = new List<ExplorerItemViewModel>();
//            await LoadSystemFoldersAsync(items, history, token);
//            _panelCaches[panelId] = items;
//            return items;
//        }

//        public async Task LoadSystemFoldersAsync(List<ExplorerItemViewModel> items, IDirectoryHistory history, CancellationToken token)
//        {
//            var systemFolders = new[]
//            {
//                new { Name = "Рабочий стол", Path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) },
//                new { Name = "Документы", Path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) },
//                new { Name = "Изображения", Path = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) },
//                new { Name = "Музыка", Path = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic) },
//                new { Name = "Видео", Path = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos) },
//                new { Name = "Загрузки", Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads") }
//            };

//            foreach (var folder in systemFolders)
//            {
//                if (token.IsCancellationRequested) break;

//                try
//                {
//                    if (!Directory.Exists(folder.Path)) continue;

//                    var icon = await _iconService.GetSpecialFolderIconAsync(folder.Path);

//                    items.Add(new ExplorerItemViewModel(history)
//                    {
//                        Name = folder.Name,
//                        FilePath = folder.Path,
//                        ImageSource = icon ?? new BitmapImage(new Uri("ms-appx:///Assets/folder1.png")),
//                        IsTreeViewNode = true,
//                        IsSpecialFolderNode = true
//                    });
//                }
//                catch
//                {
//                    items.Add(new ExplorerItemViewModel(history)
//                    {
//                        Name = folder.Name,
//                        FilePath = folder.Path,
//                        ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/folder1.png")),
//                        IsTreeViewNode = true,
//                        IsSpecialFolderNode = true
//                    });
//                }

//                await Task.Delay(1, token);
//            }
//        }

//        public ExplorerItemViewModel CreateSpecialFoldersItem(IDirectoryHistory history)
//        {
//            return new ExplorerItemViewModel(history)
//            {
//                Name = "Специальные папки",
//                FilePath = "SpecialFolders",
//                ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/home.png")),
//                IsTreeViewNode = true,
//                IsSpecialFolderNode = true
//            };
//        }

//        private async Task LoadSubfoldersAsync(List<ExplorerItemViewModel> items, string folderPath, IDirectoryHistory history, CancellationToken token)
//        {
//            var folders = Directory.GetDirectories(folderPath);
//            var defaultFolderIcon = new BitmapImage(new Uri("ms-appx:///Assets/folder1.png"));

//            foreach (var folder in folders)
//            {
//                if (token.IsCancellationRequested) break;

//                try
//                {
//                    var dirInfo = new DirectoryInfo(folder);
//                    var icon = await _iconService.GetIconAsync(folder, true) ?? defaultFolderIcon;

//                    items.Add(new ExplorerItemViewModel(history)
//                    {
//                        Name = dirInfo.Name,
//                        FilePath = folder,
//                        ImageSource = icon,
//                        IsTreeViewNode = true
//                    });
//                }
//                catch
//                {
//                }
//            }
//        }

//        private async Task LoadFilesAsync(List<ExplorerItemViewModel> items, string folderPath, IDirectoryHistory history, CancellationToken token)
//        {
//            var files = Directory.GetFiles(folderPath);
//            var defaultFileIcon = new BitmapImage(new Uri("ms-appx:///Assets/file.png"));

//            foreach (var file in files)
//            {
//                if (token.IsCancellationRequested) break;

//                try
//                {
//                    var fileInfo = new FileInfo(file);
//                    var icon = await FileCacheService.GetFileIconAsync(file) ?? defaultFileIcon;

//                    items.Add(new ExplorerItemViewModel(history)
//                    {
//                        Name = fileInfo.Name,
//                        FilePath = file,
//                        ImageSource = icon,
//                        IsTreeViewNode = true
//                    });
//                }
//                catch
//                {
//                }
//            }
//        }

//        private async Task<ExplorerItemViewModel> CreateDriveItemAsync(string drivePath, IDirectoryHistory history)
//        {
//            var driveInfo = new DriveInfo(drivePath);
//            var driveViewModel = new DriveViewModel(driveInfo, EntityFlags.IsDrive);

//            BitmapImage icon;
//            try
//            {
//                icon = await _iconService.GetIconAsync(drivePath, true);
//            }
//            catch
//            {
//                icon = new BitmapImage(new Uri("ms-appx:///Assets/harddisk.png"));
//            }

//            return new ExplorerItemViewModel(history)
//            {
//                IsProgressBarVisible = true,
//                Name = GetDriveDisplayName(driveInfo, drivePath),
//                FilePath = drivePath,
//                ImageSource = icon,
//                UsedSpaceString = driveViewModel.UsedSpaceString,
//                FreeSpaceString = driveViewModel.FreeSpaceString,
//                TotalSizeString = driveViewModel.TotalSizeString,
//                UsedProcentValue = driveViewModel.UsedProcentValue,
//                IsTreeViewNode = true
//            };
//        }

//        private ExplorerItemViewModel CreateBackItem(IDirectoryHistory history)
//        {
//            return new ExplorerItemViewModel(history)
//            {
//                Name = "..",
//                FilePath = "..",
//                ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/ahead-only.png")),
//                IsTreeViewNode = true
//            };
//        }

//        private string GetDriveDisplayName(DriveInfo driveInfo, string drivePath)
//        {
//            string driveLetter = drivePath.TrimEnd('\\');
//            if (string.IsNullOrWhiteSpace(driveInfo.VolumeLabel))
//                return driveLetter;
//            else
//                return $"{driveInfo.VolumeLabel} ({driveLetter})";
//        }

//        #endregion

//        #region Управление операциями и кэшем

//        private void CancelCurrentOperation()
//        {
//            _currentOperationCts?.Cancel();
//            _currentOperationCts?.Dispose();
//            _currentOperationCts = new CancellationTokenSource();
//        }

//        public void CancelAllOperations()
//        {
//            CancelCurrentOperation();
//        }

//        public void ClearPanelCache(string panelId)
//        {
//            if (_panelCaches.TryGetValue(panelId, out var cache))
//            {
//                foreach (var item in cache)
//                {
//                    item?.Dispose();
//                }
//                _panelCaches.Remove(panelId);
//            }
//        }

//        public void ClearAllCaches()
//        {
//            foreach (var (panelId, cache) in _panelCaches)
//            {
//                foreach (var item in cache)
//                {
//                    item?.Dispose();
//                }
//            }
//            _panelCaches.Clear();
//        }

//        public void RefreshNavigationSettings()
//        {
//            try
//            {
//                bool showBackNavigation = ShowNavigationBackItem;
//                ClearAllCaches();
//                NavigationSettingsMediator.NotifySettingsChanged(showBackNavigation);
//            }
//            catch
//            {
//            }
//        }

//        #endregion

//        #region Вспомогательные методы

//        public bool IsNavigationPath(string path)
//        {
//            return path == ".." ||
//                   path == "MyComputer" ||
//                   path == "Drives" ||
//                   Directory.Exists(path);
//        }

//        /// <summary>
//        /// Возвращает отображаемое имя для указанного пути, используя существующую логику класса и кэш.
//        /// </summary>
//        public string GetDisplayName(string path)
//        {
//            if (string.IsNullOrEmpty(path))
//                return "Неизвестный путь";

//            // Виртуальные пути
//            if (path == "MyComputer")
//                return "Мой Компьютер";
//            if (path == "SpecialFolders")
//                return "Специальные папки";
//            if (path == "Drives")
//                return "Диски";

//            // Диск (например, "C:\")
//            if (path.Length == 3 && path.EndsWith(":\\") && char.IsLetter(path[0]))
//            {
//                try
//                {
//                    var driveInfo = new DriveInfo(path);
//                    return GetDriveDisplayName(driveInfo, path);
//                }
//                catch
//                {
//                    return path;
//                }
//            }

//            // Если кэш для хлебных крошек уже загружен, ищем в нём
//            if (_breadcrumbCacheTask?.IsCompletedSuccessfully == true &&
//                _panelCaches.TryGetValue("Breadcrumb", out var cachedItems))
//            {
//                var found = cachedItems.FirstOrDefault(item =>
//                    string.Equals(item.FilePath, path, StringComparison.OrdinalIgnoreCase));
//                if (found != null)
//                    return found.Name;
//            }

//            // Fallback: синхронная проверка специальных папок (тот же словарь, что и в LoadSystemFoldersAsync)
//            string specialName = GetSpecialFolderDisplayNameSync(path);
//            if (!string.IsNullOrEmpty(specialName))
//                return specialName;

//            // Обычная папка или файл
//            if (Directory.Exists(path) || File.Exists(path))
//                return Path.GetFileName(path);

//            // Для несуществующих путей
//            try
//            {
//                return Path.GetFileName(path);
//            }
//            catch
//            {
//                return path;
//            }
//        }

//        /// <summary>
//        /// Синхронный fallback для получения имени специальной папки.
//        /// </summary>
//        private string GetSpecialFolderDisplayNameSync(string path)
//        {
//            try
//            {
//                string normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

//                // Словарь, соответствующий списку из LoadSystemFoldersAsync
//                var specialFolders = new Dictionary<string, string>
//                {
//                    { Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)).TrimEnd(Path.DirectorySeparatorChar), "Рабочий стол" },
//                    { Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)).TrimEnd(Path.DirectorySeparatorChar), "Документы" },
//                    { Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)).TrimEnd(Path.DirectorySeparatorChar), "Изображения" },
//                    { Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)).TrimEnd(Path.DirectorySeparatorChar), "Музыка" },
//                    { Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)).TrimEnd(Path.DirectorySeparatorChar), "Видео" },
//                    { Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")).TrimEnd(Path.DirectorySeparatorChar), "Загрузки" }
//                };

//                if (specialFolders.TryGetValue(normalizedPath, out string displayName))
//                    return displayName;
//            }
//            catch
//            {
//            }
//            return null;
//        }

//        #endregion

//        #region IDisposable

//        public void Dispose()
//        {
//            if (IsDisposed) return;

//            CancelCurrentOperation();
//            ClearAllCaches();
//            _currentOperationCts?.Dispose();

//            IsDisposed = true;
//        }

//        #endregion
//    }
//}



//using Core_FileManagement;
//using Microsoft.UI.Xaml.Media.Imaging;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using Windows.Storage;

//namespace ufm
//{
//    public class FileSystemService : IDisposable
//    {
//        private static readonly Core_FileManagement.IIconService _iconService =
//            new Core_FileManagement.IconService();

//        private CancellationTokenSource _currentOperationCts;
//        private readonly Dictionary<string, List<ExplorerItemViewModel>> _panelCaches = new();
//        private Task _breadcrumbCacheTask;

//        public bool IsDisposed { get; private set; }

//        public bool ShowNavigationBackItem
//        {
//            get
//            {
//                try
//                {
//                    return App.SettingsManager?.GetSetting<bool>("ShowNavigationBackItem", true) ?? true;
//                }
//                catch
//                {
//                    return true;
//                }
//            }
//        }

//        public FileSystemService()
//        {
//            _currentOperationCts = new CancellationTokenSource();
//            FileCacheService.Initialize(_iconService);

//            _breadcrumbCacheTask = Task.Run(async () =>
//            {
//                try
//                {
//                    var history = new DirectoryHistory("SpecialFolders", "Специальные папки");
//                    await LoadHomeAsync("Breadcrumb", history);
//                }
//                catch { }
//            });
//        }

//        public async Task<List<ExplorerItemViewModel>> LoadPathContentsAsync(string path, string panelId, IDirectoryHistory history = null)
//        {
//            if (IsDisposed) throw new ObjectDisposedException(nameof(FileSystemService));

//            CancelCurrentOperation();
//            var token = _currentOperationCts.Token;

//            try
//            {
//                var localHistory = history ?? new DirectoryHistory("MyComputer", "Мой Компьютер");

//                switch (path)
//                {
//                    case "MyComputer":
//                        return await LoadMyComputerAsync(panelId, localHistory, token);
//                    case "Drives":
//                        return await LoadDrivesAsync(localHistory, token);
//                    default:
//                        if (Directory.Exists(path))
//                            return await LoadFolderContentsAsync(path, localHistory, token);
//                        else
//                            return await LoadMyComputerAsync(panelId, localHistory, token);
//                }
//            }
//            catch (OperationCanceledException)
//            {
//                return new List<ExplorerItemViewModel>();
//            }
//        }

//        public async Task<List<ExplorerItemViewModel>> LoadMyComputerAsync(string panelId, IDirectoryHistory history, CancellationToken token = default)
//        {
//            if (_panelCaches.TryGetValue(panelId, out var cachedItems) && !token.IsCancellationRequested)
//                return new List<ExplorerItemViewModel>(cachedItems);

//            var items = await InitializeMyComputerCacheAsync(panelId, history, token);
//            return new List<ExplorerItemViewModel>(items);
//        }

//        public async Task<List<ExplorerItemViewModel>> LoadDrivesAsync(IDirectoryHistory history, CancellationToken token = default)
//        {
//            var items = new List<ExplorerItemViewModel>();

//            if (ShowNavigationBackItem)
//                items.Add(CreateBackItem(history));

//            foreach (var logicalDrive in Directory.GetLogicalDrives())
//            {
//                if (token.IsCancellationRequested) break;
//                try
//                {
//                    var driveItem = await CreateDriveItemAsync(logicalDrive, history);
//                    if (driveItem != null)
//                        items.Add(driveItem);
//                }
//                catch
//                {
//                    items.Add(new ExplorerItemViewModel(history)
//                    {
//                        Name = logicalDrive,
//                        FilePath = logicalDrive,
//                        ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/harddisk.png"))
//                    });
//                }
//            }

//            return items;
//        }

//        public async Task<List<ExplorerItemViewModel>> LoadFolderContentsAsync(string folderPath, IDirectoryHistory history, CancellationToken token = default)
//        {
//            if (!Directory.Exists(folderPath))
//                throw new DirectoryNotFoundException($"Directory not found: {folderPath}");

//            var items = new List<ExplorerItemViewModel>();

//            if (ShowNavigationBackItem)
//                items.Add(CreateBackItem(history));

//            await LoadSubfoldersAsync(items, folderPath, history, token);
//            await LoadFilesAsync(items, folderPath, history, token);

//            return items;
//        }

//        public async Task<List<ExplorerItemViewModel>> LoadDrivesTree(IDirectoryHistory history, CancellationToken token = default)
//        {
//            var items = new List<ExplorerItemViewModel>();

//            foreach (var logicalDrive in Directory.GetLogicalDrives())
//            {
//                if (token.IsCancellationRequested) break;
//                try
//                {
//                    var driveInfo = new DriveInfo(logicalDrive);
//                    var icon = await _iconService.GetIconAsync(logicalDrive, true).ConfigureAwait(false);
//                    var driveViewModel = new DriveViewModel(driveInfo, EntityFlags.IsDrive);

//                    var fileSystemItem = new ExplorerItemViewModel(history)
//                    {
//                        IsProgressBarVisible = true,
//                        Name = GetDriveDisplayName(driveInfo, logicalDrive),
//                        FilePath = logicalDrive,
//                        ImageSource = icon,
//                        UsedSpaceString = driveViewModel.UsedSpaceString,
//                        FreeSpaceString = driveViewModel.FreeSpaceString,
//                        TotalSizeString = driveViewModel.TotalSizeString,
//                        UsedProcentValue = driveViewModel.UsedProcentValue,
//                        IsTreeViewNode = true
//                    };
//                    items.Add(fileSystemItem);
//                }
//                catch
//                {
//                    items.Add(new ExplorerItemViewModel(history)
//                    {
//                        Name = logicalDrive,
//                        FilePath = logicalDrive,
//                        ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/harddisk.png")),
//                        IsTreeViewNode = true
//                    });
//                }
//            }

//            return items;
//        }

//        public async Task<List<ExplorerItemViewModel>> LoadSubfoldersForTreeViewAsync(string folderPath, IDirectoryHistory history, CancellationToken token = default)
//        {
//            var items = new List<ExplorerItemViewModel>();

//            try
//            {
//                var subfolders = Directory.GetDirectories(folderPath);
//                var defaultIcon = new BitmapImage(new Uri("ms-appx:///Assets/folder1.png"));
//                var throttler = new SemaphoreSlim(initialCount: 10);

//                var tasks = subfolders.Select(async subfolder =>
//                {
//                    await throttler.WaitAsync();
//                    try
//                    {
//                        var icon = await _iconService.GetIconAsync(subfolder, true);
//                        return new ExplorerItemViewModel(history)
//                        {
//                            IsProgressBarVisible = false,
//                            Name = Path.GetFileName(subfolder),
//                            FilePath = subfolder,
//                            ImageSource = icon ?? defaultIcon,
//                            IsTreeViewNode = true
//                        };
//                    }
//                    finally
//                    {
//                        throttler.Release();
//                    }
//                });

//                var results = await Task.WhenAll(tasks);
//                items.AddRange(results);
//            }
//            catch { }

//            return items;
//        }

//        public async Task<List<ExplorerItemViewModel>> LoadFoldersOnlyAsync(string folderPath, IDirectoryHistory history, CancellationToken token = default)
//        {
//            var items = new List<ExplorerItemViewModel>();

//            try
//            {
//                var folders = Directory.GetDirectories(folderPath);
//                var defaultIcon = new BitmapImage(new Uri("ms-appx:///Assets/folder1.png"));

//                foreach (var folder in folders)
//                {
//                    if (token.IsCancellationRequested) break;
//                    try
//                    {
//                        var dirInfo = new DirectoryInfo(folder);
//                        var icon = await _iconService.GetIconAsync(folder, true) ?? defaultIcon;

//                        items.Add(new ExplorerItemViewModel(history)
//                        {
//                            IsProgressBarVisible = false,
//                            Name = dirInfo.Name,
//                            FilePath = folder,
//                            ImageSource = icon,
//                            IsTreeViewNode = true
//                        });
//                    }
//                    catch { }
//                }
//            }
//            catch { }

//            return items;
//        }

//        private async Task<List<ExplorerItemViewModel>> InitializeMyComputerCacheAsync(string panelId, IDirectoryHistory history, CancellationToken token)
//        {
//            var items = new List<ExplorerItemViewModel>
//            {
//                new ExplorerItemViewModel(history)
//                {
//                    Name = "Мой Компьютер",
//                    FilePath = "Drives",
//                    ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/computer.png")),
//                    IsTreeViewNode = true
//                }
//            };

//            await LoadSystemFoldersAsync(items, history, token);
//            _panelCaches[panelId] = items;

//            return items;
//        }

//        public async Task<List<ExplorerItemViewModel>> LoadHomeAsync(string panelId, IDirectoryHistory history, CancellationToken token = default)
//        {
//            if (_panelCaches.TryGetValue(panelId, out var cachedItems) && !token.IsCancellationRequested)
//                return new List<ExplorerItemViewModel>(cachedItems);

//            var items = await InitializeHomeCacheAsync(panelId, history, token);
//            return new List<ExplorerItemViewModel>(items);
//        }

//        private async Task<List<ExplorerItemViewModel>> InitializeHomeCacheAsync(string panelId, IDirectoryHistory history, CancellationToken token)
//        {
//            var items = new List<ExplorerItemViewModel>();
//            await LoadSystemFoldersAsync(items, history, token);
//            _panelCaches[panelId] = items;
//            return items;
//        }

//        public async Task LoadSystemFoldersAsync(List<ExplorerItemViewModel> items, IDirectoryHistory history, CancellationToken token)
//        {
//            var systemFolders = new[]
//            {
//                new { Name = "Рабочий стол", Path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) },
//                new { Name = "Документы", Path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) },
//                new { Name = "Изображения", Path = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) },
//                new { Name = "Музыка", Path = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic) },
//                new { Name = "Видео", Path = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos) },
//                new { Name = "Загрузки", Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads") }
//            };

//            foreach (var folder in systemFolders)
//            {
//                if (token.IsCancellationRequested) break;
//                try
//                {
//                    if (!Directory.Exists(folder.Path)) continue;
//                    var icon = await _iconService.GetSpecialFolderIconAsync(folder.Path);
//                    items.Add(new ExplorerItemViewModel(history)
//                    {
//                        Name = folder.Name,
//                        FilePath = folder.Path,
//                        ImageSource = icon ?? new BitmapImage(new Uri("ms-appx:///Assets/folder1.png")),
//                        IsTreeViewNode = true,
//                        IsSpecialFolderNode = true
//                    });
//                }
//                catch
//                {
//                    items.Add(new ExplorerItemViewModel(history)
//                    {
//                        Name = folder.Name,
//                        FilePath = folder.Path,
//                        ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/folder1.png")),
//                        IsTreeViewNode = true,
//                        IsSpecialFolderNode = true
//                    });
//                }

//                await Task.Delay(1, token);
//            }
//        }

//        public ExplorerItemViewModel CreateSpecialFoldersItem(IDirectoryHistory history)
//        {
//            return new ExplorerItemViewModel(history)
//            {
//                Name = "Специальные папки",
//                FilePath = "SpecialFolders",
//                ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/home.png")),
//                IsTreeViewNode = true,
//                IsSpecialFolderNode = true
//            };
//        }

//        private async Task LoadSubfoldersAsync(List<ExplorerItemViewModel> items, string folderPath, IDirectoryHistory history, CancellationToken token)
//        {
//            var folders = Directory.GetDirectories(folderPath);
//            var defaultFolderIcon = new BitmapImage(new Uri("ms-appx:///Assets/folder1.png"));

//            foreach (var folder in folders)
//            {
//                if (token.IsCancellationRequested) break;
//                try
//                {
//                    var dirInfo = new DirectoryInfo(folder);
//                    var icon = await _iconService.GetIconAsync(folder, true) ?? defaultFolderIcon;
//                    items.Add(new ExplorerItemViewModel(history)
//                    {
//                        Name = dirInfo.Name,
//                        FilePath = folder,
//                        ImageSource = icon,
//                        IsTreeViewNode = true
//                    });
//                }
//                catch { }
//            }
//        }

//        private async Task LoadFilesAsync(List<ExplorerItemViewModel> items, string folderPath, IDirectoryHistory history, CancellationToken token)
//        {
//            var files = Directory.GetFiles(folderPath);
//            var defaultFileIcon = new BitmapImage(new Uri("ms-appx:///Assets/file.png"));

//            foreach (var file in files)
//            {
//                if (token.IsCancellationRequested) break;
//                try
//                {
//                    var fileInfo = new FileInfo(file);
//                    var icon = await FileCacheService.GetFileIconAsync(file) ?? defaultFileIcon;
//                    items.Add(new ExplorerItemViewModel(history)
//                    {
//                        Name = fileInfo.Name,
//                        FilePath = file,
//                        ImageSource = icon,
//                        IsTreeViewNode = true
//                    });
//                }
//                catch { }
//            }
//        }

//        private async Task<ExplorerItemViewModel> CreateDriveItemAsync(string drivePath, IDirectoryHistory history)
//        {
//            var driveInfo = new DriveInfo(drivePath);
//            var driveViewModel = new DriveViewModel(driveInfo, EntityFlags.IsDrive);

//            BitmapImage icon;
//            try
//            {
//                icon = await _iconService.GetIconAsync(drivePath, true);
//            }
//            catch
//            {
//                icon = new BitmapImage(new Uri("ms-appx:///Assets/harddisk.png"));
//            }

//            return new ExplorerItemViewModel(history)
//            {
//                IsProgressBarVisible = true,
//                Name = GetDriveDisplayName(driveInfo, drivePath),
//                FilePath = drivePath,
//                ImageSource = icon,
//                UsedSpaceString = driveViewModel.UsedSpaceString,
//                FreeSpaceString = driveViewModel.FreeSpaceString,
//                TotalSizeString = driveViewModel.TotalSizeString,
//                UsedProcentValue = driveViewModel.UsedProcentValue,
//                IsTreeViewNode = true
//            };
//        }

//        private ExplorerItemViewModel CreateBackItem(IDirectoryHistory history)
//        {
//            return new ExplorerItemViewModel(history)
//            {
//                Name = "..",
//                FilePath = "..",
//                ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/ahead-only.png")),
//                IsTreeViewNode = true
//            };
//        }

//        private string GetDriveDisplayName(DriveInfo driveInfo, string drivePath)
//        {
//            string driveLetter = drivePath.TrimEnd('\\');
//            if (string.IsNullOrWhiteSpace(driveInfo.VolumeLabel))
//                return driveLetter;
//            else
//                return $"{driveInfo.VolumeLabel} ({driveLetter})";
//        }

//        private void CancelCurrentOperation()
//        {
//            _currentOperationCts?.Cancel();
//            _currentOperationCts?.Dispose();
//            _currentOperationCts = new CancellationTokenSource();
//        }

//        public void CancelAllOperations() => CancelCurrentOperation();

//        public void ClearPanelCache(string panelId)
//        {
//            if (_panelCaches.TryGetValue(panelId, out var cache))
//            {
//                foreach (var item in cache)
//                {
//                    item?.Dispose();
//                }
//                _panelCaches.Remove(panelId);
//            }
//        }

//        public void ClearAllCaches()
//        {
//            foreach (var (panelId, cache) in _panelCaches)
//            {
//                foreach (var item in cache)
//                {
//                    item?.Dispose();
//                }
//            }
//            _panelCaches.Clear();
//        }

//        public void RefreshNavigationSettings()
//        {
//            try
//            {
//                bool showBackNavigation = ShowNavigationBackItem;
//                ClearAllCaches();
//                NavigationSettingsMediator.NotifySettingsChanged(showBackNavigation);
//            }
//            catch { }
//        }

//        public bool IsNavigationPath(string path)
//        {
//            return path == ".." ||
//                   path == "MyComputer" ||
//                   path == "Drives" ||
//                   Directory.Exists(path);
//        }

//        public string GetDisplayName(string path)
//        {
//            if (string.IsNullOrEmpty(path))
//                return "Неизвестный путь";

//            if (path == "MyComputer")
//                return "Мой Компьютер";
//            if (path == "SpecialFolders")
//                return "Специальные папки";
//            if (path == "Drives")
//                return "Диски";

//            if (path.Length == 3 && path.EndsWith(":\\") && char.IsLetter(path[0]))
//            {
//                try
//                {
//                    var driveInfo = new DriveInfo(path);
//                    return GetDriveDisplayName(driveInfo, path);
//                }
//                catch
//                {
//                    return path;
//                }
//            }

//            if (_breadcrumbCacheTask?.IsCompletedSuccessfully == true &&
//                _panelCaches.TryGetValue("Breadcrumb", out var cachedItems))
//            {
//                var found = cachedItems.FirstOrDefault(item =>
//                    string.Equals(item.FilePath, path, StringComparison.OrdinalIgnoreCase));
//                if (found != null)
//                    return found.Name;
//            }

//            string specialName = GetSpecialFolderDisplayNameSync(path);
//            if (!string.IsNullOrEmpty(specialName))
//                return specialName;

//            if (Directory.Exists(path) || File.Exists(path))
//                return Path.GetFileName(path);

//            try
//            {
//                return Path.GetFileName(path);
//            }
//            catch
//            {
//                return path;
//            }
//        }

//        private string GetSpecialFolderDisplayNameSync(string path)
//        {
//            try
//            {
//                string normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

//                var specialFolders = new Dictionary<string, string>
//                {
//                    { Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)).TrimEnd(Path.DirectorySeparatorChar), "Рабочий стол" },
//                    { Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)).TrimEnd(Path.DirectorySeparatorChar), "Документы" },
//                    { Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)).TrimEnd(Path.DirectorySeparatorChar), "Изображения" },
//                    { Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)).TrimEnd(Path.DirectorySeparatorChar), "Музыка" },
//                    { Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)).TrimEnd(Path.DirectorySeparatorChar), "Видео" },
//                    { Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")).TrimEnd(Path.DirectorySeparatorChar), "Загрузки" }
//                };

//                if (specialFolders.TryGetValue(normalizedPath, out string displayName))
//                    return displayName;
//            }
//            catch { }
//            return null;
//        }

//        public void Dispose()
//        {
//            if (IsDisposed) return;

//            CancelCurrentOperation();
//            ClearAllCaches();
//            _currentOperationCts?.Dispose();

//            IsDisposed = true;
//        }
//    }
//}

//using Core_FileManagement;
//using Microsoft.UI.Xaml.Media.Imaging;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using Windows.Storage;

//namespace ufm
//{
//    public class FileSystemService : IDisposable
//    {
//        private static readonly Core_FileManagement.IIconService _iconService = new Core_FileManagement.IconService();
//        private CancellationTokenSource _currentOperationCts;
//        private readonly Dictionary<string, List<ExplorerItemViewModel>> _panelCaches = new();
//        private Task _breadcrumbCacheTask;

//        public bool IsDisposed { get; private set; }

//        public bool ShowNavigationBackItem
//        {
//            get
//            {
//                try
//                {
//                    return App.SettingsManager?.GetSetting<bool>("ShowNavigationBackItem", true) ?? true;
//                }
//                catch
//                {
//                    return true;
//                }
//            }
//        }

//        public FileSystemService()
//        {
//            _currentOperationCts = new CancellationTokenSource();
//            FileCacheService.Initialize(_iconService);

//            _breadcrumbCacheTask = Task.Run(async () =>
//            {
//                try
//                {
//                    var history = new DirectoryHistory("SpecialFolders", "Специальные папки");
//                    await LoadHomeAsync("Breadcrumb", history);
//                }
//                catch { }
//            });
//        }

//        public async Task<List<ExplorerItemViewModel>> LoadPathContentsAsync(string path, string panelId, IDirectoryHistory history = null)
//        {
//            if (IsDisposed) throw new ObjectDisposedException(nameof(FileSystemService));

//            CancelCurrentOperation();
//            var token = _currentOperationCts.Token;

//            try
//            {
//                var localHistory = history ?? new DirectoryHistory("MyComputer", "Мой Компьютер");

//                switch (path)
//                {
//                    case "MyComputer":
//                        return await LoadMyComputerAsync(panelId, localHistory, token);
//                    case "Drives":
//                        return await LoadDrivesAsync(localHistory, token);
//                    default:
//                        if (Directory.Exists(path))
//                            return await LoadFolderContentsAsync(path, localHistory, token);
//                        else
//                            return await LoadMyComputerAsync(panelId, localHistory, token);
//                }
//            }
//            catch (OperationCanceledException)
//            {
//                return new List<ExplorerItemViewModel>();
//            }
//        }

//        public async Task<List<ExplorerItemViewModel>> LoadMyComputerAsync(string panelId, IDirectoryHistory history, CancellationToken token = default)
//        {
//            if (_panelCaches.TryGetValue(panelId, out var cachedItems) && !token.IsCancellationRequested)
//                return new List<ExplorerItemViewModel>(cachedItems);

//            var items = await InitializeMyComputerCacheAsync(panelId, history, token);
//            return new List<ExplorerItemViewModel>(items);
//        }

//        public async Task<List<ExplorerItemViewModel>> LoadDrivesAsync(IDirectoryHistory history, CancellationToken token = default)
//        {
//            var items = new List<ExplorerItemViewModel>();

//            if (ShowNavigationBackItem)
//                items.Add(CreateBackItem(history));

//            foreach (var logicalDrive in Directory.GetLogicalDrives())
//            {
//                if (token.IsCancellationRequested) break;
//                try
//                {
//                    var driveItem = await CreateDriveItemAsync(logicalDrive, history);
//                    if (driveItem != null)
//                        items.Add(driveItem);
//                }
//                catch
//                {
//                    items.Add(new ExplorerItemViewModel(history)
//                    {
//                        Name = logicalDrive,
//                        FilePath = logicalDrive,
//                        ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/harddisk.png"))
//                    });
//                }
//            }

//            return items;
//        }

//        public async Task<List<ExplorerItemViewModel>> LoadFolderContentsAsync(string folderPath, IDirectoryHistory history, CancellationToken token = default)
//        {
//            if (!Directory.Exists(folderPath))
//                throw new DirectoryNotFoundException($"Directory not found: {folderPath}");

//            var items = new List<ExplorerItemViewModel>();

//            if (ShowNavigationBackItem)
//                items.Add(CreateBackItem(history));

//            await LoadSubfoldersAsync(items, folderPath, history, token);
//            await LoadFilesAsync(items, folderPath, history, token);

//            return items;
//        }

//        public async Task<List<ExplorerItemViewModel>> LoadDrivesTree(IDirectoryHistory history, CancellationToken token = default)
//        {
//            var items = new List<ExplorerItemViewModel>();

//            foreach (var logicalDrive in Directory.GetLogicalDrives())
//            {
//                if (token.IsCancellationRequested) break;
//                try
//                {
//                    var driveInfo = new DriveInfo(logicalDrive);
//                    var icon = await _iconService.GetIconAsync(logicalDrive, true).ConfigureAwait(false);
//                    var driveViewModel = new DriveViewModel(driveInfo, EntityFlags.IsDrive);

//                    var fileSystemItem = new ExplorerItemViewModel(history)
//                    {
//                        IsProgressBarVisible = true,
//                        Name = GetDriveDisplayName(driveInfo, logicalDrive),
//                        FilePath = logicalDrive,
//                        ImageSource = icon,
//                        UsedSpaceString = driveViewModel.UsedSpaceString,
//                        FreeSpaceString = driveViewModel.FreeSpaceString,
//                        TotalSizeString = driveViewModel.TotalSizeString,
//                        UsedProcentValue = driveViewModel.UsedProcentValue,
//                        IsTreeViewNode = true
//                    };
//                    items.Add(fileSystemItem);
//                }
//                catch
//                {
//                    items.Add(new ExplorerItemViewModel(history)
//                    {
//                        Name = logicalDrive,
//                        FilePath = logicalDrive,
//                        ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/harddisk.png")),
//                        IsTreeViewNode = true
//                    });
//                }
//            }

//            return items;
//        }

//        public async Task<List<ExplorerItemViewModel>> LoadSubfoldersForTreeViewAsync(string folderPath, IDirectoryHistory history, CancellationToken token = default)
//        {
//            var items = new List<ExplorerItemViewModel>();

//            try
//            {
//                var subfolders = Directory.GetDirectories(folderPath);
//                var defaultIcon = new BitmapImage(new Uri("ms-appx:///Assets/folder1.png"));
//                var throttler = new SemaphoreSlim(initialCount: 10);

//                var tasks = subfolders.Select(async subfolder =>
//                {
//                    await throttler.WaitAsync();
//                    try
//                    {
//                        var icon = await _iconService.GetIconAsync(subfolder, true);
//                        return new ExplorerItemViewModel(history)
//                        {
//                            IsProgressBarVisible = false,
//                            Name = Path.GetFileName(subfolder),
//                            FilePath = subfolder,
//                            ImageSource = icon ?? defaultIcon,
//                            IsTreeViewNode = true
//                        };
//                    }
//                    finally
//                    {
//                        throttler.Release();
//                    }
//                });

//                var results = await Task.WhenAll(tasks);
//                items.AddRange(results);
//            }
//            catch { }

//            return items;
//        }

//        public async Task<List<ExplorerItemViewModel>> LoadFoldersOnlyAsync(string folderPath, IDirectoryHistory history, CancellationToken token = default)
//        {
//            var items = new List<ExplorerItemViewModel>();

//            try
//            {
//                var folders = Directory.GetDirectories(folderPath);
//                var defaultIcon = new BitmapImage(new Uri("ms-appx:///Assets/folder1.png"));

//                foreach (var folder in folders)
//                {
//                    if (token.IsCancellationRequested) break;
//                    try
//                    {
//                        var dirInfo = new DirectoryInfo(folder);
//                        var icon = await _iconService.GetIconAsync(folder, true) ?? defaultIcon;

//                        items.Add(new ExplorerItemViewModel(history)
//                        {
//                            IsProgressBarVisible = false,
//                            Name = dirInfo.Name,
//                            FilePath = folder,
//                            ImageSource = icon,
//                            IsTreeViewNode = true
//                        });
//                    }
//                    catch { }
//                }
//            }
//            catch { }

//            return items;
//        }

//        private async Task<List<ExplorerItemViewModel>> InitializeMyComputerCacheAsync(string panelId, IDirectoryHistory history, CancellationToken token)
//        {
//            var items = new List<ExplorerItemViewModel>
//            {
//                new ExplorerItemViewModel(history)
//                {
//                    Name = "Мой Компьютер",
//                    FilePath = "Drives",
//                    ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/computer.png")),
//                    IsTreeViewNode = true
//                }
//            };

//            await LoadSystemFoldersAsync(items, history, token);
//            _panelCaches[panelId] = items;

//            return items;
//        }

//        public async Task<List<ExplorerItemViewModel>> LoadHomeAsync(string panelId, IDirectoryHistory history, CancellationToken token = default)
//        {
//            if (_panelCaches.TryGetValue(panelId, out var cachedItems) && !token.IsCancellationRequested)
//                return new List<ExplorerItemViewModel>(cachedItems);

//            var items = await InitializeHomeCacheAsync(panelId, history, token);
//            return new List<ExplorerItemViewModel>(items);
//        }

//        private async Task<List<ExplorerItemViewModel>> InitializeHomeCacheAsync(string panelId, IDirectoryHistory history, CancellationToken token)
//        {
//            var items = new List<ExplorerItemViewModel>();
//            await LoadSystemFoldersAsync(items, history, token);
//            _panelCaches[panelId] = items;
//            return items;
//        }

//        public async Task LoadSystemFoldersAsync(List<ExplorerItemViewModel> items, IDirectoryHistory history, CancellationToken token)
//        {
//            var systemFolders = new[]
//            {
//                new { Name = "Рабочий стол", Path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) },
//                new { Name = "Документы", Path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) },
//                new { Name = "Изображения", Path = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) },
//                new { Name = "Музыка", Path = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic) },
//                new { Name = "Видео", Path = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos) },
//                new { Name = "Загрузки", Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads") }
//            };

//            foreach (var folder in systemFolders)
//            {
//                if (token.IsCancellationRequested) break;
//                try
//                {
//                    if (!Directory.Exists(folder.Path)) continue;
//                    var icon = await _iconService.GetSpecialFolderIconAsync(folder.Path);
//                    items.Add(new ExplorerItemViewModel(history)
//                    {
//                        Name = folder.Name,
//                        FilePath = folder.Path,
//                        ImageSource = icon ?? new BitmapImage(new Uri("ms-appx:///Assets/folder1.png")),
//                        IsTreeViewNode = true,
//                        IsSpecialFolderNode = true
//                    });
//                }
//                catch
//                {
//                    items.Add(new ExplorerItemViewModel(history)
//                    {
//                        Name = folder.Name,
//                        FilePath = folder.Path,
//                        ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/folder1.png")),
//                        IsTreeViewNode = true,
//                        IsSpecialFolderNode = true
//                    });
//                }

//                await Task.Delay(1, token);
//            }
//        }

//        public ExplorerItemViewModel CreateSpecialFoldersItem(IDirectoryHistory history)
//        {
//            return new ExplorerItemViewModel(history)
//            {
//                Name = "Специальные папки",
//                FilePath = "SpecialFolders",
//                ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/home.png")),
//                IsTreeViewNode = true,
//                IsSpecialFolderNode = true
//            };
//        }

//        private async Task LoadSubfoldersAsync(List<ExplorerItemViewModel> items, string folderPath, IDirectoryHistory history, CancellationToken token)
//        {
//            var folders = Directory.GetDirectories(folderPath);
//            var defaultFolderIcon = new BitmapImage(new Uri("ms-appx:///Assets/folder1.png"));

//            foreach (var folder in folders)
//            {
//                if (token.IsCancellationRequested) break;
//                try
//                {
//                    var dirInfo = new DirectoryInfo(folder);
//                    var icon = await _iconService.GetIconAsync(folder, true) ?? defaultFolderIcon;
//                    items.Add(new ExplorerItemViewModel(history)
//                    {
//                        Name = dirInfo.Name,
//                        FilePath = folder,
//                        ImageSource = icon,
//                        IsTreeViewNode = true
//                    });
//                }
//                catch { }
//            }
//        }

//        private async Task LoadFilesAsync(List<ExplorerItemViewModel> items, string folderPath, IDirectoryHistory history, CancellationToken token)
//        {
//            var files = Directory.GetFiles(folderPath);
//            var defaultFileIcon = new BitmapImage(new Uri("ms-appx:///Assets/file.png"));

//            foreach (var file in files)
//            {
//                if (token.IsCancellationRequested) break;
//                try
//                {
//                    var fileInfo = new FileInfo(file);
//                    var icon = await FileCacheService.GetFileIconAsync(file) ?? defaultFileIcon;
//                    items.Add(new ExplorerItemViewModel(history)
//                    {
//                        Name = fileInfo.Name,
//                        FilePath = file,
//                        ImageSource = icon,
//                        IsTreeViewNode = true
//                    });
//                }
//                catch { }
//            }
//        }

//        private async Task<ExplorerItemViewModel> CreateDriveItemAsync(string drivePath, IDirectoryHistory history)
//        {
//            var driveInfo = new DriveInfo(drivePath);
//            var driveViewModel = new DriveViewModel(driveInfo, EntityFlags.IsDrive);

//            BitmapImage icon;
//            try
//            {
//                icon = await _iconService.GetIconAsync(drivePath, true);
//            }
//            catch
//            {
//                icon = new BitmapImage(new Uri("ms-appx:///Assets/harddisk.png"));
//            }

//            return new ExplorerItemViewModel(history)
//            {
//                IsProgressBarVisible = true,
//                Name = GetDriveDisplayName(driveInfo, drivePath),
//                FilePath = drivePath,
//                ImageSource = icon,
//                UsedSpaceString = driveViewModel.UsedSpaceString,
//                FreeSpaceString = driveViewModel.FreeSpaceString,
//                TotalSizeString = driveViewModel.TotalSizeString,
//                UsedProcentValue = driveViewModel.UsedProcentValue,
//                IsTreeViewNode = true
//            };
//        }

//        private ExplorerItemViewModel CreateBackItem(IDirectoryHistory history)
//        {
//            return new ExplorerItemViewModel(history)
//            {
//                Name = "..",
//                FilePath = "..",
//                ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/ahead-only.png")),
//                IsTreeViewNode = true
//            };
//        }

//        private string GetDriveDisplayName(DriveInfo driveInfo, string drivePath)
//        {
//            string driveLetter = drivePath.TrimEnd('\\');
//            if (string.IsNullOrWhiteSpace(driveInfo.VolumeLabel))
//                return driveLetter;
//            else
//                return $"{driveInfo.VolumeLabel} ({driveLetter})";
//        }

//        private void CancelCurrentOperation()
//        {
//            _currentOperationCts?.Cancel();
//            _currentOperationCts?.Dispose();
//            _currentOperationCts = new CancellationTokenSource();
//        }

//        public void CancelAllOperations() => CancelCurrentOperation();

//        public void ClearPanelCache(string panelId)
//        {
//            if (_panelCaches.TryGetValue(panelId, out var cache))
//            {
//                foreach (var item in cache)
//                {
//                    item?.Dispose();
//                }
//                _panelCaches.Remove(panelId);
//            }
//        }

//        public void ClearAllCaches()
//        {
//            foreach (var (panelId, cache) in _panelCaches)
//            {
//                foreach (var item in cache)
//                {
//                    item?.Dispose();
//                }
//            }
//            _panelCaches.Clear();
//        }

//        public void RefreshNavigationSettings()
//        {
//            try
//            {
//                bool showBackNavigation = ShowNavigationBackItem;
//                ClearAllCaches();
//                NavigationSettingsMediator.NotifySettingsChanged(showBackNavigation);
//            }
//            catch { }
//        }

//        public bool IsNavigationPath(string path)
//        {
//            return path == ".." ||
//                   path == "MyComputer" ||
//                   path == "Drives" ||
//                   Directory.Exists(path);
//        }

//        // ==================== ВНЕСЕНЫ ИЗМЕНЕНИЯ ====================
//        public string GetDisplayName(string path)
//        {
//            if (string.IsNullOrEmpty(path))
//                return "Неизвестный путь";

//            // Начальная страница ("Мой компьютер") теперь называется "Домашняя страница"
//            if (path == "MyComputer")
//                return "Домашняя страница";

//            if (path == "SpecialFolders")
//                return "Специальные папки";

//            // Раздел со списком дисков – продолжаем называть "Мой компьютер"
//            if (path == "Drives")
//                return "Мой компьютер";

//            if (path.Length == 3 && path.EndsWith(":\\") && char.IsLetter(path[0]))
//            {
//                try
//                {
//                    var driveInfo = new DriveInfo(path);
//                    return GetDriveDisplayName(driveInfo, path);
//                }
//                catch
//                {
//                    return path;
//                }
//            }

//            if (_breadcrumbCacheTask?.IsCompletedSuccessfully == true &&
//                _panelCaches.TryGetValue("Breadcrumb", out var cachedItems))
//            {
//                var found = cachedItems.FirstOrDefault(item =>
//                    string.Equals(item.FilePath, path, StringComparison.OrdinalIgnoreCase));
//                if (found != null)
//                    return found.Name;
//            }

//            string specialName = GetSpecialFolderDisplayNameSync(path);
//            if (!string.IsNullOrEmpty(specialName))
//                return specialName;

//            if (Directory.Exists(path) || File.Exists(path))
//                return Path.GetFileName(path);

//            try
//            {
//                return Path.GetFileName(path);
//            }
//            catch
//            {
//                return path;
//            }
//        }

//        private string GetSpecialFolderDisplayNameSync(string path)
//        {
//            try
//            {
//                string normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

//                var specialFolders = new Dictionary<string, string>
//                {
//                    { Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)).TrimEnd(Path.DirectorySeparatorChar), "Рабочий стол" },
//                    { Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)).TrimEnd(Path.DirectorySeparatorChar), "Документы" },
//                    { Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)).TrimEnd(Path.DirectorySeparatorChar), "Изображения" },
//                    { Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)).TrimEnd(Path.DirectorySeparatorChar), "Музыка" },
//                    { Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)).TrimEnd(Path.DirectorySeparatorChar), "Видео" },
//                    { Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")).TrimEnd(Path.DirectorySeparatorChar), "Загрузки" }
//                };

//                if (specialFolders.TryGetValue(normalizedPath, out string displayName))
//                    return displayName;
//            }
//            catch { }
//            return null;
//        }

//        // Новые публичные методы для иконок
//        public async Task<BitmapImage> GetDriveIconAsync(string drivePath)
//        {
//            return await _iconService.GetIconAsync(drivePath, true);
//        }

//        public async Task<BitmapImage> GetFolderIconAsync(string folderPath)
//        {
//            return await _iconService.GetIconAsync(folderPath, true);
//        }

//        public async Task<BitmapImage> GetFileIconAsync(string filePath)
//        {
//            return await FileCacheService.GetFileIconAsync(filePath);
//        }

//        public void Dispose()
//        {
//            if (IsDisposed) return;

//            CancelCurrentOperation();
//            ClearAllCaches();
//            _currentOperationCts?.Dispose();

//            IsDisposed = true;
//        }
//    }
//}


using Core_FileManagement;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
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

        public bool IsDisposed { get; private set; }

        public bool ShowNavigationBackItem
        {
            get
            {
                try
                {
                    return App.SettingsManager?.GetSetting<bool>("ShowNavigationBackItem", true) ?? true;
                }
                catch
                {
                    return true;
                }
            }
        }

        public FileSystemService()
        {
            _currentOperationCts = new CancellationTokenSource();
            FileCacheService.Initialize(_iconService);

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

        public async Task<List<ExplorerItemViewModel>> LoadPathContentsAsync(string path, string panelId, IDirectoryHistory history = null)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(FileSystemService));

            CancelCurrentOperation();
            var token = _currentOperationCts.Token;

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
            if (_panelCaches.TryGetValue(panelId, out var cachedItems) && !token.IsCancellationRequested)
                return new List<ExplorerItemViewModel>(cachedItems);

            var items = await InitializeMyComputerCacheAsync(panelId, history, token);
            return new List<ExplorerItemViewModel>(items);
        }

        public async Task<List<ExplorerItemViewModel>> LoadDrivesAsync(IDirectoryHistory history, CancellationToken token = default)
        {
            var items = new List<ExplorerItemViewModel>();

            if (ShowNavigationBackItem)
                items.Add(CreateBackItem(history));

            foreach (var logicalDrive in Directory.GetLogicalDrives())
            {
                if (token.IsCancellationRequested) break;
                try
                {
                    var driveItem = await CreateDriveItemAsync(logicalDrive, history);
                    if (driveItem != null)
                        items.Add(driveItem);
                }
                catch
                {
                    items.Add(new ExplorerItemViewModel(history)
                    {
                        Name = logicalDrive,
                        FilePath = logicalDrive,
                        ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/harddisk.png"))
                    });
                }
            }

            return items;
        }

        public async Task<List<ExplorerItemViewModel>> LoadFolderContentsAsync(string folderPath, IDirectoryHistory history, CancellationToken token = default)
        {
            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"Directory not found: {folderPath}");

            var items = new List<ExplorerItemViewModel>();

            if (ShowNavigationBackItem)
                items.Add(CreateBackItem(history));

            await LoadSubfoldersAsync(items, folderPath, history, token);
            await LoadFilesAsync(items, folderPath, history, token);

            return items;
        }

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

                    var fileSystemItem = new ExplorerItemViewModel(history)
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
                    };
                    items.Add(fileSystemItem);
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
                var throttler = new SemaphoreSlim(initialCount: 10);

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
                    finally
                    {
                        throttler.Release();
                    }
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
                    Name = "Мой Компьютер",
                    FilePath = "Drives",
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
            if (_panelCaches.TryGetValue(panelId, out var cachedItems) && !token.IsCancellationRequested)
                return new List<ExplorerItemViewModel>(cachedItems);

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

        // ========== Новый метод для домашней страницы ==========
        public async Task<List<ExplorerItemViewModel>> LoadHomePageAsync(CancellationToken token = default)
        {
            var history = new DirectoryHistory("MyComputer", "Домашняя страница");
            var items = new List<ExplorerItemViewModel>
            {
                new ExplorerItemViewModel(history)
                {
                    Name = "Мой Компьютер",
                    FilePath = "Drives",
                    ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/computer.png")),
                    IsTreeViewNode = true
                }
            };
            await LoadSystemFoldersAsync(items, history, token);
            return items;
        }

        private async Task LoadSubfoldersAsync(List<ExplorerItemViewModel> items, string folderPath, IDirectoryHistory history, CancellationToken token)
        {
            var folders = Directory.GetDirectories(folderPath);
            var defaultFolderIcon = new BitmapImage(new Uri("ms-appx:///Assets/folder1.png"));

            foreach (var folder in folders)
            {
                if (token.IsCancellationRequested) break;
                try
                {
                    var dirInfo = new DirectoryInfo(folder);
                    var icon = await _iconService.GetIconAsync(folder, true) ?? defaultFolderIcon;
                    items.Add(new ExplorerItemViewModel(history)
                    {
                        Name = dirInfo.Name,
                        FilePath = folder,
                        ImageSource = icon,
                        IsTreeViewNode = true
                    });
                }
                catch { }
            }
        }

        private async Task LoadFilesAsync(List<ExplorerItemViewModel> items, string folderPath, IDirectoryHistory history, CancellationToken token)
        {
            var files = Directory.GetFiles(folderPath);
            var defaultFileIcon = new BitmapImage(new Uri("ms-appx:///Assets/file.png"));

            foreach (var file in files)
            {
                if (token.IsCancellationRequested) break;
                try
                {
                    var fileInfo = new FileInfo(file);
                    var icon = await FileCacheService.GetFileIconAsync(file) ?? defaultFileIcon;
                    items.Add(new ExplorerItemViewModel(history)
                    {
                        Name = fileInfo.Name,
                        FilePath = file,
                        ImageSource = icon,
                        IsTreeViewNode = true
                    });
                }
                catch { }
            }
        }

        private async Task<ExplorerItemViewModel> CreateDriveItemAsync(string drivePath, IDirectoryHistory history)
        {
            var driveInfo = new DriveInfo(drivePath);
            var driveViewModel = new DriveViewModel(driveInfo, EntityFlags.IsDrive);

            BitmapImage icon;
            try
            {
                icon = await _iconService.GetIconAsync(drivePath, true);
            }
            catch
            {
                icon = new BitmapImage(new Uri("ms-appx:///Assets/harddisk.png"));
            }

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
            string driveLetter = drivePath.TrimEnd('\\');
            if (string.IsNullOrWhiteSpace(driveInfo.VolumeLabel))
                return driveLetter;
            else
                return $"{driveInfo.VolumeLabel} ({driveLetter})";
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
                foreach (var item in cache)
                {
                    item?.Dispose();
                }
                _panelCaches.Remove(panelId);
            }
        }

        public void ClearAllCaches()
        {
            foreach (var (panelId, cache) in _panelCaches)
            {
                foreach (var item in cache)
                {
                    item?.Dispose();
                }
            }
            _panelCaches.Clear();
        }

        public void RefreshNavigationSettings()
        {
            try
            {
                bool showBackNavigation = ShowNavigationBackItem;
                ClearAllCaches();
                NavigationSettingsMediator.NotifySettingsChanged(showBackNavigation);
            }
            catch { }
        }

        public bool IsNavigationPath(string path)
        {
            return path == ".." ||
                   path == "MyComputer" ||
                   path == "Drives" ||
                   Directory.Exists(path);
        }

        public string GetDisplayName(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "Неизвестный путь";

            // Начальная страница — "Домашняя страница"
            if (path == "MyComputer")
                return "Домашняя страница";

            if (path == "SpecialFolders")
                return "Специальные папки";

            // Список дисков — "Мой компьютер"
            if (path == "Drives")
                return "Мой компьютер";

            if (path.Length == 3 && path.EndsWith(":\\") && char.IsLetter(path[0]))
            {
                try
                {
                    var driveInfo = new DriveInfo(path);
                    return GetDriveDisplayName(driveInfo, path);
                }
                catch
                {
                    return path;
                }
            }

            if (_breadcrumbCacheTask?.IsCompletedSuccessfully == true &&
                _panelCaches.TryGetValue("Breadcrumb", out var cachedItems))
            {
                var found = cachedItems.FirstOrDefault(item =>
                    string.Equals(item.FilePath, path, StringComparison.OrdinalIgnoreCase));
                if (found != null)
                    return found.Name;
            }

            string specialName = GetSpecialFolderDisplayNameSync(path);
            if (!string.IsNullOrEmpty(specialName))
                return specialName;

            if (Directory.Exists(path) || File.Exists(path))
                return Path.GetFileName(path);

            try
            {
                return Path.GetFileName(path);
            }
            catch
            {
                return path;
            }
        }

        private string GetSpecialFolderDisplayNameSync(string path)
        {
            try
            {
                string normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                var specialFolders = new Dictionary<string, string>
                {
                    { Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)).TrimEnd(Path.DirectorySeparatorChar), "Рабочий стол" },
                    { Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)).TrimEnd(Path.DirectorySeparatorChar), "Документы" },
                    { Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)).TrimEnd(Path.DirectorySeparatorChar), "Изображения" },
                    { Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)).TrimEnd(Path.DirectorySeparatorChar), "Музыка" },
                    { Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)).TrimEnd(Path.DirectorySeparatorChar), "Видео" },
                    { Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")).TrimEnd(Path.DirectorySeparatorChar), "Загрузки" }
                };

                if (specialFolders.TryGetValue(normalizedPath, out string displayName))
                    return displayName;
            }
            catch { }
            return null;
        }

        public async Task<BitmapImage> GetDriveIconAsync(string drivePath)
        {
            return await _iconService.GetIconAsync(drivePath, true);
        }

        public async Task<BitmapImage> GetFolderIconAsync(string folderPath)
        {
            return await _iconService.GetIconAsync(folderPath, true);
        }

        public async Task<BitmapImage> GetFileIconAsync(string filePath)
        {
            return await FileCacheService.GetFileIconAsync(filePath);
        }

        public void Dispose()
        {
            if (IsDisposed) return;

            CancelCurrentOperation();
            ClearAllCaches();
            _currentOperationCts?.Dispose();

            IsDisposed = true;
        }
    }
}