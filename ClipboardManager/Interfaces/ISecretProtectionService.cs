namespace ClipboardManager.Interfaces;

public interface ISecretProtectionService
{
    byte[] Protect(string secretText);
    string Unprotect(byte[] protectedValue);
}
