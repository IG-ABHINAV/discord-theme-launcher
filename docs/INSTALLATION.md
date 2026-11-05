# Installation, updates and recovery

## Supported automatic setup

The beta launcher targets Windows x64, standard per-user stable Discord in `%LOCALAPPDATA%\Discord`, and default Vencord data in `%APPDATA%\Vencord`. Windows 11 is the live-tested platform. PTB, Canary, Microsoft Store, custom profiles, linked profile paths, other client modifications and other operating systems require manual installation. The launcher refuses to run its setup elevated. No separate .NET installation is needed.

## Portable launcher (recommended)

1. Download the portable ZIP from the [release](https://github.com/PrimeBuild-pc/discord-theme-launcher/releases/tag/v0.3.0-beta.1) and extract it.
2. Run `DiscordThemeLauncher.exe` normally.
3. Choose a starting preset and paste your server ID or a Discord text-channel link. Use **How to copy a server ID** for the Developer Mode / Copy Server ID steps. Invite links are refused.
4. Choose fonts, colors, banner and background. Inspect the live preview. **Save** and **Share a copy…** write a `.dtheme` file containing selected assets and the custom font license. **Open configuration** reopens it. If needed, explicitly choose **Install / repair official Vencord if needed**.
5. Finish calls/streams and save drafts, then tick the restart checkbox.
6. Click **Apply theme**. Discord closes during the transaction and is reopened afterwards. The launcher checks startup before confirming success. Personal Apply requires the latest valid preview and loaded display font.

The EXE embeds verified presets, fonts and artwork, so the configurator works offline with Vencord already present. The ZIP also contains standalone preset CSS. The separate **Original presets…** manager keeps multi-server selection; if an adjacent file is absent, it fetches the exact pinned release file. Corrupt/mismatched files are refused. Optional Vencord installation needs Internet access.

The embedded preview needs Microsoft Edge WebView2 Runtime. If absent, the launcher explains the prerequisite and still permits configuration saving/sharing. Install it from [Microsoft's official page](https://developer.microsoft.com/microsoft-edge/webview2/) and reopen the launcher; runtime installation is not automated. The application bundles .NET.

Images: PNG/JPEG up to 8 MiB each, at most 4096 px per side and 16 million pixels; normalized output must also fit. Fonts: static TTF/OTF up to 2 MiB, redistribution license TXT up to 64,000 bytes and explicit permission confirmation. Restricted embedding, variable fonts, collections, imported WOFF/WOFF2, SVG and GIF are refused. Configurations are limited to 24 MiB. See [Personal configurations](CONFIGURATIONS.md).

The launcher does not detect membership, join a server, log into Discord, access tokens/messages, change server settings, or send messages. It does not install itself, add a service or configure auto-start. You may delete its extracted folder after closing it; backups remain separate.

The executable is unsigned. Review its source and published checksums before deciding whether to run it. The application never bypasses a Windows warning or disables a security feature.

## Apply semantics

Personal Apply replaces the single `PersonalServer.theme.css` slot and disables a bundled preset targeting the same server, retaining its bytes. Other server presets and unrelated themes remain enabled. **Disable personal theme** removes only the personal slot; it does not automatically re-enable a previously conflicting preset. Use Restore to recover that state.

**Original presets…** retains the old multi-selection flow: checked presets are installed/enabled, unchecked presets removed/disabled, and the personal slot removed. Other themes, plugins, QuickCSS, online theme links and preferences are preserved by both flows. Shared configurations affect only the installing user's client; each member must adopt a shared file independently.

The four managed files are backed up before changes, including local edits. New snapshots record exact bytes, file existence, enablement and SHA-256; full settings and credentials are not copied. All existing backup sources are checked before restoration. Failed transactions attempt restoration and keep the recovery reference. Older three-file snapshots remain usable and leave the personal slot intact.

Paths:

- Theme files: `%APPDATA%\Vencord\themes`.
- Backup history: `%LOCALAPPDATA%\PrimeBuild\DiscordThemeLauncher\backups`.
- Recovery reference: `%LOCALAPPDATA%\PrimeBuild\DiscordThemeLauncher\last-backup.txt`.
- Last saved/applied configuration: `%LOCALAPPDATA%\PrimeBuild\DiscordThemeLauncher\last-configuration.dtheme`. This editing document is independent of theme restoration. Preview mode never writes it.
- Verified upstream installer, if needed: `%LOCALAPPDATA%\PrimeBuild\DiscordThemeLauncher\downloads`.

Keep the backup folder if you want to undo earlier changes. Nothing in these local folders is uploaded by the launcher. Downloads are ordinary HTTPS requests to GitHub and its release delivery infrastructure; provider request logging is outside this project.

## Restore previous setup

Open the launcher, finish calls/save drafts and tick the restart checkbox. Click **Restore previous setup**. The previous managed file bytes and enablement are restored while current unrelated preferences remain intact. A snapshot of the state before restoration is also saved, so the restore operation can itself be undone with the same button.

This restores themes, not Vencord's installation or other applications. Historical snapshots remain available in the backup folder.

## Remove these themes

Use **Disable personal theme** for the personal slot. To remove every managed theme, open **Original presets…**, deselect all and apply after restart acknowledgement. Or disable and move individual files manually. Managed names:

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
Get-FileHash -Algorithm SHA256 -LiteralPath .\DiscordThemeLauncher-0.3.0-beta.1-windows-x64-portable.zip
```

Compare it with the corresponding filename in the release checksum file. The repository-root checksum file covers generated themes; the release checksum file also covers the launcher, ZIP and catalog. A hash checks integrity, not publisher identity.

## Missing Vencord runtime or startup error

If a recognized Vencord loader is present but `patcher.js`, `preload.js`, `renderer.js` or `renderer.css` is missing, empty or unreadable, the launcher offers **Repair official Vencord (incomplete installation)**. Explicitly select it before applying. The official installer is invoked with its repair operation; themes and unrelated settings remain separate.

A native Error window or an unconfirmed startup produces an error message rather than a success banner. No automatic second launch follows a failure. A generic loading/login window can require manual verification after the timeout; this is not proof that Discord crashed.

If Discord already reopened successfully, do not assume a repair is still necessary. Check the current installation and avoid repeatedly restarting a working client. The [startup incident report](STARTUP-INCIDENT.md) distinguishes the observed failure from the confirmed launcher validation defect.

## Updates

Download a new complete portable package, or the new standalone EXE. Each launcher is pinned to its own catalog and release; an old launcher does not silently fetch a different version. Apply your selection to update managed theme files. Local custom edits are included in the preceding backup but are replaced by the new selected release.

## Recovery without opening settings

1. Fully quit Discord, including the tray process.
2. Move only the affected managed `.theme.css` file(s) out of the Themes folder.
3. Reopen Discord. Without those files, their styles are not loaded.
4. If desired, copy original theme files from a known snapshot folder back into Themes and re-enable them manually.

Do not delete the entire Discord or Vencord data folder. If the problem remains with these CSS files removed, consult [Vencord support](https://vencord.dev/support/).

## Remove Vencord separately

Use the official Vencord installer and select Uninstall for the intended installation. The launcher deliberately does not remove the client modification automatically because it may be used by other themes/plugins.

Vencord is unofficial and carries Discord terms/account implications. Local reversibility does not guarantee account safety.
