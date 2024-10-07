using System.Collections.Generic;

namespace Playground.Script.LootGenerator.BasedOnRarityLootGenerator
{
    public static class GlobalRarityToWeight
    {
        public static readonly Dictionary<GlobalRarity, float> rarityWeights = new()
        {
            {GlobalRarity.Common, 1 },
            {GlobalRarity.Uncommon, 1500 },
            {GlobalRarity.Rare, 500 },
            {GlobalRarity.Epic, 250 },
            {GlobalRarity.Legendary, 50 },
            {GlobalRarity.Mythic, 10 },
        };
    }
}
