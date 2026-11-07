# Architecture and release model

## Theme pipeline

Beta.8 adds `src/community.json` for author/source/license/version, adaptation and capabilities; the three original CSS profiles remain in `src/profiles.json`. Catalog parsing accepts a bounded manifest of unique safe filenames instead of a three-entry allowlist. Historical restoration touches only its recorded managed subset, leaving newer files alone.

`CssSyntax` / `CssThemeImporter` parse the supported bounded CSS model in Core. Original source/digest/provenance and variable overrides are revalidated at persistence and apply boundaries. Conversion maps document roots to the exact server scope, confines ordinary selectors to descendants, enforces containment, namespaces animations and applies explicit offline resource policy. `CommunityGallery` supplies reviewed adapters, currently pinned Zenit and its offline font mapping. [Compatibility details](BETTERDISCORD-IMPORT.md) describe rejected constructs.

`src/profiles.json` defines three identities and exact guild IDs. `src/server-theme.css` contains shared scoped component rules. `scripts/artwork.mjs` produces original header/chat/channel/member SVGs. `scripts/build.mjs` embeds artwork, substitutes each profile, and writes the three standalone theme files, inspectable vector assets, `catalog.json` and theme checksums.

Each output has exactly one selected-server `@scope`. Each theme additionally registers a uniquely named, embedded data-only `@font-face`; this resource registration contains no selectors, and its font is applied only inside that theme's selected-server scope. The catalog records file sizes and digests. It is embedded in the launcher, so an existing binary cannot silently switch to a mutable main-branch catalog or a different release. The `--pb-` variable prefix is retained for compatibility with the original Officina source; each active scope gets its own values.

The theme audit and Playwright fixtures test the outputs, including all three simultaneous installations. Previews contain synthetic content; fixtures must never copy actual member messages, account data or credentials.

## Windows launcher

Version 0.3 adds `ThemeConfiguration`, `ThemeCompiler` and `ThemePreview` in Core and a WinForms host with native asset validation and a bundled WebView2 editor/preview. [Personal configurations](CONFIGURATIONS.md) describes the format, compiler, import/licensing limits, preview boundary and upgrade semantics.

- `launcher/Core`: catalog parsing, bounded HTTPS downloads with SHA-256 verification, managed-file transactions and backup restoration. No UI types.
- `launcher/Windows`: WinForms presentation and Windows process/client integration. UI stays responsive during downloads and file operations. One instance per Windows user/session avoids competing launcher operations.
- `launcher/Tests`: dependency-free executable checks using isolated temporary profiles. It never discovers or edits the real user profile.
- `launcher/WindowsTests`: temporary image decoding, actual WebView2 rendering and native configurator checks. No real Discord writes or restarts.

The normal flow is:

1. Validate the supported standard client/profile and normal privileges.
2. Compile validated personal data from embedded verified presets, or verify original preset bytes from adjacent files / the pinned release.
3. If the user opted into first-time Vencord setup, download and verify the official CLI installer.
4. Stop only verified stable Discord processes under the current user's standard installation, after the explicit calls/drafts acknowledgement.
5. Run the official installer if needed; otherwise leave Vencord's installation alone.
6. Snapshot the four managed theme files and enablement. Apply selected files and merge only their `enabledThemes` entries into current settings.
7. Validate the runtime again, reopen Discord, and wait for a loaded Discord window before reporting success.

File replacement uses a temporary sibling file and an atomic rename. A durable backup reference is written before the first profile mutation. A failed apply attempts restoration. Restore reads current settings and merges the saved managed subset; unrelated preferences changed since the snapshot survive. Restore also saves a new snapshot so it can be undone.

Only the three fixed preset filenames and `PersonalServer.theme.css` are managed. New snapshots verify SHA-256 of existing sources before restoration. Old three-file snapshots restore their subset and retain the personal slot. Profile and backup paths are checked for reparse points; backup references must be generated 32-character identifiers. No downloaded filename becomes an arbitrary destination. Hashes do not establish publisher identity.

Vencord CLI **1.4.2** is pinned to its official release asset and SHA-256 `15268aba25625797bf562187dd87ddadf42882e079c7b6192880ad3e83353ef5`. It runs as a separate upstream program with `--install --branch stable`. Its own downloads and installation behavior remain upstream responsibilities. No third-party installer binary is committed or bundled. [Official installer source](https://github.com/Vencord/Installer/blob/v1.4.2/cli.go) and [Windows instructions](https://vencord.dev/download/) describe that prerequisite.

The launcher has no account login, membership discovery, Discord token access, telemetry endpoint, auto-start, service, browser injection, or background polling. Ordinary GitHub requests still expose normal request metadata to the hosting provider.

## Preview-to-apply confirmation in beta 4

The main editor always previews changes. Apply availability depends on valid/latest preview and operation state; runtime/setup problems are shown in the overlay instead of an unexplained non-applicable mode. Requesting application is read-only. `OperationConfirmation` captures operation, revision, immutable configuration, setup requirement and problem state under a single-use ID. The host accepts only the exact current confirmation, clears it before execution, and revalidates the runtime. A late setup requirement cannot reuse consent that was not requested. Vencord installation remains an explicit unchecked opt-in in the overlay. Cancel/Esc and stale messages perform no transaction.

The old `--preview` flag now opens the normal confirmation workflow. `--safe-preview` and `BuilderForm(true)` are development-only simulations: confirmation exercises the real UI/host gate but never downloads, saves the last document, changes Discord/Vencord or stops processes. Tests use that path; no live-client application is claimed.

## Launcher presentation

Version 0.3 beta 2 replaces the long native form with Theme Studio: a bundled offline editor in `launcher/Windows/ui`, served by `StudioSurface`. It has Server, Style, Artwork and Recovery sections, explicit preview-context status, real artwork thumbnails, a visible composer and a fixed apply/restart footer. The host preserves file operations and setup transactions; source-checked, bounded messages carry edit values and named actions. Native file dialogs never accept paths from the page. The editor uses embedded Noto Sans with installed gg sans preferred, classic Discord dark surfaces and the official blurple accent. It identifies itself as an independent Prime Build tool. Interface copy defaults to English, with Italian, Spanish and Russian selected from the header and stored in `language.txt` under the launcher state folder. Documentation remains English. Cyrillic uses Noto Sans, or Segoe UI when an installed gg sans face lacks those glyphs.

The launcher shell uses observed Discord dark grays, blurple actions, native-style spacing and light text. It does not present itself as official Discord software. Server-theme identities remain inside their own previews rather than controlling the shell colors.

Installed gg sans is preferred. Otherwise unmodified Noto Sans Regular/Bold are loaded from embedded resources into process-local GDI/GDI+ font collections; fonts are not installed system-wide. Noto Sans is an observed fallback in Discord's own font stack. Source and SIL OFL notices are in `assets/fonts`, included in the portable package and viewable from the launcher.

Small CheckBox/Button subclasses own painting to avoid Windows forcing black disabled text. Native keyboard, toggle and accessibility behavior remain inherited. Readiness confirmation resets after each operation. Modal notices use the same dark palette.

## Release

The canonical release version is `package.json`; checks require generated CSS/catalog and `launcher/Directory.Build.props` to agree. The Windows manifest has the corresponding numeric version. Build outputs are ignored by Git.

`scripts/package-windows.ps1` publishes a Release, self-contained `win-x64` single-file executable, then creates a portable ZIP with themes, instructions, MIT license and bundled .NET notices. The release checksum file covers every downloadable product asset; the root checksum file covers themes only. Standalone-EXE users should consult the runtime notices included in the portable ZIP.

CI uses pinned action commit SHAs, read-only repository permissions and no deployment secrets. Linux checks preset/personal browser fixtures and Core. Windows checks Core, native decoding, WebView2 and packaging. Published Windows artifacts come from that commit's successful CI run. The ZIP includes WebView2 SDK license/notice files alongside runtime/font notices; WebView2 Runtime is not bundled.

The beta executable is unsigned. No signing certificate or signing service was configured for this project, and checksums do not change that status. Windows may show a trust warning. No warning bypass or security-setting change is automated.

Native clean-machine first-time installation, all Windows variants, call quality and game frame times remain separate validation work; do not infer them from fixture/core tests.

## Startup checks in 0.2.1

A loader marker plus renderer.js is insufficient: Electron's main process also needs patcher.js, and preload/renderer CSS are part of the supported runtime. The shared core now checks all four files for readable, nonempty contents. This is a completeness check, not a cryptographic or exhaustive functional validation of an existing third-party installation.

Recognized incomplete Vencord installations offer upstream `--repair --branch stable`; small unrecognized loaders remain refused. Startup observation is limited to process path, window handle and title. A loaded title ending in " - Discord" is accepted; a native "Error" title fails; generic/splash windows keep waiting for up to 40 seconds. No messages, tokens or renderer state are inspected. A successful window check cannot guarantee future stability.

After an operation fails, there is no automatic retry/relaunch. Theme-transaction restoration remains the responsibility of the existing snapshot mechanism.

## FantaFort typography in 0.2.2

Bangers Regular is embedded unmodified in the FantaFort CSS, together with its full SIL OFL notice. This is a shared font resource registration with the unique family name `PrimeBuild FantaFort Display`; all rules selecting elements remain guarded by the exact FantaFort server scope. The audit permits only this registration in that output, with an embedded source, and no additional global style rules.

The build uses committed glyph outlines for SVG graphic text, avoiding dependencies on font loading inside SVG image contexts. `scripts/extract-font-glyphs.py` can regenerate the map with fontTools 4.63.0; normal builds and member installation do not require Python. The map records the source font's SHA-256 digest, checked during builds.

Ordinary message/composer/username typography and the launcher UI remain unchanged. Disabling the theme or navigating away releases all display-font element styling. The font needs no OS installation and no network requests. Header/league serial numbers and decorative Dev line indices were removed.

## Per-server typography in 0.2.4

The profiles now describe each embedded display face, including its unique CSS alias, file format and mapped weight. Prime Build uses upstream Rajdhani Medium Latin WOFF2, Dev uses upstream JetBrains Mono Medium WOFF2, and FantaFort retains Bangers Regular TTF. Unmodified font files, OFL licenses and exact upstream URLs are committed under assets/fonts. WOFF2 limits the download size without requiring new theme network dependencies.

All three graphics use committed glyph outlines checked against their font bytes. Regenerate a new map with `python scripts/extract-font-glyphs.py assets/fonts/<directory>/<font-file>` and fontTools 4.63.0. SVG labels escape accessibility text independently of their path geometry. Regular build and member setup do not need Python.

The common display rules use different sizes for the compact terminal face and the hardware/arena faces. All font selections stay in the existing server scopes. Messages, authors, composer and the launcher retain their native typography.
