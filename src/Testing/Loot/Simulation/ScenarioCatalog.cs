namespace LastBreathTest.Loot.Simulation
{
    using Core.Data.NpcData;
    using Core.Enums;

    /// <summary>The scenario matrix for the full report run. Axis scenarios isolate one modifier
    /// each; stack scenarios draw the reward-vs-modifier-count curve.</summary>
    internal static class ScenarioCatalog
    {
        public static NpcArchetype Baseline { get; } = new("Baseline_Regular", EntityType.Regular, Rarity.Uncommon, 5, Fractions.Human);

        public static NpcArchetype RichBaseline { get; } = new("Baseline_Boss", EntityType.Boss, Rarity.Legendary, 50, Fractions.Undead);

        public static IEnumerable<NpcArchetype> All()
        {
            yield return Baseline;
            yield return RichBaseline;

            // Axis: each modifier alone on a budget big enough for every tier to matter.
            string[] axisModifiers =
            [
                "Npc_Modifier_Scale_Double_Health",
                "Npc_Modifier_Tier_Upgrade_To_Maximum",
                "Npc_Modifier_Tier_Upgrade_By_One",
                "Npc_Modifier_Guaranteed_Items_Crafting_Resource",
                "Npc_Modifier_Tier_Multiplier_Huge",
                "Npc_Modifier_Item_Effect_Vampire",
                "Npc_Modifier_Rarity_Upgrade_Huge",
                "Npc_Modifier_Min_Rarity_Epic",
            ];
            foreach (string modifierId in axisModifiers)
                yield return RichBaseline with { Name = $"Axis_{modifierId}", ModifierIds = [modifierId] };

            // Stack: reward curve by random modifier count (the player's "feed the NPC" incentive).
            foreach (int count in (int[])[1, 3, 5, 7, 11])
                yield return new NpcArchetype($"Stack_{count}_mods", EntityType.Boss, Rarity.Mythic, 100, Fractions.Demon)
                {
                    RandomModifierCount = count,
                };

            // Extremes.
            yield return new NpcArchetype("Extreme_Archon", EntityType.Archon, Rarity.Mythic, 150, Fractions.MysticalCreature)
            {
                RandomModifierCount = 11,
            };
            yield return Baseline with { Name = "Extreme_Lvl1", Level = 1 };

            // The #223 before/after pairs: Rolled_ is the live spawn — modifier count decided by the
            // slot cascade (NpcSpawnRolls ladders on the type × rarity ceiling); Ceiling_ is the same
            // NPC under the pre-#223 promise, every slot of the formula filled. One column against the
            // other is the loot-budget cost of the rework, per type.
            (EntityType Type, Rarity Rarity, int Level, Fractions Fraction)[] rolledMatrix =
            [
                (EntityType.Regular, Rarity.Uncommon, 5, Fractions.Human),
                (EntityType.Special, Rarity.Rare, 15, Fractions.Human),
                (EntityType.Elit, Rarity.Epic, 30, Fractions.Undead),
                (EntityType.Unique, Rarity.Legendary, 45, Fractions.Demon),
            ];
            foreach ((var type, var rarity, int level, var fraction) in rolledMatrix)
            {
                yield return new NpcArchetype($"Rolled_{type}_{rarity}", type, rarity, level, fraction) { CascadeRolled = true };
                yield return new NpcArchetype($"Ceiling_{type}_{rarity}", type, rarity, level, fraction)
                {
                    RandomModifierCount = NpcTypeDefaults.ModifierCount(type, rarity),
                };
            }
        }
    }
}
