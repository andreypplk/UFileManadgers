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