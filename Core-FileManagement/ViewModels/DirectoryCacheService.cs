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