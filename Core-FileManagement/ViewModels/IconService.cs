//using System;
//using System.Collections.Concurrent;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.UI.Xaml.Media.Imaging;
//using Windows.Storage;
//using Windows.Storage.FileProperties;

//namespace Core_FileManagement
//{
//    public sealed class IconService : IIconService
//    {
//        private static readonly ConcurrentDictionary<string, BitmapImage> _iconCache = new();
//        private const uint DefaultIconSize = 512;
//        private const uint SpecialIconSize = 96; // Увеличенный размер для спецпапок

//        private static readonly BitmapImage _defaultDriveIcon = LoadEmbeddedIcon("ms-appx:///Assets/drive.png");
//        private static readonly BitmapImage _defaultFolderIcon = LoadEmbeddedIcon("ms-appx:///Assets/folder1.png");
//        private static readonly BitmapImage _defaultFileIcon = LoadEmbeddedIcon("ms-appx:///Assets/unknown.png");

//        public async Task<BitmapImage> GetIconAsync(string path, bool isDirectory)
//        {
//            if (string.IsNullOrEmpty(path))
//                return GetDefaultIcon(isDirectory);

//            string cacheKey = $"{path}_{isDirectory}";

//            if (_iconCache.TryGetValue(cacheKey, out var cachedIcon))
//                return cachedIcon;

//            try
//            {
//                BitmapImage icon;
//                if (isDirectory && IsDrivePath(path))
//                {
//                    // Специальная обработка для дисков (синхронная)
//                    icon = GetDriveIconSync(path);
//                }
//                else if (isDirectory)
//                {
//                    icon = await GetFolderIconAsync(path);
//                }
//                else
//                {
//                    icon = await GetFileIconAsync(path);
//                }

//                _iconCache.TryAdd(cacheKey, icon);
//                return icon;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[IconService] Error loading icon for {path}: {ex}");
//                return GetDefaultIcon(isDirectory);
//            }
//        }

//        public BitmapImage GetIconSync(string path, bool isDirectory)
//        {
//            if (string.IsNullOrEmpty(path))
//                return GetDefaultIcon(isDirectory);

//            string cacheKey = $"{path}_{isDirectory}";

//            if (_iconCache.TryGetValue(cacheKey, out var cachedIcon))
//                return cachedIcon;

//            try
//            {
//                // Для синхронного вызова используем Task.Run чтобы избежать deadlock
//                BitmapImage icon = Task.Run(async () =>
//                {
//                    return await GetIconAsync(path, isDirectory).ConfigureAwait(false);
//                }).GetAwaiter().GetResult();

//                _iconCache.TryAdd(cacheKey, icon);
//                return icon;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[IconServiceSync] Error loading icon for {path}: {ex}");
//                return GetDefaultIcon(isDirectory);
//            }
//        }

//        // Новый метод для специальных системных папок

//        public async Task<BitmapImage> GetSpecialFolderIconAsync(string path)
//        {
//            if (string.IsNullOrEmpty(path))
//                return _defaultFolderIcon;

//            string cacheKey = $"{path}_special";

//            if (_iconCache.TryGetValue(cacheKey, out var cachedIcon))
//                return cachedIcon;

//            try
//            {
//                // ПРОБУЕМ БЕЗ DecodePixelWidth/Height - они могут вызывать проблемы
//                var folder = await StorageFolder.GetFolderFromPathAsync(path);
//                using var thumbnail = await folder.GetThumbnailAsync(
//                    ThumbnailMode.SingleItem,
//                    SpecialIconSize,
//                    ThumbnailOptions.UseCurrentScale);

//                if (thumbnail != null && thumbnail.Size > 0)
//                {
//                    var image = new BitmapImage();
//                    // УБИРАЕМ DecodePixelWidth/Height - пусть система сама определяет размер
//                    await image.SetSourceAsync(thumbnail);

//                    _iconCache.TryAdd(cacheKey, image);
//                    return image;
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[SpecialFolderIcon] Error for {path}: {ex}");
//                // Fallback на обычный метод
//                return await GetFolderIconAsync(path);
//            }

//            return _defaultFolderIcon;
//        }
//        private bool IsDrivePath(string path)
//        {
//            return path.Length == 3 &&
//                   char.IsLetter(path[0]) &&
//                   path[1] == ':' &&
//                   path[2] == '\\';
//        }

//        private BitmapImage GetDriveIconSync(string path)
//        {
//            try
//            {
//                var folder = StorageFolder.GetFolderFromPathAsync(path)
//                    .AsTask()
//                    .ConfigureAwait(false)
//                    .GetAwaiter()
//                    .GetResult();

//                var thumbnail = folder.GetThumbnailAsync(
//                    ThumbnailMode.SingleItem,
//                    DefaultIconSize,
//                    ThumbnailOptions.UseCurrentScale)
//                    .AsTask()
//                    .ConfigureAwait(false)
//                    .GetAwaiter()
//                    .GetResult();

//                if (thumbnail != null && thumbnail.Size > 0)
//                {
//                    var image = new BitmapImage();
//                    image.SetSource(thumbnail);
//                    return image;
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[DriveIcon] Error: {ex}");
//            }
//            return _defaultDriveIcon;
//        }

//        private async Task<BitmapImage> GetFolderIconAsync(string path)
//        {
//            try
//            {
//                var folder = await StorageFolder.GetFolderFromPathAsync(path);
//                using var thumbnail = await folder.GetThumbnailAsync(
//                    ThumbnailMode.SingleItem,
//                    DefaultIconSize,
//                    ThumbnailOptions.UseCurrentScale);

//                var image = new BitmapImage();
//                await image.SetSourceAsync(thumbnail);
//                return image;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[FolderIcon] Error: {ex}");
//                return _defaultFolderIcon;
//            }
//        }

//        private async Task<BitmapImage> GetFileIconAsync(string path)
//        {
//            try
//            {
//                var file = await StorageFile.GetFileFromPathAsync(path);
//                using var thumbnail = await file.GetThumbnailAsync(
//                    ThumbnailMode.SingleItem,
//                    DefaultIconSize,
//                    ThumbnailOptions.UseCurrentScale);

//                var image = new BitmapImage();
//                await image.SetSourceAsync(thumbnail);
//                return image;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[FileIcon] Error: {ex}");
//                return _defaultFileIcon;
//            }
//        }

//        private static BitmapImage LoadEmbeddedIcon(string uri)
//        {
//            try
//            {
//                return new BitmapImage(new Uri(uri));
//            }
//            catch
//            {
//                return new BitmapImage();
//            }
//        }

//        private BitmapImage GetDefaultIcon(bool isFolder)
//        {
//            return isFolder ? _defaultFolderIcon : _defaultFileIcon;
//        }

//        public void InvalidateCacheForItem(string path, bool isDirectory)
//        {
//            _iconCache.TryRemove($"{path}_{isDirectory}", out _);
//            // Также удаляем специальную версию если есть
//            _iconCache.TryRemove($"{path}_special", out _);
//        }

//        public void ClearCache()
//        {
//            _iconCache.Clear();
//        }

//        public void Dispose()
//        {
//            ClearCache();
//            GC.SuppressFinalize(this);
//        }

//        // Вспомогательный метод для проверки является ли папка системной
//        public static bool IsSystemFolder(string path)
//        {
//            try
//            {
//                var systemFolders = new[]
//                {
//                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
//                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
//                    Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
//                    Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
//                    Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
//                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
//                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
//                };

//                return systemFolders.Any(f =>
//                    !string.IsNullOrEmpty(f) &&
//                    string.Equals(f, path, StringComparison.OrdinalIgnoreCase));
//            }
//            catch
//            {
//                return false;
//            }
//        }
//    }
//}



//using System;
//using System.Collections.Concurrent;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.UI.Xaml.Media.Imaging;
//using Windows.Storage;
//using Windows.Storage.FileProperties;
//using Windows.ApplicationModel.Core;
//using Windows.UI.Core;

//namespace Core_FileManagement
//{
//    public sealed class IconService : IIconService, IDisposable
//    {
//        private static readonly ConcurrentDictionary<string, BitmapImage> _iconCache = new ConcurrentDictionary<string, BitmapImage>();
//        private const int MaxCacheSize = 1000;
//        private const uint StandardIconSize = 64;
//        private const uint LargeIconSize = 96;

//        private static readonly Lazy<BitmapImage> _defaultDriveIcon = new Lazy<BitmapImage>(() => LoadEmbeddedIcon("ms-appx:///Assets/drive.png"));
//        private static readonly Lazy<BitmapImage> _defaultFolderIcon = new Lazy<BitmapImage>(() => LoadEmbeddedIcon("ms-appx:///Assets/folder1.png"));
//        private static readonly Lazy<BitmapImage> _defaultFileIcon = new Lazy<BitmapImage>(() => LoadEmbeddedIcon("ms-appx:///Assets/unknown.png"));

//        private static readonly Lazy<ConcurrentDictionary<string, bool>> _systemFolders = new Lazy<ConcurrentDictionary<string, bool>>(InitializeSystemFolders);

//        public async Task<BitmapImage> GetIconAsync(string path, bool isDirectory)
//        {
//            if (string.IsNullOrEmpty(path))
//                return GetDefaultIcon(isDirectory);

//            string cacheKey = $"{path}_{isDirectory}";

//            if (_iconCache.TryGetValue(cacheKey, out var cachedIcon))
//                return cachedIcon;

//            try
//            {
//                BitmapImage icon;

//                if (isDirectory)
//                {
//                    if (IsDrivePath(path))
//                    {
//                        icon = await GetDriveIconAsync(path);
//                    }
//                    else if (IsSystemFolder(path))
//                    {
//                        icon = await GetSpecialFolderIconAsync(path);
//                    }
//                    else
//                    {
//                        icon = await GetFolderIconAsync(path);
//                    }
//                }
//                else
//                {
//                    icon = await GetFileIconAsync(path);
//                }

//                AddToCache(cacheKey, icon);
//                return icon;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[IconService] Error loading icon for {path}: {ex}");

//                var defaultIcon = GetDefaultIcon(isDirectory);
//                AddToCache(cacheKey, defaultIcon);
//                return defaultIcon;
//            }
//        }

//        public BitmapImage GetIconSync(string path, bool isDirectory)
//        {
//            if (string.IsNullOrEmpty(path))
//                return GetDefaultIcon(isDirectory);

//            string cacheKey = $"{path}_{isDirectory}";

//            if (_iconCache.TryGetValue(cacheKey, out var cachedIcon))
//                return cachedIcon;

//            try
//            {
//                // Используем Task.Run для избежания deadlock в UI потоке
//                var task = Task.Run(async () => await GetIconAsync(path, isDirectory));
//                var result = task.Result; // Блокируем текущий поток до завершения задачи
//                AddToCache(cacheKey, result);
//                return result;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[IconServiceSync] Error loading icon for {path}: {ex}");
//                return GetDefaultIcon(isDirectory);
//            }
//        }

//        public async Task<BitmapImage> GetSpecialFolderIconAsync(string path)
//        {
//            if (string.IsNullOrEmpty(path))
//                return _defaultFolderIcon.Value;

//            string cacheKey = $"{path}_special";

//            if (_iconCache.TryGetValue(cacheKey, out var cachedIcon))
//                return cachedIcon;

//            try
//            {
//                var folder = await StorageFolder.GetFolderFromPathAsync(path);
//                var thumbnail = await folder.GetThumbnailAsync(
//                    ThumbnailMode.SingleItem,
//                    LargeIconSize,
//                    ThumbnailOptions.UseCurrentScale);

//                if (thumbnail != null && thumbnail.Size > 0)
//                {
//                    var image = new BitmapImage();
//                    await image.SetSourceAsync(thumbnail);
//                    AddToCache(cacheKey, image);
//                    return image;
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[SpecialFolderIcon] Error for {path}: {ex}");
//            }

//            return _defaultFolderIcon.Value;
//        }

//        private bool IsDrivePath(string path)
//        {
//            return path.Length == 3 &&
//                   char.IsLetter(path[0]) &&
//                   path[1] == ':' &&
//                   path[2] == '\\';
//        }

//        private async Task<BitmapImage> GetDriveIconAsync(string path)
//        {
//            try
//            {
//                var folder = await StorageFolder.GetFolderFromPathAsync(path);
//                var thumbnail = await folder.GetThumbnailAsync(
//                    ThumbnailMode.SingleItem,
//                    StandardIconSize,
//                    ThumbnailOptions.UseCurrentScale);

//                if (thumbnail != null && thumbnail.Size > 0)
//                {
//                    var image = new BitmapImage();
//                    await image.SetSourceAsync(thumbnail);
//                    return image;
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[DriveIcon] Error: {ex}");
//            }
//            return _defaultDriveIcon.Value;
//        }

//        private async Task<BitmapImage> GetFolderIconAsync(string path)
//        {
//            try
//            {
//                var folder = await StorageFolder.GetFolderFromPathAsync(path);
//                var thumbnail = await folder.GetThumbnailAsync(
//                    ThumbnailMode.SingleItem,
//                    StandardIconSize,
//                    ThumbnailOptions.UseCurrentScale);

//                var image = new BitmapImage();
//                await image.SetSourceAsync(thumbnail);
//                return image;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[FolderIcon] Error: {ex}");
//                return _defaultFolderIcon.Value;
//            }
//        }

//        private async Task<BitmapImage> GetFileIconAsync(string path)
//        {
//            try
//            {
//                var file = await StorageFile.GetFileFromPathAsync(path);
//                var thumbnail = await file.GetThumbnailAsync(
//                    ThumbnailMode.SingleItem,
//                    StandardIconSize,
//                    ThumbnailOptions.UseCurrentScale);

//                var image = new BitmapImage();
//                await image.SetSourceAsync(thumbnail);
//                return image;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[FileIcon] Error: {ex}");
//                return _defaultFileIcon.Value;
//            }
//        }

//        private static BitmapImage LoadEmbeddedIcon(string uri)
//        {
//            try
//            {
//                return new BitmapImage(new Uri(uri));
//            }
//            catch
//            {
//                return new BitmapImage();
//            }
//        }

//        private BitmapImage GetDefaultIcon(bool isFolder)
//        {
//            return isFolder ? _defaultFolderIcon.Value : _defaultFileIcon.Value;
//        }

//        private void AddToCache(string key, BitmapImage image)
//        {
//            if (_iconCache.Count >= MaxCacheSize)
//            {
//                // Удаляем самые старые элементы (первые 10%)
//                var keysToRemove = _iconCache.Keys.Take(MaxCacheSize / 10).ToList();
//                foreach (var keyToRemove in keysToRemove)
//                {
//                    _iconCache.TryRemove(keyToRemove, out _);
//                }
//            }
//            _iconCache.TryAdd(key, image);
//        }

//        public void InvalidateCacheForItem(string path, bool isDirectory)
//        {
//            _iconCache.TryRemove($"{path}_{isDirectory}", out _);
//            _iconCache.TryRemove($"{path}_special", out _);
//        }

//        public void ClearCache()
//        {
//            _iconCache.Clear();
//        }

//        public void Dispose()
//        {
//            ClearCache();
//            GC.SuppressFinalize(this);
//        }

//        private static ConcurrentDictionary<string, bool> InitializeSystemFolders()
//        {
//            var systemFolders = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
//            try
//            {
//                var folders = new[]
//                {
//                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
//                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
//                    Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
//                    Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
//                    Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
//                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
//                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
//                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
//                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
//                    Environment.GetFolderPath(Environment.SpecialFolder.Windows),
//                    Environment.GetFolderPath(Environment.SpecialFolder.System),
//                    Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
//                    Environment.GetFolderPath(Environment.SpecialFolder.CommonMusic),
//                    Environment.GetFolderPath(Environment.SpecialFolder.CommonPictures),
//                    Environment.GetFolderPath(Environment.SpecialFolder.CommonVideos)
//                };

//                foreach (var folder in folders.Where(f => !string.IsNullOrEmpty(f)))
//                {
//                    systemFolders.TryAdd(folder, true);
//                }
//            }
//            catch
//            {
//                // Игнорируем ошибки инициализации
//            }
//            return systemFolders;
//        }

//        public static bool IsSystemFolder(string path)
//        {
//            if (string.IsNullOrEmpty(path))
//                return false;

//            // Нормализуем путь для сравнения
//            var normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

//            return _systemFolders.Value.ContainsKey(normalizedPath);
//        }
//    }
//}




//using System;
//using System.Collections.Concurrent;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.UI.Xaml.Media.Imaging;
//using Windows.Storage;
//using Windows.Storage.FileProperties;

//namespace Core_FileManagement
//{
//    public sealed class IconService : IIconService
//    {
//        private static readonly ConcurrentDictionary<string, BitmapImage> _iconCache = new();
//        private const uint DefaultIconSize = 512;
//        private const uint SpecialIconSize = 96;

//        private static readonly BitmapImage _defaultDriveIcon = LoadEmbeddedIcon("ms-appx:///Assets/drive.png");
//        private static readonly BitmapImage _defaultFolderIcon = LoadEmbeddedIcon("ms-appx:///Assets/folder1.png");
//        private static readonly BitmapImage _defaultFileIcon = LoadEmbeddedIcon("ms-appx:///Assets/unknown.png");

//        // Кэш для проверки системных папок
//        private static readonly Lazy<string[]> _systemFolders = new Lazy<string[]>(GetSystemFolders);
//        private static readonly string[] _driveRoots = Enumerable.Range('A', 26)
//            .Select(d => $"{Convert.ToChar(d)}:\\").ToArray();

//        public async Task<BitmapImage> GetIconAsync(string path, bool isDirectory)
//        {
//            if (string.IsNullOrEmpty(path))
//                return GetDefaultIcon(isDirectory);

//            string cacheKey = $"{path}_{isDirectory}";

//            // Быстрая проверка кэша
//            if (_iconCache.TryGetValue(cacheKey, out var cachedIcon))
//                return cachedIcon;

//            try
//            {
//                BitmapImage icon = isDirectory switch
//                {
//                    true when IsDrivePath(path) => GetDriveIconSync(path),
//                    true => await GetFolderIconAsync(path),
//                    false => await GetFileIconAsync(path)
//                };

//                return _iconCache.GetOrAdd(cacheKey, icon);
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[IconService] Error loading icon for {path}: {ex}");
//                return GetDefaultIcon(isDirectory);
//            }
//        }

//        public BitmapImage GetIconSync(string path, bool isDirectory)
//        {
//            if (string.IsNullOrEmpty(path))
//                return GetDefaultIcon(isDirectory);

//            string cacheKey = $"{path}_{isDirectory}";

//            if (_iconCache.TryGetValue(cacheKey, out var cachedIcon))
//                return cachedIcon;

//            try
//            {
//                // Используем ValueTask для уменьшения накладных расходов
//                BitmapIconResult result = Task.Run(async () =>
//                {
//                    try
//                    {
//                        var icon = await GetIconAsync(path, isDirectory).ConfigureAwait(false);
//                        return new BitmapIconResult(icon, null);
//                    }
//                    catch (Exception ex)
//                    {
//                        return new BitmapIconResult(null, ex);
//                    }
//                }).GetAwaiter().GetResult();

//                if (result.Exception != null)
//                    throw result.Exception;

//                return _iconCache.GetOrAdd(cacheKey, result.Icon);
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[IconServiceSync] Error loading icon for {path}: {ex}");
//                return GetDefaultIcon(isDirectory);
//            }
//        }

//        public async Task<BitmapImage> GetSpecialFolderIconAsync(string path)
//        {
//            if (string.IsNullOrEmpty(path))
//                return _defaultFolderIcon;

//            string cacheKey = $"{path}_special";

//            if (_iconCache.TryGetValue(cacheKey, out var cachedIcon))
//                return cachedIcon;

//            try
//            {
//                var folder = await StorageFolder.GetFolderFromPathAsync(path);
//                using var thumbnail = await folder.GetThumbnailAsync(
//                    ThumbnailMode.SingleItem,
//                    SpecialIconSize,
//                    ThumbnailOptions.UseCurrentScale);

//                if (thumbnail != null && thumbnail.Size > 0)
//                {
//                    var image = new BitmapImage();
//                    await image.SetSourceAsync(thumbnail);
//                    return _iconCache.GetOrAdd(cacheKey, image);
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[SpecialFolderIcon] Error for {path}: {ex}");
//            }

//            return await GetFolderIconAsync(path);
//        }

//        private bool IsDrivePath(string path)
//        {
//            // Используем заранее вычисленный массив дисков для быстрой проверки
//            return path.Length == 3 && _driveRoots.Contains(path);
//        }

//        private BitmapImage GetDriveIconSync(string path)
//        {
//            try
//            {
//                var folder = StorageFolder.GetFolderFromPathAsync(path)
//                    .AsTask()
//                    .GetAwaiter()
//                    .GetResult();

//                var thumbnail = folder.GetThumbnailAsync(
//                    ThumbnailMode.SingleItem,
//                    DefaultIconSize,
//                    ThumbnailOptions.UseCurrentScale)
//                    .AsTask()
//                    .GetAwaiter()
//                    .GetResult();

//                if (thumbnail != null && thumbnail.Size > 0)
//                {
//                    var image = new BitmapImage();
//                    image.SetSource(thumbnail);
//                    return image;
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[DriveIcon] Error: {ex}");
//            }
//            return _defaultDriveIcon;
//        }

//        private async Task<BitmapImage> GetFolderIconAsync(string path)
//        {
//            try
//            {
//                var folder = await StorageFolder.GetFolderFromPathAsync(path);
//                using var thumbnail = await folder.GetThumbnailAsync(
//                    ThumbnailMode.SingleItem,
//                    DefaultIconSize,
//                    ThumbnailOptions.UseCurrentScale);

//                var image = new BitmapImage();
//                await image.SetSourceAsync(thumbnail);
//                return image;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[FolderIcon] Error: {ex}");
//                return _defaultFolderIcon;
//            }
//        }

//        private async Task<BitmapImage> GetFileIconAsync(string path)
//        {
//            try
//            {
//                var file = await StorageFile.GetFileFromPathAsync(path);
//                using var thumbnail = await file.GetThumbnailAsync(
//                    ThumbnailMode.SingleItem,
//                    DefaultIconSize,
//                    ThumbnailOptions.UseCurrentScale);

//                var image = new BitmapImage();
//                await image.SetSourceAsync(thumbnail);
//                return image;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[FileIcon] Error: {ex}");
//                return _defaultFileIcon;
//            }
//        }

//        private static BitmapImage LoadEmbeddedIcon(string uri)
//        {
//            try
//            {
//                return new BitmapImage(new Uri(uri));
//            }
//            catch
//            {
//                return new BitmapImage();
//            }
//        }

//        private BitmapImage GetDefaultIcon(bool isFolder)
//        {
//            return isFolder ? _defaultFolderIcon : _defaultFileIcon;
//        }

//        public void InvalidateCacheForItem(string path, bool isDirectory)
//        {
//            _iconCache.TryRemove($"{path}_{isDirectory}", out _);
//            _iconCache.TryRemove($"{path}_special", out _);
//        }

//        public void ClearCache()
//        {
//            _iconCache.Clear();
//        }

//        public void Dispose()
//        {
//            ClearCache();
//            GC.SuppressFinalize(this);
//        }

//        public static bool IsSystemFolder(string path)
//        {
//            if (string.IsNullOrEmpty(path))
//                return false;

//            var folders = _systemFolders.Value;
//            return folders.Any(f =>
//                !string.IsNullOrEmpty(f) &&
//                string.Equals(f, path, StringComparison.OrdinalIgnoreCase));
//        }

//        private static string[] GetSystemFolders()
//        {
//            try
//            {
//                return new[]
//                {
//                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
//                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
//                    Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
//                    Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
//                    Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
//                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
//                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
//                };
//            }
//            catch
//            {
//                return Array.Empty<string>();
//            }
//        }

//        // Вспомогательная структура для синхронного метода
//        private readonly struct BitmapIconResult
//        {
//            public BitmapImage Icon { get; }
//            public Exception Exception { get; }

//            public BitmapIconResult(BitmapImage icon, Exception exception)
//            {
//                Icon = icon;
//                Exception = exception;
//            }
//        }
//    }
//}


//0005

//using System;
//using System.Collections.Concurrent;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Runtime.CompilerServices;
//using System.Threading.Tasks;
//using Microsoft.UI.Xaml.Media.Imaging;
//using Windows.Storage;
//using Windows.Storage.FileProperties;

//namespace Core_FileManagement
//{
//    public sealed class IconService : IIconService
//    {
//        private readonly ConcurrentDictionary<(string Path, bool IsDirectory), BitmapImage> _iconCache = new();
//        private readonly ConcurrentDictionary<(string Path, bool IsDirectory), Task<BitmapImage>> _loadingTasks = new();

//        private const uint DefaultIconSize = 512;
//        private const uint SpecialIconSize = 96;

//        private static readonly BitmapImage _defaultDriveIcon = LoadEmbeddedIcon("ms-appx:///Assets/drive.png");
//        private static readonly BitmapImage _defaultFolderIcon = LoadEmbeddedIcon("ms-appx:///Assets/folder1.png");
//        private static readonly BitmapImage _defaultFileIcon = LoadEmbeddedIcon("ms-appx:///Assets/unknown.png");

//        private static readonly Lazy<string[]> _systemFolders = new Lazy<string[]>(GetSystemFolders);
//        private static readonly string[] _driveRoots = Enumerable.Range('A', 26)
//            .Select(d => $"{Convert.ToChar(d)}:\\").ToArray();

//        public async Task<BitmapImage> GetIconAsync(string path, bool isDirectory)
//        {
//            if (string.IsNullOrEmpty(path))
//                return GetDefaultIcon(isDirectory);

//            var key = (path, isDirectory);

//            // Быстрая проверка кэша
//            if (_iconCache.TryGetValue(key, out var cachedIcon))
//                return cachedIcon;

//            // Избегаем дублирующих загрузок
//            var loadingTask = _loadingTasks.GetOrAdd(key, _ => LoadIconInternalAsync(path, isDirectory));

//            try
//            {
//                var icon = await loadingTask.ConfigureAwait(false);
//                _iconCache.TryAdd(key, icon);
//                return icon;
//            }
//            finally
//            {
//                _loadingTasks.TryRemove(key, out _);
//            }
//        }

//        public BitmapImage GetIconSync(string path, bool isDirectory)
//        {
//            if (string.IsNullOrEmpty(path))
//                return GetDefaultIcon(isDirectory);

//            var key = (path, isDirectory);

//            if (_iconCache.TryGetValue(key, out var cachedIcon))
//                return cachedIcon;

//            // Используем Lazy для потокобезопасной ленивой загрузки
//            var lazyIcon = new Lazy<BitmapImage>(() =>
//            {
//                try
//                {
//                    return Task.Run(async () => await LoadIconInternalAsync(path, isDirectory))
//                        .GetAwaiter()
//                        .GetResult();
//                }
//                catch (Exception ex)
//                {
//                    Debug.WriteLine($"[IconServiceSync] Error loading icon for {path}: {ex}");
//                    return GetDefaultIcon(isDirectory);
//                }
//            }, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

//            var icon = _iconCache.GetOrAdd(key, _ => lazyIcon.Value);
//            return icon;
//        }

//        public async Task<BitmapImage> GetSpecialFolderIconAsync(string path)
//        {
//            if (string.IsNullOrEmpty(path))
//                return _defaultFolderIcon;

//            // УБИРАЕМ неиспользуемую переменную key
//            var specialKey = (path + "_special", true);

//            if (_iconCache.TryGetValue(specialKey, out var cachedIcon))
//                return cachedIcon;

//            try
//            {
//                var folder = await StorageFolder.GetFolderFromPathAsync(path);
//                using var thumbnail = await folder.GetThumbnailAsync(
//                    ThumbnailMode.SingleItem,
//                    SpecialIconSize,
//                    ThumbnailOptions.UseCurrentScale);

//                if (thumbnail != null && thumbnail.Size > 0)
//                {
//                    var image = new BitmapImage();
//                    await image.SetSourceAsync(thumbnail);
//                    return _iconCache.GetOrAdd(specialKey, image);
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[SpecialFolderIcon] Error for {path}: {ex}");
//            }

//            // Fallback на обычный метод
//            return await GetIconAsync(path, true);
//        }

//        private async Task<BitmapImage> LoadIconInternalAsync(string path, bool isDirectory)
//        {
//            try
//            {
//                if (isDirectory && IsDrivePath(path))
//                    return GetDriveIconSync(path);

//                if (isDirectory)
//                    return await GetFolderIconAsync(path).ConfigureAwait(false);
//                else
//                    return await GetFileIconAsync(path).ConfigureAwait(false);
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[IconService] Error loading icon for {path}: {ex}");
//                return GetDefaultIcon(isDirectory);
//            }
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private bool IsDrivePath(string path)
//        {
//            // Самая быстрая проверка - битовые операции
//            return path.Length == 3 &&
//                   path[1] == ':' &&
//                   path[2] == '\\' &&
//                   ((path[0] >= 'A' && path[0] <= 'Z') || (path[0] >= 'a' && path[0] <= 'z'));
//        }

//        private BitmapImage GetDriveIconSync(string path)
//        {
//            try
//            {
//                var folder = StorageFolder.GetFolderFromPathAsync(path)
//                    .AsTask()
//                    .GetAwaiter()
//                    .GetResult();

//                var thumbnail = folder.GetThumbnailAsync(
//                    ThumbnailMode.SingleItem,
//                    DefaultIconSize,
//                    ThumbnailOptions.UseCurrentScale)
//                    .AsTask()
//                    .GetAwaiter()
//                    .GetResult();

//                if (thumbnail != null && thumbnail.Size > 0)
//                {
//                    var image = new BitmapImage();
//                    image.SetSource(thumbnail);
//                    return image;
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[DriveIcon] Error: {ex}");
//            }
//            return _defaultDriveIcon;
//        }

//        private async Task<BitmapImage> GetFolderIconAsync(string path)
//        {
//            try
//            {
//                var folder = await StorageFolder.GetFolderFromPathAsync(path);
//                using var thumbnail = await folder.GetThumbnailAsync(
//                    ThumbnailMode.SingleItem,
//                    DefaultIconSize,
//                    ThumbnailOptions.UseCurrentScale);

//                return await CreateBitmapFromThumbnail(thumbnail) ?? _defaultFolderIcon;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[FolderIcon] Error: {ex}");
//                return _defaultFolderIcon;
//            }
//        }

//        private async Task<BitmapImage> GetFileIconAsync(string path)
//        {
//            try
//            {
//                var file = await StorageFile.GetFileFromPathAsync(path);
//                using var thumbnail = await file.GetThumbnailAsync(
//                    ThumbnailMode.SingleItem,
//                    DefaultIconSize,
//                    ThumbnailOptions.UseCurrentScale);

//                return await CreateBitmapFromThumbnail(thumbnail) ?? _defaultFileIcon;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[FileIcon] Error: {ex}");
//                return _defaultFileIcon;
//            }
//        }

//        private static async Task<BitmapImage> CreateBitmapFromThumbnail(StorageItemThumbnail thumbnail)
//        {
//            if (thumbnail == null || thumbnail.Size == 0)
//                return null;

//            var image = new BitmapImage();
//            await image.SetSourceAsync(thumbnail);
//            return image;
//        }

//        private static BitmapImage LoadEmbeddedIcon(string uri)
//        {
//            try
//            {
//                return new BitmapImage(new Uri(uri));
//            }
//            catch
//            {
//                return new BitmapImage();
//            }
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private BitmapImage GetDefaultIcon(bool isFolder)
//        {
//            return isFolder ? _defaultFolderIcon : _defaultFileIcon;
//        }

//        public void InvalidateCache(string path, bool isDirectory)
//        {
//            var key = (path, isDirectory);
//            var specialKey = (path + "_special", true);

//            _iconCache.TryRemove(key, out _);
//            _iconCache.TryRemove(specialKey, out _);
//            _loadingTasks.TryRemove(key, out _);
//        }

//        public void ClearCache()
//        {
//            _iconCache.Clear();
//            _loadingTasks.Clear();
//        }

//        public void Dispose()
//        {
//            ClearCache();
//            GC.SuppressFinalize(this);
//        }

//        public static bool IsSystemFolder(string path)
//        {
//            if (string.IsNullOrEmpty(path))
//                return false;

//            var folders = _systemFolders.Value;
//            return folders.Any(f =>
//                !string.IsNullOrEmpty(f) &&
//                string.Equals(f, path, StringComparison.OrdinalIgnoreCase));
//        }

//        private static string[] GetSystemFolders()
//        {
//            try
//            {
//                return new[]
//                {
//                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
//                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
//                    Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
//                    Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
//                    Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
//                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
//                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
//                };
//            }
//            catch
//            {
//                return Array.Empty<string>();
//            }
//        }

//        // Метод для предзагрузки часто используемых иконок
//        public async Task PreloadCommonIconsAsync()
//        {
//            var commonPaths = new[]
//            {
//                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
//                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
//                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
//                Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
//                Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
//                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
//            };

//            var tasks = commonPaths
//                .Where(path => !string.IsNullOrEmpty(path) && Directory.Exists(path))
//                .Select(path => GetIconAsync(path, true))
//                .ToArray();

//            if (tasks.Length > 0)
//            {
//                await Task.WhenAll(tasks).ContinueWith(_ =>
//                {
//                    // Игнорируем ошибки при предзагрузке
//                }, TaskContinuationOptions.OnlyOnRanToCompletion);
//            }
//        }

//        // Метод для получения статистики кэша (для отладки)
//        public (int CacheCount, int LoadingTasksCount) GetCacheStats()
//        {
//            return (_iconCache.Count, _loadingTasks.Count);
//        }

//        // Метод для принудительной загрузки без кэша (для тестирования)
//        public async Task<BitmapImage> GetIconNoCacheAsync(string path, bool isDirectory)
//        {
//            if (string.IsNullOrEmpty(path))
//                return GetDefaultIcon(isDirectory);

//            try
//            {
//                if (isDirectory && IsDrivePath(path))
//                    return GetDriveIconSync(path);

//                if (isDirectory)
//                    return await GetFolderIconAsync(path);
//                else
//                    return await GetFileIconAsync(path);
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[IconServiceNoCache] Error loading icon for {path}: {ex}");
//                return GetDefaultIcon(isDirectory);
//            }
//        }
//    }

//}


using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Core_FileManagement
{
    public sealed class IconService : IIconService
    {
        private readonly ConcurrentDictionary<(string Path, bool IsDirectory), BitmapImage> _iconCache = new();
        private readonly ConcurrentDictionary<(string Path, bool IsDirectory), Task<BitmapImage>> _loadingTasks = new();

        private const uint DefaultIconSize = 512;
        private const uint SpecialIconSize = 96;

        private static readonly BitmapImage _defaultDriveIcon = LoadEmbeddedIcon("ms-appx:///Assets/drive.png");
        private static readonly BitmapImage _defaultFolderIcon = LoadEmbeddedIcon("ms-appx:///Assets/folder1.png");
        private static readonly BitmapImage _defaultFileIcon = LoadEmbeddedIcon("ms-appx:///Assets/unknown.png");

        private static readonly Lazy<string[]> _systemFolders = new Lazy<string[]>(GetSystemFolders);
        private static readonly string[] _driveRoots = Enumerable.Range('A', 26)
            .Select(d => $"{Convert.ToChar(d)}:\\").ToArray();

        public async Task<BitmapImage> GetIconAsync(string path, bool isDirectory)
        {
            if (string.IsNullOrEmpty(path))
                return GetDefaultIcon(isDirectory);

            var key = (path, isDirectory);

            if (_iconCache.TryGetValue(key, out var cachedIcon))
                return cachedIcon;

            var loadingTask = _loadingTasks.GetOrAdd(key, _ => LoadIconInternalAsync(path, isDirectory));

            try
            {
                var icon = await loadingTask.ConfigureAwait(false);
                _iconCache.TryAdd(key, icon);
                return icon;
            }
            finally
            {
                _loadingTasks.TryRemove(key, out _);
            }
        }

        public BitmapImage GetIconSync(string path, bool isDirectory)
        {
            if (string.IsNullOrEmpty(path))
                return GetDefaultIcon(isDirectory);

            var key = (path, isDirectory);

            if (_iconCache.TryGetValue(key, out var cachedIcon))
                return cachedIcon;

            var lazyIcon = new Lazy<BitmapImage>(() =>
            {
                try
                {
                    return Task.Run(async () => await LoadIconInternalAsync(path, isDirectory))
                        .GetAwaiter()
                        .GetResult();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[IconServiceSync] Error loading icon for {path}: {ex}");
                    return GetDefaultIcon(isDirectory);
                }
            }, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

            var icon = _iconCache.GetOrAdd(key, _ => lazyIcon.Value);
            return icon;
        }

        public async Task<BitmapImage> GetSpecialFolderIconAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
                return _defaultFolderIcon;

            var specialKey = (path + "_special", true);

            if (_iconCache.TryGetValue(specialKey, out var cachedIcon))
                return cachedIcon;

            try
            {
                // УБИРАЕМ все проверки перед попыткой - пусть система сама попробует
                var folder = await StorageFolder.GetFolderFromPathAsync(path);
                using var thumbnail = await folder.GetThumbnailAsync(
                    ThumbnailMode.SingleItem,
                    SpecialIconSize,
                    ThumbnailOptions.UseCurrentScale);

                if (thumbnail != null && thumbnail.Size > 0)
                {
                    var image = new BitmapImage();
                    await image.SetSourceAsync(thumbnail);
                    return _iconCache.GetOrAdd(specialKey, image);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"[SpecialFolderIcon] Unauthorized access to {path}: {ex}");
                // После исключения пробуем получить иконку через обычный метод
            }
            catch (System.Runtime.InteropServices.COMException ex) when (ex.HResult == unchecked((int)0x8000000A))
            {
                Debug.WriteLine($"[SpecialFolderIcon] OneDrive access error for {path}: {ex}");
                // OneDrive ошибка - пробуем через обычный метод
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                Debug.WriteLine($"[SpecialFolderIcon] COM error for {path} (0x{ex.HResult:X8}): {ex}");
                // Любая другая COM ошибка - пробуем через обычный метод
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SpecialFolderIcon] General error for {path}: {ex}");
                // Любая другая ошибка - пробуем через обычный метод
            }

            // Fallback на обычный метод - ВАЖНО: он может успешно получить иконку!
            return await GetIconAsync(path, true);
        }

        private async Task<BitmapImage> LoadIconInternalAsync(string path, bool isDirectory)
        {
            try
            {
                if (isDirectory && IsDrivePath(path))
                    return GetDriveIconSync(path);

                if (isDirectory)
                    return await GetFolderIconAsync(path).ConfigureAwait(false);
                else
                    return await GetFileIconAsync(path).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[IconService] Error loading icon for {path}: {ex}");
                return GetDefaultIcon(isDirectory);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsDrivePath(string path)
        {
            return path.Length == 3 &&
                   path[1] == ':' &&
                   path[2] == '\\' &&
                   ((path[0] >= 'A' && path[0] <= 'Z') || (path[0] >= 'a' && path[0] <= 'z'));
        }

        private BitmapImage GetDriveIconSync(string path)
        {
            try
            {
                var folder = StorageFolder.GetFolderFromPathAsync(path)
                    .AsTask()
                    .GetAwaiter()
                    .GetResult();

                var thumbnail = folder.GetThumbnailAsync(
                    ThumbnailMode.SingleItem,
                    DefaultIconSize,
                    ThumbnailOptions.UseCurrentScale)
                    .AsTask()
                    .GetAwaiter()
                    .GetResult();

                if (thumbnail != null && thumbnail.Size > 0)
                {
                    var image = new BitmapImage();
                    image.SetSource(thumbnail);
                    return image;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DriveIcon] Error: {ex}");
            }
            return _defaultDriveIcon;
        }

        private async Task<BitmapImage> GetFolderIconAsync(string path)
        {
            try
            {
                // УБИРАЕМ все проверки - пусть система сама попробует
                var folder = await StorageFolder.GetFolderFromPathAsync(path);
                using var thumbnail = await folder.GetThumbnailAsync(
                    ThumbnailMode.SingleItem,
                    DefaultIconSize,
                    ThumbnailOptions.UseCurrentScale);

                return await CreateBitmapFromThumbnail(thumbnail) ?? _defaultFolderIcon;
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"[FolderIcon] Unauthorized access to {path}: {ex}");
                return _defaultFolderIcon;
            }
            catch (System.Runtime.InteropServices.COMException ex) when (ex.HResult == unchecked((int)0x8000000A))
            {
                Debug.WriteLine($"[FolderIcon] OneDrive access error for {path}: {ex}");
                return _defaultFolderIcon;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FolderIcon] Error for {path}: {ex}");
                return _defaultFolderIcon;
            }
        }

        private async Task<BitmapImage> GetFileIconAsync(string path)
        {
            try
            {
                var file = await StorageFile.GetFileFromPathAsync(path);
                using var thumbnail = await file.GetThumbnailAsync(
                    ThumbnailMode.SingleItem,
                    DefaultIconSize,
                    ThumbnailOptions.UseCurrentScale);

                return await CreateBitmapFromThumbnail(thumbnail) ?? _defaultFileIcon;
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"[FileIcon] Unauthorized access to {path}: {ex}");
                return _defaultFileIcon;
            }
            catch (System.Runtime.InteropServices.COMException ex) when (ex.HResult == unchecked((int)0x8000000A))
            {
                Debug.WriteLine($"[FileIcon] OneDrive access error for {path}: {ex}");
                return _defaultFileIcon;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FileIcon] Error for {path}: {ex}");
                return _defaultFileIcon;
            }
        }

        private static async Task<BitmapImage> CreateBitmapFromThumbnail(StorageItemThumbnail thumbnail)
        {
            if (thumbnail == null || thumbnail.Size == 0)
                return null;

            var image = new BitmapImage();
            await image.SetSourceAsync(thumbnail);
            return image;
        }

        private static BitmapImage LoadEmbeddedIcon(string uri)
        {
            try
            {
                return new BitmapImage(new Uri(uri));
            }
            catch
            {
                return new BitmapImage();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private BitmapImage GetDefaultIcon(bool isFolder)
        {
            return isFolder ? _defaultFolderIcon : _defaultFileIcon;
        }

        public void InvalidateCache(string path, bool isDirectory)
        {
            var key = (path, isDirectory);
            var specialKey = (path + "_special", true);

            _iconCache.TryRemove(key, out _);
            _iconCache.TryRemove(specialKey, out _);
            _loadingTasks.TryRemove(key, out _);
        }

        public void ClearCache()
        {
            _iconCache.Clear();
            _loadingTasks.Clear();
        }

        public void Dispose()
        {
            ClearCache();
            GC.SuppressFinalize(this);
        }

        public static bool IsSystemFolder(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            var folders = _systemFolders.Value;
            return folders.Any(f =>
                !string.IsNullOrEmpty(f) &&
                string.Equals(f, path, StringComparison.OrdinalIgnoreCase));
        }

        private static string[] GetSystemFolders()
        {
            try
            {
                return new[]
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                    Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                    Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
                };
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        public async Task PreloadCommonIconsAsync()
        {
            var commonPaths = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
            };

            var tasks = commonPaths
                .Where(path => !string.IsNullOrEmpty(path) && Directory.Exists(path))
                .Select(path => GetIconAsync(path, true))
                .ToArray();

            if (tasks.Length > 0)
            {
                await Task.WhenAll(tasks).ContinueWith(_ =>
                {
                }, TaskContinuationOptions.OnlyOnRanToCompletion);
            }
        }

        public (int CacheCount, int LoadingTasksCount) GetCacheStats()
        {
            return (_iconCache.Count, _loadingTasks.Count);
        }

        public async Task<BitmapImage> GetIconNoCacheAsync(string path, bool isDirectory)
        {
            if (string.IsNullOrEmpty(path))
                return GetDefaultIcon(isDirectory);

            try
            {
                if (isDirectory && IsDrivePath(path))
                    return GetDriveIconSync(path);

                if (isDirectory)
                    return await GetFolderIconAsync(path);
                else
                    return await GetFileIconAsync(path);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[IconServiceNoCache] Error loading icon for {path}: {ex}");
                return GetDefaultIcon(isDirectory);
            }
        }
    }
}