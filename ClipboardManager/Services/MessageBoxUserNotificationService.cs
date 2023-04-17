using System.Windows;
using ClipboardManager.Interfaces;

namespace ClipboardManager.Services;

public sealed class MessageBoxUserNotificationService : IUserNotificationService
{
    public void ShowError(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
