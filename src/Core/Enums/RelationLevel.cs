namespace Core.Enums
{
    /// <summary>
    /// Relation ladder, ordered from worst to best. Faction-vs-faction entries use the
    /// Hostility/Dislike/Neutral band; the full ladder applies to the player's standing
    /// (settlement access, quest rewards and crafting prices hook into it later).
    /// </summary>
    public enum RelationLevel : byte
    {
        /// <summary>Attack on sight; settlements are closed.</summary>
        Hatred,

        /// <summary>Enemies: settlements demand payment, no quests, prices +50%.</summary>
        Hostility,

        /// <summary>Guards may demand an entrance fee; rewards −30%, prices +30%.</summary>
        Dislike,

        Neutral,

        /// <summary>Rewards +15%, prices −15%.</summary>
        Friendly,

        /// <summary>Rewards +30%, prices −30%.</summary>
        Respect,

        /// <summary>Rewards +50%, prices −50%, mythic-grade gear access.</summary>
        Alliance
    }
}
