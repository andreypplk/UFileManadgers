//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.UI.Xaml.Media.Imaging;

//namespace Core_FileManagement
//{
//    public static class FileCacheService
//    {
//        private static readonly ConcurrentDictionary<string, (FileViewModel model, DateTime timestamp)> _metadataCache
//            = new ConcurrentDictionary<string, (FileViewModel, DateTime)>(StringComparer.OrdinalIgnoreCase);

//        private static readonly ConcurrentDictionary<string, (BitmapImage icon, DateTime timestamp)> _iconCache
//            = new ConcurrentDictionary<string, (BitmapImage, DateTime)>(StringComparer.OrdinalIgnoreCase);

//        private static readonly TimeSpan _metadataCacheExpiry = TimeSpan.FromMinutes(30);
//        private static readonly TimeSpan _iconCacheExpiry = TimeSpan.FromMinutes(60);

//        private static readonly ReaderWriterLockSlim _metadataLock = new ReaderWriterLockSlim();
//        private static readonly SemaphoreSlim _iconLoadSemaphore = new SemaphoreSlim(1, 1);

//        private static IIconService _iconService;
//        private static bool _isInitialized = false;

//        public static event EventHandler<FileCacheEventArgs> CacheUpdated;
//        public static event EventHandler<FileCacheEventArgs> CacheInvalidated;

//        public static void Initialize(IIconService iconService)
//        {
//            _iconService = iconService ?? throw new ArgumentNullException(nameof(iconService));
//            _isInitialized = true;
//        }

//        public static FileViewModel GetFileMetadata(FileInfo fileInfo)
//        {
//            if (fileInfo == null) throw new ArgumentNullException(nameof(fileInfo));

//            _metadataLock.EnterUpgradeableReadLock();
//            try
//            {
//                if (_metadataCache.TryGetValue(fileInfo.FullName, out var cached) &&
//                    DateTime.Now - cached.timestamp <= _metadataCacheExpiry &&
//                    fileInfo.Exists &&
//                    fileInfo.LastWriteTime <= cached.timestamp)
//                {
//                    return cached.model;
//                }

//                return UpdateFileMetadata(fileInfo);
//            }
//            finally
//            {
//                _metadataLock.ExitUpgradeableReadLock();
//            }
//        }

//        private static FileViewModel UpdateFileMetadata(FileInfo fileInfo)
//        {
//            _metadataLock.EnterWriteLock();
//            try
//            {
//                if (_metadataCache.TryGetValue(fileInfo.FullName, out var existing) &&
//                    fileInfo.Exists &&
//                    fileInfo.LastWriteTime <= existing.timestamp &&
//                    DateTime.Now - existing.timestamp <= _metadataCacheExpiry)
//                {
//                    return existing.model;
//                }

//                var fileVm = new FileViewModel(fileInfo, EntityFlags.IsFile);
//                _metadataCache[fileInfo.FullName] = (fileVm, DateTime.Now);

//                CacheUpdated?.Invoke(null, new FileCacheEventArgs(fileInfo.FullName));

//                return fileVm;
//            }
//            finally
//            {
//                _metadataLock.ExitWriteLock();
//            }
//        }

//        public static async Task<BitmapImage> GetFileIconAsync(string filePath)
//        {
//            if (!_isInitialized) throw new InvalidOperationException("Сервис иконок не инициализирован");
//            if (string.IsNullOrEmpty(filePath)) throw new ArgumentException("Некорректный путь к файлу");

//            try
//            {
//                // Учитываем настройки производительности перед загрузкой иконки
//                var perfSettings = PerformanceManager.Settings;
//                float cpuPriority = perfSettings.CpuPriority / 100f;
//                float ioPriority = perfSettings.IoPriority / 100f;

//                // Ограничиваем параллелизм на основе настроек
//                await _iconLoadSemaphore.WaitAsync();
//                try
//                {
//                    // Добавляем искусственную задержку для соблюдения IO приоритета
//                    if (ioPriority < 0.7f)
//                    {
//                        await Task.Delay((int)(10 * (1 - ioPriority)));
//                    }

//                    return await _iconService.GetIconAsync(filePath, false);
//                }
//                finally
//                {
//                    _iconLoadSemaphore.Release();
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[FileCache] Error getting icon for {filePath}: {ex.Message}");
//                return null;
//            }
//        }

//        public static async Task<BitmapImage> GetFileIconCachedAsync(string filePath)
//        {
//            try
//            {
//                if (_iconCache.TryGetValue(filePath, out var cached) &&
//                    DateTime.Now - cached.timestamp <= _iconCacheExpiry &&
//                    File.Exists(filePath))
//                {
//                    return cached.icon;
//                }

//                // Получаем настройки производительности
//                var perfSettings = PerformanceManager.Settings;
//                var storageType = StorageDetector.DetectStorageType(filePath);
//                var storageProfile = StorageCharacteristics.GetProfile(storageType);

//                // Рассчитываем приоритет загрузки
//                float priorityFactor = (perfSettings.CpuPriority + perfSettings.IoPriority) / 200f;
//                int delayMs = (int)(50 * (1 - priorityFactor));

//                if (delayMs > 0)
//                {
//                    await Task.Delay(delayMs);
//                }

//                var icon = await GetFileIconAsync(filePath);
//                if (icon != null)
//                {
//                    _iconCache[filePath] = (icon, DateTime.Now);
//                }
//                else
//                {
//                    _iconCache.TryRemove(filePath, out _);
//                }

//                return icon;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[FileCache] Error in GetFileIconCachedAsync for {filePath}: {ex.Message}");
//                return null;
//            }
//        }

//        public static void InvalidateFile(string filePath)
//        {
//            if (string.IsNullOrEmpty(filePath)) return;

//            _metadataLock.EnterWriteLock();
//            try
//            {
//                _metadataCache.TryRemove(filePath, out _);
//                _iconCache.TryRemove(filePath, out _);

//                _iconService?.InvalidateCacheForItem(filePath, false);

//                CacheInvalidated?.Invoke(null, new FileCacheEventArgs(filePath));
//            }
//            finally
//            {
//                _metadataLock.ExitWriteLock();
//            }
//        }

//        public static void ClearCache()
//        {
//            _metadataLock.EnterWriteLock();
//            try
//            {
//                int metadataCount = _metadataCache.Count;
//                int iconCount = _iconCache.Count;

//                _metadataCache.Clear();
//                _iconCache.Clear();

//                _iconService?.ClearCache();

//                Debug.WriteLine($"[FileCache] Cleared caches: {metadataCount} metadata, {iconCount} icons");

//                CacheInvalidated?.Invoke(null, new FileCacheEventArgs(null));
//            }
//            finally
//            {
//                _metadataLock.ExitWriteLock();
//            }
//        }

//        public static void CleanupExpiredEntries()
//        {
//            var now = DateTime.Now;
//            var expiredMetadata = new List<string>();
//            var expiredIcons = new List<string>();

//            // Учитываем настройки производительности при очистке
//            var perfSettings = PerformanceManager.Settings;
//            bool aggressiveCleanup = perfSettings.Profile == PerformanceProfile.Low ||
//                                   perfSettings.Profile == PerformanceProfile.PowerSaver;

//            foreach (var kvp in _metadataCache)
//            {
//                if (now - kvp.Value.timestamp > (aggressiveCleanup ?
//                    TimeSpan.FromMinutes(15) : _metadataCacheExpiry))
//                {
//                    expiredMetadata.Add(kvp.Key);
//                }
//            }

//            foreach (var kvp in _iconCache)
//            {
//                if (now - kvp.Value.timestamp > (aggressiveCleanup ?
//                    TimeSpan.FromMinutes(30) : _iconCacheExpiry))
//                {
//                    expiredIcons.Add(kvp.Key);
//                }
//            }

//            // Ограничиваем количество очищаемых элементов за раз
//            int maxCleanupPerCycle = aggressiveCleanup ? 100 : 500;
//            if (expiredMetadata.Count > maxCleanupPerCycle)
//            {
//                expiredMetadata = expiredMetadata
//                    .OrderBy(x => _metadataCache[x].timestamp)
//                    .Take(maxCleanupPerCycle)
//                    .ToList();
//            }

//            if (expiredIcons.Count > maxCleanupPerCycle)
//            {
//                expiredIcons = expiredIcons
//                    .OrderBy(x => _iconCache[x].timestamp)
//                    .Take(maxCleanupPerCycle)
//                    .ToList();
//            }

//            foreach (var key in expiredMetadata)
//            {
//                _metadataCache.TryRemove(key, out _);
//            }

//            foreach (var key in expiredIcons)
//            {
//                _iconCache.TryRemove(key, out _);
//            }

//            if (expiredMetadata.Count > 0 || expiredIcons.Count > 0)
//            {
//                Debug.WriteLine($"[FileCache] Cleaned up {expiredMetadata.Count} metadata and {expiredIcons.Count} icon entries");
//            }
//        }
//    }

//    public class FileCacheEventArgs : EventArgs
//    {
//        public string FilePath { get; }

//        public FileCacheEventArgs(string filePath)
//        {
//            FilePath = filePath;
//        }
//    }
//}



//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.UI.Xaml.Media.Imaging;

//namespace Core_FileManagement
//{
//    public static class FileCacheService
//    {
//        private static readonly ConcurrentDictionary<string, (FileViewModel model, DateTime timestamp)> _metadataCache
//            = new ConcurrentDictionary<string, (FileViewModel, DateTime)>(StringComparer.OrdinalIgnoreCase);

//        private static readonly ConcurrentDictionary<string, (BitmapImage icon, DateTime timestamp)> _iconCache
//            = new ConcurrentDictionary<string, (BitmapImage, DateTime)>(StringComparer.OrdinalIgnoreCase);

//        private static readonly TimeSpan _metadataCacheExpiry = TimeSpan.FromMinutes(30);
//        private static readonly TimeSpan _iconCacheExpiry = TimeSpan.FromMinutes(60);

//        private static readonly SemaphoreSlim _metadataLock = new SemaphoreSlim(1, 1);
//        private static readonly SemaphoreSlim _iconLock = new SemaphoreSlim(1, 1);
//        private static readonly SemaphoreSlim _iconLoadSemaphore = new SemaphoreSlim(1, 1);

//        private static IIconService _iconService;
//        private static bool _isInitialized = false;

//        public static event EventHandler<FileCacheEventArgs> CacheUpdated;
//        public static event EventHandler<FileCacheEventArgs> CacheInvalidated;

//        public static void Initialize(IIconService iconService)
//        {
//            _iconService = iconService ?? throw new ArgumentNullException(nameof(iconService));
//            _isInitialized = true;
//        }

//        // СИНХРОННЫЕ МЕТОДЫ ДЛЯ ОБРАТНОЙ СОВМЕСТИМОСТИ (оставляем как были)

//        public static FileViewModel GetFileMetadata(FileInfo fileInfo)
//        {
//            if (fileInfo == null) throw new ArgumentNullException(nameof(fileInfo));

//            _metadataLock.Wait();
//            try
//            {
//                if (_metadataCache.TryGetValue(fileInfo.FullName, out var cached) &&
//                    DateTime.Now - cached.timestamp <= _metadataCacheExpiry &&
//                    fileInfo.Exists &&
//                    fileInfo.LastWriteTime <= cached.timestamp)
//                {
//                    return cached.model;
//                }

//                if (_metadataCache.TryGetValue(fileInfo.FullName, out var existing) &&
//                    fileInfo.Exists &&
//                    fileInfo.LastWriteTime <= existing.timestamp &&
//                    DateTime.Now - existing.timestamp <= _metadataCacheExpiry)
//                {
//                    return existing.model;
//                }

//                if (!fileInfo.Exists)
//                {
//                    _metadataCache.TryRemove(fileInfo.FullName, out _);
//                    return null;
//                }

//                var fileVm = new FileViewModel(fileInfo, EntityFlags.IsFile);
//                _metadataCache[fileInfo.FullName] = (fileVm, DateTime.Now);

//                CacheUpdated?.Invoke(null, new FileCacheEventArgs(fileInfo.FullName));
//                return fileVm;
//            }
//            finally
//            {
//                _metadataLock.Release();
//            }
//        }

//        public static async Task<BitmapImage> GetFileIconAsync(string filePath)
//        {
//            if (!_isInitialized) throw new InvalidOperationException("Сервис иконок не инициализирован");
//            if (string.IsNullOrEmpty(filePath)) throw new ArgumentException("Некорректный путь к файлу");

//            try
//            {
//                // Сначала быстрая синхронная проверка
//                if (!File.Exists(filePath)) return null;

//                var perfSettings = PerformanceManager.Settings;
//                float ioPriority = perfSettings.IoPriority / 100f;

//                await _iconLoadSemaphore.WaitAsync().ConfigureAwait(false);
//                try
//                {
//                    if (ioPriority < 0.7f)
//                    {
//                        await Task.Delay((int)(10 * (1 - ioPriority))).ConfigureAwait(false);
//                    }

//                    return await _iconService.GetIconAsync(filePath, false).ConfigureAwait(false);
//                }
//                finally
//                {
//                    _iconLoadSemaphore.Release();
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[FileCache] Error getting icon for {filePath}: {ex.Message}");
//                return null;
//            }
//        }

//        public static async Task<BitmapImage> GetFileIconCachedAsync(string filePath)
//        {
//            try
//            {
//                // ВАЖНО: сначала синхронная проверка существования файла
//                if (!File.Exists(filePath))
//                {
//                    _iconCache.TryRemove(filePath, out _);
//                    return null;
//                }

//                // Быстрая проверка кэша (синхронная часть)
//                if (_iconCache.TryGetValue(filePath, out var cached) &&
//                    DateTime.Now - cached.timestamp <= _iconCacheExpiry)
//                {
//                    // Двойная проверка, что файл все еще существует
//                    if (File.Exists(filePath))
//                    {
//                        return cached.icon;
//                    }
//                    else
//                    {
//                        _iconCache.TryRemove(filePath, out _);
//                        return null;
//                    }
//                }

//                // Если нет в кэше или просрочено - загружаем асинхронно
//                var perfSettings = PerformanceManager.Settings;
//                float priorityFactor = (perfSettings.CpuPriority + perfSettings.IoPriority) / 200f;

//                if (priorityFactor < 0.8f)
//                {
//                    await Task.Delay((int)(50 * (1 - priorityFactor))).ConfigureAwait(false);
//                }

//                var icon = await GetFileIconAsync(filePath).ConfigureAwait(false);
//                if (icon != null)
//                {
//                    await _iconLock.WaitAsync().ConfigureAwait(false);
//                    try
//                    {
//                        _iconCache[filePath] = (icon, DateTime.Now);
//                    }
//                    finally
//                    {
//                        _iconLock.Release();
//                    }
//                }
//                else
//                {
//                    _iconCache.TryRemove(filePath, out _);
//                }

//                return icon;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[FileCache] Error in GetFileIconCachedAsync for {filePath}: {ex.Message}");
//                return null;
//            }
//        }

//        // Остальные методы остаются как в рабочей версии...

//        public static void InvalidateFile(string filePath)
//        {
//            if (string.IsNullOrEmpty(filePath)) return;

//            _metadataLock.Wait();
//            try
//            {
//                _metadataCache.TryRemove(filePath, out _);
//                _iconCache.TryRemove(filePath, out _);
//                _iconService?.InvalidateCacheForItem(filePath, false);
//                CacheInvalidated?.Invoke(null, new FileCacheEventArgs(filePath));
//            }
//            finally
//            {
//                _metadataLock.Release();
//            }
//        }

//        public static void ClearCache()
//        {
//            _metadataLock.Wait();
//            try
//            {
//                int metadataCount = _metadataCache.Count;
//                int iconCount = _iconCache.Count;

//                _metadataCache.Clear();
//                _iconCache.Clear();
//                _iconService?.ClearCache();

//                Debug.WriteLine($"[FileCache] Cleared caches: {metadataCount} metadata, {iconCount} icons");
//                CacheInvalidated?.Invoke(null, new FileCacheEventArgs(null));
//            }
//            finally
//            {
//                _metadataLock.Release();
//            }
//        }

//        public static void CleanupExpiredEntries()
//        {
//            var now = DateTime.Now;
//            var expiredMetadata = new List<string>();
//            var expiredIcons = new List<string>();

//            var perfSettings = PerformanceManager.Settings;
//            bool aggressiveCleanup = perfSettings.Profile == PerformanceProfile.Low ||
//                                   perfSettings.Profile == PerformanceProfile.PowerSaver;

//            foreach (var kvp in _metadataCache)
//            {
//                if (now - kvp.Value.timestamp > (aggressiveCleanup ?
//                    TimeSpan.FromMinutes(15) : _metadataCacheExpiry))
//                {
//                    expiredMetadata.Add(kvp.Key);
//                }
//            }

//            foreach (var kvp in _iconCache)
//            {
//                if (now - kvp.Value.timestamp > (aggressiveCleanup ?
//                    TimeSpan.FromMinutes(30) : _iconCacheExpiry))
//                {
//                    expiredIcons.Add(kvp.Key);
//                }
//            }

//            int maxCleanupPerCycle = aggressiveCleanup ? 100 : 500;
//            if (expiredMetadata.Count > maxCleanupPerCycle)
//            {
//                expiredMetadata = expiredMetadata.Take(maxCleanupPerCycle).ToList();
//            }

//            if (expiredIcons.Count > maxCleanupPerCycle)
//            {
//                expiredIcons = expiredIcons.Take(maxCleanupPerCycle).ToList();
//            }

//            _metadataLock.Wait();
//            try
//            {
//                foreach (var key in expiredMetadata)
//                {
//                    _metadataCache.TryRemove(key, out _);
//                }
//                foreach (var key in expiredIcons)
//                {
//                    _iconCache.TryRemove(key, out _);
//                }
//            }
//            finally
//            {
//                _metadataLock.Release();
//            }

//            if (expiredMetadata.Count > 0 || expiredIcons.Count > 0)
//            {
//                Debug.WriteLine($"[FileCache] Cleaned up {expiredMetadata.Count} metadata and {expiredIcons.Count} icon entries");
//            }
//        }

//        // Асинхронные версии методов (если нужны) можно добавить позже,
//        // но основная логика должна оставаться синхронной для совместимости
//    }

//    public class FileCacheEventArgs : EventArgs
//    {
//        public string FilePath { get; }

//        public FileCacheEventArgs(string filePath)
//        {
//            FilePath = filePath;
//        }
//    }
//}


//29 09 2025

//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.UI.Xaml.Media.Imaging;

//namespace Core_FileManagement
//{
//    public static class FileCacheService
//    {
//        private static readonly ConcurrentDictionary<string, (FileViewModel model, DateTime timestamp)> _metadataCache
//            = new ConcurrentDictionary<string, (FileViewModel, DateTime)>(StringComparer.OrdinalIgnoreCase);

//        private static readonly ConcurrentDictionary<string, (BitmapImage icon, DateTime timestamp)> _iconCache
//            = new ConcurrentDictionary<string, (BitmapImage, DateTime)>(StringComparer.OrdinalIgnoreCase);

//        private static readonly TimeSpan _metadataCacheExpiry = TimeSpan.FromMinutes(30);
//        private static readonly TimeSpan _iconCacheExpiry = TimeSpan.FromMinutes(60);

//        private static readonly SemaphoreSlim _metadataLock = new SemaphoreSlim(1, 1);
//        private static readonly SemaphoreSlim _iconLoadSemaphore = new SemaphoreSlim(1, 1);

//        private static IIconService _iconService;
//        private static bool _isInitialized = false;

//        public static event EventHandler<FileCacheEventArgs> CacheUpdated;
//        public static event EventHandler<FileCacheEventArgs> CacheInvalidated;

//        public static void Initialize(IIconService iconService)
//        {
//            _iconService = iconService ?? throw new ArgumentNullException(nameof(iconService));
//            _isInitialized = true;
//        }

//        // Синхронная версия для обратной совместимости
//        public static FileViewModel GetFileMetadata(FileInfo fileInfo)
//        {
//            return GetFileMetadataAsync(fileInfo).GetAwaiter().GetResult();
//        }

//        // Асинхронная версия - исправленная
//        public static async Task<FileViewModel> GetFileMetadataAsync(FileInfo fileInfo)
//        {
//            if (fileInfo == null) throw new ArgumentNullException(nameof(fileInfo));

//            // Сначала проверяем кэш без блокировки
//            if (_metadataCache.TryGetValue(fileInfo.FullName, out var cached) &&
//                DateTime.Now - cached.timestamp <= _metadataCacheExpiry &&
//                fileInfo.Exists &&
//                fileInfo.LastWriteTime <= cached.timestamp)
//            {
//                return cached.model;
//            }

//            // Если нет в кэше или устарело, обновляем с блокировкой
//            return await UpdateFileMetadataAsync(fileInfo);
//        }

//        private static async Task<FileViewModel> UpdateFileMetadataAsync(FileInfo fileInfo)
//        {
//            await _metadataLock.WaitAsync();
//            try
//            {
//                // Двойная проверка после получения блокировки
//                if (_metadataCache.TryGetValue(fileInfo.FullName, out var existing) &&
//                    fileInfo.Exists &&
//                    fileInfo.LastWriteTime <= existing.timestamp &&
//                    DateTime.Now - existing.timestamp <= _metadataCacheExpiry)
//                {
//                    return existing.model;
//                }

//                var fileVm = new FileViewModel(fileInfo, EntityFlags.IsFile);
//                _metadataCache[fileInfo.FullName] = (fileVm, DateTime.Now);

//                CacheUpdated?.Invoke(null, new FileCacheEventArgs(fileInfo.FullName));

//                return fileVm;
//            }
//            finally
//            {
//                _metadataLock.Release();
//            }
//        }

//        public static async Task<BitmapImage> GetFileIconAsync(string filePath)
//        {
//            if (!_isInitialized) throw new InvalidOperationException("Сервис иконок не инициализирован");
//            if (string.IsNullOrEmpty(filePath)) throw new ArgumentException("Некорректный путь к файлу");

//            try
//            {
//                // Учитываем настройки производительности перед загрузкой иконки
//                var perfSettings = PerformanceManager.Settings;
//                float cpuPriority = perfSettings.CpuPriority / 100f;
//                float ioPriority = perfSettings.IoPriority / 100f;

//                // Ограничиваем параллелизм на основе настроек
//                await _iconLoadSemaphore.WaitAsync();
//                try
//                {
//                    // Добавляем искусственную задержку для соблюдения IO приоритета
//                    if (ioPriority < 0.7f)
//                    {
//                        await Task.Delay((int)(10 * (1 - ioPriority)));
//                    }

//                    return await _iconService.GetIconAsync(filePath, false);
//                }
//                finally
//                {
//                    _iconLoadSemaphore.Release();
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[FileCache] Error getting icon for {filePath}: {ex.Message}");
//                return null;
//            }
//        }

//        public static async Task<BitmapImage> GetFileIconCachedAsync(string filePath)
//        {
//            try
//            {
//                // Проверяем кэш без блокировки сначала
//                if (_iconCache.TryGetValue(filePath, out var cached) &&
//                    DateTime.Now - cached.timestamp <= _iconCacheExpiry &&
//                    File.Exists(filePath))
//                {
//                    return cached.icon;
//                }

//                // Получаем настройки производительности
//                var perfSettings = PerformanceManager.Settings;
//                var storageType = StorageDetector.DetectStorageType(filePath);
//                var storageProfile = StorageCharacteristics.GetProfile(storageType);

//                // Рассчитываем приоритет загрузки
//                float priorityFactor = (perfSettings.CpuPriority + perfSettings.IoPriority) / 200f;
//                int delayMs = (int)(50 * (1 - priorityFactor));

//                if (delayMs > 0)
//                {
//                    await Task.Delay(delayMs);
//                }

//                var icon = await GetFileIconAsync(filePath);
//                if (icon != null)
//                {
//                    // Обновляем кэш с минимальной блокировкой
//                    _iconCache[filePath] = (icon, DateTime.Now);
//                }
//                else
//                {
//                    _iconCache.TryRemove(filePath, out _);
//                }

//                return icon;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[FileCache] Error in GetFileIconCachedAsync for {filePath}: {ex.Message}");
//                return null;
//            }
//        }

//        // Синхронная версия для обратной совместимости
//        public static void InvalidateFile(string filePath)
//        {
//            InvalidateFileAsync(filePath).GetAwaiter().GetResult();
//        }

//        // Асинхронная версия
//        public static async Task InvalidateFileAsync(string filePath)
//        {
//            if (string.IsNullOrEmpty(filePath)) return;

//            await _metadataLock.WaitAsync();
//            try
//            {
//                _metadataCache.TryRemove(filePath, out _);
//                _iconCache.TryRemove(filePath, out _);

//                _iconService?.InvalidateCacheForItem(filePath, false);

//                CacheInvalidated?.Invoke(null, new FileCacheEventArgs(filePath));
//            }
//            finally
//            {
//                _metadataLock.Release();
//            }
//        }

//        // Синхронная версия для обратной совместимости
//        public static void ClearCache()
//        {
//            ClearCacheAsync().GetAwaiter().GetResult();
//        }

//        // Асинхронная версия
//        public static async Task ClearCacheAsync()
//        {
//            await _metadataLock.WaitAsync();
//            try
//            {
//                int metadataCount = _metadataCache.Count;
//                int iconCount = _iconCache.Count;

//                _metadataCache.Clear();
//                _iconCache.Clear();

//                _iconService?.ClearCache();

//                Debug.WriteLine($"[FileCache] Cleared caches: {metadataCount} metadata, {iconCount} icons");

//                CacheInvalidated?.Invoke(null, new FileCacheEventArgs(null));
//            }
//            finally
//            {
//                _metadataLock.Release();
//            }
//        }

//        // Синхронная версия для обратной совместимости
//        public static void CleanupExpiredEntries()
//        {
//            CleanupExpiredEntriesAsync().GetAwaiter().GetResult();
//        }

//        // Асинхронная версия
//        public static async Task CleanupExpiredEntriesAsync()
//        {
//            await _metadataLock.WaitAsync();
//            try
//            {
//                var now = DateTime.Now;
//                var expiredMetadata = new List<string>();
//                var expiredIcons = new List<string>();

//                // Учитываем настройки производительности при очистке
//                var perfSettings = PerformanceManager.Settings;
//                bool aggressiveCleanup = perfSettings.Profile == PerformanceProfile.Low ||
//                                       perfSettings.Profile == PerformanceProfile.PowerSaver;

//                foreach (var kvp in _metadataCache)
//                {
//                    if (now - kvp.Value.timestamp > (aggressiveCleanup ?
//                        TimeSpan.FromMinutes(15) : _metadataCacheExpiry))
//                    {
//                        expiredMetadata.Add(kvp.Key);
//                    }
//                }

//                foreach (var kvp in _iconCache)
//                {
//                    if (now - kvp.Value.timestamp > (aggressiveCleanup ?
//                        TimeSpan.FromMinutes(30) : _iconCacheExpiry))
//                    {
//                        expiredIcons.Add(kvp.Key);
//                    }
//                }

//                // Ограничиваем количество очищаемых элементов за раз
//                int maxCleanupPerCycle = aggressiveCleanup ? 100 : 500;
//                if (expiredMetadata.Count > maxCleanupPerCycle)
//                {
//                    expiredMetadata = expiredMetadata
//                        .OrderBy(x => _metadataCache[x].timestamp)
//                        .Take(maxCleanupPerCycle)
//                        .ToList();
//                }

//                if (expiredIcons.Count > maxCleanupPerCycle)
//                {
//                    expiredIcons = expiredIcons
//                        .OrderBy(x => _iconCache[x].timestamp)
//                        .Take(maxCleanupPerCycle)
//                        .ToList();
//                }

//                foreach (var key in expiredMetadata)
//                {
//                    _metadataCache.TryRemove(key, out _);
//                }

//                foreach (var key in expiredIcons)
//                {
//                    _iconCache.TryRemove(key, out _);
//                }

//                if (expiredMetadata.Count > 0 || expiredIcons.Count > 0)
//                {
//                    Debug.WriteLine($"[FileCache] Cleaned up {expiredMetadata.Count} metadata and {expiredIcons.Count} icon entries");
//                }
//            }
//            finally
//            {
//                _metadataLock.Release();
//            }
//        }
//    }

//    public class FileCacheEventArgs : EventArgs
//    {
//        public string FilePath { get; }

//        public FileCacheEventArgs(string filePath)
//        {
//            FilePath = filePath;
//        }
//    }
//}


//0004

//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.UI.Xaml.Media.Imaging;

//namespace Core_FileManagement
//{
//    public static class FileCacheService
//    {
//        private static readonly ConcurrentDictionary<string, (FileViewModel model, DateTime timestamp)> _metadataCache
//            = new ConcurrentDictionary<string, (FileViewModel, DateTime)>(StringComparer.OrdinalIgnoreCase);

//        private static readonly ConcurrentDictionary<string, (BitmapImage icon, DateTime timestamp)> _iconCache
//            = new ConcurrentDictionary<string, (BitmapImage, DateTime)>(StringComparer.OrdinalIgnoreCase);

//        private static readonly TimeSpan _metadataCacheExpiry = TimeSpan.FromMinutes(30);
//        private static readonly TimeSpan _iconCacheExpiry = TimeSpan.FromMinutes(60);
//        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new();

//        private static IIconService _iconService;
//        private static bool _isInitialized = false;
//        private static readonly SemaphoreSlim _iconBulkLoadSemaphore = new SemaphoreSlim(3, 3);

//        public static event EventHandler<FileCacheEventArgs> CacheUpdated;
//        public static event EventHandler<FileCacheEventArgs> CacheInvalidated;

//        public static void Initialize(IIconService iconService)
//        {
//            _iconService = iconService ?? throw new ArgumentNullException(nameof(iconService));
//            _isInitialized = true;
//        }

//        // Оптимизированная асинхронная версия
//        public static async Task<FileViewModel> GetFileMetadataAsync(FileInfo fileInfo)
//        {
//            if (fileInfo == null) throw new ArgumentNullException(nameof(fileInfo));

//            // Сначала проверяем кэш без блокировки
//            if (_metadataCache.TryGetValue(fileInfo.FullName, out var cached) &&
//                IsMetadataCacheValid(fileInfo, cached))
//            {
//                return cached.model;
//            }

//            // Блокировка на уровне файла
//            var fileLock = _fileLocks.GetOrAdd(fileInfo.FullName, _ => new SemaphoreSlim(1, 1));
//            await fileLock.WaitAsync();
//            try
//            {
//                // Двойная проверка после получения блокировки
//                if (_metadataCache.TryGetValue(fileInfo.FullName, out cached) && IsMetadataCacheValid(fileInfo, cached))
//                {
//                    return cached.model;
//                }

//                var fileVm = new FileViewModel(fileInfo, EntityFlags.IsFile);
//                _metadataCache[fileInfo.FullName] = (fileVm, DateTime.Now);

//                CacheUpdated?.Invoke(null, new FileCacheEventArgs(fileInfo.FullName));

//                return fileVm;
//            }
//            finally
//            {
//                fileLock.Release();
//                _fileLocks.TryRemove(fileInfo.FullName, out _);
//            }
//        }

//        private static bool IsMetadataCacheValid(FileInfo fileInfo, (FileViewModel model, DateTime timestamp) cached)
//        {
//            return fileInfo.Exists &&
//                   fileInfo.LastWriteTime <= cached.timestamp &&
//                   DateTime.Now - cached.timestamp <= _metadataCacheExpiry;
//        }

//        // Синхронная версия для обратной совместимости
//        public static FileViewModel GetFileMetadata(FileInfo fileInfo)
//        {
//            return GetFileMetadataAsync(fileInfo).GetAwaiter().GetResult();
//        }

//        public static async Task<BitmapImage> GetFileIconAsync(string filePath)
//        {
//            if (!_isInitialized) throw new InvalidOperationException("Сервис иконок не инициализирован");
//            if (string.IsNullOrEmpty(filePath)) throw new ArgumentException("Некорректный путь к файлу");

//            await _iconBulkLoadSemaphore.WaitAsync();
//            try
//            {
//                var perfSettings = PerformanceManager.Settings;
//                float ioPriority = perfSettings.IoPriority / 100f;

//                // Более интеллектуальная задержка на основе приоритета
//                if (ioPriority < 0.5f)
//                {
//                    int delayMs = (int)(50 * (1 - ioPriority));
//                    await Task.Delay(delayMs).ConfigureAwait(false);
//                }

//                return await _iconService.GetIconAsync(filePath, false).ConfigureAwait(false);
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[FileCache] Error getting icon for {filePath}: {ex.Message}");
//                return null;
//            }
//            finally
//            {
//                _iconBulkLoadSemaphore.Release();
//            }
//        }

//        public static async Task<BitmapImage> GetFileIconCachedAsync(string filePath)
//        {
//            if (string.IsNullOrEmpty(filePath)) return null;

//            // Проверка кэша иконок
//            if (_iconCache.TryGetValue(filePath, out var cached) &&
//                DateTime.Now - cached.timestamp <= _iconCacheExpiry &&
//                File.Exists(filePath))
//            {
//                return cached.icon;
//            }

//            try
//            {
//                var perfSettings = PerformanceManager.Settings;
//                var storageType = StorageDetector.DetectStorageType(filePath);

//                // Приоритет загрузки на основе настроек и типа хранилища
//                float priorityFactor = (perfSettings.CpuPriority + perfSettings.IoPriority) / 200f;
//                int delayMs = CalculateIconLoadDelay(priorityFactor, storageType);

//                if (delayMs > 0)
//                {
//                    await Task.Delay(delayMs).ConfigureAwait(false);
//                }

//                // Прямой вызов IIconService без лишней асинхронной обертки
//                var icon = await _iconService.GetIconAsync(filePath, false).ConfigureAwait(false);

//                if (icon != null)
//                {
//                    _iconCache[filePath] = (icon, DateTime.Now);
//                }
//                else
//                {
//                    _iconCache.TryRemove(filePath, out _);
//                }

//                return icon;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[FileCache] Error in GetFileIconCachedAsync for {filePath}: {ex.Message}");
//                return null;
//            }
//        }

//        private static int CalculateIconLoadDelay(float priorityFactor, StorageType storageType)
//        {
//            int baseDelay = (int)(30 * (1 - priorityFactor));

//            // Учитываем тип хранилища
//            return storageType switch
//            {
//                StorageType.Network or StorageType.USB => baseDelay * 2,
//                StorageType.HDD => baseDelay,
//                StorageType.SSD => baseDelay / 2,
//                StorageType.NVMe or StorageType.RAMDisk => 0,
//                _ => baseDelay
//            };
//        }

//        public static Task InvalidateFileAsync(string filePath)
//        {
//            if (string.IsNullOrEmpty(filePath)) return Task.CompletedTask;

//            _metadataCache.TryRemove(filePath, out _);
//            _iconCache.TryRemove(filePath, out _);

//            _iconService?.InvalidateCacheForItem(filePath, false);

//            CacheInvalidated?.Invoke(null, new FileCacheEventArgs(filePath));

//            return Task.CompletedTask;
//        }

//        public static Task ClearCacheAsync()
//        {
//            int metadataCount = _metadataCache.Count;
//            int iconCount = _iconCache.Count;

//            _metadataCache.Clear();
//            _iconCache.Clear();

//            _iconService?.ClearCache();

//            Debug.WriteLine($"[FileCache] Cleared caches: {metadataCount} metadata, {iconCount} icons");

//            CacheInvalidated?.Invoke(null, new FileCacheEventArgs(null));

//            return Task.CompletedTask;
//        }

//        public static Task CleanupExpiredEntriesAsync()
//        {
//            var now = DateTime.Now;
//            var expiredMetadata = new List<string>();
//            var expiredIcons = new List<string>();

//            // Учитываем настройки производительности при очистке
//            var perfSettings = PerformanceManager.Settings;
//            bool aggressiveCleanup = perfSettings.Profile == PerformanceProfile.Low ||
//                                   perfSettings.Profile == PerformanceProfile.PowerSaver;

//            foreach (var kvp in _metadataCache)
//            {
//                if (now - kvp.Value.timestamp > (aggressiveCleanup ?
//                    TimeSpan.FromMinutes(15) : _metadataCacheExpiry))
//                {
//                    expiredMetadata.Add(kvp.Key);
//                }
//            }

//            foreach (var kvp in _iconCache)
//            {
//                if (now - kvp.Value.timestamp > (aggressiveCleanup ?
//                    TimeSpan.FromMinutes(30) : _iconCacheExpiry))
//                {
//                    expiredIcons.Add(kvp.Key);
//                }
//            }

//            // Ограничиваем количество очищаемых элементов за раз
//            int maxCleanupPerCycle = aggressiveCleanup ? 100 : 500;
//            if (expiredMetadata.Count > maxCleanupPerCycle)
//            {
//                expiredMetadata = expiredMetadata
//                    .OrderBy(x => _metadataCache[x].timestamp)
//                    .Take(maxCleanupPerCycle)
//                    .ToList();
//            }

//            if (expiredIcons.Count > maxCleanupPerCycle)
//            {
//                expiredIcons = expiredIcons
//                    .OrderBy(x => _iconCache[x].timestamp)
//                    .Take(maxCleanupPerCycle)
//                    .ToList();
//            }

//            foreach (var key in expiredMetadata)
//            {
//                _metadataCache.TryRemove(key, out _);
//            }

//            foreach (var key in expiredIcons)
//            {
//                _iconCache.TryRemove(key, out _);
//            }

//            if (expiredMetadata.Count > 0 || expiredIcons.Count > 0)
//            {
//                Debug.WriteLine($"[FileCache] Cleaned up {expiredMetadata.Count} metadata and {expiredIcons.Count} icon entries");
//            }

//            return Task.CompletedTask;
//        }

//        // Синхронные версии для обратной совместимости
//        public static void InvalidateFile(string filePath)
//        {
//            InvalidateFileAsync(filePath).GetAwaiter().GetResult();
//        }

//        public static void ClearCache()
//        {
//            ClearCacheAsync().GetAwaiter().GetResult();
//        }

//        public static void CleanupExpiredEntries()
//        {
//            CleanupExpiredEntriesAsync().GetAwaiter().GetResult();
//        }
//    }

//    public class FileCacheEventArgs : EventArgs
//    {
//        public string FilePath { get; }

//        public FileCacheEventArgs(string filePath)
//        {
//            FilePath = filePath;
//        }
//    }
//}

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Core_FileManagement
{
    public static class FileCacheService
    {
        private static readonly ConcurrentDictionary<string, FileViewModel> _metadataCache = new(StringComparer.OrdinalIgnoreCase);
        private static IIconService _iconService;

        public static event EventHandler<FileCacheEventArgs> CacheUpdated;
        public static event EventHandler<FileCacheEventArgs> CacheInvalidated;

        public static void Initialize(IIconService iconService)
        {
            _iconService = iconService ?? throw new ArgumentNullException(nameof(iconService));
        }

        public static FileViewModel GetFileMetadata(FileInfo fileInfo)
        {
            if (fileInfo == null) throw new ArgumentNullException(nameof(fileInfo));

            string filePath = fileInfo.FullName;

            // Простая проверка кэша
            if (_metadataCache.TryGetValue(filePath, out var cached) &&
                fileInfo.Exists &&
                fileInfo.LastWriteTime <= DateTime.Now.AddMinutes(-5))
            {
                return cached;
            }

            // Создаем новую модель
            var fileVm = new FileViewModel(fileInfo, EntityFlags.IsFile);
            _metadataCache[filePath] = fileVm;

            CacheUpdated?.Invoke(null, new FileCacheEventArgs(filePath));
            return fileVm;
        }
        public static async Task<FileViewModel> GetFileMetadataAsync(FileInfo fileInfo)
        {
            return await Task.Run(() => GetFileMetadata(fileInfo));
        }
        public static async Task<BitmapImage> GetFileIconAsync(string filePath)
        {
            if (_iconService == null)
                throw new InvalidOperationException("Icon service not initialized");

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("Invalid file path");

            try
            {
                return await _iconService.GetIconAsync(filePath, false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FileCache] Error getting icon for {filePath}: {ex.Message}");
                return null;
            }
        }

        public static Task InvalidateFileAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return Task.CompletedTask;

            _metadataCache.TryRemove(filePath, out _);

            // Для файлов ВСЕГДА передаем false
            _iconService?.InvalidateCache(filePath, false);

            CacheInvalidated?.Invoke(null, new FileCacheEventArgs(filePath));
            return Task.CompletedTask;
        }

        public static void ClearCache()
        {
            _metadataCache.Clear();
            _iconService?.ClearCache();
        }

    }

    public class FileCacheEventArgs : EventArgs
    {
        public string FilePath { get; }
        public FileCacheEventArgs(string filePath) => FilePath = filePath;
    }
}