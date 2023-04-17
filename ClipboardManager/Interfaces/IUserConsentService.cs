namespace ClipboardManager.Interfaces;

public enum SecretAccessKind
{
    Reveal,
    Copy
}

public interface IUserConsentService
{
    Task<bool> RequestSecretAccessAsync(
        string secretName,
        SecretAccessKind accessKind,
        CancellationToken cancellationToken = default);
}
