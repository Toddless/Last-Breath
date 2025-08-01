namespace LastBreath.Script.LootGenerator.BasedOnRarityLootGenerator
{
    using System.Collections.Generic;
    using Contracts.Enums;

    public static class ConvertGlobalRarity
    {
        public static readonly Dictionary<GlobalRarity, float> rarityWeights = new()
        {
            {GlobalRarity.Uncommon, 1500 },
            {GlobalRarity.Rare, 250 },
            {GlobalRarity.Epic, 125 },
            {GlobalRarity.Legendary, 25 },
        };

        public static readonly Dictionary<GlobalRarity, int> multiplier = new()
        {
            {GlobalRarity.Uncommon, 1 },
            {GlobalRarity.Rare, 2 },
            {GlobalRarity.Epic, 3 },
            {GlobalRarity.Legendary, 4 },
            {GlobalRarity.Mythic, 5 },
        };

        public static readonly Dictionary<GlobalRarity, int> abilityQuantity = new()
        {
            {GlobalRarity.Uncommon, 1 },
            {GlobalRarity.Rare, 2 },
            {GlobalRarity.Epic, 4 },
            {GlobalRarity.Legendary, 6 },
            {GlobalRarity.Mythic, 8 },
        };
    }
}
