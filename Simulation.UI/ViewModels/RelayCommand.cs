using System;
using System.Windows.Input;

namespace Simulation.UI.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            var result = _canExecute?.Invoke(parameter) ?? true;
            System.Diagnostics.Debug.WriteLine($"   RelayCommand.CanExecute: {result}");
            return result;
        }

        public void Execute(object? parameter)
        {
            System.Diagnostics.Debug.WriteLine("   RelayCommand.Execute вызван");
            _execute(parameter);
        }

        public event EventHandler? CanExecuteChanged;
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}