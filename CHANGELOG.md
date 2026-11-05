# Changelog

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
