# Startup incident - 2026-11-05

## Observed symptom and current state

The user supplied a native Discord error dialog: Electron could not find Vencord's `dist/patcher.js` required by the patched `app.asar` bootstrap. The user clarified that Discord was opened manually and a subsequent manual attempt succeeded.

At inspection, Discord had a loaded Friends window. The referenced patcher and other runtime files existed, were readable/nonempty, and had their original installation timestamps. Node module resolution for the patcher succeeded; patcher/preload syntax checks also passed. No matching Vencord/launcher detection events were returned by the scoped Defender query. These observations do not prove what caused the earlier missing-module event or rule out every external/transient cause.

The working runtime was not reinstalled or intentionally broken to reproduce the crash. No account data, messages, settings folders or protection settings were deleted or changed.

## Confirmed launcher defect

The old readiness predicate checked a recognized Vencord loader and `renderer.js` only. A minimized fixture with that loader and renderer but no patcher was therefore reported ready. This is a confirmed validation defect, even though the original transient disappearance/inaccessibility was not reproduced on the now-working installation.

The regression was added at the shared readiness seam used by `DiscordClient`, preserving the old predicate first. Command:

```sh
dotnet run --project launcher/Tests/Tests.csproj -c Release -- .
```

Before the fix it failed with:

```text
FAILED: renderer without patcher.js must not be reported ready
```

After the fix the same check and the rest of the suite pass. Additional cases cover a complete runtime, empty patcher, missing preload, and differentiation between native Error, generic splash and loaded Discord window titles.

## Corrected behavior

- Require readable, nonempty `patcher.js`, `preload.js`, `renderer.js` and `renderer.css` for an existing supported Vencord installation.
- Distinguish a recognized incomplete Vencord loader from an unknown client modification. Offer the official Repair operation for the former; keep refusing automatic changes to the latter.
- Recheck before launch and wait for a loaded Discord window before declaring success. Error windows fail; an unconfirmed startup times out with a qualified message.
- Do not automatically relaunch again after a failed operation.

The completeness check cannot prove that arbitrary existing runtime files are authentic or free of defects. Window-title observation is a bounded startup signal, not an exhaustive application health check. First-time installation and all native error variants still need wider clean-machine testing.

For a genuinely incomplete installation, [official Vencord support](https://vencord.dev/support/) recommends the installer's Repair operation. Theme snapshots restore managed CSS/enablement separately; they do not restore or remove Vencord's runtime.
