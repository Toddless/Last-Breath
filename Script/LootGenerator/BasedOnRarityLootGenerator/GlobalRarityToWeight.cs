namespace Playground.Script.LootGenerator.BasedOnRarityLootGenerator
{
    using System.Collections.Generic;

    public static class GlobalRarityToWeight
    {
        public static readonly Dictionary<GlobalRarity, float> rarityWeights = new()
        {
            {GlobalRarity.Common, 5000 },
            {GlobalRarity.Uncommon, 1500 },
            {GlobalRarity.Rare, 500 },
            {GlobalRarity.Epic, 250 },
            {GlobalRarity.Legendary, 50 },
            {GlobalRarity.Mythic, 10 },
        };
    }
}
