# ClipVault Studio User Guide

This guide describes the current Microsoft Store Free, trial, and Pro
workflows in ClipVault Studio 2.0.4. This public GitHub repository contains
product documentation and support resources; Microsoft Store is the official
application distribution channel.

## First Launch

ClipVault Studio starts with an empty local history on a new Windows profile. It does not import Windows clipboard history or scan existing documents automatically.

The application begins recording supported clipboard changes while it is running:

- copied text;
- URLs contained in copied text;
- copied images;
- copied file references.

The default interface language is English. Russian can be selected in Settings.

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

The current desktop interface supports light and dark themes. Select an item in a supported library or collection view to open its Inspector, where focused preview, metadata, and type-specific actions remain available without leaving the current list.

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

## Text Compare

Text Compare is available in Pro and during the seven-day Pro trial. Open it from the main toolbar or from a supported text/file workbench.

Each side can be populated independently from:

- searchable text history;
- a supported local text file;
- pasted text;
- the current workbench content when opened from an editor.

Select **Compare** after both sides are ready. Available options include ignoring spaces, empty lines, or letter case, plus word-level differences and syntax colors.

The result workspace provides:

- aligned left/right rows and original source line numbers;
- separate added, removed, and modified counters;
- word-level highlights inside changed lines;
- synchronized vertical scrolling while horizontal scrolling remains independent;
- movable splitters for the source and result panes;
- a minimap showing change locations across the whole comparison;
- search and previous/next difference navigation;
- **Collapse unchanged**, which replaces long unchanged runs with a compact separator while preserving nearby context.

Comparison work runs in the background and superseded requests are cancelled. Programmatic scroll updates are suppressed briefly to prevent feedback loops and visible jumping between panes.

Text Compare is intentionally non-destructive. It has no autosave path, does not overwrite selected files, and does not modify clipboard history or the database. Copy any result that you want to retain before closing the window.

## Collections

Collections are a Pro organization layer over existing history.

Create a collection from the Collections pane, choose a name and accent color, then add items by:

- right-clicking a history item and choosing a collection;
- dragging a history item onto a collection.

A collection can contain mixed item types in one view. Deleting a collection removes only its membership records; the underlying clipboard items remain in history.

Selecting an item in a collection updates the Inspector with its preview, metadata, and available actions. This keeps mixed text, links, images, and files in one collection workflow instead of switching between library tabs.

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

In the full AI workspace, the left Actions/Library panel and the right Run Settings panel can be hidden independently. Use `Ctrl+Alt+L` and `Ctrl+Alt+R` to toggle them. Advanced run settings remain collapsed until requested, reducing visual noise during normal generation.

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

After the trial, Pro is available through either:

- a monthly Microsoft Store subscription;
- a one-time Lifetime Microsoft Store purchase.

Both plans unlock the same current Pro feature set. The Lifetime purchase does not expire. Subscription renewal, cancellation, purchase restoration, regional availability, and payment processing are handled by Microsoft Store. No ClipVault account is required.

## Settings

Current settings include:

- interface language;
- light or dark appearance;
- minimize to tray;
- start with Windows;
- enable and configure the global hotkey;
- automatically clear copied secrets;
- history load batch size;
- link refresh interval;
- import and export;
- clear link preview cache;
- clear regular clipboard history;
- open or clear bounded local diagnostic logs;
- AI history retention and source-text storage inside AI Assist.

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
%LOCALAPPDATA%\ClipboardManager\Logs\crash.previous.log
%APPDATA%\ClipboardManager\settings.json
```

The `CLIPVAULT_DATA_DIR` environment variable can redirect Local and Roaming application data roots for development and automated testing.

## Troubleshooting

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

### Text Compare Looks Misaligned Or Jumps

- Run Compare again after changing either source or an ignore option.
- Confirm both sides are plain supported text and the selected source files still exist.
- Disable **Collapse unchanged** temporarily when inspecting original line placement.
- Vertical movement is synchronized; horizontal scrolling is intentionally independent for differently indented or long lines.
- If scrolling repeatedly changes without input, include sanitized line counts and the application version in the support report.

### Pro Is Not Detected

- Confirm the app was installed from Microsoft Store.
- Confirm Windows is signed in to the Microsoft account that owns the Lifetime add-on or active monthly subscription.
- Restart the Store app and ClipVault Studio.
- Use the purchase/restore flow again. ClipVault Studio verifies existing Store entitlement before presenting a new purchase.

See [SUPPORT.md](../SUPPORT.md) before sharing diagnostics.
