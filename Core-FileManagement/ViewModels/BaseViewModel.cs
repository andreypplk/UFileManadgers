//using System.ComponentModel;
//using Microsoft.UI.Dispatching;
//using System.Runtime.CompilerServices;

//namespace Core_FileManagement
//{
//    public class BaseViewModel : INotifyPropertyChanged
//    {
//        public event PropertyChangedEventHandler PropertyChanged;

//        protected DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();

//        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
//        {
//            var handler = PropertyChanged;
//            if (handler == null) return;

//            void RaiseEvent()
//            {
//                handler.Invoke(this, new PropertyChangedEventArgs(propertyName));
//            }

//            if (_dispatcher != null && !_dispatcher.HasThreadAccess)
//            {
//                _dispatcher.TryEnqueue(RaiseEvent);
//            }
//            else
//            {
//                RaiseEvent();
//            }
//        }
//    }
//}


//using System.ComponentModel;
//using Microsoft.UI.Dispatching;
//using System.Runtime.CompilerServices;
//using System.Collections.Concurrent;
//using System.Collections.Generic;

//namespace Core_FileManagement
//{
//    public class BaseViewModel : INotifyPropertyChanged
//    {
//        public event PropertyChangedEventHandler PropertyChanged;

//        protected DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();

//        // Кэш для PropertyChangedEventArgs
//        private static readonly ConcurrentDictionary<string, PropertyChangedEventArgs> _propertyChangedCache = new();

//        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
//        {
//            var handler = PropertyChanged;
//            if (handler == null) return;

//            // Используем кэшированные аргументы
//            var args = _propertyChangedCache.GetOrAdd(propertyName, name => new PropertyChangedEventArgs(name));

//            if (_dispatcher != null && !_dispatcher.HasThreadAccess)
//            {
//                _dispatcher.TryEnqueue(() => handler.Invoke(this, args));
//            }
//            else
//            {
//                handler.Invoke(this, args);
//            }
//        }

//        // Метод для пакетного обновления свойств
//        protected void OnPropertiesChanged(params string[] propertyNames)
//        {
//            if (PropertyChanged == null) return;

//            foreach (var propertyName in propertyNames)
//            {
//                OnPropertyChanged(propertyName);
//            }
//        }

//        // Без SetProperty, используем прямое присваивание с OnPropertyChanged
//        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
//        {
//            if (EqualityComparer<T>.Default.Equals(field, value))
//                return false;

//            field = value;
//            OnPropertyChanged(propertyName);
//            return true;
//        }
//    }
//}


using System.ComponentModel;
using Microsoft.UI.Dispatching;
using System.Runtime.CompilerServices;

namespace Core_FileManagement
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler == null) return;

            void RaiseEvent()
            {
                handler.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            if (_dispatcher != null && !_dispatcher.HasThreadAccess)
            {
                _dispatcher.TryEnqueue(RaiseEvent);
            }
            else
            {
                RaiseEvent();
            }
        }
    }
}