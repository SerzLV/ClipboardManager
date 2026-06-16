using ClipboardManager.Helper;

namespace ClipboardManager.ViewModels;

public sealed class LinkRefreshIntervalOption : BaseViewModel
{
    private string _displayName;

    public LinkRefreshIntervalOption(int days, string displayName)
    {
        Days = days;
        _displayName = displayName;
    }

    public int Days { get; }

    public string DisplayName
    {
        get => _displayName;
        set
        {
            if (_displayName == value)
            {
                return;
            }

            _displayName = value;
            OnPropertyChanged();
        }
    }
}
