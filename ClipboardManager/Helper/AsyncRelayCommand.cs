using System.Windows.Input;

namespace ClipboardManager.Helper;

public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<object?, Task> _execute;
    private readonly Predicate<object?>? _canExecute;
    private event EventHandler? CanExecuteChangedHandlers;
    private bool _isExecuting;

    public AsyncRelayCommand(Func<object?, Task> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter)
    {
        return !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);
    }

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        try
        {
            _isExecuting = true;
            RaiseCanExecuteChanged();
            await _execute(parameter);
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public event EventHandler? CanExecuteChanged
    {
        add
        {
            CanExecuteChangedHandlers += value;
            CommandManager.RequerySuggested += value;
        }
        remove
        {
            CanExecuteChangedHandlers -= value;
            CommandManager.RequerySuggested -= value;
        }
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChangedHandlers?.Invoke(this, EventArgs.Empty);
    }
}
