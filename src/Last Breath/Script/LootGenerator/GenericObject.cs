namespace LastBreath.Script.LootGenerator
{
    using LastBreath.Script.Enums;

    public abstract class GenericObject
    {
        public float ProbabilityWeight;

        public float ProbabilityPercent;

        public float ProbabilityRangeFrom;

        public float ProbabilityRangeTo;

        public GlobalRarity Rarity;
    }
}
