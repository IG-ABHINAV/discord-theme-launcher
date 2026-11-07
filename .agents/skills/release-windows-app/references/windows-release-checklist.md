# Windows Release Checklist

Use this checklist for final release readiness, release candidate reviews, and GitHub Release preparation.

## Source And Version

- Confirm release branch and clean worktree expectations.
- Confirm tag name, product version, package version, installer version, assembly/resource version, and changelog version match.
- Check CI version variables and generated version files.
- Confirm copyright, publisher, company, product name, icon, and file metadata.

## Changelog And Notes

- Update changelog with user-facing changes.
- Mention breaking changes, migration notes, security fixes, known issues, and upgrade steps.
- Prepare GitHub Release title, body, tag, milestone, and compare link when useful.
- Avoid publishing secrets, internal paths, or unsupported claims.

## Build

- Use Release configuration, not Debug.
- Build the intended architecture: x64, x86, ARM64, AnyCPU, or multi-arch.
- Verify runtime choice: self-contained, framework-dependent, single-file, trimmed, AOT, bundled runtime, or native dependencies.
- Confirm output contains required DLLs, assets, config, localization, icons, licenses, and data files.
- Keep symbols/source maps according to project policy.

## Signing

- Sign final executables, DLLs if required, installers, MSIX packages, ClickOnce manifests, and update packages.
- Use timestamping.
- Verify signature with `Get-AuthenticodeSignature` or `signtool verify`.
- Confirm publisher identity matches installer/package identity.
- Keep certificates, PFX files, passwords, and signing tokens out of the repository.

## Installers

- Verify installer product code/upgrade code/version behavior when applicable.
- Test clean install, upgrade from previous release, repair if supported, rollback, uninstall, and reinstall.
- Check per-user/per-machine behavior, UAC prompts, shortcuts, file associations, protocols, services, scheduled tasks, registry entries, firewall rules, and install directory.
- Confirm logs and crash dumps are stored in appropriate user-writable locations.

## MSIX

- Check package identity, publisher, version, capabilities, app manifest, dependencies, signing, app installer file, update URI, update frequency, and Store/enterprise requirements.
- Test install, launch, upgrade, uninstall, and data persistence.
- Verify restricted capabilities or broad file-system access are justified.

## ClickOnce

- Check publish profile, application version, minimum required version, update URL, update mode, prerequisites, signing certificate, deployment manifest, application manifest, and rollback behavior.
- Test first install, online/offline mode, update prompt, automatic update, and uninstall.

## Smoke Tests

- Test the final packaged artifact, not only raw binaries.
- Launch on Windows 11.
- Verify version/about screen and signed binary identity.
- Exercise primary user flow, settings load/save, file operations, network calls, update checks, and app shutdown.
- Inspect logs for startup errors and missing dependencies.
- Test uninstall cleanup for files, shortcuts, registry, scheduled tasks, and services.

## Antivirus False Positives

- Generate SHA256 hashes for every published asset.
- Check signature validity and timestamp before scanning.
- Avoid packers or obfuscators unless the project intentionally uses them.
- If scanning is required, record vendor detections, detection names, asset hash, signature status, and submission links.
- Prepare false-positive submissions with signed samples and release provenance.

## GitHub Release Assets

- Attach all intended assets and no stale artifacts.
- Use consistent naming: app-version-windows-arch-package.ext.
- Publish SHA256 checksums in a `.txt`, `.sha256`, or release notes section.
- Verify downloads install or extract correctly.
- Set draft, prerelease, and latest flags intentionally.
- Confirm release notes match the tag and attached assets.

## Final Gate

- Build passed.
- Tests passed or accepted exceptions are documented.
- Signatures verified.
- Installer/package smoke tests passed.
- Checksums generated.
- Changelog and release notes are ready.
- GitHub Release assets are complete.
