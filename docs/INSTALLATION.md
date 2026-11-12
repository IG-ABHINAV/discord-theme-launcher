# Installation, updates and recovery

Theme Studio beta.12 starts at Home with create, import and template actions. One Editor contains Server, Style, Artwork and Files. Import individual .dtheme/.theme.css files or a folder and review compatibility. Save without a server; select a target before applying. German and the other supported languages cover the synthetic preview as well as controls. Submission uses a user-operated GitHub issue form after an embedded explanation.

## Supported automatic setup

The beta launcher targets Windows x64, standard per-user stable Discord in `%LOCALAPPDATA%\Discord`, and default Vencord data in `%APPDATA%\Vencord`. Windows 11 is the live-tested platform. PTB, Canary, Microsoft Store, custom profiles, linked profile paths, other client modifications and other operating systems require manual installation. The launcher refuses to run its setup elevated. No separate .NET installation is needed.

## Portable launcher (recommended)

1. Download/extract the [portable ZIP](https://github.com/PrimeBuild-pc/discord-theme-launcher/releases/tag/v0.3.0-beta.12), then run the EXE normally.
2. Create a theme, or choose **Home → Use a saved theme → Open in editor**. Select targets in **Servers / DMs** and customize the design in Style/Artwork. To include another server, keep the current IDs and add its ID on a new line.
3. Choose **Save theme** to retain the design in My themes. **Duplicate theme…** in My themes or **Save a new copy…** in the editor keeps a separate variation. Export a `.dtheme` copy to share it.
4. Click **Review & apply**. Review the theme and every target in the overlay. It explains that Discord will restart and changes are local to this client. Application saves the theme in My themes and enables it for this complete target list; other enabled themes stay active.
5. Finish calls and save drafts, then choose **Continue & apply**. **Back to preview** or Esc cancels without applying. No separate restart checkbox is required in the editor.

Missing/incomplete Vencord is offered in the same confirmation. Its installation/repair checkbox starts unchecked; the upstream installer is downloaded and verified only after consent and Continue. Environment problems (unsupported client, elevation, unreadable profile, unknown loader) are shown in the overlay and prevent continuation. Conditions are checked again before execution. Apply closes only verified stable Discord processes, applies the managed transaction and checks startup.

The EXE includes verified preset fonts/artwork/CSS. Normal theme setup works offline with Vencord installed. A separate .NET install is unnecessary. WebView2 Runtime is required; if missing, install it from [Microsoft](https://developer.microsoft.com/microsoft-edge/webview2/) and reopen. Runtime installation is never automated.

Images: PNG/JPEG up to 8 MiB, 4096 px per side and 16 million pixels; normalized output must fit too. Fonts: static TTF/OTF up to 2 MiB, redistribution license TXT up to 64,000 bytes and explicit rights confirmation. Configurations are limited to 24 MiB. See [Personal configurations](CONFIGURATIONS.md).

The launcher never logs in, accesses tokens/messages, joins a server or changes server settings. The executable is unsigned; review source and published checksums before running. No security warning is bypassed.

## Preview and confirmation

Preview is the default editor state, and Apply opens the review overlay. The legacy `--preview` argument opens this same workflow; it no longer traps users in a non-applicable product mode. Development-only `--safe-preview` keeps all client operations as simulations and does not write the last-configuration state.

If Apply is disabled, review configuration errors, wait for the latest design/font or reload the preview. If Continue is disabled in the overlay, read its installation problem or explicitly select the offered Vencord setup. Cancel/Esc leaves the client unchanged. Confirmations refer to the exact reviewed configuration revision, are single-use, and expire when edits occur. Repeated/stale confirmation messages cannot repeat a transaction.

## Apply semantics

Personal Apply replaces the single `PersonalServer.theme.css` slot and disables other launcher presets, retaining their bytes. External themes remain enabled. Shared configurations affect only the installing user's client.

Recovery offers **Restore Discord**, with a choice between launcher themes only or all local/online themes and QuickCSS. Files and plugins are preserved. A safety snapshot is written before updating settings. Alternatively choose a previous setup from the history and confirm its restoration.

Snapshots record managed file bytes, existence, enablement and SHA-256. A full styling reset additionally records only enabledThemes, themeLinks and useQuickCss, because that action explicitly changes those fields. Plugins, credentials and unrelated preferences are not copied or reverted. All sources are checked before restoration. Old three-file backups remain usable and leave the personal slot intact.

Paths:

- Theme files: `%APPDATA%\Vencord\themes`.
- Backup history: `%LOCALAPPDATA%\PrimeBuild\DiscordThemeLauncher\backups`.
- Recovery reference: `%LOCALAPPDATA%\PrimeBuild\DiscordThemeLauncher\last-backup.txt`.
- Last saved/applied configuration: `%LOCALAPPDATA%\PrimeBuild\DiscordThemeLauncher\last-configuration.dtheme`. This editing document is independent of theme restoration. Editing alone does not write it; local Save or confirmed application can persist it. Exporting a copy does not mark the local profile saved. The development-only safe simulation never writes it.
- Session state: `%LOCALAPPDATA%\PrimeBuild\DiscordThemeLauncher\studio-session.json`. A clean exit restores the document, local identity, editor section and preview context. Unsaved edits, including pending palette names, offer Save and close, Discard or Keep editing.
- Reusable themes, palettes and images: `%LOCALAPPDATA%\PrimeBuild\DiscordThemeLauncher\library`.
- Verified upstream installer, if needed: `%LOCALAPPDATA%\PrimeBuild\DiscordThemeLauncher\downloads`.

Keep the backup folder if you want to undo earlier changes. Nothing in these local folders is uploaded by the launcher. Downloads are ordinary HTTPS requests to GitHub and its release delivery infrastructure; provider request logging is outside this project.

## Restore previous setup

Open Recovery, select a previously applied setup and choose **Restore selected setup**. Review the embedded confirmation and finish calls before continuing. A new safety snapshot makes the restoration reversible. This restores themes, not the Vencord installation.

## Disable themes

Choose **Recovery → Restore Discord**, select launcher themes only or all themes, and click Apply. It disables styling without deleting CSS, QuickCSS or runtime files. Managed names:

- `PrimeBuild.theme.css`
- `PrimeBuildDev.theme.css`
- `FantaFort.theme.css`
- `PersonalServer.theme.css`

## Manual theme installation

1. Install [official Vencord](https://vencord.dev/download/) if needed. Do not run its installer as Administrator.
2. Download only the desired `.theme.css` files from the release assets.
3. Open **User Settings > Vencord > Themes > Open Themes Folder** in Discord.
4. Copy the files into that folder and enable the corresponding cards.
5. Visit the matching server. Refresh the Themes page or restart Discord if files are not listed immediately.

No Nitro, bot, server boost or server administrator permission is needed. Each member chooses their own installation. A similarly named server with a different ID will not activate a theme.

## Verify a download

Download `SHA256SUMS.txt` from the same release. On Windows:

```powershell
Get-FileHash -Algorithm SHA256 -LiteralPath .\DiscordThemeLauncher-0.3.0-beta.12-windows-x64-portable.zip
```

Compare it with the corresponding filename in the release checksum file. The repository-root checksum file covers generated themes; the release checksum file also covers the launcher, ZIP and catalog. A hash checks integrity, not publisher identity.

## Missing Vencord runtime or startup error

If a recognized Vencord loader is present but `patcher.js`, `preload.js`, `renderer.js` or `renderer.css` is missing, empty or unreadable, the launcher reports the incomplete installation. Opening the editor never downloads an installer or closes Discord. Review & apply offers official installation/repair in its confirmation; only your explicit setup consent and Continue authorize downloading the pinned CLI, closing Discord and running the repair. Discord opens only after the runtime files are readable. If the repair does not finish and Discord kept an official `_app.asar` backup, that backup is restored so Discord can start instead of showing the missing-module error. Themes and unrelated settings remain separate.

A native Error window or an unconfirmed startup produces an error message rather than a success banner. No automatic second launch follows a failure. A generic loading/login window can require manual verification after the timeout; this is not proof that Discord crashed.

If Discord already reopened successfully, do not assume a repair is still necessary. Check the current installation and avoid repeatedly restarting a working client.

## Updates

From beta.16, Theme Studio checks GitHub Releases when it opens. When a newer version exists, **Update to …** appears in the header. It downloads that release's standalone EXE, verifies it against the release `SHA256SUMS.txt`, replaces the running app in place (the previous file is kept as `.old` until the new version starts and removes it), and restarts. Only the version number comes from the GitHub API; download URLs are rebuilt for the pinned repository. Nothing is downloaded or replaced without the click. If the app folder is not writable, download the release manually.

Versions up to beta.15 have no updater: download the new portable ZIP or EXE once and run it in place of the old one. Libraries, saved themes and recovery history stay under `%LOCALAPPDATA%\PrimeBuild\DiscordThemeLauncher`.

**Automatic cleanup of older installations.** On every start, the app checks the enabled launcher files in Vencord. Files from older versions — the single personal slot, preset files, the slow `:has()` scope or per-theme files compiled by an older version — are migrated: every saved theme is recompiled by the current version into its own `Studio-<id>.theme.css`, bundled presets become saved themes in My themes, and a legacy personal slot is matched to its saved theme by name (or to the last applied configuration). Every theme stays enabled; external themes, plugins and QuickCSS are untouched, and a recovery snapshot is taken first. If Discord is closed, this happens automatically; if it is open, **Optimize installed themes** appears in the header and restarts Discord when clicked (right after an in-app update it runs automatically). A separate installer script is not needed: the new executable performs the cleanup itself.

## Recovery without opening settings

1. Fully quit Discord, including the tray process.
2. Move only the affected managed `.theme.css` file(s) out of the Themes folder.
3. Reopen Discord. Without those files, their styles are not loaded.
4. If desired, copy original theme files from a known snapshot folder back into Themes and re-enable them manually.

Do not delete the entire Discord or Vencord data folder. If the problem remains with these CSS files removed, consult [Vencord support](https://vencord.dev/support/).

## Remove Vencord separately

Use the official Vencord installer and select Uninstall for the intended installation. The launcher deliberately does not remove the client modification automatically because it may be used by other themes/plugins.

Vencord is unofficial and carries Discord terms/account implications. Local reversibility does not guarantee account safety.
