# ClipVault Studio Support

Publisher: Serz Studio

Support email: clipboardmanager.app@outlook.com

Public issue tracker: https://github.com/SerzLV/ClipboardManager/issues

## Before Reporting A Problem

Please include:

- ClipVault Studio version.
- Windows version and whether the system is x64.
- Installation source: Microsoft Store.
- Current edition: Free, 7-day trial, or Pro.
- The affected area: clipboard capture, search, Quick Paste, secret, link, image, file preview, text workbench, collection, AI Assist, tray, startup, hotkey, import/export, purchase, or trial.
- Clear reproduction steps.
- What you expected.
- What happened instead.
- Whether the issue occurs after restarting the application.
- The short error ID shown by ClipVault Studio, when available.

Open **Settings > Diagnostics** to copy a support summary or inspect the bounded local log. Review and sanitize it before sharing. Do not attach the log publicly when it contains private paths or other sensitive context.

For AI issues, also include:

- model status: not installed, downloading, verifying, ready, or failed;
- whether the problem occurs in the popover, full AI workspace, or both;
- the selected AI action and response language;
- approximate source length, without including private source text.

For Microsoft Store licensing issues, state whether Windows is signed in to the account that owns the Pro add-on. Do not include receipts, payment data, account tokens, or authentication screenshots containing personal information.

For Quick Paste issues, include:

- the configured Quick Paste hotkey;
- whether the palette opened and showed recent/search results;
- whether `Ctrl+Enter` copied successfully;
- whether normal `Enter` pasted, fell back to copy-only, or targeted an elevated application.

## Sensitive Information

Do not post any of the following in a public issue:

- passwords or secret values;
- access tokens, cookies, API keys, or connection strings;
- private clipboard text;
- personal images;
- confidential source code or documents;
- full local databases;
- AI prompts, responses, or source text that contain private data.
- unsanitized diagnostic logs that contain personal file paths or other private context.

Create a minimal sanitized example whenever possible.

## Common Checks

- Make sure the tray process is closed before rebuilding the application.
- Confirm the copied content is supported and the Windows clipboard is not locked by another application.
- Confirm the original file still exists before opening a stored file reference.
- Confirm the optional AI model has completed SHA-256 verification.
- Confirm Pro features are active through either the trial or Microsoft Store entitlement.
- Restart Microsoft Store and ClipVault Studio if a completed Pro purchase is not immediately reflected.
- If Quick Paste cannot paste into an elevated application, use `Ctrl+Enter` and paste manually from a process with matching privileges.

See [docs/USER_GUIDE.md](docs/USER_GUIDE.md) for feature behavior and troubleshooting.
