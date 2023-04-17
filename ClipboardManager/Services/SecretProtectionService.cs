using System.Security.Cryptography;
using System.Text;
using ClipboardManager.Interfaces;

namespace ClipboardManager.Services;

public sealed class DpapiSecretProtectionService : ISecretProtectionService
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("ClipboardManager.Secrets.v1");

    public byte[] Protect(string secretText)
    {
        ArgumentNullException.ThrowIfNull(secretText);

        return ProtectedData.Protect(
            Encoding.UTF8.GetBytes(secretText),
            Entropy,
            DataProtectionScope.CurrentUser);
    }

    public string Unprotect(byte[] protectedValue)
    {
        ArgumentNullException.ThrowIfNull(protectedValue);

        var unprotectedBytes = ProtectedData.Unprotect(
            protectedValue,
            Entropy,
            DataProtectionScope.CurrentUser);

        return Encoding.UTF8.GetString(unprotectedBytes);
    }
}
