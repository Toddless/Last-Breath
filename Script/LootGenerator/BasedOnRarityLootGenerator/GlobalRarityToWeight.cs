namespace Playground.Script.LootGenerator.BasedOnRarityLootGenerator
{
    using System.Collections.Generic;

    public static class GlobalRarityToWeight
    {
        public static readonly Dictionary<GlobalRarity, float> rarityWeights = new()
        {
            {GlobalRarity.Uncommon, 1500 },
            {GlobalRarity.Rare, 250 },
            {GlobalRarity.Epic, 125 },
            {GlobalRarity.Legendary, 25 },
            {GlobalRarity.Mythic, 5 },
        };
    }
}
