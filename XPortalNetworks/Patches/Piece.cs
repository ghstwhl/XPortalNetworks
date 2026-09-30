using HarmonyLib;
using XPortalNetworks.RPC;

namespace XPortalNetworks.Patches
{
    [HarmonyPatch(typeof(Piece), nameof(Piece.SetCreator))]
    static class Piece_SetCreator
    {
        static void Postfix(Piece __instance)
        {
            if (__instance == null || string.IsNullOrEmpty(__instance.m_name) || !__instance.m_name.Contains("$piece_portal"))
                return;

            WearNTear wearNTear = __instance.GetComponent<WearNTear>();
            if (wearNTear == null)
                return;

            CheckWearNTearCreationTime(true, wearNTear);
        }

        private static void CheckWearNTearCreationTime(bool delayed = true, object state = null)
        {
            if (state is not WearNTear wearNTear || wearNTear == null)
                return;

            if (delayed)
            {
                QueuedAction.Queue(CheckWearNTearCreationTime, delay: 1, state: wearNTear);
                return;
            }

            if (wearNTear.m_createTime == -1f)
            {
                Log.Debug("Portal detection work-around: manually invoking WearNTear.OnPlace postfix");
                WearNTear_OnPlaced.Postfix(wearNTear);
            }
        }
    }

    [HarmonyPatch(typeof(Piece), nameof(Piece.CanBeRemoved))]
    static class Piece_CanBeRemoved
    {
        static void Postfix(Piece __instance, ref bool __result)
        {
            if (__instance == null || string.IsNullOrEmpty(__instance.m_name) || !__instance.m_name.Contains("$piece_portal"))
            {
                return;
            }

            var config = XPortalNetworksConfig.Instance.Local;
            if (!config.RestrictPortalRemovalToCreator && !config.RestrictPortalRemovalToUsable)
            {
                return;
            }

            __result = CanRemovePortal(__instance);
        }

        /// <summary>
        /// Portal hammer-removal rules. Each enabled restriction has to be satisfied:
        /// <see cref="XPortalNetworksConfig.ConfigSettings.RestrictPortalRemovalToCreator"/> requires the player to
        /// have placed the portal (or to be privileged), and
        /// <see cref="XPortalNetworksConfig.ConfigSettings.RestrictPortalRemovalToUsable"/> requires the portal
        /// to be one they are allowed to use. With both enabled - the default - a player may only remove a
        /// portal they placed <b>and</b> may still use, so enabling an extra restriction never loosens the
        /// other one. Only called when at least one of the two is enabled.
        ///
        /// Privileged players (the host, server admins and portal-network admins) are only above the
        /// portal-network rules while the server allows it (<c>AdminsSeeAllNetworks</c>) - the same gate
        /// <see cref="CustomNetworks.IsLocalPlayerAllowed"/> applies. Without it the host could hammer a
        /// portal it is not allowed to use while being unable to open it.
        ///
        /// NOTE: <c>WearNTear.UpdateWear</c> also reaches this rule, through <c>WearNTear.CanBeRemoved()</c>.
        /// <see cref="WearNTear_CanBeRemoved"/> keeps the wear simulation out of it, so unlike before no
        /// unconditional server-side short-circuit is needed here.
        /// </summary>
        static bool CanRemovePortal(Piece piece)
        {
            if (piece == null)
                return false;

            var config = XPortalNetworksConfig.Instance.Local;

            if (config.RestrictPortalRemovalToCreator
                && !piece.IsCreator()
                && !CustomNetworks.IsLocalPlayerNetworkPrivileged())
            {
                return false;
            }

            // IsLocalPlayerAllowed folds in the same gated privilege.
            if (config.RestrictPortalRemovalToUsable && !IsUsableByLocalPlayer(piece))
                return false;

            return true;
        }

        /// <summary>
        /// True when the local player is allowed to use the portal: the portal's own network must be one
        /// they may use (the Global network, an unrestricted network, or a network they are a member of),
        /// and a private portal may only be used by its owner. This mirrors the check that gates the portal
        /// configuration panel, and deliberately reads the portal's own network/privacy rather than its
        /// destination's.
        /// </summary>
        static bool IsUsableByLocalPlayer(Piece piece)
        {
            ZNetView nview = piece.m_nview;
            ZDO zdo = nview != null ? nview.GetZDO() : null;
            if (zdo == null)
                return false;

            long networkId = ZdoTools.GetNetworkOwnerPlayerId(zdo);
            if (!CustomNetworks.IsLocalPlayerAllowed(networkId))
                return false;

            if (!ZdoTools.GetIsPrivate(zdo))
                return true;

            long playerId = NetPeerUtility.GetLocalPlayerId();
            if (playerId == 0L)
                return false;

            long creator = zdo.GetLong(ZDOVars.s_creator);
            return (creator != 0L && creator == playerId)
                || (networkId != 0L && networkId == playerId);
        }
    }
}
