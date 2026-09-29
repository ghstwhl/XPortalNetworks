namespace XPortalNetworks
{
    internal static class ZdoTools
    {
        public static string GetName(ZDO portalZdo)
        {
            return portalZdo.GetString("tag");
        }

        public static void SetName (ZDO portalZdo, string name)
        {
            portalZdo.Set("tag", name);
        }

        public static void SetOwner(ZDO portalZdo)
        {
            portalZdo.SetOwner(ZDOMan.GetSessionID());
        }

        /// <summary>
        /// Portal data lives on the portal's ZDO, under keys prefixed with the plugin Name. That prefix
        /// changed in 3.0.0, so every read falls back to the key the mod used before (<c>XPortalNetworks_*</c>,
        /// which existing world saves still carry) and every write updates both keys. The legacy key therefore
        /// keeps mirroring the current value, which is what makes the "value unset? then use the legacy key"
        /// fallback safe: a portal deliberately reset to Global/an empty name cannot resurrect an older value.
        /// </summary>
        public static void SetPreviousId(ZDO portalZdo)
        {
            portalZdo.Set(XPortalNetworks.Key_PreviousId, portalZdo.m_uid);
            portalZdo.Set(XPortalNetworks.LegacyKey_PreviousId, portalZdo.m_uid);
        }

        public static ZDOID GetPreviousId(ZDO portalZdo)
        {
            var previousId = portalZdo.GetZDOID(XPortalNetworks.Key_PreviousId);
            return previousId != ZDOID.None
                ? previousId
                : portalZdo.GetZDOID(XPortalNetworks.LegacyKey_PreviousId);
        }

        public static void SetTarget(ZDO portalZdo, ZDOID targetId)
        {
            portalZdo.Set(XPortalNetworks.Key_TargetId, targetId);
            portalZdo.Set(XPortalNetworks.LegacyKey_TargetId, targetId);
            portalZdo.SetConnection(ZDOExtraData.ConnectionType.Portal, targetId);
        }

        public static ZDOID GetTarget(ZDO portalZdo)
        {
            var targetId = portalZdo.GetZDOID(XPortalNetworks.Key_TargetId);
            return targetId != ZDOID.None
                ? targetId
                : portalZdo.GetZDOID(XPortalNetworks.LegacyKey_TargetId);
        }

        public static long GetNetworkOwnerPlayerId(ZDO portalZdo)
        {
            var ownerPlayerId = portalZdo.GetLong(XPortalNetworks.Key_NetworkOwnerPlayerId);
            return ownerPlayerId != 0L
                ? ownerPlayerId
                : portalZdo.GetLong(XPortalNetworks.LegacyKey_NetworkOwnerPlayerId);
        }

        public static void SetNetworkOwnerPlayerId(ZDO portalZdo, long ownerPlayerId)
        {
            portalZdo.Set(XPortalNetworks.Key_NetworkOwnerPlayerId, ownerPlayerId);
            portalZdo.Set(XPortalNetworks.LegacyKey_NetworkOwnerPlayerId, ownerPlayerId);
        }

        public static string GetNetworkOwnerDisplayName(ZDO portalZdo)
        {
            var displayName = portalZdo.GetString(XPortalNetworks.Key_NetworkOwnerDisplayName);
            return !string.IsNullOrEmpty(displayName)
                ? displayName
                : portalZdo.GetString(XPortalNetworks.LegacyKey_NetworkOwnerDisplayName);
        }

        public static void SetNetworkOwnerDisplayName(ZDO portalZdo, string displayName)
        {
            portalZdo.Set(XPortalNetworks.Key_NetworkOwnerDisplayName, displayName ?? string.Empty);
            portalZdo.Set(XPortalNetworks.LegacyKey_NetworkOwnerDisplayName, displayName ?? string.Empty);
        }

        public static bool GetIsPrivate(ZDO portalZdo)
        {
            return portalZdo.GetBool(XPortalNetworks.Key_IsPrivate, false)
                || portalZdo.GetBool(XPortalNetworks.LegacyKey_IsPrivate, false);
        }

        public static void SetIsPrivate(ZDO portalZdo, bool isPrivate)
        {
            portalZdo.Set(XPortalNetworks.Key_IsPrivate, isPrivate);
            portalZdo.Set(XPortalNetworks.LegacyKey_IsPrivate, isPrivate);
        }

        public static void UpdateFromKnownPortal(bool delayed = false, object state = null)
        {
            if (delayed)
            {
                QueuedAction.Queue(UpdateFromKnownPortal, delay: 1);
                return;
            }

            var portal = (KnownPortal)state;
            var portalZdo = ZDOMan.instance.GetZDO(portal.Id);

            if (portalZdo == null)
            {
                Log.Debug("Portal ZDO not found, trying again with delay..");
                QueuedAction.Queue(UpdateFromKnownPortal, delay: 3, state: portal);
                return;
            }

            SetOwner(portalZdo);
            SetName(portalZdo, portal.Name);
            SetPreviousId(portalZdo);
            SetNetworkOwnerPlayerId(portalZdo, portal.NetworkOwnerPlayerId);
            SetNetworkOwnerDisplayName(portalZdo, portal.NetworkOwnerDisplayName ?? string.Empty);
            SetIsPrivate(portalZdo, portal.IsPrivate);
            SetTarget(portalZdo, portal.Target);
        }
    }
}
