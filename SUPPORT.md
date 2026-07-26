# ClipVault Studio Support

Publisher: Serz Studio

Support email: clipboardmanager.app@outlook.com

Public issue tracker: https://github.com/SerzLV/ClipboardManager/issues

## Before Reporting A Problem

Please include:

- ClipVault Studio version.
- Windows version and whether the system is x64.
- Confirm that the application was installed or updated through Microsoft Store.
- Current edition: Free, 7-day trial, or Pro.
- The affected area: clipboard capture, search, secret, link, image, file preview, text workbench, Text Compare, collection, AI Assist, tray, startup, hotkey, import/export, purchase, or trial.
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

For Text Compare issues, also include:

- whether each side came from clipboard history, pasted text, or a local file;
- the enabled ignore/word-diff options;
- approximate line counts and whether Collapse unchanged was enabled;
- whether the issue affects scrolling, the minimap, search, or rendered differences.

Use sanitized sample text when possible. Text Compare does not autosave its input or result, so retain any minimal reproduction separately before closing the comparison window.

For Microsoft Store licensing issues, state whether Windows is signed in to the account that owns the Lifetime add-on or active monthly subscription. Do not include receipts, payment data, account tokens, or authentication screenshots containing personal information.

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
- Restart Microsoft Store and ClipVault Studio if a completed Pro purchase or subscription is not immediately reflected.

See [docs/USER_GUIDE.md](docs/USER_GUIDE.md) for feature behavior and troubleshooting.
