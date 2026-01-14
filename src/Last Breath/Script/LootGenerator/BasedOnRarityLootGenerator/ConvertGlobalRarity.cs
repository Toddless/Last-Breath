namespace LastBreath.Script.LootGenerator.BasedOnRarityLootGenerator
{
    using System.Collections.Generic;

    public static class ConvertGlobalRarity
    {
        public static readonly Dictionary<Core.Enums.Rarity, float> rarityWeights = new()
        {
            { Core.Enums.Rarity.Uncommon, 1500 },
            { Core.Enums.Rarity.Rare, 250 },
            { Core.Enums.Rarity.Epic, 125 },
            { Core.Enums.Rarity.Legendary, 25 },
        };

        public static readonly Dictionary<Core.Enums.Rarity, int> multiplier = new()
        {
            { Core.Enums.Rarity.Uncommon, 1 },
            { Core.Enums.Rarity.Rare, 2 },
            { Core.Enums.Rarity.Epic, 3 },
            { Core.Enums.Rarity.Legendary, 4 },
            { Core.Enums.Rarity.Mythic, 5 },
        };

        public static readonly Dictionary<Core.Enums.Rarity, int> abilityQuantity = new()
        {
            { Core.Enums.Rarity.Uncommon, 1 },
            { Core.Enums.Rarity.Rare, 2 },
            { Core.Enums.Rarity.Epic, 4 },
            { Core.Enums.Rarity.Legendary, 6 },
            { Core.Enums.Rarity.Mythic, 8 },
        };
    }
}
