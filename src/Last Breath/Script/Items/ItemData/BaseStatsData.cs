namespace Playground.Script.Items.ItemData
{
    using System.Collections.Generic;
    using Playground.Script.Enums;

    public abstract class BaseStatsData
    {
        public Dictionary<GlobalRarity, ItemStats> SimpleStats = [];
        public Dictionary<AttributeType, Dictionary<GlobalRarity, ItemStats>> AttributeStats = [];

        public ItemStats GetSimpleStats(GlobalRarity rarity) => GetValuesFromDictionary(rarity, SimpleStats);

        public ItemStats GetAttributeStats(AttributeType type, GlobalRarity rarity)
        {
            if (!AttributeStats.TryGetValue(type, out var dictionary))
            {
                dictionary = [];
            }

            return GetValuesFromDictionary(rarity, dictionary);
        }

        private ItemStats GetValuesFromDictionary(GlobalRarity rarity, Dictionary<GlobalRarity, ItemStats> dictionary)
        {
            if (!dictionary.TryGetValue(rarity, out var stats))
            {
                stats = new ItemStats();
            }
            return stats;
        }
    }
}
