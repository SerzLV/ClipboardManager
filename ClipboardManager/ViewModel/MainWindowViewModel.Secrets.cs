using System.Globalization;
using System.Windows.Input;
using ClipboardManager.Interfaces;
using ClipboardManager.Models;
using ClipboardManager.Services;

namespace ClipboardManager.ViewModels;

public sealed partial class MainWindowViewModel
{
    private async Task SaveTextAsSecretAsync(object? parameter)
    {
        if (parameter is not TextModel sourceText || string.IsNullOrWhiteSpace(sourceText.Text))
        {
            return;
        }

        var secretName = _secretDialogService.ShowCreateSecretDialog(CreateSuggestedSecretName());
        if (secretName is null)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            await _clipboardLock.WaitAsync();

            try
            {
                await _persistenceLock.WaitAsync();

                try
                {
                    var relatedUrls = FindRelatedUrls(sourceText.Text);
                    var secret = new SecretModel
                    {
                        Name = secretName,
                        ProtectedValue = _secretProtectionService.Protect(sourceText.Text),
                        CreatedAt = DateTimeOffset.Now,
                        IsPinned = sourceText.IsPinned
                    };

                    await _itemPersistenceService.SaveSecretFromTextAsync(secret, sourceText, relatedUrls);

                    BeginBulkCollectionUpdate();
                    try
                    {
                        Texts.Remove(sourceText);
                        _knownTexts.Remove(sourceText.Text);
                        foreach (var relatedUrl in relatedUrls)
                        {
                            Urls.Remove(relatedUrl);
                        }

                        RebuildUrlLookupIndexes();
                        Secrets.Add(secret);
                        _totalTextCount = Math.Max(0, _totalTextCount - 1);
                        _totalUrlCount = Math.Max(0, _totalUrlCount - relatedUrls.Length);
                        _totalSecretCount++;
                        _textPageOffset = Math.Max(0, _textPageOffset - 1);
                        _urlPageOffset = Math.Max(0, _urlPageOffset - relatedUrls.Length);
                        _secretPageOffset = Math.Min(_secretPageOffset + 1, _totalSecretCount);
                        ResetPageOffsetsFromLoadedCollections();
                    }
                    finally
                    {
                        EndBulkCollectionUpdate();
                    }

                    await _clipboardService.ClearTextIfCurrentTextEqualsAsync(sourceText.Text);
                    _lastHandledClipboardSignature = null;
                    SetStatus(text => text.SecretSavedStatus);
                }
                finally
                {
                    _persistenceLock.Release();
                }
            }
            finally
            {
                _clipboardLock.Release();
            }

            CommandManager.InvalidateRequerySuggested();
        });
    }

    private async Task CopySecretAsync(SecretModel secret)
    {
        try
        {
            var secretText = await GetAuthorizedSecretTextAsync(secret, SecretAccessKind.Copy);
            if (secretText is null)
            {
                return;
            }

            var signature = await _clipboardService.SetTextAsync(secretText);
            _clipboardChangeSuppressor.Suppress(signature, SecretClipboardSuppressionDuration);
            _lastHandledClipboardSignature = signature;
            ScheduleSecretClipboardClear(signature);
            SetStatus(text => text.SecretCopiedStatus(SecretClipboardClearSeconds, SecretCopyTrustSeconds));
        }
        catch (Exception ex)
        {
            ShowError(_localization.SecretVerificationFailedTitle, ex);
        }
    }

    private async Task RevealSecretAsync(object? parameter)
    {
        if (parameter is not SecretModel secret)
        {
            return;
        }

        try
        {
            var secretText = await GetAuthorizedSecretTextAsync(secret, SecretAccessKind.Reveal);
            if (secretText is null)
            {
                return;
            }

            secret.Reveal(secretText);
            StartSecretRevealTimer(secret);
            SetStatus(text => text.SecretRevealedStatus);
            CommandManager.InvalidateRequerySuggested();
        }
        catch (Exception ex)
        {
            ShowError(_localization.SecretVerificationFailedTitle, ex);
        }
    }

    private void HideSecret(object? parameter)
    {
        if (parameter is not SecretModel secret)
        {
            return;
        }

        HideSecretCore(secret, true);
        SetStatus(text => text.SecretHiddenStatus);
        CommandManager.InvalidateRequerySuggested();
    }

    private static bool CanSaveTextAsSecret(object? parameter)
    {
        return parameter is TextModel text && !string.IsNullOrWhiteSpace(text.Text);
    }

    private static bool CanRevealSecret(object? parameter)
    {
        return parameter is SecretModel { IsHidden: true, ProtectedValue.Length: > 0 };
    }

    private static bool CanHideSecret(object? parameter)
    {
        return parameter is SecretModel { IsRevealed: true };
    }

    private async Task<string?> GetAuthorizedSecretTextAsync(
        SecretModel secret,
        SecretAccessKind accessKind,
        CancellationToken cancellationToken = default)
    {
        if (accessKind == SecretAccessKind.Copy && IsSecretCopyTrusted(secret))
        {
            return UnprotectSecret(secret);
        }

        var isVerified = await _userConsentService.RequestSecretAccessAsync(
            secret.Name,
            accessKind,
            cancellationToken);

        if (!isVerified)
        {
            SetStatus(text => text.SecretAccessDeniedStatus);
            return null;
        }

        TrustSecretForCopy(secret);
        return UnprotectSecret(secret);
    }

    private string UnprotectSecret(SecretModel secret)
    {
        return _secretProtectionService.Unprotect(secret.ProtectedValue);
    }

    private void TrustSecretForCopy(SecretModel secret)
    {
        _trustedSecretCopyExpirations[secret] = DateTimeOffset.UtcNow.Add(SecretCopyTrustDuration);
    }

    private bool IsSecretCopyTrusted(SecretModel secret)
    {
        if (!_trustedSecretCopyExpirations.TryGetValue(secret, out var expiresAt))
        {
            return false;
        }

        if (DateTimeOffset.UtcNow <= expiresAt)
        {
            return true;
        }

        _trustedSecretCopyExpirations.Remove(secret);
        return false;
    }

    private void ForgetSecretCopyTrust(SecretModel secret)
    {
        _trustedSecretCopyExpirations.Remove(secret);
    }

    private void StartSecretRevealTimer(SecretModel secret)
    {
        CancelSecretRevealTimer(secret);

        var cancellation = new CancellationTokenSource();
        _secretRevealTimers[secret] = cancellation;
        _ = HideSecretAfterDelayAsync(secret, cancellation);
    }

    private async Task HideSecretAfterDelayAsync(
        SecretModel secret,
        CancellationTokenSource cancellation)
    {
        try
        {
            await Task.Delay(SecretRevealDuration, cancellation.Token);
            if (_secretRevealTimers.Remove(secret))
            {
                secret.Hide();
                CommandManager.InvalidateRequerySuggested();
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            cancellation.Dispose();
        }
    }

    private void HideSecretCore(SecretModel secret, bool cancelTimer)
    {
        if (cancelTimer)
        {
            CancelSecretRevealTimer(secret);
        }

        secret.Hide();
    }

    private void CancelSecretRevealTimer(SecretModel secret)
    {
        if (!_secretRevealTimers.Remove(secret, out var cancellation))
        {
            return;
        }

        cancellation.Cancel();
    }

    private void ScheduleSecretClipboardClear(ClipboardContentSignature signature)
    {
        _secretClipboardClearCancellation?.Cancel();
        _pendingSecretClipboardSignature = signature;

        var cancellation = new CancellationTokenSource();
        _secretClipboardClearCancellation = cancellation;
        _ = ClearSecretClipboardAfterDelayAsync(signature, cancellation);
    }

    private async Task ClearSecretClipboardAfterDelayAsync(
        ClipboardContentSignature signature,
        CancellationTokenSource cancellation)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(SecretClipboardClearSeconds), cancellation.Token);
            await _clipboardService.ClearIfCurrentSignatureMatchesAsync(signature, cancellation.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ShowError(_localization.CopyFailedTitle, ex);
        }
        finally
        {
            if (ReferenceEquals(_secretClipboardClearCancellation, cancellation))
            {
                _secretClipboardClearCancellation = null;
                _pendingSecretClipboardSignature = null;
            }

            cancellation.Dispose();
        }
    }

    private async Task ClearPendingSecretClipboardAsync(CancellationToken cancellationToken)
    {
        if (_pendingSecretClipboardSignature is null)
        {
            return;
        }

        var signature = _pendingSecretClipboardSignature;
        _pendingSecretClipboardSignature = null;

        _secretClipboardClearCancellation?.Cancel();
        _secretClipboardClearCancellation = null;

        await _clipboardService.ClearIfCurrentSignatureMatchesAsync(signature, cancellationToken);
    }

    private UrlModel[] FindRelatedUrls(string text)
    {
        return Urls
            .Where(url => !string.IsNullOrWhiteSpace(url.Url)
                && text.Contains(url.Url, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private string CreateSuggestedSecretName()
    {
        return string.Format(
            CultureInfo.CurrentCulture,
            "{0} {1:HH:mm}",
            _localization.SecretTypeLabel,
            DateTime.Now);
    }
}
