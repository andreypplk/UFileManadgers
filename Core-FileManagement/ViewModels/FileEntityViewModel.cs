using System;
using System.IO;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Core_FileManagement
{
    public abstract class FileEntityViewModel : BaseViewModel, IImageSourceProvider
    {
        public string Name { get; }
        public string FullName { get; set; }
        public EntityFlags Flags { get; set; }
        public BitmapImage ImageSource { get; set; }
        public DateTime LastModified { get; protected set; }

        protected FileEntityViewModel(string name, EntityFlags flags)
            : this(name, null, flags)
        {
        }

        protected FileEntityViewModel(string name, string fullName, EntityFlags flags)
        {
            Name = name;
            FullName = fullName;
            Flags = flags;
            LastModified = DateTime.MinValue;
        }
    }
}