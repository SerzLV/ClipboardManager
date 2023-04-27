using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using MahApps.Metro.Controls;

namespace ClipboardManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        //private const int MOD_ALT = 0x0001;
        //private const int MOD_CTRL = 0x0002;
        //private const int MOD_SHIFT = 0x0004;
        //private const int MOD_WIN = 0x0008;
        //private const int WM_HOTKEY = 0x0312;
        //private const int HOTKEY_ID = 9000;
        //private IntPtr _handle;
        MainWindowViewModel _mwvm = new MainWindowViewModel();
        public MainWindow()
        {
            InitializeComponent();
            DataContext = _mwvm;

            //CreateTrayIconContextMenu();
            //// Register hotkey
            //RegisterHotKey(ModifierKeys.Control | ModifierKeys.Alt, Key.Q);
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var windowClipboardManager = new Helper.ClipboardManager(this);
            windowClipboardManager.ClipboardChanged += _mwvm.ClipboardContextChange;
        }

        //private void CreateTrayIconContextMenu()
        //{
        //    var contextMenu = new ContextMenu();

        //    var showMenuItem = new MenuItem();
        //    showMenuItem.Header = "Show";
        //    showMenuItem.Click += (sender, args) =>
        //    {
        //        Show();
        //        WindowState = WindowState.Normal;
        //        Activate();
        //    };

        //    var exitMenuItem = new MenuItem();
        //    exitMenuItem.Header = "Exit";
        //    exitMenuItem.Click += (sender, args) =>
        //    {
        //        Close();
        //    };

        //    contextMenu.Items.Add(showMenuItem);
        //    contextMenu.Items.Add(new Separator());
        //    contextMenu.Items.Add(exitMenuItem);

        //    TrayIcon.ContextMenu = contextMenu;
        //}

        //private void RegisterHotKey(ModifierKeys modifier, Key key)
        //{
        //    // Get the handle to the window
        //    _handle = new WindowInteropHelper(this).Handle;

        //    // Register the hotkey
        //    int modifiers = 0;
        //    if ((modifier & ModifierKeys.Alt) == ModifierKeys.Alt)
        //        modifiers |= MOD_ALT;
        //    if ((modifier & ModifierKeys.Control) == ModifierKeys.Control)
        //        modifiers |= MOD_CTRL;
        //    if ((modifier & ModifierKeys.Shift) == ModifierKeys.Shift)
        //        modifiers |= MOD_SHIFT;
        //    if ((modifier & ModifierKeys.Windows) == ModifierKeys.Windows)
        //        modifiers |= MOD_WIN;

        //    bool success = NativeMethods.RegisterHotKey(_handle, HOTKEY_ID, modifiers, KeyInterop.VirtualKeyFromKey(key));
        //    if (!success)
        //        throw new InvalidOperationException("Failed to register hotkey.");
        //}

        //protected override void OnClosing(CancelEventArgs e)
        //{
        //    // Unregister the hotkey
        //    NativeMethods.UnregisterHotKey(_handle, HOTKEY_ID);
        //    base.OnClosing(e);
        //}

        //protected override void OnSourceInitialized(EventArgs e)
        //{
        //    base.OnSourceInitialized(e);

        //    // Add the hotkey hook
        //    var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        //    source.AddHook(HwndHook);
        //}

        //protected override void OnStateChanged(EventArgs e)
        //{
        //    base.OnStateChanged(e);

        //    // Hide the window when minimized
        //    if (WindowState == WindowState.Minimized)
        //        Hide();
        //}

        //private void HandleHotKey()
        //{
        //    // Toggle window visibility
        //    if (IsVisible)
        //    {
        //        Hide();
        //    }
        //    else
        //    {
        //        Show();
        //        WindowState = WindowState.Normal;
        //        Activate();
        //    }
        //}

        //private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        //{
        //    if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        //    {
        //        HandleHotKey();
        //        handled = true;
        //    }
        //    return IntPtr.Zero;
        //}

        //internal static class NativeMethods
        //{
        //    [DllImport("user32.dll")]
        //    public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        //    [DllImport("user32.dll")]
        //    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        //}
    }
}