namespace Battle.Source.Abilities.DoubleStrike
{
    using Core.Battle.Abilities;
    using Core.Modifiers.Context;

    /// <summary>L2 upgrade: both strikes gain bonus accuracy.</summary>
    public class DstUpgradeAccuracy(string id, string[] tags, int tier, float amount)
        : AbilityUpgrade<DoubleStrike>(id, tags, tier)
    {
        private readonly AccuracyAttackContextModifier _contextModifier = new(amount);

        public override void ApplyUpgrade(DoubleStrike ability) => ability.AddAttackModifier(_contextModifier);

        public override void RemoveUpgrade(DoubleStrike ability) => ability.RemoveAttackModifier(_contextModifier.Id);

        public override IAbilityUpgrade Copy() => new DstUpgradeAccuracy(Id, Tags, Tier, amount);
    }
}
