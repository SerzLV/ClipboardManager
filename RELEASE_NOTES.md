# ClipVault Studio Release Notes

## Microsoft Store 2.0.4 - July 22, 2026

The `2.0.4` Store update completes the first premium Text Compare workspace, makes AI Assist easier to focus, and improves comparison stability without changing the useful Free clipboard foundation.

### Pro Text Compare

- Compare clipboard-history text, pasted revisions, supported local text files, or current workbench content.
- DiffPlex-powered aligned line and word differences with original source line numbers.
- Ignore spaces, empty lines, or letter case; enable word-level differences and syntax colors independently.
- Search and previous/next difference navigation.
- Synchronized vertical scrolling with independent horizontal scrolling for long or differently indented lines.
- Movable source/result splitters, a change minimap, and collapsible unchanged regions with preserved context.
- Background comparison cancels superseded work to keep the interface responsive.
- Delayed programmatic scroll events are suppressed to prevent feedback loops and visible pane jumping.
- Text Compare is non-destructive: it does not autosave, overwrite files, or modify clipboard history.

### AI Assist

- The full workspace can hide the Actions/Library and Run Settings panels independently.
- Added `Ctrl+Alt+L` and `Ctrl+Alt+R` shortcuts for focused layouts.
- Advanced run settings stay collapsed until requested.
- Local Qwen inference, encrypted saved prompts, and optional encrypted response history remain unchanged.

### Documentation And Licensing

- Updated the English and Russian Store documentation for version `2.0.4`.
- Added DiffPlex `1.9.0` under Apache-2.0 to the open-source inventory and bundled notices.
- Clarified that Text Compare is local, in-memory, and has no autosave behavior.
- The optional Qwen model remains a separate user-initiated download and is not included in the Store package.

Microsoft Store is the only supported distribution and update channel. Earlier
GitHub installer and portable-package releases have been retired.
