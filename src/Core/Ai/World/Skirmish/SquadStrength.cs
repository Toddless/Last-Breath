namespace Core.Ai.World.Skirmish
{
    using System.Collections.Generic;
    using System.Linq;
    using Enums;

    /// <summary>
    /// The "Сила" of a squad added to its d20 rolls. Level, rarity, type and headcount only
    /// ADD chances (never guarantee): a strong squad shifts the roll, the dice still decide.
    /// </summary>
    public static class SquadStrength
    {
        /// <summary>Level contribution: Regular cap 15 → 1.5, Archon 150 → 15.</summary>
        private const float LevelWeight = 0.1f;

        /// <summary>Each member beyond the first adds a flat bonus (headcount advantage).</summary>
        private const float ExtraMemberBonus = 1.5f;

        public static float Calculate(IReadOnlyList<ISkirmishParticipant> squad)
        {
            if (squad.Count == 0) return 0f;
            return squad.Average(MemberStrength) + (squad.Count - 1) * ExtraMemberBonus;
        }

        private static float MemberStrength(ISkirmishParticipant member) =>
            member.Level * LevelWeight + RarityBonus(member.Rarity) + TypeBonus(member.EntityType);

        private static float RarityBonus(Rarity rarity) => rarity switch
        {
            Rarity.Uncommon => 0.25f,
            Rarity.Rare => 0.5f,
            Rarity.Epic => 1f,
            Rarity.Legendary => 1.5f,
            Rarity.Mythic => 2f,
            Rarity.Unique => 2.5f,
            _ => 0f
        };

        private static float TypeBonus(EntityType type) => type switch
        {
            EntityType.Special => 0.5f,
            EntityType.Elit => 1f,
            EntityType.Unique => 1.5f,
            EntityType.Boss => 3f,
            EntityType.Archon => 5f,
            _ => 0f
        };
    }
}
