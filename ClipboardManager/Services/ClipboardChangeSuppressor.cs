using ClipboardManager.Interfaces;

namespace ClipboardManager.Services;

public sealed class ClipboardChangeSuppressor
{
    private static readonly TimeSpan SuppressionWindow = TimeSpan.FromSeconds(2);

    private readonly object _syncRoot = new();
    private ClipboardContentSignature? _signature;
    private DateTimeOffset _expiresAt;

    public void Suppress(ClipboardContentSignature signature)
    {
        Suppress(signature, SuppressionWindow);
    }

    public void Suppress(ClipboardContentSignature signature, TimeSpan duration)
    {
        lock (_syncRoot)
        {
            _signature = signature;
            _expiresAt = DateTimeOffset.UtcNow.Add(duration);
        }
    }

    public bool ShouldSuppress(ClipboardContentSignature signature)
    {
        lock (_syncRoot)
        {
            if (_signature is null)
            {
                return false;
            }

            if (DateTimeOffset.UtcNow > _expiresAt)
            {
                ClearCore();
                return false;
            }

            if (_signature != signature)
            {
                ClearCore();
                return false;
            }

            return true;
        }
    }

    public void Clear()
    {
        lock (_syncRoot)
        {
            ClearCore();
        }
    }

    private void ClearCore()
    {
        _signature = null;
        _expiresAt = default;
    }
}
