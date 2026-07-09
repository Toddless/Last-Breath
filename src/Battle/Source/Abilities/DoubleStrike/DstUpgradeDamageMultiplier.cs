namespace Battle.Source.Abilities.DoubleStrike
{
    using Core.Battle.Abilities;
    using Core.Components.Decorator;
    using Core.Enums;

    /// <summary>L2 upgrade: both strikes deal a percentage more damage.</summary>
    public class DstUpgradeDamageMultiplier(string id, string[] tags, int tier, float amount)
        : AbilityUpgrade<DoubleStrike>(id, tags, tier)
    {
        private const string DecoratorId = "Ability_Parameter_Decorator_Dst_Damage_Multiplier";

        public override void ApplyUpgrade(DoubleStrike ability) =>
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator<DoubleStrike.Parameters>(
                DoubleStrike.Parameters.DamageMultiplier, Priority.Weak, OperationType.Add, amount, DecoratorId, Id));

        public override void RemoveUpgrade(DoubleStrike ability) =>
            ability.RemoveParameterDecorator(DecoratorId, DoubleStrike.Parameters.DamageMultiplier);

        public override IAbilityUpgrade Copy() => new DstUpgradeDamageMultiplier(Id, Tags, Tier, amount);
    }
}
