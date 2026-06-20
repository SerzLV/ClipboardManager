# Privacy Notice

ClipboardManager is designed as a local Windows desktop application. It does
not use cloud sync and does not intentionally send your clipboard history to
the author.

## Local Data

Clipboard history is stored locally in SQLite under:

```text
%LOCALAPPDATA%\ClipboardManager\clipboardDatabase.sqlite
```

Link preview images are cached locally under:

```text
%LOCALAPPDATA%\ClipboardManager\Cache\LinkPreviews
```

Application settings are stored under:

```text
%APPDATA%\ClipboardManager\settings.json
```

Secrets are stored in the local database, but secret values are encrypted with
Windows DPAPI and scoped to the current Windows user.

## Network Access

When you copy or refresh a web link, ClipboardManager may request that web page
and its preview image in order to show the title, description, and preview
card. Those requests are made directly from your machine to the linked website
or image host.

ClipboardManager does not provide analytics, telemetry, advertising tracking,
or cloud backup.

## User Control

You can delete individual records, clear regular clipboard history, clear
cached link preview images, disable link refresh, export/import regular
history, and delete secrets directly from the app.

Exports intentionally exclude secrets.

## Sensitive Content

Clipboard history can include passwords, tokens, personal messages, files,
images, links, or other sensitive data. Review saved entries regularly and
delete anything you do not want stored locally.
