# Supported BetterDiscord import

Download a `.theme.css` from [BetterDiscord](https://betterdiscord.app/themes), then choose **Community → Import downloaded .theme.css**. Review compatibility and resources, accept the design, choose a server under **Create**, and preview it. **Style** shows only supported author variables. **My themes** saves/opens/shares `.dtheme` files and exports the original CSS bytes. Apply and recovery use the existing confirmation and managed transaction.

This is a supported subset, not a universal converter. A compatible import preserves its source design rather than replacing it with a preset. Importing or editing never applies to Discord. Changes affect the local client only.

## Conversion and resource policy

The native Core parser tokenizes strings, comments, selector lists, function/attribute nesting, blocks and declarations. Bounds: 2 MiB UTF-8 source, 4096 rules/declarations per block, 256 selectors per list, 32 syntax nesting levels, 16 grouping levels and 128 editable root variables. Malformed braces, escaped identifiers and nested CSS are rejected. There is no downloaded parser, script or plugin execution.

Every visual rule is emitted in the existing exact selected-server `@scope`. Ordinary selectors are confined to descendants of the server pane. Leading `:root`, `html`, `body` and `#app-mount` map to the pane. Document-root declarations support appearance and variables; root layout changes are rejected. Paint containment and isolation keep fixed backgrounds inside the pane. A former body backdrop sits behind pane content; pane stacking is normalized so detached settings remain visible above it. These adaptations are shown in the review; whole-window decorations become server-pane decorations. Detached settings, popouts, DMs and other servers keep their normal appearance.

| Construct | Supported behavior |
| --- | --- |
| Root custom properties | Six-digit hex colors and bounded nonnegative decimal literals are editable; other values remain in original CSS. `!important` author variables are supported. |
| Ordinary selectors | Original selector meaning within the selected pane; selectors targeting obsolete Discord classes may have no effect. Preview is illustrative, not a live compatibility oracle. |
| `@media`, `@supports`, `@container` | Preserved inside the server scope. |
| Named `@keyframes` | Preserved with a source-hash name prefix; direct animation references are rewritten. Reduced-motion rules are retained. |
| Fixed backgrounds/decorations | Contained and clipped to the selected pane. |
| Embedded images | PNG/JPEG/WebP data URLs; signature and dimensions checked, at most 4096 × 4096 and 16 million pixels per image, with a combined 32 million pixel budget. Actual preview image decoding must succeed before Apply. |
| Fonts / stylesheet imports | A reviewed offline adapter is required for stylesheet imports and `@font-face` registrations. The pinned Zenit import maps to bundled, hash-verified Montserrat with OFL notice and a unique family alias. No remote font request occurs. |
| External/relative resources | Rejected before accepting the import. No arbitrary resource is downloaded, including unused variable URLs. SVG and active URLs are rejected. |
| Global registrations and unsafe layout | `@property`, `@layer`, unknown rules, nesting, `all` resets, unsafe containment/display values and document-root layout changes produce an actionable rejection. |

CSS can still be visually unsuitable, expensive, or written for old Discord classes. Check the actual preview and author source. The converter does not claim that every current Discord surface or arbitrary imported design works. Generic fonts, advanced CSS syntax, remote import graphs and layouts requiring styles outside the server pane need separate reviewed adapters.

## Portability, integrity and recovery

Schema-one `.dtheme` files retain existing preset fields and may add an `Imported` record: original UTF-8 bytes, SHA-256, display metadata, source, license text and validated variable overrides. Packages are bounded to 24 MiB and never extracted. Source and overrides are revalidated on open, preview, compile and apply. SHA-256 detects byte corruption; it does not prove author identity or permission.

Original CSS export writes the exact imported bytes. That original may be global when installed directly in a different client: it is **not** the server-isolated compiled output. Use `.dtheme` to share the chosen target and edits through Theme Studio. Reviewed bundled adapters are available offline in this launcher; updated or changed upstream bytes need a new review and may be rejected. There are no automatic upstream updates.

One personal theme is active at a time; any number of portable files can be saved. Applying another design replaces `PersonalServer.theme.css`. Managed snapshots preserve previous exact bytes and enablement, including after a fresh process restart. Unrelated themes, plugins, QuickCSS, online links, preferences and Vencord runtime files are preserved. Historical backups restore only their recorded subset and leave newer files alone.

## Pinned representative fixtures

| Design | Provenance | Local source SHA-256 |
| --- | --- | --- |
| Zenit Gold — by JACKPOT71 | [Original source](https://github.com/JACKPOT71/zenit-gold/tree/64cc7a1eaf77dc3b081d5fd0637d8dc165b273c6), v2.2.1, MIT, © 2026 ZENIT / Jackpot. Original embedded WebP artwork retained; server adaptation by Theme Studio. | `3a196069c3fe38a9c2831e95955ba50d31381c2e7a9ae2878583049e24d61006` |
| Dracula | [Upstream base.css](https://github.com/dracula/betterdiscord/blob/77e73629086e5b77381f87b255b9f96401c61ced/base.css), MIT, © 2023 Dracula Theme. Stored unchanged as `tests/imports/dracula/DraculaBase.theme.css`; a self-contained source stylesheet, rather than its remote-import wrapper. Test fixture only, not a gallery entry. | `0e22391d6967acd78345196e0691ef28bec0c3ae1568c2931cfa5a26ee7b3fd7` |
| Montserrat | [Google Fonts source](https://github.com/google/fonts/tree/86aced91de05cd4f96a06838124cca45be337b2b/ofl/montserrat), unmodified `Montserrat[wght].ttf`, SIL OFL 1.1. The curated adapter is distinct from the existing static user-font importer. | `0f7b311b2f3279e4eef9b2f968bcdbab6e28f4daeb1f049f4f278a902bcd82f7` |

Source and font licenses are adjacent to the committed bytes and included in the portable package where applicable. Lorenzo supplied author consent for Zenit inclusion/adaptation. No author contact or publication is performed by the importer.
