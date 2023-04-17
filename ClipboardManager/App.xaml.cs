using System.Windows;
using System.Threading;
using System.Threading.Tasks;
using ClipboardManager.Data;
using ClipboardManager.Interfaces;
using ClipboardManager.Localization;
using ClipboardManager.Services;
using ClipboardManager.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ClipboardManager;

public partial class App : System.Windows.Application
{
    private const string SingleInstanceMutexName = @"Local\ClipboardManager.SingleInstance";
    private const string ActivationEventName = @"Local\ClipboardManager.ActivateMainWindow";

    private Mutex? _singleInstanceMutex;
    private EventWaitHandle? _activationEvent;
    private CancellationTokenSource? _activationCancellation;
    private Task? _activationListenerTask;
    private ServiceProvider? _serviceProvider;
    private bool _ownsSingleInstanceMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        _singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            if (!IsStartupLaunch(e.Args))
            {
                SignalFirstInstance();
            }

            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
            Shutdown();
            return;
        }

        _ownsSingleInstanceMutex = true;
        StartActivationListener();

        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        MainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        MainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        StopActivationListener();

        if (_ownsSingleInstanceMutex)
        {
            _singleInstanceMutex?.ReleaseMutex();
        }

        _singleInstanceMutex?.Dispose();
        _singleInstanceMutex = null;

        _serviceProvider?.Dispose();
        _serviceProvider = null;

        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(AppSettingsStore.Load());
        services.AddSingleton(provider =>
        {
            var settings = provider.GetRequiredService<AppSettings>();
            return new LocalizationService(settings.Language);
        });

        services.AddSingleton<IClipboardRepository, ClipboardRepository>();
        services.AddSingleton<IClipboardFileCaptureService, ClipboardFileCaptureService>();
        services.AddSingleton<ILinkMetadataService, LinkMetadataService>();
        services.AddSingleton<IClipboardTextCaptureService, ClipboardTextCaptureService>();
        services.AddSingleton<ILinkPreviewImageService, LinkPreviewImageService>();
        services.AddSingleton<ILinkMetadataRefreshService, LinkMetadataRefreshService>();
        services.AddSingleton<IShellLauncher, ShellLauncher>();
        services.AddSingleton<IClipboardService, WpfClipboardService>();
        services.AddSingleton<IImageStorageService, ImageStorageService>();
        services.AddSingleton<IClipboardItemLookupService, ClipboardItemLookupService>();
        services.AddSingleton<IClipboardItemPersistenceService, ClipboardItemPersistenceService>();
        services.AddSingleton<IClipboardHistoryService, ClipboardHistoryService>();
        services.AddSingleton<IUserNotificationService, MessageBoxUserNotificationService>();
        services.AddSingleton<IClipboardTransferService, ClipboardTransferService>();
        services.AddSingleton<IClipboardTransferDialogService, ClipboardTransferDialogService>();
        services.AddSingleton<ISecretProtectionService, DpapiSecretProtectionService>();
        services.AddSingleton<IUserConsentService, WindowsUserConsentService>();
        services.AddSingleton<ISecretDialogService, SecretDialogService>();

        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<MainWindow>();
    }

    private void StartActivationListener()
    {
        _activationEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ActivationEventName);
        _activationCancellation = new CancellationTokenSource();
        _activationListenerTask = Task.Run(() => ListenForActivationRequests(_activationCancellation.Token));
    }

    private void StopActivationListener()
    {
        _activationCancellation?.Cancel();
        _activationEvent?.Set();

        try
        {
            _activationListenerTask?.Wait(TimeSpan.FromSeconds(1));
        }
        catch
        {
        }

        _activationEvent?.Dispose();
        _activationCancellation?.Dispose();
        _activationEvent = null;
        _activationCancellation = null;
        _activationListenerTask = null;
    }

    private void ListenForActivationRequests(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _activationEvent?.WaitOne();
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                _ = Dispatcher.InvokeAsync(ActivateMainWindow);
            }
        }
    }

    private void ActivateMainWindow()
    {
        if (Dispatcher.HasShutdownStarted)
        {
            return;
        }

        if (MainWindow is ClipboardManager.MainWindow window)
        {
            window.ActivateFromExternalInstance();
            return;
        }

        _ = Dispatcher.InvokeAsync(ActivateMainWindow, System.Windows.Threading.DispatcherPriority.ContextIdle);
    }

    private static void SignalFirstInstance()
    {
        try
        {
            using var activationEvent = EventWaitHandle.OpenExisting(ActivationEventName);
            activationEvent.Set();
        }
        catch (WaitHandleCannotBeOpenedException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static bool IsStartupLaunch(IEnumerable<string> arguments)
    {
        return arguments.Any(argument => string.Equals(
            argument,
            ClipboardManager.MainWindow.StartupLaunchArgument,
            StringComparison.OrdinalIgnoreCase));
    }
}
