# Theme Studio: editor, community gallery and BetterDiscord import

Status: ready for handoff review, 2026-11-07. Implementation and the overnight job have not started.

## Product promise

Create or edit a Discord theme easily, preview it, apply it to the server you choose, share it, and restore the previous setup.

Suggested English repository description: `Create, edit, preview and share Discord themes for the server you choose, with compatible BetterDiscord theme import and backup/restore.` Use the import claim publicly only after its acceptance checks pass. Keep the local-client explanation prominent: each member independently installs a theme; the launcher does not change the server for everyone.

## Baseline and delivery

- Start from the complete local source snapshot, including the uncommitted beta.7 Discord-class compatibility fixes, the class map and both research/proposal documents. Public main alone is an incomplete baseline.
- Use Codex `gpt-6.1-sol`, reasoning `high`, through Hermes on the existing development VM. Keep the existing .NET/WinForms/WebView2 stack unless a measured requirement justifies a small change.
- Deliver a branch prefixed `codex/`, a draft PR, an extracted portable Windows build, checksums, screenshots with synthetic data, and a tested/not-tested report. Keep production releases and main-branch merging for human review.

## Implementation sequence

### 1. Generalize the theme model and protect recovery

Replace hard-coded three-theme assumptions with a bounded manifest containing stable ID, display name, author, source, license, version, supported editor capabilities and verified local assets. Retain old `.dtheme` schema loading and historical backups. Define editable built-in/adapted themes separately from imported CSS capabilities; never represent an imported theme as a recolored Prime Build preset.

Completion: all existing fixtures load, backup/restore preserves exact bytes and unrelated settings, and a fourth community entry needs data plus an adapter where necessary rather than scattered UI special cases.

### 2. Improve the editor and neutral starting point

Create a minimal Fluent Dark starting design: charcoal surfaces, configurable accent, native message/input typography, restrained optional artwork, no community branding or implicit Prime Build target. A target server must be chosen before applying.

Organize around Create, Community and My themes. Make target, selected design, unsaved changes and preview state obvious. Provide approachable controls for colors, typography, backgrounds and banners; show only controls the selected theme supports. Keep an advanced CSS view secondary if needed. Editing never mutates Discord before confirmation. Preserve keyboard navigation, readable contrast, responsive layout, cancellation and recovery.

Completion: a new user can create, preview, save, reopen, share, apply and restore a theme without opening source files. Preview starts on the selected server and demonstrates other-server/DM isolation.

### 3. Community gallery and Zenit Gold

Include Prime Build, Prime Build Dev, FantaFort and `Zenit Gold — by JACKPOT71`. Every card exposes author/source/license and preview, customize and apply actions appropriate to its capabilities. Retain the original Zenit license/copyright and identify adaptation work separately. Preserve Zenit's artwork and design, handle its font resources deliberately, and test a reviewed server-specific adaptation. Lorenzo supplied the author's consent for inclusion and adaptation.

Completion: all four gallery entries work independently; DM/other-server/global-settings isolation and save/reopen are verified. Zenit is visibly credited and is not just the original global file copied into the profile.

### 4. BetterDiscord import is a required workflow

Provide an external `Browse BetterDiscord themes` button pointing to https://betterdiscord.app/themes. Import a downloaded `.theme.css`, inspect metadata/resources, choose the server, preview the actual imported design, customize supported variables, then confirm application and recovery.

Build a parser-based import/normalization pipeline rather than relying on regex-only selector prefixing. Address stylesheet-level imports, document-root rules, fixed backgrounds, fonts, animations and external resources. Keep the original source/provenance for lossless export and updates. Resolve external resources only through an explicit import policy and user-facing review; preview documents must not silently fetch arbitrary network resources. Persist a portable, bounded representation with integrity checks and safe reopen semantics. Do not execute imported scripts or load plugins.

A universal arbitrary-CSS converter is not an achievable blanket compatibility promise. Supported imports must be isolated and preserve the theme's real appearance; unsupported constructs produce an actionable compatibility report before touching Discord. No silent global application, no substitution with a built-in design, and no marketing claim that every theme is supported. If design fidelity and strict subtree isolation conflict, document the chosen supported behavior and reject the unsupported case.

Completion: at least two materially different representative upstream CSS designs, including Zenit, have pinned provenance and license-appropriate fixtures; successful import/preview/edit/save/reopen/apply/restore and deliberate rejection cases are tested. Missing import support means this milestone is incomplete, not silently deferred.

### 5. German and submission entry points

Add German across the WebView UI, native dialogs, errors, confirmations, recovery and language persistence. Retain English, Italian, Spanish and Russian. Check long German labels at supported window sizes.

Add `.dtheme` import/share and a `Propose a theme` action with submission guidance: author, preview, file/source and license. Use an existing verified Discord channel/invite if discoverable in repository sources; otherwise provide a real GitHub issue-template fallback, clearly label it, and keep the Discord destination configurable. Do not invent an invite or leave a dead button. A new upload backend/email service is outside this iteration.

### 6. Validate and deliver

Run the repository verification, native Windows checks and Release packaging. Add regression tests at the real import/persistence/isolation seams. Exercise startup, save/reopen, rapid edits, cancellation, migration, corrupt imports, missing Vencord and recovery in isolated profiles. Use screenshots of synthetic conversations. Verify repeated app/process restarts preserve runtime/themes/settings; describe Windows reboot coverage separately.

Review the patch, fix actionable findings, rerun affected checks, update README/package metadata/changelog/compatibility/validation docs, and prepare the draft PR and portable artifacts. Update the GitHub repository description if the existing account permits it; report a permission limitation without broadening credentials.

## Autonomous execution contract

Routine design and implementation choices are delegated. Record assumptions and continue independent work. Use project-local Ponytail, Matt Pocock TDD/diagnosis, desktop architecture/release and UI/UX guidance. Agent-desktop is for the VM's actual interactive desktop when available; load its desktop/platform instructions first. Headless/SSH execution is not evidence of an interactive session. Use automated WebView/browser checks when native interaction is unavailable and report that limitation accurately.

Hermes must supervise a persistent job independent of Lorenzo's PC. The current runner lacks quota resume and can terminate on SSH loss, so qualify a job-scoped persistent runner before starting implementation. Save exact Codex session ID, branch/worktree, logs, remaining checklist and artifacts. Resume that session/worktree after quota renewal; never create another branch for each retry. Honor provider reset timestamps, use bounded backoff when absent, retain the same model/account, and avoid API/paid-credit fallback. Stop on verified completion. Notify through the existing Hermes Telegram gateway at start, meaningful quota wait/resume, completion or actionable failure; deduplicate unchanged status.

No-approval execution applies to this authorized development job. Preserve the Hermes gateway, unrelated repositories, home network, NAS and credentials. Use the dedicated development VM and normal per-user privileges. Do not make production releases, merge main, reset shared machines, or send messages to Jackpot during the unattended run.

## Readiness found in this chat

Hermes is reachable; its gateway is active and user lingering is enabled. The existing dev-task pipeline supports the requested model and draft-PR workflow. The development VM is currently stopped, so its current Codex login, tools and interactive desktop have not been verified. Startup/preflight and the persistent quota-resume supervisor remain work for the approved dispatch. A prompt alone cannot guarantee quota renewal, valid credentials or eventual success.
