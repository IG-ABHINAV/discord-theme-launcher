# Changelog

## Unreleased

## 0.3.0-beta.14 - 2026-11-11

- Stop personal themes from restyling all of Discord on every DOM change. The `:has()` target guard is now evaluated once on the app root and the remaining rules are gated by an inherited `@container style()` flag. In a synthetic 9,300-node Chromium benchmark, one DOM mutation (new message, voice participant, selection change) dropped from about 170 ms to about 0.6 ms with one personal theme.
- Keep exact server/DM targeting, hidden-rail and ambiguous-selection fail-closed behavior, multi-target and imported-theme support. Reapply the personal theme to regenerate the installed CSS.

## 0.3.0-beta.13 - 2026-11-11

- Cache the verified bundled presets and parsed catalog. Keep artwork payloads out of repeated palette, typography and target rewrites to reduce compilation allocations.
- Downscale new image imports to at most 1920 px per side and 2,073,600 pixels, preserving proportions and transparency. Existing shared assets remain byte-preserving.
- Release legacy preview response buffers without waiting for a later navigation. Keep the existing completed-resource retention regression from beta.12.
- Remove the invisible photo decoration box below the existing chat-width threshold. Preserve exact server and DM targeting, imported-theme compatibility and saved libraries.
- Document measured compiler allocation changes and their limits; no universal Discord RAM reduction or game-crash fix is claimed.

## 0.3.0-beta.12 - 2026-11-09

- Make saved designs reachable from Home, label Open in editor explicitly, and offer independent duplication without replacing the open draft. Clarify Save theme, Review & apply and adding targets in all five languages.
- Disable editable fields and language changes while the host is busy, instead of silently dropping edits.
- Require reviewed setup consent for Vencord repair; opening the editor no longer downloads an installer or closes Discord automatically.
- Release completed WebView2 response buffers instead of retaining them until exit. Skip malformed recovery entries and reject them safely before restoration.
- Remove the unreachable legacy launcher window and unused document-path bookkeeping. Add native end-to-end checks for reuse, small edits, duplication, multiple targets and cancellation.
- Display the user-chosen name and `by you` on personal Vencord theme cards. Preserve original author credit on imported designs.
- Add an optional, saved theme description in the Editor and show it on the Vencord card after application. Existing theme files remain compatible.

## 0.3.0-beta.11 - 2026-11-07

- Refresh the README with an HTML header, badges, product preview, quick start and focused contributor documentation.
- Remove the duplicate Save as new theme action in Files.
- Show a persistent saved check on theme, palette and image buttons; disable redundant saves until values change. Keep draft palette names while editing colors.
- Apply only the open theme to its explicit targets, disabling older launcher presets while preserving their files, external themes, plugins and recovery snapshots.
- Resume the last document, local profile identity, editor section, Templates tab and preview context.
- Flush pending edits before closing and offer embedded Save and close, Discard or Keep editing; include pending palette names and keep validation failures visible. Discarded edits stay discarded on reopening.

## 0.3.0-beta.10 - 2026-11-07 (included in beta.11)

- Order navigation as Home, Templates, Editor, Recovery, Help; add an empty template and Standard / My themes tabs.
- Save, select, rename and duplicate local theme profiles; save and reuse named three-color palettes.
- Make overly bright surface/panel errors visible beside their controls, in red, with accessible invalid-state markers.
- Show actual imported-art thumbnails, including changes with the same filename; save reusable images and select them for banners/backgrounds across themes, including imported designs.
- Apply one theme to up to 16 explicit server IDs and 16 explicit DM channel IDs. Hide Other server; show a real DM conversation preview only when DM targets are configured. DM styling requires visible message markers from the selected chat and stays off when detection fails.
- Preserve existing schema-one themes and assets. Local libraries persist under the user profile; saving does not restart Discord.

## 0.3.0-beta.9 - 2026-11-07 (included in beta.11)

- Add 64 searchable, categorized Google Fonts families downloaded on selection, pinned SHA-256 verification, reusable local cache and embedded OFL licenses; retain custom font import.
- Add the Theme Studio executable/window icon at Windows sizes 16–256 px.
- Simplify Home and Templates; consolidate Server, Style, Artwork and Files under one Editor. Move source/license details into an expandable section.
- Import individual .dtheme/.theme.css files or a folder with per-file compatibility reports. Save reusable designs before choosing a server.
- Localize illustrative preview content and complete German labels; keep authored names and source data intact.
- Embed Help, submission guidance and unsaved-edit prompts in the application. GitHub issue submission is user-operated, with no automatic upload or PR.
- Replace the legacy recovery window with an embedded reset choice (launcher themes or all styling) and selectable setup history. Reset preserves files/plugins and can itself be undone.

## 0.3.0-beta.8 - 2026-11-07 (included in beta.11)

- Start in neutral Fluent Dark with an explicit server target; organize Create, Community and My themes.
- Add Zenit Gold — by JACKPOT71 with pinned MIT source/artwork and an offline Montserrat server adaptation.
- Import downloaded compatible BetterDiscord CSS, review resources/limitations, edit supported author variables, and save/reopen/share portable configurations or export original bytes. Unsupported CSS is rejected before configuration or profile changes.
- Generalize catalog metadata and historical recovery; preserve exact managed bytes, Vencord runtime files and unrelated preferences.
- Add complete German native/editor copy and persistent language choice; retain English, Italian, Spanish and Russian.
- Add a real theme-submission issue template and configurable Discord destination.
- Keep all beta.7 November class fixes and startup/recovery regressions.
- Draft build only: unsigned; accessible-desktop, live-account and Windows reboot checks remain pending.

## 0.3.0-beta.7 - 2026-11-07 (included in beta.11)

- Restore the three server themes after Discord changed its CSS class names. Keep exact aliases for the previous and current layouts, including personal theme typography and custom backgrounds.
- Preserve the selected-server guard: other servers, DMs and unknown layouts remain outside the theme.
- Add regression coverage for all three presets and personal themes using the November Discord classes.

## 0.3.0-beta.6 - 2026-11-06

- Repair a Discord install that still loads Vencord when `patcher.js` is missing, instead of opening the JavaScript error window.
- On startup, download and verify the official Vencord installer, close the broken client, repair stable Discord, and open it only after the runtime files are present.
- If that repair does not finish, restore the local official `app.asar` backup so Discord can start.

## 0.3.0-beta.5 - 2026-11-06

- Added Italian, Spanish and Russian to the launcher interface. English remains the default.
- Kept documentation, release notes and setup instructions in English.
- Saved the interface language for the Windows user without changing `.dtheme` files.

## 0.2.4-beta.1 - 2026-11-05

- Added embedded Rajdhani display typography to Prime Build and JetBrains Mono to Prime Build Dev, including graphics, headings, categories, channel names and member groups.
- Kept message, username, composer and launcher typography native.
- Reused exact server scopes with one uniquely named font resource per theme and no font/image network dependencies.
- Added upstream WOFF2 fonts, full OFL notices, provenance and reusable glyph-outline extraction; all three themes remain below 350 KiB each.
- Extended font-loading, cross-server isolation and native text checks to all three identities.

## 0.2.3-beta.1 - 2026-11-05

- Renamed Prime Build / Officina to Build Your Dream and Prime Build Dev / Workbench to Software House in artwork, theme metadata, launcher cards and current documentation.
- Generate artwork subtitles from the profile names so the launcher and graphics stay consistent.

## 0.2.2-beta.1 - 2026-11-05

- Removed theme numbering from all three server headers, the FantaFort league artwork and decorative Dev line indices.
- Added embedded Bangers display typography to FantaFort graphics, server/channel headings, navigation and member-group labels.
- Kept ordinary messages, usernames, composer and launcher typography unchanged.
- Converted FantaFort graphic lettering to static outlines and included the unmodified font and SIL OFL notice in its CSS for offline use.
- Audited the uniquely named font registration separately from all server-scoped visual rules; added font-loading and isolation coverage.

## 0.2.1-beta.1 - 2026-11-05

- Reject incomplete Vencord installations before restarting Discord: check readable, nonempty patcher, preload, renderer and renderer CSS files.
- Offer official Repair for recognized Vencord loaders with missing runtime files; preserve unknown-loader refusal.
- Confirm a loaded Discord window before reporting success. Error windows and a still-starting client no longer pass.
- Remove automatic second launch after failure and retain explicit error/recovery instructions.
- Add regression coverage for the reported missing-patcher failure pattern. The original intermittent crash was not reproducible once the user reopened Discord successfully.

## 0.2.0-beta.1 — 2026-11-05

- Expanded the project into Discord Theme Launcher for Windows x64.
- Added independent Prime Build Dev / Workbench and FantaFort / Arena themes.
- Added original static chat, channel-panel and member-panel backgrounds for all three identities.
- Added a portable, self-contained launcher with selective setup, optional official Vencord installation, pinned download verification, backups, removal and reversible restoration.
- Preserved unrelated themes, plugins, QuickCSS and preferences.
- Matched the launcher shell to Discord dark colors and blurple controls, with installed gg sans or embedded Noto Sans fallback. Fixed unreadable disabled text.
- Added multi-theme isolation checks, installer-core tests and Windows CI packaging.
- Renamed the repository to `PrimeBuild-pc/discord-theme-launcher`; the earlier URL redirects.
- Kept the requested FantaFort community/channel revision as a separate workstream.

## 0.1.0-beta.1 — 2026-11-05

- First Officina theme for the public Prime Build server on Discord desktop with Vencord.
- Selected-server CSS isolation with a fail-closed layout guard.
- Original hardware header, embedded Prime Build logo, and coordinated dark/orange surfaces.
- Main-view channel, message, composer, card, reaction, member-list and user-panel styling.
- Self-contained CSS, deliberate local-file updates, automated scope checks, and documented removal.
- External portals and unsupported detached windows intentionally retain their normal appearance.
