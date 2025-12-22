//using System;
//using System.IO;
//using Microsoft.UI.Xaml.Media.Imaging;

//namespace Core_FileManagement
//{
//    public abstract class FileEntityViewModel : BaseViewModel, IImageSourceProvider
//    {
//        public string Name { get; }
//        public string FullName { get; set; }
//        public EntityFlags Flags { get; set; }
//        public BitmapImage ImageSource { get; set; }
//        public DateTime LastModified { get; protected set; }

//        protected FileEntityViewModel(string name, EntityFlags flags)
//            : this(name, null, flags)
//        {
//        }

//        protected FileEntityViewModel(string name, string fullName, EntityFlags flags)
//        {
//            Name = name;
//            FullName = fullName;
//            Flags = flags;
//            LastModified = DateTime.MinValue;
//        }
//    }
//}


//using System;
//using System.IO;
//using Microsoft.UI.Xaml.Media.Imaging;

//namespace Core_FileManagement
//{
//    public abstract class FileEntityViewModel : BaseViewModel, IImageSourceProvider
//    {
//        private string _name;
//        private string _fullName;
//        private EntityFlags _flags;
//        public BitmapImage _imageSource;
//        private DateTime _lastModified;

//        public string Name
//        {
//            get => _name;
//            protected set
//            {
//                if (_name != value)
//                {
//                    _name = value;
//                    OnPropertyChanged();
//                }
//            }
//        }

//        public string FullName
//        {
//            get => _fullName;
//            set
//            {
//                if (_fullName != value)
//                {
//                    _fullName = value;
//                    OnPropertyChanged();
//                }
//            }
//        }

//        public EntityFlags Flags
//        {
//            get => _flags;
//            set
//            {
//                if (_flags != value)
//                {
//                    _flags = value;
//                    OnPropertyChanged();
//                }
//            }
//        }

//        public virtual BitmapImage ImageSource
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

//        public DateTime LastModified
//        {
//            get => _lastModified;
//            protected set
//            {
//                if (_lastModified != value)
//                {
//                    _lastModified = value;
//                    OnPropertyChanged();
//                }
//            }
//        }

//        protected FileEntityViewModel(string name, EntityFlags flags)
//            : this(name, null, flags)
//        {
//        }

//        protected FileEntityViewModel(string name, string fullName, EntityFlags flags)
//        {
//            _name = name;
//            _fullName = fullName;
//            _flags = flags;
//            _lastModified = DateTime.MinValue;
//        }

//        // Быстрый метод проверки флагов
//        public bool HasFlag(EntityFlags flag) => (_flags & flag) == flag;

//        // Метод для добавления флагов без вызова PropertyChanged
//        protected void AddFlag(EntityFlags flag)
//        {
//            _flags |= flag;
//        }

//        // Метод для удаления флагов без вызова PropertyChanged  
//        protected void RemoveFlag(EntityFlags flag)
//        {
//            _flags &= ~flag;
//        }
//    }
//}


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