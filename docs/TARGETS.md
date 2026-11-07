# Target scopes

Schema-one configurations retain Guild as the first server. Optional AdditionalGuilds and DmChannels arrays add explicit targets, and Panel adds a third custom-palette color. Absent optional fields keep previous behavior; a missing Panel falls back to Surface. Save/export can remain untargeted. Application requires at least one server or DM channel.

The editor accepts IDs/links separated by lines, commas, semicolons or whitespace, normalizes duplicates and validates 17–20 digit IDs. Server and DM inputs are separate. DM links use https://discord.com/channels/@me/channel, optionally followed by a message ID; user IDs are not chat channel IDs. Each category is bounded at 16 targets. The combined CSS scope is a union of exact target selectors rather than global styling.

Server detection retains the existing visible rail and unambiguous selected-server guards. DM detection additionally requires Home selected and a visible chat-messages list containing a message-row ID with the exact configured channel prefix. Vencord's [MessageLogger source](https://github.com/Vendicated/Vencord/blob/main/src/plugins/messageLogger/index.tsx) uses the same chat-messages-channel-message convention. This is a DOM compatibility seam, not a Discord API contract. If a DM is empty, markers are absent or the supported layout changes, styling stays off. No plugin, token, account discovery or message fetch is added.

The DM preview uses synthetic private-chat navigation, a named contact and a message composer. Other server is kept only in test fixtures for isolation regression checks. DM controls are hidden in the application when no DM targets are selected.

Automated tests cover both server IDs, the selected DM, a different DM, absent messages, a matching decoy outside the message list, DM-only save/reopen and initial preview, current/legacy layout anchors and native WebView bridge behavior. Live Discord DM application and empty-conversation styling are not claimed as tested.

Applying a personal theme still replaces the one personal slot. A bundled preset is disabled only when its server is among the new server targets. Other preset files and unrelated preferences are preserved, with the existing recovery snapshots.
