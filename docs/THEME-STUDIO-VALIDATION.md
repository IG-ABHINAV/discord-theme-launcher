# Theme Studio tested / not tested

Local beta.10, 2026-11-07, Windows x64, .NET 10 and Chromium/WebView2. All automated profile operations use temporary profiles and synthetic conversations; the user's Discord was not restarted or modified.

## New beta.10 evidence

Local library tests cover profile save/reopen/rename, exact font/image preservation, named three-color palettes and image storage across fresh library instances. Browser tests exercise navigation order, empty template, local profile discovery, red contrast errors and real custom-image cards. Native tests load selected-art and saved-image endpoints and save local profiles without a file dialog or client operation.

Target tests cover two servers, an explicit DM, a different DM, missing messages and decoys outside the chat list; DM-only profiles start in a synthetic private-conversation layout. The native host also confirms selected-DM styling and named palette/profile operations. Imported Zenit artwork overrides are checked for actual rendering and scope isolation. Extra imported artwork shares the existing 32-million-pixel image budget.

The user’s existing customized source document was copied to the local theme library with identical SHA-256 before development. Its assets remain local and are not part of the repository or screenshots.

The existing design’s colors were also preserved as a named palette, and its two normalized images were saved as independent reusable library items. The source .dtheme retains its original SHA-256.

## Verified

- **127 Core checks**: legacy snapshots, exact-byte recovery, CSS import/isolation, corruption rejection, named setup history, save/reopen before target selection, and both reset scopes. Full reset disables local/online themes and QuickCSS, preserves files/plugins, and restores all three styling settings on undo. Launcher-only reset preserves external styling.
- **64 Chromium tests**: consolidated editor navigation, template selection, import review/editing, confirmation cancellation/replay, localization and responsive layouts. Five focused localization/dialog tests were rerun after moving new copy into the build generator's source JSON.
- **61 native Windows checks**: real WebView2, icon availability, German preview/help, embedded unsaved-edit prompt, one-shot recovery confirmations, nested folder import with incompatibility reporting and preview-only selection. Native checks were repeated after the final host changes.
- CSS regeneration, checksum/scope/offline-resource audits and JavaScript syntax checks pass. Release packaging succeeds with no compiler warnings or errors and includes original license notices.
- The previous packaged beta.9 executable was launched through agent-desktop in an interactive session with --safe-preview. Its Home view and window icon were visually inspected, and Help was opened via accessibility and verified as an embedded overlay in the same window. This is separate from the automated WebView captures. A real Alata selection through accessibility downloaded and verified its font/license, returned the selected family, rendered the latest preview and re-enabled Apply; the safe-preview cache hashes match the catalog.
- README screenshots were updated with synthetic content. The original beta.8 working tree and the running beta.8 window were preserved.

The executable remains unsigned (NotSigned). Release SHA-256 values are generated under dist/release/SHA256SUMS.txt. Local delivery evidence records the final commit and artifact identities.

The 64 actual upstream static fonts and licenses were individually verified for hashes, size, font tables and embedding rights. Font tests cover deferred requests, verified cache reuse without network, corrupt cache replacement, invalid download rejection and portable embedding.

## Limits

Windows reboot/power cycle, real confirmed Discord application/reset (including live DM targets), live calls/streams, clean-machine installation, repair against a real broken installation, exhaustive DPI/high-contrast/screen-reader coverage, code signing and antivirus-vendor checks were not performed in this refinement.

BetterDiscord import remains a supported subset; remote or relative resource graphs, unreviewed global registrations and unsupported CSS produce a compatibility report. Only one personal slot is active at once. There is no automatic theme upload or pull-request creation; the user submits a GitHub issue and maintainers review it.

No public release or main merge is part of this draft. Earlier beta.8 evidence remains in the task's historical progress record. See [import limits](BETTERDISCORD-IMPORT.md) and [acceptance history](THEME-STUDIO-PROGRESS.md).
