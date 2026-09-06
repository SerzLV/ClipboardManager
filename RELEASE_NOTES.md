# ClipVault Studio 2.1.6

ClipVault Studio 2.1.6 preserves collections in backups and improves restoration of edited and previously deleted history.

## Backup And Restore

- Included collection names, colours, and membership in standard and password-protected backups.
- Added persistent record identifiers and capture timestamps to the version 2 export format while retaining support for version 1 files.
- Filled identifiers for existing history before export so upgraded databases retain collection membership.
- Added a choice to restore previously deleted records or leave them deleted during import.
- Matched text collection membership against current content, including text edited independently on different devices.
- Restored collection membership after importing Secrets from protected backups.
- Preserved distinct edited contents that share an original record identifier.

## Window And Dialog Fixes

- Matched the restore-deleted confirmation to the application's light and dark themes.
- Made the close button, Escape, and Alt+F4 cancel the entire import, including Secrets and collections. **Skip deleted records** continues importing the rest.
- Fixed foreground activation from the global hotkey and tray without leaving the main window always on top.
- Preserved focus after restoring hidden, minimized, and covered windows.

## Documentation And Verification

- Updated English and Russian in-app guidance and backup privacy documentation.
- Added regression coverage for edited text membership, protected backup restoration, repeated imports, and deletion choices.
- Verified import cancellation for both backup formats and real global-hotkey activation, including the native always-on-top flag.

## Store And Packaging

- Application version: `2.1.6`.
- Microsoft Store package version: `2.1.6.0`.
- The Store package remains the Free base application with unchanged Monthly and Lifetime purchase identifiers.

---

# ClipVault Studio 2.1.5

ClipVault Studio 2.1.5 adds stronger optional protection for Secrets, encrypted full backups, and a faster way to clean up history in batches.

## Secret Vault

- Added an optional master-password vault for protected Secrets while preserving current-user Windows protection before setup.
- Added transactional migration of existing Secrets to authenticated vault encryption.
- Added explicit lock, unlock, password change, and automatic 30-second key expiry.
- Added a Recovery Kit that can reset a forgotten master password while matching local vault state is available.
- Made replacement Recovery Kits invalidate every earlier kit.

## Protected Backup And Restore

- Kept readable `.clipboard.json` exports compatible and guaranteed to exclude Secrets.
- Added password-protected `.cvbackup` export with optional Secrets and authentication of the complete backup.
- Added protected import with wrong-password and tamper rejection before any records are merged.
- Kept backup passwords independent from the vault master password and Recovery Kit.

## Multi-Select History Cleanup

- Added a focused selection mode for history, Favorites, content types, and collections.
- Added **Select shown** for the currently visible filtered items while preserving control over individual selections.
- Added one clear confirmation before deleting all selected records together.
- Deleted mixed item types and their collection memberships in one database transaction.
- Kept Favorites, section counts, pagination, and collection counts synchronized after deletion.

## Interface Polish

- Matched password fields, backup choices, checkboxes, and dialog actions to the ClipVault visual system.
- Fixed Cancel in backup options so it closes the workflow without opening a save-file dialog.
- Added English and Russian labels, confirmations, status messages, and Guide coverage for the new workflows.

## Documentation And Verification

- Added English and Russian in-app guidance for vault setup, recovery, protected backup, and multi-select deletion.
- Updated privacy, support, architecture, disclaimer, and public documentation.
- Added regression coverage for migration, locking, password changes, recovery, Recovery Kit replacement, persistence, wrong passwords, and modified backup data.
- Verified Free and Pro builds and the complete automated test suite before packaging.

## Store And Packaging

- Application version: `2.1.5`.
- Microsoft Store package version: `2.1.5.0`.
- Microsoft Store Monthly and Lifetime purchase identifiers remain unchanged.

---

# ClipVault Studio 2.1.4

ClipVault Studio 2.1.4 keeps Favorites complete and predictable across large histories, collections, and smart text views.

## Favorites Reliability

- Fixed Favorites showing only the subset that matched the previously selected smart text category.
- Kept the Favorites count stable while switching between collections and All history.
- Kept every pinned item available during incremental history loading, including histories larger than the configured batch size.
- Preserved search filtering inside Favorites without letting collection navigation change the global Favorites badge.

## Reliability And Verification

- Added a regression scenario with 13 pinned text items and a five-record history batch.
- Covered direct navigation from a collection to Favorites, return to All history, and a retained Styles filter.
- Verified the complete automated test suite and the warning-free Release build.

## Store And Packaging

- Application version: `2.1.4`.
- Microsoft Store package version: `2.1.4.0`.
- Microsoft Store Monthly and Lifetime purchase identifiers remain unchanged.

---

# ClipVault Studio 2.1.3

ClipVault Studio 2.1.3 restores predictable text workbench layouts after resizing the split view.

## Text Workbench Reliability

- Restored a single full-width editor for plain text and other formats without a structured preview.
- Restored a single full-width Markdown preview when Preview mode is selected.
- Kept Editor, Preview, and Split modes independent after moving the pane divider.
- Preserved the user's Split pane ratio while switching between workbench modes.

## Store And Packaging

- Application version: `2.1.3`.
- Microsoft Store package version: `2.1.3.0`.
- Microsoft Store Monthly and Lifetime purchase identifiers remain unchanged.

---

# ClipVault Studio 2.1.2

ClipVault Studio 2.1.2 refines everyday navigation, document search, and background control with a new native-looking tray experience.

## Premium Tray Controls

- Replaced the system tray context menu with a themed ClipVault Studio menu that follows the active light or dark appearance.
- Added direct actions for opening the application, launching Quick Paste, opening Settings, and exiting cleanly.
- Added a runtime pause/resume control for clipboard monitoring with an immediate status update and reliable worker restart.
- Preserved the previously focused application when Quick Paste is opened from the tray.
- Added localized English and Russian labels, descriptions, status text, and the active Quick Paste hotkey.

## Workbench Search

- Added `Match case` and `Whole word` options to in-document search.
- Added Visual Studio-style `Ctrl+F` behavior that uses the current single-line editor selection as the search query.
- Kept the search field, caret, result count, and navigation controls stable while matches refresh.
- Refined the search surface, icon-only workbench controls, tooltips, and keyboard accessibility.

## Interface Polish

- Refined sidebar navigation, selection indicators, spacing, cards, inspector surfaces, and light/dark palette contrast.
- Restored clear visual separation between history items while keeping rounded clipping and compact density.
- Kept search and filter controls at stable dimensions so the workspace no longer shifts when state changes.

## Reliability And Verification

- Added focused tests for tray actions, localization refresh, monitoring state, and tray-window initialization.
- Expanded document-search coverage for casing, whole words, selected text, cancellation, and large result sets.

## Store And Packaging

- Application version: `2.1.2`.
- Microsoft Store package version: `2.1.2.0`.
- Microsoft Store Monthly and Lifetime purchase identifiers remain unchanged.

---

# ClipVault Studio 2.1.1

ClipVault Studio 2.1.1 improves the reliability and control of optional local
AI model installation and updates.

## Local AI Model Management

- Added a user-selectable model storage folder, including full support for
  Unicode and Cyrillic paths.
- Added a manual model update check with release details and explicit user
  confirmation before any replacement is downloaded.
- Kept model delivery independent from Microsoft Store application releases
  while enforcing signed-manifest and minimum-app-version compatibility.
- Hardened resumable downloads with signed exact-size limits, HTTPS validation,
  SHA-256 verification, and safe fallback when a server cannot resume a range.
- Preserved the currently verified model until a replacement finishes download
  and validation successfully.
- Added local diagnostics reporting for model discovery, download, path, and
  runtime failures without uploading logs automatically.

## Store And Packaging

- Application version: `2.1.1`.
- Microsoft Store package version: `2.1.1.0`.
- The optional Qwen model remains outside the Store package.
- Microsoft Store Monthly and Lifetime purchase identifiers remain unchanged.

---

# ClipVault Studio 2.1.0

ClipVault Studio 2.1 turns clipboard history into a faster keyboard-first workflow and adds a clearer product About experience.

## Quick Paste Command Palette

- Added a dedicated configurable Quick Paste hotkey that opens a compact palette over the current application.
- Added immediate recent-history results plus cancellable full-database search across text, links, files, and images.
- Added keyboard-first actions: `Enter` pastes into the previously active application, `Ctrl+Enter` copies only, and `Escape` clears or closes.
- Added safe focus restoration with target-process verification so a recycled window handle cannot receive the paste.
- Added copy-only fallback with visible status when Windows prevents focus restoration or synthetic input.
- Excluded protected secrets from every palette result.
- Added active-monitor placement with DPI-aware work-area clamping.
- Added an in-app English and Russian workflow guide.

## History And Reliability

- Added a cross-kind `ClipboardTimeline` index so recent mixed history has stable database ordering without merging unrelated table IDs.
- Added timeline maintenance triggers for text, links, files, and images.
- Kept clipboard monitoring active when a trial expires or the effective license changes.
- Preserved the palette query and target application when the hotkey is pressed while the palette is already open.
- Hardened foreground target validation immediately before paste.
- Refreshed workbench document search after text changes without affecting editor persistence.

## About And Documentation

- Rebuilt Settings > About with the installed version, active edition, Microsoft Store link, local-first privacy summary, user guide, support, and bundled third-party notices.
- Updated product documentation for Quick Paste, version 2.1, Store certification, privacy, architecture, and support.

## Store And Packaging

- Application version: `2.1.0`.
- Microsoft Store package version: `2.1.0.0`.
- Microsoft Store Monthly and Lifetime purchase identifiers remain unchanged.

---

# ClipVault Studio 2.0.4

ClipVault Studio 2.0.4 adds a complete Pro comparison workspace and refines local AI for focused desktop work.

## Pro Image Annotation

- Added a focused image annotation workspace available from image preview and Inspector.
- Added movable text, outline rectangles with optional fill opacity, arrows, and line/underline tools with curated colors, font size, and stroke thickness.
- Added selection, move, delete, clear, bounded undo/redo, zoom, and automatic fit-to-window behavior.
- Added non-destructive Save as copy: annotations render into a new PNG history item while the original image remains unchanged.
- Reused the existing image persistence pipeline without a database migration or additional runtime dependency.

## Pro Text Compare

- Added a premium side-by-side comparison workspace powered by DiffPlex.
- Added aligned added, removed, and modified lines with optional word-level highlights.
- Added options to ignore spaces, empty lines, or case and to toggle syntax highlighting.
- Added optional visual word wrapping for text and Markdown comparisons; it is disabled by default and does not recalculate the diff.
- Added synchronized source line numbers and scrolling, including correct physical line mapping when empty lines are ignored.
- Added an interactive change map for overview and click-or-drag navigation through long comparisons.
- Added instant Collapse unchanged presentation with surrounding context and no repeated DiffPlex run.
- Added movable splitters for both source and result panes, plus clearer light/dark colors for added, removed, modified, inline, placeholder, and collapsed rows.
- Added search across both panes, difference navigation, swap, paste, copy, and clear actions.
- Kept keyboard focus and the text caret in Search diff while live matches update, so queries can be entered continuously.
- Added a dedicated title-bar entry point plus direct file selection and drag-and-drop for both comparison sides.
- Added a searchable picker for loading either side directly from the complete local Text library without preloading the main history view.
- Reused the guarded 5 MiB text-file loader and preserved independent syntax highlighting for files of different formats.
- Kept large comparisons off the UI thread and discard stale results when source text changes.
- Reused the existing AvalonEdit syntax palette in both light and dark themes.
- Prevented delayed AvalonEdit events from creating a scroll feedback loop; result rows remain vertically synchronized while horizontal positions stay independent.
- Kept Compare intentionally non-destructive: it never auto-saves, writes to selected files, or modifies clipboard history.

## Focused AI Assist

- Added independently collapsible Actions/Library and Run settings panels to reduce visual noise in the full AI workspace.
- Kept advanced context and history controls collapsed until requested.
- Added `Ctrl+Alt+L` and `Ctrl+Alt+R` shortcuts for quickly reclaiming response space without clearing the current source, request, or response.

## Store And Packaging

- Application version: `2.0.4`.
- Microsoft Store package version: `2.0.4.0`.
- Microsoft Store Monthly and Lifetime purchase identifiers remain unchanged.

---

# ClipVault Studio 2.0.3

ClipVault Studio 2.0.3 is a packaging and documentation maintenance release that carries forward every reliability and interface fix from 2.0.2.

## Store And Legal Notices

- Included the complete `THIRD-PARTY-NOTICES.txt` inventory in build, publish, and Microsoft Store package output.
- Documented direct and resolved runtime dependencies, selected icon sources, SQLite, and the optional Qwen3 4B model.
- Kept the Qwen model outside the application package and available only through an explicit user download.
- Preserved the existing Free, seven-day trial, Monthly, and Lifetime entitlement behavior.

## Packaging

- Application version: `2.0.3`.
- Microsoft Store package version: `2.0.3.0`.
- Microsoft Store Monthly and Lifetime purchase identifiers remain unchanged.

---

# ClipVault Studio 2.0.2

ClipVault Studio 2.0.2 is a focused reliability and interface update for history navigation, collection layout, and local AI setup.

## History And Navigation

- Fixed `All history` so it opens a unified mixed-content view instead of only clearing the selected collection.
- Added incremental loading for the unified history view across text, files, links, images, and secrets.
- Improved transitions between All history, collections, Favorites, and individual content sections.
- Kept the All history total count stable while older records load during scrolling.
- Refined the sidebar hierarchy, spacing, alignment, and localized content-type heading.

## Interface Polish

- Improved collection pane and inspector alignment across content sections.
- Reduced image card height so more useful content remains visible at once.
- Correctly centered the Local AI setup and Pro requirement icons at different DPI scales.

## Reliability

- Hardened opening a newly captured URL as text while its link preview is still loading in the background.
- Reused the captured text format when opening the workbench to avoid redundant classification and UI work.
- Added a bounded local crash log at `%LOCALAPPDATA%\ClipboardManager\Logs\crash.log` for actionable diagnostics without transmitting data.
- Added centralized recoverable-error handling so non-fatal UI and background failures no longer terminate the application.
- Added visible error IDs in the application status and a dedicated Diagnostics settings page.
- Added actions to open the current log, open its folder, copy a support summary, dismiss the current error, or clear rotated logs.
- Added compact startup and shutdown lifecycle entries to distinguish crashes from normal Windows or user-requested exits.
- Kept fatal runtime failures fail-fast while ensuring they are recorded before termination whenever the runtime permits.

## Store And Packaging

- Application version: `2.0.2`.
- Microsoft Store package version: `2.0.2.0`.
- Microsoft Store Monthly and Lifetime purchase identifiers and entitlement restoration remain unchanged.

---

# ClipVault Studio 2.0.1

ClipVault Studio 2.0.1 improves the Free-to-Pro upgrade experience while preserving the Microsoft Store purchase and entitlement model introduced in 2.0.0.

## Upgrade Experience

- Redesigned the Pro plan dialog with clearer Monthly and Lifetime plan hierarchy.
- Highlighted Lifetime as the best long-term value with the current 16% first-year saving compared with Monthly pricing.
- Added concise explanations of Collections, advanced editors, local AI, updates, cancellation, and permanent access.
- Improved English and Russian upgrade copy.
- Fixed spacing between the seven-day trial and Buy Pro actions in Settings.
- Correctly centered the Pro crown icon and improved dialog spacing at desktop resolutions.
- Added a Debug-only Store preview mode for safely reviewing localized plan UI without package identity or a real purchase.

## Store And Packaging

- Application version: `2.0.1`.
- Microsoft Store package version: `2.0.1.0`.
- The production Store license flow, Lifetime purchase, Monthly subscription, and entitlement restoration remain unchanged.

---

# ClipVault Studio 2.0.0

ClipVault Studio 2.0.0 introduces private local AI assistance, a protected seven-day Pro trial, broader structured-text tooling, premium light/dark UI, and a substantial reliability pass over the existing clipboard and collections workspace.

## Local AI Assist

- Added optional CPU-local inference through LLamaSharp.
- Added a user-initiated download for the recommended Qwen3 4B `Q4_K_M` model.
- The approximately 2.5 GB model remains separate from the application and Microsoft Store package.
- Added interrupted-download resume/restart handling and SHA-256 verification.
- Added Generate, Improve writing, Summarize, Translate, Explain text, and Explain code actions.
- Added a full AI workspace with response language, style, detail, formatting, code-block, and risk controls.
- Added a movable editor popover that keeps the current document context and response.
- Added structured Markdown response rendering with headings, lists, inline code, and fenced code blocks.
- Added copy, restore, expand-source, save-prompt, and history workflows.

## AI Privacy And Library

- Added DPAPI-encrypted saved prompts.
- Added DPAPI-encrypted local AI response history.
- Added Off, 1-day, 7-day, and 30-day history retention; 7 days is the default.
- Added a maximum of 200 unpinned history records.
- Added pinned history entries that survive normal age/count cleanup.
- Added clear-unpinned and individual-delete actions.
- Source text remains excluded from AI history by default and can be enabled explicitly.
- AI prompts, source text, and responses remain on the device during inference.

## Free, Trial, And Pro

- Added a one-time protected seven-day Pro trial.
- Trial access starts only after explicit user confirmation.
- Added integrity checks for conflicting protected state and significant clock rollback.
- Added Pro activation through Microsoft Store Monthly subscription or one-time Lifetime purchase.
- Added entitlement restoration for both Pro plans.
- Added retry behavior for temporary Microsoft Store license failures.
- Free clipboard history and protected secrets remain available after trial expiration.
- Trial expiration does not delete regular history, collections, saved prompts, or AI history.

## Structured Text And File Tools

- Expanded format recognition for JSON, Markdown, SQL, C#, JavaScript, TypeScript, Python, XML, HTML, CSS, SCSS, YAML, plain text, and generic code.
- Added category and exact-format filtering backed by the complete local database.
- Improved JSON formatting, minification, validation, tree navigation, and split view.
- Added XML formatting, minification, and validation.
- Added CSS/SCSS formatting, minification, and structural validation.
- Improved SQL formatting and validation through Microsoft ScriptDom.
- Improved Markdown split view and caret-following preview synchronization.
- Improved syntax colors and editor readability across supported formats.
- Expanded read-only file preview for supported source, script, markup, configuration, and documentation files up to 5 MiB.

## Collections And Interaction

- Refined the mixed-content collection workspace.
- Improved collection spacing, headers, counts, and empty states.
- Added premium styled context menus for item and collection actions.
- Improved drag-and-drop feedback and collection targets.
- Added polished create, rename, recolor, and delete dialogs.
- Fixed a collection context-menu crash caused by applying a `MenuItem` style to a separator.

## Search, Clipboard, And Background Work

- Hardened clipboard snapshot processing and cancellation.
- Improved dispatcher safety for clipboard and image work.
- Kept complete-history search debounced, cancellable, and database-backed.
- Prevented expected search cancellation from breaking the debugger workflow.
- Improved history counters and collection counts after bulk changes.
- Improved link metadata refresh coordination so repeated requests are not lost.
- Improved lifecycle cancellation for links, previews, search, text indexing, AI, and shutdown.
- Reduced UI stalls during metadata, image, and format-index loading.

## Link And Network Safety

- Added public-address validation for linked pages and preview images.
- Blocked localhost, private, link-local, reserved, and unsafe redirect targets.
- Added bounded redirects, DNS validation, connection timeout, and request timeout.
- Preserved stale metadata and cached images while newer data refreshes in the background.
- Improved preview cache cleanup and cancellation.

## UI And Localization

- Refined AI workspace and popover layout, spacing, controls, dropdowns, toggles, labels, and action buttons.
- Added premium styled sorting and context-menu controls.
- Fixed AI history and saved-prompt counters so they update immediately.
- Fixed task labels in saved prompts and history.
- Added movable AI popover behavior.
- Improved card sizing, action-bar fit, collection headers, and settings layout.
- Kept English as the default UI language with complete Russian localization.

## Reliability

- Stabilized tray minimize, restore, close, and explicit Exit behavior.
- Kept application shutdown deterministic with bounded cancellation and final flush.
- Prevented deferred callbacks from recreating UI/tray state during shutdown.
- Improved SQLite initialization, indexes, pooling, busy handling, and WAL usage where applicable.
- Added defensive schema checks and cleanup for stale collection references.
- Improved local model recovery when a download completed before the verification marker was written.

## Backup And Packaging

- Regular `.clipboard.json` backup continues to include files, text, images, links, metadata, and favorite state.
- Secrets, collections, AI prompts/history, settings, model files, trial state, and Store entitlement remain excluded.
- Application version: `2.0.0`.
- Microsoft Store package version: `2.0.0.0`.
- The Store base app remains Free and unlocks Pro through Lifetime Store ID `9NCXBJJNB87G` or Monthly Store ID `9PK011BHGKN1`.
- The optional Qwen model is not bundled with GitHub or Microsoft Store artifacts.

## Notes

ClipVault Studio stores clipboard and AI library data locally. It does not provide cloud synchronization or publisher-operated telemetry. Local AI output can be inaccurate and should be reviewed before use.

Review [PRIVACY.md](PRIVACY.md), [DISCLAIMER.md](DISCLAIMER.md), and [docs/USER_GUIDE.md](docs/USER_GUIDE.md) before publishing or distributing this release.
