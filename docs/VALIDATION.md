# Validation - 0.2.3-beta.1

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
