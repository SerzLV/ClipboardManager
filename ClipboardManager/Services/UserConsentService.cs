using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using ClipboardManager.Interfaces;
using ClipboardManager.Localization;
using Microsoft.Win32.SafeHandles;
using Windows.Security.Credentials.UI;

namespace ClipboardManager.Services;

public sealed class WindowsUserConsentService : IUserConsentService
{
    private const int MaxUserNameLength = 256;
    private const int MaxPasswordLength = 256;
    private const int NoError = 0;
    private const int ErrorCancelled = 1223;
    private const int Logon32LogonNetwork = 3;
    private const int Logon32ProviderDefault = 0;
    private const CredUiFlags PasswordPromptFlags =
        CredUiFlags.GenericCredentials
        | CredUiFlags.AlwaysShowUi
        | CredUiFlags.DoNotPersist
        | CredUiFlags.KeepUsername;

    private readonly LocalizationService _localization;

    public WindowsUserConsentService(LocalizationService localization)
    {
        _localization = localization;
    }

    public async Task<bool> RequestSecretAccessAsync(
        string secretName,
        SecretAccessKind accessKind,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (await TryRequestWindowsHelloAsync(secretName, accessKind, cancellationToken) is { } helloResult)
        {
            return helloResult;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return RequestWindowsPasswordOnUiThread(secretName, accessKind);
    }

    private async Task<bool?> TryRequestWindowsHelloAsync(
        string secretName,
        SecretAccessKind accessKind,
        CancellationToken cancellationToken)
    {
        try
        {
            var availability = await UserConsentVerifier.CheckAvailabilityAsync();
            if (availability != UserConsentVerifierAvailability.Available)
            {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var result = await UserConsentVerifier.RequestVerificationAsync(
                GetVerificationMessage(secretName, accessKind));

            return result == UserConsentVerificationResult.Verified;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private bool RequestWindowsPassword(string secretName, SecretAccessKind accessKind)
    {
        var userName = new StringBuilder(GetCurrentUserName(), MaxUserNameLength);
        var password = new StringBuilder(MaxPasswordLength);
        var save = false;
        var info = new CredUiInfo
        {
            Size = Marshal.SizeOf<CredUiInfo>(),
            Parent = GetMainWindowHandle(),
            CaptionText = _localization.WindowsPasswordPromptTitle,
            MessageText = GetVerificationMessage(secretName, accessKind)
        };

        var result = CredUIPromptForCredentials(
            ref info,
            "ClipboardManager.Secrets",
            IntPtr.Zero,
            NoError,
            userName,
            MaxUserNameLength,
            password,
            MaxPasswordLength,
            ref save,
            PasswordPromptFlags);

        if (result == ErrorCancelled)
        {
            return false;
        }

        if (result != NoError)
        {
            throw new Win32Exception(result);
        }

        try
        {
            return ValidateCredentials(userName.ToString(), password.ToString());
        }
        finally
        {
            Clear(password);
        }
    }

    private bool RequestWindowsPasswordOnUiThread(string secretName, SecretAccessKind accessKind)
    {
        var application = Application.Current;
        if (application is null || application.Dispatcher.CheckAccess())
        {
            return RequestWindowsPassword(secretName, accessKind);
        }

        return application.Dispatcher.Invoke(() => RequestWindowsPassword(secretName, accessKind));
    }

    private string GetVerificationMessage(string secretName, SecretAccessKind accessKind)
    {
        return accessKind == SecretAccessKind.Copy
            ? _localization.SecretCopyVerificationMessage(secretName)
            : _localization.SecretRevealVerificationMessage(secretName);
    }

    private static bool ValidateCredentials(string userName, string password)
    {
        SplitUserName(userName, out var domain, out var accountName);

        if (!LogonUser(
            accountName,
            domain,
            password,
            Logon32LogonNetwork,
            Logon32ProviderDefault,
            out var token))
        {
            return false;
        }

        token.Dispose();
        return true;
    }

    private static void SplitUserName(string userName, out string? domain, out string accountName)
    {
        var separatorIndex = userName.IndexOf('\\');
        if (separatorIndex > 0 && separatorIndex < userName.Length - 1)
        {
            domain = userName[..separatorIndex];
            accountName = userName[(separatorIndex + 1)..];
            return;
        }

        if (userName.Contains('@', StringComparison.Ordinal))
        {
            domain = null;
            accountName = userName;
            return;
        }

        domain = Environment.UserDomainName;
        accountName = userName;
    }

    private static string GetCurrentUserName()
    {
        return string.IsNullOrWhiteSpace(Environment.UserDomainName)
            ? Environment.UserName
            : $@"{Environment.UserDomainName}\{Environment.UserName}";
    }

    private static IntPtr GetMainWindowHandle()
    {
        var application = System.Windows.Application.Current;
        if (application is null)
        {
            return IntPtr.Zero;
        }

        return application.Dispatcher.CheckAccess()
            ? GetMainWindowHandleCore(application)
            : application.Dispatcher.Invoke(() => GetMainWindowHandleCore(application));
    }

    private static IntPtr GetMainWindowHandleCore(System.Windows.Application application)
    {
        return application.MainWindow is { } window
            ? new WindowInteropHelper(window).Handle
            : IntPtr.Zero;
    }

    private static void Clear(StringBuilder value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            value[index] = '\0';
        }
    }

    [DllImport("credui.dll", CharSet = CharSet.Unicode)]
    private static extern int CredUIPromptForCredentials(
        ref CredUiInfo creditUiInfo,
        string targetName,
        IntPtr reserved,
        int authError,
        StringBuilder userName,
        int maxUserName,
        StringBuilder password,
        int maxPassword,
        ref bool save,
        CredUiFlags flags);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool LogonUser(
        string userName,
        string? domain,
        string password,
        int logonType,
        int logonProvider,
        out SafeAccessTokenHandle token);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CredUiInfo
    {
        public int Size;
        public IntPtr Parent;
        public string MessageText;
        public string CaptionText;
        public IntPtr Banner;
    }

    [Flags]
    private enum CredUiFlags
    {
        DoNotPersist = 0x2,
        AlwaysShowUi = 0x80,
        GenericCredentials = 0x40000,
        KeepUsername = 0x100000
    }
}
