using System.Windows.Input;
using System;

namespace Core_FileManagement
{
    public class DelegateCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;
        public DelegateCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public DelegateCommand()
        {
        }

        public bool CanExecute(object parameter) => _canExecute != null ? _canExecute.Invoke(parameter) : true;
        public void Execute(object parameter)
        {
            _execute?.Invoke(parameter);
        }
        public event EventHandler CanExecuteChanged;
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
