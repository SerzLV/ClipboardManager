using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using ClipboardManager.Helper;
using ClipboardManager.ViewModels;
using MahApps.Metro.Controls;

namespace ClipboardManager;

public partial class MainWindow : MetroWindow
{
    private readonly MainWindowViewModel _viewModel = new();
    private ClipboardWatcher? _clipboardWatcher;
    private bool _isCloseConfirmed;
    private bool _isSavingBeforeClose;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
    }

    private async void MetroWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadAsync();

        if (_clipboardWatcher is not null)
        {
            return;
        }

        _clipboardWatcher = new ClipboardWatcher(this);
        _clipboardWatcher.ClipboardChanged += ClipboardWatcher_ClipboardChanged;
    }

    private void ClipboardWatcher_ClipboardChanged(object? sender, EventArgs e)
    {
        _ = _viewModel.HandleClipboardChangedAsync();
    }

    private void MetroWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_isCloseConfirmed)
        {
            return;
        }

        e.Cancel = true;
        if (_isSavingBeforeClose)
        {
            return;
        }

        _isSavingBeforeClose = true;
        _clipboardWatcher?.Dispose();
        _clipboardWatcher = null;

        Dispatcher.InvokeAsync(
            () => _ = CloseAfterFlushAsync(),
            DispatcherPriority.ContextIdle);
    }

    private async Task CloseAfterFlushAsync()
    {
        await _viewModel.FlushPendingSavesAsync();

        _isCloseConfirmed = true;
        Close();
    }
}
