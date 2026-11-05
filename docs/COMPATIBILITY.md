# Compatibility and scope

## Architecture decision

Officina uses the **official Vencord build plus one CSS file**. A custom plugin was evaluated as a possible architecture, but is not required for the first release: the current interface exposes the selected server through a server-rail item with both a guild-specific `data-list-item-id` and `aria-selected="true"`.

The active guard is anchored to Discord's recognized `.base__5e434` layout and its server navigation subtree. All declarations, custom properties, pseudo-elements, and scrollbar changes live inside one CSS `@scope` block. No rule is promoted to `body` or `:root`.

The guard also rejects a hidden/ARIA-hidden rail and a rail with an ambiguous selection. It does not activate just because a target icon, channel link, mention, message, or voice connection exists somewhere in the page.

The exact layout anchor is deliberate: after an incompatible structural update, styling should fail closed. Updating the theme may be needed even if some selectors could still render correctly.

## Target

- Public server: **Prime Build**, guild `1206415718728601600`.
- First tested platform: Windows Discord desktop, official Vencord.
- The similarly named development server is not included in the guard and was not used for theme testing.
- Browser and mobile installations are outside first-release support. Automated browser fixtures are development tools, not a claim of product support for Discord Web.

Exact tested client and environment details are in [Validation](VALIDATION.md).

## Coverage

| Area | Coverage |
| --- | --- |
| Server navigation, header and channel rows | Implemented |
| Selected, hover, muted and unread channel treatment | Implemented; preserve native state distinctions |
| Message area, mentions, links, reply styling and dividers | Implemented |
| Composer, focus, attachment/reply surfaces | Implemented; some less common states require further live testing |
| Common in-view embeds and reaction containers | Implemented |
| Code blocks and quotes | Implemented; preserve syntax colors |
| Member list and user panel | Implemented; preserve inline role and presence colors |
| Top bar, common surfaces, scrollbars | Implemented within the recognized Prime Build view |
| Search/thread/forum-specific surfaces | May inherit scoped surface tokens; not all layouts live-tested |
| Voice/video and screen-sharing views | May inherit scoped tokens; no calls or streams were started as test traffic |
| Portaled menus, profile cards and dialogs outside the base view | Stock appearance |
| Global user/account settings, billing, login and security | Stock appearance |
| Other servers, Home/Friends and DMs | Stock appearance |
| Detached windows without the recognized selected-server rail | Stock appearance |
| Game overlay | Outside the theme's target scope |

This is a detailed visual theme for the supported server view, not a promise to repaint every possible Discord window.

## Why no companion plugin?

A small event-driven plugin could expose more precise context for external windows and portals. However, [Vencord currently requires source builds for custom plugins](https://docs.vencord.dev/installing/custom-plugins/). That would increase installation and maintenance work for every member using the advanced path.

The current CSS path satisfies basic server isolation and main-view coverage. Keep external contexts stock rather than accepting that distribution cost for decorative consistency alone. Revisit a companion if a concrete, useful coverage requirement cannot be met safely in CSS.

## Other themes and settings

The theme does not change Discord's stored theme preference, font scale, account settings, or another theme's files. It styles the supported Prime Build view as a dark design regardless of the user's normal palette, and releases that scope on navigation away.

Arbitrary third-party theme combinations have not been exhaustively tested. If there is a conflict, compare Officina on its own and record the other theme's name and version in a bug report.

## Maintenance

After a Discord layout update:

1. Check the selected-server marker and layout ancestry in the actual client.
2. Re-run negative scope cases before updating any anchor.
3. Update the sanitized fixture only from observed UI structure, without copying community messages or account data.
4. Run `npm run verify`, perform the desktop checks, and record the new tested versions.
5. Rebuild and publish the CSS and matching checksum together.

Do not fix a missing theme by removing its server guard or adding a global fallback.
