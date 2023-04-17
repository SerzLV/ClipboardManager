using System.Globalization;

namespace ClipboardManager.Models;

public sealed class SecretModel : ClipboardItemModel
{
    public const string MaskedValue = "********";

    private string? _revealedText;

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public byte[] ProtectedValue { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

    public string DisplayValue => _revealedText ?? MaskedValue;
    public bool IsRevealed => _revealedText is not null;
    public bool IsHidden => !IsRevealed;
    public string CreatedAtText => CreatedAt.LocalDateTime.ToString("g", CultureInfo.CurrentCulture);

    public void Reveal(string secretText)
    {
        ArgumentNullException.ThrowIfNull(secretText);

        _revealedText = secretText;
        OnPropertyChanged(nameof(DisplayValue));
        OnPropertyChanged(nameof(IsRevealed));
        OnPropertyChanged(nameof(IsHidden));
    }

    public void Hide()
    {
        if (_revealedText is null)
        {
            return;
        }

        _revealedText = null;
        OnPropertyChanged(nameof(DisplayValue));
        OnPropertyChanged(nameof(IsRevealed));
        OnPropertyChanged(nameof(IsHidden));
    }
}
