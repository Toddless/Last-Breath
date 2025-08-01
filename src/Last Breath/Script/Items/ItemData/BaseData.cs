namespace LastBreath.Script.Items.ItemData
{
    using System.Collections.Generic;
    using Contracts.Enums;

    public abstract class BaseData<T>
        where T : class, new()
    {
        public Dictionary<GlobalRarity, T> SimpleData = [];
        public Dictionary<AttributeType, Dictionary<GlobalRarity, T>> AttributeData = [];

        public T GetSimpleData(GlobalRarity rarity) => GetValuesFromDictionary(rarity, SimpleData);

        public T GetAttributeData(AttributeType type, GlobalRarity rarity)
        {
            if (!AttributeData.TryGetValue(type, out var dictionary))
            {
                dictionary = [];
            }

            return GetValuesFromDictionary(rarity, dictionary);
        }

        private T GetValuesFromDictionary(GlobalRarity rarity, Dictionary<GlobalRarity, T> dictionary)
        {
            if (!dictionary.TryGetValue(rarity, out var stats))
            {
                stats = new T();
            }
            return stats;
        }
    }
}
