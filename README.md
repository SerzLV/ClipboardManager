# ClipVault Studio

[Get ClipVault Studio from Microsoft Store](https://apps.microsoft.com/detail/9N7B9J7F81WF)

ClipVault Studio is a private Windows clipboard workspace for people who work
with text, links, screenshots, images, and file references throughout the day.
It keeps clipboard history on the device, provides complete-history search,
adds keyboard-first Quick Paste, and offers optional Pro tools for organization,
structured text, file preview, image annotation, text comparison, and local AI
assistance.

This public repository is the official **documentation and support portal** for
ClipVault Studio. Application source code and build infrastructure are not
published here.

Current version: **2.1.0**.

## Editions

### Free

- Clipboard history for files, text, links, and images.
- Favorites and protected secrets.
- Complete-history search, sorting, and incremental loading.
- Quick Paste palette for recent and searched text, links, files, and images.
- Link cards and local preview caching.
- Image preview, descriptions, search, copy, and save.
- Tray mode, Windows startup, configurable app and Quick Paste hotkeys, and
  JSON backup/import.
- Light and dark themes with English and Russian localization.

## Quick Paste

Quick Paste opens a compact mixed-history palette over the application you
were using:

- press the configurable Quick Paste hotkey to see recent clipboard items;
- type to search the complete local history;
- press `Enter` to paste into the previously active application;
- press `Ctrl+Enter` to copy without pasting;
- press `Escape` to clear the query, then again to close the palette.

Protected secrets never appear in Quick Paste. Before pasting, ClipVault Studio
revalidates the destination window and owning process. If Windows blocks focus
restoration or synthetic input, the item remains copied and the app reports a
copy-only fallback instead of pasting into an unrelated window.

### Pro

Eligible users can evaluate Pro with a one-time seven-day local trial. Pro can
then be unlocked through Microsoft Store with a monthly subscription or a
one-time Lifetime purchase.

Pro adds:

- Collections with mixed item types, context actions, and drag-and-drop.
- Smart text categories and exact format filters.
- Structured editors and previews for JSON, Markdown, SQL, XML, CSS/SCSS,
  YAML, HTML, and common programming languages.
- Read-only preview for supported text-based files.
- Local Text Compare with aligned line and word differences, search, change
  navigation, synchronized scrolling, a minimap, collapsible unchanged
  regions, word wrap, and movable splitters.
- Quick image annotation with text, highlights, shapes, arrows, colors,
  opacity, and save-as-copy.
- Optional local AI Assist for generation, rewriting, summarization,
  translation, and explanation of text or code.

Trial expiration returns the application to Free without deleting regular
clipboard history or user-created data. Microsoft Store handles subscription
renewal, cancellation, Lifetime ownership, purchase restoration, regional
availability, and payment processing.

## Privacy

Clipboard history is stored locally. ClipVault Studio does not use cloud
synchronization, advertising, publisher-operated analytics, or
publisher-operated telemetry.

Network access is limited to user-visible features such as:

- loading metadata and preview images for copied public links;
- downloading the optional Qwen model after explicit user confirmation;
- Microsoft Store licensing and purchase flows handled by Windows.

The optional AI model is not included in the Store package. AI inference runs
locally on the CPU through LLamaSharp. Saved prompts and optional AI response
history are encrypted on the device.

Read the complete [Privacy Notice](PRIVACY.md).

## About And Diagnostics

**Settings > About** shows the installed version and active edition and provides
direct access to Microsoft Store, the user guide, privacy notice, support, and
the third-party notices shipped with the app.

**Settings > Diagnostics** provides a bounded local crash log and a sanitized
support-summary workflow. Logs are never uploaded automatically.

## Screenshots

### Dark Theme

![ClipVault Studio dark theme](docs/screenshots/themes/dark-01-library.png)

### Light Theme

![ClipVault Studio light theme](docs/screenshots/themes/light-01-library.png)

### Pro Text Workbench

![ClipVault Studio text workbench](docs/screenshots/themes/dark-02-workbench.png)

### Settings

![ClipVault Studio settings](docs/screenshots/themes/light-03-settings.png)

## Documentation

- [User Guide](docs/USER_GUIDE.md)
- [Privacy Notice](PRIVACY.md)
- [Support and Troubleshooting](SUPPORT.md)
- [Security Policy](SECURITY.md)
- [Disclaimer](DISCLAIMER.md)
- [Release Notes](RELEASE_NOTES.md)
- [Open-Source Components and Qwen Model](docs/OPEN_SOURCE.md)
- [Third-Party Notices](THIRD-PARTY-NOTICES.txt)

## Local Data

Depending on the features used, ClipVault Studio can create data in:

```text
%LOCALAPPDATA%\ClipboardManager\clipboardDatabase.sqlite
%LOCALAPPDATA%\ClipboardManager\Cache\LinkPreviews
%LOCALAPPDATA%\ClipboardManager\AI\ai-assistant.sqlite
%LOCALAPPDATA%\ClipboardManager\Models
%LOCALAPPDATA%\ClipboardManager\Logs\crash.log
%LOCALAPPDATA%\ClipboardManager\Logs\crash.previous.log
%APPDATA%\ClipboardManager\settings.json
```

The AI database and Models folder are created only when the related Pro
features are used. Diagnostic logs are bounded and are never uploaded
automatically.

Backup exports contain regular clipboard history. They do not contain secrets,
collections, AI prompts or history, settings, model files, trial state, or
Microsoft Store entitlement data.

## Download

Microsoft Store is the official distribution channel and provides automatic
updates, trial eligibility, and paid entitlement restoration:

[Download ClipVault Studio](https://apps.microsoft.com/detail/9N7B9J7F81WF)

This repository does not distribute installers, portable builds, application
packages, or source archives.

## Support

- Email: clipboardmanager.app@outlook.com
- Issues: [SerzLV/ClipboardManager issues](https://github.com/SerzLV/ClipboardManager/issues)
- Reporting guide: [SUPPORT.md](SUPPORT.md)

Do not include clipboard contents, passwords, tokens, private documents,
database files, AI history, or other sensitive data in public issues.
