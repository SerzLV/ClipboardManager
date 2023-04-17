using System.ComponentModel;
using System.Runtime.CompilerServices;
using ClipboardManager.Interfaces;

namespace ClipboardManager.Models;

public abstract class ClipboardItemModel : IPinnedClipboardItem, INotifyPropertyChanged
{
    private bool _isPinned;

    public bool IsPinned
    {
        get => _isPinned;
        set
        {
            if (_isPinned == value)
            {
                return;
            }

            _isPinned = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
