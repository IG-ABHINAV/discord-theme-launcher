# Theme Studio acceptance evidence

Task `theme-studio-20261107`, branch `codex/theme-studio-community-theme-studio-20261107`.
The supplied beta.7 source overlay, November Discord class map, research and six local skill packs are the baseline. Archive SHA-256 values match the supplied manifest. Job supervision belongs to Hermes; this repository does not configure it.

| Milestone | Status | Required evidence / remaining work |
| --- | --- | --- |
| baseline_beta7 | Verified | Supplied snapshot checkpoint `f477486` (see history for exact SHA); CSS audits and 61 core checks passed. Pinned Chromium installed; baseline `npm test`: 45 passed. Startup/runtime/file regression coverage retained. |
| catalog_recovery | Verified | Bounded extensible catalog/capabilities; fourth-entry regression failed before fix. `evidence/catalog-recovery.txt`: 63 checks passed, including historical subset restore, newer files and unchanged runtime. |
| neutral_editor | Verified | Neutral defaults without a server; Create/Community/My themes; synthetic preview before target selection. Browser edit/share/confirmation/cancellation/layout and real native empty-target preview pass. Core save/reopen/recovery pass. |
| community_zenit | Verified | Four credited entries, pinned original CSS/licenses and hash-verified offline Montserrat; original banner/mascot retained. Actual browser appearance/edit/isolation and native gallery/edit/cancel pass. Original source and generated design roundtrip exactly. |
| betterdiscord_import | Verified | Real downloaded-file picker uses tested Core file seam; strict parser/conversion, compatibility/resource dialog, author variables, source export and portable original bytes/provenance. Zenit and Dracula tests include fresh-process recovery, rejection/resource failure, bounded image decoding, containment and visible settings portal. |
| localization_submission | Verified | Five complete UI catalogs, German native keys/import errors/persistence and long browser labels at 1040/1365. Native long-consent wrapping regression fixed. GitHub submission template and validated configurable Discord route implemented; no invite invented. |
| validation_packaging | Verified | Reviewed `npm run verify`: 98 Core + 55 browser tests passed. Reviewed native suite: 45 Windows/WebView checks passed. Self-contained Release EXE/ZIP, extracted executable identity, required notices and release hashes verified. Packaged safe-preview process remains running for observation; no accessible window/title is available, so interactive launch/close remains pending. Artifacts/checksums/screenshots/report are durable in job evidence. |
| final_review_delivery | Verified delivery candidate; independent gate pending | Full product review fixed backdrop/portal layering, syntax escape, containment resets, image decode/budget and German label wrapping. README/changelog/compatibility/provenance/report/credited screenshots and portable artifacts are prepared. [Draft PR #1](https://github.com/PrimeBuild-pc/discord-theme-launcher/pull/1) is open. The original commit chain was published through GitHub's official Git Data API using existing authorized CLI access, with exact blob/tree/commit SHA verification before the branch ref update. Credential storage and account settings were unchanged. The independent verifier checks the final committed head, artifacts and CI before completion. |

## Constraints and assumptions

- Keep WinForms/.NET 10/WebView2 and one active personal slot; users can save multiple portable files. No production Discord profile is used.
- The supported import model must reject CSS whose design cannot safely stay inside the selected server subtree. Preview must never download arbitrary resources.
- Native interactive and Windows reboot checks are **pending: no logged-in desktop**. Compilation, isolated native integration and browser coverage do not imply those checks passed.
- Do not publish a release or merge. Do not contact JACKPOT71. Preserve MIT copyright and credit adaptation separately.

Detailed command output and phase outcomes are stored in the job-local evidence directory and progress.json; they are not public PR content.
