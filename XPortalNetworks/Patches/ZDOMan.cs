using HarmonyLib;
using System.Collections.Generic;

namespace XPortalNetworks.Patches
{
    [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.ConnectPortals))]
    static class ZDOMan_ConnectPortals
    {
        static bool Prefix()
        {
            Log.Debug("Restoring Portal connections..");

            List<ZDO> allPortals = ZDOMan.instance != null ? ZDOMan.instance.GetPortalList() : new List<ZDO>();

            HashSet<ZDOID> portalIds = new HashSet<ZDOID>();
            foreach (ZDO zdo in allPortals)
            {
                if (zdo != null && zdo.m_uid != ZDOID.None)
                {
                    portalIds.Add(zdo.m_uid);
                }
            }

            List<ZDOID> connectionIds1 = ZDOExtraData.GetAllConnectionZDOIDs(ZDOExtraData.ConnectionType.Portal);
            List<ZDOID> connectionIds2 = ZDOExtraData.GetAllConnectionZDOIDs(ZDOExtraData.ConnectionType.Portal | ZDOExtraData.ConnectionType.Target);

            foreach (ZDOID connId in connectionIds1)
            {
                if (connId != ZDOID.None && portalIds.Add(connId))
                {
                    ZDO extraZdo = ZDOMan.instance.GetZDO(connId);
                    if (extraZdo != null)
                    {
                        allPortals.Add(extraZdo);
                    }
                }
            }

            foreach (ZDOID connId in connectionIds2)
            {
                if (connId != ZDOID.None && portalIds.Add(connId))
                {
                    ZDO extraZdo = ZDOMan.instance.GetZDO(connId);
                    if (extraZdo != null)
                    {
                        allPortals.Add(extraZdo);
                    }
                }
            }

            Log.Debug($"Found {allPortals.Count} portal(s).");

            if (allPortals.Count == 0) return false;

            Dictionary<ZDOID, ZDO> portalsByPreviousId = new Dictionary<ZDOID, ZDO>();
            foreach (ZDO portalZdo in allPortals)
            {
                if (portalZdo == null || portalZdo.m_uid == ZDOID.None) continue;

                ZDOID previousId = ZdoTools.GetPreviousId(portalZdo);
                if (previousId != ZDOID.None)
                {
                    portalsByPreviousId[previousId] = portalZdo;
                }
            }

            foreach (ZDO portalZdo in allPortals)
            {
                if (portalZdo == null || portalZdo.m_uid == ZDOID.None) continue;

                ZDOID portalId = portalZdo.m_uid;
                string portalName = portalZdo.GetString("tag");
                Log.Debug($"Checking connection for `{portalId}` (`{portalName}`)");

                ZDOID targetId = ZdoTools.GetTarget(portalZdo);

                if (targetId == ZDOID.None) continue;

                ZDO targetZdo = null;

                if (portalsByPreviousId.TryGetValue(targetId, out ZDO matchedZdo))
                {
                    targetZdo = matchedZdo;
                }
                else
                {
                    foreach (ZDO candidate in allPortals)
                    {
                        if (candidate != null && candidate.m_uid == targetId)
                        {
                            targetZdo = candidate;
                            break;
                        }
                    }
                }

                if (targetZdo == null)
                {
                    Log.Debug($"Target `{targetId}` could not be found for portal `{portalId}` (`{portalName}`). Clearing target.");
                    portalZdo.SetOwner(ZDOMan.GetSessionID());
                    ZdoTools.SetTarget(portalZdo, ZDOID.None);
                    continue;
                }

                ZDOID newTargetId = targetZdo.m_uid;
                string targetPortalName = targetZdo.GetString("tag");
                Log.Info($"Connecting: `{portalId}` (`{portalName}`)  ==>  `{newTargetId}` (`{targetPortalName}`)");

                portalZdo.SetOwner(ZDOMan.GetSessionID());
                ZdoTools.SetTarget(portalZdo, newTargetId);
            }

            Log.Debug("Updating PreviousId for all portals..");
            foreach (ZDO portalZdo in allPortals)
            {
                if (portalZdo != null && portalZdo.m_uid != ZDOID.None)
                {
                    ZdoTools.SetPreviousId(portalZdo);
                }
            }

            return false;
        }
    }
}
