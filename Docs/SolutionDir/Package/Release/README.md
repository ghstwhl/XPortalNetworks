
# XPortal Networks Tribes Pins

An [AnyPortal](https://valheim.thunderstore.io/package/sweetgiorni/AnyPortal/) revamp.

<img src="https://raw.githubusercontent.com/ghstwhl/XPortalNetworksTribesPins/main/images/controller.gif" height="180" />


# Description

XPortal Networks Tribes Pins lets you select a portal destination from a list of existing portals. 

No more tag pairing, and no more portal hubs!


# Features

#### Select a destination

When interacting with a portal, instead of entering a tag which has to match another portal's, XPortal Networks Tribes Pins lets you choose the portal's destination from a list.
For your convenience, this list also shows you how far away the portals are.

#### Default destination

A Portal can be marked as the "Default Portal". When a Default Portal has been set, all newly built portals will immediately connect with that portal, without you having to go into the portal configuration panel.

#### Longer names

XPortal Networks Tribes Pins completely removes the character length restriction on portal names, so that you can give your portals clear and descriptive titles.

#### Ping a portal location

Forgot where you put your portal? You don't need to teleport to it to find out. Just click the Ping button next to the list, and XPortal Networks Tribes Pins will show the selected portal on your map, while also pinging its location to all players on the server.

If you prefer to play without a map, this button can be hidden, either by using the `nomap` global key, or by setting `PingMapDisabled` to `True`.

#### Portals on your map

XPortal Networks Tribes Pins can pin every portal you are allowed to use onto your own map - this is the feature the standalone [XPortal Shared Map Pins](https://github.com/buldosik/valheim-mods/tree/master/XPortalSharedMapPins) mod inspired, built right in as this mod's own re-implementation (an inspiration only - no code from that project is used here). Portals on the Global network, portals on tribe networks you are a member of, and your own private portals are pinned automatically; other players' private portals and restricted networks are not, so the map never reveals portals you cannot use.

The pins are local to your client - nothing is written to the world and nothing is sent to other players - and they follow renamed, moved and destroyed portals. Use `Show Portal Map Pins` to turn them off, and `Show Network In Pin Name` to prefix each pin with the network it belongs to (for example "[Trade Hub] North Base").

#### Multiplayer

XPortal Networks Tribes Pins has been built with multiplayer support at its core. All players must run the same version of XPortal Networks Tribes Pins. If you play on a dedicated server, that too needs to have same version of XPortal Networks Tribes Pins installed.

#### Gamepad support

The XPortal Networks Tribes Pins UI will respond to gamepad input when configuring your portal. As of v1.2.10 it even shows you the gamepad keyhints!

<img src="https://raw.githubusercontent.com/SpikeHimself/XPortal/main/images/ui-keyhints-small.png" />

The controls are as follows:

* `A` / `Cross` - Submit (i.e. press the OK button)
* `B`/ `Circle` - Cancel
* `Y` / `Triangle` - Ping the selected portal
* `X` / `Square` - Show/hide the contents of the dropdown
* `D-Pad Up` / `D-Pad Down` - Select the previous / next item in the dropdown


#### Mod compatibility and integration

XPortal Networks Tribes Pins has been made fully compatible with the following mods:

* [Nexus Update Check](https://valheim.thunderstore.io/package/nexusreupload/aedenthorn_Nexus_Update_Check/) by aedenthorn
* [VHVR - Valheim VR](https://valheim.thunderstore.io/package/Maynard/VHVR/) by Flatscreen to VR Modders
* [Stone Portal](https://valheim.thunderstore.io/package/JereKuusela/Stone_Portal/) by Jere Kuusela
* [Advanced Portals](https://valheim.thunderstore.io/package/RandyKnapp/AdvancedPortals/) by Randy Knapp
* [XPortal Shared Map Pins](https://github.com/buldosik/valheim-mods/tree/master/XPortalSharedMapPins) by buldosik (built in since v2.6.0 as an inspiration only - no code from that project is used here; remove the standalone mod to avoid duplicate map pins)

Furthermore, XPortal Networks Tribes Pins has a configuration option to fully integrate with [Advanced Portals](https://valheim.thunderstore.io/package/RandyKnapp/AdvancedPortals/). If you set `DisplayPortalColour` to `True`, each portal in XPortal Networks Tribes Pins's dropdown list will be prepended by a ">>" tag that has the same colour as the light that the portal emits. As of v1.2.10, Stone Portals also get their own colour!

<img src="https://raw.githubusercontent.com/SpikeHimself/XPortal/main/images/advancedportals-small.png" />

XPortal Networks Tribes Pins is known to be fully incompatible with:

* [AnyPortal](https://valheim.thunderstore.io/package/sweetgiorni/AnyPortal/) by sweetgiorni
* [ComfyGizmo](https://github.com/redseiko/ValheimMods/releases) by redseiko
* [Custom Meshes](https://www.nexusmods.com/valheim/mods/184) by aedenthorn


# Configuration

XPortal Networks Tribes Pins's config file, which can be found at `Valheim\BepInEx\config\ghostwheel.mods.xportalnetworkstribespins.cfg`, contains the following settings:

`PingMapDisabled`

Disable the Ping Map button completely. For players who wish to play without a map. This setting is owned by the server: it is synchronized to all clients and can only be changed by server admins.

`DisplayPortalColour`

Show a coloured ">>" tag in the list of portals to indicate the portal type (integration with [Advanced Portals](https://valheim.thunderstore.io/package/RandyKnapp/AdvancedPortals/) and [Stone Portal](https://valheim.thunderstore.io/package/JereKuusela/Stone_Portal/)).

`DoublePortalCosts`

Since XPortal Networks Tribes Pins is essentially a cheat, in that you only need half the amount of portals now, this setting allows you to compensate for that by doubling portal costs. This setting is owned by the server: it is synchronized to all clients and can only be changed by server admins.

`HidePortalDistance`

If you don't want to see how far away the portals in the list are, you can use this option to remove that. This setting is owned by the server: it is synchronized to all clients and can only be changed by server admins.

`DefaultPortal`

This configuration option exists to save your personal Default Portal. Its value will be set by checking the Default Portal checkbox on the portal configuration panel. This value should not be manually edited in the file.

`DefaultPrivatePortal`

If true, newly placed portals start as private (owner-only). If false, they start public on the Global network until changed.

`RestrictPortalRemovalToCreator`

Restricts removing a portal with the hammer to the player who placed it - or to a server admin while `AdminsSeeAllNetworks` lets admins bypass portal networks. Enabled by default. When `RestrictPortalRemovalToUsable` is also enabled, both rules have to be satisfied. Other removal (such as structural damage) is unaffected. This setting is owned by the server: it is synchronized to all clients and can only be changed by server admins.

`RestrictPortalRemovalToUsable`

When enabled, a player may only remove a portal with the hammer if they are allowed to use it: portals on the Global network, portals on unrestricted networks, portals on networks they are a member of, and their own private portals. Admins and the host are treated like normal players here unless `AdminsSeeAllNetworks` lets them bypass portal networks. Enabled by default. When `RestrictPortalRemovalToCreator` is also enabled, both rules have to be satisfied - a player may then only remove a portal they placed and may still use. Other removal (such as structural damage) is unaffected. This setting is owned by the server: it is synchronized to all clients and can only be changed by server admins.

`AdminsSeeAllNetworks`

When disabled (the default), server admins and the host are treated like normal players and only see and use unrestricted portal networks, or networks they are members of. When enabled, they can see and use every network. This setting is owned by the server: it is synchronized to all clients and can only be changed by server admins.

`Portal Network 1`

Each network (ids 1-15) has its own section with two settings: `Name` and `Permitted`. Leave the name empty to keep that slot unused, and leave `Permitted` empty to let everyone use the network. `Permitted` takes comma separated player ids, such as `Steam_12345678901234567`. These settings are owned by the server: they are synchronized to all clients and can only be changed by server admins - which means networks can be added, renamed and restricted from inside the game, through the ConfigurationManager window.

`Show Portal Map Pins`

If enabled (the default), the portals you are allowed to use are shown as pins on your own map. Only portals on networks you can access, plus your own private portals, are pinned - other players' private portals never are. This is a local preference, and no portal pins are shown at all while the server has `PingMapDisabled` enabled.

`Show Network In Pin Name`

If enabled, the map pin of a portal is prefixed with the name of the portal network it is on, for example "`[Trade Hub] North Base`". This is a local preference.


# Installation instructions

XPortal Networks Tribes Pins is a [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) plugin. As such, you must have BepInEx installed. Most other Valheim mods are also BepInEx plugins, so chances are you already have this.

XPortal Networks Tribes Pins makes use of the [Jotunn](https://valheim.thunderstore.io/package/ValheimModding/Jotunn/) library, so you must install that before installing XPortal Networks Tribes Pins. If you do not install Jotunn, XPortal Networks Tribes Pins will simply not be loaded by your game and it will not work.

I very strongly recommend using a mod manager such as [Vortex](https://www.nexusmods.com/site/mods/1) or [r2modman](https://valheim.thunderstore.io/package/ebkr/r2modman/). They will take care of everything for you and you don't have to worry about which files go where. I recommend against manual installation.
1. Make sure you have [BepInEx](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) installed.
2. Install [Jotunn](https://valheim.thunderstore.io/package/ValheimModding/Jotunn/).
3. On [Thunderstore](https://thunderstore.io/c/valheim/p/NorCal_Nerds/XPortalNetworksTribesPins/) click 'Install with Mod Manager', or download the latest release from the [GitHub releases page](https://github.com/ghstwhl/XPortalNetworksTribesPins/releases).


To install XPortal Networks Tribes Pins on a dedicated server, copy all of the contents of the `plugins\` directory found inside the .zip file download to the  `Valheim\BepInEx\plugins\` directory on your server. 


# Bugs, Feature Requests and Translations

First of all, before you report a bug, please make sure that the problem you are experiencing is actually caused by XPortal Networks Tribes Pins. If you are running other mods, disable those, and see if the problem goes away. Or the other way around: disable XPortal Networks Tribes Pins, and see if that makes the problem go away. If you discover that XPortal Networks Tribes Pins is incompatible with another mod, please do report that, because I might be able to create work-arounds for that. If you are not sure, or you are struggling with these steps, then just report the problem, and we'll go from there.

It is important to me that I can make XPortal Networks Tribes Pins as bug-free as possible, but **please bear in mind that without your `LogOutput.log`, I will not be able to debug your issue at all**. Just showing me a screenshot of an error is not enough for me to discover the cause of that error.

To report a bug, please navigate to the [Issues page](https://github.com/ghstwhl/XPortalNetworksTribesPins/issues), click [New issue](https://github.com/ghstwhl/XPortalNetworksTribesPins/issues/new/choose), choose `Bug report`, and fill out the template.

For feature requests, choose `Feature request` on the [New issue](https://github.com/ghstwhl/XPortalNetworksTribesPins/issues/new/choose) page.

To add a translation to XPortal Networks Tribes Pins, choose `Translation` when submitting a [New issue](https://github.com/ghstwhl/XPortalNetworksTribesPins/issues/new/choose).


# Credits

* sweetgiorni for creating AnyPortal
* kaiqueknup for translating to Brazillian Portuguese
* makou for translating to French, Spanish
* Smok3y97 for translating to German
* MexExe for translating to Polish, Russian
* hanawa07 for translating to Korean
* bonesbro for adding a colour for Stone Portals (#39)
* VasariRulez for translating to Italian
* Felix for translating to Chinese
* cawa-93 for translating to Ukrainian
* Yukimimiya for translating to Japanese


# I did more too!

Please have a look at my other mod too! [XStorage](https://valheim.thunderstore.io/package/SpikeHimself/XStorage/) lets you open multiple chests at once, rename them, and move items/stacks to the most suitable chest.
