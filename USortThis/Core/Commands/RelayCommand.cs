using System;
using System.Windows.Input;

namespace USortThis.Core.Commands
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        /// <summary>Initializes a command with its execution and optional availability callbacks.</summary>
        /// <param name="execute">The action invoked when the command executes.</param>
        /// <param name="canExecute">The predicate that determines whether the command can execute.</param>
        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>Determines whether the command can execute for the supplied parameter.</summary>
        /// <param name="parameter">The command parameter to evaluate.</param>
        /// <returns>True when no predicate is supplied or the predicate allows execution.</returns>
        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        /// <summary>Invokes the command's execution callback.</summary>
        /// <param name="parameter">The parameter passed to the execution callback.</param>
        public void Execute(object? parameter) => _execute(parameter);

        public event EventHandler? CanExecuteChanged;

        /// <summary>Notifies command sources to reevaluate whether the command can execute.</summary>
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}