namespace Playground.Script.LootGenerator
{
    using Playground.Script.Enums;

    public abstract class GenericObject
    {
        public float ProbabilityWeight;

        public float ProbabilityPercent;

        public float ProbabilityRangeFrom;

        public float ProbabilityRangeTo;

        public GlobalRarity Rarity;
    }
}
