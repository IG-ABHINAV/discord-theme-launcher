# Historical implementation plan — Officina 0.1

Status: historical plan for the original single-server 0.1 beta. The maintainer subsequently authorized three server designs and a Windows launcher. Current scope and implementation are documented in [Architecture](docs/ARCHITECTURE.md), [Compatibility](docs/COMPATIBILITY.md), and [Validation](docs/VALIDATION.md).

Date: 2026-11-05

## 1. Outcome and approved decisions

Build an optional Discord desktop theme for members of the public Prime Build server. The theme should provide detailed visual customization, remain comfortable during everyday conversations, and be straightforward to disable or remove.

The approved direction is **Officina**, refined around PC gaming hardware: restrained PCB traces, heatsink fins, chip details, and mechanical finishes. The approved concept establishes visual direction; it is not a screenshot of a working Discord theme and does not prove server isolation.

Confirmed by Lorenzo:

- Discord desktop only; Windows is the first supported and tested platform.
- The target is the public **Prime Build** server, including both testing and release. Do not use Prime Build · Dev for testing.
- Public target guild ID: `1206415718728601600`, obtained from the public invite linked by the Prime Build website. Reconfirm the identity before the first live test.
- The theme must affect the current Prime Build view, rather than every server or direct message on the user's client.
- Evaluate a companion plugin if CSS alone cannot provide sufficient coverage and reliable isolation.
- All repository documentation, code comments, issue templates, release notes, and GitHub metadata will be in English. Conversation with Lorenzo remains in Italian.
- Publish under the GitHub account **PrimeBuild-pc**, using that account's credentials and Prime Build's commit identity.

## 2. Product boundaries

This is a client-side visual theme, installed voluntarily by each interested member. It does not change the server for members who have not installed it and does not require Nitro or server boosts for its local decorative styling.

The implementation changes presentation, not server settings, permissions, messages, channel names, roles, or account data. A decorative header must not be represented as a native server banner or a newly unlocked Discord feature.

Global user settings, billing, authentication, account security, unrelated servers, direct messages, and the game overlay retain their normal appearance. Context menus and dialogs may be themed only where their ownership by the active Prime Build view can be established reliably. Detached windows without a reliable context stay unthemed.

Compatibility with arbitrary third-party themes is not promised. Disabling this theme must restore the user's previous visual state, including any independently installed theme.

## 3. Visual specification

### Palette and typography

- Brand accent: `#FF6600`.
- Main near-black reference: `#050505`, with slightly lighter charcoal surfaces to distinguish the message area, navigation, cards, and composer.
- White and readable neutral grays for text. Preserve Discord's meaningful role, warning, error, connection, mute, and presence colors.
- Montserrat influences branded headings and the Prime Build wordmark. Keep Discord's existing UI font for conversation text and routine controls.
- Prefer the existing brand logo and a small embedded wordmark asset. Distributing a font is optional; include its original license if a font file is actually bundled.
- Preserve text scaling, comfortable spacing, visible keyboard focus, and native layout behavior.

### Hardware language

- Concentrate the strongest hardware detail in the Prime Build header and a small number of decorative edges.
- Use subtle chip/PCB/heatsink references, restrained copper/orange traces, compact fastener details, and lightly machined panel edges.
- Keep the message background uniform and unobtrusive.
- Use static artwork and modest hover/focus transitions. Decorative layers must not capture pointer input, cover controls, or shift layout.

### Coverage

Required in the main Prime Build view:

1. Server header, local decorative brand treatment, channel categories and channel rows.
2. Selected, hovered, unread, muted, and mentioned channel states.
3. Message area, message hover, selected/replied-to messages, mentions, links, dividers, and unread markers.
4. Composer, attachments, replies, edit state, toolbar, and visible focus states.
5. Embeds, code blocks, quotes, reactions, and common attachment surfaces.
6. Member list, group headings, and relevant user/voice panels.
7. Buttons, scrollbars, separators, and consistent small surface details.

Context-dependent coverage, verified individually:

- Search, threads, forum posts, pinned messages, media previews, tooltips, menus, profile cards, and dialogs opened within Prime Build.
- Voice/video views and screen-sharing controls where branding does not obscure connection or control state.
- Server-owned detached windows only if their context can be established independently of the main window.

Publish a coverage table distinguishing supported, limited, and intentionally stock surfaces. Do not describe a conceptual mockup as implemented functionality.

## 4. Architecture and the first implementation gate

### Default path: official Vencord plus one CSS file

The first implementation deliverable is a minimal scoping proof, before the full theme.

1. Inspect the actual current Discord desktop UI and identify a reliable active-server signal.
2. Prefer stable identifiers and the active navigation context over localized labels or an incidental class substring.
3. Validate that a selected Prime Build view can be distinguished from a server icon in the rail, an ordinary channel link in a message, a voice connection, or hidden/cached UI.
4. Apply a small temporary visual marker only to an intended container, navigate away, and verify complete removal.
5. Verify the same behavior after reload and during navigation between channels, threads, another server, and DMs.

The guard must fail closed: missing, unsupported, ambiguous, or changed context means the theme stays off. Simply finding the Prime Build server ID somewhere in the document is not sufficient.

All visual rules and theme variables must be scoped. Do not leave global `body`, `:root`, font, selection, scrollbar, or broad wildcard overrides active outside the target context. Scope decorative pseudo-elements as carefully as ordinary rules.

If `:has()` is used, constrain the anchor and inner traversal as much as the actual UI permits. Avoid repeated document-wide searches through messages. Check the Chromium/Electron version actually in use; do not assume support from a browser-only fixture.

### Conditional path: a small companion plugin

Lorenzo has authorized evaluation of an additional plugin. Evaluate it if it materially improves correctness, coverage, or scope-related performance.

- First check whether an official built-in capability can supply the required context.
- If a custom companion is justified, its responsibility is limited to identifying the current relevant server/window context and exposing a scoped attribute to the CSS.
- Prefer store subscriptions and lifecycle events over timers, DOM polling, source patches, or message inspection. Vencord currently exposes `SelectedGuildStore` and `SelectedChannelStore`; inspect the relevant APIs before implementing against them.
- When context is unknown, the target view is not active, the plugin is stopped, or cleanup runs, remove the theme activation attribute and event subscriptions.
- No outbound network requests, analytics, credential access, or message collection belong in the companion.

**Adoption tradeoff:** custom Vencord plugins currently require building Vencord from source. They are not installable like a normal theme file in the official installer build. Do not disguise this as a simple one-click dependency or distribute a replacement Discord installer.

Record the scoping evidence and decision in `docs/COMPATIBILITY.md`. Keep the standard CSS installation path whenever it can meet the isolation requirement. If advanced coverage needs the companion, document the standard and advanced coverage separately. If a plugin is required even for basic isolation, explicitly label that requirement before presenting any build as ready for general members.

## 5. Installation, removal, and local test safety

Member installation target:

1. Install Vencord from its official source if needed, following its current Windows instructions and without administrator elevation.
2. Open Vencord's Themes folder from Discord settings.
3. Copy `PrimeBuild.theme.css` into that folder and enable it.
4. Open Prime Build; other contexts retain their habitual appearance.

Use a local, versioned file as the recommended default so updates are deliberate. An online-theme URL may be documented later as an opt-in convenience, with its update behavior explained. There must be no remote CSS or font imports hidden inside the distributed theme.

Rollback:

- Normal: disable the theme in Vencord; optional removal deletes only `PrimeBuild.theme.css` and any files installed specifically for this project.
- If the settings UI cannot be used: fully close Discord, move this project's theme file out of the Themes folder, then reopen Discord. Verify this recovery path during testing rather than assuming it works.
- If an optional companion exists: disable it and verify attribute/subscription cleanup. If necessary, return to the official Vencord build using its documented process.
- Vencord itself can be removed through the official installer's Uninstall action.

After plan approval, installation of official Vencord for the necessary local tests is within implementation scope. Before changing the local installation, record the baseline and back up only settings/files that the task will change. Keep local backups outside Git. Do not copy account tokens or publish session data. Restart Discord only when this will not interrupt an active call or stream; ask interactively if the user must choose a safe restart time.

Testing on the public server is limited to read-only navigation and local presentation. Do not send test messages, upload files, start calls, change server configuration, or contact members.

Reversibility is a local technical property, not a guarantee of account safety. Vencord states that client modifications violate Discord's terms. Document this clearly without claiming ban immunity.

## 6. Implementation sequence after approval

### Phase A — Establish scope and baseline

- Initialize the local Git repository with repository-local Prime Build identity.
- Record the tested Discord, Vencord, Windows, display scaling, and hardware versions/configuration.
- Reconfirm the public guild ID and capture only the UI structure needed for scoping.
- Record theme-off behavior and performance.
- Complete the scoping proof and choose CSS-only or the documented companion arrangement.

Completion gate: the activation mechanism has evidence from the current desktop client, including negative cases. No global fallback is permitted.

### Phase B — Implement the approved visual system

- Implement the palette and surfaces first, then navigation, messages/composer, cards, members/voice controls, and contextual surfaces.
- Reuse verified selectors and a small set of namespaced theme variables.
- Integrate small original hardware artwork and actual Prime Build branding.
- Keep one distributable CSS file. Use a small asset/build helper only if needed; avoid an application framework or installer project.
- Check contrast, text scaling, keyboard focus, and semantic status colors throughout.

Completion gate: required surfaces match Officina's approved direction, controls remain usable, and no required content is hidden or replaced.

### Phase C — Validate isolation, rollback, and performance

- Add focused automated scope/regression checks with sanitized fixtures based on the observed UI.
- Use a browser renderer for visual checks, with any tooling kept as development-only dependencies. A small CSS parser is justified if needed to audit rule scoping reliably; avoid a custom regex-based CSS parser.
- Run the actual desktop test matrix below.
- Test disable/re-enable, file removal, reload, and recovery behavior.
- Capture sanitized screenshots of the real implemented theme and record measured results.

Completion gate: automated checks pass, the desktop checks are recorded honestly, and rollback restores the baseline appearance.

### Phase D — Documentation and publication

- Complete English documentation and repository metadata.
- Review the files to be published; exclude account data, local backups, private messages, temporary captures, and unrelated files.
- Create the public repository under `PrimeBuild-pc`, push the checked implementation using that account, and publish the first version with a downloadable CSS file and checksum.
- Verify the remote owner, metadata, README rendering, links, preview images, release download, and commit attribution.

Approval of this plan authorizes these implementation and publication steps after their gates pass. Routine implementation choices do not need another approval. Ask interactively if a material new tradeoff or an action outside this scope arises.

## 7. Validation matrix and acceptance criteria

### Scope and behavior

- Theme on in the target server's ordinary channels and relevant supported views.
- Theme off in another server, DMs, Friends/Home, and global user/account settings.
- A Prime Build icon remaining in the rail does not activate the theme elsewhere.
- A message or notification containing a Prime Build channel link does not activate it.
- Remaining connected to a Prime Build voice channel while browsing another server does not keep the other server themed.
- Fast server switching, reload, thread navigation, modal opening/closing, and sidebar variations do not leak styling.
- Unsupported detached windows and unknown UI states remain stock.
- Existing layout density, zoom, accessibility settings, and interaction targets remain usable.

### Restoration

- Disabling the theme restores previous colors, fonts, surfaces, scrollbars, and decorations.
- Removing the project's file is sufficient to remove its visual changes.
- An optional companion leaves no activation attribute, background timer, or event subscription after stop.
- Other installed themes/settings are preserved; compatibility limits are documented.

### Accessibility and visual QA

- Normal text contrast target: at least 4.5:1 against its actual surface; interactive boundaries/focus target: 3:1 where applicable.
- Verify the primary layout at 1280×720 and 1920×1080, a resized desktop window, and common 100%/125% scaling or zoom.
- Maintain visible focus, readable timestamps, native role colors, distinction between unread/selected/muted states, and clear destructive/voice control states.
- Decorative layers remain static, noninteractive, and unobtrusive. Respect reduced-motion preferences for any transitions.

### Performance

Separate the cost of Vencord from the theme: compare the same Vencord installation with this theme off and on, with the same plugins, window size, channel content, and hardware acceleration setting. Record stock Discord separately where practical.

- Compare idle, message scrolling, repeated server switching, and an existing call/stream view when safely available. Do not start a public call just to obtain a benchmark.
- Use repeated comparable runs and record CPU/GPU activity, memory, and scroll responsiveness where reliable measurement is available.
- Investigate reproducible additional idle work or persistent scroll stutter; reduce selectors/effects before release.
- Budget targets: CSS logic up to 100 KiB, original decorative assets up to 250 KiB total, and a combined standalone theme around 350 KiB or less. Treat these as engineering budgets, not performance claims.
- No perpetual theme animation, full-window blur, polling, remote asset dependency, or decorative work continuing after the target context is inactive.

Record environment, method, actual results, and limitations in `docs/VALIDATION.md`. Label untested cases honestly. Browser-fixture results must not be presented as desktop benchmarks.

## 8. Repository and public presentation

Proposed repository: **`PrimeBuild-pc/primebuild-discord-theme`**. The account and repository name have been checked; no matching repository was returned at planning time. Creation remains part of the approved implementation, not this planning phase.

Public title: **Prime Build Discord Theme — Officina**

Proposed description, adjusted only if actual verified capabilities require it:

> A server-scoped Discord desktop theme for Vencord, inspired by PC gaming hardware. Prime Build orange, clean dark surfaces, and simple rollback.

Homepage: `https://primebuild.website/`

Relevant topics: `discord`, `discord-theme`, `vencord`, `vencord-theme`, `css`, `dark-theme`, `gaming`, `pc-gaming`, `primebuild`, `customization`, `desktop`.

Search/discovery work means an accurate searchable name, clear opening description, useful topic labels, descriptive image alt text, stable section headings, and direct installation/download links. Do not add unsupported compatibility tags, artificial keyword repetition, or promises of search ranking.

README structure:

1. What the theme does, its Prime Build-only activation, and who needs to install it.
2. Actual sanitized preview images with descriptive alt text.
3. Requirements and honest compatibility/coverage table.
4. Short standard installation, immediate disable/removal, and optional advanced installation only if implemented.
5. Customization variables and deliberate update instructions.
6. Performance summary linked to measured evidence.
7. Troubleshooting, recovery, known limitations, and the Vencord/Discord terms notice.
8. Maintainer, website/community links, licenses, and asset credits where applicable.

Keep the repository small. Expected files:

```text
PrimeBuild.theme.css
README.md
PLAN.md
CHANGELOG.md
LICENSE
assets/
docs/
  INSTALLATION.md
  COMPATIBILITY.md
  VALIDATION.md
tests/
scripts/                  # only the checks/helpers actually needed
.github/workflows/check.yml
.gitignore
package.json              # development checks only, if needed
```

Add a companion directory or third-party notices only if those are actually needed. No unused scaffolding.

Proposed license: MIT for original theme code, with accurate separate notices for any third-party font or artwork actually included. Identify Prime Build branding separately from generic theme code so code licensing is not mistaken for an endorsement of forks. Copy no Zenit Gold code or artwork: it is a reference for customization depth, not the selected design.

## 9. GitHub identity and release controls

Verified during planning:

- Connected GitHub profile: `PrimeBuild-pc`.
- Active GitHub CLI account: `PrimeBuild-pc`.
- Existing local Git identity: `PrimeBuild <primebuild.official@gmail.com>`.

At initialization, set that identity locally in this repository. Before the first push, verify the authenticated actor, repository owner, remote URL, and commit author/committer. Do not attribute commits to Codex or add a Codex co-author trailer. Do not alter global Git identity or another account's repositories.

A GitHub owner/admin identity does not mean running the Vencord installer as Windows Administrator.

Release `v0.1.0` only after the required gates pass. If important compatibility remains unverified, publish an explicitly labeled prerelease rather than a stable claim. Include the exact CSS asset, SHA-256 checksum, tested versions, known limits, and rollback instructions. Use least-privilege CI and development-only checks; no credentials belong in committed files or logs.

## 10. Evidence and current limitations

Already established:

- Visual direction and hardware refinement approved by Lorenzo.
- Public target server identified from the brand website's invite.
- At planning time, the project directory was empty and was not yet a Git repository.
- At planning time, Discord was installed and running, and the normal Vencord settings folder was not found. The implementation subsequently installed official Vencord.
- GitHub publishing identity and proposed repository name checked without creating a repository.
- Vencord's theme loader applies imported CSS to client documents; its built-in light/dark URL filters do not provide a server selector.
- Vencord exposes selected guild/channel stores that may support a narrowly scoped companion. Their availability is not a tested implementation.

Questions investigated during implementation (results in [Validation](docs/VALIDATION.md)):

- A reliable active-server guard in the current Discord desktop UI.
- The exact standard versus companion-supported coverage.
- Actual desktop screenshots, rollback results, and performance measurements.

Primary references:

- [Prime Build website](https://primebuild.website/)
- [Prime Build website source](https://github.com/PrimeBuild-pc/website)
- [Vencord download and Windows installation](https://vencord.dev/download/)
- [Vencord support, removal, performance troubleshooting, and terms notice](https://vencord.dev/support/)
- [Vencord theme loader](https://github.com/Vendicated/Vencord/blob/main/src/api/Themes.ts)
- [Vencord selected stores](https://github.com/Vendicated/Vencord/blob/main/src/webpack/common/stores.ts)
- [Vencord custom plugin installation requirements](https://docs.vencord.dev/installing/custom-plugins/)
- [MDN: CSS :has(), compatibility and performance considerations](https://developer.mozilla.org/en-US/docs/Web/CSS/Reference/Selectors/:has)
- [GitHub repository topics](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/classifying-your-repository-with-topics)
- [GitHub README guidance](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-readmes)
