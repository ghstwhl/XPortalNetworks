# 3.0.0 - Independent Plugin Identity
* **New plugin GUID (`ModInfo.cs`)**
  * The plugin GUID is now `ghostwheel.mods.xportalnetworkstribespins` (was `vapok.mods.xportalnetworks`), so this fork no longer registers itself under the upstream author's namespace: BepInEx, Jotunn and any mod that inspects plugin metadata now see a mod of its own. `HarmonyGUID` follows the change automatically.
  * The old GUID is kept as `Mod.Info.LegacyGUID` and is used for exactly one thing - carrying an existing config file over to its new name (below). It is deliberately not used as a plugin GUID any more.
  * `Mod.Info.Name` carries the rename too: it is now `XPortalNetworksTribesPins` (was `XPortalNetworks`), which is the name BepInEx/Jotunn display and what the portal ZDO keys (`XPortalNetworksTribesPins_TargetId`, ...) and the RPC names (`XPortalNetworksTribesPins_SyncPortal`, ...) are derived from.
  * The previous name is kept as `Mod.Info.LegacyName`, and the portal data on existing portals is carried over read-side (`ZdoTools`, `XPortalNetworks.LegacyKey_*`): every read prefers the new key and falls back to the `XPortalNetworks_*` key a world saved by 2.6.0 or older carries, and every write updates both. Because the legacy key keeps mirroring the current value, a portal that is deliberately reset - Global network, not private, cleared name - cannot resurrect its earlier value, and the legacy keys can simply be dropped in a later cleanup.
  * The names of the UI GameObjects this mod creates (`XPortalNetworksTribesPins_MainPanel`, ...) and the RPC names follow the new Name. Both are per-session, so nothing persisted depends on them. The one-time `xportal_networks.json` import also still looks in the old folder (`BepInEx/config/XPortalNetworks/`), not just the new one.
* **Config file renamed to match the GUID (`XPortalNetworksConfig.cs`)**
  * BepInEx names the plugin's config file after the GUID, so it is now `BepInEx/config/ghostwheel.mods.xportalnetworkstribespins.cfg`. Everything lives there as before: `General/NexusID`, the server-owned `Portal Networks` sections with their `Name`/`Permitted` keys, `DefaultPortal`, and the `[General]` + `[Local Config]` toggles.
  * `XPortalNetworksConfig.MigrateLegacyConfigFile` runs before any setting is bound: if `vapok.mods.xportalnetworks.cfg` exists and the new file does not hold settings yet, the old file is copied to the new name and reloaded, so an upgrade keeps its networks, allow lists and preferences. A log line reports the migration and the old file can then be deleted. A new config file that already contains settings is never overwritten, and a failed copy is logged together with the target path so it can be redone by hand.
* **Vapok splash screen and telemetry removed (`ModInfo.cs`, `XPortalNetworks.cs`, `XPortalNetworksConfig.cs`)**
  * The mod no longer registers with Vapok.Valheim.Common's `ModSplashManager`, so it takes no part in that library's startup splash modal, its `ModSplashDossier` metadata, or its `TelemetryManager` (anonymous `mod_launch` / `mod_heartbeat` / `world_session_start` events and error reporting to the library's endpoint). `IPluginInfo` and its `PluginId` / `DisplayName` / `Version` / `Instance` members existed only for that registration and are gone with it.
  * Removing the code paths is deliberate, rather than switching them off: the library keeps its opt-in and error-report flags in shared Unity PlayerPrefs (`Vapok_Telemetry_OptIn`, `Vapok_Telemetry_ErrorReports_Enabled` - error reports default to *enabled*) and installs process-wide Harmony and `AppDomain.UnhandledException` hooks when its type is initialised. None of that is touched now, so this mod cannot influence another Vapok mod's telemetry settings, and a stored opt-in cannot switch anything back on for us. The library's endpoint is a compile-time constant, so it can be neither re-pointed nor overridden from a mod - which is another reason not to participate at all.
  * The three `[Local Config]` entries that only existed for that integration are removed with it: `Show Splash on Startup`, `Enable Anonymous Telemetry` and `Send Error Reports`. `Show Portal Map Pins` and `Show Network In Pin Name` become the remaining local preferences. Config files keep the retired keys, which BepInEx simply ignores.
  * The `Vapok.Valheim.Common` dependency is then dropped outright - `<Reference>`, `packages.config` entry and the ILRepack merge of it - so the shipped DLL contains no Vapok code at all. The last thing it was still used for, `Vapok.Common.Shared.ConfigurationManagerAttributes` on the two local preferences, now uses Jotunn's `ConfigurationManagerAttributes` (the class the server-owned entries already use for `IsAdminOnly`); only its `Order` field was ever needed there, and since the ConfigurationManager resolves that attribute by type name and copies matching fields, the ordering is unchanged. ILRepack goes with it: the project no longer imports its targets and `ILRepack.targets` / `ILRepack.Config.props` are deleted, leaving a plain single-assembly MSBuild build. The DLL shrinks from 552,960 to 118,784 bytes and the release zip from 339,969 to 176,874 bytes, and the `System.Net.Http` / `UnityEngine.UnityWebRequestModule` references - which came from the library's telemetry HTTP pipeline - disappear from the assembly too.
  * Docs updated to match: the configuration sections of `Docs/Modules/25Configuration.t4` and the mirrored tracked outputs (`Docs/README.Nexus.bbcode`, package README), the settings tables of `README.md` and `Docs/SolutionDir/README.md`, and the README privacy section, which is now a plain "this mod collects and sends nothing" statement without telemetry toggles, the in-game privacy-policy overlay, or opt-in/opt-out bullets.
* **Breaking for mixed-version multiplayer (`MAJOR`)**
  * Because the plugin identity changed, Jotunn's `NetworkCompatibility` and ServerSync see 3.0.0 as a different mod: the server and every client must be updated together. A 2.6.0 peer on a 3.0.0 server (or the reverse) counts as not having the mod installed.
  * Mods that hard-depend on `vapok.mods.xportalnetworks` - the standalone XPortal Shared Map Pins mod does - refer to the upstream mod and no longer resolve against this one. The map-pin feature is built in here, so 3.0.0 replaces that mod as before: do not install both.
  * Existing worlds keep their portal links, networks and private flags through the legacy ZDO keys described above; what changes is the key prefix and the RPC names.
* **2.6.0 superseded**: 2.6.0 was uploaded to Thunderstore but its listing was rejected, so it never became public - and Thunderstore refuses the same namespace/name/version twice, so the version had to move past it regardless.
* **Docs**: every place that names the config file was updated - `README.md`, `Docs/SolutionDir/README.md`, the tracked generated `Docs/README.Nexus.bbcode` and package README (the `25Configuration.t4` template derives the path from `Mod.Info.GUID`, so it only needed re-mirroring) - the `CustomNetworks` doc comment names the new file, and `REFERENCES.md` records the GUID and Name change. The Nexus reference (`Mod.Info.NexusId`, ID 3719) is deliberately left as the upstream mod's page - it is an external identifier, not part of the plugin identity being renamed, and this fork has no Nexus upload of its own.

# 2.6.0 - Portal Map Pins
* **Project home moved (`ModInfo.cs`, both manifests, docs)**
  * The project now lives at **[ghstwhl/XPortalNetworksTribesPins](https://github.com/ghstwhl/XPortalNetworksTribesPins)**: `ModInfo.GitHubRepo` (which drives the generated GitHub links/issue templates), both `manifest.json` files (Thunderstore `name` + `website_url`) and the packaged README/CHANGELOG all point there.
  * **[Vapok/XPortalNetworks](https://github.com/Vapok/XPortalNetworks) stays credited** as the base this project is built upon: the README credits it explicitly (`Based On`), `Docs/SolutionDir/README.md` links it as the base, and `ModInfo.GitHubRepo` carries a comment naming it.
  * **No identity change where it matters:** the plugin `GUID` (`vapok.mods.xportalnetworks`), the plugin `Name` (`XPortalNetworks` - the portal ZDO keys are derived from it, e.g. `XPortalNetworks_TargetId`) and the config file name are unchanged, so existing worlds, settings and multiplayer compatibility are unaffected.
  * **New publishing identity:** `Mod.Info.Author` is now `ghstwhl`, and the Thunderstore team/package (`NorCal_Nerds` / `XPortalNetworksTribesPins`) is declared once in `ModInfo.cs` (`ThunderstoreTeam` / `ThunderstorePackage`). `Docs/_Header.t4` builds the generated install links from it, and `tools/Build.ps1` names the package staging folder after it (`Release/NorCal_Nerds-XPortalNetworksTribesPins`, handed to MSBuild as `/p:PackageFolder` - the csproj `Copy` target now uses a `PackageFolder` property instead of a hard-coded `-Vapok` suffix).
  * `Mod.Info.Description` is synced with the manifest description. Still outstanding: `Mod.Info.NexusId` (and the README/README-Nexus Nexus links) point at the upstream mod's Nexus page until this fork has one of its own.
* **Portal map pins (`PortalMapPins.cs`)** - new feature
  * The standalone [XPortal Shared Map Pins](https://github.com/buldosik/valheim-mods/tree/master/XPortalSharedMapPins) mod by buldosik is now built into XPortalNetworks, so no companion mod is needed: every known portal you are allowed to use is drawn as a pin on your own map.
  * The pins are local map data - nothing is written to the world, nothing is sent over the network, vanilla player pins are untouched, and the pins are never saved to your map file (they are added with `save: false`, `ownerID: 0`).
  * Pins use a dedicated `Minimap.PinType` registered right after the vanilla enum (`Minimap.m_icons` + `Minimap.m_visibleIconTypes`), so they do not clash with - or toggle - the vanilla pin categories in the map legend.
  * The marker is the game's own portal map icon, located in `Minimap.m_icons` by sprite name (a sprite whose `Sprite.name` contains `portal`) exactly how the standalone mod found it, tinted with that mod's bright blue (`0.4, 0.8, 1`). The standalone mod's generated white ring is kept as the fallback when the icon list has no portal sprite (the "no portal icon found" case logs the icon names at debug level). The game's wood-toned portal *build* icon is not used - a tint only darkened it.
  * `Minimap.UpdatePins` re-tints every marker itself (white when the pin has no owner), so `Patches/Minimap.cs` adds a postfix that re-applies the colour after each pass - the standalone mod needed the very same patch.
* **Pins only for portals you are allowed to use (`PortalMapPins.IsVisibleToLocalPlayer`)**
  * A portal is pinned when it sits on the Global network, on a tribe network you are a member of (or may bypass as an admin - controlled by `AdminsSeeAllNetworks`), on a personal network (public portals there are usable by anyone), or when it is your own private portal.
  * Portals on someone else's private/personal network and on restricted tribe networks are never pinned. This mirrors the filtering the destination dropdown applies, and it is evaluated locally against the same synchronized network definitions the UI uses. The standalone mod's `IncludePrivatePortals` option is gone: permission now decides.
* **Two new `[Local Config]` settings (`XPortalNetworksConfig.cs`)**
  * `Show Portal Map Pins` (default on) - turns the pins off again; disabling removes only this mod's pins.
  * `Show Network In Pin Name` (default off) - prefixes a portal's pin with its network name, e.g. `[Trade Hub] North Base`.
  * Both are client-local preferences (not synchronized); they are listed after the other `[Local Config]` toggles.
  * A server that asks to be played without a map (`PingMapDisabled`) also gets no portal pins, so the no-map intent is respected.
* **Pins stay up to date (`PortalMapPins.Reconcile`)**
  * Pins are reconciled every 5 seconds, and immediately whenever the portal list changes (`KnownPortalsManager.AddOrUpdate` / `Remove` / `Reset`) or a setting changes (network list, `PingMapDisabled`, the pin toggles, admin status). Renamed or moved portals get their pin recreated, destroyed portals lose theirs, and pins the player deletes on the map are restored.
  * The minimap instance is tracked, so loading another world re-registers the pin type and rebuilds the pins instead of leaking stale ones.
* **Build tooling (`tools/Build.ps1`)**
  * A Release build now writes **two** artifacts: the existing `Release/XPortalNetworks-release.zip` and a copy named after the version being built (`Release/XPortalNetworks-<version>.zip`, e.g. `XPortalNetworks-2.6.0.zip`), so a release can be pinned to an exact build while the unversioned name stays the stable "latest" download.
  * `tools/README.md` documents both outputs (the zip is copied straight after `Compress-Archive` creates it, so the two files are always identical).
* **Docs**: `Docs/Modules/25Configuration.t4`, `Docs/Modules/20Features.t4` and the hand-maintained READMEs (mirrored into the tracked generated `README.Nexus.bbcode` and package README) document the new settings and the map-pin feature, and call out that the standalone XPortal Shared Map Pins mod has to be removed to avoid duplicate pins. buldosik is credited in the READMEs' *Credits & Acknowledgements* section, linked to the standalone mod's [Thunderstore page](https://thunderstore.io/c/valheim/p/buldosik/XPortalSharedMapPins/).
* **References (`REFERENCES.md`)**
  * Added the [XPortal Shared Map Pins](https://github.com/buldosik/valheim-mods/tree/master/XPortalSharedMapPins) source - the origin of the map-pin feature - to the referenced-mods table and to the attribution section (the upstream repository declares **no licence**, so it is credited explicitly).
  * Recorded that the BepInEx Valheim uses - and the one this mod compiles and ships against - is **not** built from upstream `BepInEx/BepInEx` (EOL 5.x), but from the maintenance branch **`v5-lts`** at <https://github.com/AzumattDev/BepInEx>: the pack's `5.4.2350` is assembly `5.4.23.5`, and our `BepInEx.dll` carries that fork's commit `ef506e0a` (*"Bump ThunderStore version to 5.4.2350"*), which does not exist upstream. The note also records the fork's `HarmonyX` 2.9.0 pin.
  * Documented the source-availability route for this work (shell `api.github.com` / `raw.githubusercontent.com`, since the local fetch server refuses GitHub `tree`/`blob` pages) and the Mono.Cecil-verified `Minimap` map-pin API facts.
* **Terminology**: shared portal networks are now called **tribe** networks (was: team networks) throughout the code comments and documentation - a wording change only, with no behaviour, config-key or localization change. Entries for already-released versions below keep their original wording.

# 2.5.0 - Portal Network Config Sections
* **Per-Network Config Sections (`XPortalNetworksConfig.cs`)**
  * Networks are now bound as `[Portal Network <n>]` sections with `Name` and `Permitted` keys (was: one `[Portal Networks]` section with `Network <n> Name` / `Network <n> Allow List`). Sections are bound 1-15 so the UI lists them numerically; within a section `Name` carries the higher `ConfigurationManagerAttributes.Order` (2 vs 1) because ConfigurationManager sorts by Order **descending** (`ConfigurationManager.cs`: `OrderByDescending(set => set.Order).ThenBy(set => set.DispName)`), which is also why the previous layout showed "Allow List" above "Name" (both Order 0, so the alphabetical display-name tie-break decided it).
  * Added `MigrateLegacyNetworkSections()`: on load, values left in the old `[Portal Networks]` section are read straight from the config file (`ConfigFile.OrphanedEntries` is not accessible in BepInEx 5) and copied into the new sections where those are still empty. The old keys are deliberately left in the file as an inert section - they are no longer bound, so they do not appear in the ConfigurationManager UI.
  * `CustomNetworks` is unchanged apart from documentation/log wording: it reads the same `GetNetworkName()` / `GetNetworkAllowList()` accessors.
* **Local Config Ordering (`XPortalNetworksConfig.cs`)**
  * `Show Splash on Startup` and `Enable Anonymous Telemetry` used `Order` 4 and 5, so ConfigurationManager (which sorts descending) listed telemetry first; the values are swapped (`Order = 5` for splash, `4` for telemetry) so the splash toggle is listed first.
* **Docs**: `25Configuration.t4`, the generated Nexus/package READMEs and the README configuration lists now document the per-network layout.

# 2.4.0 - Portal Networks in the Server Config
* **Config-Owned Networks (`XPortalNetworksConfig.cs`, `CustomNetworks.cs`)**
  * Portal networks are now defined by the `Portal Networks` config section: `Network <n> Name` and `Network <n> Allow List` (ids 1-15, empty name = unused slot, empty list = open to everyone). Both are tagged `ConfigurationManagerAttributes.IsAdminOnly`, so ServerSync distributes them and only server admins (or the host) can change them.
  * `RebuildFromConfig()` replaces the JSON load path and runs on every `SettingChanged`; `ResetSession()` now rebuilds instead of clearing, so the list survives a session reset.
* **Legacy Import (`CustomNetworks.cs`)**
  * `InitializeServer()` imports a pre-2.4.0 `BepInEx/config/XPortalNetworks/xportal_networks.json` into the config when no network is defined yet (re-using the old parser, now reachable only from `ImportLegacyJsonIfNeeded()`), then logs that the file is obsolete.
* **Removed RPC (`RPC/RPCManager.cs`, `RPC/ClientEvents.cs`, `RPC/ServerEvents.cs`, `RPC/SendToClient.cs`, `RPC/SendToServer.cs`)**
  * Dropped `RPC_CustomNetworks` / `RPC_RequestCustomNetworks`, the queued re-send helpers and `SendToClient.CustomNetworks` / `SendToServer.RequestCustomNetworks`; `CustomNetworks.PackForClient` / `ApplyFromServer` / `BroadcastToAllPeers` are gone too.
* **Hot-Reload Machinery Removed (`CustomNetworks.cs`, `XPortalNetworks.cs`)**
  * The `FileSystemWatcher`, `ServerTick()` polling, debounce state, `EnsureDefaultConfigExists` and the embedded JSON template were all deleted; `xportal_networks.json` is no longer an embedded resource in `XPortalNetworks.csproj`, and `tools/Build.ps1` no longer validates it.
* **Docs**: The README configuration sections describe the in-game workflow, and the setting lists are complete again - `Docs/Modules/25Configuration.t4` (plus the generated Nexus and package READMEs) now document `DefaultPrivatePortal`, `RestrictPortalRemoval`, `AdminsSeeAllNetworks`, the `Portal Networks` entries and the two `[Local Config]` toggles, and the README settings table gained the matching rows.
* **Doc Templates (`Docs/Modules/*.t4`, `Docs/Docs.csproj`)**
  * Removed the hard-coded self-references that had been by-passing the assembly-derived variables: `10Header.t4` / `11HeaderGitHub.t4` / `20Features.t4` / `25Configuration.t4` / `90InstallationDev.t4` now use `thisModName` (and `thisModGitHubRepo` for the banner image) instead of the literal `XPortal` and the original `SpikeHimself/XPortal` image URL, so a regeneration no longer re-introduces the pre-rename branding. Links that intentionally point at the original mod (`00Urls.t4`) and the historical changelog entries (`52Changelogs-previous.t4`) were left as-is.
  * Removed `Docs/SolutionDir/README.tt`: `Docs/SolutionDir/README.md` is a hybrid of generated and hand-written sections (the configuration and installation sections exist in no template), so regenerating it would have deleted hand-authored content. The file is now explicitly hand-maintained, and `tools/README.md` documents which files are generated and how to regenerate them.

# 2.3.2 - Offline-Capable Research Tooling
* **Local Web Access (`/.vscode/mcp.json`)**
  * Added a locally-hosted MCP fetch server (`docker run -i --rm mcp/fetch`, MCP `2024-11-05` / `mcp-fetch` 1.23.0). MCP servers run locally, so agent web retrieval no longer depends on GitHub-hosted tools (which are gated by a Copilot entitlement and were refusing every request during this work).
  * Verified end-to-end by fetching the previously-failing `https://github.com/BepInEx/BepInEx.ConfigurationManager`.
  * Added a second locally-hosted server, `mcp/brave-search` (keyword web search); the API key is supplied through VS Code's secure `${input:...}` prompt (`password: true`) instead of being written into the repo.
* **Documentation (`REFERENCES.md`)**
  * Section 7 now distinguishes the GitHub-tool outage from the restored local MCP path, so the source provenance record stays accurate.
  * Section 2 now records the verified ConfigurationManager contract: it resolves an attributes class **by type name** (`SettingEntryBase.cs`) and copies only same-named fields, its own class is `internal sealed` with no admin concept, and `IsAdminOnly`/`IsUnlocked` belong to the sync library (Jötunn). It also corrects the earlier claim that non-admin players see server-owned settings locked in the ConfigurationManager window - they do not; enforcement is via ServerSync.
* **No mod changes**: version bump only (tooling + docs = PATCH per `.github/copilot-instructions.md`); `ModInfo.cs`, `manifest.json` and `Docs/SolutionDir/Package/Release/manifest.json` kept in sync.

# 2.3.1 - Reference Documentation
* **New Source Inventory (`REFERENCES.md`)**
  * Documents every external source used for this mod: game assemblies and exact versions (Valheim 1.0.16 / Unity 6000.0.75f1), modding framework and libraries (BepInEx 5.4.2350, HarmonyX 2.9.0, Jötunn 2.30.2, Vapok.Valheim.Common 3.21.1015, BepInEx ConfigurationManager contract), build/analysis tooling (AssemblyPublicizer, ILRepack 2.0.44.1, Mono.Cecil, nuget.exe, .NET Framework reference assemblies), the offline XML-doc/NuGet/IL sources used for verification, in-repo prior art, referenced third-party mods, and attribution/licensing notes.
  * Records which facts came from which source, including the `AdminOnlyStrictness` semantics quoted from `Jotunn.xml` and the decompilation-verified ServerSync data path.
* **No code changes**: version bump only (documentation is a PATCH per `.github/copilot-instructions.md`); `ModInfo.cs`, `manifest.json` and `Docs/SolutionDir/Package/Release/manifest.json` kept in sync.

# 2.3.0 - Server-Owned Config via Jotunn ServerSync
* **ServerSync Opt-In (`XPortalNetworks.cs`)**
  * Added `[SynchronizationMode(AdminOnlyStrictness.Always)]` to the plugin, which registers its config file with Jotunn's `SynchronizationManager` (ServerSync).
* **Server-Owned Entries (`XPortalNetworksConfig.cs`)**
  * `PingMapDisabled`, `DoublePortalCosts`, `HidePortalDistance`, `RestrictPortalRemoval` and `AdminsSeeAllNetworks` now carry `ConfigurationManagerAttributes.IsAdminOnly`, so ServerSync pushes the server's values into every client's config file, unlocks the entries for server admins/host in the ConfigurationManager window and locks them for everyone else.
  * Removed the `Server` settings mirror and `TrackServerConfig()`: synced entries hold the server's values in the local config, so the cached settings are read from `Local` (`CustomNetworks.cs`, `Patches/Piece.cs`, `UI/PortalConfigurationPanel.cs`, `XPortalNetworks.cs`).
  * Removed `PackLocalConfig()`/`ReceiveServerConfig()` and the now unused `System.IO`/`XPortalNetworks.RPC` usings.
* **Retired Config RPC (`RPC/RPCManager.cs`, `RPC/ClientEvents.cs`, `RPC/ServerEvents.cs`, `RPC/SendToClient.cs`, `RPC/SendToServer.cs`)**
  * Dropped `RPC_Config`/`RPC_ConfigRequest` and the `SendToClient.Config`/`SendToServer.ConfigRequest` helpers (server-to-client only). `LocalConfigChanged` on the server still re-broadcasts the per-client portal network lists, so `AdminsSeeAllNetworks` changes still take effect immediately.
* **Docs**
  * Configuration sections now describe the server-owned settings as synchronized and admin-editable instead of "enforced (but not overwritten) by the server".

# 2.2.1 - Admin Bypass Live Toggle Fix
* **Server Config Aliasing (`XPortalNetworksConfig.cs`, `XPortalNetworks.cs`)**
  * Re-assert `Server = Local` whenever the config reloads and at server session start (`TrackServerConfig`), so server-enforced settings (incl. `AdminsSeeAllNetworks`) are read correctly even though the plugin loads before `ZNet` exists.
* **Live Network Re-Push (`XPortalNetworksConfig.cs`)**
  * On a server config change, re-broadcast each client's permitted network list and refresh the local UI, so toggling `AdminsSeeAllNetworks` takes effect immediately.

# 2.2.0 - Admin Network Bypass Option
* **New Config Option (`XPortalNetworksConfig.cs`)**
  * Added `AdminsSeeAllNetworks` (General section, default `false`, server-enforced). When disabled, server admins/host are treated like normal players for portal-network allow lists; when enabled, they can see and use every network.
* **Consistent Gating (`CustomNetworks.cs`, `XPortalNetworks.cs`, `RPC/ServerEvents.cs`)**
  * Per-client network push, client-side visibility (dropdowns + hover), portal interaction, teleport gating, and server-side portal edit/link validation now all honour the setting through a single `AdminsBypassNetworks` gate.

# 2.1.2 - Valheim 1.0.16 Alignment
* **Game References (`Directory.Build.props`, `XPortalNetworks/XPortalNetworks.csproj`, `tools/*`)**:
  * Re-publicized the Valheim 1.0.16 game assemblies and regenerated the reference layout.
  * Added `ValheimGameVersion` to `Directory.Build.props` as the single source of truth; `VALHEIM_INSTALL` and the reference `HintPath`s (now `$(VALHEIM_INSTALL)`/`$(BEPINEX_PATH)`) plus the `tools/` scripts all derive from it, so future game updates are a one-line change.
* **Build**: Verified the mod compiles against the 1.0.16 assemblies (no source changes required).

# 2.1.1 - Config Hot-Reload Resilience
* **Hot-Reload Poll (`CustomNetworks.cs`)**:
  * `DetectFileChange` now treats the config file disappearing as a change, so deleting `xportal_networks.json` while the server is running reliably re-seeds it from the embedded default and reloads (previously this depended on a `FileSystemWatcher` delete event; the polling fallback ignored the file being absent).
  * Added a warning log when the file is found missing and recreated.
  * `UpdateFileBaseline` now records the "missing" state explicitly, and the reload re-baselines after reading so the just-read file isn't flagged again on the next poll.

# 2.1.0 - Team Portal Networks
* **Team Networks via `allow_list` (`CustomNetworks.cs`, `PortalNetwork.cs`)**:
  * Network entries in `xportal_networks.json` now accept an optional `"allow_list"` of player ids (e.g. `Steam_12345678901234567`); the parser was rewritten to support the new object form (`id` / `name` / `allow_list`).
  * Only players on a network's allow list can see the network in the configuration UI, edit its portals, or step through them. An omitted or empty `allow_list` keeps the network open to everyone.
* **Server-Authoritative Network Policy (`CustomNetworks.cs`, `RPC/ServerEvents.cs`, `RPC/ClientEvents.cs`, `RPC/SendToClient.cs`, `NetPeerUtility.cs`)**:
  * The server now sends each client only the networks that client is permitted to use (id + name only); allow lists never leave the server.
  * Portal add/update requests are rejected when they assign a network, edit a restricted portal, or link to a portal on a team network the requester cannot access.
  * Added `NetPeerUtility` helpers to resolve a player's platform id (`Steam_...`) for allow-list matching.
* **Client Privacy (`XPortalNetworks.cs`, `UI/PortalConfigurationPanel.cs`)**:
  * Portals on a restricted network the player is not a member of are hidden from hover text and from the network/destination dropdowns, and show a localized "cannot access" message on interaction.
* **Config Hot-Reload Hardening (`CustomNetworks.cs`, `XPortalNetworks.cs`)**:
  * `xportal_networks.json` reloads now run on the game's main thread (via the frame update pump) instead of a background thread.
  * Added a file-timestamp polling fallback so edits are picked up even when `FileSystemWatcher` events are missed, with debounced coalescing of rapid saves.
* **Localization**:
  * Added `hud_xportal_network_restricted` to the shipped translations.

# 2.0.10 - Portal Connection Fix
* **Portal Reconnection & Target Resolution (`Patches/ZDOMan.cs`)**:
  * Resolved cross-session portal scrambling in `ZDOMan_ConnectPortals` by eliminating premature current-session ID collision check (`GetZDO(targetId)`).
  * Built an $O(1)$ dictionary lookup (`portalsByPreviousId`) to resolve previous session target IDs directly against each portal's loaded `Key_PreviousId`.
  * Expanded portal enumeration to `ZDOMan.instance.GetPortalList()` supplemented with `ZDOExtraData` connection IDs to ensure all loaded portals are captured.
  * Ensured unresolvable targets are safely cleared (`ZDOID.None`) rather than attaching to mismatched runtime entities.

# 2.0.9 - Dedicated Server UI Patch Hardening & Dependency Updates
* **Dedicated Server Isolation (`Environment.cs`, `Patches/Patcher.cs`, `Patches/Dropdown.cs`)**:
  * Switched headless detection to `Jotunn.Managers.GUIManager.IsHeadless()` directly, removing `SystemInfo.graphicsDeviceType` in compliance with repository invariants.
  * Added early returns in `Dropdown_*` patches and guarded UI patch registrations in `Patcher.Patch()` when running on headless servers.
* **Portal Reconnection & Identity (`Patches/ZDOMan.cs`, `KnownPortal.cs`, `KnownPortalsManager.cs`)**:
  * Fixed ZDOID type mismatch in `ZDOMan_ConnectPortals` where `Key_PreviousId` was checked via `GetString()` instead of `GetZDOID()`, avoiding spurious fallback lookup on session load.
  * Implemented `IEquatable<KnownPortal>`, `Equals`, and `GetHashCode` based on `ZDOID` on `KnownPortal`.
  * Updated `KnownPortalsManager.UpdateFromList` to reconcile using `HashSet<ZDOID>`, eliminating object reference mismatch during network resync.
* **Placement & State Hardening (`Patches/Piece.cs`, `Patches/WearNTear.cs`, `Patches/Player.cs`)**:
  * Eliminated static `m_WearNTear` field in `Piece_SetCreator`, scoped check strictly to portal pieces, and passed instance via `QueuedAction` state.
  * Added null safety guards on `Piece`, `piece.m_name`, and `ZNetView` in `WearNTear_OnPlaced.Postfix`, resolving `XPORTALNETWORKS-9`.
  * Removed dead `Patches/Player.cs` stub.
* **RPC & Server Hardening (`RPC/ServerEvents.cs`, `RPC/XPortalNetworksAdminSync.cs`, `NetPeerUtility.cs`, `RPC/RPCManager.cs`)**:
  * Guarded `peer.m_socket != null` before `GetHostName()` in `RPC_RequestAdminSync` and `NetPeerUtility.IsPeerPrivilegedForPortalNetwork`.
  * Protected `UserInfo.GetLocalUser()` with try-catch in `XPortalNetworksAdminSync.IsLocalPortalNetworkAdmin()`.
  * Guarded `ZRoutedRpc.instance == null` in `RPCManager.Register()`.
* **Unity Lifecycle & Code Hygiene (`UI/PortalConfigurationPanel.cs`, `XPortalNetworks.cs`)**:
  * Replaced `?.` on Unity objects (`Dropdown`, `ScrollRect`, `Component`, `GameObject`) with explicit `!= null` checks adhering to Unity lifecycle semantics.
  * Removed legacy XML summary blocks across codebase.
* **Ecosystem Compatibility**:
  * Noted that an issue in [ValheimCommunityPatch](https://thunderstore.io/c/valheim/p/MidnightMods/ValheimCommunityPatch/) prevented portal network connections; resolved in ValheimCommunityPatch 0.29.0.
* **Dependency Updates**:
  * Updated internalized `Vapok.Valheim.Common` to 3.19.1015.
  * Updated `JotunnLib` dependency to 2.30.2.

# 2.0.8 - Valheim 1.0.15 Alignment & Internalized Dependency Updates
* **Valheim 1.0.15 Alignment**:
  * Aligned publicized game assembly and UnityEngine references to Valheim 1.0.15.
  * Updated internalized `Vapok.Valheim.Common` dependency to 3.13.1015.
* **Transpiler & Patch Hardening**:
  * Added index bounds validation (`i + 2 < instrs.Count`) and null-safe operand equality checks (`Equals(instrs[i+2].operand, mTargetFound)`) in `TeleportWorld_UpdatePortal_Transpiler`.
  * Added safety guards against unresolvable target members (`m_target_found` and `IsUsablePortal`) to prevent Harmony `ArgumentException` during patch initialization.
* **Stability & Localization**:
  * Synchronized all 35 game localizations for splash screen and configuration registry.
  * Audited network RPCs, ZDO portal mappings, and headless UI isolation against game version 1.0.15.

# 2.0.7 - Scene Transition & Portal Target Exception Hardening
* **Scene Transition Exception Resolution**:
  * Fixed `ArgumentException: The scene is invalid` thrown by `Environment.IsHeadless` when queried during active scene loading and logout transitions.
  * Cached headless state in `Environment.IsHeadless` and implemented a protected fallback to `SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null`.
  * Updated `PortalConfigurationPanel.InitialiseUI()` to reference the cached `Environment.IsHeadless` property rather than querying `GUIManager.IsHeadless()` directly.
* **Portal Lookup Null-Safety & Dictionary Resilience**:
  * Replaced unsafe dictionary indexer (`knownPortals[id]`) in `KnownPortalsManager.GetKnownPortalById(ZDOID id)` with `knownPortals.TryGetValue(id, out var portal) ? portal : null` to avoid `KeyNotFoundException`.
  * Added null guards across all callers (`KnownPortal.GetFriendlyTargetName()`, `XPortalNetworks.OnPrePortalHover()`, `XPortalNetworks.OnPortalRequestText()`, `XPortalNetworks.OnPortalDestroyed()`, `ServerEvents.RPC_AddOrUpdateRequest()`, and `PortalConfigurationPanel.ResolveInitialDestinationNetworkOwnerId()`).
* **Map Ping Hardening**:
  * Guarded `SendToClient.PingMap()` against null `ZRoutedRpc.instance` and null `UserInfo.GetLocalUser()` instances.
  * Added exception handling and fallback name string assignment to prevent UI cancellation during map ping requests.

# 2.0.6 - Splash Window Updates & Valheim 1.0.14 Alignment
* **Splash Window Updates**:
  * Updated telemetry default to unchecked on first launch (Opt-In).
  * Added Send Error Logs toggle (Opt-Out) to capture anonymous crash diagnostics and error reports.
  * Added in-game scrollable Privacy Policy overlay with responsive mouse wheel support.
  * Added interactive tooltip data disclaimers on checkbox hover.
* **Valheim 1.0.14 Alignment**:
  * Aligned publicized game assembly and UnityEngine references to Valheim 1.0.14.
  * Updated internalized Vapok.Valheim.Common dependency to 3.12.1014.

# 2.0.5 - Jewelcrafting Font Compatibility
* **Compatibility Fix**: Fixed issue where Jewelcrafting packages its own font which was overriding part of a vanilla font, causing the Splash screen to appear blank.
* **Vapok.Common Dependency Bump**: Updated internalized dependency to `Vapok.Valheim.Common` 3.11.1012.

# 2.0.4 - Updated README with Telemetry Information
* **Documentation Update**: Updated the README.md with Anonymous Telemetry and Privacy section per request of mod stores.
* **Vapok.Common Dependency Bump**: Updated internalized dependency to `Vapok.Valheim.Common` 3.9.1012.

# 2.0.3 - Unified Splash Screen & Telemetry Controls
* **Unified Startup Splash Screen & Telemetry**:
  * Updated `Vapok.Valheim.Common` dependency reference to `v3.5.1012`.
  * Registered mod metadata with centralized `ModSplashManager`.
  * Added `ShowSplashOnStartup` and `Enable Anonymous Telemetry` configuration bindings to `ConfigRegistry`.

# 2.0.1 - Dependency & Compatibility Maintenance
* **Runtime & Dependency Updates**:
  * Synchronized package manifest and project references with Jotunn `2.30.0` and BepInEx `5.4.2350`.
  * Verified build pipeline and ILRepack bundling with `Vapok.Valheim.Common` `3.2.1012`.
* **Compatibility & Documentation**:
  * Validated portal destination selection UI and network configuration hot-reloading against current Valheim 1.0 builds.
  * Standardized mod documentation, changelog tiers, and release staging.

# 2.0.0 - Portal Networks & Valheim 1.0+ Overhaul
* **Portal Networks Architecture**:
  * Overhauled portal mechanics to introduce an expansive multi-tier **Portal Networks** system:
    * **Global / Public Network**: Accessible to all players on the server without restriction.
    * **Player Networks & Private Portals**: Dedicated per-player network channels with private portal protection to restrict unauthorized access.
    * **Custom Named Networks**: Dynamic support for up to 15 server-defined custom networks configured in `xportal_networks.json` with live hot-reloading support.
* **Server Administration & Permission Controls**:
  * Implemented permission checks restricting portal deconstruction and destruction to the original creator or authenticated server admins.
  * Synchronized portal network configurations and permission sets strictly across dedicated servers via Jotunn ServerSync.
* **Valheim 1.0 Compatibility & Core Updates**:
  * Updated assembly references for Valheim 1.0 (`1.0.12`), BepInEx 5.4.2350, and Jotunn 2.30.0.
  * Rebuilt on .NET Framework 4.8.
  * Bundled `Vapok.Valheim.Common` 3.2.1012 via ILRepack.
* **UI, Gamepad & Networking Fixes**:
  * Resolved controller legend rendering artifacts and gamepad input focus issues in portal configuration dialogs.
  * Fixed dedicated server admin portal destruction permission validation.
  * Improved ZDO network key synchronization and portal pairing resolution to eliminate connection dropouts under high network load.

# 1.0.0 - Initial Portal Management Release
* Initial release of portal grouping and tag management mechanics.
