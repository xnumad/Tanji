using System;
using System.Windows.Input;
using System.Threading.Tasks;

namespace Tanji.Helpers
{
    public class AsyncCommand : ICommand
    {
        private readonly Predicate<object> _canExecuteDelegate;
        private readonly Func<object, Task> _executeAsyncDelegate;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public AsyncCommand(Func<object, Task> executeAsync)
        {
            _executeAsyncDelegate = executeAsync;
        }
        public AsyncCommand(Func<object, Task> executeAsync, Predicate<object> canExecute)
            : this(executeAsync)
        {
            _canExecuteDelegate = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return (_canExecuteDelegate?.Invoke(parameter) ?? true);
        }
        public async void Execute(object parameter)
        {
            await _executeAsyncDelegate(parameter);
            CommandManager.InvalidateRequerySuggested();
        }
    }
}