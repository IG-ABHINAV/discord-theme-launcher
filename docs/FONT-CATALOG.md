# On-demand font catalog

The 64 additional families in `src/font-catalog.json` are static TrueType fonts from the official [Google Fonts repository](https://github.com/google/fonts), pinned to commit `86aced91de05cd4f96a06838124cca45be337b2b`. Each entry records its family, category, immutable source path, byte size, SHA-256 and OFL.txt SHA-256. Every actual upstream font/license pair was checked against those digests and the application's static font/embedding validation before inclusion.

Only catalog metadata is bundled. Selecting a family downloads its TTF and OFL.txt from `raw.githubusercontent.com` over HTTPS; redirects are disabled. Font download is bounded at 2 MiB and license download at 64,000 bytes. Nothing is installed into Windows fonts. Merely opening or searching the catalog downloads nothing.

Verified files are cached at `%LOCALAPPDATA%\PrimeBuild\DiscordThemeLauncher\font-cache`. Safe-preview uses `%TEMP%\ThemeStudio-font-cache`. Cached content is checked again before reuse; corrupt content is fetched again and never replaces the current font unless the new pair validates. Offline selection works for an already cached family. A failed request leaves the previous font selected.

The existing custom asset format embeds the selected font bytes, digest and complete OFL license into the saved .dtheme. Reopening/exporting/applying a saved theme is independent of the cache and network. Source font names and copyright notices are retained; font bytes are not modified. Font style applies to display headings/channels, while message/composer typography stays native. Imported CSS designs retain their own typography and compatibility limits.

Users can still import other static TTF/OTF fonts with appropriate embedding/redistribution rights and a license. Variable fonts, WOFF/WOFF2 and restricted embedding flags are not accepted by that custom import boundary.

To update the curated catalog, deliberately review a pinned Google Fonts commit, static font files and each family's OFL.txt, recompute both digests and run Core/native/browser validation. Do not replace immutable paths with mutable main-branch URLs or add arbitrary user-supplied download URLs.

The Alata file in tests/fonts is a test-only fixture from the same pinned source with its original OFL.txt. It is not bundled in the application.
