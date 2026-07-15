namespace Battle.Source.Abilities.BerserkFury
{
    using Core.Battle.Abilities;
    using Core.Entity.Components.Decorator;
    using Core.Enums;

    /// <summary>L1 upgrade: shortens the Fury effect (less health burned overall).</summary>
    public class BfUpgradeFuryDuration(string id, string[] tags, int tier, float amount)
        : AbilityUpgrade<BerserkFury>(id, tags, tier)
    {
        private const string DecoratorId = "Ability_Parameter_Decorator_Bf_Fury_Duration";

        public override void ApplyUpgrade(BerserkFury ability) =>
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator<BerserkFury.Parameters>(
                BerserkFury.Parameters.FuryDuration, Priority.Weak, OperationType.Subtract, amount, DecoratorId, Id));

        public override void RemoveUpgrade(BerserkFury ability) =>
            ability.RemoveParameterDecorator(DecoratorId, BerserkFury.Parameters.FuryDuration);

        public override IAbilityUpgrade Copy() => new BfUpgradeFuryDuration(Id, Tags, Tier, amount);
    }
}
