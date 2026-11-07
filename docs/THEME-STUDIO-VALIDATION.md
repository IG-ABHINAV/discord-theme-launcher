# Theme Studio tested / not tested

Local beta.9, 2026-11-07, Windows x64, .NET 10 and Chromium/WebView2. All automated profile operations use temporary profiles and synthetic conversations; the user's Discord was not restarted or modified.

## Verified

- **109 Core checks**: legacy snapshots, exact-byte recovery, CSS import/isolation, corruption rejection, named setup history, save/reopen before target selection, and both reset scopes. Full reset disables local/online themes and QuickCSS, preserves files/plugins, and restores all three styling settings on undo. Launcher-only reset preserves external styling.
- **58 Chromium tests**: consolidated editor navigation, template selection, import review/editing, confirmation cancellation/replay, localization and responsive layouts. Five focused localization/dialog tests were rerun after moving new copy into the build generator's source JSON.
- **54 native Windows checks**: real WebView2, icon availability, German preview/help, embedded unsaved-edit prompt, one-shot recovery confirmations, nested folder import with incompatibility reporting and preview-only selection. Native checks were repeated after the final host changes.
- CSS regeneration, checksum/scope/offline-resource audits and JavaScript syntax checks pass. Release packaging succeeds with no compiler warnings or errors and includes original license notices.
- The packaged beta.9 executable was launched through agent-desktop in an interactive session with --safe-preview. Its Home view and window icon were visually inspected, and Help was opened via accessibility and verified as an embedded overlay in the same window. This is separate from the automated WebView captures.
- README screenshots were updated with synthetic content. The original beta.8 working tree and the running beta.8 window were preserved.

The executable remains unsigned (NotSigned). Release SHA-256 values are generated under dist/release/SHA256SUMS.txt. Local delivery evidence records the final commit and artifact identities.

## Limits

Windows reboot/power cycle, real confirmed Discord application/reset, live calls/streams, clean-machine installation, repair against a real broken installation, exhaustive DPI/high-contrast/screen-reader coverage, code signing and antivirus-vendor checks were not performed in this refinement.

BetterDiscord import remains a supported subset; remote or relative resource graphs, unreviewed global registrations and unsupported CSS produce a compatibility report. Only one personal slot is active at once. There is no automatic theme upload or pull-request creation; the user submits a GitHub issue and maintainers review it.

No public release or main merge is part of this draft. Earlier beta.8 evidence remains in the task's historical progress record. See [import limits](BETTERDISCORD-IMPORT.md) and [acceptance history](THEME-STUDIO-PROGRESS.md).
