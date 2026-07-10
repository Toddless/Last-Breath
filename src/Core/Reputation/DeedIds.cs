namespace Core.Reputation
{
    /// <summary>Ids of the deeds the code publishes itself; the full catalog lives in ReputationDeeds.json.</summary>
    public static class DeedIds
    {
        public const string KillNpc = "Deed_Kill_Npc";
    }

    /// <summary>Reasons carried by ReputationChangedArgs for changes that are not deeds.</summary>
    public static class ReputationReasons
    {
        /// <summary>Direct write: save-load restore, quest/design overrides. Not shown to the player.</summary>
        public const string DirectSet = "Set";
    }
}
