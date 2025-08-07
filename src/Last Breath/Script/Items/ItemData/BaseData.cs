namespace LastBreath.Script.Items.ItemData
{
    using System.Collections.Generic;
    using Core.Enums;

    public class BaseData<T>
        where T : class, new()
    {
        public Dictionary<Rarity, T> SimpleData = [];
        public Dictionary<AttributeType, Dictionary<Rarity, T>> AttributeData = [];

        public T GetSimpleData(Rarity rarity) => GetValuesFromDictionary(rarity, SimpleData);

        public T GetAttributeData(AttributeType type, Rarity rarity)
        {
            if (!AttributeData.TryGetValue(type, out var dictionary))
            {
                dictionary = [];
            }

            return GetValuesFromDictionary(rarity, dictionary);
        }

        private T GetValuesFromDictionary(Rarity rarity, Dictionary<Rarity, T> dictionary)
        {
            if (!dictionary.TryGetValue(rarity, out var stats))
            {
                stats = new T();
            }
            return stats;
        }
    }
}
