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
            if (!config.RestrictPortalRemoval && !config.RestrictPortalRemovalToUsable)
            {
                return;
            }

            __result = CanRemovePortal(__instance);
        }

        /// <summary>
        /// Portal hammer-removal rules. A player may remove a portal they created
        /// (<see cref="XPortalNetworksConfig.ConfigSettings.RestrictPortalRemoval"/>) or one they are
        /// allowed to use (<see cref="XPortalNetworksConfig.ConfigSettings.RestrictPortalRemovalToUsable"/>).
        /// Server admins, the host and portal-network admins may always remove portals. Only called when at
        /// least one of the two restrictions is enabled.
        ///
        /// NOTE: the server/owner runs this method too - <c>WearNTear.UpdateWear</c> consults
        /// <c>WearNTear.CanBeRemoved()</c> to decide whether environmental wear may destroy a piece - so the
        /// privileged short-circuit below is required, not just a convenience.
        /// </summary>
        static bool CanRemovePortal(Piece piece)
        {
            if (piece == null)
                return false;

            if (ZNet.instance != null && (ZNet.instance.LocalPlayerIsAdminOrHost() || XPortalNetworksAdminSync.IsLocalPortalNetworkAdmin()))
                return true;

            var config = XPortalNetworksConfig.Instance.Local;

            if (config.RestrictPortalRemoval && piece.IsCreator())
                return true;

            if (config.RestrictPortalRemovalToUsable && IsUsableByLocalPlayer(piece))
                return true;

            return false;
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
