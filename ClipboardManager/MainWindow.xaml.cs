using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using ClipboardManager.Helper;
using ClipboardManager.Localization;
using ClipboardManager.ViewModels;
using MahApps.Metro.Controls;
using DrawingIcon = System.Drawing.Icon;
using DrawingSystemIcons = System.Drawing.SystemIcons;
using Forms = System.Windows.Forms;
using WpfMessageBox = System.Windows.MessageBox;

namespace ClipboardManager;

public partial class MainWindow : MetroWindow
{
    private const int ToggleWindowHotKeyId = 0x434D;
    private const uint ToggleWindowHotKeyModifiers = NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT;
    private const uint ToggleWindowHotKeyVirtualKey = 0x56;
    internal const string StartupLaunchArgument = "--startup";
    private const string StartupRegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupRegistryValueName = "ClipboardManager";

    private readonly MainWindowViewModel _viewModel;
    private ClipboardWatcher? _clipboardWatcher;
    private HwndSource? _hwndSource;
    private Forms.NotifyIcon? _notifyIcon;
    private DrawingIcon? _trayIcon;
    private IntPtr _windowHandle;
    private bool _isCloseConfirmed;
    private bool _isSavingBeforeClose;
    private bool _isExitRequested;
    private bool _isHotKeyRegistered;
    private bool _isApplyingShellSettings;

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _viewModel.SettingsChanged += ViewModel_SettingsChanged;
        System.Windows.Application.Current.SessionEnding += Application_SessionEnding;
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
        ApplyShellSettings();

        if (IsStartupLaunch() && _viewModel.MinimizeToTray)
        {
            _ = Dispatcher.BeginInvoke(HideToTray, DispatcherPriority.ContextIdle);
        }
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

        if (!_isExitRequested && _viewModel.MinimizeToTray)
        {
            e.Cancel = true;
            HideToTray();
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

        _ = CloseAfterFlushAsync();
    }

    private async Task CloseAfterFlushAsync()
    {
        try
        {
            await _viewModel.FlushPendingSavesAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
        finally
        {
            _isCloseConfirmed = true;
            Close();
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        _windowHandle = new WindowInteropHelper(this).Handle;
        _hwndSource = HwndSource.FromHwnd(_windowHandle);
        _hwndSource?.AddHook(WndProc);
        ApplyShellSettings();
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);

        if (_viewModel.MinimizeToTray && WindowState == WindowState.Minimized && !_isCloseConfirmed)
        {
            Dispatcher.BeginInvoke(HideToTray, DispatcherPriority.ContextIdle);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.SettingsChanged -= ViewModel_SettingsChanged;
        System.Windows.Application.Current.SessionEnding -= Application_SessionEnding;
        DisposeShellResources();
        base.OnClosed(e);
    }

    private void Application_SessionEnding(object sender, SessionEndingCancelEventArgs e)
    {
        _isExitRequested = true;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && wParam.ToInt32() == ToggleWindowHotKeyId)
        {
            handled = true;
            ToggleWindowFromHotKey();
        }

        return IntPtr.Zero;
    }

    private void ViewModel_SettingsChanged(object? sender, AppSettings e)
    {
        Dispatcher.InvokeAsync(ApplyShellSettings, DispatcherPriority.Normal);
    }

    private void ApplyShellSettings()
    {
        if (_isApplyingShellSettings)
        {
            return;
        }

        _isApplyingShellSettings = true;
        try
        {
            ApplyTraySetting();
            ApplyGlobalHotKeySetting();
            ApplyStartupSetting();
        }
        finally
        {
            _isApplyingShellSettings = false;
        }
    }

    private void ApplyTraySetting()
    {
        if (_viewModel.MinimizeToTray)
        {
            EnsureNotifyIcon();
            if (_notifyIcon is not null)
            {
                _notifyIcon.Visible = true;
            }

            return;
        }

        if (_notifyIcon is not null)
        {
            UpdateTrayMenu();
            _notifyIcon.Visible = false;
        }

        if (!IsVisible)
        {
            ShowFromTray();
        }
    }

    private void EnsureNotifyIcon()
    {
        if (_notifyIcon is null)
        {
            _trayIcon = CreateTrayIcon();
            _notifyIcon = new Forms.NotifyIcon
            {
                Icon = _trayIcon,
                Text = GetTrayText(),
                Visible = false
            };
            _notifyIcon.MouseDoubleClick += NotifyIcon_MouseDoubleClick;
        }

        UpdateTrayMenu();
    }

    private void UpdateTrayMenu()
    {
        if (_notifyIcon is null)
        {
            return;
        }

        var menu = new Forms.ContextMenuStrip();
        var openItem = new Forms.ToolStripMenuItem(_viewModel.L.TrayOpenMenuItem);
        var exitItem = new Forms.ToolStripMenuItem(_viewModel.L.TrayExitMenuItem);

        openItem.Click += (_, _) => Dispatcher.Invoke(ShowFromTray);
        exitItem.Click += (_, _) => Dispatcher.Invoke(ExitApplication);

        menu.Items.Add(openItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(exitItem);

        var previousMenu = _notifyIcon.ContextMenuStrip;
        _notifyIcon.ContextMenuStrip = menu;
        _notifyIcon.Text = GetTrayText();
        previousMenu?.Dispose();
    }

    private void NotifyIcon_MouseDoubleClick(object? sender, Forms.MouseEventArgs e)
    {
        if (e.Button == Forms.MouseButtons.Left)
        {
            Dispatcher.Invoke(ShowFromTray);
        }
    }

    private void HideToTray()
    {
        if (!_viewModel.MinimizeToTray)
        {
            WindowState = WindowState.Minimized;
            return;
        }

        EnsureNotifyIcon();
        if (_notifyIcon is not null)
        {
            _notifyIcon.Visible = true;
        }

        Hide();
    }

    private void ShowFromTray()
    {
        if (!IsVisible)
        {
            Show();
        }

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
    }

    private void ExitApplication()
    {
        _isExitRequested = true;
        Close();
    }

    internal void ActivateFromExternalInstance()
    {
        ShowFromTray();
        Topmost = true;
        Topmost = false;
        Activate();
    }

    private void ToggleWindowFromHotKey()
    {
        if (IsVisible && WindowState != WindowState.Minimized && IsActive)
        {
            MinimizeOrHide();
            return;
        }

        ShowFromTray();
    }

    private void MinimizeOrHide()
    {
        if (_viewModel.MinimizeToTray)
        {
            HideToTray();
            return;
        }

        WindowState = WindowState.Minimized;
    }

    private void ApplyGlobalHotKeySetting()
    {
        if (_viewModel.GlobalHotKeyEnabled)
        {
            RegisterToggleHotKey();
            return;
        }

        UnregisterToggleHotKey();
    }

    private void RegisterToggleHotKey()
    {
        if (_isHotKeyRegistered)
        {
            return;
        }

        if (_windowHandle == IntPtr.Zero)
        {
            _windowHandle = new WindowInteropHelper(this).Handle;
        }

        if (_windowHandle == IntPtr.Zero)
        {
            return;
        }

        if (NativeMethods.RegisterHotKey(
            _windowHandle,
            ToggleWindowHotKeyId,
            ToggleWindowHotKeyModifiers,
            ToggleWindowHotKeyVirtualKey))
        {
            _isHotKeyRegistered = true;
            return;
        }

        _viewModel.GlobalHotKeyEnabled = false;
        WpfMessageBox.Show(
            this,
            _viewModel.L.GlobalHotKeyUnavailableMessage(_viewModel.GlobalHotKeyText),
            _viewModel.L.GlobalHotKeyUnavailableTitle,
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private void UnregisterToggleHotKey()
    {
        if (!_isHotKeyRegistered)
        {
            return;
        }

        NativeMethods.UnregisterHotKey(_windowHandle, ToggleWindowHotKeyId);
        _isHotKeyRegistered = false;
    }

    private void ApplyStartupSetting()
    {
        var requestedState = _viewModel.StartWithWindows;

        try
        {
            if (requestedState)
            {
                EnableWindowsStartup();
                return;
            }

            DisableWindowsStartup();
        }
        catch (Exception ex)
        {
            if (_viewModel.StartWithWindows == requestedState)
            {
                _viewModel.StartWithWindows = !requestedState;
            }

            WpfMessageBox.Show(
                this,
                $"{_viewModel.L.StartupSettingFailedMessage(requestedState)}\n\n{ex.Message}",
                _viewModel.L.StartupSettingFailedTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private static void EnableWindowsStartup()
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(StartupRegistryKeyPath, true)
            ?? throw new InvalidOperationException("Windows startup registry key is unavailable.");

        key.SetValue(
            StartupRegistryValueName,
            $"{Quote(GetExecutablePath())} {StartupLaunchArgument}",
            Microsoft.Win32.RegistryValueKind.String);
    }

    private static void DisableWindowsStartup()
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(StartupRegistryKeyPath, true);
        key?.DeleteValue(StartupRegistryValueName, false);
    }

    private static bool IsStartupLaunch()
    {
        return Environment
            .GetCommandLineArgs()
            .Any(argument => string.Equals(argument, StartupLaunchArgument, StringComparison.OrdinalIgnoreCase));
    }

    private static DrawingIcon CreateTrayIcon()
    {
        var executablePath = TryGetExecutablePath();
        if (executablePath is not null)
        {
            var icon = DrawingIcon.ExtractAssociatedIcon(executablePath);
            if (icon is not null)
            {
                return icon;
            }
        }

        return (DrawingIcon)DrawingSystemIcons.Application.Clone();
    }

    private static string GetExecutablePath()
    {
        return TryGetExecutablePath()
            ?? throw new InvalidOperationException("Application executable path is unavailable.");
    }

    private static string? TryGetExecutablePath()
    {
        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            executablePath = Process.GetCurrentProcess().MainModule?.FileName;
        }

        return string.IsNullOrWhiteSpace(executablePath)
            ? null
            : executablePath;
    }

    private static string Quote(string value)
    {
        return $"\"{value}\"";
    }

    private string GetTrayText()
    {
        var text = _viewModel.L.WindowTitle;
        return text.Length <= 63 ? text : text[..63];
    }

    private void DisposeShellResources()
    {
        UnregisterToggleHotKey();
        _hwndSource?.RemoveHook(WndProc);
        _hwndSource = null;

        if (_notifyIcon is not null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        _trayIcon?.Dispose();
        _trayIcon = null;
    }
}
