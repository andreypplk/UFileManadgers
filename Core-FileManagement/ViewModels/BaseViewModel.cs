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