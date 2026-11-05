# Prime Build Discord Theme — Officina

A **server-scoped Discord desktop theme for Vencord**, inspired by PC gaming hardware. Near-black surfaces, Prime Build orange, restrained PCB and heatsink details, and a clean message area.

**It activates only while you are viewing the public Prime Build server.** Other servers, direct messages, and global account settings keep their normal appearance. Every interested member installs the theme on their own computer; it does not change the server for everyone automatically.

[Download the theme](https://github.com/PrimeBuild-pc/primebuild-discord-theme/releases/download/v0.1.0-beta.1/PrimeBuild.theme.css) · [Installation and removal](docs/INSTALLATION.md) · [Compatibility](docs/COMPATIBILITY.md) · [Validation](docs/VALIDATION.md) · [Prime Build](https://primebuild.website/)

![Officina's actual stylesheet on a sanitized test fixture: dark Discord-style surfaces, orange channel selection, hardware header, mentions, message cards, and composer](assets/officina-preview.png)

*The preview uses the distributed stylesheet with synthetic content. It is a test fixture, not a screenshot containing community messages. Desktop checks and their limits are recorded separately in [Validation](docs/VALIDATION.md).*

## What you get

- One self-contained `PrimeBuild.theme.css` file, with artwork embedded.
- Automatic activation for the public Prime Build server, with no companion plugin or custom Vencord build required.
- Coordinated navigation, messages, mentions, links, composer, common cards, member list, and user panel.
- A local hardware-inspired header using Prime Build's actual logo.
- Static decoration, no full-window blur, no theme JavaScript, and no external CSS/font/image imports.
- Immediate removal through the theme toggle. Updates remain your choice when using the recommended local file.

This initial **beta** targets **Windows Discord desktop with the official Vencord build**. It relies on Discord's current interface structure; see the exact tested configuration and coverage in [Compatibility](docs/COMPATIBILITY.md).

## Install

1. Install [Vencord from its official website](https://vencord.dev/download/) if you do not already use it. Follow the Windows instructions; do not run its installer as Administrator.
2. Download `PrimeBuild.theme.css` from this repository's release assets.
3. In Discord, open **User Settings → Vencord → Themes → Open Themes Folder**.
4. Copy the file into that folder and enable **Prime Build — Officina** in the Themes page.
5. Open the [Prime Build community](https://discord.gg/ERUwSxE79q).

If the file is not listed immediately, refresh the Themes page or restart Discord. The filename must end in `.theme.css`, not `.css.txt`.

No Node.js, build tools, bot, Nitro subscription, server boost, or administrator access is needed to install the theme file. Vencord is a separate prerequisite.

## Disable or remove

Turn off **Prime Build — Officina** in the Themes page. Your previous appearance returns. Delete or move `PrimeBuild.theme.css` out of the Themes folder to remove it completely.

If a broken style prevents opening settings, fully close Discord, move **only this project's file** out of the Themes folder, and reopen Discord. Instructions for this and optional Vencord removal are in [Installation and removal](docs/INSTALLATION.md).

## Scope and coverage

The public target server is `1206415718728601600`. The theme requires its server-rail item to be selected within the recognized Discord layout. A link to Prime Build inside a message is not enough to activate it.

| Surface | Behavior |
| --- | --- |
| Main Prime Build navigation, channel view, messages and composer | Themed |
| Prime Build member list and user panel | Themed |
| Common in-view embeds, reactions, quotes and code blocks | Themed; syntax and role/status colors retained |
| Another server, DMs, Home/Friends | Original appearance |
| Global user settings, billing, login and security pages | Original appearance |
| Menus/profile cards/dialogs rendered outside the server view | Original appearance |
| Detached windows without the recognized server rail | Original appearance |
| Calls, screen sharing, threads and forum-specific layouts | Limited or not yet live-tested; see compatibility details |

When the required layout or selection signal is missing, the theme stays off. An incompatible Discord update can therefore make the theme stop appearing instead of making it global.

## Customize

The generated CSS has a short customization section near the top, **inside its guarded scope**:

```css
--pb-accent: #ff6600;
--pb-accent-text: #ff9a57;
--pb-chat: #0d0d0f;
--pb-sidebar: #111113;
--pb-panel: #19191d;
--pb-radius: 6px;
--pb-header-height: 112px;
```

Edit the local file and save it. Keep the `@scope` condition intact: moving declarations to `:root` or removing the server guard changes the isolation behavior. The original artwork and some orange tints remain part of the Prime Build identity; rebuilding artwork is a maintainer task.

## Performance and privacy

The theme contains CSS and embedded artwork. It adds no polling, telemetry, background service, or runtime script. This does **not** imply zero rendering cost or describe the behavior of Vencord or unrelated plugins.

Use [Validation](docs/VALIDATION.md) for measured results, the hardware and method used, and untested cases. If you notice stutter, disable the theme first and compare the same view with the same plugins enabled.

## Troubleshooting

- **Nothing changes:** check the theme toggle, confirm you are in the public Prime Build server, and check the tested Discord layout. Other servers, including similarly named servers, are intentionally excluded.
- **The header is not visible:** it is part of the channel-list scroller; scroll that list to the top. It is local decoration, not a native server-banner perk.
- **An area stays standard:** compare it with the coverage table. Portaled menus and account settings deliberately remain outside the theme.
- **A Discord update breaks the theme:** disable it and [open an issue](https://github.com/PrimeBuild-pc/primebuild-discord-theme/issues) with client/Vencord versions, the affected surface, and a screenshot with private information removed.
- **Another theme conflicts:** try Officina on its own. Arbitrary combinations of third-party themes are not guaranteed.

Never include account tokens, API keys, private messages, or Vencord cloud credentials in an issue.

## Development

The source is `src/PrimeBuild.css`; `npm run build` embeds the original artwork and produces the installable file and checksum. Members download the result; they do not need to build it.

```sh
npm ci
npx playwright install chromium
npm run verify
```

The automated checks audit the CSS boundary, offline assets and size budget, then exercise activation, negative cases, restoration, semantic colors, contrast, and desktop-sized layouts using sanitized fixtures. They are not a replacement for actual Discord desktop checks.

## Important notice

Vencord is an unofficial client modification and [states that using client modifications violates Discord's terms](https://vencord.dev/support/). Simple local rollback is not a guarantee against account restrictions. Prime Build is not affiliated with or endorsed by Discord or Vencord.

## Maintainer and license

Maintained by [PrimeBuild-pc](https://github.com/PrimeBuild-pc) · [primebuild.website](https://primebuild.website/) · [Community](https://discord.gg/ERUwSxE79q)

Original theme code and hardware artwork are provided under the [MIT license](LICENSE). The Prime Build name and logo identify this project and its maintainer; the code license does not imply Prime Build's endorsement of modified versions. Logo source: [Prime Build website](https://primebuild.website/). No Zenit Gold code or artwork is included.
