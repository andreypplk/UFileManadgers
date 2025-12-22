using Microsoft.UI.Xaml.Media.Imaging;
using System.Threading.Tasks;

namespace Core_FileManagement
{
    public interface IIconService
    {
        Task<BitmapImage> GetIconAsync(string path, bool isDirectory);
        BitmapImage GetIconSync(string path, bool isDirectory);
        Task<BitmapImage> GetSpecialFolderIconAsync(string path);

        // Методы для управления кэшем
        void InvalidateCache(string path, bool isDirectory);
        void ClearCache();

        // Новые методы из реализации
        Task PreloadCommonIconsAsync();
        (int CacheCount, int LoadingTasksCount) GetCacheStats();
        Task<BitmapImage> GetIconNoCacheAsync(string path, bool isDirectory);

        void Dispose();
    }
}