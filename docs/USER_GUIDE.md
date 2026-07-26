# ClipVault Studio User Guide

This guide describes the current Free, trial, and Pro workflows in ClipVault Studio 2.1.0.

## First Launch

ClipVault Studio starts with an empty local history on a new Windows profile. It does not import Windows clipboard history or scan existing documents automatically.

The application begins recording supported clipboard changes while it is running:

- copied text;
- URLs contained in copied text;
- copied images;
- copied file references.

The default interface language is English. Russian can be selected in Settings.

## Quick Paste

Quick Paste is available in Free, trial, and Pro. Configure its dedicated global hotkey under **Settings > Hotkey**, then press it while working in another application.

The compact palette remembers the application that was active before it opened:

- recent text, links, files, and images appear immediately;
- typing searches the complete local history, including records not loaded in the main window;
- arrow keys change selection;
- `Enter` copies the selected item, restores the previous application, and requests paste;
- `Ctrl+Enter` copies only;
- `Escape` clears a non-empty query first and closes the palette when the query is empty.

Protected secrets never appear in Quick Paste. Opening or searching the palette does not modify history.

Windows can prevent one application from restoring or sending input to another, especially when the target is elevated. In that case ClipVault Studio keeps the selected content on the clipboard, reports the fallback, and asks you to press `Ctrl+V` manually. Before sending paste, the app verifies that the target window still belongs to the process that was active when the palette opened.

If the configured combination is already registered by another application, choose a different Quick Paste hotkey. On first use ClipVault Studio probes available combinations instead of silently replacing another global shortcut.

## Main History

The main tabs separate content into Favorites, Secrets, Files, Text, Links, and Images.

Each item exposes only the actions that apply to its type. Common actions include:

- add or remove from Favorites;
- copy back to the clipboard;
- open or preview;
- edit text or image description;
- add to a collection in Pro;
- delete.

The default order is newest first. Use the compact sort control beside Search to switch between newest-first and oldest-first ordering.

### Lazy Loading

ClipVault Studio loads history in batches while you scroll. The batch size is configurable in Settings:

- 25;
- 50;
- 100;
- 200;
- 500;
- 1000.

The default is 100. Larger batches reduce the number of loads but consume more memory and take longer to render.

### Search

Search queries the complete local SQLite history, not only the cards already visible in the UI. Search includes:

- text contents;
- file names and paths;
- URL, title, and description;
- image names and descriptions;
- secret names;
- favorites and collection content where applicable.

Typing is debounced and previous searches are cancelled when the query changes.

## Favorites

Favorites are normal history items with a pinned state. Favoriting an item does not duplicate its data.

Deleting the original item removes it from Favorites. Favorite state is included in `.clipboard.json` backups for regular history.

## Protected Secrets

Any text item can be saved as a named secret. Secret values are encrypted with Windows DPAPI for the current Windows user and remain masked in the interface.

When revealing or copying a secret:

1. ClipVault Studio requests Windows verification.
2. Windows Hello or PIN is used when available.
3. If that flow is unavailable, the app falls back to a Windows password credential prompt.
4. A successful secret copy allows repeat copies of that same secret for 30 seconds.

Revealed values hide again after 30 seconds. When automatic clearing is enabled, copied secret text is removed from the Windows clipboard after 45 seconds if the clipboard still contains that exact secret.

Secrets are not included in backup exports. Delete secrets individually when they are no longer needed.

ClipVault Studio provides convenient local secret storage, but it is not a replacement for a dedicated password manager.

## Links

When a URL is captured, ClipVault Studio can request the destination page to obtain:

- page title;
- description;
- Open Graph preview image.

Existing saved metadata appears immediately. Stale metadata can refresh in the background according to the selected interval:

- Never;
- 7 days;
- 30 days.

Link preview images are cached under `%LOCALAPPDATA%\ClipboardManager\Cache\LinkPreviews`. The cache can be cleared from Settings without deleting saved links.

For network safety, metadata requests accept only public HTTP/HTTPS destinations. Localhost, private network addresses, link-local addresses, and unsafe redirect targets are rejected.

## Images

Image history supports:

- thumbnail cards;
- full preview;
- mouse-wheel and button zoom;
- copy back to the clipboard;
- editable descriptions;
- search by name or description;
- save as PNG, JPG, or BMP.

Pro and the seven-day Pro trial add a focused annotation editor. Open an image preview or its Inspector and select Edit image. The editor supports:

- movable text annotations with configurable font size;
- outline rectangles with optional color fill and adjustable opacity;
- arrows plus straight lines and underlines with configurable thickness;
- a curated color palette;
- select, move, delete, clear, undo, and redo actions;
- zoom controls and automatic fit when the editor opens.

Select Save as copy to render the annotations into a new PNG entry in Images. The original history image is never modified or replaced. Closing or cancelling the editor discards unsaved annotations.

Image bytes are stored in the local history database and are included in regular backup exports.

## Text

Free includes a plain editor for long text snippets.

Pro adds automatic classification into:

- Plain;
- Data;
- Documents;
- Styles;
- Code.

Recognized exact formats include JSON, Markdown, SQL, C#, JavaScript, TypeScript, Python, XML, HTML, CSS, SCSS, YAML, plain text, and generic code.

Use the Text filter menu to select a category or exact format. Format indexing runs in the background and database-backed filtering does not require the user to scroll through the full history first.

### JSON

The JSON workbench provides:

- syntax highlighting;
- format;
- minify;
- validate;
- editor, tree, and split modes;
- navigable object/array tree;
- copy path and copy scalar value actions.

### Markdown

The Markdown workbench provides:

- syntax highlighting;
- headings, bold, italic, inline code, links, lists, quotes, and fenced code actions;
- rendered preview;
- editor, preview, and split modes;
- smooth preview synchronization based on the editor caret.

HTML tags embedded in Markdown do not automatically reclassify the whole document as HTML.

### SQL

SQL formatting and validation are powered by Microsoft ScriptDom.

### XML

XML tools provide formatting, minification, and validation.

### CSS And SCSS

Stylesheet tools provide formatting, minification, and structural validation for CSS and SCSS snippets.

### Other Code

C#, JavaScript, TypeScript, Python, HTML, YAML, and generic code use syntax-aware editing where a matching highlighting definition is available.

### Text Compare

Text Compare is available in Pro and during the seven-day Pro trial. Open it from the Compare icon in the application title bar to start with an empty two-file workspace. You can also open a text item or supported text file in the workbench and select Compare in the editor toolbar; the current document is then placed on the Original side.

Use the History button above either side to choose from the complete local Text library. The picker initially shows up to 40 newest text records and searches the database as you type, so older records do not need to be loaded by scrolling the main window. You can also use Choose file, drag one supported text or code file directly onto a pane, or paste and edit text manually. Selected history or file labels stay visible above their editors. Then select Compare. The workspace provides:

- aligned Original and Changed panes with physical source line numbers;
- added, removed, and modified line counts;
- optional word-level highlighting inside modified lines and theme-aware diff colors;
- synchronized vertical result scrolling, independent horizontal scrolling, and movable source/result splitters;
- an interactive change map on the right for long-document overview and navigation;
- instant Collapse unchanged mode that keeps nearby context visible;
- previous/next difference navigation;
- search across both result panes;
- independent syntax highlighting for the Original and Changed documents;
- optional Word wrap across all four editors, disabled by default and applied without recalculating the diff;
- Ignore spaces, Ignore empty lines, and Ignore case options;
- swap, searchable Text-history selection, choose file, drag-and-drop, clear, paste, and copy actions.

Keyboard navigation is available for repeated review work: `Ctrl+Enter` compares, `Ctrl+F` focuses result search, `F3` and `Shift+F3` move between matches, and `Alt+Down` / `Alt+Up` move between differences. `Escape` first closes an open Text-history picker or clears the active result search.

File loading uses the same guarded reader as the file workbench: only supported text formats up to 5 MiB are accepted. Reopening the workspace or closing the app cancels unfinished reads, and a stale read cannot replace the current document. Changing either input invalidates an older result and its search/navigation state. Comparison and result search run outside the UI thread, and a newer request supersedes an unfinished one. Result search is capped at 10,000 displayed matches so pathological inputs cannot grow UI state without bound. Collapse unchanged is enabled only when differences exist and only changes the displayed projection, so toggling it does not rerun the comparison or alter the added, removed, and modified counts. At the beginning or end of a document, only the context nearest a difference remains visible. Entering a search temporarily expands collapsed regions so matches in unchanged text remain discoverable; clearing the search restores the collapsed projection.

Text Compare is intentionally temporary and non-destructive. It has no auto-save and does not write edited text back to selected files, clipboard history, or the database. Use Copy when you want to keep a result elsewhere.

## Files

The Files tab stores file references and paths. It does not copy the original file into ClipVault Studio.

Pro can open supported text-based files in the shared workbench as read-only documents. Supported examples include:

- `.txt`, `.log`, `.csv`, `.tsv`, `.ini`;
- `.md`, `.markdown`;
- `.json`, `.sql`;
- `.cs`, `.csx`, `.js`, `.jsx`, `.mjs`, `.cjs`, `.ts`, `.tsx`, `.py`;
- `.xml`, `.svg`, `.xaml`, `.csproj`, `.props`, `.targets`, `.config`, `.manifest`;
- `.html`, `.htm`, `.css`, `.scss`, `.sass`, `.yaml`, `.yml`;
- common scripts, project files, source files, `Dockerfile`, `Makefile`, `.env`, and repository configuration files.

Preview restrictions:

- maximum file size: 5 MiB;
- binary files are rejected;
- text must use UTF-8 or a supported Unicode byte-order mark;
- preview is read-only and never overwrites the original file.

## Collections

Collections are a Pro organization layer over existing history.

Create a collection from the Collections pane, choose a name and accent color, then add items by:

- right-clicking a history item and choosing a collection;
- dragging a history item onto a collection.

A collection can contain mixed item types in one view. Deleting a collection removes only its membership records; the underlying clipboard items remain in history.

Deleting an underlying history item also removes stale collection references.

Collections are local and are not included in `.clipboard.json` backup files in the current format.

## AI Assist

AI Assist is a Pro feature and is also available during the seven-day Pro trial.

### Install The Model

The application package does not contain an AI model. Open AI Assist and choose Download to install the recommended model:

- Qwen3 4B;
- `Q4_K_M` quantization;
- approximately 2.5 GB;
- Apache 2.0 license.

The model downloads from the official Qwen repository on Hugging Face. Interrupted downloads can resume. The app verifies the completed file with SHA-256 before it can be used.

The model can be removed from AI Assist at any time.

### AI Actions

- Generate;
- Improve writing;
- Summarize;
- Translate;
- Explain text;
- Explain code.

AI Assist can open as a full workspace or as a movable popover inside the text/file editor. The popover keeps the current response while the same document context remains active and can be expanded into the full workspace.

Response controls include language, style, detail level, source format preservation, code block preservation, and risk mentions where relevant.

### Workspace Layout

The full AI Assist workspace keeps the response in the center and allows both side panels to be hidden independently:

- actions and library: use the left-panel button or `Ctrl+Alt+L`;
- run settings: use the settings-panel button or `Ctrl+Alt+R`;
- advanced context and history options stay collapsed until needed.

Hiding either panel expands the response workspace without clearing the source, request, or current response. The panel state remains available while the AI Assist window stays open in the current app session.

### Saved Prompts And History

Saved prompts remain until deleted.

AI response history is local and encrypted with Windows DPAPI:

- default retention: 7 days;
- options: Off, 1 day, 7 days, 30 days;
- maximum: 200 unpinned entries;
- pinned entries are preserved from normal retention cleanup;
- source text is not saved by default;
- enabling "Save source text" stores the supplied source with the encrypted history entry.

Clear History removes unpinned entries. Pinned entries can be deleted individually.

Local inference can produce incorrect, incomplete, or unsafe suggestions. Review generated content before using it in documents, code, credentials, or production systems.

## Free, Trial, And Purchase

Free features do not expire.

The seven-day Pro trial:

- starts only after user confirmation;
- unlocks the current Pro feature set;
- runs for seven consecutive days;
- cannot be paused;
- returns the app to Free when it expires;
- does not delete user-created history or collections.

Pro can be activated through either a monthly Microsoft Store subscription or a one-time Lifetime purchase. The plan chooser shows Store-localized prices. Microsoft Store handles checkout, subscription renewal/cancellation, and entitlement restoration; no ClipVault account is required. Monthly subscribers can manage billing through their Microsoft account and can upgrade to Lifetime at any time. Existing Lifetime owners keep permanent Pro access.

Clipboard monitoring remains a Free capability. Trial expiration or a temporary Store-license refresh never stops capture; only Pro tools become locked.

## Settings

Current settings include:

- interface language;
- minimize to tray;
- start with Windows;
- enable and configure the global hotkey;
- enable and configure the separate Quick Paste hotkey;
- automatically clear copied secrets;
- history load batch size;
- link refresh interval;
- import and export;
- clear link preview cache;
- clear regular clipboard history;
- AI history retention and source-text storage inside AI Assist.

The **About** page shows the installed version and edition and provides direct links to Microsoft Store, this user guide, privacy information, support, and the bundled third-party notices.

History sort order is controlled from the compact sort button beside Search and is remembered automatically.

The default global hotkey configuration can be changed to avoid conflicts with other applications.

## Backup And Restore

Export creates a `.clipboard.json` file containing:

- export version and timestamp;
- file paths and display names;
- text snippets;
- image bytes, names, and descriptions;
- URLs and cached metadata;
- favorite state.

The export intentionally excludes:

- secrets;
- collections and collection membership;
- AI saved prompts and history;
- application settings;
- downloaded AI model;
- trial state and Store license data.

Import merges regular data into the current database and avoids obvious duplicates. File entries remain references to their original paths.

## Data Locations

```text
%LOCALAPPDATA%\ClipboardManager\clipboardDatabase.sqlite
%LOCALAPPDATA%\ClipboardManager\Cache\LinkPreviews
%LOCALAPPDATA%\ClipboardManager\AI\ai-assistant.sqlite
%LOCALAPPDATA%\ClipboardManager\Models
%LOCALAPPDATA%\ClipboardManager\Logs\crash.log
%APPDATA%\ClipboardManager\settings.json
```

The `CLIPVAULT_DATA_DIR` environment variable can redirect Local and Roaming application data roots for development and automated testing.

## Troubleshooting

### Diagnostics And Recoverable Errors

ClipVault Studio records startup, shutdown, and unexpected failures in a bounded local diagnostic log. Recoverable errors remain visible in the application status with a short error ID instead of silently closing the application.

Open **Settings > Diagnostics** to:

- open the current log;
- open the log folder;
- copy a support summary;
- dismiss the current error;
- clear the current and previous logs.

The current log is limited to 512 KiB and rotates once to `crash.previous.log`, so both files use approximately 1 MiB at most. Logs remain on the device and are never uploaded automatically. Review them before sharing because file paths or clipboard-related exception context may be present.

### The App Does Not Capture A Copy

- Confirm ClipVault Studio is running.
- Check whether another application owns or repeatedly rewrites the clipboard.
- Retry the copy after the Windows clipboard is available.
- Report persistent listener errors with the application and Windows version.

### Build Output Is Locked

Close ClipVault Studio, including the tray process, before rebuilding. A running `ClipboardManager.exe` locks the target apphost.

### A Link Has No Preview

The destination may block automated metadata requests, return no Open Graph data, exceed the timeout, or resolve to an address rejected by the network safety policy. The URL itself remains available.

### AI Download Was Interrupted

Open AI Assist and choose Resume. If the server does not accept the previous range, the app safely restarts the partial transfer. The model is not marked installed until SHA-256 verification succeeds.

### AI Is Slow

Inference runs on the CPU. Performance depends on processor speed, available memory, prompt size, source size, and requested response length.

### Pro Is Not Detected

- Confirm the app was installed from Microsoft Store.
- Confirm Windows is signed in to the Microsoft account that owns the Lifetime add-on or active Monthly subscription.
- Restart the Store app and ClipVault Studio.
- Use the purchase/restore flow again; an already-owned add-on is not charged twice.

See [SUPPORT.md](../SUPPORT.md) before sharing diagnostics.
