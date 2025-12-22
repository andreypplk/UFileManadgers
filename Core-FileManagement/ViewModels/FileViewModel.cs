using System;
using System.IO;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Core_FileManagement
{
    public sealed class FileViewModel : FileEntityViewModel
    {
        private BitmapImage _imageSource;

        public long Size { get; }

        public new BitmapImage ImageSource
        {
            get
            {
                // Ленивая инициализация - создаем только при первом обращении
                if (_imageSource == null)
                {
                    _imageSource = new BitmapImage();
                }
                return _imageSource;
            }
            set
            {
                if (_imageSource != value)
                {
                    _imageSource = value;
                    OnPropertyChanged();
                }
            }
        }

        public new DateTime LastModified { get; }

        public FileViewModel(string name, EntityFlags flags) : base(name, flags)
        {
            Size = 0;
            LastModified = DateTime.MinValue;
            // УБИРАЕМ инициализацию ImageSource здесь!
        }

        public FileViewModel(FileInfo fileInfo, EntityFlags flags) : base(fileInfo.Name, flags)
        {
            FullName = fileInfo.FullName;
            Size = fileInfo.Length;
            LastModified = fileInfo.LastWriteTime;
            Flags = flags;

            if (fileInfo.Attributes.HasFlag(FileAttributes.Hidden))
            {
                Flags |= EntityFlags.IsHidden;
            }
            if (fileInfo.Attributes.HasFlag(FileAttributes.ReadOnly))
            {
                Flags |= EntityFlags.IsReadOnly;
            }
            if (fileInfo.Attributes.HasFlag(FileAttributes.System))
            {
                Flags |= EntityFlags.IsSystem;
            }
        }
    }
}