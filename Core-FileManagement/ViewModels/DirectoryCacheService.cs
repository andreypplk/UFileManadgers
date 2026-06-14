//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
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
//        #endregion

//        #region Public Methods
//        public static async Task<List<FileEntityViewModel>> GetDirectoryContentAsync(string path, CancellationToken cancellationToken = default)
//        {
//            if (string.IsNullOrEmpty(path))
//                throw new ArgumentException("Path cannot be null or empty", nameof(path));

//            if (_cache.TryGetValue(path, out var cached) &&
//                DateTime.Now - cached.timestamp <= _cacheLifetime)
//            {
//                return cached.items;
//            }

//            var dirLock = _directoryLocks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
//            await dirLock.WaitAsync(cancellationToken).ConfigureAwait(false);

//            try
//            {
//                if (_cache.TryGetValue(path, out cached) &&
//                    DateTime.Now - cached.timestamp <= _cacheLifetime)
//                {
//                    return cached.items;
//                }

//                var items = await LoadDirectoryItemsAsync(path, cancellationToken).ConfigureAwait(false);

//                _cache.AddOrUpdate(path, (DateTime.Now, items), (key, old) => (DateTime.Now, items));
//                return items;
//            }
//            finally
//            {
//                dirLock.Release();
//                _directoryLocks.TryRemove(path, out _);
//            }
//        }

//        public static List<FileEntityViewModel> GetDirectoryContent(string path)
//        {
//            return GetDirectoryContentAsync(path).GetAwaiter().GetResult();
//        }

//        public static void InvalidateCache(string path = null)
//        {
//            if (path == null)
//            {
//                _cache.Clear();
//            }
//            else
//            {
//                _cache.TryRemove(path, out _);
//            }
//        }

//        public static async Task PreloadDirectoryAsync(string path)
//        {
//            if (string.IsNullOrEmpty(path))
//                return;

//            try
//            {
//                await GetDirectoryContentAsync(path).ConfigureAwait(false);
//            }
//            catch
//            {
//            }
//        }

//        public static int GetCacheItemCount()
//        {
//            return _cache.Count;
//        }
//        #endregion

//        #region Directory Loading Methods
//        private static async Task<List<FileEntityViewModel>> LoadDirectoryItemsAsync(string path, CancellationToken cancellationToken)
//        {
//            try
//            {
//                var dirInfo = new DirectoryInfo(path);
//                if (!dirInfo.Exists)
//                    return new List<FileEntityViewModel>();

//                var estimatedCount = QuickFileCountEstimate(dirInfo);
//                var processingMode = DetermineProcessingMode(estimatedCount);

//                return processingMode switch
//                {
//                    ProcessingMode.Parallel => await ProcessDirectoryParallelAsync(dirInfo, cancellationToken),
//                    _ => await ProcessDirectorySequentialAsync(dirInfo, cancellationToken)
//                };
//            }
//            catch
//            {
//                return new List<FileEntityViewModel>();
//            }
//        }

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

//        private static ProcessingMode DetermineProcessingMode(int estimatedCount)
//        {
//            return estimatedCount > 500 ? ProcessingMode.Parallel : ProcessingMode.Sequential;
//        }
//        #endregion

//        #region Processing Methods
//        private static async Task<List<FileEntityViewModel>> ProcessDirectoryParallelAsync(DirectoryInfo dirInfo, CancellationToken cancellationToken)
//        {
//            FileSystemInfo[] fileSystemInfos;
//            try
//            {
//                fileSystemInfos = dirInfo.GetFileSystemInfos();
//                if (fileSystemInfos.Length == 0)
//                    return new List<FileEntityViewModel>();
//            }
//            catch
//            {
//                return new List<FileEntityViewModel>();
//            }

//            var concurrentItems = new ConcurrentBag<FileEntityViewModel>();
//            var tasks = new List<Task>();

//            foreach (var fsi in fileSystemInfos)
//            {
//                cancellationToken.ThrowIfCancellationRequested();

//                if (tasks.Count >= Environment.ProcessorCount)
//                {
//                    await Task.WhenAny(tasks);
//                    tasks.RemoveAll(t => t.IsCompleted);
//                }

//                var task = ProcessFileSystemItemAsync(fsi, concurrentItems, cancellationToken);
//                tasks.Add(task);
//            }

//            if (tasks.Count > 0)
//            {
//                await Task.WhenAll(tasks);
//            }

//            return concurrentItems.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
//        }

//        private static async Task<List<FileEntityViewModel>> ProcessDirectorySequentialAsync(DirectoryInfo dirInfo, CancellationToken cancellationToken)
//        {
//            FileSystemInfo[] fileSystemInfos;
//            try
//            {
//                fileSystemInfos = dirInfo.GetFileSystemInfos()
//                    .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
//                    .ToArray();
//            }
//            catch
//            {
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
//                }
//            }

//            return items;
//        }

//        private static async Task ProcessFileSystemItemAsync(FileSystemInfo fsi, ConcurrentBag<FileEntityViewModel> items, CancellationToken cancellationToken)
//        {
//            try
//            {
//                var item = await CreateFileSystemItemAsync(fsi, cancellationToken).ConfigureAwait(false);
//                if (item != null)
//                    items.Add(item);
//            }
//            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
//            {
//            }
//        }

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

//        #region Helper Methods
//        private static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
//        {
//            var batch = new List<T>(batchSize);
//            foreach (var item in source)
//            {
//                batch.Add(item);
//                if (batch.Count >= batchSize)
//                {
//                    yield return batch;
//                    batch = new List<T>(batchSize);
//                }
//            }
//            if (batch.Count > 0)
//                yield return batch;
//        }
//        #endregion

//        #region Enums
//        private enum ProcessingMode
//        {
//            Sequential,
//            Parallel
//        }
//        #endregion
//    }
//}


using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        public static async Task<List<FileEntityViewModel>> GetDirectoryContentAsync(string path, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));

            if (_cache.TryGetValue(path, out var cached) &&
                DateTime.Now - cached.timestamp <= _cacheLifetime)
            {
                return cached.items;
            }

            var dirLock = _directoryLocks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
            await dirLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_cache.TryGetValue(path, out cached) &&
                    DateTime.Now - cached.timestamp <= _cacheLifetime)
                {
                    return cached.items;
                }

                var items = await LoadDirectoryItemsAsync(path, cancellationToken).ConfigureAwait(false);

                _cache.AddOrUpdate(path, (DateTime.Now, items), (key, old) => (DateTime.Now, items));
                return items;
            }
            finally
            {
                dirLock.Release();
                _directoryLocks.TryRemove(path, out _);
            }
        }

        public static List<FileEntityViewModel> GetDirectoryContent(string path)
        {
            return GetDirectoryContentAsync(path).GetAwaiter().GetResult();
        }

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

        public static async Task PreloadDirectoryAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                await GetDirectoryContentAsync(path).ConfigureAwait(false);
            }
            catch
            {
            }
        }

        public static int GetCacheItemCount()
        {
            return _cache.Count;
        }
        #endregion

        #region Directory Loading Methods
        private static async Task<List<FileEntityViewModel>> LoadDirectoryItemsAsync(string path, CancellationToken cancellationToken)
        {
            try
            {
                var dirInfo = new DirectoryInfo(path);
                if (!dirInfo.Exists)
                    return new List<FileEntityViewModel>();

                // Используем потоковое перечисление – элементы отдаются по мере чтения файловой системой
                var items = new List<FileEntityViewModel>();
                await Task.Run(() =>
                {
                    foreach (var fsi in dirInfo.EnumerateFileSystemInfos())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        try
                        {
                            var item = CreateFileSystemItem(fsi);
                            if (item != null)
                                items.Add(item);
                        }
                        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                        {
                            // Игнорируем элементы, к которым нет доступа
                        }
                    }
                }, cancellationToken).ConfigureAwait(false);

                // Сортируем после сбора всех элементов (сортировка по имени)
                return items.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
            }
            catch (OperationCanceledException)
            {
                return new List<FileEntityViewModel>();
            }
            catch
            {
                return new List<FileEntityViewModel>();
            }
        }

        // Синхронное создание ViewModel – никаких лишних асинхронных вызовов
        private static FileEntityViewModel CreateFileSystemItem(FileSystemInfo fsi)
        {
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
    }
}