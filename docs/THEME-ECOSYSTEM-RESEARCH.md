# Discord theme ecosystem decision

Researched 2026-11-07. Official documentation, upstream source code, and a read-only API request only. **Verified** describes those sources; **Assessment** is an engineering recommendation or inference. No implementation or runtime compatibility test was performed.

## Recommendation

**Assessment:** Do not replace the current Vencord launcher with BetterDiscord just to obtain themes. Vencord already imports BetterDiscord CSS. BetterDiscord adds a curated catalog and distribution workflow, but does not automatically turn global themes into server-specific themes. Keep the existing client integration if server-specific behavior is the product; begin with one explicitly supported Zenit Gold adaptation. If the goal has become ordinary global theme browsing/installing, using BetterDiscord's existing store/CLI could eliminate much of the custom launcher work. The evidence for these distinctions follows.

## What themes actually provide

- **Verified:** BetterDiscord distributes a vanilla-CSS `*.theme.css` file with a metadata header. Its documented environment has no automatic variable scoping. Its current theme manager injects the CSS rather than converting selectors to a server-specific format. Sources: [theme structure](https://docs.betterdiscord.app/themes/introduction/structure), [environment](https://docs.betterdiscord.app/themes/introduction/environment), [theme manager](https://github.com/BetterDiscord/BetterDiscord/blob/development/src/betterdiscord/modules/thememanager.ts).
- **Verified:** Vencord explicitly supports importing BetterDiscord themes. Its loader imports enabled local files and online URLs as stylesheets; the reviewed loader supports light/dark URL conditions, without a guild mapping. This establishes CSS-format support, not a guarantee that every theme renders identically. Sources: [Vencord README](https://github.com/Vendicated/Vencord/blob/main/README.md), [theme loader](https://github.com/Vendicated/Vencord/blob/main/src/api/Themes.ts).
- **Verified:** Theme customization is normally exposed through author-selected CSS variables; there is no shared schema requiring all themes to expose the same colors, backgrounds, or layout controls. Source: [BetterDiscord user configuration guide](https://docs.betterdiscord.app/themes/tutorials/user).
- **Assessment:** Another launcher can download/edit CSS files and offer theme-specific controls. A universal visual editor or automatic server converter is a separate engineering project. A server-aware activation mechanism, or carefully gated selectors, must still be supplied and maintained.

## Why automatic server isolation remains difficult

**Verified:** Zenit Gold currently contains `:root`, `body`, `body::before`, and `#app-mount` rules, embedded artwork, and an external Google Fonts import. Its author documents both BetterDiscord and Vencord installation and editable `--zg-*` variables. Sources: [stylesheet](https://github.com/JACKPOT71/zenit-gold/blob/main/ZenitGold.theme.css), [author README](https://github.com/JACKPOT71/zenit-gold).

**Assessment:** Simply placing those rules beneath a server-content selector changes their meaning: `body` is not inside a channel pane, root variables can affect shared UI, and menus/popouts may live elsewhere in the document. Decide whether “per server” means whole-client theme activation while that server is selected or styling only its content pane. The former is much easier to preserve for an existing global theme. Neither is supplied merely by switching theme loaders.

**Verified:** CSS `@import` is a stylesheet-level construct with ordering restrictions. The CSS scoping draft describes `@scope` as matching a document subtree/fragment. **Assessment:** Wrapping arbitrary downloaded CSS or prefixing every selector cannot be treated as a universal converter; imports, document-root rules, and shared UI need explicit handling. Sources: [W3C import rules](https://www.w3.org/TR/css-cascade-5/#at-import), [W3C scoping draft](https://www.w3.org/TR/css-cascade-6/#scoped-styles).

## BetterDiscord publishing and updates

**Verified:** Submission goes through the website: connect Discord, obtain account authorization, submit, then await code review. The documentation gives no review-time guarantee. Acceptance creates a website listing; approval is not necessary for loading a local CSS file. Sources: [submission process](https://docs.betterdiscord.app/themes/publishing/submit), [local quick start](https://docs.betterdiscord.app/themes/introduction/quick-start).

**Verified:** A published addon needs a stable final-file location in a public GitHub repository. GitHub updates trigger a webhook and renewed approval; README updates are separate. Accepted addons appear in the in-client store and have download/deep links. Remote CSS imports are also documented as an automatic-update technique; do not equate live upstream imports with each fetched byte receiving a fresh catalog review. Sources: [distribution and updates](https://docs.betterdiscord.app/themes/publishing/distribution), [remote imports](https://docs.betterdiscord.app/themes/tutorials/remote).

**Verified:** Catalog guidelines reject automatically generated themes and customized versions of someone else's theme; the codebase must primarily be the submitter's. Permission to reuse some code is separately recognized. **Assessment:** Author permission/MIT licensing does not establish catalog eligibility for a Zenit Gold derivative. Coordinate an upstream author-maintained version or ask the reviewers about the exact proposal before relying on admission. Source: [theme guidelines](https://docs.betterdiscord.app/themes/publishing/guidelines).

## Catalog integration and permissions

**Verified:** BetterDiscord has an official Go CLI with theme management and store search. Current core source names the v3 store endpoints. On the research date, `GET https://api.betterdiscord.app/v3/store/themes` returned HTTP 200 and 112 records, including source URLs and metadata, without a license field. Sources: [official CLI](https://github.com/BetterDiscord/cli), [endpoint definitions](https://github.com/BetterDiscord/BetterDiscord/blob/development/src/betterdiscord/data/web.ts), [themes endpoint](https://api.betterdiscord.app/v3/store/themes).

**Research limit:** No documented third-party API availability commitment, rate-limit policy, or blanket catalog/assets redistribution license was found in the reviewed official material. Public reachability alone does not resolve those questions. **Assessment:** Start with links and a small author-approved manifest; obtain integration guidance before depending on their API or mirroring the catalog.

**Verified:** Zenit Gold's MIT license permits modification/distribution subject to preserving its copyright and permission notice. GitHub distinguishes public visibility from an open-source license. **Assessment:** Preserve Zenit Gold attribution and notices; verify each additional theme and separately licensed asset/font before redistribution. One author's permission is not permission for the whole catalog. Sources: [Zenit Gold license](https://github.com/JACKPOT71/zenit-gold/blob/main/LICENSE), [GitHub licensing guidance](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/licensing-a-repository). These are source-based observations, not a legal determination.
