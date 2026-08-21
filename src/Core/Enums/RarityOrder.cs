namespace Core.Enums
{
    /// <summary>
    /// Rarity as an ORDER: best first. The enum's own numbering cannot be sorted on — Unique and Mythic
    /// sit at 10 and 11, past Common, while both are better than everything below them — so anything
    /// laying rarities out in front of the player ranks them here instead of comparing the raw values.
    /// </summary>
    public static class RarityOrder
    {
        /// <summary>Where the rarity stands when the best is shown first. Lower is better.</summary>
        public static int DisplayRank(this Rarity rarity) => rarity switch
        {
            Rarity.Mythic => 0,
            Rarity.Unique => 1,
            _ => (int)rarity + 2,
        };
    }
}
