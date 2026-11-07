# Proposed direction: an editor for community server themes

Status: proposal only, 2026-11-07. Lorenzo requested advice before implementation. No application, Discord profile, submission service or homelab deployment was changed for this proposal.

## Recommendation

Continue the project with a narrow purpose: let a community member create, preview, share and apply a design to a chosen Discord server. Keep Vencord as the existing runtime. A second general-purpose theme marketplace or a universal BetterDiscord replacement would duplicate established products and expand maintenance without a demonstrated need.

For someone who only wants a global Discord skin, installing a theme directly in Vencord or BetterDiscord is already sufficient. The useful differentiators here are an approachable editor, server isolation, portable editable configurations, and recoverable application. Changes remain local to each member; a server owner cannot impose the theme on everyone.

## Verified current gaps

- The installed local build is 0.3.0-beta.7. Its catalog and configuration allowlists contain Prime Build, Prime Build Dev and FantaFort. Zenit Gold is absent.
- Host localization and the editor selector support `en`, `it`, `es` and `ru`. German is absent in both layers.
- New configurations currently start from Prime Build assets and its server ID. A neutral design should replace this starting experience, and a new user should supply the target explicitly.
- The editor accepts validated `.dtheme` configuration data, images and fonts. It is not an arbitrary CSS editor/importer. There is one editable personal theme slot plus the three original presets; multiple independent custom configurations active on different servers would require an explicit extension to storage and recovery.

## Proposed interface

| Area | Content and actions |
| --- | --- |
| Create | Start from a neutral editable Fluent Dark design. Choose a server, colors, typography and artwork; preview before applying. |
| Community | Four curated starting designs: Prime Build, Prime Build Dev, FantaFort, and Zenit Gold — by JACKPOT71. Show author, source, license, compatibility and whether it is an adapted server edition. |
| My themes | Open/import a `.dtheme`, save and export an editable configuration, apply or recover a prior setup. |
| Propose a theme | Explain the review process and open the chosen Discord submission channel. GitHub issue/PR remains an optional route. |
| Explore more | Open the BetterDiscord catalog externally. Downloading an external theme does not imply it is compatible with this editor or isolated to one server. |

Fluent Dark should use charcoal surfaces, a restrained configurable accent, native readable message typography, generous spacing and a quiet low-contrast background. Any generated abstract artwork should be optional and avoid logos or embedded text. The initial design should look complete without artwork. This is a design brief, not a completed visual asset.

## Zenit Gold integration

The [upstream project](https://github.com/JACKPOT71/zenit-gold) documents support for both Vencord and BetterDiscord. Its CSS currently uses global `:root` values and a remote Google Fonts import. It cannot simply be copied into the existing server-only, offline preview/compiler and declared equivalent.

Create a reviewed server adaptation that preserves the original visual identity. Verify artwork, contrast, external font handling, current Discord layout and isolation from DMs, other servers and settings. Preserve the upstream source/version and copyright, ship the [MIT license](https://github.com/JACKPOT71/zenit-gold/blob/main/LICENSE), and show **Zenit Gold — by JACKPOT71**, linking to the author's profile and project. Label adaptation work separately. Lorenzo supplied the author's affirmative response to the proposed inclusion/adaptation; do not represent the adaptation as already shipped or tested.

## Relationship with BetterDiscord

[BetterDiscord themes](https://betterdiscord.app/themes) are CSS addons loaded by a client modification. [Standard theme installation](https://docs.betterdiscord.app/themes/introduction/quick-start) does not provide this project's editor or its server isolation guarantee. Reuse compatible upstream CSS deliberately; do not promise automatic conversion of arbitrary global CSS into an editable, server-isolated design.

Start with an external catalog link and curated integrations. Embedded third-party search/download would require a supported API arrangement, provenance, per-theme license handling and compatibility checks. Do not assume catalog availability grants permission to republish every theme.

The [official submission guidelines](https://docs.betterdiscord.app/themes/publishing/guidelines) exclude automatically generated themes and customized versions of someone else's theme. Our editable presets and the Zenit adaptation should not be presented as automatically eligible for that official gallery. Jackpot may choose to submit his original project himself.

## Submission workflow

Begin with one dedicated Discord forum/channel: author posts a `.dtheme` or source CSS, preview, source link and redistribution/license information. No GitHub account is required. Review precedes gallery inclusion, and receiving a file must never automatically install or execute it. Use statuses such as received, changes requested, accepted and published. Accepted assets enter the existing versioned repository/build flow with clear credits.

If submission volume later justifies a web form, it can collect the same information, store uploads in a private review folder, and send an email containing a review link. That is a separate small homelab service, with bounded uploads and no automatic publication. Local Homelab documentation was inspected only for context; no running upload service, email delivery or network exposure was verified or configured. Proxmox supplies hosting, not the complete submission workflow by itself.

## First implementation milestone

1. Add complete German UI coverage, including native errors, confirmations and recovery; verify language persistence and layout.
2. Introduce the neutral starting design and separate it from the curated community gallery; add author/source/license metadata instead of special-casing a fourth card throughout the app.
3. Integrate and test the server edition of Zenit Gold, retaining existing configurations and recoverable backups. Show Jackpot a preview before announcing availability.
4. Add `.dtheme` import/share entry points and the Discord submission route. The exact channel/invite must be supplied or verified before linking it in the app.

Defer a custom marketplace backend, arbitrary CSS conversion, multi-client installation and public homelab uploads. Revisit these only when actual community usage shows which limitation matters.
