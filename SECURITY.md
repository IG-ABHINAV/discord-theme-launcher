# Security Policy

## Supported versions

Only the **latest published beta release** of Theme Studio (Discord Theme Launcher) is supported for security fixes. This project is pre-release software: every release to date is a `v0.x.0-beta.N` pre-release, and older betas are not patched. Check the [releases page](https://github.com/PrimeBuild-pc/discord-theme-launcher/releases) for the current supported version before reporting.

## Reporting a vulnerability

Please do **not** open a public issue for a security vulnerability.

Report it privately using GitHub's private vulnerability reporting form:

**[Report a vulnerability](https://github.com/PrimeBuild-pc/discord-theme-launcher/security/advisories/new)**

See [GitHub's guide to privately reporting a security vulnerability](https://docs.github.com/en/code-security/how-tos/report-and-fix-vulnerabilities/report-privately) if the form is unfamiliar. Include the affected version, what the issue allows an attacker to do, and steps to reproduce it. If you cannot use the form, email **primebuild.official@gmail.com**.

## Scope

In scope:

- The Theme Studio launcher application itself (Windows x64).
- The in-app update check, release downloads, and their SHA256 checksum verification (`AppUpdate`).
- Theme import and validation of untrusted CSS (BetterDiscord theme import, catalog parsing).
- The way the launcher writes theme files and settings into the local Vencord/Discord installation, and its repair of a broken Vencord `app.asar` stub.

Out of scope:

- **Discord** and **Vencord** themselves (including their own websites, clients, and update mechanisms), and BetterDiscord.
- Vulnerabilities that only affect a third-party theme's CSS content, not how this application handles it.
- Social-engineering attacks, physical attacks, and issues requiring a modified or malicious install source other than this repository's releases.

## What this application touches on your machine

Stated plainly so you know what a finding might involve:

- It writes theme files and settings under your local Vencord profile (`%APPDATA%\Vencord`) and reads/writes Vencord's `settings.json`.
- It can replace or restore Discord's `resources\app.asar` when installing or repairing the Vencord loader, and it restarts the Discord desktop process while you confirm the change. It refuses to run elevated (as Administrator).
- It makes outbound HTTPS requests to `github.com` / `api.github.com` (release and update checks, pinned downloads) and `raw.githubusercontent.com` (fonts). Downloads are restricted to HTTPS from `github.com` and verified against SHA256 digests before anything is installed.
- It does **not** read your Discord token, login, password, or other credentials, and no Discord login or token is required to use it.

Because it modifies your local Discord/Vencord installation, applying themes or repairs is done at your own risk; the launcher keeps local recovery snapshots so a setup can be restored.
