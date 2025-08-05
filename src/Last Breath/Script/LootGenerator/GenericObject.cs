namespace LastBreath.Script.LootGenerator
{
    using Core.Enums;

    public abstract class GenericObject
    {
        public float ProbabilityWeight;

        public float ProbabilityPercent;

        public float ProbabilityRangeFrom;

        public float ProbabilityRangeTo;

        public Rarity Rarity;
    }
}
