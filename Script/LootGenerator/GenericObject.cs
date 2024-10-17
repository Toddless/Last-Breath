namespace Playground.Script.LootGenerator
{
    using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;

    public abstract class GenericObject<T>
        where T : class
    {
        public T? Item;

        public float probabilityWeight;

        public float probabilityPercent;

        public float probabilityRangeFrom;

        public float probabilityRangeTo;

        public GlobalRarity Rarity;
    }
}
