# Theme Studio acceptance evidence

Task `theme-studio-20261107`, branch `codex/theme-studio-community-theme-studio-20261107`.
The supplied beta.7 source overlay, November Discord class map, research and six local skill packs are the baseline. Archive SHA-256 values match the supplied manifest. Job supervision belongs to Hermes; this repository does not configure it.

| Milestone | Status | Required evidence / remaining work |
| --- | --- | --- |
| baseline_beta7 | Verified | Supplied snapshot checkpoint `5090a9a` (see history for exact SHA); CSS audits and 61 core checks passed. Pinned Chromium installed; baseline `npm test`: 45 passed. Startup/runtime/file regression coverage retained. |
| catalog_recovery | Verified | Bounded extensible catalog/capabilities; fourth-entry regression failed before fix. `evidence/catalog-recovery.txt`: 63 checks passed, including historical subset restore, newer files and unchanged runtime. |
| neutral_editor | Implemented, final verification pending | Neutral defaults with no server; Create/Community/My themes; preview stays available on a synthetic example until an explicit target is supplied. Browser tests cover edit/share/confirmation/cancellation/layout. Save/reopen/recovery exercised at real Core seams. |
| community_zenit | Implemented, final verification pending | Four credited entries, pinned original CSS and licenses, offline pinned Montserrat; author background/mascot retained. Browser review found and fixed backdrop occlusion. Core tests cover exact source roundtrip/recovery; native gallery/variable edits/cancellation passed. |
| betterdiscord_import | Implemented, final verification pending | Actual downloaded CSS picker, strict bounded parser and conversion, resource/compatibility dialog, variable edits, source export, portable source/provenance/integrity. Zenit and Dracula fixtures tested. Rejections include resources, corruption, size/path abuse, syntax escapes and containment resets. Preview now verifies image decoding before application. |
| localization_submission | Implemented, final verification pending | German native/WebView coverage, persistent language, long labels at 1040/1365; existing four languages retained. Source keys and new import errors translated. Real GitHub issue template fallback; optional validated Discord destination through `THEME_STUDIO_SUBMISSION_DISCORD`. No existing Discord invite found in project sources. |
| validation_packaging | In progress | `npm run verify`: 93 Core + 54 browser tests passed before final hardening. Native integration: 43 real Windows/WebView checks passed; these are not interactive desktop tests. Release packaging and final run remain. |
| final_review_delivery | In progress | Review fixed backdrop layering, scope breakout, containment resets and resource readiness. Documentation, draft PR, artifacts and final acceptance review remain. |

## Constraints and assumptions

- Keep WinForms/.NET 10/WebView2 and one active personal slot; users can save multiple portable files. No production Discord profile is used.
- The supported import model must reject CSS whose design cannot safely stay inside the selected server subtree. Preview must never download arbitrary resources.
- Native interactive and Windows reboot checks are **pending: no logged-in desktop**. Compilation, isolated native integration and browser coverage do not imply those checks passed.
- Do not publish a release or merge. Do not contact JACKPOT71. Preserve MIT copyright and credit adaptation separately.

Detailed command output and phase outcomes are stored in the job-local evidence directory and progress.json; they are not public PR content.
