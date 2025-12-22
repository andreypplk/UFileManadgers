//using Core_FileManagement;
//using Microsoft.UI.Xaml.Media.Imaging;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
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

//        // ИСПРАВЛЕНО: правильное объявление IconService
//        private static readonly Core_FileManagement.IIconService _iconService =
//            new Core_FileManagement.IconService();

//        private CancellationTokenSource _currentOperationCts;

//        // ИСПРАВЛЕНО: словарь для индивидуальных кэшей по ID панели
//        private readonly Dictionary<string, List<ExplorerItemViewModel>> _panelCaches = new();

//        public bool IsDisposed { get; private set; }

//        public bool ShowNavigationBackItem
//        {
//            get
//            {
//                try
//                {
//                    // Используем App.SettingsManager для получения настройки
//                    return App.SettingsManager?.GetSetting<bool>("ShowNavigationBackItem", true) ?? true;
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"Error getting navigation setting: {ex}");
//                    return true; // По умолчанию показываем
//                }
//            }
//        }

//        #endregion

//        #region Конструктор

//        public FileSystemService()
//        {
//            _currentOperationCts = new CancellationTokenSource();

//            // Инициализация кэша
//            FileCacheService.Initialize(_iconService);
//        }

//        #endregion

//        #region Основные методы для TileViewIcons

//        /// <summary>
//        /// Загружает содержимое указанного пути
//        /// </summary>
//        public async Task<List<ExplorerItemViewModel>> LoadPathContentsAsync(string path, string panelId = "DefaultPanel", IDirectoryHistory history = null)
//        {
//            if (IsDisposed) throw new ObjectDisposedException(nameof(FileSystemService));

//            CancelCurrentOperation();
//            var token = _currentOperationCts.Token;

//            try
//            {
//                // ИСПРАВЛЕНО: создаем историю, если не передана
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
//                            return await LoadMyComputerAsync(panelId, localHistory, token); // Fallback
//                }
//            }
//            catch (OperationCanceledException)
//            {
//                Debug.WriteLine("LoadPathContentsAsync canceled");
//                return new List<ExplorerItemViewModel>();
//            }
//        }

//        /// <summary>
//        /// Загружает содержимое "Мой компьютер" с индивидуальным кэшированием по panelId
//        /// </summary>
//        public async Task<List<ExplorerItemViewModel>> LoadMyComputerAsync(string panelId, IDirectoryHistory history, CancellationToken token = default)
//        {
//            // ИСПРАВЛЕНО: используем индивидуальный кэш для каждой панели
//            if (_panelCaches.TryGetValue(panelId, out var cachedItems) && !token.IsCancellationRequested)
//            {
//                return new List<ExplorerItemViewModel>(cachedItems);
//            }

//            // Создаем новый кэш для этой панели
//            var items = await InitializeMyComputerCacheAsync(panelId, history, token);
//            return new List<ExplorerItemViewModel>(items);
//        }

//        /// <summary>
//        /// Загружает список дисков
//        /// </summary>
//        public async Task<List<ExplorerItemViewModel>> LoadDrivesAsync(IDirectoryHistory history, CancellationToken token = default)
//        {
//            var items = new List<ExplorerItemViewModel>();

//            // Добавляем кнопку "Назад"
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
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"Error loading drive {logicalDrive}: {ex.Message}");

//                    // Fallback item
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

//        /// <summary>
//        /// Загружает содержимое папки
//        /// </summary>
//        public async Task<List<ExplorerItemViewModel>> LoadFolderContentsAsync(string folderPath, IDirectoryHistory history, CancellationToken token = default)
//        {
//            if (!Directory.Exists(folderPath))
//                throw new DirectoryNotFoundException($"Directory not found: {folderPath}");

//            var items = new List<ExplorerItemViewModel>();

//            // Добавляем кнопку "Назад"
//            if (ShowNavigationBackItem)
//            {
//                items.Add(CreateBackItem(history));
//            }
//            // Загружаем папки
//            await LoadSubfoldersAsync(items, folderPath, history, token);

//            // Загружаем файлы
//            await LoadFilesAsync(items, folderPath, history, token);

//            return items;
//        }

//        #endregion

//        #region Методы для TreeView (синхронные и асинхронные)

//        /// <summary>
//        /// Загружает диски синхронно для TreeView (сохраняет оригинальную логику)
//        /// </summary>
//        public List<ExplorerItemViewModel> LoadDrivesSync(IDirectoryHistory history)
//        {
//            var items = new List<ExplorerItemViewModel>();

//            foreach (var logicalDrive in Directory.GetLogicalDrives())
//            {
//                try
//                {
//                    var driveInfo = new DriveInfo(logicalDrive);
//                    var folder = StorageFolder.GetFolderFromPathAsync(logicalDrive).GetAwaiter().GetResult();
//                    var icon = _iconService.GetIconAsync(folder.Path, true).GetAwaiter().GetResult();

//                    var driveViewModel = new DriveViewModel(driveInfo, EntityFlags.IsDrive);

//                    var fileSystemItem = new ExplorerItemViewModel(history)
//                    {
//                        IsProgressBarVisible = true,
//                        Name = string.IsNullOrEmpty(folder.DisplayName) ? logicalDrive : folder.DisplayName,
//                        FilePath = logicalDrive,
//                        ImageSource = icon,
//                        UsedSpaceString = driveViewModel.UsedSpaceString,
//                        FreeSpaceString = driveViewModel.FreeSpaceString,
//                        TotalSizeString = driveViewModel.TotalSizeString,
//                        UsedProcentValue = driveViewModel.UsedProcentValue
//                    };

//                    items.Add(fileSystemItem);
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"Ошибка загрузки диска {logicalDrive}: {ex.Message}");
//                }
//            }

//            return items;
//        }

//        /// <summary>
//        /// Загружает подпапки для TreeView (сохраняет оригинальную логику с throttler)
//        /// </summary>
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

//                        var fileSystemItem = new ExplorerItemViewModel(history)
//                        {
//                            IsProgressBarVisible = false,
//                            Name = Path.GetFileName(subfolder),
//                            FilePath = subfolder,
//                            ImageSource = icon ?? defaultIcon
//                        };

//                        return fileSystemItem;
//                    }
//                    finally
//                    {
//                        throttler.Release();
//                    }
//                });

//                var results = await Task.WhenAll(tasks);
//                items.AddRange(results);
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Ошибка загрузки подпапок: {ex.Message}");
//            }

//            return items;
//        }

//        /// <summary>
//        /// Загружает только папки для TreeView (без файлов и кнопки "Назад")
//        /// </summary>
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
//                            ImageSource = icon
//                        });
//                    }
//                    catch (Exception ex)
//                    {
//                        Debug.WriteLine($"Error loading folder {folder}: {ex.Message}");
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Error loading folders for tree view: {ex.Message}");
//            }

//            return items;
//        }

//        #endregion

//        #region Приватные методы загрузки данных

//        private async Task<List<ExplorerItemViewModel>> InitializeMyComputerCacheAsync(string panelId, IDirectoryHistory history, CancellationToken token)
//        {
//            var items = new List<ExplorerItemViewModel>();

//            // Добавляем основной элемент "Мой компьютер"
//            items.Add(new ExplorerItemViewModel(history)
//            {
//                Name = "Мой Компьютер",
//                FilePath = "Drives",
//                ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/computer.png"))
//            });

//            // Добавляем системные папки
//            await LoadSystemFoldersAsync(items, history, token);

//            // Сохраняем в индивидуальный кэш панели
//            _panelCaches[panelId] = items;

//            return items;
//        }

//        public async Task<List<ExplorerItemViewModel>> LoadHomeAsync(string panelId, IDirectoryHistory history, CancellationToken token = default)
//        {
//            // ИСПРАВЛЕНО: используем индивидуальный кэш для каждой панели
//            if (_panelCaches.TryGetValue(panelId, out var cachedItems) && !token.IsCancellationRequested)
//            {
//                return new List<ExplorerItemViewModel>(cachedItems);
//            }

//            // Создаем новый кэш для этой панели
//            var items = await InitializeHomeCacheAsync(panelId, history, token);
//            return new List<ExplorerItemViewModel>(items);
//        }

//        private async Task<List<ExplorerItemViewModel>> InitializeHomeCacheAsync(string panelId, IDirectoryHistory history, CancellationToken token)
//        {
//            var items = new List<ExplorerItemViewModel>();

//            // Добавляем ТОЛЬКО системные папки как отдельные узлы
//            await LoadSystemFoldersAsync(items, history, token);

//            // Сохраняем в индивидуальный кэш панели
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
//                        ImageSource = icon ?? new BitmapImage(new Uri("ms-appx:///Assets/folder1.png"))
//                    });
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"Error loading system folder {folder.Name}: {ex.Message}");

//                    // Fallback
//                    items.Add(new ExplorerItemViewModel(history)
//                    {
//                        Name = folder.Name,
//                        FilePath = folder.Path,
//                        ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/folder1.png"))
//                    });
//                }

//                await Task.Delay(1, token); // Небольшая пауза
//            }
//        }
//        // В класс FileSystemService добавим метод
//        public ExplorerItemViewModel CreateSpecialFoldersItem(IDirectoryHistory history)
//        {
//            return new ExplorerItemViewModel(history)
//            {
//                Name = "Специальные папки",
//                FilePath = "SpecialFolders",
//                ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/home.png")),
//                IsTreeViewNode = true,
//                IsSpecialFolderNode = true // ДОБАВЛЕНО
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
//                        ImageSource = icon
//                    });
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"Error loading folder {folder}: {ex.Message}");
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
//                        ImageSource = icon
//                    });
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"Error loading file {file}: {ex.Message}");
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
//                Name = driveInfo.VolumeLabel ?? drivePath,
//                FilePath = drivePath,
//                ImageSource = icon,
//                UsedSpaceString = driveViewModel.UsedSpaceString,
//                FreeSpaceString = driveViewModel.FreeSpaceString,
//                TotalSizeString = driveViewModel.TotalSizeString,
//                UsedProcentValue = driveViewModel.UsedProcentValue
//            };
//        }

//        private ExplorerItemViewModel CreateBackItem(IDirectoryHistory history)
//        {
//            return new ExplorerItemViewModel(history)
//            {
//                Name = "..",
//                FilePath = "..",
//                ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/ahead-only.png"))
//            };
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

//        /// <summary>
//        /// Очищает кэш для конкретной панели
//        /// </summary>
//        public void ClearPanelCache(string panelId)
//        {
//            if (_panelCaches.TryGetValue(panelId, out var cache))
//            {
//                foreach (var item in cache)
//                {
//                    item?.Dispose();
//                }
//                _panelCaches.Remove(panelId);
//                Debug.WriteLine($"Cleared cache for panel: {panelId}");
//            }
//        }

//        /// <summary>
//        /// Очищает все кэши панелей
//        /// </summary>
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
//            Debug.WriteLine("Cleared all panel caches");
//        }

//        public void RefreshNavigationSettings()
//        {
//            try
//            {
//                bool showBackNavigation = ShowNavigationBackItem;
//                ClearAllCaches();

//                Debug.WriteLine($"[FileSystemService] Refreshing navigation settings via mediator");

//                // Уведомляем медиатор
//                NavigationSettingsMediator.NotifySettingsChanged(showBackNavigation);
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"RefreshNavigationSettings error: {ex}");
//            }
//        }

//        #endregion

//        #region Вспомогательные методы

//        /// <summary>
//        /// Проверяет, является ли путь навигационным (папка, диск и т.д.)
//        /// </summary>
//        public bool IsNavigationPath(string path)
//        {
//            return path == ".." ||
//                   path == "MyComputer" ||
//                   path == "Drives" ||
//                   Directory.Exists(path);
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

using Core_FileManagement;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace ufm
{
    public class FileSystemService : IDisposable
    {
        #region Поля и свойства

        // ИСПРАВЛЕНО: правильное объявление IconService
        private static readonly Core_FileManagement.IIconService _iconService =
            new Core_FileManagement.IconService();

        private CancellationTokenSource _currentOperationCts;

        // ИСПРАВЛЕНО: словарь для индивидуальных кэшей по ID панели
        private readonly Dictionary<string, List<ExplorerItemViewModel>> _panelCaches = new();

        public bool IsDisposed { get; private set; }

        public bool ShowNavigationBackItem
        {
            get
            {
                try
                {
                    // Используем App.SettingsManager для получения настройки
                    return App.SettingsManager?.GetSetting<bool>("ShowNavigationBackItem", true) ?? true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting navigation setting: {ex}");
                    return true; // По умолчанию показываем
                }
            }
        }

        #endregion

        #region Конструктор

        public FileSystemService()
        {
            _currentOperationCts = new CancellationTokenSource();

            // Инициализация кэша
            FileCacheService.Initialize(_iconService);
        }

        #endregion

        #region Основные методы для TileViewIcons

        /// <summary>
        /// Загружает содержимое указанного пути
        /// </summary>
        public async Task<List<ExplorerItemViewModel>> LoadPathContentsAsync(string path, string panelId = "DefaultPanel", IDirectoryHistory history = null)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(FileSystemService));

            CancelCurrentOperation();
            var token = _currentOperationCts.Token;

            try
            {
                // ИСПРАВЛЕНО: создаем историю, если не передана
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
                            return await LoadMyComputerAsync(panelId, localHistory, token); // Fallback
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("LoadPathContentsAsync canceled");
                return new List<ExplorerItemViewModel>();
            }
        }

        /// <summary>
        /// Загружает содержимое "Мой компьютер" с индивидуальным кэшированием по panelId
        /// </summary>
        public async Task<List<ExplorerItemViewModel>> LoadMyComputerAsync(string panelId, IDirectoryHistory history, CancellationToken token = default)
        {
            // ИСПРАВЛЕНО: используем индивидуальный кэш для каждой панели
            if (_panelCaches.TryGetValue(panelId, out var cachedItems) && !token.IsCancellationRequested)
            {
                return new List<ExplorerItemViewModel>(cachedItems);
            }

            // Создаем новый кэш для этой панели
            var items = await InitializeMyComputerCacheAsync(panelId, history, token);
            return new List<ExplorerItemViewModel>(items);
        }

        /// <summary>
        /// Загружает список дисков
        /// </summary>
        public async Task<List<ExplorerItemViewModel>> LoadDrivesAsync(IDirectoryHistory history, CancellationToken token = default)
        {
            var items = new List<ExplorerItemViewModel>();

            // Добавляем кнопку "Назад"
            if (ShowNavigationBackItem)
            {
                items.Add(CreateBackItem(history));
            }

            foreach (var logicalDrive in Directory.GetLogicalDrives())
            {
                if (token.IsCancellationRequested) break;

                try
                {
                    var driveItem = await CreateDriveItemAsync(logicalDrive, history);
                    if (driveItem != null)
                        items.Add(driveItem);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading drive {logicalDrive}: {ex.Message}");

                    // Fallback item
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

        /// <summary>
        /// Загружает содержимое папки
        /// </summary>
        public async Task<List<ExplorerItemViewModel>> LoadFolderContentsAsync(string folderPath, IDirectoryHistory history, CancellationToken token = default)
        {
            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"Directory not found: {folderPath}");

            var items = new List<ExplorerItemViewModel>();

            // Добавляем кнопку "Назад"
            if (ShowNavigationBackItem)
            {
                items.Add(CreateBackItem(history));
            }
            // Загружаем папки
            await LoadSubfoldersAsync(items, folderPath, history, token);

            // Загружаем файлы
            await LoadFilesAsync(items, folderPath, history, token);

            return items;
        }

        #endregion

        #region Методы для TreeView (синхронные и асинхронные)

        /// <summary>
        /// Загружает диски синхронно для TreeView (сохраняет оригинальную логику)
        /// </summary>
        public List<ExplorerItemViewModel> LoadDrivesSync(IDirectoryHistory history)
        {
            var items = new List<ExplorerItemViewModel>();

            foreach (var logicalDrive in Directory.GetLogicalDrives())
            {
                try
                {
                    var driveInfo = new DriveInfo(logicalDrive);
                    var folder = StorageFolder.GetFolderFromPathAsync(logicalDrive).GetAwaiter().GetResult();
                    var icon = _iconService.GetIconAsync(folder.Path, true).GetAwaiter().GetResult();

                    var driveViewModel = new DriveViewModel(driveInfo, EntityFlags.IsDrive);

                    var fileSystemItem = new ExplorerItemViewModel(history)
                    {
                        IsProgressBarVisible = true,
                        Name = string.IsNullOrEmpty(folder.DisplayName) ? logicalDrive : folder.DisplayName,
                        FilePath = logicalDrive,
                        ImageSource = icon,
                        UsedSpaceString = driveViewModel.UsedSpaceString,
                        FreeSpaceString = driveViewModel.FreeSpaceString,
                        TotalSizeString = driveViewModel.TotalSizeString,
                        UsedProcentValue = driveViewModel.UsedProcentValue,
                        IsTreeViewNode = true // ДОБАВЛЕНО
                    };

                    items.Add(fileSystemItem);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка загрузки диска {logicalDrive}: {ex.Message}");
                }
            }

            return items;
        }

        /// <summary>
        /// Загружает подпапки для TreeView (сохраняет оригинальную логику с throttler)
        /// </summary>
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

                        var fileSystemItem = new ExplorerItemViewModel(history)
                        {
                            IsProgressBarVisible = false,
                            Name = Path.GetFileName(subfolder),
                            FilePath = subfolder,
                            ImageSource = icon ?? defaultIcon,
                            IsTreeViewNode = true // ДОБАВЛЕНО
                        };

                        return fileSystemItem;
                    }
                    finally
                    {
                        throttler.Release();
                    }
                });

                var results = await Task.WhenAll(tasks);
                items.AddRange(results);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки подпапок: {ex.Message}");
            }

            return items;
        }

        /// <summary>
        /// Загружает только папки для TreeView (без файлов и кнопки "Назад")
        /// </summary>
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
                            IsTreeViewNode = true // ДОБАВЛЕНО
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error loading folder {folder}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading folders for tree view: {ex.Message}");
            }

            return items;
        }

        #endregion

        #region Приватные методы загрузки данных

        private async Task<List<ExplorerItemViewModel>> InitializeMyComputerCacheAsync(string panelId, IDirectoryHistory history, CancellationToken token)
        {
            var items = new List<ExplorerItemViewModel>();

            // Добавляем основной элемент "Мой компьютер"
            items.Add(new ExplorerItemViewModel(history)
            {
                Name = "Мой Компьютер",
                FilePath = "Drives",
                ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/computer.png")),
                IsTreeViewNode = true // ДОБАВЛЕНО
            });

            // Добавляем системные папки
            await LoadSystemFoldersAsync(items, history, token);

            // Сохраняем в индивидуальный кэш панели
            _panelCaches[panelId] = items;

            return items;
        }

        public async Task<List<ExplorerItemViewModel>> LoadHomeAsync(string panelId, IDirectoryHistory history, CancellationToken token = default)
        {
            // ИСПРАВЛЕНО: используем индивидуальный кэш для каждой панели
            if (_panelCaches.TryGetValue(panelId, out var cachedItems) && !token.IsCancellationRequested)
            {
                return new List<ExplorerItemViewModel>(cachedItems);
            }

            // Создаем новый кэш для этой панели
            var items = await InitializeHomeCacheAsync(panelId, history, token);
            return new List<ExplorerItemViewModel>(items);
        }

        private async Task<List<ExplorerItemViewModel>> InitializeHomeCacheAsync(string panelId, IDirectoryHistory history, CancellationToken token)
        {
            var items = new List<ExplorerItemViewModel>();

            // Добавляем ТОЛЬКО системные папки как отдельные узлы
            await LoadSystemFoldersAsync(items, history, token);

            // Сохраняем в индивидуальный кэш панели
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
                        IsTreeViewNode = true, // ДОБАВЛЕНО
                        IsSpecialFolderNode = true // ДОБАВЛЕНО
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading system folder {folder.Name}: {ex.Message}");

                    // Fallback
                    items.Add(new ExplorerItemViewModel(history)
                    {
                        Name = folder.Name,
                        FilePath = folder.Path,
                        ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/folder1.png")),
                        IsTreeViewNode = true, // ДОБАВЛЕНО
                        IsSpecialFolderNode = true // ДОБАВЛЕНО
                    });
                }

                await Task.Delay(1, token); // Небольшая пауза
            }
        }

        // В класс FileSystemService добавим метод
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
                        IsTreeViewNode = true // ДОБАВЛЕНО для вложенных папок в TreeView
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading folder {folder}: {ex.Message}");
                }
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
                        IsTreeViewNode = true // ДОБАВЛЕНО для файлов в TreeView
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading file {file}: {ex.Message}");
                }
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
                Name = driveInfo.VolumeLabel ?? drivePath,
                FilePath = drivePath,
                ImageSource = icon,
                UsedSpaceString = driveViewModel.UsedSpaceString,
                FreeSpaceString = driveViewModel.FreeSpaceString,
                TotalSizeString = driveViewModel.TotalSizeString,
                UsedProcentValue = driveViewModel.UsedProcentValue,
                IsTreeViewNode = true // ДОБАВЛЕНО
            };
        }

        private ExplorerItemViewModel CreateBackItem(IDirectoryHistory history)
        {
            return new ExplorerItemViewModel(history)
            {
                Name = "..",
                FilePath = "..",
                ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/ahead-only.png")),
                IsTreeViewNode = true // ДОБАВЛЕНО
            };
        }

        #endregion

        #region Управление операциями и кэшем

        private void CancelCurrentOperation()
        {
            _currentOperationCts?.Cancel();
            _currentOperationCts?.Dispose();
            _currentOperationCts = new CancellationTokenSource();
        }

        public void CancelAllOperations()
        {
            CancelCurrentOperation();
        }

        /// <summary>
        /// Очищает кэш для конкретной панели
        /// </summary>
        public void ClearPanelCache(string panelId)
        {
            if (_panelCaches.TryGetValue(panelId, out var cache))
            {
                foreach (var item in cache)
                {
                    item?.Dispose();
                }
                _panelCaches.Remove(panelId);
                Debug.WriteLine($"Cleared cache for panel: {panelId}");
            }
        }

        /// <summary>
        /// Очищает все кэши панелей
        /// </summary>
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
            Debug.WriteLine("Cleared all panel caches");
        }

        public void RefreshNavigationSettings()
        {
            try
            {
                bool showBackNavigation = ShowNavigationBackItem;
                ClearAllCaches();

                Debug.WriteLine($"[FileSystemService] Refreshing navigation settings via mediator");

                // Уведомляем медиатор
                NavigationSettingsMediator.NotifySettingsChanged(showBackNavigation);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RefreshNavigationSettings error: {ex}");
            }
        }

        #endregion

        #region Вспомогательные методы

        /// <summary>
        /// Проверяет, является ли путь навигационным (папка, диск и т.д.)
        /// </summary>
        public bool IsNavigationPath(string path)
        {
            return path == ".." ||
                   path == "MyComputer" ||
                   path == "Drives" ||
                   Directory.Exists(path);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (IsDisposed) return;

            CancelCurrentOperation();
            ClearAllCaches();
            _currentOperationCts?.Dispose();

            IsDisposed = true;
        }

        #endregion
    }
}