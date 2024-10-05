using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

namespace Playground.Script.LootGenerator
{
    public abstract class GenericObject<T>
        where T : class
    {
        public T Item;

        public float probabilityWeight;

        public float probabilityPercent;

        public float probabilityRangeFrom;

        public float probabilityRangeTo;

        public GlobalRarity Rarity;
    }

}
