# Performance audit — 0.3.0-beta.13

What this repo does: Theme Studio builds targeted Discord CSS and hosts a Windows editor/preview in WebView2. It supports built-in designs, reviewed CSS imports, local libraries and explicit server/DM targets. Expected load: one editor, frequent edits and large imported artwork alongside one local Discord client.

## Findings and fixes

1. **Repeated resource reads and large CSS copies during editing — fixed.** `launcher/Core/ThemeCompiler.cs` repeatedly parsed the catalog, loaded and hashed the same embedded presets, then carried embedded images through successive string rewrites. Verified presets now have a fixed three-entry lazy cache; the parsed catalog returns a defensive array copy. Working CSS omits image payloads during palette/font/target rewrites, and custom artwork is inserted last. Imported assets and imported CSS still pass their original validation on every compilation. Without the fix, large edits create avoidable temporary allocations and collection pressure on the UI thread.

2. **Large decoded artwork — fixed for new imports.** `launcher/Windows/AssetImport.cs` accepted up to 16 million pixels and preserved all of them when converting to PNG. A single 32-bit pixel buffer could therefore require about 61 MiB. New imports are downscaled to at most 1920 px per side and 2,073,600 pixels, preserving aspect ratio and alpha and never upscaling. This bounds one pixel buffer to about 7.9 MiB; browser/GPU copies and other overhead remain additional. Existing shared/library assets retain their bytes and need re-import to benefit. Large/high-DPI displays may show less artwork detail.

3. **Legacy preview response history — simplified.** `launcher/Windows/BuilderPreview.cs` retained response streams until a later completed navigation. It now relies on the native response retaining its managed stream during reading, allowing collection after consumption. These memory streams own byte arrays rather than file handles. The main Studio response-history fix and its native retention regression already shipped in beta.12 and are retained here; they are not new beta.13 savings.

4. **Invisible photo box — fixed, small unquantified improvement.** `src/server-theme.css` used opacity zero for the masked board-photo decoration below a 720 px chat width. It now uses `display: none` below that threshold and restores the box above it. Browser tests verify both cases. No CPU/GPU/VRAM saving is claimed from this rule alone; the engine may already optimize transparent drawing.

## Compiler measurements

The baseline is published beta.12 (`298b22e`). Both measurements use Release .NET builds on the same Windows host, three warm-up compilations followed by twenty measured calls. Timings use `Stopwatch`; allocations use `GC.GetAllocatedBytesForCurrentThread`. The custom case contains two synthetic 2 MiB PNG-signature payloads accepted by Core, measuring compilation/string processing rather than image decoding. The preset case explicitly selects Prime Build artwork/font/palette and a synthetic valid target.

| Workload | Beta.12 | Optimized | Allocation reduction |
| --- | ---: | ---: | ---: |
| Preset allocation per compile | 30.19 MiB | 4.17 MiB | 86% |
| Two custom payloads: allocation per compile | 149.25 MiB | 55.52 MiB | 63% |
| Preset time per compile | 6.53 ms | 0.99 ms | — |
| Two custom payloads: time per compile | 26.09 ms | 10.19 ms | — |

These are local samples. Timings vary with concurrent activity and hardware. Allocation volume is temporary managed allocation per compilation, not retained RAM or a Discord process working-set reduction. Before changing the decoration CSS and release version, output SHA-256 matched the baseline for 60 preset/palette/font combinations, one custom-payload configuration, a multi-server/DM configuration and neutral Fluent Dark.

## Validation

- 142 Windows Core checks, including defensive catalog copying, unknown preset rejection and continued integrity checks on mutable imported assets. Linux CI adds its platform-specific link check.
- 72 browser tests, including exact server/DM isolation, imported themes, fonts, libraries, editor flows and decoration geometry.
- 90 native Windows checks using real WebView2 and temporary profiles. Image cases cover square/portrait/landscape, alpha, aspect ratio, no upscaling and refusal of oversized input. Existing completed-response retention, large previews, rapid edits, recovery and saved-theme/target flows remain covered.
- No production dependency added. No GPU flag or automatic Discord hardware-acceleration change introduced.

## Live Discord measurement

Exploratory measurements restarted the installed Discord/Vencord client with managed themes enabled, disabled twice, then enabled again. Each run warmed up for 20 seconds and collected four samples five seconds apart. The game and editor were closed. Hardware acceleration was already disabled and was not changed. Process-tree working set/private bytes and normalized CPU time were sampled; GPU counters reported zero allocation/activity for the software-rendered client and do not constitute a GPU benchmark.

The disabled-theme sessions ranged from **920�1138 MiB working set** and **587�807 MiB private bytes**. Enabled-theme readings ranged from 1048�1776 MiB working set, but the window changed between a server channel and Friends during/between runs. Vencord runtime files also changed on the first restart. Consequently **this is not a valid same-workload A/B comparison** and its difference cannot be attributed to themes or presented as a beta.13 saving. The disabled-theme readings simply demonstrate substantial client memory use even without the managed themes in those observed sessions.

The original enabled-theme list and all Vencord settings were restored and compared semantically equal to the original. Theme files and QuickCSS bytes retained their original hashes. Updated upstream runtime files were left in place; no manual Vencord installation/repair, hardware-acceleration change or driver modification was performed. Raw diagnostics stay local and are not part of this repository.

A controlled live-client comparison still requires a fixed active target view, stable runtime version, matching call/media activity, adequate warm-up and repeated scroll/navigation workloads. The editor/compiler results above must not be interpreted as a universal Discord memory reduction.

The project cannot establish the cause of a particular multi-gigabyte Discord session from CSS inspection alone. The generated built-in themes add no runtime JavaScript or message cache. Reviewed third-party designs can contain supported effects/animations and need separate workload measurements. Hardware acceleration remains a separate Discord preference; changing it is a troubleshooting comparison, not a guaranteed memory fix. See [Discord's troubleshooting guide](https://support.discord.com/hc/en-us/articles/31623498041623-Discord-Troubleshooting-Guide).

Verdict: repeat compilation allocations are substantially lower, new artwork imports bound decoded pixels, and the existing targeted design/import/library behavior remains covered. The release does not claim to fix a game's GPU watchdog crash.

Lean: one legacy response-history collection removed; zero new production dependencies.

Not checked: clean-VM first installation, long-running memory growth, game frame times, prolonged calls/streams, every imported theme or hardware/driver combination. The executable remains unsigned.
