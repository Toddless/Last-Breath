namespace LastBreathTest.LootSimulation
{
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
        }
    }
}
