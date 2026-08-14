namespace Battle.Source.Abilities.DoubleStrike
{
    using Core.Battle.Abilities;
    using Core.Modifiers.Context;

    /// <summary>L2 upgrade: both strikes gain bonus accuracy.</summary>
    public class DstAugmentAccuracy(string id, string[] tags, int tier, float amount)
        : AbilityAugment<DoubleStrike>(id, tags, tier)
    {
        private readonly AccuracyAttackContextModifier _contextModifier = new(amount);

        public override void ApplyUpgrade(DoubleStrike ability) => ability.AddAttackModifier(_contextModifier);

        public override void RemoveUpgrade(DoubleStrike ability) => ability.RemoveAttackModifier(_contextModifier.Id);

        public override IAbilityAugment Copy() => new DstAugmentAccuracy(Id, Tags, Tier, amount);
    }
}
