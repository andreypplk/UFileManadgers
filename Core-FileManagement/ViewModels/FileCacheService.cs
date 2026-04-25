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
            catch 
            {
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