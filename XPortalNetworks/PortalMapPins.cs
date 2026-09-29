using System;
using System.Collections.Generic;
using UnityEngine;

namespace XPortalNetworks
{
    /// <summary>
    /// Client-side map integration where every known portal the local player is allowed to use gets a pin on
    /// that player's own map.
    ///
    /// The idea comes from buldosik's standalone "XPortal Shared Map Pins" mod, which is credited as an
    /// inspiration only: the implementation here is this mod's own, written against the vanilla <c>Minimap</c>
    /// API, and it contains no code from that project.
    ///
    /// The pins are local map data - nothing is written to the world, nothing is sent over the network and
    /// vanilla player pins are untouched. The access rules are XPortalNetworks' own, so a player only ever
    /// sees portals they may actually use: Global network portals, portals on tribe networks they are a
    /// member of (or may bypass as an admin, see <c>AdminsSeeAllNetworks</c>) and their own private
    /// portals. Someone else's private/personal portals are never pinned.
    ///
    /// Pins are reconciled every <see cref="RefreshIntervalSeconds"/> seconds (and whenever the portal list
    /// or the config changes), so renamed, moved or destroyed portals lose or update their pin.
    /// </summary>
    internal static class PortalMapPins
    {
        /// <summary>How often the pins are reconciled with the known portal list.</summary>
        private const float RefreshIntervalSeconds = 5f;

        /// <summary>Our pins, keyed by portal id.</summary>
        private static readonly Dictionary<ZDOID, Minimap.PinData> managedPins = new Dictionary<ZDOID, Minimap.PinData>();

        /// <summary>The minimap the pins below belong to (a new world gets a new Minimap instance).</summary>
        private static Minimap installedMinimap;

        /// <summary>The extra <see cref="Minimap.PinType"/> registered for our pins.</summary>
        private static Minimap.PinType portalPinType;

        /// <summary>
        /// The sprite our pins use - the game's own portal map icon when the map icon list has one,
        /// otherwise a generated marker. Resolved once; it is a shared asset, not scene data.
        /// </summary>
        private static Sprite portalPinSprite;

        /// <summary>
        /// Lime green (<c>#32CD32</c>), this fork's own pin colour - the standalone "XPortal Shared Map Pins"
        /// mod that inspired these pins (an inspiration only - no code from that project is used here)
        /// tinted its own pins bright blue. It renders as-is because our marker is a neutral sprite;
        /// <see cref="ReapplyColours"/> re-applies it because <see cref="Minimap.UpdatePins"/> tints every
        /// marker itself (white for the player's own pins).
        /// </summary>
        private static readonly Color PortalPinColour = new Color(0.196f, 0.804f, 0.196f, 1f);

        private static bool pinTypeInstalled;
        private static bool refreshQueued = true;
        private static float nextRefreshTime;

        /// <summary>Asks for a pin refresh on the next frame. Cheap and safe to call from any event.</summary>
        internal static void MarkDirty()
        {
            refreshQueued = true;
        }

        /// <summary>
        /// Forgets everything and drops our pins from the current map. Called when a session starts or ends.
        /// </summary>
        internal static void Reset()
        {
            try
            {
                if (!Environment.IsHeadless && Minimap.instance != null)
                {
                    RemoveAllPins(Minimap.instance);
                }
            }
            catch (Exception ex)
            {
                Log.Debug($"Could not remove the portal map pins: {ex.Message}");
            }

            managedPins.Clear();
            installedMinimap = null;
            portalPinType = default;
            pinTypeInstalled = false;
            refreshQueued = true;
            nextRefreshTime = 0f;
        }

        /// <summary>Called every frame by the plugin; throttles the actual reconciliation.</summary>
        internal static void Tick()
        {
            if (Environment.IsHeadless)
            {
                return;
            }

            var minimap = Minimap.instance;
            if (minimap == null)
            {
                return;
            }

            var now = Time.unscaledTime;
            if (!refreshQueued && now < nextRefreshTime)
            {
                return;
            }

            refreshQueued = false;
            nextRefreshTime = now + RefreshIntervalSeconds;
            Refresh(minimap);
        }

        private static void Refresh(Minimap minimap)
        {
            try
            {
                // A new Minimap means a new world: the pins we tracked died with the old one.
                if (!ReferenceEquals(installedMinimap, minimap))
                {
                    managedPins.Clear();
                    installedMinimap = minimap;
                    pinTypeInstalled = false;
                    portalPinType = default;
                }

                if (!ShouldShowPins())
                {
                    RemoveAllPins(minimap);
                    return;
                }

                if (!EnsurePortalPinType(minimap))
                {
                    return;
                }

                Reconcile(minimap);
            }
            catch (Exception ex)
            {
                Log.Warning($"Could not update the portal map pins: {ex.GetType().Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Pins are a local preference, but a server that asks to be played without a map
        /// (<see cref="XPortalNetworksConfig.ConfigSettings.PingMapDisabled"/>) gets no portal pins either.
        /// </summary>
        private static bool ShouldShowPins()
        {
            var local = XPortalNetworksConfig.Instance.Local;
            return local.ShowPortalPins.Value && !local.PingMapDisabled;
        }

        private static void Reconcile(Minimap minimap)
        {
            var localPlayerId = NetPeerUtility.GetLocalPlayerId();

            var wanted = new Dictionary<ZDOID, string>();
            foreach (var portal in KnownPortalsManager.Instance.GetList())
            {
                if (!IsVisibleToLocalPlayer(portal, localPlayerId))
                {
                    continue;
                }

                wanted[portal.Id] = GetPinName(portal);
            }

            // Drop pins for portals that are gone, no longer allowed, renamed or moved.
            foreach (var id in new List<ZDOID>(managedPins.Keys))
            {
                if (!wanted.TryGetValue(id, out var pinName) || !IsStillOnMap(minimap, id))
                {
                    DropPin(minimap, id);
                    continue;
                }

                var portal = KnownPortalsManager.Instance.GetKnownPortalById(id);
                var pin = managedPins[id];
                if (portal == null
                    || pin == null
                    || pin.m_name != pinName
                    || pin.m_type != portalPinType
                    || pin.m_icon != portalPinSprite
                    || !IsAt(pin, portal.Location))
                {
                    DropPin(minimap, id);
                }
            }

            foreach (var desired in wanted)
            {
                if (managedPins.ContainsKey(desired.Key))
                {
                    continue;
                }

                var portal = KnownPortalsManager.Instance.GetKnownPortalById(desired.Key);
                if (portal == null)
                {
                    continue;
                }

                // save:false keeps the pin out of the player's map file; ownerID 0 keeps it opaque
                // (Minimap.UpdatePins fades the markers of shared map data, i.e. pins with an owner).
                var pin = minimap.AddPin(portal.Location, portalPinType, desired.Value, false, false);
                if (pin != null)
                {
                    ApplyPinAppearance(pin);
                    managedPins[desired.Key] = pin;
                }
            }
        }

        /// <summary>
        /// True when the local player may see and use the portal. This mirrors the filtering the
        /// destination dropdown in <see cref="UI.PortalConfigurationPanel"/> applies: portals on the
        /// Global network, on a tribe network the player is a member of, on a personal network (public
        /// portals there are usable by anyone) and the player's own private portals.
        /// </summary>
        private static bool IsVisibleToLocalPlayer(KnownPortal portal, long localPlayerId)
        {
            if (portal == null)
            {
                return false;
            }

            var networkId = portal.NetworkOwnerPlayerId;

            if (CustomNetworks.IsReservedIdRange(networkId))
            {
                // Tribe network: hidden from non-members (IsLocalPlayerAllowed gated the admin bypass).
                return !portal.IsPrivate && CustomNetworks.IsLocalPlayerAllowed(networkId);
            }

            if (portal.IsPrivate)
            {
                // Private portals are owner-only, and use the owner's player id as their network.
                return localPlayerId != 0L && networkId == localPlayerId;
            }

            return true;
        }

        private static string GetPinName(KnownPortal portal)
        {
            var name = portal.GetFriendlyName();

            if (!XPortalNetworksConfig.Instance.Local.ShowNetworkInPinName.Value
                || !CustomNetworks.IsReservedIdRange(portal.NetworkOwnerPlayerId)
                || !CustomNetworks.TryGetDisplayName(portal.NetworkOwnerPlayerId, out var networkName)
                || string.IsNullOrWhiteSpace(networkName))
            {
                return name;
            }

            return $"[{networkName}] {name}";
        }

        /// <summary>True while the game still knows the pin - the player can delete pins from the map.</summary>
        private static bool IsStillOnMap(Minimap minimap, ZDOID id)
        {
            if (!managedPins.TryGetValue(id, out var pin) || pin == null)
            {
                return false;
            }

            var pins = minimap.m_pins;
            return pins != null && pins.Contains(pin);
        }

        private static bool IsAt(Minimap.PinData pin, Vector3 location)
        {
            return (pin.m_pos - location).sqrMagnitude < 0.01f;
        }

        private static void DropPin(Minimap minimap, ZDOID id)
        {
            if (!managedPins.TryGetValue(id, out var pin))
            {
                return;
            }

            managedPins.Remove(id);

            var pins = minimap.m_pins;
            if (pin != null && pins != null && pins.Contains(pin))
            {
                minimap.RemovePin(pin);
            }
        }

        private static void RemoveAllPins(Minimap minimap)
        {
            foreach (var id in new List<ZDOID>(managedPins.Keys))
            {
                DropPin(minimap, id);
            }
        }

        /// <summary>
        /// Registers the extra <see cref="Minimap.PinType"/> our pins use, so the map draws them with the
        /// portal icon instead of clashing with the vanilla pin categories (and the player's legend
        /// toggles). Idempotent: it is re-checked because <see cref="Minimap.Start"/> rebuilds
        /// <see cref="Minimap.m_visibleIconTypes"/> from the enum alone.
        /// </summary>
        private static bool EnsurePortalPinType(Minimap minimap)
        {
            var icons = minimap.m_icons;
            if (icons == null)
            {
                return false;
            }

            if (!pinTypeInstalled || !icons.Exists(IsOurSpriteData))
            {
                var icon = ResolvePortalPinSprite(minimap);
                if (icon == null)
                {
                    return false;
                }

                // The vanilla enum has no portal pin type, so ours goes right after the last PinType.
                portalPinType = (Minimap.PinType)Enum.GetValues(typeof(Minimap.PinType)).Length;

                icons.RemoveAll(entry => entry.m_name == portalPinType);
                icons.Add(new Minimap.SpriteData { m_name = portalPinType, m_icon = icon });
                pinTypeInstalled = true;
            }

            EnsurePinTypeVisible(minimap);
            return true;
        }

        private static bool IsOurSpriteData(Minimap.SpriteData entry)
        {
            return entry.m_name == portalPinType && entry.m_icon == portalPinSprite;
        }

        /// <summary>
        /// Makes sure our pin type is inside <see cref="Minimap.m_visibleIconTypes"/> and enabled, since
        /// Minimap.AddPin falls back to Icon3 for out-of-range types.
        /// </summary>
        private static void EnsurePinTypeVisible(Minimap minimap)
        {
            var index = (int)portalPinType;
            var visible = minimap.m_visibleIconTypes;

            if (visible != null && visible.Length > index)
            {
                visible[index] = true;
                return;
            }

            var expanded = new bool[index + 1];
            if (visible != null)
            {
                Array.Copy(visible, expanded, visible.Length);
            }

            for (var i = visible != null ? visible.Length : 0; i < expanded.Length; i++)
            {
                expanded[i] = true;
            }

            minimap.m_visibleIconTypes = expanded;
        }

        /// <summary>
        /// Gives one of our pins the portal icon and colour. The colour has to be re-applied because
        /// <see cref="Minimap.UpdatePins"/> assigns the marker colour itself on every pass (white for pins
        /// without an owner).
        /// </summary>
        private static void ApplyPinAppearance(Minimap.PinData pin)
        {
            if (pin == null)
            {
                return;
            }

            // AddPin already picked our sprite out of m_icons; setting it again is harmless and covers the
            // case where that entry went missing.
            pin.m_icon = portalPinSprite;

            var iconElement = pin.m_iconElement;
            if (iconElement != null)
            {
                iconElement.sprite = portalPinSprite;
            }

            ApplyColour(pin);
        }

        /// <summary>
        /// Gives one of our pins the portal colour. Needed because <see cref="Minimap.UpdatePins"/> assigns
        /// the marker colour itself on every pass (white for pins without an owner).
        /// </summary>
        private static void ApplyColour(Minimap.PinData pin)
        {
            var iconElement = pin != null ? pin.m_iconElement : null;
            if (iconElement != null)
            {
                iconElement.color = PortalPinColour;
            }
        }

        /// <summary>
        /// Re-applies our colour to every pin we own. Called from the <see cref="Minimap.UpdatePins"/>
        /// postfix, right after the game has reset the markers to its own colour.
        /// </summary>
        internal static void ReapplyColours()
        {
            if (Environment.IsHeadless || managedPins.Count == 0)
            {
                return;
            }

            foreach (var pin in managedPins.Values)
            {
                ApplyColour(pin);
            }
        }

        /// <summary>
        /// Resolves the sprite our pins use, once per session: the game's own portal map icon when the map
        /// icon list has one - the standalone "XPortal Shared Map Pins" mod that inspired these pins (an
        /// inspiration only - no code from that project is used here) looked for a sprite whose name
        /// contains "portal" - otherwise a generated marker.
        /// </summary>
        private static Sprite ResolvePortalPinSprite(Minimap minimap)
        {
            if (portalPinSprite != null)
            {
                return portalPinSprite;
            }

            var icons = minimap.m_icons;
            var gameIcon = FindPortalIcon(icons);
            if (gameIcon != null)
            {
                Log.Debug($"Portal map pins use the game's `{gameIcon.name}` icon.");
                portalPinSprite = gameIcon;
                return portalPinSprite;
            }

            Log.Warning("No portal icon was found in the minimap icon list; the portal map pins fall back " +
                        "to a generated marker.");
            Log.Debug($"Minimap icon list: {DescribeIcons(icons)}");

            portalPinSprite = CreateFallbackPortalIcon();
            return portalPinSprite;
        }

        /// <summary>
        /// The map icon list's portal sprite, if it has one - the same rule the standalone mod that inspired
        /// these pins used (an inspiration only - no code from that project is used here).
        /// </summary>
        private static Sprite FindPortalIcon(List<Minimap.SpriteData> icons)
        {
            if (icons == null)
            {
                return null;
            }

            foreach (var entry in icons)
            {
                var icon = entry.m_icon;
                if (icon != null && icon.name != null
                    && icon.name.IndexOf("portal", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return icon;
                }
            }

            return null;
        }

        /// <summary>Diagnostic helper: names the map icons, for the "no portal icon found" debug message.</summary>
        private static string DescribeIcons(List<Minimap.SpriteData> icons)
        {
            if (icons == null)
            {
                return "the icon list is unavailable";
            }

            var names = new List<string>();
            foreach (var entry in icons)
            {
                names.Add(entry.m_icon != null && entry.m_icon.name != null ? entry.m_icon.name : "<unnamed>");
            }

            return $"{names.Count} icons: {string.Join(", ", names)}";
        }

        /// <summary>
        /// The generated portal marker: a thin white ring, so the pin colour can be applied as a tint. It is
        /// the same ring the standalone mod that inspired these pins generated, and is only used when the
        /// game's own portal icon cannot be found (the same fallback order that mod had) - an inspiration
        /// only: no code from that project is used here.
        ///
        /// The ring is drawn by walking the pixel buffer by index and measuring how far each pixel sits from
        /// the texture's centre, so the result is a pale circle whose coverage fades outwards over 0.9 pixels
        /// on either side of a 9.5 pixel radius.
        /// </summary>
        private static Sprite CreateFallbackPortalIcon()
        {
            const int dimension = 32;
            const float ringRadius = 9.5f;
            const float ringWidth = 0.9f;

            // The centre of the texture sits between its two middle pixels (31 / 2 = 15.5).
            var middle = new Vector2((dimension - 1) / 2f, (dimension - 1) / 2f);

            var texture = new Texture2D(dimension, dimension, TextureFormat.RGBA32, false);
            texture.name = "XPortalNetworks_PortalIcon";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            var pixels = new Color[dimension * dimension];
            for (var index = 0; index < pixels.Length; index++)
            {
                var offset = new Vector2(index % dimension, index / dimension) - middle;
                var edgeDistance = Mathf.Abs(offset.magnitude - ringRadius);
                pixels[index] = new Color(1f, 1f, 1f, Mathf.Clamp01((ringWidth - edgeDistance) * 4f));
            }

            texture.SetPixels(pixels);
            texture.Apply(true, false);
            return Sprite.Create(texture, new Rect(0f, 0f, dimension, dimension), new Vector2(0.5f, 0.5f),
                dimension);
        }
    }
}
