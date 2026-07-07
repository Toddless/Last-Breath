namespace Battle.Source.Abilities.DoubleStrike
{
    using Core.Battle.Abilities;
    using Modifiers;

    /// <summary>L2 upgrade: both strikes gain bonus accuracy.</summary>
    public class DstUpgradeAccuracy(string id, string[] tags, int tier, float amount)
        : AbilityUpgrade<DoubleStrike>(id, tags, tier)
    {
        private readonly AccuracyAttackModifier _modifier = new(amount);

        public override void ApplyUpgrade(DoubleStrike ability) => ability.AddAttackModifier(_modifier);

        public override void RemoveUpgrade(DoubleStrike ability) => ability.RemoveAttackModifier(_modifier.Id);

        public override IAbilityUpgrade Copy() => new DstUpgradeAccuracy(Id, Tags, Tier, amount);
    }
}
