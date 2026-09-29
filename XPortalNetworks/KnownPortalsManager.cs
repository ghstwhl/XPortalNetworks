using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XPortalNetworks.Extension;
using XPortalNetworks.RPC;

namespace XPortalNetworks
{
    internal sealed class KnownPortalsManager : IDisposable
    {
        ////////////////////////////
        //// Singleton instance ////
        private static readonly Lazy<KnownPortalsManager> lazy = new Lazy<KnownPortalsManager>(() => new KnownPortalsManager());
        public static KnownPortalsManager Instance { get { return lazy.Value; } }
        ////////////////////////////

        private readonly Dictionary<ZDOID, KnownPortal> knownPortals = new Dictionary<ZDOID, KnownPortal>();

        public int Count
        {
            get
            {
                return knownPortals.Count;
            }
        }

        private KnownPortalsManager() { }

        public bool ContainsId(ZDOID id)
        {
            return knownPortals.ContainsKey(id);
        }

        public KnownPortal GetKnownPortalById(ZDOID id)
        {
            return knownPortals.TryGetValue(id, out KnownPortal portal) ? portal : null;
        }

        public bool TryGetValue(ZDOID id, out KnownPortal portal)
        {
            return knownPortals.TryGetValue(id, out portal);
        }
        
        public KnownPortal GetKnownPortalByPreviousId(ZDOID previousId)
        {
            return knownPortals.Where(p => p.Value.PreviousId == previousId).Select(kvp => kvp.Value).FirstOrDefault();
        }

        public string GetNetworkOwnerDisplayNameForPlayerId(long networkOwnerPlayerId)
        {
            if (networkOwnerPlayerId == 0L)
            {
                return null;
            }

            foreach (KnownPortal p in knownPortals.Values)
            {
                if (p.NetworkOwnerPlayerId != networkOwnerPlayerId)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(p.NetworkOwnerDisplayName))
                {
                    return p.NetworkOwnerDisplayName;
                }
            }

            return null;
        }

        public List<KnownPortal> GetList()
        {
            return knownPortals.Values.ToList();
        }

        public ZPackage Pack()
        {
            List<KnownPortal> allPortals = GetList();

            var pkg = new ZPackage();
            pkg.Write(allPortals.Count);

            foreach (var knownPortal in allPortals)
            {
                pkg.Write(knownPortal.Pack());
            }

            return pkg;
        }

        public List<KnownPortal> GetSortedList()
        {
            var list = GetList();
            list.Sort((valueA, valueB) => valueA.Name.CompareTo(valueB.Name));
            return list;
        }

        public List<KnownPortal> GetPortalsWithTarget(ZDOID target)
        {
            return knownPortals.Values.Where(p => p.Target == target).ToList();
        }

        public KnownPortal AddOrUpdate(KnownPortal portal)
        {
            if (!ContainsId(portal.Id))
            {
                //Log.Debug($"Adding {portal}");
                knownPortals.Add(portal.Id, portal);
            }
            else
            {
                //Log.Debug($"Updating {portal}");
                knownPortals[portal.Id] = portal;
            }

            PortalMapPins.MarkDirty();

            return knownPortals[portal.Id];
        }

        public bool Remove(ZDOID id)
        {
            var removed = knownPortals.Remove(id);
            if (removed)
            {
                PortalMapPins.MarkDirty();
            }

            return removed;
        }

        public bool Remove(KnownPortal portal)
        {
            return Remove(portal.Id);
        }

        public void UpdateFromZDOList(List<ZDO> zdoList)
        {
            var portalsWithZdos = new List<KnownPortal>();
            
            // Create a list of all portals
            foreach (var portalZDO in zdoList)
            {
                var knownPortal = new KnownPortal(portalZDO.m_uid)
                {
                    Name = portalZDO.GetString("tag"),
                    Location = portalZDO.GetPosition(),
                    PreviousId = ZdoTools.GetPreviousId(portalZDO),
                    Target = ZdoTools.GetTarget(portalZDO),
                    NetworkOwnerPlayerId = ZdoTools.GetNetworkOwnerPlayerId(portalZDO),
                    NetworkOwnerDisplayName = ZdoTools.GetNetworkOwnerDisplayName(portalZDO),
                    IsPrivate = ZdoTools.GetIsPrivate(portalZDO),
                };

                portalsWithZdos.Add(knownPortal);
            }

            // Update our known portals
            UpdateFromList(portalsWithZdos);
        }

        public void UpdateFromResyncPackage(ZPackage pkg)
        {
            var count = pkg.ReadInt();

            Log.Debug($"Received {count} portals from server");

            // Unpack all portals 
            var portalsInPackage = new List<KnownPortal>();
            if (count > 0)
            {
                for (var i = 0; i < count; i++)
                {
                    var portalPkg = pkg.ReadPackage();
                    var portal = new KnownPortal(portalPkg);
                    portalsInPackage.Add(portal);
                }
            }

            // Update our known portals
            UpdateFromList(portalsInPackage);
        }

        private void UpdateFromList(List<KnownPortal> updatedPortals)
        {
            Log.Debug($"Updating {updatedPortals.Count} portals");
            foreach (KnownPortal portal in updatedPortals)
            {
                AddOrUpdate(portal);
            }

            HashSet<ZDOID> updatedIds = new HashSet<ZDOID>(updatedPortals.Select(p => p.Id));
            List<KnownPortal> currentPortals = GetList();
            List<KnownPortal> deletedPortals = currentPortals.Where(p => !updatedIds.Contains(p.Id)).ToList();
            Log.Debug($"Removing {deletedPortals.Count} portals");
            foreach (KnownPortal portal in deletedPortals)
            {
                Remove(portal);
            }

            List<KnownPortal> targetingInvalidPortals = GetList().Where(p => p.Target != ZDOID.None && !ContainsId(p.Target)).ToList();
            Log.Debug($"Retargeting {targetingInvalidPortals.Count} portals");
            foreach (KnownPortal portal in targetingInvalidPortals)
            {
                portal.Target = ZDOID.None;
                SendToServer.AddOrUpdateRequest(portal);
            }

            Log.Info("Known portals updated");
            ReportAllPortals();
        }

        public KnownPortal FindByLocation(Vector3 location)
        {
            return knownPortals.Values.Where(p => p.Location.Round().Equals(location)).FirstOrDefault();
        }

        public ZDOID FindDefaultPortal()
        {
            var defaultLocation = XPortalNetworksConfig.Instance.Local.DefaultPortal.Value.Round();
            var defaultPortal = FindByLocation(defaultLocation);

            if (defaultPortal == null)
            {
                return ZDOID.None;
            }

            return defaultPortal.Id;
        }

        public void ReportAllPortals()  // "reportalls" hehehe
        {
            if (!knownPortals.Any())
            {
                Log.Debug(" No portals found.");
                return;
            }

            foreach (var p in knownPortals.Values)
            {
                Log.Debug($" {p}");
            }
        }

        public void Reset()
        {
            knownPortals.Clear();
            PortalMapPins.MarkDirty();
        }

        public void Dispose()
        {
            knownPortals?.Clear();
        }
    }
}
