using System.Windows;
using System.Windows.Interop;

namespace ClipboardManager.Helper;

public sealed class ClipboardWatcher : IDisposable
{
    private readonly HwndSource _source;
    private readonly IntPtr _windowHandle;
    private bool _disposed;

    public ClipboardWatcher(Window window)
    {
        _source = PresentationSource.FromVisual(window) as HwndSource
            ?? throw new ArgumentException("Window source must be initialized before creating a clipboard watcher.", nameof(window));

        _source.AddHook(WndProc);
        _windowHandle = new WindowInteropHelper(window).Handle;
        NativeMethods.AddClipboardFormatListener(_windowHandle);
    }

    public event EventHandler? ClipboardChanged;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        NativeMethods.RemoveClipboardFormatListener(_windowHandle);
        _source.RemoveHook(WndProc);
        _disposed = true;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_CLIPBOARDUPDATE)
        {
            ClipboardChanged?.Invoke(this, EventArgs.Empty);
            handled = true;
        }

        return IntPtr.Zero;
    }
}
