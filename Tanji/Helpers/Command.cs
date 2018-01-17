using System;
using System.Windows.Input;

namespace Tanji.Helpers
{
    public class Command<T> : ICommand
    {
        private readonly Action<T> _executeDelegate;
        private readonly Predicate<T> _canExecuteDelegate;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public Command(Action<T> execute)
        {
            _executeDelegate = execute;
        }
        public Command(Action<T> execute, Predicate<T> canExecute)
            : this(execute)
        {
            _canExecuteDelegate = canExecute;
        }

        public virtual void Execute(object parameter)
        {
            _executeDelegate((T)parameter);
        }
        public virtual bool CanExecute(object parameter)
        {
            return (_canExecuteDelegate?.Invoke((T)parameter) ?? true);
        }
    }
    public class Command : Command<object>
    {
        public Command(Action<object> execute)
            : base(execute)
        { }
        public Command(Action<object> execute, Predicate<object> canExecute)
            : base(execute, canExecute)
        { }
    }
}