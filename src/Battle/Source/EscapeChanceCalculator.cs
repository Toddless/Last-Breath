namespace Battle.Source
{
    using System;
    using System.Collections.Generic;
    using Core.Entity;
    using Core.Enums;

    /// <summary>
    /// The player's flee chance is dictated by the TOUGHEST standing enemy: a common regular
    /// barely holds anyone (~95%), a mythic archon lets nobody go (~5%). Penalty numbers are
    /// invented defaults — tune to taste.
    /// </summary>
    public static class EscapeChanceCalculator
    {
        private const float BaseChance = 0.95f;
        private const float MinChance = 0.05f;

        public static float For(IEnumerable<IFightableNpc> enemies)
        {
            float hardestGrip = 0f;
            foreach (var enemy in enemies)
                hardestGrip = Math.Max(hardestGrip, TypePenalty(enemy.EntityType) + RarityPenalty(enemy.Rarity));

            return Math.Clamp(BaseChance - hardestGrip, MinChance, BaseChance);
        }

        private static float TypePenalty(EntityType type) => type switch
        {
            EntityType.Regular => 0f,
            EntityType.Special => 0.1f,
            EntityType.Elit => 0.15f,
            EntityType.Unique => 0.25f,
            EntityType.Boss => 0.35f,
            EntityType.Archon => 0.5f,
            _ => 0f
        };

        private static float RarityPenalty(Rarity rarity) => rarity switch
        {
            Rarity.Common => 0f,
            Rarity.Uncommon => 0.05f,
            Rarity.Rare => 0.1f,
            Rarity.Epic => 0.15f,
            Rarity.Legendary => 0.25f,
            Rarity.Unique => 0.3f,
            Rarity.Mythic => 0.45f,
            _ => 0f
        };
    }
}
