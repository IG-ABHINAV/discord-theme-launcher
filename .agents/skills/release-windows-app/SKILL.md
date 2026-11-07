---
name: release-windows-app
description: Prepare, review, or troubleshoot Windows application releases. Use when Codex needs to verify release versioning, changelog entries, Release builds, code signing, Authenticode certificates, installers, MSIX, ClickOnce, smoke tests, antivirus false positives, checksums, GitHub Release notes, and release assets for Windows desktop apps built with .NET, WPF, WinForms, Flutter, Electron, Tauri, native C/C++, or similar stacks.
---

# Release Windows App

Use this skill to prepare or audit a Windows app release before distribution. Treat release work as a reproducibility and trust problem: version identity, source tag, binary provenance, signature, installer behavior, smoke test evidence, and published assets must agree.

## Workflow

1. Identify the release target:
   - App stack: .NET/WPF/WinForms, Flutter Windows, Electron, Tauri, native, or another Windows desktop stack.
   - Packaging model: portable zip, setup exe, MSI, MSIX, ClickOnce, Microsoft Store, winget, or GitHub-only assets.
   - Architecture: x64, x86, ARM64, AnyCPU, single-file, self-contained, framework-dependent, or multi-asset.
2. Verify versioning before building:
   - Confirm semantic/product version, assembly/package version, installer version, app manifest version, and Git tag match the intended release.
   - Check release branch, clean worktree expectations, CI version variables, and generated version files.
   - Avoid changing version numbers in only one layer.
3. Verify changelog and release notes:
   - Ensure `CHANGELOG.md`, release notes, commit range, and tag name describe the same release.
   - Mention breaking changes, migrations, known issues, security fixes, and upgrade notes when relevant.
4. Build Release artifacts:
   - Use repository scripts or CI workflows first.
   - Build in Release mode with the intended runtime identifier and architecture.
   - Capture exact commands, output paths, and environment prerequisites.
5. Sign and package:
   - Verify code signing certificate, timestamping, signature chain, publisher identity, and package identity.
   - Build installer artifacts and ensure each executable/package that should be signed is signed after final packaging or according to the toolchain.
   - For MSIX and ClickOnce, verify identity, publisher, update settings, capabilities, and signing.
6. Smoke test clean artifacts:
   - Install or unpack on a clean Windows environment when possible.
   - Launch, exercise primary flows, verify version/about screen, update behavior, uninstall cleanup, file associations, and first-run state.
7. Prepare publication:
   - Generate checksums for each asset.
   - Review antivirus false-positive risk and vendor submission process when relevant.
   - Prepare GitHub Release assets, notes, tags, and draft/prerelease/latest flags.

Load `references/windows-release-checklist.md` for broad release readiness reviews, high-risk releases, or when preparing GitHub Release assets.

## Versioning

Check every version surface the project uses:
 - Source tag and release branch.
 - `AssemblyVersion`, `AssemblyFileVersion`, `AssemblyInformationalVersion`, `.csproj` `Version`, MSIX package version, ClickOnce publish version, installer product version.
 - Flutter `pubspec.yaml` `version`, Windows runner metadata, installer config.
 - Electron/Tauri package version, Windows resource metadata, installer config.
 - CI variables, generated files, and release scripts.

Prefer one canonical version source and generated downstream versions when the repository supports it. Report mismatches as release blockers.

## Changelog

Ensure release notes are user-facing and traceable:
 - Summarize notable features, fixes, security updates, and known issues.
 - Include upgrade or migration notes for settings, data files, protocols, plugins, or installer changes.
 - Avoid dumping raw commit logs unless the project convention expects it.

For GitHub Releases, verify the tag, title, notes, attached assets, checksums, and prerelease/latest status before publishing.

## Build Release

Use the project's existing build path. Typical examples:

```bash
dotnet publish -c Release
flutter build windows --release
npm run build
```

Do not assume Debug artifacts are acceptable. Check optimization, runtime identifier, self-contained settings, trimming/AOT/single-file settings, native dependencies, PDB/source map policy, and reproducible output paths.

## Signing

Verify Authenticode signatures on executables, DLLs where required, installers, MSIX packages, ClickOnce manifests, and update packages. Use timestamping so signatures survive certificate expiry.

Typical verification commands on Windows include:

```powershell
Get-AuthenticodeSignature .\MyApp.exe
signtool verify /pa /v .\MyApp.exe
```

Never hardcode certificate passwords, PFX passphrases, API tokens, or signing credentials. Prefer secure CI secrets, hardware tokens, certificate stores, or dedicated signing services.

## Installer, MSIX, And ClickOnce

Review installer behavior from a user's clean machine perspective:
 - Install, upgrade, repair if supported, uninstall, rollback, and reinstall.
 - Per-user versus per-machine install and UAC requirements.
 - Start menu shortcuts, desktop shortcuts, file associations, protocols, registry keys, services, scheduled tasks, and firewall rules.
 - Required runtimes and bundled dependencies.

For MSIX, check package identity, publisher, capabilities, app installer update settings, signing, dependencies, and Store/enterprise constraints.

For ClickOnce, check publish profile, update URL, update mode, prerequisites, signing, application files, manifest versions, and rollback behavior.

## Smoke Tests

Run smoke tests on the final artifact, not only on build output:
 - Install or extract artifact.
 - Launch app.
 - Confirm version/about screen.
 - Exercise login or primary startup path if safe.
 - Open/save/import/export a representative file if supported.
 - Check logs, crash handling, auto-update behavior, and uninstall cleanup.

Automate smoke tests where the repository already has UI automation, Playwright, WinAppDriver, Appium, PowerShell, or project-specific scripts.

## Antivirus False Positives

Reduce false-positive risk by signing binaries, using stable publisher identity, avoiding suspicious packers, documenting network/process behavior, and shipping predictable installer structure.

When false positives are reported, collect hashes, signature details, detection names, vendor names, build provenance, and minimal reproduction notes. Prefer vendor false-positive submission portals and avoid unsafe workarounds.

## GitHub Release Assets

Before publishing, verify:
 - Tag points to the intended commit.
 - Asset names include app, version, platform, architecture, and package type.
 - Checksums are generated and published.
 - Release notes match changelog and asset set.
 - Draft/prerelease/latest flags are correct.
 - Assets install and launch after download.

## Output Format

For reviews, lead with blockers and high-risk findings first. Include file/line or artifact reference, impact, recommended fix, and verification command.

For release preparation, report the exact version, tag, generated artifacts, signing status, smoke tests run, antivirus notes, checksums, and GitHub Release readiness.
