# Installation, updates and recovery

In Theme Studio beta.8, **Create** starts neutral with no target. **Community** offers four credited designs and downloaded `.theme.css` import with compatibility/resource review. **My themes** opens/saves/shares portable configurations and exports original CSS bytes. Choose a target before applying. Unsupported imports never mutate the profile; [the supported CSS model](BETTERDISCORD-IMPORT.md) explains limitations. German joins the four existing languages and persists for the Windows user. Submission opens the real repository issue template unless an existing Discord destination is configured via `THEME_STUDIO_SUBMISSION_DISCORD`.

## Supported automatic setup

The beta launcher targets Windows x64, standard per-user stable Discord in `%LOCALAPPDATA%\Discord`, and default Vencord data in `%APPDATA%\Vencord`. Windows 11 is the live-tested platform. PTB, Canary, Microsoft Store, custom profiles, linked profile paths, other client modifications and other operating systems require manual installation. The launcher refuses to run its setup elevated. No separate .NET installation is needed.

## Portable launcher (recommended)

1. Download/extract the [portable ZIP](https://github.com/PrimeBuild-pc/discord-theme-launcher/releases/tag/v0.3.0-beta.6), then run the EXE normally.
2. Select your target in Server and customize the design in Style/Artwork. All editing remains a preview.
3. Save/share a `.dtheme` configuration if desired.
4. Click **Apply to Discord**. Review the theme and target server in the overlay. It explains that Discord will restart and changes are local to this client.
5. Finish calls and save drafts, then choose **Continue & apply**. **Back to preview** or Esc cancels without applying. No separate restart checkbox is required in the editor.

Missing/incomplete Vencord is offered in the same confirmation. Its installation/repair checkbox starts unchecked; the upstream installer is downloaded and verified only after consent and Continue. Environment problems (unsupported client, elevation, unreadable profile, unknown loader) are shown in the overlay and prevent continuation. Conditions are checked again before execution. Apply closes only verified stable Discord processes, applies the managed transaction and checks startup.

The EXE includes verified preset fonts/artwork/CSS. Normal theme setup works offline with Vencord installed. The original-preset manager can still fetch absent adjacent files from the pinned release. A separate .NET install is unnecessary. WebView2 Runtime is required; if missing, install it from [Microsoft](https://developer.microsoft.com/microsoft-edge/webview2/) and reopen. Runtime installation is never automated.

Images: PNG/JPEG up to 8 MiB, 4096 px per side and 16 million pixels; normalized output must fit too. Fonts: static TTF/OTF up to 2 MiB, redistribution license TXT up to 64,000 bytes and explicit rights confirmation. Configurations are limited to 24 MiB. See [Personal configurations](CONFIGURATIONS.md).

The launcher never logs in, accesses tokens/messages, joins a server or changes server settings. The executable is unsigned; review source and published checksums before running. No security warning is bypassed.

## Preview and confirmation

Preview is the default editor state, and Apply opens the review overlay. The legacy `--preview` argument opens this same workflow; it no longer traps users in a non-applicable product mode. Development-only `--safe-preview` keeps all client operations as simulations and does not write the last-configuration state.

If Apply is disabled, review configuration errors, wait for the latest design/font or reload the preview. If Continue is disabled in the overlay, read its installation problem or explicitly select the offered Vencord setup. Cancel/Esc leaves the client unchanged. Confirmations refer to the exact reviewed configuration revision, are single-use, and expire when edits occur. Repeated/stale confirmation messages cannot repeat a transaction.

## Apply semantics

Personal Apply replaces the single `PersonalServer.theme.css` slot and disables a bundled preset targeting the same server, retaining its bytes. Other server presets and unrelated themes remain enabled. **Disable personal theme** removes only the personal slot; it does not automatically re-enable a previously conflicting preset. Use Restore to recover that state.

**Recovery > Open original presets** retains the old multi-selection flow: checked presets are installed/enabled, unchecked presets removed/disabled, and the personal slot removed. Other themes, plugins, QuickCSS, online theme links and preferences are preserved by both flows. Shared configurations affect only the installing user's client; each member must adopt a shared file independently.

The four managed files are backed up before changes, including local edits. New snapshots record exact bytes, file existence, enablement and SHA-256; full settings and credentials are not copied. All existing backup sources are checked before restoration. Failed transactions attempt restoration and keep the recovery reference. Older three-file snapshots remain usable and leave the personal slot intact.

Paths:

- Theme files: `%APPDATA%\Vencord\themes`.
- Backup history: `%LOCALAPPDATA%\PrimeBuild\DiscordThemeLauncher\backups`.
- Recovery reference: `%LOCALAPPDATA%\PrimeBuild\DiscordThemeLauncher\last-backup.txt`.
- Last saved/applied configuration: `%LOCALAPPDATA%\PrimeBuild\DiscordThemeLauncher\last-configuration.dtheme`. This editing document is independent of theme restoration. Editing alone does not write it; Save, Share or confirmed application can persist it. The development-only safe simulation never writes it.
- Verified upstream installer, if needed: `%LOCALAPPDATA%\PrimeBuild\DiscordThemeLauncher\downloads`.

Keep the backup folder if you want to undo earlier changes. Nothing in these local folders is uploaded by the launcher. Downloads are ordinary HTTPS requests to GitHub and its release delivery infrastructure; provider request logging is outside this project.

## Restore previous setup

Open Recovery and click **Restore previous setup**. Review the overlay, finish calls/save drafts and confirm **Continue & restore**. The previous managed file bytes and enablement are restored while current unrelated preferences remain intact. A snapshot of the state before restoration is also saved, so the restore operation can itself be undone with the same button.

This restores themes, not Vencord's installation or other applications. Historical snapshots remain available in the backup folder.

## Remove these themes

Use **Disable personal theme** for the personal slot. To remove every managed theme, open **Recovery > Open original presets**, deselect all and apply after restart acknowledgement. Or disable and move individual files manually. Managed names:

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
Get-FileHash -Algorithm SHA256 -LiteralPath .\DiscordThemeLauncher-0.3.0-beta.6-windows-x64-portable.zip
```

Compare it with the corresponding filename in the release checksum file. The repository-root checksum file covers generated themes; the release checksum file also covers the launcher, ZIP and catalog. A hash checks integrity, not publisher identity.

## Missing Vencord runtime or startup error

If a recognized Vencord loader is present but `patcher.js`, `preload.js`, `renderer.js` or `renderer.css` is missing, empty or unreadable, opening the launcher repairs that install. It downloads the pinned official CLI, closes the broken Discord window, runs the official repair, and opens Discord only after those files are readable. If the repair does not finish and Discord kept an official `_app.asar` backup, that backup is restored so Discord can start instead of showing the missing-module error. The preset manager still offers **Repair official Vencord (incomplete installation)** and waits for an explicit selection. Themes and unrelated settings remain separate.

A native Error window or an unconfirmed startup produces an error message rather than a success banner. No automatic second launch follows a failure. A generic loading/login window can require manual verification after the timeout; this is not proof that Discord crashed.

If Discord already reopened successfully, do not assume a repair is still necessary. Check the current installation and avoid repeatedly restarting a working client.

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
