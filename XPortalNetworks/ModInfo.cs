namespace Mod
{
    public static class Info
    {
        // This is *the* place to edit plugin details. Everywhere else will be generated based on this info.
        public const string GUID = "vapok.mods.xportalnetworks";
        public const string HarmonyGUID = GUID + ".harmony";
        public const string Author = "ghstwhl";
        // NOTE: Name is the plugin's identity - the portal ZDO keys (XPortalNetworks_TargetId, ...) and the
        // RPC names are derived from it, so renaming it would disconnect every existing portal in the world.
        public const string Name = "XPortalNetworks";
        // This project's own repository. The original mod it is built upon lives at
        // https://github.com/Vapok/XPortalNetworks and is credited in README.md and REFERENCES.md.
        public const string GitHubRepo = "ghstwhl/XPortalNetworksTribesPins";
        // Thunderstore identity of the published package (the team name differs from the GitHub account).
        // These drive the generated install links and the build's package staging folder.
        public const string ThunderstoreTeam = "NorCal_Nerds";
        public const string ThunderstorePackage = "XPortalNetworksTribesPins";
        public const string Version = "2.6.0";
        public const string Description = "Select portal destination from a list of existing portals with custom networks with private portal and tribe restrictions. No more tag pairing, and no more portal hubs!  Also manages map pins for the portals a player is allowed to use.";
        public const string WebsiteUrl = "https://github.com/" + GitHubRepo;
        public const int NexusId = 3719;
        public const string BepInExPackVersion = "5.4.2350";
        public const string JotunnVersion = Jotunn.Main.Version;
    }
}
