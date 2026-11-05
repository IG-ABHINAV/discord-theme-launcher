# Compatibility and coverage

## Independent selected-server scopes

| Theme | Exact server ID |
| --- | --- |
| Prime Build / Officina | `1206415718728601600` |
| Prime Build Dev / Workbench | `1537861795090268302` |
| FantaFort / Arena | `1536382143796748389` |

Each generated file wraps all visual rules and custom properties in one native CSS `@scope`. FantaFort also registers one uniquely named embedded font resource outside the scope; it has no selectors and does not apply a font globally. The guard requires the matching selected server-rail item within the recognized `.base__5e434` layout. It rejects hidden/ARIA-hidden rails, missing selection and ambiguous selection. Merely having a server icon or message link is insufficient.

The three CSS files can be enabled together. Only one matching server scope activates. Other servers, Home/DM contexts and global settings remain outside those scopes. Exact layout anchoring is intentional: incompatible Discord updates should make a theme stay off rather than turn global. It still needs maintenance as Discord changes.

## Visual coverage

| Surface | Behavior |
| --- | --- |
| Normal server text-channel chat | Theme-specific embedded background, coordinated dark surfaces |
| Channel panel | Dedicated background, branded header, category separators and channel states |
| Members panel | Dedicated background, retained role/presence colors |
| Composer, headers, common cards, reactions, links, mentions and code blocks | Coordinated within the selected server scope |
| Server icon rail | No dedicated redesign; inherited scoped tokens may affect some native accents |
| Home/Friends, other servers, global account settings | Stock appearance |
| Portaled menus/profile cards/dialogs outside the base view | Stock appearance |
| Detached windows without the recognized server rail | Stock appearance |
| Forum, thread, search, attachment, reply and rare error layouts | Partial inherited styling; not exhaustive live coverage |
| Voice/video/screen sharing | Not live-tested by starting calls/streams |
| Overlay, mobile, macOS, Linux and Discord Web installation | Outside first-release support |

The theme does not add or rename channels, change descriptions, grant permissions, replace member names or insert messages. Background code/arena text is decorative artwork, not a status indicator or community announcement. Channel-list headers are local decoration rather than Discord's native boosted-server banner.

## Launcher support

Automatic setup: Windows x64, per-user stable Discord, standard Vencord folder, normal privileges. The installer operation uses official Vencord CLI v1.4.2 with a pinned binary digest. It can still require manual assistance if Discord's installation layout changes or upstream services are unavailable.

Store, PTB, Canary, custom Vencord profiles, obvious unknown loaders and linked profile folders are not automatically modified. Use the manual route in [Installation](INSTALLATION.md). Full Windows 10, clean-machine and low-end hardware coverage is still pending; see [Validation](VALIDATION.md).

## Performance and accessibility

All decorative images are static SVG data embedded in the CSS. No theme JavaScript, polling, remote font/image imports, full-window blur or continuous animation is added. The launcher exits when closed and adds no resident process to Discord. Static backgrounds and CSS relational selectors still have rendering cost.

Tested text/input contrast pairs and desktop fixture widths are recorded in Validation. Scaling, high contrast, large text and other third-party theme combinations need wider testing. If a conflict appears, compare with these themes disabled and preserve the server guard when investigating.

## Maintenance

After a Discord update, inspect the selected-server marker and panel hierarchy in the actual client, update sanitized fixtures only from structural observations, rerun negative isolation cases, verify all three servers, and publish CSS/catalog/checksums together. Never remove the guard as a quick fix.

A companion plugin is still unnecessary for supported views. [Vencord custom plugins require source builds](https://docs.vencord.dev/installing/custom-plugins/), which would make installation harder for members. External portals remain stock instead of introducing that dependency.

FantaFort uses Bangers for display lettering, server/channel headings, channel names and member groups. Normal messages, usernames and input retain their existing fonts. The font and artwork are embedded; no font installation is required. Other scripts/languages fall back to available native fonts when a glyph is absent.
