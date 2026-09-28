using HarmonyLib;

namespace XPortalNetworks.Patches
{
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdatePins))]
    static class Minimap_UpdatePins
    {
        /// <summary>
        /// <see cref="Minimap.UpdatePins"/> sets every pin marker's colour itself - white for pins without
        /// an owner - which would wash out the portal pins' colour, so ours is re-applied right after each
        /// pass (this is what the standalone "XPortal Shared Map Pins" mod did as well).
        /// </summary>
        static void Postfix()
        {
            PortalMapPins.ReapplyColours();
        }
    }
}
