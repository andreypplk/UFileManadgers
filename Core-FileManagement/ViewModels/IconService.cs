//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Runtime.CompilerServices;
//using System.Threading.Tasks;
//using Microsoft.UI.Xaml.Media.Imaging;
//using Microsoft.UI.Dispatching;
//using Windows.Storage;
//using Windows.Storage.FileProperties;

//namespace Core_FileManagement
//{
//    public sealed class IconService : IIconService
//    {
//        private readonly ConcurrentDictionary<(string Path, bool IsDirectory), BitmapImage> _iconCache = new();
//        private readonly ConcurrentDictionary<(string Path, bool IsDirectory), Task<BitmapImage>> _loadingTasks = new();

//        private const uint DefaultIconSize = 128;
//        private const uint SpecialIconSize = 96;

//        private readonly DispatcherQueue _dispatcherQueue;
//        private readonly BitmapImage _defaultDriveIcon;
//        private readonly BitmapImage _defaultFolderIcon;
//        private readonly BitmapImage _defaultFileIcon;

//        private static readonly HashSet<string> _systemFolderNames = new(StringComparer.OrdinalIgnoreCase)
//        {
//            "$recycle.bin",
//            "system volume information",
//            "$windows.~ws",
//            "$winreagent",
//            "onedrivetemp",
//            "config.msi",
//            "msdownld.tmp",
//            "documents and settings",
//            "recovery",
//            "programdata"
//        };

//        private static readonly Lazy<string[]> _systemFolders = new Lazy<string[]>(GetSystemFolders);
//        private static readonly string[] _driveRoots = Enumerable.Range('A', 26)
//            .Select(d => $"{Convert.ToChar(d)}:\\").ToArray();

//        public IconService()
//        {
//            _dispatcherQueue = DispatcherQueue.GetForCurrentThread()
//                ?? throw new InvalidOperationException("IconService must be created on UI thread");

//            _defaultDriveIcon = LoadEmbeddedIcon("ms-appx:///Assets/drive.png");
//            _defaultFolderIcon = LoadEmbeddedIcon("ms-appx:///Assets/folder1.png");
//            _defaultFileIcon = LoadEmbeddedIcon("ms-appx:///Assets/unknown.png");
//        }

//        private Task<T> RunOnUIThreadAsync<T>(Func<Task<T>> func)
//        {
//            var tcs = new TaskCompletionSource<T>();
//            if (!_dispatcherQueue.TryEnqueue(async () =>
//            {
//                try
//                {
//                    var result = await func();
//                    tcs.SetResult(result);
//                }
//                catch (Exception ex)
//                {
//                    tcs.SetException(ex);
//                }
//            }))
//            {
//                tcs.SetException(new InvalidOperationException("Failed to enqueue on UI thread"));
//            }
//            return tcs.Task;
//        }

//        public async Task<BitmapImage> GetIconAsync(string path, bool isDirectory)
//        {
//            try
//            {
//                if (string.IsNullOrEmpty(path))
//                    return GetDefaultIcon(isDirectory);

//                var key = (path, isDirectory);

//                if (_iconCache.TryGetValue(key, out var cachedIcon))
//                    return cachedIcon;

//                var loadingTask = _loadingTasks.GetOrAdd(key, _ => LoadIconInternalAsync(path, isDirectory));

//                try
//                {
//                    var icon = await loadingTask.ConfigureAwait(false);
//                    _iconCache.TryAdd(key, icon);
//                    return icon;
//                }
//                finally
//                {
//                    _loadingTasks.TryRemove(key, out _);
//                }
//            }
//            finally
//            {
//            }
//        }

//        public BitmapImage GetIconSync(string path, bool isDirectory)
//        {
//            try
//            {
//                if (string.IsNullOrEmpty(path))
//                    return GetDefaultIcon(isDirectory);

//                var key = (path, isDirectory);

//                if (_iconCache.TryGetValue(key, out var cachedIcon))
//                    return cachedIcon;

//                var lazyIcon = new Lazy<BitmapImage>(() =>
//                {
//                    try
//                    {
//                        return Task.Run(async () => await LoadIconInternalAsync(path, isDirectory))
//                            .GetAwaiter()
//                            .GetResult();
//                    }
//                    catch (Exception ex)
//                    {
//                        Debug.WriteLine($"[IconServiceSync] Error loading icon for {path}: {ex}");
//                        return GetDefaultIcon(isDirectory);
//                    }
//                }, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

//                var icon = _iconCache.GetOrAdd(key, _ => lazyIcon.Value);
//                return icon;
//            }
//            finally
//            {
//            }
//        }

//        public async Task<BitmapImage> GetSpecialFolderIconAsync(string path)
//        {
//            try
//            {
//                if (string.IsNullOrEmpty(path))
//                    return _defaultFolderIcon;

//                var specialKey = (path + "_special", true);

//                if (_iconCache.TryGetValue(specialKey, out var cachedIcon))
//                    return cachedIcon;

//                try
//                {
//                    var folder = await StorageFolder.GetFolderFromPathAsync(path);
//                    using var thumbnail = await folder.GetThumbnailAsync(
//                        ThumbnailMode.SingleItem,
//                        SpecialIconSize,
//                        ThumbnailOptions.UseCurrentScale);

//                    if (thumbnail != null && thumbnail.Size > 0)
//                    {
//                        var image = await CreateBitmapFromThumbnail(thumbnail);
//                        if (image != null)
//                            return _iconCache.GetOrAdd(specialKey, image);
//                    }
//                }
//                catch (Exception ex) when (ex is UnauthorizedAccessException or System.Runtime.InteropServices.COMException)
//                {
//                    Debug.WriteLine($"[SpecialFolderIcon] Access error for {path}: {ex.Message}");
//                }

//                return await GetIconAsync(path, true);
//            }
//            finally
//            {
//            }
//        }

//        private async Task<BitmapImage> LoadIconInternalAsync(string path, bool isDirectory)
//        {
//            try
//            {
//                if (isDirectory && IsSystemFolderName(path))
//                {
//                    Debug.WriteLine($"[IconService] Skipping system folder: {path}");
//                    return GetDefaultIcon(isDirectory);
//                }

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
//            finally
//            {
//            }
//        }

//        private bool IsSystemFolderName(string path)
//        {
//            try
//            {
//                string folderName = Path.GetFileName(path);
//                return _systemFolderNames.Contains(folderName);
//            }
//            catch
//            {
//                return false;
//            }
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private bool IsDrivePath(string path)
//        {
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
//                    if (_dispatcherQueue.HasThreadAccess)
//                    {
//                        image.SetSource(thumbnail);
//                    }
//                    else
//                    {
//                        _dispatcherQueue.TryEnqueue(() => image.SetSource(thumbnail));
//                    }
//                    return image;
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[DriveIcon] Error: {ex}");
//            }
//            finally
//            {
//            }
//            return _defaultDriveIcon;
//        }

//        private async Task<BitmapImage> GetFolderIconAsync(string path)
//        {
//            try
//            {
//                if (IsSystemFolderName(path))
//                    return _defaultFolderIcon;

//                var folder = await StorageFolder.GetFolderFromPathAsync(path);
//                using var thumbnail = await folder.GetThumbnailAsync(
//                    ThumbnailMode.SingleItem,
//                    DefaultIconSize,
//                    ThumbnailOptions.UseCurrentScale);

//                return await CreateBitmapFromThumbnail(thumbnail) ?? _defaultFolderIcon;
//            }
//            catch (Exception ex) when (ex is UnauthorizedAccessException or System.Runtime.InteropServices.COMException)
//            {
//                Debug.WriteLine($"[FolderIcon] Access error for {path}: {ex.Message}");
//                return _defaultFolderIcon;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[FolderIcon] Error for {path}: {ex}");
//                return _defaultFolderIcon;
//            }
//            finally
//            {
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
//            catch (Exception ex) when (ex is UnauthorizedAccessException or System.Runtime.InteropServices.COMException)
//            {
//                Debug.WriteLine($"[FileIcon] Access error for {path}: {ex.Message}");
//                return _defaultFileIcon;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[FileIcon] Error for {path}: {ex}");
//                return _defaultFileIcon;
//            }
//            finally
//            {
//            }
//        }

//        private async Task<BitmapImage> CreateBitmapFromThumbnail(StorageItemThumbnail thumbnail)
//        {
//            try
//            {
//                if (thumbnail == null || thumbnail.Size == 0)
//                    return null;

//                if (_dispatcherQueue.HasThreadAccess)
//                {
//                    var image = new BitmapImage();
//                    await image.SetSourceAsync(thumbnail);
//                    return image;
//                }
//                return await RunOnUIThreadAsync(async () =>
//                {
//                    var image = new BitmapImage();
//                    await image.SetSourceAsync(thumbnail);
//                    return image;
//                });
//            }
//            finally
//            {
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
//            finally
//            {
//            }
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private BitmapImage GetDefaultIcon(bool isFolder)
//        {
//            return isFolder ? _defaultFolderIcon : _defaultFileIcon;
//        }

//        public void InvalidateCache(string path, bool isDirectory)
//        {
//            try
//            {
//                var key = (path, isDirectory);
//                var specialKey = (path + "_special", true);

//                _iconCache.TryRemove(key, out _);
//                _iconCache.TryRemove(specialKey, out _);
//                _loadingTasks.TryRemove(key, out _);
//            }
//            finally
//            {
//            }
//        }

//        public void ClearCache()
//        {
//            try
//            {
//                _iconCache.Clear();
//                _loadingTasks.Clear();
//            }
//            finally
//            {
//            }
//        }

//        public void Dispose()
//        {
//            try
//            {
//                ClearCache();
//                GC.SuppressFinalize(this);
//            }
//            finally
//            {
//            }
//        }

//        public static bool IsSystemFolder(string path)
//        {
//            try
//            {
//                if (string.IsNullOrEmpty(path))
//                    return false;

//                var folders = _systemFolders.Value;
//                return folders.Any(f =>
//                    !string.IsNullOrEmpty(f) &&
//                    string.Equals(f, path, StringComparison.OrdinalIgnoreCase));
//            }
//            finally
//            {
//            }
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
//            finally
//            {
//            }
//        }

//        public async Task PreloadCommonIconsAsync()
//        {
//            try
//            {
//                var commonPaths = new[]
//                {
//                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
//                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
//                    Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
//                    Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
//                    Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
//                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
//                };

//                var tasks = commonPaths
//                    .Where(path => !string.IsNullOrEmpty(path) && Directory.Exists(path))
//                    .Select(path => GetIconAsync(path, true))
//                    .ToArray();

//                if (tasks.Length > 0)
//                {
//                    await Task.WhenAll(tasks).ContinueWith(_ =>
//                    {
//                    }, TaskContinuationOptions.OnlyOnRanToCompletion);
//                }
//            }
//            finally
//            {
//            }
//        }

//        public (int CacheCount, int LoadingTasksCount) GetCacheStats()
//        {
//            try
//            {
//                return (_iconCache.Count, _loadingTasks.Count);
//            }
//            finally
//            {
//            }
//        }

//        public async Task<BitmapImage> GetIconNoCacheAsync(string path, bool isDirectory)
//        {
//            try
//            {
//                if (string.IsNullOrEmpty(path))
//                    return GetDefaultIcon(isDirectory);

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
//            finally
//            {
//            }
//        }
//    }
//}


using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Dispatching;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Core_FileManagement
{
    public sealed class IconService : IIconService
    {
        private readonly ConcurrentDictionary<(string Path, bool IsDirectory), BitmapImage> _iconCache = new();
        private readonly ConcurrentDictionary<(string Path, bool IsDirectory), Task<BitmapImage>> _loadingTasks = new();

        private const uint DefaultIconSize = 128;
        private const uint SpecialIconSize = 96;

        private readonly DispatcherQueue _dispatcherQueue;
        private readonly BitmapImage _defaultDriveIcon;
        private readonly BitmapImage _defaultFolderIcon;
        private readonly BitmapImage _defaultFileIcon;

        private static readonly HashSet<string> _systemFolderNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "$recycle.bin",
            "system volume information",
            "$windows.~ws",
            "$winreagent",
            "onedrivetemp",
            "config.msi",
            "msdownld.tmp",
            "documents and settings",
            "recovery",
            "programdata"
        };

        private static readonly Lazy<string[]> _systemFolders = new Lazy<string[]>(GetSystemFolders);
        private static readonly string[] _driveRoots = Enumerable.Range('A', 26)
            .Select(d => $"{Convert.ToChar(d)}:\\").ToArray();

        public IconService()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread()
                ?? throw new InvalidOperationException("IconService must be created on UI thread");

            _defaultDriveIcon = LoadEmbeddedIcon("ms-appx:///Assets/drive.png");
            _defaultFolderIcon = LoadEmbeddedIcon("ms-appx:///Assets/folder1.png");
            _defaultFileIcon = LoadEmbeddedIcon("ms-appx:///Assets/unknown.png");
        }

        private Task<T> RunOnUIThreadAsync<T>(Func<Task<T>> func)
        {
            var tcs = new TaskCompletionSource<T>();
            if (!_dispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    var result = await func();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }))
            {
                tcs.SetException(new InvalidOperationException("Failed to enqueue on UI thread"));
            }
            return tcs.Task;
        }

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
                catch
                {
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
                var folder = await StorageFolder.GetFolderFromPathAsync(path);
                using var thumbnail = await folder.GetThumbnailAsync(
                    ThumbnailMode.SingleItem,
                    SpecialIconSize,
                    ThumbnailOptions.UseCurrentScale);

                if (thumbnail != null && thumbnail.Size > 0)
                {
                    var image = await CreateBitmapFromThumbnail(thumbnail);
                    if (image != null)
                        return _iconCache.GetOrAdd(specialKey, image);
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or System.Runtime.InteropServices.COMException)
            {
            }

            return await GetIconAsync(path, true);
        }

        private async Task<BitmapImage> LoadIconInternalAsync(string path, bool isDirectory)
        {
            try
            {
                if (isDirectory && IsSystemFolderName(path))
                {
                    return GetDefaultIcon(isDirectory);
                }

                if (isDirectory && IsDrivePath(path))
                    return GetDriveIconSync(path);

                if (isDirectory)
                    return await GetFolderIconAsync(path).ConfigureAwait(false);
                else
                    return await GetFileIconAsync(path).ConfigureAwait(false);
            }
            catch
            {
                return GetDefaultIcon(isDirectory);
            }
        }

        private bool IsSystemFolderName(string path)
        {
            try
            {
                string folderName = Path.GetFileName(path);
                return _systemFolderNames.Contains(folderName);
            }
            catch
            {
                return false;
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
                    if (_dispatcherQueue.HasThreadAccess)
                    {
                        image.SetSource(thumbnail);
                    }
                    else
                    {
                        _dispatcherQueue.TryEnqueue(() => image.SetSource(thumbnail));
                    }
                    return image;
                }
            }
            catch
            {
            }
            return _defaultDriveIcon;
        }

        private async Task<BitmapImage> GetFolderIconAsync(string path)
        {
            try
            {
                if (IsSystemFolderName(path))
                    return _defaultFolderIcon;

                var folder = await StorageFolder.GetFolderFromPathAsync(path);
                using var thumbnail = await folder.GetThumbnailAsync(
                    ThumbnailMode.SingleItem,
                    DefaultIconSize,
                    ThumbnailOptions.UseCurrentScale);

                return await CreateBitmapFromThumbnail(thumbnail) ?? _defaultFolderIcon;
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or System.Runtime.InteropServices.COMException)
            {
                return _defaultFolderIcon;
            }
            catch
            {
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
            catch (Exception ex) when (ex is UnauthorizedAccessException or System.Runtime.InteropServices.COMException)
            {
                return _defaultFileIcon;
            }
            catch
            {
                return _defaultFileIcon;
            }
        }

        private async Task<BitmapImage> CreateBitmapFromThumbnail(StorageItemThumbnail thumbnail)
        {
            if (thumbnail == null || thumbnail.Size == 0)
                return null;

            if (_dispatcherQueue.HasThreadAccess)
            {
                var image = new BitmapImage();
                await image.SetSourceAsync(thumbnail);
                return image;
            }
            return await RunOnUIThreadAsync(async () =>
            {
                var image = new BitmapImage();
                await image.SetSourceAsync(thumbnail);
                return image;
            });
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
                await Task.WhenAll(tasks);
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

            if (isDirectory && IsDrivePath(path))
                return GetDriveIconSync(path);

            if (isDirectory)
                return await GetFolderIconAsync(path);
            else
                return await GetFileIconAsync(path);
        }
    }
}