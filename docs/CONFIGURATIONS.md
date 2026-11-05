# Personal configurations

## Format and target

`.dtheme` is versioned JSON data: schema 1, at most 24 MiB. It stores the exact target ID, preset/palette/font/artwork choices, custom colors and selected embedded assets. Assets contain a display filename, MIME type, bytes, SHA-256 and optional license. Filenames never become extraction paths: packages are not unpacked. Unknown choices and schemas are refused. Arbitrary CSS, HTML, JavaScript and remote asset references are not accepted.

The generated `PersonalServer.theme.css` is disposable output; retain the configuration for editing/sharing. Newer launchers may regenerate CSS from the same schema with updated Discord selectors. There are no external references, cloud synchronization or automatic updates.

Target selection accepts a 17–20 digit unsigned server ID or HTTPS Discord channel link. Invite links and arbitrary hosts are refused. The UI explains **User Settings > Advanced > Developer Mode**, then **right-click server > Copy Server ID**. The launcher never reads Discord account storage or requests credentials. Users must already belong to the server.

## Compilation and readability

`ThemeCompiler` verifies embedded CSS against the embedded catalog, retains its selected-server guard and retargets the ID. It substitutes validated colors and embedded artwork and registers one personal display-font alias. Message, author and composer typography stay native. Native typography omits font registration.

Imported backgrounds get a 93% dark scrim in recognized chat/forum containers. Custom surface luminance is capped at 0.055; composer boundaries use a stronger fixed border and accent button foregrounds adapt to accent luminance. Included SVG lettering remains part of its original design, even if the target name or display font changes.

## Import boundaries

The Windows importer decodes PNG/JPEG inputs, limited to 8 MiB, 4096 px per side and 16 million pixels, then re-encodes them as static PNG without original metadata. Output must still fit the limit. Reopened shared images are decoded before entering the editor. GIF, SVG, WebP and animated-image import are unsupported.

Custom static TTF/OTF files are limited to 2 MiB. Core validation checks signatures, table bounds, required tables and OS/2 embedding flags. Restricted, preview/print-only and bitmap-only embedding are rejected. Variable fonts, collections and imported WOFF/WOFF2 are refused. Import requires a license TXT up to 64,000 bytes, bounded license text and confirmation of editable embedding and redistribution rights. Flags alone cannot establish copyright permission. See the [OpenType OS/2 specification](https://learn.microsoft.com/en-us/typography/opentype/spec/os2).

Bundled fonts retain their unmodified bytes and SIL OFL notices. No fonts are installed system-wide. Missing glyphs may use native fallbacks.

## Preview trust boundary

`ThemePreview` escapes labels/license delimiters and inserts generated CSS into a synthetic fixture. `BuilderPreview` serves it through WebView2 resource interception at a reserved in-memory origin, avoiding NavigateToString's 2 MiB ceiling. External navigation, popups, downloads, permissions and document network requests are blocked. A CSP limits resources to embedded data. The runtime uses an isolated temporary profile, never Discord's or a signed-in browser profile. Runtime cache files may remain in the temporary directory after exit.

WebView2 Runtime is a prerequisite, never silently installed; missing runtime still permits saving/sharing. See [Microsoft local-content guidance](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/working-with-local-content) and [runtime distribution](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/distribution). Preview approximates supported Discord structures; it is not a live client capture.

Refresh is debounced. Navigation IDs stop obsolete loads from marking a newer configuration ready. Apply requires the latest valid preview, a loaded display font and restart acknowledgement. Invalid edits retain the previous preview but prevent application. File saves are atomic. The last saved/applied document is retained separately as `last-configuration.dtheme`; theme restoration does not overwrite it.

## Transactions and upgrade

One personal slot, `PersonalServer.theme.css`, is active at a time. Apply replaces it and disables a bundled preset for the same target, retaining that preset's bytes and other server presets. Disable removes only the personal slot; it does not automatically re-enable a conflicting preset. Restore recovers preceding managed bytes/enablement. Retargeting replaces the previous personal theme rather than accumulating files.

**Original presets…** retains multi-selection setup. Applying there replaces the personal slot too and removes unchecked bundled themes. Both flows use common snapshots and current-settings merging. New snapshots store four managed filenames and SHA-256 for existing files. All sources are verified before restoration. Old three-file backups restore their own subset and retain the personal slot. Other themes, plugins, QuickCSS and preferences stay outside management.

Original standalone CSS retains community IDs. Existing 0.2.x installations require no destructive migration. Save personal configurations before replacing the portable application with a newer version.
