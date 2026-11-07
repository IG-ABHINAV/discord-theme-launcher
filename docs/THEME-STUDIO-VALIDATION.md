# Theme Studio tested / not tested

Draft beta.8, Windows x64, .NET 10, Node 24 and pinned Chromium/WebView2. Tests use synthetic conversations and isolated temporary profiles; no real Discord profile/account is modified. Final command outcomes and artifact hashes are recorded in the job evidence report and progress record.

Verified commands: `npm run verify` (Core and browser checks), `dotnet run --project launcher/WindowsTests/WindowsTests.csproj -c Release` (native/WebView integration), and `./scripts/package-windows.ps1` (self-contained Release EXE and portable ZIP). The native suite additionally catches long German consent wrapping. Final review added detached-settings hit-testing so an unstyled portal must also remain visible above the imported pane, and a combined embedded-image pixel budget before preview allocation.

Final reviewed outcomes: **98 Core checks, 55 browser tests and 45 native Windows/WebView checks passed**. Portable ZIP extraction, extracted EXE identity, all release hashes, required notices and beta.8 product version were verified. The packaged `--safe-preview` process remained alive for ten seconds; no accessible window title was available and it was stopped after observation. This is a process smoke check, not proof of a loaded interactive UI or normal close. The EXE remains `NotSigned`.

## Tested

- CSS build/audit, November class aliases, current/previous layouts, baseline startup completeness checks and missing-patcher recovery.
- Bounded catalog expansion; legacy schema-one configurations and historical snapshots; exact-byte managed restoration preserving newer entries, runtime files, unrelated themes/plugins/QuickCSS/online links/preferences.
- Original Zenit and materially different upstream Dracula CSS: actual imported appearance, offline resources, root conversion, variable edits, original-byte portable save/reopen, repeated apply and recovery in fresh child processes.
- Other-server/DM/global-settings isolation; missing/hidden/ambiguous server selection. Imported fixed layers are contained in the selected pane.
- Malformed CSS and selector breakout, unsafe containment resets, external/relative/active resources, unsupported global registrations/nesting, corruption, source/asset size limits, embedded image bounds, unsafe names and invalid edits.
- Image decode failures block Apply; no arbitrary preview resource requests. Zenit retains its banner/mascot and uses a pinned offline Montserrat font with unique alias and preserved license.
- Create/Community/My themes, four credited gallery entries, import review/cancel/accept, author-variable controls, sharing entry points, confirmation/cancel/stale-message/replay protection, responsive layouts and five-language copy.
- Native Windows image decoding and real WebView2 renderer/bridge integration, including German language persistence/recovery/error text and actual native Zenit selection/edit/cancel.
- German long labels at 1040 × 720 and 1365 × 900, keyboard focus in confirmations and preview composer visibility. These are automated renderer checks.

## Explicitly not tested / unsupported

- Accessible interactive Windows desktop: no logged-in user session was available. WebView captures are renderer captures, not human desktop interaction evidence.
- Windows reboot, production Discord startup/application, live account/server navigation, calls/streams, real installer repair, clean-machine first install and exhaustive Windows/DPI/high-contrast/screen-reader coverage.
- Universal BetterDiscord CSS support, arbitrary remote import/resource graphs, generic font registrations, CSS nesting, global registrations, root layouts that cannot preserve pane bounds and detached portal theming.
- Automatic multi-profile, PTB/Canary/Store, other platforms, concurrent personal slots or a public upload backend. Multiple configurations may be saved, but one personal slot is active.
- Code signing / antivirus-vendor validation. Portable EXE remains unsigned; checksums establish integrity, not publisher trust.

No public release or main merge is part of this draft. See [precise import/provenance limits](BETTERDISCORD-IMPORT.md), [acceptance checklist](THEME-STUDIO-PROGRESS.md), and [credited preview](ZENIT-PREVIEW.md).
