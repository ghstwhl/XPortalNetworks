using HarmonyLib;
using UnityEngine;

namespace XPortalNetworks.Patches
{
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.OnPlaced))]
    static class WearNTear_OnPlaced
    {
        internal static void Postfix(WearNTear __instance)
        {
            if (__instance == null)
                return;

            Piece piece = __instance.GetComponent<Piece>();
            ZNetView nview = __instance.GetComponent<ZNetView>();
            if (piece != null && !string.IsNullOrEmpty(piece.m_name) && piece.m_name.Contains("$piece_portal") && nview != null)
            {
                ZDO portalZDO = nview.GetZDO();
                if (portalZDO == null)
                {
                    Log.Error("A portal was placed but the ZDO is not available");
                    return;
                }

                ZDOID portalId = portalZDO.m_uid;
                Vector3 location = portalZDO.GetPosition();
                XPortalNetworks.OnPortalPlaced(portalId, location);
            }
        }
    }

    /// <summary>
    /// Keeps the portal removal rules out of the environmental wear simulation.
    ///
    /// <c>WearNTear.UpdateWear</c> (owner side) asks <see cref="WearNTear.CanBeRemoved"/> whether a piece may
    /// be removed and, when it may not, never lets wear damage destroy it. "May this player hammer it" is a
    /// different question, so the wear simulation always sees a removable portal - otherwise a portal on a
    /// restricted network would silently become immune to decay. <c>UpdateWear</c> is the only caller of
    /// this method, and the hammer never uses it (<c>Player.RemovePiece</c> calls <c>Piece.CanBeRemoved</c>
    /// directly), so this cannot hand a player any removal rights.
    /// </summary>
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.CanBeRemoved))]
    static class WearNTear_CanBeRemoved
    {
        static void Postfix(WearNTear __instance, ref bool __result)
        {
            if (__result || __instance == null)
                return;

            Piece piece = __instance.m_piece;
            if (piece != null && !string.IsNullOrEmpty(piece.m_name) && piece.m_name.Contains("$piece_portal"))
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Destroy))]
    static class WearNTear_Destroy
    {
        static void Prefix(WearNTear __instance)
        {
            if (__instance == null)
                return;

            Piece piece = __instance.m_piece;
            if (piece == null)
                return;

            ZNetView nview = piece.m_nview;
            if (nview == null)
                return;

            if (!string.IsNullOrEmpty(piece.m_name) && piece.m_name.Contains("$piece_portal") && piece.CanBeRemoved())
            {
                ZDO portalZDO = nview.GetZDO();
                if (portalZDO == null)
                {
                    Log.Error("A portal was destroyed but the ZDO is not available");
                    return;
                }

                ZDOID portalId = portalZDO.m_uid;
                XPortalNetworks.OnPortalDestroyed(portalId);
            }
        }
    }
}
