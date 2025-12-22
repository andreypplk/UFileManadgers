using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using System;
using System.Diagnostics;

namespace ufm
{
    public class IconService : IDisposable
    {
        private static readonly ConcurrentDictionary<string, BitmapImage> _iconCache = new();
        private const uint DefaultIconSize = 48;

        // Предзагруженная иконка диска
        private static readonly BitmapImage _defaultDriveIcon = new BitmapImage(new Uri("ms-appx:///Assets/drive.png"));

        public async Task<BitmapImage> GetIconAsync(string path, bool isDirectory)
        {
            string cacheKey = $"{path}_{isDirectory}";

            if (_iconCache.TryGetValue(cacheKey, out var cachedIcon))
                return cachedIcon;

            BitmapImage icon;
            try
            {
                if (isDirectory && IsDrivePath(path))
                {
                    // Синхронная загрузка иконки диска
                    icon = GetDriveIconSync(path);
                }
                else if (isDirectory)
                {
                    // Асинхронная загрузка иконки папки
                    icon = await GetFolderIconAsync(path);
                }
                else
                {
                    // Асинхронная загрузка иконки файла
                    icon = await GetFileIconAsync(path);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading icon: {ex.Message}");
                icon = new BitmapImage(new Uri(isDirectory ?
                    "ms-appx:///Assets/folder1.png" :
                    "ms-appx:///Assets/file.png"));
            }

            _iconCache.TryAdd(cacheKey, icon);
            return icon;
        }

        private bool IsDrivePath(string path)
        {
            return path.Length == 3 &&
                   char.IsLetter(path[0]) &&
                   path[1] == ':' &&
                   path[2] == '\\';
        }

        private BitmapImage GetDriveIconSync(string path)
        {
            try
            {
                var folder = StorageFolder.GetFolderFromPathAsync(path).AsTask().GetAwaiter().GetResult();
                var thumbnail = folder.GetThumbnailAsync(
                    ThumbnailMode.SingleItem,
                    DefaultIconSize,
                    ThumbnailOptions.UseCurrentScale).AsTask().GetAwaiter().GetResult();

                if (thumbnail != null && thumbnail.Size > 0)
                {
                    var iconImage = new BitmapImage();
                    iconImage.SetSource(thumbnail); // Синхронный SetSource
                    return iconImage;
                }
            }
            catch
            {
                // Логирование уже в основном методе
            }

            return _defaultDriveIcon;
        }

        private async Task<BitmapImage> GetFolderIconAsync(string path)
        {
            try
            {
                var folder = await StorageFolder.GetFolderFromPathAsync(path);
                using var thumbnail = await folder.GetThumbnailAsync(
                    ThumbnailMode.SingleItem,
                    DefaultIconSize,
                    ThumbnailOptions.UseCurrentScale);

                var iconImage = new BitmapImage();
                await iconImage.SetSourceAsync(thumbnail);
                return iconImage;
            }
            catch
            {
                return new BitmapImage(new Uri("ms-appx:///Assets/folder1.png"));
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

                var iconImage = new BitmapImage();
                await iconImage.SetSourceAsync(thumbnail);
                return iconImage;
            }
            catch
            {
                return new BitmapImage(new Uri("ms-appx:///Assets/file.png"));
            }
        }

        public void Dispose() => ClearCache();
        public static void ClearCache() => _iconCache.Clear();
    }
}