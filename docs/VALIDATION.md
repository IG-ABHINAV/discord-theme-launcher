# Validation - 0.3.0-beta.1

Date: **2026-11-05**. This beta adds a personal configurator while retaining the three original presets. The user's running Discord was not restarted or modified during development. Native preview, isolated-profile transaction tests and actual-client coverage are distinguished below.

## Current automated evidence

- **57 Windows core checks** (58 on Linux, including symlink refusal): existing preset transactions and startup checks, server ID/channel-link parsing, invalid targets/schema/colors, portable assets and license round trips, deterministic compilation, corrupt assets, active SVG rejection, image dimensions, font table bounds/embedding flags, personal apply/retarget/disable/restore, conflicting-preset disablement, unrelated preferences/QuickCSS/plugins preserved, backup integrity and legacy three-file restore.
- **27 Playwright tests**: 19 preset checks and 8 personal-builder checks. All three presets retarget to a synthetic server ID; custom and native typography load; other servers/DMs, hidden/ambiguous rails, changed structure and global settings stay outside the scope. Message/author/composer fonts remain native. The generated CSS parses, uses embedded resources and contains one visual scope. Custom background scrim and composer/text contrast pass. User labels/license text cannot inject preview markup.
- **13 native Windows integration checks**: JPEG-to-PNG normalization, shared-image decoding, disguised SVG and invalid dimensions refused, preview above 2 MiB with actual WebView2, embedded font loading, generated CSS activation/isolation, external-navigation blocking, latest-config rendering after rapid refresh, and mutation controls disabled in preview mode.
- `dotnet build launcher/Windows/Launcher.csproj -c Release`: passes with **zero warnings/errors**. `scripts/package-windows.ps1` produces the self-contained x64 EXE/portable ZIP, preset CSS, catalog and checksums, including .NET/font/WebView2 SDK notices.

Environment: Windows 11 x64, .NET SDK 10.0.403 / runtime 10.0.14. Native rendering uses the locally installed Evergreen WebView2 Runtime. Tests use temporary assets/profiles and synthetic messages; no Discord token, messages, membership data or credentials are read.

The configurator was visually inspected in non-mutating preview mode. Updated screenshots show the actual Windows application and synthetic CSS fixtures. The preview has a separate temporary browser profile and blocks external document navigation/requests. Runtime cache files may remain in Temp; this is not a zero-files-written mode. Preview mode never applies to Discord/Vencord or writes the saved-configuration state.

## Current release limits

The native launcher screenshot records the beta editor during development, before the final acknowledgement-spacing and preview-zoom refinements. The final package was extracted and opened in preview mode; subsequent native automated checks passed. A final packaged-window screenshot can replace that capture in a later documentation update.

- A new personal theme has **not** been applied to the user's running Discord. The new workflow is verified in temporary profiles and WebView2/Chromium; inherited native Discord observations below apply to the earlier preset implementation.
- First-time Vencord installation/repair and full apply/restart/recovery have not been repeated on a clean VM for this beta. The upstream installer and client-startup logic are retained; temporary-profile checks do not establish every native setup condition.
- One personal configuration is active at a time. Custom profiles, PTB/Canary/Store, other operating systems, ARM64 and Discord Web are outside automatic support.
- The executable remains **unsigned**. No signing certificate is configured. SHA-256 verifies integrity, not publisher identity; antivirus/SmartScreen acceptance is not guaranteed.
- The preview requires WebView2 Runtime. Missing-runtime UI fallback is implemented; the native test host has the runtime installed. Runtime removal/installation is not part of the tests.
- General performance/game/call benchmarks, every Discord forum/thread/voice/search layout, high contrast, screen-reader behavior, all DPI combinations and unrelated theme combinations remain outside exhaustive coverage.
- Custom font validation checks format/table/embedding metadata and actual preview loading; it cannot determine ownership or guarantee every glyph/script works. Users must have redistribution rights. Imported image formats and sizes are intentionally bounded; bright backgrounds are strongly darkened for readable chat.

## Inherited validation - 0.2.4-beta.1

**19 theme tests** cover all three embedded display fonts, real font loading, heading/category/channel/member-group application, cross-server/Home font isolation and unchanged message, author and composer typography. The CSS audit checks one uniquely named font resource per output, one guarded visual scope, offline assets, file size and checksums. The 24 Windows launcher-core checks and self-contained Release package build also pass locally; Linux adds its symlink-refusal check in CI.

This release changes display typography and outlined artwork, with no startup or theme-transaction logic changes. Sanitized previews are generated from the distributed CSS; they contain no real messages or member data. The native launcher preview was visually checked. Managed CSS was backed up and atomically hot-reloaded while the same Discord process stayed open. Appearance of the new faces in the native Discord server views was not visually confirmed for this release; earlier native-client observations are inherited evidence below. Controlled performance/game/call benchmarks have not been repeated.

## Inherited validation - 0.2.3-beta.1

This copy-only update replaces the Officina and Workbench theme labels with Build Your Dream and Software House across graphic subtitles, theme metadata and launcher cards. All 17 existing theme tests and the Windows Release package build pass. Regenerated previews and the native launcher in non-mutating preview mode were visually checked. Updated CSS was hot-reloaded without restarting Discord; no setup or server-scope logic changes.

## Inherited validation - 0.2.2-beta.1

The artwork update removes identity numbering and adds FantaFort display typography. **17 theme tests** pass, including a new check that the embedded Bangers face actually loads, applies to FantaFort headings/navigation, preserves normal message typography, and releases element styling on Prime Build/Home navigation. Static audit permits only the uniquely named embedded font registration outside the existing server guards. The unchanged launcher core retains its 24 Windows checks (25 on Linux).

The sanitized theme previews were regenerated from the distributed stylesheets. FantaFort's SVG graphic lettering uses static outlines, independent of font loading in image contexts. Native Discord was also checked after replacing only the three managed CSS files: unnumbered identities and the FantaFort display face appeared in graphics, headings, channel names and member groups without restarting Discord. The font test includes nested channel-label typography spans observed in the current client. Installed CSS hashes match the generated files. This is an appearance update; startup protections introduced in 0.2.1 remain intact. Earlier live/setup evidence and its limitations are retained below.

## Inherited validation - 0.2.1-beta.1

The patch adds runtime completeness and startup-result checks. The regression first failed against the original renderer-only readiness predicate, then passed after the fix. **24 Windows core checks** pass (Linux additionally checks symlink refusal), and the Windows Release build passes with warnings treated as errors. The same 16 theme tests cover this release; server artwork and scope behavior are unchanged apart from version metadata.

The live installation was read-only checked after the user successfully reopened Discord: all required runtime files were readable/nonempty, patcher resolution and patcher/preload syntax checks passed, and a loaded Friends window was present. No repair or restart of that working client was needed. A native startup window does not prove stability beyond that observation.

The full automated first-install/repair path and every possible startup error remain outside exhaustive live coverage. See [startup incident and regression evidence](STARTUP-INCIDENT.md). The report below records the inherited 0.2.0 desktop/package checks separately; those are not claims that the 0.2.1 monitor was tested by restarting the user's working Discord.

## Inherited validation - 0.2.0-beta.1

Date: **2026-11-05**. This release adds three independently scoped themes and a Windows launcher. The original 0.1 measurements are historical; they are not measurements of the new backgrounds.

## Environment

- Windows 11 Pro 10.0.26200, x64.
- Discord stable 663810 / host 1.0.9267; official Vencord commit `383409e`.
- Electron 44.10.0 / Chromium 152.0.7977.169.
- Ryzen 7 5800X3D, 16 logical processors; Radeon RX 6950 XT.
- Local build: .NET SDK 10.0.403, Release, self-contained win-x64; bundled runtime 10.0.14.

## Actual desktop observations

| Check | Result |
| --- | --- |
| Portable ZIP extracted and native EXE launched | Passed; no separate runtime installation |
| Existing standard Discord/Vencord detected | Passed |
| Select all three and Apply & open Discord | Backup created, all three exact files enabled, Discord reopened |
| Installed files compared with generated release candidates | SHA-256 matched |
| QuickCSS | Remained empty and unchanged |
| Prime Build normal text view | Hardware artwork visible in chat/channel/member panels |
| Prime Build Dev | Distinct coding surfaces; native forum main-panel artwork also checked after a container-specific fix |
| FantaFort normal text view | Distinct blue/violet/cyan/yellow arena surfaces and fantasy roster artwork |
| Home/Friends | Stock appearance |
| Global account settings with FantaFort selected | Stock appearance, controls usable |
| Restore previous setup from launcher | Previous 0.1 Prime Build file and enablement recovered; new Dev/FantaFort files removed |
| Native launcher presentation | Discord-like grays and blurple, embedded Noto Sans fallback, clear disabled checkbox/button text; normal and maximized layouts inspected |

No messages were sent, calls/streams started, or server channels/permissions/settings changed. Only local presentation was modified. Private conversations were not opened for inspection. Published theme screenshots contain synthetic content; the launcher screenshot is the actual app in non-mutating preview mode.

The GUI setup/restore checks used release candidates with the same installer core. Final packaging and CI checks remain the authority for the exact released bytes; minor presentation/readiness refinements followed the first successful transaction.

## Automated checks

`npm run verify`: **16 Playwright tests** plus generated-CSS audit pass locally. Coverage includes target activation, non-target/DM isolation, missing/hidden/ambiguous selection, changed layout, global portals, role/status colors, removal, reload, offline assets, fixture widths 1024/1280/1920, separate backgrounds, forum-container decoration and text/input-boundary contrast for all three identities.

`dotnet run --project launcher/Tests/Tests.csproj -c Release -- .`: **19 checks on Windows**. Linux additionally checks symlink refusal. Temporary synthetic profiles cover all/partial/empty selections, exact custom-byte restoration, reversible restoration, unrelated files/preferences/plugins/QuickCSS retained, no unrelated settings in backups, corrupt/missing downloads, malformed settings, unknown IDs, catalog path traversal, backup traversal and installer digest rejection.

`dotnet build launcher/Windows/Launcher.csproj -c Release`: passes with warnings treated as errors. The Windows packaging script produces a self-contained single-file EXE, portable ZIP, three CSS files, catalog and asset checksums. The executable is **NotSigned**; no signing certificate was available. Runtime and font notices are included.

CI repeats theme/core checks on Linux and core/packaging on Windows, using read-only repository permissions. Windows release assets are sourced from the successful commit's CI artifact. Their hashes are published with the release.

## Performance limits

The launcher is a setup tool, not a resident Discord plugin. No service, polling, auto-start task or theme runtime JavaScript is added. Backgrounds are static SVG data, with no theme network dependencies or full-window blur.

The new backgrounds have **not** received controlled CPU/GPU, gaming FPS, frame-time or call-quality benchmarks. The [0.1 report](VALIDATION-0.1.md) and [old idle samples](idle-measurements.json) apply only to that earlier theme candidate. Do not use those small differences to promise zero overhead for this release.

## Remaining coverage

- Fresh-machine automated Vencord installation was not repeated in a clean VM. The same official CLI version was previously used successfully on this host; that does not establish every first-install condition.
- Windows 10, ARM64, Store/PTB/Canary/custom profiles, unusual paths and low-end machines need further coverage. Automatic setup targets stable/default Windows x64 only.
- All forum/thread/search/attachment/reply/error states, scaling/high contrast and third-party theme combinations are not exhaustively checked.
- Gaming, voice/video and screen sharing remain unmeasured.
- Portals outside the server view, unsupported detached windows and overlays keep their stock behavior.
- Unsigned executables can trigger Windows trust prompts. No warning bypass or security-setting change is automated.

See [Installation](INSTALLATION.md) for supported paths and recovery, and [Compatibility](COMPATIBILITY.md) for exact scope. Reversible CSS does not remove the terms/account implications of an unofficial client modification.
