namespace LastBreath.Script.LootGenerator.BasedOnRarityLootGenerator
{
    using System.Collections.Generic;

    public static class ConvertGlobalRarity
    {
        public static readonly Dictionary<Contracts.Enums.Rarity, float> rarityWeights = new()
        {
            { Contracts.Enums.Rarity.Uncommon, 1500 },
            { Contracts.Enums.Rarity.Rare, 250 },
            { Contracts.Enums.Rarity.Epic, 125 },
            { Contracts.Enums.Rarity.Legendary, 25 },
        };

        public static readonly Dictionary<Contracts.Enums.Rarity, int> multiplier = new()
        {
            { Contracts.Enums.Rarity.Uncommon, 1 },
            { Contracts.Enums.Rarity.Rare, 2 },
            { Contracts.Enums.Rarity.Epic, 3 },
            { Contracts.Enums.Rarity.Legendary, 4 },
            { Contracts.Enums.Rarity.Mythic, 5 },
        };

        public static readonly Dictionary<Contracts.Enums.Rarity, int> abilityQuantity = new()
        {
            { Contracts.Enums.Rarity.Uncommon, 1 },
            { Contracts.Enums.Rarity.Rare, 2 },
            { Contracts.Enums.Rarity.Epic, 4 },
            { Contracts.Enums.Rarity.Legendary, 6 },
            { Contracts.Enums.Rarity.Mythic, 8 },
        };
    }
}
