# Validation — 0.1.0-beta.1

Test date: **2026-11-05**. This is a beta with a deliberately limited desktop support claim.

## Tested environment

| Component | Version or configuration |
| --- | --- |
| OS | Windows 11 Pro, 10.0.26200 |
| Discord | Stable 663810 (164cf72), host 1.0.9267 x64 |
| Vencord | Official standalone build, commit `383409e` |
| Electron / Chromium | 44.10.0 / 152.0.7977.169 |
| CPU | AMD Ryzen 7 5800X3D, 16 logical processors |
| GPU | AMD Radeon RX 6950 XT |
| Target | Public Prime Build server, `1206415718728601600` |

The official Windows Vencord CLI installer was used without elevation. No custom plugin or source build is needed. The final installation uses the normal Themes folder and native Themes toggle; QuickCSS was restored to its original empty state.

## Actual desktop checks

These checks used the real Discord desktop client, separate from the automated fixture:

| Check | Result |
| --- | --- |
| Public Prime Build normal text-channel view | Hardware header, dark surfaces, composer and scoped styling visible |
| Another server while Prime Build remains in the server rail | Original appearance, no Officina header |
| Home/Friends | Original appearance |
| Return to Prime Build | Styling resumes |
| Global User Settings and Vencord Themes while enabled | Original appearance, settings usable |
| Disable using the native theme toggle | Original appearance returns in Prime Build |
| Re-enable using the same toggle | Officina resumes |
| Full quit, move only the theme file out, reopen, return to Prime Build | Original appearance; no residual header or scoped surfaces |
| Restore the file | Normal local installation restored |
| Existing inline role/presence colors and media | Retained in the observed view |

No server setting, bot, permission, message, or member profile was changed. The development server was not used. The Home/Friends test covers the live DM navigation area; individual private conversations were not opened for inspection. Individual DM isolation is also exercised in the synthetic fixture.

## Automated checks

`npm run verify` passes: build, static CSS audit, and **13 Playwright tests**. They use a sanitized fixture with interface structure observed from the current Discord client, and exercise the distributed CSS itself.

- Target selection, switching to another server and DM context, and return.
- Negative cases: unselected server icon, an unrelated message link, missing marker, missing server, changed layout, hidden rail and ambiguous selection.
- Global portal isolation and preservation of inline role/status/danger colors.
- Text contrast targets of 4.5:1 and composer boundary contrast of 3:1 for the tested pairs.
- Disable, stylesheet removal, reload and repeated navigation without accumulated styles.
- Chromium parsing, embedded artwork and no network-dependent theme assets.
- Usable fixture layout and composer at widths 1024, 1280 and 1920 pixels.
- A reproducible sanitized preview using the real release stylesheet.

The static audit requires all styles inside one guarded `@scope`, checks the target and layout anchors, prohibits external URL dependencies and ongoing animation/backdrop-filter effects, enforces size budgets, and checks the version and SHA-256 checksum. The installable file is **266,534 bytes**; [SHA256SUMS.txt](../SHA256SUMS.txt) is authoritative for its hash.

The README preview is synthetic. It does not show real community messages and must not be presented as a native Discord screenshot. Browser fixture tests do not establish support for Discord Web.

## Exploratory idle measurements

The same Vencord installation and public text-channel view were sampled with Officina off and on. Three alternating pairs were retained after warm-up, with five seconds of settling before each approximately twenty-second sample. CPU is total Discord process CPU normalized across 16 logical processors; memory is summed across Discord processes. Process identities stayed unchanged within every retained sample.

| Median across three samples | Theme off | Theme on |
| --- | ---: | ---: |
| CPU, all logical processors | 0.053% | 0.063% |
| Private memory | 803.28 MiB | 804.15 MiB |
| Working set | 1149.95 MiB | 1152.00 MiB |

No material idle penalty was apparent in these short observations. The differences are **not a causal overhead estimate**: caching, garbage collection, network activity and sample timing can exceed them. This was a development candidate with the same scoped architecture and artwork; small member-selector refinements followed. Raw retained results and method are in [idle-measurements.json](idle-measurements.json).

These measurements compare the theme on/off **with Vencord present**. They do not measure Vencord versus stock Discord, gaming performance, GPU load, frame times, or lower-end machines. Initial startup/warm-up samples were excluded because their client/content conditions differed. CSS `:has()` still has rendering cost; the guard is anchored to the recognized app/server-rail subtree, and no mutation observer or polling loop is added by this theme.

## Remaining limits

- Calls, video and screen sharing were not started as test traffic; game performance and call quality are unmeasured.
- Thread, forum, search, attachment, reply and rare error states have not all received live desktop coverage.
- Non-default UI scaling, accessibility settings, light-mode combinations and arbitrary third-party themes need wider testing.
- macOS, Linux, mobile, browser installations, game overlays and detached windows are outside first-release support.
- Portaled menus, profile cards and dialogs outside the selected-server base view keep their stock appearance.
- Discord changes can invalidate exact layout/class anchors. The theme should then stay off; revalidation is required before broadening those anchors.

See [Compatibility](COMPATIBILITY.md) for coverage and [Installation and removal](INSTALLATION.md) for rollback. Local reversibility is not a guarantee against restrictions associated with an unofficial client modification.
