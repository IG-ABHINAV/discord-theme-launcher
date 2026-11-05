# Installation, updates, and removal

## Requirements

- Windows Discord desktop and the official Vencord build.
- Membership in the public Prime Build server if you want to use its themed views.
- One local file: `PrimeBuild.theme.css`.

The theme does not require Nitro, server boosts, a bot, a custom Vencord build, or a companion plugin. Each member makes their own installation choice.

## Install Vencord if needed

Use only [vencord.dev/download](https://vencord.dev/download/) and its linked official releases. Choose the intended Discord installation and follow Vencord's instructions. Do not run the Windows installer as Administrator. Close any active call or stream before restarting Discord.

Vencord modifies your client installation separately from this theme. Its plugins, updates, and settings are independent of the theme. Read its [support and terms notice](https://vencord.dev/support/) before choosing to install it.

## Install Officina

1. Download `PrimeBuild.theme.css` from the [release assets](https://github.com/PrimeBuild-pc/primebuild-discord-theme/releases).
2. In Discord, open **User Settings → Vencord → Themes → Open Themes Folder**.
3. Copy the file into the folder shown. The normal Windows location is `%APPDATA%\Vencord\themes`; use the folder opened by Vencord if you have a custom location.
4. Enable **Prime Build — Officina** on the Themes page.
5. Open the public [Prime Build server](https://discord.gg/ERUwSxE79q).

If needed, refresh the Themes page or restart Discord. Check that the file was not saved as `PrimeBuild.theme.css.txt`.

The channel-list artwork is local decoration. It does not unlock or change Discord's native server banner. No server settings are changed.

## Verify the download

Each release includes `SHA256SUMS.txt`. On Windows, calculate the hash of the downloaded file:

```powershell
Get-FileHash -Algorithm SHA256 -LiteralPath .\PrimeBuild.theme.css
```

Compare the result with the release checksum. A checksum detects an incomplete or changed download; it is not a signature or a guarantee of trust in the publisher.

## Update deliberately

1. Disable the theme.
2. Keep a copy of your current file if you have customized it.
3. Download the new release and replace only `PrimeBuild.theme.css`.
4. Re-enable the theme and check Prime Build, another server, and DMs.

The recommended local file does not automatically follow changes on the repository's main branch. An online-theme setup is not part of the supported first-release installation.

## Disable

Turn off **Prime Build — Officina** on the Themes page. The previous appearance should return immediately. If another theme was already installed, it remains installed.

## Remove the theme

Disable it, then delete or move only `PrimeBuild.theme.css` out of the Themes folder. You do not need to delete Discord's settings, reinstall Discord, or log out of your account.

## Recover if settings cannot be opened

1. Fully quit Discord, including its tray process.
2. Open the Themes folder in File Explorer. The normal location is `%APPDATA%\Vencord\themes`.
3. Move `PrimeBuild.theme.css` into another folder outside Themes.
4. Reopen Discord. If the theme was the problem, its styling is no longer loaded.

Do not delete the entire Vencord or Discord data folder. If the problem also occurs without this file, consult [Vencord support](https://vencord.dev/support/).

## Remove Vencord itself

Run the official Vencord installer again, select the intended Discord installation, and choose **Uninstall**. This is separate from removing Officina. Follow Vencord's current instructions and restart when it is safe to do so.
