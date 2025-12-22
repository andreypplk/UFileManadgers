//using Microsoft.UI.Xaml.Media.Imaging;
//using System.IO;
//using System.Linq;

//namespace Core_FileManagement
//{
//    public class DirectoryViewModel : FileEntityViewModel
//    {
//        public long Size { get; private set; }
//        public new BitmapImage ImageSource { get; set; }

//        public DirectoryViewModel(string directoryName, EntityFlags flags) : base(directoryName, flags)
//        {
//            FullName = directoryName;
//            CalculateSize();
//        }

//        public DirectoryViewModel(DirectoryInfo directoryInfo, EntityFlags flags) : base(directoryInfo.Name, flags)
//        {
//            FullName = directoryInfo.FullName;
//            CalculateSize();
//        }

//        private void CalculateSize()
//        {
//            try
//            {
//                var dirInfo = new DirectoryInfo(FullName);
//                Size = dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
//            }
//            catch
//            {
//                Size = 0;
//            }
//        }
//    }
//}


//using Microsoft.UI.Xaml.Media;
//using Microsoft.UI.Xaml.Media.Imaging;
//using System;
//using System.Collections.Concurrent;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;

//namespace Core_FileManagement
//{
//    public class DirectoryViewModel : FileEntityViewModel
//    {
//        private long _size;
//        private static readonly ConcurrentDictionary<string, (long Size, DateTime LastUpdated)> _sizeCache = new();
//        private static readonly TimeSpan _cacheTimeout = TimeSpan.FromMinutes(5);

//        public new BitmapImage ImageSource
//        {
//            get => _imageSource;
//            set
//            {
//                if (_imageSource != value)
//                {
//                    _imageSource = value;
//                    OnPropertyChanged();
//                }
//            }
//        }

//        public long Size
//        {
//            get => _size;
//            private set
//            {
//                if (_size != value)
//                {
//                    _size = value;
//                    OnPropertyChanged();
//                }
//            }
//        }

//        public DirectoryViewModel(string directoryName, EntityFlags flags) : base(directoryName, flags)
//        {
//            FullName = directoryName;
//            _ = CalculateSizeAsync(); // Асинхронная загрузка
//        }

//        public DirectoryViewModel(DirectoryInfo directoryInfo, EntityFlags flags) : base(directoryInfo.Name, flags)
//        {
//            FullName = directoryInfo.FullName;
//            _ = CalculateSizeAsync(); // Асинхронная загрузка
//        }

//        private async Task CalculateSizeAsync()
//        {
//            try
//            {
//                // Проверяем кэш
//                if (_sizeCache.TryGetValue(FullName, out var cached) &&
//                    DateTime.Now - cached.LastUpdated < _cacheTimeout)
//                {
//                    Size = cached.Size;
//                    return;
//                }

//                // Асинхронное вычисление размера
//                var size = await Task.Run(() =>
//                {
//                    long totalSize = 0;
//                    var dirInfo = new DirectoryInfo(FullName);

//                    try
//                    {
//                        // Быстрый обход только первого уровня для отзывчивости
//                        foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
//                        {
//                            totalSize += file.Length;
//                        }

//                        // Рекурсивный обход в фоне если нужно
//                        _ = CalculateRecursiveSizeAsync(dirInfo);
//                    }
//                    catch
//                    {
//                        // Игнорируем ошибки доступа
//                    }

//                    return totalSize;
//                });

//                Size = size;
//                _sizeCache[FullName] = (size, DateTime.Now);
//            }
//            catch
//            {
//                Size = 0;
//            }
//        }

//        private async Task CalculateRecursiveSizeAsync(DirectoryInfo directory)
//        {
//            await Task.Run(() =>
//            {
//                try
//                {
//                    long recursiveSize = 0;
//                    foreach (var file in directory.EnumerateFiles("*", SearchOption.AllDirectories))
//                    {
//                        recursiveSize += file.Length;
//                    }

//                    // Обновляем кэш с полным размером
//                    _sizeCache[directory.FullName] = (recursiveSize, DateTime.Now);

//                    // Обновляем UI если это текущая директория
//                    if (directory.FullName == FullName)
//                    {
//                        _dispatcher?.TryEnqueue(() =>
//                        {
//                            if (_size != recursiveSize)
//                            {
//                                _size = recursiveSize;
//                                OnPropertyChanged(nameof(Size));
//                            }
//                        });
//                    }
//                }
//                catch
//                {
//                    // Игнорируем ошибки
//                }
//            });
//        }

//        // Метод для принудительного пересчета размера
//        public async Task RefreshSizeAsync()
//        {
//            _sizeCache.TryRemove(FullName, out _);
//            await CalculateSizeAsync();
//        }

//        // Статический метод для очистки кэша
//        public static void ClearSizeCache()
//        {
//            _sizeCache.Clear();
//        }

//        // Статический метод для получения размера из кэша
//        public static long? GetCachedSize(string path)
//        {
//            if (_sizeCache.TryGetValue(path, out var cached))
//                return cached.Size;
//            return null;
//        }
//    }
//}


using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;
using System.Linq;

namespace Core_FileManagement
{
    public class DirectoryViewModel : FileEntityViewModel
    {
        public long Size { get; private set; }
        public new BitmapImage ImageSource { get; set; }

        public DirectoryViewModel(string directoryName, EntityFlags flags) : base(directoryName, flags)
        {
            FullName = directoryName;
            CalculateSize();
        }

        public DirectoryViewModel(DirectoryInfo directoryInfo, EntityFlags flags) : base(directoryInfo.Name, flags)
        {
            FullName = directoryInfo.FullName;
            CalculateSize();
        }

        private void CalculateSize()
        {
            try
            {
                var dirInfo = new DirectoryInfo(FullName);
                // Ограничиваем сканирование первым уровнем для производительности
                Size = dirInfo.EnumerateFiles("*", SearchOption.TopDirectoryOnly).Sum(f => f.Length);
            }
            catch
            {
                Size = 0;
            }
        }
    }
}