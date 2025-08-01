namespace LastBreath.Script.LootGenerator
{
    using Contracts.Enums;

    public abstract class GenericObject
    {
        public float ProbabilityWeight;

        public float ProbabilityPercent;

        public float ProbabilityRangeFrom;

        public float ProbabilityRangeTo;

        public GlobalRarity Rarity;
    }
}
