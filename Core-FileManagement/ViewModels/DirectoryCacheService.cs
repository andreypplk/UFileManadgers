//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;

//namespace Core_FileManagement
//{
//    public static class DirectoryCacheService
//    {
//        private static readonly ConcurrentDictionary<string, (DateTime timestamp, List<FileEntityViewModel> items)> _cache
//            = new ConcurrentDictionary<string, (DateTime, List<FileEntityViewModel>)>(StringComparer.OrdinalIgnoreCase);

//        private static readonly TimeSpan _cacheLifetime = TimeSpan.FromMinutes(5);
//        private static readonly ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();

//        public static List<FileEntityViewModel> GetDirectoryContent(string path, ParallelOptions parallelOptions)
//        {
//            if (string.IsNullOrEmpty(path))
//                throw new ArgumentException("Path cannot be null or empty", nameof(path));

//            _cacheLock.EnterUpgradeableReadLock();
//            try
//            {
//                if (_cache.TryGetValue(path, out var cached) &&
//                    DateTime.Now - cached.timestamp <= _cacheLifetime)
//                {
//                    return cached.items;
//                }

//                _cacheLock.EnterWriteLock();
//                try
//                {
//                    var items = LoadDirectoryItems(path, parallelOptions);
//                    _cache.AddOrUpdate(path,
//                        (DateTime.Now, items),
//                        (key, old) => (DateTime.Now, items));

//                    return items;
//                }
//                finally
//                {
//                    _cacheLock.ExitWriteLock();
//                }
//            }
//            finally
//            {
//                _cacheLock.ExitUpgradeableReadLock();
//            }
//        }

//        private static List<FileEntityViewModel> LoadDirectoryItems(string path, ParallelOptions parallelOptions)
//        {
//            var items = new List<FileEntityViewModel>();

//            try
//            {
//                var dirInfo = new DirectoryInfo(path);
//                if (!dirInfo.Exists) return items;

//                var storageType = StorageDetector.DetectStorageType(path);
//                var storageProfile = StorageCharacteristics.GetProfile(storageType);
//                int optimalThreads = parallelOptions.MaxDegreeOfParallelism;
//                bool shouldUseParallel = storageProfile.ParallelismFactor > 0.5f && optimalThreads > 1;
//                int itemCount = 0;

//                try
//                {
//                    itemCount = dirInfo.GetFileSystemInfos().Length;
//                }
//                catch
//                {
//                    shouldUseParallel = false;
//                }

//                shouldUseParallel = shouldUseParallel && itemCount > 100;

//                if (shouldUseParallel)
//                {
//                    var fileSystemInfos = dirInfo.GetFileSystemInfos()
//                        .OrderBy(fsi => fsi.Name, StringComparer.OrdinalIgnoreCase)
//                        .ToArray();

//                    var directories = new ConcurrentBag<DirectoryViewModel>();
//                    var files = new ConcurrentBag<FileViewModel>();

//                    Parallel.ForEach(fileSystemInfos, parallelOptions, fsi =>
//                    {
//                        try
//                        {
//                            if (fsi is DirectoryInfo dir)
//                            {
//                                var flags = EntityFlags.IsDirectory;
//                                if (dir.Attributes.HasFlag(FileAttributes.Hidden))
//                                    flags |= EntityFlags.IsHidden;
//                                if (dir.Attributes.HasFlag(FileAttributes.System))
//                                    flags |= EntityFlags.IsSystem;

//                                directories.Add(new DirectoryViewModel(dir, flags));
//                            }
//                            else if (fsi is FileInfo file)
//                            {
//                                var flags = EntityFlags.IsFile;
//                                if (file.Attributes.HasFlag(FileAttributes.Hidden))
//                                    flags |= EntityFlags.IsHidden;
//                                if (file.Attributes.HasFlag(FileAttributes.System))
//                                    flags |= EntityFlags.IsSystem;
//                                if (file.Attributes.HasFlag(FileAttributes.ReadOnly))
//                                    flags |= EntityFlags.IsReadOnly;

//                                files.Add(new FileViewModel(file, flags));
//                            }
//                        }
//                        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
//                        {
//                            Debug.WriteLine($"[Cache] Error processing item: {fsi?.FullName} - {ex.Message}");
//                        }
//                    });

//                    items.AddRange(directories.OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase));
//                    items.AddRange(files.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase));
//                }
//                else
//                {
//                    foreach (var fsi in dirInfo.GetFileSystemInfos()
//                        .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase))
//                    {
//                        try
//                        {
//                            if (fsi is DirectoryInfo dir)
//                            {
//                                var flags = EntityFlags.IsDirectory;
//                                if (dir.Attributes.HasFlag(FileAttributes.Hidden))
//                                    flags |= EntityFlags.IsHidden;
//                                if (dir.Attributes.HasFlag(FileAttributes.System))
//                                    flags |= EntityFlags.IsSystem;

//                                items.Add(new DirectoryViewModel(dir, flags));
//                            }
//                            else if (fsi is FileInfo file)
//                            {
//                                var flags = EntityFlags.IsFile;
//                                if (file.Attributes.HasFlag(FileAttributes.Hidden))
//                                    flags |= EntityFlags.IsHidden;
//                                if (file.Attributes.HasFlag(FileAttributes.System))
//                                    flags |= EntityFlags.IsSystem;
//                                if (file.Attributes.HasFlag(FileAttributes.ReadOnly))
//                                    flags |= EntityFlags.IsReadOnly;

//                                items.Add(new FileViewModel(file, flags));
//                            }
//                        }
//                        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
//                        {
//                            Debug.WriteLine($"[Cache] Error loading item: {fsi.FullName} - {ex.Message}");
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[Cache] Critical error for {path}: {ex.Message}");
//            }

//            return items;
//        }

//        private static void ProcessFileSystemInfo(FileSystemInfo fsi,
//            ConcurrentBag<DirectoryViewModel> directories,
//            ConcurrentBag<FileViewModel> files)
//        {
//            if (fsi is DirectoryInfo dir)
//            {
//                var flags = EntityFlags.IsDirectory;
//                if (dir.Attributes.HasFlag(FileAttributes.Hidden))
//                    flags |= EntityFlags.IsHidden;
//                if (dir.Attributes.HasFlag(FileAttributes.System))
//                    flags |= EntityFlags.IsSystem;

//                directories.Add(new DirectoryViewModel(dir, flags));
//            }
//            else if (fsi is FileInfo file)
//            {
//                var flags = EntityFlags.IsFile;
//                if (file.Attributes.HasFlag(FileAttributes.Hidden))
//                    flags |= EntityFlags.IsHidden;
//                if (file.Attributes.HasFlag(FileAttributes.System))
//                    flags |= EntityFlags.IsSystem;
//                if (file.Attributes.HasFlag(FileAttributes.ReadOnly))
//                    flags |= EntityFlags.IsReadOnly;

//                files.Add(new FileViewModel(file, flags));
//            }
//        }

//        private static void ProcessFileSystemInfo(FileSystemInfo fsi, List<FileEntityViewModel> items)
//        {
//            if (fsi is DirectoryInfo dir)
//            {
//                var flags = EntityFlags.IsDirectory;
//                if (dir.Attributes.HasFlag(FileAttributes.Hidden))
//                    flags |= EntityFlags.IsHidden;
//                if (dir.Attributes.HasFlag(FileAttributes.System))
//                    flags |= EntityFlags.IsSystem;

//                items.Add(new DirectoryViewModel(dir, flags));
//            }
//            else if (fsi is FileInfo file)
//            {
//                var flags = EntityFlags.IsFile;
//                if (file.Attributes.HasFlag(FileAttributes.Hidden))
//                    flags |= EntityFlags.IsHidden;
//                if (file.Attributes.HasFlag(FileAttributes.System))
//                    flags |= EntityFlags.IsSystem;
//                if (file.Attributes.HasFlag(FileAttributes.ReadOnly))
//                    flags |= EntityFlags.IsReadOnly;

//                items.Add(new FileViewModel(file, flags));
//            }
//        }

//        public static void InvalidateCache(string path = null)
//        {
//            _cacheLock.EnterWriteLock();
//            try
//            {
//                if (path == null)
//                    _cache.Clear();
//                else
//                    _cache.TryRemove(path, out _);
//            }
//            finally
//            {
//                _cacheLock.ExitWriteLock();
//            }
//        }

//        public static async Task PreloadDirectoryAsync(string path)
//        {
//            if (string.IsNullOrEmpty(path))
//                return;

//            try
//            {
//                // Получаем текущие настройки производительности
//                var perfSettings = PerformanceManager.Settings;
//                var storageType = StorageDetector.DetectStorageType(path);
//                var storageProfile = StorageCharacteristics.GetProfile(storageType);

//                // Рассчитываем оптимальные параметры
//                float cpuPriority = perfSettings.CpuPriority / 100f;
//                float ioPriority = perfSettings.IoPriority / 100f;
//                int maxThreads = perfSettings.MaxThreads;
//                int maxCores = perfSettings.MaxCores;

//                // Определяем степень параллелизма
//                int degreeOfParallelism = Math.Max(1,
//                    Math.Min(
//                        (int)(storageProfile.PreferredThreads * cpuPriority * ioPriority),
//                        maxThreads
//                    )
//                );

//                var parallelOptions = new ParallelOptions
//                {
//                    MaxDegreeOfParallelism = degreeOfParallelism
//                };

//                // Добавляем задержку для соблюдения IO приоритета
//                if (ioPriority < 0.5f)
//                {
//                    await Task.Delay((int)(100 * (1 - ioPriority)));
//                }

//                await Task.Run(() =>
//                {
//                    try
//                    {
//                        // Используем перегруженную версию с ParallelOptions
//                        GetDirectoryContent(path, parallelOptions);
//                    }
//                    catch (Exception ex)
//                    {
//                        Debug.WriteLine($"[Preload] Error during parallel load: {ex.Message}");
//                    }
//                }).ConfigureAwait(false);
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[Preload] Error preloading {path}: {ex.Message}");
//            }
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

//namespace Core_FileManagement
//{
//    public static class DirectoryCacheService
//    {
//        private static readonly ConcurrentDictionary<string, (DateTime timestamp, List<FileEntityViewModel> items)> _cache
//            = new ConcurrentDictionary<string, (DateTime, List<FileEntityViewModel>)>(StringComparer.OrdinalIgnoreCase);

//        private static readonly TimeSpan _cacheLifetime = TimeSpan.FromMinutes(5);
//        private static readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1, 1);

//        public static async Task<List<FileEntityViewModel>> GetDirectoryContentAsync(string path, ParallelOptions parallelOptions)
//        {
//            if (string.IsNullOrEmpty(path))
//                throw new ArgumentException("Path cannot be null or empty", nameof(path));

//            await _cacheLock.WaitAsync().ConfigureAwait(false);
//            try
//            {
//                if (_cache.TryGetValue(path, out var cached) &&
//                    DateTime.Now - cached.timestamp <= _cacheLifetime)
//                {
//                    return cached.items;
//                }

//                var items = await LoadDirectoryItemsAsync(path, parallelOptions).ConfigureAwait(false);
//                _cache.AddOrUpdate(path,
//                    (DateTime.Now, items),
//                    (key, old) => (DateTime.Now, items));

//                return items;
//            }
//            finally
//            {
//                _cacheLock.Release();
//            }
//        }

//        private static async Task<List<FileEntityViewModel>> LoadDirectoryItemsAsync(string path, ParallelOptions parallelOptions)
//        {
//            var items = new List<FileEntityViewModel>();

//            try
//            {
//                var dirInfo = new DirectoryInfo(path);
//                if (!dirInfo.Exists) return items;

//                var storageType = StorageDetector.DetectStorageType(path);
//                var storageProfile = StorageCharacteristics.GetProfile(storageType);
//                int optimalThreads = parallelOptions.MaxDegreeOfParallelism;
//                bool shouldUseParallel = storageProfile.ParallelismFactor > 0.5f && optimalThreads > 1;
//                int itemCount = 0;

//                try
//                {
//                    itemCount = dirInfo.GetFileSystemInfos().Length;
//                }
//                catch
//                {
//                    shouldUseParallel = false;
//                }

//                shouldUseParallel = shouldUseParallel && itemCount > 100;

//                if (shouldUseParallel)
//                {
//                    var fileSystemInfos = dirInfo.GetFileSystemInfos()
//                        .OrderBy(fsi => fsi.Name, StringComparer.OrdinalIgnoreCase)
//                        .ToArray();

//                    var directories = new ConcurrentBag<DirectoryViewModel>();
//                    var files = new ConcurrentBag<FileViewModel>();

//                    var tasks = new List<Task>();
//                    var semaphore = new SemaphoreSlim(optimalThreads, optimalThreads);

//                    foreach (var fsi in fileSystemInfos)
//                    {
//                        await semaphore.WaitAsync().ConfigureAwait(false);

//                        tasks.Add(Task.Run(async () =>
//                        {
//                            try
//                            {
//                                await ProcessFileSystemInfoAsync(fsi, directories, files).ConfigureAwait(false);
//                            }
//                            finally
//                            {
//                                semaphore.Release();
//                            }
//                        }));
//                    }

//                    await Task.WhenAll(tasks).ConfigureAwait(false);

//                    items.AddRange(directories.OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase));
//                    items.AddRange(files.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase));
//                }
//                else
//                {
//                    foreach (var fsi in dirInfo.GetFileSystemInfos()
//                        .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase))
//                    {
//                        try
//                        {
//                            await ProcessFileSystemInfoAsync(fsi, items).ConfigureAwait(false);
//                        }
//                        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
//                        {
//                            Debug.WriteLine($"[Cache] Error loading item: {fsi.FullName} - {ex.Message}");
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[Cache] Critical error for {path}: {ex.Message}");
//            }

//            return items;
//        }

//        private static async Task ProcessFileSystemInfoAsync(FileSystemInfo fsi,
//            ConcurrentBag<DirectoryViewModel> directories,
//            ConcurrentBag<FileViewModel> files)
//        {
//            await Task.Yield(); // Даем возможность прервать синхронное выполнение

//            if (fsi is DirectoryInfo dir)
//            {
//                var flags = EntityFlags.IsDirectory;
//                if (dir.Attributes.HasFlag(FileAttributes.Hidden))
//                    flags |= EntityFlags.IsHidden;
//                if (dir.Attributes.HasFlag(FileAttributes.System))
//                    flags |= EntityFlags.IsSystem;

//                directories.Add(new DirectoryViewModel(dir, flags));
//            }
//            else if (fsi is FileInfo file)
//            {
//                var flags = EntityFlags.IsFile;
//                if (file.Attributes.HasFlag(FileAttributes.Hidden))
//                    flags |= EntityFlags.IsHidden;
//                if (file.Attributes.HasFlag(FileAttributes.System))
//                    flags |= EntityFlags.IsSystem;
//                if (file.Attributes.HasFlag(FileAttributes.ReadOnly))
//                    flags |= EntityFlags.IsReadOnly;

//                files.Add(new FileViewModel(file, flags));
//            }
//        }

//        private static async Task ProcessFileSystemInfoAsync(FileSystemInfo fsi, List<FileEntityViewModel> items)
//        {
//            await Task.Yield(); // Даем возможность прервать синхронное выполнение

//            if (fsi is DirectoryInfo dir)
//            {
//                var flags = EntityFlags.IsDirectory;
//                if (dir.Attributes.HasFlag(FileAttributes.Hidden))
//                    flags |= EntityFlags.IsHidden;
//                if (dir.Attributes.HasFlag(FileAttributes.System))
//                    flags |= EntityFlags.IsSystem;

//                items.Add(new DirectoryViewModel(dir, flags));
//            }
//            else if (fsi is FileInfo file)
//            {
//                var flags = EntityFlags.IsFile;
//                if (file.Attributes.HasFlag(FileAttributes.Hidden))
//                    flags |= EntityFlags.IsHidden;
//                if (file.Attributes.HasFlag(FileAttributes.System))
//                    flags |= EntityFlags.IsSystem;
//                if (file.Attributes.HasFlag(FileAttributes.ReadOnly))
//                    flags |= EntityFlags.IsReadOnly;

//                items.Add(new FileViewModel(file, flags));
//            }
//        }

//        public static async Task InvalidateCacheAsync(string path = null)
//        {
//            await _cacheLock.WaitAsync().ConfigureAwait(false);
//            try
//            {
//                if (path == null)
//                    _cache.Clear();
//                else
//                    _cache.TryRemove(path, out _);
//            }
//            finally
//            {
//                _cacheLock.Release();
//            }
//        }

//        public static async Task PreloadDirectoryAsync(string path)
//        {
//            if (string.IsNullOrEmpty(path))
//                return;

//            try
//            {
//                // Получаем текущие настройки производительности
//                var perfSettings = PerformanceManager.Settings;
//                var storageType = StorageDetector.DetectStorageType(path);
//                var storageProfile = StorageCharacteristics.GetProfile(storageType);

//                // Рассчитываем оптимальные параметры
//                float cpuPriority = perfSettings.CpuPriority / 100f;
//                float ioPriority = perfSettings.IoPriority / 100f;
//                int maxThreads = perfSettings.MaxThreads;
//                int maxCores = perfSettings.MaxCores;

//                // Определяем степень параллелизма
//                int degreeOfParallelism = Math.Max(1,
//                    Math.Min(
//                        (int)(storageProfile.PreferredThreads * cpuPriority * ioPriority),
//                        maxThreads
//                    )
//                );

//                var parallelOptions = new ParallelOptions
//                {
//                    MaxDegreeOfParallelism = degreeOfParallelism
//                };

//                // Добавляем задержку для соблюдения IO приоритета
//                if (ioPriority < 0.5f)
//                {
//                    await Task.Delay((int)(100 * (1 - ioPriority))).ConfigureAwait(false);
//                }

//                // Используем асинхронную версию
//                await GetDirectoryContentAsync(path, parallelOptions).ConfigureAwait(false);
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[Preload] Error preloading {path}: {ex.Message}");
//            }
//        }

//        // Сохраняем синхронную версию для обратной совместимости
//        public static List<FileEntityViewModel> GetDirectoryContent(string path, ParallelOptions parallelOptions)
//        {
//            return GetDirectoryContentAsync(path, parallelOptions).GetAwaiter().GetResult();
//        }

//        // Сохраняем синхронную версию для обратной совместимости
//        public static void InvalidateCache(string path = null)
//        {
//            InvalidateCacheAsync(path).GetAwaiter().GetResult();
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

//namespace Core_FileManagement
//{
//    public static class DirectoryCacheService
//    {
//        private record CacheStrategy
//        {
//            public bool UseFastCache { get; init; }
//            public TimeSpan FastCacheExpiry { get; init; }
//            public TimeSpan MainCacheExpiry { get; init; }
//        }

//        private static readonly ConcurrentDictionary<string, (DateTime timestamp, List<FileEntityViewModel> items)> _cache
//            = new ConcurrentDictionary<string, (DateTime, List<FileEntityViewModel>)>(StringComparer.OrdinalIgnoreCase);

//        private static readonly TimeSpan _cacheLifetime = TimeSpan.FromMinutes(5);
//        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _directoryLocks =
//            new ConcurrentDictionary<string, SemaphoreSlim>();

//        // Быстрый кэш для часто запрашиваемых директорий
//        private static readonly ConcurrentDictionary<string, (DateTime timestamp, List<FileEntityViewModel> items)> _fastCache
//            = new ConcurrentDictionary<string, (DateTime, List<FileEntityViewModel>)>(StringComparer.OrdinalIgnoreCase);
//        private static readonly TimeSpan _fastCacheLifetime = TimeSpan.FromMinutes(1);

//        public static async Task<List<FileEntityViewModel>> GetDirectoryContentAsync(string path, ParallelOptions parallelOptions)
//        {
//            if (string.IsNullOrEmpty(path))
//                throw new ArgumentException("Path cannot be null or empty", nameof(path));

//            var storageType = StorageDetector.DetectStorageType(path);

//            // РАЗНЫЕ СТРАТЕГИИ КЭШИРОВАНИЯ ДЛЯ РАЗНЫХ ТИПОВ ХРАНИЛИЩ
//            var cacheStrategy = GetCacheStrategy(storageType);

//            if (cacheStrategy.UseFastCache && _fastCache.TryGetValue(path, out var fastCached) &&
//                DateTime.Now - fastCached.timestamp <= cacheStrategy.FastCacheExpiry)
//            {
//                return fastCached.items;
//            }

//            var dirLock = _directoryLocks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
//            await dirLock.WaitAsync().ConfigureAwait(false);
//            try
//            {
//                if (_cache.TryGetValue(path, out var cached) &&
//                    DateTime.Now - cached.timestamp <= cacheStrategy.MainCacheExpiry)
//                {
//                    // Обновляем быстрый кэш для часто используемых путей
//                    if (cacheStrategy.UseFastCache)
//                        _fastCache.AddOrUpdate(path, (DateTime.Now, cached.items), (k, v) => (DateTime.Now, cached.items));

//                    return cached.items;
//                }

//                var items = await LoadDirectoryItemsOptimizedAsync(path, parallelOptions).ConfigureAwait(false);

//                // Обновляем кэши с учетом стратегии
//                _cache.AddOrUpdate(path, (DateTime.Now, items), (key, old) => (DateTime.Now, items));

//                if (cacheStrategy.UseFastCache)
//                    _fastCache.AddOrUpdate(path, (DateTime.Now, items), (key, old) => (DateTime.Now, items));

//                return items;
//            }
//            finally
//            {
//                dirLock.Release();
//                _directoryLocks.TryRemove(path, out _);
//            }
//        }

//        // Добавляем новый метод
//        private static CacheStrategy GetCacheStrategy(StorageType storageType)
//        {
//            return storageType switch
//            {
//                StorageType.SSD or StorageType.NVMe => new CacheStrategy
//                {
//                    UseFastCache = true,
//                    FastCacheExpiry = TimeSpan.FromMinutes(2),
//                    MainCacheExpiry = TimeSpan.FromMinutes(10)
//                },
//                StorageType.HDD => new CacheStrategy
//                {
//                    UseFastCache = false, // HDD медленные, не тратим время на быстрый кэш
//                    MainCacheExpiry = TimeSpan.FromMinutes(5) // Короткий кэш для HDD
//                },
//                StorageType.Network => new CacheStrategy
//                {
//                    UseFastCache = true,
//                    FastCacheExpiry = TimeSpan.FromMinutes(5),
//                    MainCacheExpiry = TimeSpan.FromMinutes(15) // Долгий кэш для сетевых путей
//                },
//                _ => new CacheStrategy
//                {
//                    UseFastCache = true,
//                    FastCacheExpiry = TimeSpan.FromMinutes(1),
//                    MainCacheExpiry = TimeSpan.FromMinutes(5)
//                }
//            };
//        }

//        private static async Task<List<FileEntityViewModel>> LoadDirectoryItemsOptimizedAsync(string path, ParallelOptions parallelOptions)
//        {
//            var items = new List<FileEntityViewModel>();

//            try
//            {
//                var dirInfo = new DirectoryInfo(path);
//                if (!dirInfo.Exists) return items;

//                var storageType = StorageDetector.DetectStorageType(path);
//                var storageProfile = StorageCharacteristics.GetProfile(storageType);

//                // БЫСТРАЯ ПРОВЕРКА: если файлов больше 1000, используем упрощенную загрузку
//                var estimatedCount = QuickFileCountEstimate(dirInfo);
//                if (estimatedCount > 1000)
//                {
//                    return await LoadLargeDirectoryOptimized(dirInfo, storageType, parallelOptions);
//                }

//                // Стандартная загрузка для маленьких/средних папок
//                int optimalThreads = CalculateOptimalThreadCount(storageProfile, parallelOptions);
//                bool shouldUseParallel = ShouldUseParallelProcessing(dirInfo, storageProfile, optimalThreads);

//                if (shouldUseParallel)
//                {
//                    await ProcessDirectoryParallelOptimized(dirInfo, items, optimalThreads, parallelOptions.CancellationToken);
//                }
//                else
//                {
//                    await ProcessDirectorySequentialOptimized(dirInfo, items, parallelOptions.CancellationToken);
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[Cache] Critical error for {path}: {ex.Message}");
//            }

//            return items;
//        }

//        private static int CalculateOptimalThreadCount(StorageProfile storageProfile, ParallelOptions parallelOptions)
//        {
//            // Учитываем настройки производительности и тип хранилища
//            var perfSettings = PerformanceManager.Settings;
//            float cpuPriority = perfSettings.CpuPriority / 100f;
//            float ioPriority = perfSettings.IoPriority / 100f;

//            int baseThreads = Math.Min(
//                parallelOptions.MaxDegreeOfParallelism,
//                PerformanceManager.CalculateOptimalThreadCount()
//            );

//            // Корректируем на основе приоритетов и типа хранилища
//            float adjustmentFactor = storageProfile.ParallelismFactor * cpuPriority * ioPriority;
//            int adjustedThreads = Math.Max(1, (int)(baseThreads * adjustmentFactor));

//            return Math.Min(adjustedThreads, parallelOptions.MaxDegreeOfParallelism);
//        }

//        private static bool ShouldUseParallelProcessing(DirectoryInfo dirInfo, StorageProfile storageProfile, int optimalThreads)
//        {
//            if (optimalThreads <= 1) return false;
//            if (storageProfile.ParallelismFactor <= 0.3f) return false;

//            try
//            {
//                // Быстрая оценка количества элементов без полного перечисления
//                var estimatedCount = dirInfo.EnumerateFileSystemInfos().Take(201).Count();
//                return estimatedCount > 100 && estimatedCount < 10000; // Ограничение для очень больших директорий
//            }
//            catch
//            {
//                return false;
//            }
//        }

//        private static async Task ProcessDirectoryParallelOptimized(DirectoryInfo dirInfo,
//            List<FileEntityViewModel> items, int optimalThreads, CancellationToken cancellationToken)
//        {
//            FileSystemInfo[] fileSystemInfos;
//            try
//            {
//                fileSystemInfos = dirInfo.GetFileSystemInfos();
//                if (fileSystemInfos.Length == 0) return;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[Cache] Error getting file system infos: {ex.Message}");
//                return;
//            }

//            // Используем более эффективный Parallel.ForEachAsync
//            var concurrentItems = new ConcurrentBag<FileEntityViewModel>();

//            await Parallel.ForEachAsync(
//                fileSystemInfos,
//                new ParallelOptions
//                {
//                    MaxDegreeOfParallelism = optimalThreads,
//                    CancellationToken = cancellationToken
//                },
//                async (fsi, ct) =>
//                {
//                    try
//                    {
//                        var item = await CreateFileSystemItemAsync(fsi, ct).ConfigureAwait(false);
//                        if (item != null)
//                            concurrentItems.Add(item);
//                    }
//                    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
//                    {
//                        Debug.WriteLine($"[Cache] Error processing {fsi.FullName}: {ex.Message}");
//                    }
//                });

//            // Сортировка после параллельной обработки (более эффективно)
//            items.AddRange(concurrentItems.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase));
//        }

//        private static async Task ProcessDirectorySequentialOptimized(DirectoryInfo dirInfo,
//            List<FileEntityViewModel> items, CancellationToken cancellationToken)
//        {
//            FileSystemInfo[] fileSystemInfos;
//            try
//            {
//                fileSystemInfos = dirInfo.GetFileSystemInfos()
//                    .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
//                    .ToArray();
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[Cache] Error getting file system infos: {ex.Message}");
//                return;
//            }

//            // Пакетная обработка для лучшей производительности
//            foreach (var fsi in fileSystemInfos)
//            {
//                cancellationToken.ThrowIfCancellationRequested();

//                try
//                {
//                    var item = await CreateFileSystemItemAsync(fsi, cancellationToken).ConfigureAwait(false);
//                    if (item != null)
//                        items.Add(item);
//                }
//                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
//                {
//                    Debug.WriteLine($"[Cache] Error loading item: {fsi.FullName} - {ex.Message}");
//                }
//            }
//        }

//        private static async Task<FileEntityViewModel> CreateFileSystemItemAsync(FileSystemInfo fsi, CancellationToken cancellationToken)
//        {
//            await Task.Yield(); // Освобождаем контекст

//            cancellationToken.ThrowIfCancellationRequested();

//            if (fsi is DirectoryInfo dir)
//            {
//                var flags = EntityFlags.IsDirectory;
//                if (dir.Attributes.HasFlag(FileAttributes.Hidden))
//                    flags |= EntityFlags.IsHidden;
//                if (dir.Attributes.HasFlag(FileAttributes.System))
//                    flags |= EntityFlags.IsSystem;

//                return new DirectoryViewModel(dir, flags);
//            }
//            else if (fsi is FileInfo file)
//            {
//                var flags = EntityFlags.IsFile;
//                if (file.Attributes.HasFlag(FileAttributes.Hidden))
//                    flags |= EntityFlags.IsHidden;
//                if (file.Attributes.HasFlag(FileAttributes.System))
//                    flags |= EntityFlags.IsSystem;
//                if (file.Attributes.HasFlag(FileAttributes.ReadOnly))
//                    flags |= EntityFlags.IsReadOnly;

//                return new FileViewModel(file, flags);
//            }

//            return null;
//        }

//        public static async Task InvalidateCacheAsync(string path = null)
//        {
//            if (path == null)
//            {
//                _cache.Clear();
//                _fastCache.Clear();
//            }
//            else
//            {
//                _cache.TryRemove(path, out _);
//                _fastCache.TryRemove(path, out _);
//            }

//            await Task.CompletedTask;
//        }

//        public static async Task PreloadDirectoryAsync(string path)
//        {
//            if (string.IsNullOrEmpty(path))
//                return;

//            try
//            {
//                var perfSettings = PerformanceManager.Settings;
//                var storageType = StorageDetector.DetectStorageType(path);
//                var storageProfile = StorageCharacteristics.GetProfile(storageType);

//                // Более агрессивные ограничения для предзагрузки
//                float cpuPriority = perfSettings.CpuPriority / 100f;
//                float ioPriority = perfSettings.IoPriority / 100f;

//                int degreeOfParallelism = Math.Max(1,
//                    Math.Min(
//                        (int)(storageProfile.PreferredThreads * cpuPriority * ioPriority * 0.7f),
//                        perfSettings.MaxThreads
//                    )
//                );

//                var parallelOptions = new ParallelOptions
//                {
//                    MaxDegreeOfParallelism = degreeOfParallelism
//                };

//                // Прямой вызов без Task.Run
//                await GetDirectoryContentAsync(path, parallelOptions).ConfigureAwait(false);
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[Preload] Error preloading {path}: {ex.Message}");
//            }
//        }

//        // Сохраняем синхронные версии для обратной совместимости
//        public static List<FileEntityViewModel> GetDirectoryContent(string path, ParallelOptions parallelOptions)
//        {
//            return GetDirectoryContentAsync(path, parallelOptions).GetAwaiter().GetResult();
//        }

//        public static void InvalidateCache(string path = null)
//        {
//            InvalidateCacheAsync(path).GetAwaiter().GetResult();
//        }
//        private static int QuickFileCountEstimate(DirectoryInfo dirInfo)
//        {
//            try
//            {
//                // Используем Enumerate для быстрой оценки без полной загрузки
//                return dirInfo.EnumerateFileSystemInfos().Take(1001).Count();
//            }
//            catch
//            {
//                return 0;
//            }
//        }

//        private static async Task<List<FileEntityViewModel>> LoadLargeDirectoryOptimized(DirectoryInfo dirInfo, StorageType storageType, ParallelOptions parallelOptions)
//        {
//            var items = new List<FileEntityViewModel>();

//            try
//            {
//                // Для больших папок на HDD отключаем параллелизм
//                if (storageType == StorageType.HDD)
//                {
//                    parallelOptions.MaxDegreeOfParallelism = 1;
//                }

//                // Загружаем только базовую информацию без метаданных
//                await foreach (var item in LoadDirectoryBasicInfoAsync(dirInfo, parallelOptions.CancellationToken))
//                {
//                    items.Add(item);

//                    // Периодически yield для отзывчивости UI
//                    if (items.Count % 100 == 0)
//                        await Task.Yield();
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[LargeDir] Error loading large directory: {ex.Message}");
//            }

//            return items;
//        }

//        private static async IAsyncEnumerable<FileEntityViewModel> LoadDirectoryBasicInfoAsync(DirectoryInfo dirInfo, CancellationToken cancellationToken)
//        {
//            await foreach (var fsi in EnumerateFileSystemInfosAsync(dirInfo, cancellationToken))
//            {
//                cancellationToken.ThrowIfCancellationRequested();

//                if (fsi is DirectoryInfo dir)
//                {
//                    var flags = EntityFlags.IsDirectory;
//                    if (dir.Attributes.HasFlag(FileAttributes.Hidden)) flags |= EntityFlags.IsHidden;
//                    if (dir.Attributes.HasFlag(FileAttributes.System)) flags |= EntityFlags.IsSystem;

//                    yield return new DirectoryViewModel(dir, flags);
//                }
//                else if (fsi is FileInfo file)
//                {
//                    var flags = EntityFlags.IsFile;
//                    if (file.Attributes.HasFlag(FileAttributes.Hidden)) flags |= EntityFlags.IsHidden;
//                    if (file.Attributes.HasFlag(FileAttributes.System)) flags |= EntityFlags.IsSystem;

//                    yield return new FileViewModel(file, flags);
//                }
//            }
//        }

//        private static async IAsyncEnumerable<FileSystemInfo> EnumerateFileSystemInfosAsync(DirectoryInfo dirInfo, CancellationToken cancellationToken)
//        {
//            var batchSize = 50; // Обрабатываем пачками для отзывчивости

//            foreach (var batch in dirInfo.EnumerateFileSystemInfos().Batch(batchSize))
//            {
//                foreach (var fsi in batch)
//                {
//                    cancellationToken.ThrowIfCancellationRequested();
//                    yield return fsi;
//                }
//                await Task.Yield(); // Даем возможность прерваться
//            }
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

//namespace Core_FileManagement
//{
//    public static class DirectoryCacheService
//    {
//        #region Fields and Constants
//        private static readonly ConcurrentDictionary<string, (DateTime timestamp, List<FileEntityViewModel> items)> _cache
//            = new ConcurrentDictionary<string, (DateTime, List<FileEntityViewModel>)>(StringComparer.OrdinalIgnoreCase);

//        private static readonly TimeSpan _cacheLifetime = TimeSpan.FromMinutes(5);
//        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _directoryLocks =
//            new ConcurrentDictionary<string, SemaphoreSlim>();
//        private static readonly ConcurrentDictionary<string, (DateTime timestamp, List<FileEntityViewModel> items)> _fastCache
//            = new ConcurrentDictionary<string, (DateTime, List<FileEntityViewModel>)>(StringComparer.OrdinalIgnoreCase);
//        private static readonly TimeSpan _fastCacheLifetime = TimeSpan.FromMinutes(1);

//        // Кэш предзагрузки для больших папок
//        private static readonly ConcurrentDictionary<string, (DateTime timestamp, List<FileEntityViewModel> items)> _preloadCache
//            = new ConcurrentDictionary<string, (DateTime, List<FileEntityViewModel>)>();
//        #endregion

//        #region Cache Strategy Types
//        private record CacheStrategy
//        {
//            public bool UseFastCache { get; init; }
//            public TimeSpan FastCacheExpiry { get; init; }
//            public TimeSpan MainCacheExpiry { get; init; }
//        }
//        #endregion

//        #region Public Methods
//        /// <summary>
//        /// Получает содержимое директории с использованием кэширования
//        /// </summary>
//        /// <param name="path">Путь к директории</param>
//        /// <param name="parallelOptions">Параллельные опции для обработки</param>
//        /// <returns>Список элементов директории</returns>
//        public static async Task<List<FileEntityViewModel>> GetDirectoryContentAsync(string path, ParallelOptions parallelOptions)
//        {
//            if (string.IsNullOrEmpty(path))
//                throw new ArgumentException("Path cannot be null or empty", nameof(path));

//            var storageType = StorageDetector.DetectStorageType(path);

//            // РАЗНЫЕ СТРАТЕГИИ КЭШИРОВАНИЯ ДЛЯ РАЗНЫХ ТИПОВ ХРАНИЛИЩ
//            var cacheStrategy = GetCacheStrategy(storageType);

//            if (cacheStrategy.UseFastCache && _fastCache.TryGetValue(path, out var fastCached) &&
//                DateTime.Now - fastCached.timestamp <= cacheStrategy.FastCacheExpiry)
//            {
//                return fastCached.items;
//            }

//            // Проверяем кэш предзагрузки для больших папок
//            if (_preloadCache.TryGetValue(path, out var preloaded) &&
//                DateTime.Now - preloaded.timestamp <= TimeSpan.FromMinutes(10))
//            {
//                return preloaded.items;
//            }

//            var dirLock = _directoryLocks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
//            await dirLock.WaitAsync().ConfigureAwait(false);
//            try
//            {
//                if (_cache.TryGetValue(path, out var cached) &&
//                    DateTime.Now - cached.timestamp <= cacheStrategy.MainCacheExpiry)
//                {
//                    // Обновляем быстрый кэш для часто используемых путей
//                    if (cacheStrategy.UseFastCache)
//                        _fastCache.AddOrUpdate(path, (DateTime.Now, cached.items), (k, v) => (DateTime.Now, cached.items));

//                    return cached.items;
//                }

//                var items = await LoadDirectoryItemsOptimizedAsync(path, parallelOptions).ConfigureAwait(false);

//                // Обновляем кэши с учетом стратегии
//                _cache.AddOrUpdate(path, (DateTime.Now, items), (key, old) => (DateTime.Now, items));

//                if (cacheStrategy.UseFastCache)
//                    _fastCache.AddOrUpdate(path, (DateTime.Now, items), (key, old) => (DateTime.Now, items));

//                return items;
//            }
//            finally
//            {
//                dirLock.Release();
//                _directoryLocks.TryRemove(path, out _);
//            }
//        }

//        /// <summary>
//        /// Синхронная версия получения содержимого директории
//        /// </summary>
//        public static List<FileEntityViewModel> GetDirectoryContent(string path, ParallelOptions parallelOptions)
//        {
//            return GetDirectoryContentAsync(path, parallelOptions).GetAwaiter().GetResult();
//        }

//        /// <summary>
//        /// Асинхронная инвалидация кэша
//        /// </summary>
//        /// <param name="path">Путь для инвалидации (null для очистки всего кэша)</param>
//        public static async Task InvalidateCacheAsync(string path = null)
//        {
//            if (path == null)
//            {
//                _cache.Clear();
//                _fastCache.Clear();
//                _preloadCache.Clear();
//            }
//            else
//            {
//                _cache.TryRemove(path, out _);
//                _fastCache.TryRemove(path, out _);
//                _preloadCache.TryRemove(path, out _);
//            }

//            await Task.CompletedTask;
//        }

//        /// <summary>
//        /// Синхронная версия инвалидации кэша
//        /// </summary>
//        public static void InvalidateCache(string path = null)
//        {
//            InvalidateCacheAsync(path).GetAwaiter().GetResult();
//        }

//        /// <summary>
//        /// Предварительная загрузка директории в кэш
//        /// </summary>
//        /// <param name="path">Путь к директории для предзагрузки</param>
//        public static async Task PreloadDirectoryAsync(string path)
//        {
//            if (string.IsNullOrEmpty(path))
//                return;

//            try
//            {
//                var perfSettings = PerformanceManager.Settings;
//                var storageType = StorageDetector.DetectStorageType(path);
//                var storageProfile = StorageCharacteristics.GetProfile(storageType);

//                int degreeOfParallelism = Math.Max(1,
//                    Math.Min(
//                        (int)(storageProfile.PreferredThreads * (perfSettings.CpuPriority / 100f) * (perfSettings.IoPriority / 100f) * 0.7f),
//                        perfSettings.MaxThreads
//                    )
//                );

//                var parallelOptions = new ParallelOptions
//                {
//                    MaxDegreeOfParallelism = degreeOfParallelism
//                };

//                await GetDirectoryContentAsync(path, parallelOptions).ConfigureAwait(false);
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[Preload] Error preloading {path}: {ex.Message}");
//            }
//        }
//        #endregion

//        #region Cache Strategy Methods
//        /// <summary>
//        /// Определяет стратегию кэширования на основе типа хранилища
//        /// </summary>
//        /// <param name="storageType">Тип хранилища</param>
//        /// <returns>Стратегия кэширования</returns>
//        private static CacheStrategy GetCacheStrategy(StorageType storageType)
//        {
//            return storageType switch
//            {
//                StorageType.SSD or StorageType.NVMe => new CacheStrategy
//                {
//                    UseFastCache = true,
//                    FastCacheExpiry = TimeSpan.FromMinutes(2),
//                    MainCacheExpiry = TimeSpan.FromMinutes(10)
//                },
//                StorageType.HDD => new CacheStrategy
//                {
//                    UseFastCache = false,
//                    MainCacheExpiry = TimeSpan.FromMinutes(5)
//                },
//                StorageType.Network => new CacheStrategy
//                {
//                    UseFastCache = true,
//                    FastCacheExpiry = TimeSpan.FromMinutes(5),
//                    MainCacheExpiry = TimeSpan.FromMinutes(15)
//                },
//                _ => new CacheStrategy
//                {
//                    UseFastCache = true,
//                    FastCacheExpiry = TimeSpan.FromMinutes(1),
//                    MainCacheExpiry = TimeSpan.FromMinutes(5)
//                }
//            };
//        }
//        #endregion

//        #region Directory Loading Methods
//        /// <summary>
//        /// Оптимизированная загрузка элементов директории с учетом размера и типа хранилища
//        /// </summary>
//        private static async Task<List<FileEntityViewModel>> LoadDirectoryItemsOptimizedAsync(string path, ParallelOptions parallelOptions)
//        {
//            var items = new List<FileEntityViewModel>();

//            try
//            {
//                var dirInfo = new DirectoryInfo(path);
//                if (!dirInfo.Exists) return items;

//                var storageType = StorageDetector.DetectStorageType(path);
//                var storageProfile = StorageCharacteristics.GetProfile(storageType);

//                // БЫСТРАЯ ПРОВЕРКА: если файлов больше 1000, используем упрощенную загрузку
//                var estimatedCount = QuickFileCountEstimate(dirInfo);
//                if (estimatedCount > 5000)
//                {
//                    return await LoadGiantDirectoryOptimized(dirInfo, storageType, parallelOptions);
//                }
//                else if (estimatedCount > 1000)
//                {
//                    return await LoadLargeDirectoryOptimized(dirInfo, storageType, parallelOptions);
//                }

//                // Стандартная загрузка для маленьких/средних папок
//                int optimalThreads = CalculateOptimalThreadCount(storageProfile, parallelOptions);
//                bool shouldUseParallel = ShouldUseParallelProcessing(dirInfo, storageProfile, optimalThreads);

//                if (shouldUseParallel)
//                {
//                    await ProcessDirectoryParallelOptimized(dirInfo, items, optimalThreads, parallelOptions.CancellationToken);
//                }
//                else
//                {
//                    await ProcessDirectorySequentialOptimized(dirInfo, items, parallelOptions.CancellationToken);
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[Cache] Critical error for {path}: {ex.Message}");
//            }

//            return items;
//        }

//        /// <summary>
//        /// Быстрая оценка количества файлов в директории
//        /// </summary>
//        private static int QuickFileCountEstimate(DirectoryInfo dirInfo)
//        {
//            try
//            {
//                return dirInfo.EnumerateFileSystemInfos().Take(1001).Count();
//            }
//            catch
//            {
//                return 0;
//            }
//        }

//        /// <summary>
//        /// Оптимизированная загрузка очень больших директорий (5000+ элементов)
//        /// </summary>
//        private static async Task<List<FileEntityViewModel>> LoadGiantDirectoryOptimized(DirectoryInfo dirInfo, StorageType storageType, ParallelOptions parallelOptions)
//        {
//            var items = new List<FileEntityViewModel>();

//            try
//            {
//                // Для гигантских папок используем прогрессивную загрузку
//                await foreach (var item in LoadDirectoryProgressiveAsync(dirInfo, parallelOptions.CancellationToken))
//                {
//                    items.Add(item);

//                    // Периодически yield для отзывчивости UI
//                    if (items.Count % 100 == 0)
//                        await Task.Yield();
//                }

//                // Сохраняем в кэш предзагрузки для будущего использования
//                _preloadCache.AddOrUpdate(dirInfo.FullName, (DateTime.Now, items), (k, v) => (DateTime.Now, items));
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[GiantDir] Error loading giant directory: {ex.Message}");
//            }

//            return items;
//        }

//        /// <summary>
//        /// Оптимизированная загрузка больших директорий (1000-5000 элементов)
//        /// </summary>
//        private static async Task<List<FileEntityViewModel>> LoadLargeDirectoryOptimized(DirectoryInfo dirInfo, StorageType storageType, ParallelOptions parallelOptions)
//        {
//            var items = new List<FileEntityViewModel>();

//            try
//            {
//                // Для больших папок на HDD отключаем параллелизм
//                if (storageType == StorageType.HDD)
//                {
//                    parallelOptions.MaxDegreeOfParallelism = 1;
//                }

//                // Загружаем только базовую информацию без метаданных
//                await foreach (var item in LoadDirectoryBasicInfoAsync(dirInfo, parallelOptions.CancellationToken))
//                {
//                    items.Add(item);

//                    // Периодически yield для отзывчивости UI
//                    if (items.Count % 100 == 0)
//                        await Task.Yield();
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[LargeDir] Error loading large directory: {ex.Message}");
//            }

//            return items;
//        }
//        #endregion

//        #region Progressive Loading Methods
//        /// <summary>
//        /// Прогрессивная загрузка элементов директории с чередованием папок и файлов
//        /// </summary>
//        private static async IAsyncEnumerable<FileEntityViewModel> LoadDirectoryProgressiveAsync(DirectoryInfo dirInfo, CancellationToken cancellationToken)
//        {
//            var loadedCount = 0;

//            // Сначала загружаем папки (обычно их меньше)
//            await foreach (var dir in EnumerateDirectoriesAsync(dirInfo, cancellationToken))
//            {
//                yield return dir;
//                loadedCount++;

//                if (loadedCount % 50 == 0)
//                {
//                    await Task.Yield();
//                    Debug.WriteLine($"[Progressive] Loaded {loadedCount} directories...");
//                }
//            }

//            // Затем загружаем файлы пачками
//            await foreach (var file in EnumerateFilesAsync(dirInfo, cancellationToken))
//            {
//                yield return file;
//                loadedCount++;

//                if (loadedCount % 100 == 0)
//                {
//                    await Task.Yield();
//                    Debug.WriteLine($"[Progressive] Loaded {loadedCount} items total...");
//                }
//            }
//        }

//        /// <summary>
//        /// Загрузка только базовой информации об элементах директории
//        /// </summary>
//        private static async IAsyncEnumerable<FileEntityViewModel> LoadDirectoryBasicInfoAsync(DirectoryInfo dirInfo, CancellationToken cancellationToken)
//        {
//            await foreach (var fsi in EnumerateFileSystemInfosAsync(dirInfo, cancellationToken))
//            {
//                cancellationToken.ThrowIfCancellationRequested();

//                if (fsi is DirectoryInfo dir)
//                {
//                    var flags = EntityFlags.IsDirectory;
//                    if (dir.Attributes.HasFlag(FileAttributes.Hidden)) flags |= EntityFlags.IsHidden;
//                    if (dir.Attributes.HasFlag(FileAttributes.System)) flags |= EntityFlags.IsSystem;

//                    yield return new DirectoryViewModel(dir, flags);
//                }
//                else if (fsi is FileInfo file)
//                {
//                    var flags = EntityFlags.IsFile;
//                    if (file.Attributes.HasFlag(FileAttributes.Hidden)) flags |= EntityFlags.IsHidden;
//                    if (file.Attributes.HasFlag(FileAttributes.System)) flags |= EntityFlags.IsSystem;

//                    yield return new FileViewModel(file, flags);
//                }
//            }
//        }
//        #endregion

//        #region Enumeration Methods
//        /// <summary>
//        /// Асинхронное перечисление поддиректорий с пакетной обработкой
//        /// </summary>
//        private static async IAsyncEnumerable<DirectoryViewModel> EnumerateDirectoriesAsync(DirectoryInfo dirInfo, CancellationToken cancellationToken)
//        {
//            foreach (var batch in dirInfo.EnumerateDirectories().Batch(100))
//            {
//                foreach (var dir in batch)
//                {
//                    cancellationToken.ThrowIfCancellationRequested();

//                    var flags = EntityFlags.IsDirectory;
//                    if (dir.Attributes.HasFlag(FileAttributes.Hidden)) flags |= EntityFlags.IsHidden;
//                    if (dir.Attributes.HasFlag(FileAttributes.System)) flags |= EntityFlags.IsSystem;

//                    yield return new DirectoryViewModel(dir, flags);
//                }
//                await Task.Yield();
//            }
//        }

//        /// <summary>
//        /// Асинхронное перечисление файлов с пакетной обработкой
//        /// </summary>
//        private static async IAsyncEnumerable<FileViewModel> EnumerateFilesAsync(DirectoryInfo dirInfo, CancellationToken cancellationToken)
//        {
//            foreach (var batch in dirInfo.EnumerateFiles().Batch(200))
//            {
//                foreach (var file in batch)
//                {
//                    cancellationToken.ThrowIfCancellationRequested();

//                    var flags = EntityFlags.IsFile;
//                    if (file.Attributes.HasFlag(FileAttributes.Hidden)) flags |= EntityFlags.IsHidden;
//                    if (file.Attributes.HasFlag(FileAttributes.System)) flags |= EntityFlags.IsSystem;

//                    yield return new FileViewModel(file, flags);
//                }
//                await Task.Yield();
//            }
//        }

//        /// <summary>
//        /// Асинхронное перечисление всех элементов файловой системы
//        /// </summary>
//        private static async IAsyncEnumerable<FileSystemInfo> EnumerateFileSystemInfosAsync(DirectoryInfo dirInfo, CancellationToken cancellationToken)
//        {
//            var batchSize = 50;

//            foreach (var batch in dirInfo.EnumerateFileSystemInfos().Batch(batchSize))
//            {
//                foreach (var fsi in batch)
//                {
//                    cancellationToken.ThrowIfCancellationRequested();
//                    yield return fsi;
//                }
//                await Task.Yield();
//            }
//        }
//        #endregion

//        #region Processing Methods
//        /// <summary>
//        /// Параллельная обработка элементов директории
//        /// </summary>
//        private static async Task ProcessDirectoryParallelOptimized(DirectoryInfo dirInfo,
//            List<FileEntityViewModel> items, int optimalThreads, CancellationToken cancellationToken)
//        {
//            FileSystemInfo[] fileSystemInfos;
//            try
//            {
//                fileSystemInfos = dirInfo.GetFileSystemInfos();
//                if (fileSystemInfos.Length == 0) return;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[Cache] Error getting file system infos: {ex.Message}");
//                return;
//            }

//            var concurrentItems = new ConcurrentBag<FileEntityViewModel>();

//            await Parallel.ForEachAsync(
//                fileSystemInfos,
//                new ParallelOptions
//                {
//                    MaxDegreeOfParallelism = optimalThreads,
//                    CancellationToken = cancellationToken
//                },
//                async (fsi, ct) =>
//                {
//                    try
//                    {
//                        var item = await CreateFileSystemItemAsync(fsi, ct).ConfigureAwait(false);
//                        if (item != null)
//                            concurrentItems.Add(item);
//                    }
//                    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
//                    {
//                        Debug.WriteLine($"[Cache] Error processing {fsi.FullName}: {ex.Message}");
//                    }
//                });

//            items.AddRange(concurrentItems.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase));
//        }

//        /// <summary>
//        /// Последовательная обработка элементов директории
//        /// </summary>
//        private static async Task ProcessDirectorySequentialOptimized(DirectoryInfo dirInfo,
//            List<FileEntityViewModel> items, CancellationToken cancellationToken)
//        {
//            FileSystemInfo[] fileSystemInfos;
//            try
//            {
//                fileSystemInfos = dirInfo.GetFileSystemInfos()
//                    .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
//                    .ToArray();
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[Cache] Error getting file system infos: {ex.Message}");
//                return;
//            }

//            foreach (var fsi in fileSystemInfos)
//            {
//                cancellationToken.ThrowIfCancellationRequested();

//                try
//                {
//                    var item = await CreateFileSystemItemAsync(fsi, cancellationToken).ConfigureAwait(false);
//                    if (item != null)
//                        items.Add(item);
//                }
//                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
//                {
//                    Debug.WriteLine($"[Cache] Error loading item: {fsi.FullName} - {ex.Message}");
//                }
//            }
//        }

//        /// <summary>
//        /// Создание ViewModel для элемента файловой системы
//        /// </summary>
//        private static async Task<FileEntityViewModel> CreateFileSystemItemAsync(FileSystemInfo fsi, CancellationToken cancellationToken)
//        {
//            await Task.Yield();

//            cancellationToken.ThrowIfCancellationRequested();

//            if (fsi is DirectoryInfo dir)
//            {
//                var flags = EntityFlags.IsDirectory;
//                if (dir.Attributes.HasFlag(FileAttributes.Hidden))
//                    flags |= EntityFlags.IsHidden;
//                if (dir.Attributes.HasFlag(FileAttributes.System))
//                    flags |= EntityFlags.IsSystem;

//                return new DirectoryViewModel(dir, flags);
//            }
//            else if (fsi is FileInfo file)
//            {
//                var flags = EntityFlags.IsFile;
//                if (file.Attributes.HasFlag(FileAttributes.Hidden))
//                    flags |= EntityFlags.IsHidden;
//                if (file.Attributes.HasFlag(FileAttributes.System))
//                    flags |= EntityFlags.IsSystem;
//                if (file.Attributes.HasFlag(FileAttributes.ReadOnly))
//                    flags |= EntityFlags.IsReadOnly;

//                return new FileViewModel(file, flags);
//            }

//            return null;
//        }
//        #endregion

//        #region Performance Optimization Methods
//        /// <summary>
//        /// Вычисление оптимального количества потоков для обработки
//        /// </summary>
//        private static int CalculateOptimalThreadCount(StorageProfile storageProfile, ParallelOptions parallelOptions)
//        {
//            var perfSettings = PerformanceManager.Settings;
//            float cpuPriority = perfSettings.CpuPriority / 100f;
//            float ioPriority = perfSettings.IoPriority / 100f;

//            int baseThreads = Math.Min(
//                parallelOptions.MaxDegreeOfParallelism,
//                PerformanceManager.CalculateOptimalThreadCount()
//            );

//            float adjustmentFactor = storageProfile.ParallelismFactor * cpuPriority * ioPriority;
//            int adjustedThreads = Math.Max(1, (int)(baseThreads * adjustmentFactor));

//            return Math.Min(adjustedThreads, parallelOptions.MaxDegreeOfParallelism);
//        }

//        /// <summary>
//        /// Определяет необходимость использования параллельной обработки
//        /// </summary>
//        private static bool ShouldUseParallelProcessing(DirectoryInfo dirInfo, StorageProfile storageProfile, int optimalThreads)
//        {
//            if (optimalThreads <= 1) return false;
//            if (storageProfile.ParallelismFactor <= 0.3f) return false;

//            try
//            {
//                var estimatedCount = dirInfo.EnumerateFileSystemInfos().Take(201).Count();
//                return estimatedCount > 100 && estimatedCount < 10000;
//            }
//            catch
//            {
//                return false;
//            }
//        }
//        #endregion
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

//namespace Core_FileManagement
//{
//    public static class DirectoryCacheService
//    {
//        #region Fields and Constants
//        private static readonly ConcurrentDictionary<string, (DateTime timestamp, List<FileEntityViewModel> items)> _cache
//            = new ConcurrentDictionary<string, (DateTime, List<FileEntityViewModel>)>(StringComparer.OrdinalIgnoreCase);

//        private static readonly TimeSpan _cacheLifetime = TimeSpan.FromMinutes(5);
//        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _directoryLocks =
//            new ConcurrentDictionary<string, SemaphoreSlim>();
//        private static readonly ConcurrentDictionary<string, (DateTime timestamp, List<FileEntityViewModel> items)> _fastCache
//            = new ConcurrentDictionary<string, (DateTime, List<FileEntityViewModel>)>(StringComparer.OrdinalIgnoreCase);
//        private static readonly TimeSpan _fastCacheLifetime = TimeSpan.FromMinutes(1);

//        // Кэш предзагрузки для больших папок
//        private static readonly ConcurrentDictionary<string, (DateTime timestamp, List<FileEntityViewModel> items)> _preloadCache
//            = new ConcurrentDictionary<string, (DateTime, List<FileEntityViewModel>)>();

//        // Приблизительные счетчики для мониторинга производительности
//        private static volatile int _approximateTotalItems = 0;
//        private static volatile int _approximateFastCacheItems = 0;
//        private static volatile int _approximatePreloadItems = 0;
//        #endregion

//        #region Cache Strategy Types
//        private record CacheStrategy
//        {
//            public bool UseFastCache { get; init; }
//            public TimeSpan FastCacheExpiry { get; init; }
//            public TimeSpan MainCacheExpiry { get; init; }
//        }

//        private enum ProcessingMode
//        {
//            Sequential,
//            Parallel,
//            Progressive,
//            BasicInfo,
//            AggressiveParallel
//        }

//        private enum ItemType
//        {
//            DirectoriesOnly,
//            FilesOnly,
//            AllItems
//        }
//        #endregion

//        #region Public Methods
//        /// <summary>
//        /// Получает содержимое директории с использованием кэширования
//        /// </summary>
//        public static async Task<List<FileEntityViewModel>> GetDirectoryContentAsync(string path, ParallelOptions parallelOptions)
//        {
//            if (string.IsNullOrEmpty(path))
//                throw new ArgumentException("Path cannot be null or empty", nameof(path));

//            var storageType = StorageDetector.DetectStorageType(path);
//            var cacheStrategy = GetCacheStrategy(storageType);

//            if (cacheStrategy.UseFastCache && _fastCache.TryGetValue(path, out var fastCached) &&
//                DateTime.Now - fastCached.timestamp <= cacheStrategy.FastCacheExpiry)
//            {
//                return fastCached.items;
//            }

//            // Проверяем кэш предзагрузки для больших папок
//            if (_preloadCache.TryGetValue(path, out var preloaded) &&
//                DateTime.Now - preloaded.timestamp <= TimeSpan.FromMinutes(10))
//            {
//                return preloaded.items;
//            }

//            var dirLock = _directoryLocks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
//            await dirLock.WaitAsync().ConfigureAwait(false);
//            try
//            {
//                if (_cache.TryGetValue(path, out var cached) &&
//                    DateTime.Now - cached.timestamp <= cacheStrategy.MainCacheExpiry)
//                {
//                    // Обновляем быстрый кэш для часто используемых путей
//                    if (cacheStrategy.UseFastCache)
//                        _fastCache.AddOrUpdate(path, (DateTime.Now, cached.items), (k, v) => (DateTime.Now, cached.items));

//                    return cached.items;
//                }

//                var items = await LoadDirectoryItemsOptimizedAsync(path, parallelOptions).ConfigureAwait(false);

//                // Обновляем кэши с учетом стратегии
//                _cache.AddOrUpdate(path, (DateTime.Now, items), (key, old) => (DateTime.Now, items));
//                Interlocked.Increment(ref _approximateTotalItems);

//                if (cacheStrategy.UseFastCache)
//                {
//                    _fastCache.AddOrUpdate(path, (DateTime.Now, items), (key, old) => (DateTime.Now, items));
//                    Interlocked.Increment(ref _approximateFastCacheItems);
//                }

//                return items;
//            }
//            finally
//            {
//                dirLock.Release();
//                _directoryLocks.TryRemove(path, out _);
//            }
//        }

//        /// <summary>
//        /// Синхронная версия получения содержимого директории
//        /// </summary>
//        public static List<FileEntityViewModel> GetDirectoryContent(string path, ParallelOptions parallelOptions)
//        {
//            return GetDirectoryContentAsync(path, parallelOptions).GetAwaiter().GetResult();
//        }

//        /// <summary>
//        /// Асинхронная инвалидация кэша
//        /// </summary>
//        public static async Task InvalidateCacheAsync(string path = null)
//        {
//            if (path == null)
//            {
//                _cache.Clear();
//                _fastCache.Clear();
//                _preloadCache.Clear();
//                Interlocked.Exchange(ref _approximateTotalItems, 0);
//                Interlocked.Exchange(ref _approximateFastCacheItems, 0);
//                Interlocked.Exchange(ref _approximatePreloadItems, 0);
//            }
//            else
//            {
//                if (_cache.TryRemove(path, out _))
//                    Interlocked.Decrement(ref _approximateTotalItems);
//                if (_fastCache.TryRemove(path, out _))
//                    Interlocked.Decrement(ref _approximateFastCacheItems);
//                if (_preloadCache.TryRemove(path, out _))
//                    Interlocked.Decrement(ref _approximatePreloadItems);
//            }

//            await Task.CompletedTask;
//        }

//        /// <summary>
//        /// Синхронная версия инвалидации кэша
//        /// </summary>
//        public static void InvalidateCache(string path = null)
//        {
//            InvalidateCacheAsync(path).GetAwaiter().GetResult();
//        }

//        /// <summary>
//        /// Предварительная загрузка директории в кэш
//        /// </summary>
//        public static async Task PreloadDirectoryAsync(string path)
//        {
//            if (string.IsNullOrEmpty(path))
//                return;

//            try
//            {
//                var perfSettings = PerformanceManager.Settings;
//                var storageType = StorageDetector.DetectStorageType(path);
//                var storageProfile = StorageCharacteristics.GetProfile(storageType);

//                int degreeOfParallelism = Math.Max(1,
//                    Math.Min(
//                        (int)(storageProfile.PreferredThreads * (perfSettings.CpuPriority / 100f) * (perfSettings.IoPriority / 100f) * 0.7f),
//                        perfSettings.MaxThreads
//                    )
//                );

//                var parallelOptions = new ParallelOptions
//                {
//                    MaxDegreeOfParallelism = degreeOfParallelism
//                };

//                await GetDirectoryContentAsync(path, parallelOptions).ConfigureAwait(false);
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[Preload] Error preloading {path}: {ex.Message}");
//            }
//        }

//        /// <summary>
//        /// Получает статистику кэша (оптимизированная версия)
//        /// </summary>
//        public static (int TotalItems, int FastCacheItems, int PreloadItems) GetCacheStats()
//        {
//            return (_approximateTotalItems, _approximateFastCacheItems, _approximatePreloadItems);
//        }
//        #endregion

//        #region Cache Strategy Methods
//        /// <summary>
//        /// Определяет стратегию кэширования на основе типа хранилища
//        /// </summary>
//        private static CacheStrategy GetCacheStrategy(StorageType storageType)
//        {
//            return storageType switch
//            {
//                StorageType.SSD or StorageType.NVMe => new CacheStrategy
//                {
//                    UseFastCache = true,
//                    FastCacheExpiry = TimeSpan.FromMinutes(2),
//                    MainCacheExpiry = TimeSpan.FromMinutes(10)
//                },
//                StorageType.HDD => new CacheStrategy
//                {
//                    UseFastCache = false,
//                    MainCacheExpiry = TimeSpan.FromMinutes(5)
//                },
//                StorageType.Network => new CacheStrategy
//                {
//                    UseFastCache = true,
//                    FastCacheExpiry = TimeSpan.FromMinutes(5),
//                    MainCacheExpiry = TimeSpan.FromMinutes(15)
//                },
//                StorageType.USB => new CacheStrategy
//                {
//                    UseFastCache = true,
//                    FastCacheExpiry = TimeSpan.FromMinutes(3),
//                    MainCacheExpiry = TimeSpan.FromMinutes(8)
//                },
//                StorageType.DVD or StorageType.BD => new CacheStrategy
//                {
//                    UseFastCache = false,
//                    MainCacheExpiry = TimeSpan.FromMinutes(30)
//                },
//                _ => new CacheStrategy
//                {
//                    UseFastCache = true,
//                    FastCacheExpiry = TimeSpan.FromMinutes(1),
//                    MainCacheExpiry = TimeSpan.FromMinutes(5)
//                }
//            };
//        }
//        #endregion

//        #region Unified Directory Loading Methods
//        /// <summary>
//        /// Оптимизированная загрузка элементов директории с учетом размера и типа хранилища
//        /// </summary>
//        private static async Task<List<FileEntityViewModel>> LoadDirectoryItemsOptimizedAsync(string path, ParallelOptions parallelOptions)
//        {
//            try
//            {
//                var dirInfo = new DirectoryInfo(path);
//                if (!dirInfo.Exists) return new List<FileEntityViewModel>();

//                var storageType = StorageDetector.DetectStorageType(path);
//                var storageProfile = StorageCharacteristics.GetProfile(storageType);

//                // БЫСТРАЯ ПРОВЕРКА: если файлов больше 1000, используем упрощенную загрузку
//                var estimatedCount = QuickFileCountEstimate(dirInfo);
//                var processingMode = DetermineProcessingMode(dirInfo, estimatedCount, storageType, storageProfile, parallelOptions);

//                return processingMode switch
//                {
//                    ProcessingMode.Progressive => await LoadDirectoryProgressiveAsync(dirInfo, parallelOptions.CancellationToken),
//                    ProcessingMode.BasicInfo => await LoadDirectoryBasicAsync(dirInfo, storageType, parallelOptions),
//                    ProcessingMode.AggressiveParallel => await ProcessDirectoryAggressiveParallelAsync(dirInfo, storageProfile, parallelOptions),
//                    ProcessingMode.Parallel => await ProcessDirectoryParallelAsync(dirInfo, storageProfile, parallelOptions),
//                    _ => await ProcessDirectorySequentialAsync(dirInfo, parallelOptions.CancellationToken)
//                };
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[Cache] Critical error for {path}: {ex.Message}");
//                return new List<FileEntityViewModel>();
//            }
//        }

//        /// <summary>
//        /// Определяет оптимальный режим обработки с использованием ShouldUseParallelProcessing
//        /// </summary>
//        private static ProcessingMode DetermineProcessingMode(DirectoryInfo dirInfo, int estimatedCount,
//            StorageType storageType, StorageProfile storageProfile, ParallelOptions parallelOptions)
//        {
//            // Для очень больших папок (>5000)
//            if (estimatedCount > 5000)
//            {
//                return storageType switch
//                {
//                    StorageType.SSD or StorageType.NVMe => ProcessingMode.AggressiveParallel, // SSD: сверхагрессивный параллелизм
//                    StorageType.HDD => ProcessingMode.Progressive,                            // HDD: прогрессивная загрузка
//                    StorageType.Network => ProcessingMode.BasicInfo,                          // Сеть: базовая информация
//                    StorageType.USB => ProcessingMode.BasicInfo,                              // USB: базовая информация
//                    StorageType.DVD or StorageType.BD => ProcessingMode.Sequential,           // Диски: последовательная загрузка
//                    _ => ProcessingMode.Progressive
//                };
//            }

//            // Для больших папок (1000-5000)
//            if (estimatedCount > 1000)
//            {
//                return storageType switch
//                {
//                    StorageType.HDD => ProcessingMode.BasicInfo,                              // HDD: базовая информация
//                    StorageType.SSD or StorageType.NVMe => ProcessingMode.Parallel,           // SSD: полный параллелизм
//                    StorageType.Network => ProcessingMode.BasicInfo,                          // Сеть: базовая информация
//                    StorageType.USB => ProcessingMode.BasicInfo,                              // USB: базовая информация
//                    StorageType.DVD or StorageType.BD => ProcessingMode.Sequential,           // Диски: последовательная загрузка
//                    _ => ProcessingMode.Progressive
//                };
//            }

//            // Для маленьких/средних папок используем ShouldUseParallelProcessing
//            int optimalThreads = CalculateOptimalThreadCount(storageProfile, parallelOptions);
//            bool shouldUseParallel = ShouldUseParallelProcessing(dirInfo, storageProfile, optimalThreads);

//            return shouldUseParallel ? ProcessingMode.Parallel : ProcessingMode.Sequential;
//        }

//        /// <summary>
//        /// Быстрая оценка количества файлов в директории
//        /// </summary>
//        private static int QuickFileCountEstimate(DirectoryInfo dirInfo)
//        {
//            try
//            {
//                return dirInfo.EnumerateFileSystemInfos().Take(1001).Count();
//            }
//            catch
//            {
//                return 0;
//            }
//        }
//        #endregion

//        #region Unified Processing Methods
//        /// <summary>
//        /// Прогрессивная загрузка с чередованием папок и файлов (для больших директорий)
//        /// </summary>
//        private static async Task<List<FileEntityViewModel>> LoadDirectoryProgressiveAsync(DirectoryInfo dirInfo, CancellationToken cancellationToken)
//        {
//            var items = new List<FileEntityViewModel>();
//            var loadedCount = 0;

//            try
//            {
//                // Сначала загружаем папки с оптимальным batch size
//                await foreach (var dir in EnumerateFileSystemItemsAsync(dirInfo, ItemType.DirectoriesOnly, 100, cancellationToken))
//                {
//                    items.Add(dir);
//                    loadedCount++;

//                    if (loadedCount % 50 == 0)
//                    {
//                        await Task.Yield();
//                        cancellationToken.ThrowIfCancellationRequested();
//                    }
//                }

//                // Затем загружаем файлы с увеличенным batch size
//                await foreach (var file in EnumerateFileSystemItemsAsync(dirInfo, ItemType.FilesOnly, 200, cancellationToken))
//                {
//                    items.Add(file);
//                    loadedCount++;

//                    if (loadedCount % 100 == 0)
//                    {
//                        await Task.Yield();
//                        cancellationToken.ThrowIfCancellationRequested();
//                    }
//                }

//                // Сохраняем в кэш предзагрузки для будущего использования
//                _preloadCache.AddOrUpdate(dirInfo.FullName, (DateTime.Now, items), (k, v) => (DateTime.Now, items));
//                Interlocked.Increment(ref _approximatePreloadItems);
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[Progressive] Error loading directory: {ex.Message}");
//            }

//            return items;
//        }

//        /// <summary>
//        /// Загрузка только базовой информации (для больших директорий на HDD)
//        /// </summary>
//        private static async Task<List<FileEntityViewModel>> LoadDirectoryBasicAsync(DirectoryInfo dirInfo, StorageType storageType, ParallelOptions parallelOptions)
//        {
//            var items = new List<FileEntityViewModel>();
//            var loadedCount = 0;

//            try
//            {
//                // Для больших папок на HDD отключаем параллелизм
//                if (storageType == StorageType.HDD)
//                {
//                    parallelOptions.MaxDegreeOfParallelism = 1;
//                }

//                await foreach (var item in EnumerateFileSystemItemsAsync(dirInfo, ItemType.AllItems, 100, parallelOptions.CancellationToken))
//                {
//                    items.Add(item);
//                    loadedCount++;

//                    if (loadedCount % 100 == 0)
//                        await Task.Yield();
//                }
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[Basic] Error loading directory: {ex.Message}");
//            }

//            return items;
//        }

//        /// <summary>
//        /// УНИФИЦИРОВАННЫЙ МЕТОД: перечисление элементов файловой системы
//        /// Заменяет 5 отдельных методов перечисления
//        /// </summary>
//        private static async IAsyncEnumerable<FileEntityViewModel> EnumerateFileSystemItemsAsync(
//            DirectoryInfo dirInfo,
//            ItemType itemType,
//            int batchSize,
//            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
//        {
//            IEnumerable<FileSystemInfo> items = itemType switch
//            {
//                ItemType.DirectoriesOnly => dirInfo.EnumerateDirectories(),
//                ItemType.FilesOnly => dirInfo.EnumerateFiles(),
//                _ => dirInfo.EnumerateFileSystemInfos()
//            };

//            foreach (var batch in items.Batch(batchSize))
//            {
//                foreach (var fsi in batch)
//                {
//                    cancellationToken.ThrowIfCancellationRequested();

//                    var item = await CreateFileSystemItemAsync(fsi, cancellationToken).ConfigureAwait(false);
//                    if (item != null)
//                        yield return item;
//                }
//                await Task.Yield();
//            }
//        }
//        #endregion

//        #region Unified Directory Processing
//        /// <summary>
//        /// Агрессивная параллельная обработка для SSD/NVMe
//        /// </summary>
//        private static async Task<List<FileEntityViewModel>> ProcessDirectoryAggressiveParallelAsync(DirectoryInfo dirInfo, StorageProfile storageProfile, ParallelOptions parallelOptions)
//        {
//            FileSystemInfo[] fileSystemInfos;
//            try
//            {
//                fileSystemInfos = dirInfo.GetFileSystemInfos();
//                if (fileSystemInfos.Length == 0) return new List<FileEntityViewModel>();
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[AggressiveParallel] Error getting file system infos: {ex.Message}");
//                return new List<FileEntityViewModel>();
//            }

//            // Увеличиваем параллелизм для SSD/NVMe
//            int optimalThreads = Math.Min(parallelOptions.MaxDegreeOfParallelism * 2, Environment.ProcessorCount * 2);
//            var concurrentItems = new ConcurrentBag<FileEntityViewModel>();

//            await Parallel.ForEachAsync(
//                fileSystemInfos,
//                new ParallelOptions
//                {
//                    MaxDegreeOfParallelism = optimalThreads,
//                    CancellationToken = parallelOptions.CancellationToken
//                },
//                async (fsi, ct) =>
//                {
//                    try
//                    {
//                        var item = await CreateFileSystemItemAsync(fsi, ct).ConfigureAwait(false);
//                        if (item != null)
//                            concurrentItems.Add(item);
//                    }
//                    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
//                    {
//                        Debug.WriteLine($"[AggressiveParallel] Error processing {fsi.FullName}: {ex.Message}");
//                    }
//                });

//            return concurrentItems.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
//        }

//        /// <summary>
//        /// Параллельная обработка директории
//        /// </summary>
//        private static async Task<List<FileEntityViewModel>> ProcessDirectoryParallelAsync(DirectoryInfo dirInfo, StorageProfile storageProfile, ParallelOptions parallelOptions)
//        {
//            FileSystemInfo[] fileSystemInfos;
//            try
//            {
//                fileSystemInfos = dirInfo.GetFileSystemInfos();
//                if (fileSystemInfos.Length == 0) return new List<FileEntityViewModel>();
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[Parallel] Error getting file system infos: {ex.Message}");
//                return new List<FileEntityViewModel>();
//            }

//            int optimalThreads = CalculateOptimalThreadCount(storageProfile, parallelOptions);
//            var concurrentItems = new ConcurrentBag<FileEntityViewModel>();

//            await Parallel.ForEachAsync(
//                fileSystemInfos,
//                new ParallelOptions
//                {
//                    MaxDegreeOfParallelism = optimalThreads,
//                    CancellationToken = parallelOptions.CancellationToken
//                },
//                async (fsi, ct) =>
//                {
//                    try
//                    {
//                        var item = await CreateFileSystemItemAsync(fsi, ct).ConfigureAwait(false);
//                        if (item != null)
//                            concurrentItems.Add(item);
//                    }
//                    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
//                    {
//                        Debug.WriteLine($"[Parallel] Error processing {fsi.FullName}: {ex.Message}");
//                    }
//                });

//            return concurrentItems.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
//        }

//        /// <summary>
//        /// Последовательная обработка директории
//        /// </summary>
//        private static async Task<List<FileEntityViewModel>> ProcessDirectorySequentialAsync(DirectoryInfo dirInfo, CancellationToken cancellationToken)
//        {
//            FileSystemInfo[] fileSystemInfos;
//            try
//            {
//                fileSystemInfos = dirInfo.GetFileSystemInfos()
//                    .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
//                    .ToArray();
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"[Sequential] Error getting file system infos: {ex.Message}");
//                return new List<FileEntityViewModel>();
//            }

//            var items = new List<FileEntityViewModel>();

//            foreach (var fsi in fileSystemInfos)
//            {
//                cancellationToken.ThrowIfCancellationRequested();

//                try
//                {
//                    var item = await CreateFileSystemItemAsync(fsi, cancellationToken).ConfigureAwait(false);
//                    if (item != null)
//                        items.Add(item);
//                }
//                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
//                {
//                    Debug.WriteLine($"[Sequential] Error loading item: {fsi.FullName} - {ex.Message}");
//                }
//            }

//            return items;
//        }

//        /// <summary>
//        /// Создание ViewModel для элемента файловой системы
//        /// </summary>
//        private static async ValueTask<FileEntityViewModel> CreateFileSystemItemAsync(FileSystemInfo fsi, CancellationToken cancellationToken)
//        {
//            await Task.Yield();
//            cancellationToken.ThrowIfCancellationRequested();

//            if (fsi is DirectoryInfo dir)
//            {
//                var flags = EntityFlags.IsDirectory;
//                if (dir.Attributes.HasFlag(FileAttributes.Hidden)) flags |= EntityFlags.IsHidden;
//                if (dir.Attributes.HasFlag(FileAttributes.System)) flags |= EntityFlags.IsSystem;

//                return new DirectoryViewModel(dir, flags);
//            }
//            else if (fsi is FileInfo file)
//            {
//                var flags = EntityFlags.IsFile;
//                if (file.Attributes.HasFlag(FileAttributes.Hidden)) flags |= EntityFlags.IsHidden;
//                if (file.Attributes.HasFlag(FileAttributes.System)) flags |= EntityFlags.IsSystem;
//                if (file.Attributes.HasFlag(FileAttributes.ReadOnly)) flags |= EntityFlags.IsReadOnly;

//                return new FileViewModel(file, flags);
//            }

//            return null;
//        }
//        #endregion

//        #region Performance Optimization Methods
//        /// <summary>
//        /// Вычисление оптимального количества потоков для обработки
//        /// </summary>
//        private static int CalculateOptimalThreadCount(StorageProfile storageProfile, ParallelOptions parallelOptions)
//        {
//            var perfSettings = PerformanceManager.Settings;
//            float cpuPriority = perfSettings.CpuPriority / 100f;
//            float ioPriority = perfSettings.IoPriority / 100f;

//            int baseThreads = Math.Min(
//                parallelOptions.MaxDegreeOfParallelism,
//                PerformanceManager.CalculateOptimalThreadCount()
//            );

//            float adjustmentFactor = storageProfile.ParallelismFactor * cpuPriority * ioPriority;
//            int adjustedThreads = Math.Max(1, (int)(baseThreads * adjustmentFactor));

//            return Math.Min(adjustedThreads, parallelOptions.MaxDegreeOfParallelism);
//        }

//        /// <summary>
//        /// Определяет необходимость использования параллельной обработки
//        /// </summary>
//        private static bool ShouldUseParallelProcessing(DirectoryInfo dirInfo, StorageProfile storageProfile, int optimalThreads)
//        {
//            if (optimalThreads <= 1) return false;
//            if (storageProfile.ParallelismFactor <= 0.3f) return false;

//            try
//            {
//                var estimatedCount = dirInfo.EnumerateFileSystemInfos().Take(201).Count();
//                return estimatedCount > 100 && estimatedCount < 10000;
//            }
//            catch
//            {
//                return false;
//            }
//        }
//        #endregion
//    }
//}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core_FileManagement
{
    public static class DirectoryCacheService
    {
        #region Fields and Constants
        private static readonly ConcurrentDictionary<string, (DateTime timestamp, List<FileEntityViewModel> items)> _cache
            = new ConcurrentDictionary<string, (DateTime, List<FileEntityViewModel>)>(StringComparer.OrdinalIgnoreCase);

        private static readonly TimeSpan _cacheLifetime = TimeSpan.FromMinutes(5);
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _directoryLocks =
            new ConcurrentDictionary<string, SemaphoreSlim>();
        #endregion

        #region Public Methods
        /// <summary>
        /// Получает содержимое директории с использованием кэширования
        /// </summary>
        public static async Task<List<FileEntityViewModel>> GetDirectoryContentAsync(string path, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));

            // Проверка кэша
            if (_cache.TryGetValue(path, out var cached) &&
                DateTime.Now - cached.timestamp <= _cacheLifetime)
            {
                return cached.items;
            }

            var dirLock = _directoryLocks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
            await dirLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                // Двойная проверка после получения блокировки
                if (_cache.TryGetValue(path, out cached) &&
                    DateTime.Now - cached.timestamp <= _cacheLifetime)
                {
                    return cached.items;
                }

                var items = await LoadDirectoryItemsAsync(path, cancellationToken).ConfigureAwait(false);

                // Обновляем кэш
                _cache.AddOrUpdate(path, (DateTime.Now, items), (key, old) => (DateTime.Now, items));
                return items;
            }
            finally
            {
                dirLock.Release();
                _directoryLocks.TryRemove(path, out _);
            }
        }

        /// <summary>
        /// Синхронная версия получения содержимого директории
        /// </summary>
        public static List<FileEntityViewModel> GetDirectoryContent(string path)
        {
            return GetDirectoryContentAsync(path).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Инвалидация кэша
        /// </summary>
        public static void InvalidateCache(string path = null)
        {
            if (path == null)
            {
                _cache.Clear();
            }
            else
            {
                _cache.TryRemove(path, out _);
            }
        }

        /// <summary>
        /// Предварительная загрузка директории в кэш
        /// </summary>
        public static async Task PreloadDirectoryAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                await GetDirectoryContentAsync(path).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Preload] Error preloading {path}: {ex.Message}");
            }
        }

        /// <summary>
        /// Получает статистику кэша
        /// </summary>
        public static int GetCacheItemCount()
        {
            return _cache.Count;
        }
        #endregion

        #region Directory Loading Methods
        /// <summary>
        /// Загрузка элементов директории
        /// </summary>
        private static async Task<List<FileEntityViewModel>> LoadDirectoryItemsAsync(string path, CancellationToken cancellationToken)
        {
            try
            {
                var dirInfo = new DirectoryInfo(path);
                if (!dirInfo.Exists)
                    return new List<FileEntityViewModel>();

                // Определяем оптимальный метод загрузки на основе размера директории
                var estimatedCount = QuickFileCountEstimate(dirInfo);
                var processingMode = DetermineProcessingMode(estimatedCount);

                return processingMode switch
                {
                    ProcessingMode.Parallel => await ProcessDirectoryParallelAsync(dirInfo, cancellationToken),
                    _ => await ProcessDirectorySequentialAsync(dirInfo, cancellationToken)
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Cache] Error loading directory {path}: {ex.Message}");
                return new List<FileEntityViewModel>();
            }
        }

        /// <summary>
        /// Быстрая оценка количества файлов в директории
        /// </summary>
        private static int QuickFileCountEstimate(DirectoryInfo dirInfo)
        {
            try
            {
                return dirInfo.EnumerateFileSystemInfos().Take(1001).Count();
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Определяет оптимальный режим обработки
        /// </summary>
        private static ProcessingMode DetermineProcessingMode(int estimatedCount)
        {
            // Для больших директорий используем параллельную обработку
            return estimatedCount > 500 ? ProcessingMode.Parallel : ProcessingMode.Sequential;
        }
        #endregion

        #region Processing Methods
        /// <summary>
        /// Параллельная обработка директории
        /// </summary>
        private static async Task<List<FileEntityViewModel>> ProcessDirectoryParallelAsync(DirectoryInfo dirInfo, CancellationToken cancellationToken)
        {
            FileSystemInfo[] fileSystemInfos;
            try
            {
                fileSystemInfos = dirInfo.GetFileSystemInfos();
                if (fileSystemInfos.Length == 0)
                    return new List<FileEntityViewModel>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Parallel] Error getting file system infos: {ex.Message}");
                return new List<FileEntityViewModel>();
            }

            var concurrentItems = new ConcurrentBag<FileEntityViewModel>();
            var tasks = new List<Task>();

            foreach (var fsi in fileSystemInfos)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Ограничиваем количество одновременных задач
                if (tasks.Count >= Environment.ProcessorCount)
                {
                    await Task.WhenAny(tasks);
                    tasks.RemoveAll(t => t.IsCompleted);
                }

                var task = ProcessFileSystemItemAsync(fsi, concurrentItems, cancellationToken);
                tasks.Add(task);
            }

            // Ожидаем завершения всех задач
            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }

            return concurrentItems.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// Последовательная обработка директории
        /// </summary>
        private static async Task<List<FileEntityViewModel>> ProcessDirectorySequentialAsync(DirectoryInfo dirInfo, CancellationToken cancellationToken)
        {
            FileSystemInfo[] fileSystemInfos;
            try
            {
                fileSystemInfos = dirInfo.GetFileSystemInfos()
                    .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Sequential] Error getting file system infos: {ex.Message}");
                return new List<FileEntityViewModel>();
            }

            var items = new List<FileEntityViewModel>();

            foreach (var fsi in fileSystemInfos)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var item = await CreateFileSystemItemAsync(fsi, cancellationToken).ConfigureAwait(false);
                    if (item != null)
                        items.Add(item);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    Debug.WriteLine($"[Sequential] Error loading item: {fsi.FullName} - {ex.Message}");
                }
            }

            return items;
        }

        /// <summary>
        /// Асинхронная обработка элемента файловой системы
        /// </summary>
        private static async Task ProcessFileSystemItemAsync(FileSystemInfo fsi, ConcurrentBag<FileEntityViewModel> items, CancellationToken cancellationToken)
        {
            try
            {
                var item = await CreateFileSystemItemAsync(fsi, cancellationToken).ConfigureAwait(false);
                if (item != null)
                    items.Add(item);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Debug.WriteLine($"[Parallel] Error processing {fsi.FullName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Создание ViewModel для элемента файловой системы
        /// </summary>
        private static async ValueTask<FileEntityViewModel> CreateFileSystemItemAsync(FileSystemInfo fsi, CancellationToken cancellationToken)
        {
            await Task.Yield(); // Освобождаем поток для асинхронности
            cancellationToken.ThrowIfCancellationRequested();

            if (fsi is DirectoryInfo dir)
            {
                var flags = EntityFlags.IsDirectory;
                if (dir.Attributes.HasFlag(FileAttributes.Hidden)) flags |= EntityFlags.IsHidden;
                if (dir.Attributes.HasFlag(FileAttributes.System)) flags |= EntityFlags.IsSystem;

                return new DirectoryViewModel(dir, flags);
            }
            else if (fsi is FileInfo file)
            {
                var flags = EntityFlags.IsFile;
                if (file.Attributes.HasFlag(FileAttributes.Hidden)) flags |= EntityFlags.IsHidden;
                if (file.Attributes.HasFlag(FileAttributes.System)) flags |= EntityFlags.IsSystem;
                if (file.Attributes.HasFlag(FileAttributes.ReadOnly)) flags |= EntityFlags.IsReadOnly;

                return new FileViewModel(file, flags);
            }

            return null;
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Метод для пакетной обработки (заменяет расширение Batch)
        /// </summary>
        private static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
        {
            var batch = new List<T>(batchSize);
            foreach (var item in source)
            {
                batch.Add(item);
                if (batch.Count >= batchSize)
                {
                    yield return batch;
                    batch = new List<T>(batchSize);
                }
            }
            if (batch.Count > 0)
                yield return batch;
        }
        #endregion

        #region Enums
        private enum ProcessingMode
        {
            Sequential,
            Parallel
        }
        #endregion
    }
}
