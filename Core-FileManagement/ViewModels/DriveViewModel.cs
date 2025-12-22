//using System;
//using System.IO;
//using Microsoft.UI.Xaml.Media.Imaging;

//namespace Core_FileManagement
//{
//    public class DriveViewModel : FileEntityViewModel
//    {
//        public StorageType StorageType { get; }

//        public long TotalSize { get; }
//        public long UsedSpace { get; }
//        public long FreeSpace { get; }
//        public int UsedProcentValue { get; set; }
//        public string UsedSpaceString { get; }
//        public string FreeSpaceString { get; }
//        public string TotalSizeString { get; }
//        public new BitmapImage ImageSource { get; set; }

//        // Добавленное свойство для проверки доступности
//        public bool IsUnavailable => Flags.HasFlag(EntityFlags.IsUnavailable);

//        public DriveViewModel(DriveInfo driveInfo, EntityFlags flags) : base(driveInfo.Name, flags)
//        {
//            if (driveInfo.IsReady)
//            {
//                if (driveInfo.DriveType == DriveType.Fixed)
//                {
//                    Flags |= EntityFlags.IsSystem;
//                }
//                if (driveInfo.DriveType == DriveType.Network)
//                {
//                    Flags |= EntityFlags.IsHidden;
//                }
//            }
//            else
//            {
//                Flags |= EntityFlags.IsUnavailable;
//            }

//            Flags |= flags;

//            TotalSize = driveInfo.IsReady ? driveInfo.TotalSize : 0;
//            UsedSpace = driveInfo.IsReady ? (driveInfo.TotalSize - driveInfo.AvailableFreeSpace) : 0;
//            FreeSpace = driveInfo.IsReady ? driveInfo.AvailableFreeSpace : 0;

//            UsedSpaceString = FormatBytes(UsedSpace);
//            FreeSpaceString = FormatBytes(FreeSpace);
//            TotalSizeString = FormatBytes(TotalSize);

//            UsedProcentValue = TotalSize > 0 ? (int)((double)UsedSpace / TotalSize * 100) : 0;
//        }

//        public DriveViewModel(
//            DriveInfo driveInfo,
//            EntityFlags flags,
//            StorageType storageType) : this(driveInfo, flags)
//        {
//            StorageType = storageType;
//        }

//        private const long TB = 1099511627776;
//        private const long GB = 1073741824;
//        private const long MB = 1048576;
//        private const long KB = 1024;

//        public static string FormatBytes(long bytes)
//        {
//            if (bytes >= TB)
//                return $"{bytes / (double)TB:0.##} TB";
//            if (bytes >= GB)
//                return $"{bytes / (double)GB:0.##} GB";
//            if (bytes >= MB)
//                return $"{bytes / (double)MB:0.##} MB";
//            if (bytes >= KB)
//                return $"{bytes / (double)KB:0.##} KB";
//            return $"{bytes} B";
//        }
//    }
//}


//using System;
//using System.IO;
//using Microsoft.UI.Xaml.Media.Imaging;
//using System.Collections.Concurrent;
//using System.Diagnostics;
//using System.Threading.Tasks;

//namespace Core_FileManagement
//{
//    public class DriveViewModel : FileEntityViewModel
//    {
//        private long _totalSize;
//        private long _usedSpace;
//        private long _freeSpace;
//        private int _usedProcentValue;
//        private string _usedSpaceString;
//        private string _freeSpaceString;
//        private string _totalSizeString;
//        private StorageType _storageType;

//        public StorageType StorageType
//        {
//            get => _storageType;
//            private set
//            {
//                if (_storageType != value)
//                {
//                    _storageType = value;
//                    OnPropertyChanged();
//                }
//            }
//        }

//        public long TotalSize
//        {
//            get => _totalSize;
//            private set
//            {
//                if (_totalSize != value)
//                {
//                    _totalSize = value;
//                    OnPropertyChanged();
//                }
//            }
//        }

//        public long UsedSpace
//        {
//            get => _usedSpace;
//            private set
//            {
//                if (_usedSpace != value)
//                {
//                    _usedSpace = value;
//                    OnPropertyChanged();
//                }
//            }
//        }

//        public long FreeSpace
//        {
//            get => _freeSpace;
//            private set
//            {
//                if (_freeSpace != value)
//                {
//                    _freeSpace = value;
//                    OnPropertyChanged();
//                }
//            }
//        }

//        public int UsedProcentValue
//        {
//            get => _usedProcentValue;
//            private set
//            {
//                if (_usedProcentValue != value)
//                {
//                    _usedProcentValue = value;
//                    OnPropertyChanged();
//                }
//            }
//        }

//        public string UsedSpaceString
//        {
//            get => _usedSpaceString;
//            private set
//            {
//                if (_usedSpaceString != value)
//                {
//                    _usedSpaceString = value;
//                    OnPropertyChanged();
//                }
//            }
//        }

//        public string FreeSpaceString
//        {
//            get => _freeSpaceString;
//            private set
//            {
//                if (_freeSpaceString != value)
//                {
//                    _freeSpaceString = value;
//                    OnPropertyChanged();
//                }
//            }
//        }

//        public string TotalSizeString
//        {
//            get => _totalSizeString;
//            private set
//            {
//                if (_totalSizeString != value)
//                {
//                    _totalSizeString = value;
//                    OnPropertyChanged();
//                }
//            }
//        }

//        private static readonly ConcurrentDictionary<long, string> _formatCache = new();

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

//        // Добавленное свойство для проверки доступности
//        public bool IsUnavailable => HasFlag(EntityFlags.IsUnavailable);

//        public DriveViewModel(DriveInfo driveInfo, EntityFlags flags) : base(driveInfo.Name, flags)
//        {
//            FullName = driveInfo.Name;
//            UpdateFromDriveInfo(driveInfo, flags);
//        }

//        public DriveViewModel(
//            DriveInfo driveInfo,
//            EntityFlags flags,
//            StorageType storageType) : this(driveInfo, flags)
//        {
//            StorageType = storageType;
//        }

//        private void UpdateFromDriveInfo(DriveInfo driveInfo, EntityFlags flags)
//        {
//            // Сбрасываем флаги к исходным
//            Flags = flags;

//            if (driveInfo.IsReady)
//            {
//                // Определяем тип хранилища
//                StorageType = StorageDetector.DetectStorageType(driveInfo.Name);

//                // Устанавливаем флаги на основе типа диска
//                if (driveInfo.DriveType == DriveType.Fixed)
//                {
//                    AddFlag(EntityFlags.IsSystem);
//                }
//                if (driveInfo.DriveType == DriveType.Network)
//                {
//                    AddFlag(EntityFlags.IsHidden);
//                }

//                // Убираем флаг недоступности если диск готов
//                RemoveFlag(EntityFlags.IsUnavailable);

//                // Обновляем статистику
//                TotalSize = driveInfo.TotalSize;
//                UsedSpace = driveInfo.TotalSize - driveInfo.AvailableFreeSpace;
//                FreeSpace = driveInfo.AvailableFreeSpace;
//            }
//            else
//            {
//                // Диск недоступен
//                AddFlag(EntityFlags.IsUnavailable);
//                StorageType = StorageType.Unknown;

//                TotalSize = 0;
//                UsedSpace = 0;
//                FreeSpace = 0;
//            }

//            // Форматируем строки
//            UsedSpaceString = FormatBytes(UsedSpace);
//            FreeSpaceString = FormatBytes(FreeSpace);
//            TotalSizeString = FormatBytes(TotalSize);

//            // Вычисляем процент использования
//            UsedProcentValue = TotalSize > 0 ? (int)((double)UsedSpace / TotalSize * 100) : 0;

//            // Обновляем имя если оно изменилось
//            if (Name != driveInfo.Name)
//            {
//                Name = driveInfo.Name;
//            }
//        }

//        // Метод для обновления информации о диске
//        public bool RefreshDriveInfo()
//        {
//            try
//            {
//                var driveInfo = new DriveInfo(FullName);
//                UpdateFromDriveInfo(driveInfo, Flags);
//                Debug.WriteLine($"Диск {Name} успешно обновлен. Тип: {StorageType}, Использовано: {UsedProcentValue}%");
//                return true;
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"Ошибка обновления информации о диске {FullName}: {ex.Message}");

//                // Если диск стал недоступен, устанавливаем соответствующий флаг
//                AddFlag(EntityFlags.IsUnavailable);
//                OnPropertyChanged(nameof(IsUnavailable));

//                return false;
//            }
//        }

//        // Асинхронная версия обновления
//        public async Task<bool> RefreshDriveInfoAsync()
//        {
//            return await Task.Run(() => RefreshDriveInfo());
//        }

//        private const long TB = 1099511627776;
//        private const long GB = 1073741824;
//        private const long MB = 1048576;
//        private const long KB = 1024;

//        public static string FormatBytes(long bytes)
//        {
//            // Используем кэш для часто используемых значений
//            if (_formatCache.TryGetValue(bytes, out var cached))
//                return cached;

//            string result;
//            if (bytes >= TB)
//                result = $"{bytes / (double)TB:0.##} TB";
//            else if (bytes >= GB)
//                result = $"{bytes / (double)GB:0.##} GB";
//            else if (bytes >= MB)
//                result = $"{bytes / (double)MB:0.##} MB";
//            else if (bytes >= KB)
//                result = $"{bytes / (double)KB:0.##} KB";
//            else
//                result = $"{bytes} B";

//            // Кэшируем только "круглые" значения чтобы не засорять кэш
//            if (bytes % (1024 * 1024) == 0 || bytes < 1024 * 1024)
//            {
//                _formatCache.TryAdd(bytes, result);
//            }

//            return result;
//        }

//        // Статический метод для очистки кэша форматирования
//        public static void ClearFormatCache()
//        {
//            _formatCache.Clear();
//        }
//    }
//}


//using System;
//using System.IO;
//using Microsoft.UI.Xaml.Media.Imaging;
//using System.Collections.Concurrent;

//namespace Core_FileManagement
//{
//    public class DriveViewModel : FileEntityViewModel
//    {
//        public StorageType StorageType { get; }

//        public long TotalSize { get; }
//        public long UsedSpace { get; }
//        public long FreeSpace { get; }
//        public int UsedProcentValue { get; set; }
//        public string UsedSpaceString { get; }
//        public string FreeSpaceString { get; }
//        public string TotalSizeString { get; }
//        public new BitmapImage ImageSource { get; set; }

//        // Добавленное свойство для проверки доступности
//        public bool IsUnavailable => Flags.HasFlag(EntityFlags.IsUnavailable);

//        public DriveViewModel(DriveInfo driveInfo, EntityFlags flags) : base(driveInfo.Name, flags)
//        {
//            if (driveInfo.IsReady)
//            {
//                if (driveInfo.DriveType == DriveType.Fixed)
//                {
//                    Flags |= EntityFlags.IsSystem;
//                }
//                if (driveInfo.DriveType == DriveType.Network)
//                {
//                    Flags |= EntityFlags.IsHidden;
//                }
//            }
//            else
//            {
//                Flags |= EntityFlags.IsUnavailable;
//            }

//            Flags |= flags;

//            TotalSize = driveInfo.IsReady ? driveInfo.TotalSize : 0;
//            UsedSpace = driveInfo.IsReady ? (driveInfo.TotalSize - driveInfo.AvailableFreeSpace) : 0;
//            FreeSpace = driveInfo.IsReady ? driveInfo.AvailableFreeSpace : 0;

//            UsedSpaceString = FormatBytes(UsedSpace);
//            FreeSpaceString = FormatBytes(FreeSpace);
//            TotalSizeString = FormatBytes(TotalSize);

//            UsedProcentValue = TotalSize > 0 ? (int)((double)UsedSpace / TotalSize * 100) : 0;

//            // Определяем тип хранилища
//            StorageType = StorageDetector.DetectStorageType(driveInfo.Name);
//        }

//        public DriveViewModel(
//            DriveInfo driveInfo,
//            EntityFlags flags,
//            StorageType storageType) : this(driveInfo, flags)
//        {
//            StorageType = storageType;
//        }

//        private const long TB = 1099511627776;
//        private const long GB = 1073741824;
//        private const long MB = 1048576;
//        private const long KB = 1024;

//        // Кэш для форматированных значений (единственное полезное улучшение)
//        private static readonly ConcurrentDictionary<long, string> _formatCache = new();

//        public static string FormatBytes(long bytes)
//        {
//            // Используем кэш для часто используемых значений
//            if (_formatCache.TryGetValue(bytes, out var cached))
//                return cached;

//            string result;
//            if (bytes >= TB)
//                result = $"{bytes / (double)TB:0.##} TB";
//            else if (bytes >= GB)
//                result = $"{bytes / (double)GB:0.##} GB";
//            else if (bytes >= MB)
//                result = $"{bytes / (double)MB:0.##} MB";
//            else if (bytes >= KB)
//                result = $"{bytes / (double)KB:0.##} KB";
//            else
//                result = $"{bytes} B";

//            // Кэшируем только "круглые" значения чтобы не засорять кэш
//            if (bytes % (1024 * 1024) == 0 || bytes < 1024 * 1024)
//            {
//                _formatCache.TryAdd(bytes, result);
//            }

//            return result;
//        }

//        // Метод для очистки кэша (опционально)
//        public static void ClearFormatCache()
//        {
//            _formatCache.Clear();
//        }
//    }
//}

using System;
using System.IO;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.Concurrent;

namespace Core_FileManagement
{
    public class DriveViewModel : FileEntityViewModel
    {
        public string DriveTypeName { get; }  // Переименовано

        public long TotalSize { get; }
        public long UsedSpace { get; }
        public long FreeSpace { get; }
        public int UsedProcentValue { get; set; }
        public string UsedSpaceString { get; }
        public string FreeSpaceString { get; }
        public string TotalSizeString { get; }
        public new BitmapImage ImageSource { get; set; }

        public bool IsUnavailable => Flags.HasFlag(EntityFlags.IsUnavailable);

        public DriveViewModel(DriveInfo driveInfo, EntityFlags flags) : base(driveInfo.Name, flags)
        {
            if (driveInfo.IsReady)
            {
                if (driveInfo.DriveType == DriveType.Fixed)
                {
                    Flags |= EntityFlags.IsSystem;
                }
                if (driveInfo.DriveType == DriveType.Network)
                {
                    Flags |= EntityFlags.IsHidden;
                }
            }
            else
            {
                Flags |= EntityFlags.IsUnavailable;
            }

            Flags |= flags;

            TotalSize = driveInfo.IsReady ? driveInfo.TotalSize : 0;
            UsedSpace = driveInfo.IsReady ? (driveInfo.TotalSize - driveInfo.AvailableFreeSpace) : 0;
            FreeSpace = driveInfo.IsReady ? driveInfo.AvailableFreeSpace : 0;

            UsedSpaceString = FormatBytes(UsedSpace);
            FreeSpaceString = FormatBytes(FreeSpace);
            TotalSizeString = FormatBytes(TotalSize);

            UsedProcentValue = TotalSize > 0 ? (int)((double)UsedSpace / TotalSize * 100) : 0;

            // Простое определение типа диска
            DriveTypeName = driveInfo.DriveType.ToString();  // Используем переименованное свойство
        }

        public DriveViewModel(
            DriveInfo driveInfo,
            EntityFlags flags,
            string driveType) : this(driveInfo, flags)
        {
            DriveTypeName = driveType;  // Используем переименованное свойство
        }

        private const long TB = 1099511627776;
        private const long GB = 1073741824;
        private const long MB = 1048576;
        private const long KB = 1024;

        private static readonly ConcurrentDictionary<long, string> _formatCache = new();

        public static string FormatBytes(long bytes)
        {
            if (_formatCache.TryGetValue(bytes, out var cached))
                return cached;

            string result;
            if (bytes >= TB)
                result = $"{bytes / (double)TB:0.##} TB";
            else if (bytes >= GB)
                result = $"{bytes / (double)GB:0.##} GB";
            else if (bytes >= MB)
                result = $"{bytes / (double)MB:0.##} MB";
            else if (bytes >= KB)
                result = $"{bytes / (double)KB:0.##} KB";
            else
                result = $"{bytes} B";

            if (bytes % (1024 * 1024) == 0 || bytes < 1024 * 1024)
            {
                _formatCache.TryAdd(bytes, result);
            }

            return result;
        }

        public static void ClearFormatCache()
        {
            _formatCache.Clear();
        }
    }
}