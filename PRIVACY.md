# ClipVault Studio Privacy Notice

Effective date: July 16, 2026

Publisher: Serz Studio

Contact: clipboardmanager.app@outlook.com

ClipVault Studio is designed as a local Windows desktop application. It does not use cloud synchronization, advertising, publisher-operated analytics, or publisher-operated telemetry. Clipboard history is not intentionally sent to Serz Studio.

ClipVault Studio does not sell personal information.

## Data The App Processes

While ClipVault Studio is running, it can read supported content copied by the current Windows user:

- text;
- URLs contained in text;
- images;
- file references and paths.

The application processes this data to provide history, search, Quick Paste, favorites, image descriptions, link previews, text editing, collections, secure secrets, file previews, and optional local AI assistance.

ClipVault Studio does not automatically scan the user's documents or import the Windows clipboard history on first launch.

### Quick Paste

When the user opens Quick Paste, ClipVault Studio reads recent or matching records from the same local clipboard database. Protected secrets are excluded.

When the user confirms a result, the app places that selected value on the Windows clipboard. For a normal `Enter` action, it also attempts to restore the application that was active before Quick Paste opened and sends the standard paste shortcut. `Ctrl+Enter` copies without sending paste. This processing stays on the device and does not contact Serz Studio or a cloud service.

Windows can reject focus restoration or synthetic input. In that case the selected value remains on the clipboard for manual paste. The app validates that the target window still belongs to the originally captured process before sending input.

## Local Storage

### Clipboard History

Regular clipboard history, images, link metadata, favorites, encrypted secrets, collections, and collection membership are stored in:

```text
%LOCALAPPDATA%\ClipboardManager\clipboardDatabase.sqlite
```

File history records store file names and paths. ClipVault Studio does not copy the original file contents into the clipboard database.

Image history records store image bytes in the local database.

### Link Preview Cache

Downloaded link preview images are cached under:

```text
%LOCALAPPDATA%\ClipboardManager\Cache\LinkPreviews
```

### Application Settings

Application preferences are stored under:

```text
%APPDATA%\ClipboardManager\settings.json
```

### Local Diagnostics

ClipVault Studio records startup, shutdown, and unexpected application failures in:

```text
%LOCALAPPDATA%\ClipboardManager\Logs\crash.log
%LOCALAPPDATA%\ClipboardManager\Logs\crash.previous.log
```

The current log rotates at 512 KiB and only one previous log is retained. Diagnostic entries can include the application and Windows version, error source, stack trace, support error ID, and file paths present in exception context. Clipboard content is not intentionally written as a diagnostic field, but exception messages from Windows or a dependency can contain contextual data.

Logs remain local, are never uploaded automatically, and can be opened or cleared from **Settings > Diagnostics**. Users should review and sanitize a log before sharing it.

### Optional AI Model

When the user chooses to install AI Assist, the model and verification files are stored under:

```text
%LOCALAPPDATA%\ClipboardManager\Models
```

The model is not included in the Microsoft Store package.

### AI Saved Prompts And History

Saved prompts and AI response history are stored in:

```text
%LOCALAPPDATA%\ClipboardManager\AI\ai-assistant.sqlite
```

Prompt names, prompt instructions, AI instructions, AI responses, and optional source text are encrypted with Windows DPAPI and scoped to the current Windows user.

AI history behavior:

- history retention is 7 days by default;
- available retention settings are Off, 1 day, 7 days, and 30 days;
- unpinned history is limited to the newest 200 entries;
- pinned entries are not removed by normal age/count retention;
- source text is not saved by default;
- source text is stored only when the user enables the corresponding option.

### Trial State

If the user starts the seven-day Pro trial, protected trial state is stored locally in DPAPI-protected per-user anchors:

```text
%LOCALAPPDATA%\ClipboardManager\License\trial-license.bin
%APPDATA%\ClipboardManager\License\trial-license.bin
HKCU\Software\Serz Studio\ClipVault Studio
```

This state is used only to determine trial availability, integrity, start time, and expiration for the current Windows user/device.

## Protected Secrets

Secrets are stored in the main local database, but secret values are encrypted with Windows DPAPI using `DataProtectionScope.CurrentUser`.

Secret names remain searchable. Secret values remain masked unless the user explicitly requests reveal or copy and completes Windows verification.

The app can temporarily place a decrypted secret on the Windows clipboard after a verified copy action. When automatic clearing is enabled, the app attempts to clear that value after 45 seconds if the clipboard still contains the same secret.

## Network Access

### Link Metadata

When a URL is copied or refreshed, ClipVault Studio may request:

- the linked web page;
- a preview image referenced by that page.

These requests are made directly from the user's device to the destination website or image host. The destination can receive standard network information such as the user's IP address and request headers.

ClipVault Studio rejects local, loopback, private, link-local, and other non-public network targets and validates redirects before connecting.

### AI Model Download

If the user chooses to install AI Assist, ClipVault Studio downloads the selected Qwen model from the official Qwen repository hosted by Hugging Face. The file is verified locally with SHA-256 before use.

Hugging Face and its infrastructure providers can receive standard network information related to that download according to their own privacy practices.

### Local AI Inference

AI prompts, source text, and generated responses are processed locally through LLamaSharp. They are not sent to Serz Studio, Hugging Face, OpenAI, Microsoft AI services, or another cloud AI endpoint by ClipVault Studio.

Microsoft Store can process normal product, purchase, license, and account information when the app checks or purchases the durable Pro add-on. That processing is governed by Microsoft's privacy terms. ClipVault Studio does not receive the user's payment card details.

## Backup And Import

`.clipboard.json` exports can include:

- file names and paths;
- text snippets;
- image bytes, names, and descriptions;
- URLs and cached metadata;
- favorite state;
- export timestamp and format version.

Exports intentionally exclude:

- secrets;
- collections and collection membership;
- saved AI prompts and AI history;
- application settings;
- downloaded AI model files;
- trial state;
- Microsoft Store entitlement or payment data.

The user chooses where an export file is stored and is responsible for protecting it. Export files are readable JSON and are not encrypted by ClipVault Studio.

## User Controls

The user can:

- delete individual clipboard records;
- clear regular clipboard history;
- delete individual secrets;
- delete collections without deleting their underlying history items;
- clear cached link preview images;
- disable link refresh;
- export or import regular history;
- delete saved AI prompts;
- delete individual AI history entries;
- clear unpinned AI history;
- disable AI history;
- choose whether source text is stored in AI history;
- remove the optional AI model;
- open or clear local diagnostic logs.

Uninstalling the app removes application binaries. Windows or the package manager may leave local data under the user's profile. The user can remove the folders listed above when the data is no longer required.

## Sensitive Content

Clipboard history can contain passwords, access tokens, personal messages, file paths, images, links, source code, or other sensitive information. Review stored records regularly and delete anything that should not remain on the device.

ClipVault Studio includes local secure-secret features, but it is not a replacement for a dedicated password manager, enterprise data-loss-prevention system, or security-audited secret-management product.

Local AI output can be incomplete or incorrect. Users should review generated or transformed content before using it.

## Contact

Privacy questions can be sent to:

clipboardmanager.app@outlook.com
