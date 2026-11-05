# Architecture and release model

## Theme pipeline

`src/profiles.json` defines three identities and exact guild IDs. `src/server-theme.css` contains shared scoped component rules. `scripts/artwork.mjs` produces original header/chat/channel/member SVGs. `scripts/build.mjs` embeds artwork, substitutes each profile, and writes the three standalone theme files, inspectable vector assets, `catalog.json` and theme checksums.

Each output has exactly one selected-server `@scope`. FantaFort additionally registers a uniquely named, embedded data-only `@font-face`; this resource registration contains no selectors, and its font is applied only inside the FantaFort scope. The catalog records file sizes and digests. It is embedded in the launcher, so an existing binary cannot silently switch to a mutable main-branch catalog or a different release. The `--pb-` variable prefix is retained for compatibility with the original Officina source; each active scope gets its own values.

The theme audit and Playwright fixtures test the outputs, including all three simultaneous installations. Previews contain synthetic content; fixtures must never copy actual member messages, account data or credentials.

## Windows launcher

- `launcher/Core`: catalog parsing, bounded HTTPS downloads with SHA-256 verification, managed-file transactions and backup restoration. No UI types.
- `launcher/Windows`: WinForms presentation and Windows process/client integration. UI stays responsive during downloads and file operations. One instance per Windows user/session avoids competing launcher operations.
- `launcher/Tests`: dependency-free executable checks using isolated temporary profiles. It never discovers or edits the real user profile.

The normal flow is:

1. Validate the supported standard client/profile and normal privileges.
2. Read only known adjacent theme files, or download selected assets from a pinned release; verify all selected bytes.
3. If the user opted into first-time Vencord setup, download and verify the official CLI installer.
4. Stop only verified stable Discord processes under the current user's standard installation, after the explicit calls/drafts acknowledgement.
5. Run the official installer if needed; otherwise leave Vencord's installation alone.
6. Snapshot the three managed theme files and enablement. Apply selected files and merge only their `enabledThemes` entries into current settings.
7. Validate the runtime again, reopen Discord, and wait for a loaded Discord window before reporting success.

File replacement uses a temporary sibling file and an atomic rename. A durable backup reference is written before the first profile mutation. A failed apply attempts restoration. Restore reads current settings and merges the saved managed subset; unrelated preferences changed since the snapshot survive. Restore also saves a new snapshot so it can be undone.

Only the three fixed filenames are accepted. Profile and backup paths are checked for reparse points; backup references must be a generated 32-character identifier. No downloaded CSS filename becomes an arbitrary destination. Integrity checks are not a substitute for trusting the publisher.

Vencord CLI **1.4.2** is pinned to its official release asset and SHA-256 `15268aba25625797bf562187dd87ddadf42882e079c7b6192880ad3e83353ef5`. It runs as a separate upstream program with `--install --branch stable`. Its own downloads and installation behavior remain upstream responsibilities. No third-party installer binary is committed or bundled. [Official installer source](https://github.com/Vencord/Installer/blob/v1.4.2/cli.go) and [Windows instructions](https://vencord.dev/download/) describe that prerequisite.

The launcher has no account login, membership discovery, Discord token access, telemetry endpoint, auto-start, service, browser injection, or background polling. Ordinary GitHub requests still expose normal request metadata to the hosting provider.

## Launcher presentation

The launcher shell uses observed Discord dark grays, blurple actions, native-style spacing and light text. It does not present itself as official Discord software. Server-theme identities remain inside their own previews rather than controlling the shell colors.

Installed gg sans is preferred. Otherwise unmodified Noto Sans Regular/Bold are loaded from embedded resources into process-local GDI/GDI+ font collections; fonts are not installed system-wide. Noto Sans is an observed fallback in Discord's own font stack. Source and SIL OFL notices are in `assets/fonts`, included in the portable package and viewable from the launcher.

Small CheckBox/Button subclasses own painting to avoid Windows forcing black disabled text. Native keyboard, toggle and accessibility behavior remain inherited. Readiness confirmation resets after each operation. Modal notices use the same dark palette.

## Release

The canonical release version is `package.json`; checks require generated CSS/catalog and `launcher/Directory.Build.props` to agree. The Windows manifest has the corresponding numeric version. Build outputs are ignored by Git.

`scripts/package-windows.ps1` publishes a Release, self-contained `win-x64` single-file executable, then creates a portable ZIP with themes, instructions, MIT license and bundled .NET notices. The release checksum file covers every downloadable product asset; the root checksum file covers themes only. Standalone-EXE users should consult the runtime notices included in the portable ZIP.

CI uses pinned action commit SHAs, read-only repository permissions and no deployment secrets. Linux checks the theme outputs and installer core. Windows checks the core and packages the native application. Published Windows artifacts are taken from that commit's successful CI run.

The beta executable is unsigned. No signing certificate or signing service was configured for this project, and checksums do not change that status. Windows may show a trust warning. No warning bypass or security-setting change is automated.

Native clean-machine first-time installation, all Windows variants, call quality and game frame times remain separate validation work; do not infer them from fixture/core tests.

## Startup checks in 0.2.1

A loader marker plus renderer.js is insufficient: Electron's main process also needs patcher.js, and preload/renderer CSS are part of the supported runtime. The shared core now checks all four files for readable, nonempty contents. This is a completeness check, not a cryptographic or exhaustive functional validation of an existing third-party installation.

Recognized incomplete Vencord installations offer upstream `--repair --branch stable`; small unrecognized loaders remain refused. Startup observation is limited to process path, window handle and title. A loaded title ending in " - Discord" is accepted; a native "Error" title fails; generic/splash windows keep waiting for up to 40 seconds. No messages, tokens or renderer state are inspected. A successful window check cannot guarantee future stability.

After an operation fails, there is no automatic retry/relaunch. Theme-transaction restoration remains the responsibility of the existing snapshot mechanism. Original user incident evidence and regression limits are documented in STARTUP-INCIDENT.md.

## FantaFort typography in 0.2.2

Bangers Regular is embedded unmodified in the FantaFort CSS, together with its full SIL OFL notice. This is a shared font resource registration with the unique family name `PrimeBuild FantaFort Display`; all rules selecting elements remain guarded by the exact FantaFort server scope. The audit permits only this registration in that output, with an embedded source, and no additional global style rules.

The build uses committed glyph outlines for SVG graphic text, avoiding dependencies on font loading inside SVG image contexts. `scripts/extract-font-glyphs.py` can regenerate the map with fontTools 4.63.0; normal builds and member installation do not require Python. The map records the source font's SHA-256 digest, checked during builds.

Ordinary message/composer/username typography and the launcher UI remain unchanged. Disabling the theme or navigating away releases all display-font element styling. The font needs no OS installation and no network requests. Header/league serial numbers and decorative Dev line indices were removed.
