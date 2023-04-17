namespace ClipboardManager.Interfaces;

public interface ISecretDialogService
{
    string? ShowCreateSecretDialog(string suggestedName);
}
