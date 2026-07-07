namespace Battle.Source.Abilities.BerserkFury
{
    using Core.Battle.Abilities;
    using Core.Enums;
    using Decorators;

    /// <summary>L2 upgrades: attacks burn more (positive amount) or less (negative amount) health.</summary>
    public class BfUpgradeFuryBurn(string id, string[] tags, int tier, float amount)
        : AbilityUpgrade<BerserkFury>(id, tags, tier)
    {
        private const string DecoratorId = "Ability_Parameter_Decorator_Bf_Fury_Burn";

        public override void ApplyUpgrade(BerserkFury ability) =>
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator<BerserkFury.Parameters>(
                BerserkFury.Parameters.FuryHealthPercent, Priority.Weak, OperationType.Add, amount, DecoratorId, Id));

        public override void RemoveUpgrade(BerserkFury ability) =>
            ability.RemoveParameterDecorator(DecoratorId, BerserkFury.Parameters.FuryHealthPercent);

        public override IAbilityUpgrade Copy() => new BfUpgradeFuryBurn(Id, Tags, Tier, amount);
    }
}
