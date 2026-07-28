namespace Battle.Source.Abilities
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>
    /// Upgrade that replaces the ability's resource type (e.g. mana → health). CostType is stored as a
    /// number in the modules, so the swap is an <see cref="OperationType.Override"/> decorator with
    /// absolute priority — nothing may stack on top of a categorical value.
    /// </summary>
    public class AbilityUpgradeCostTypeOverride(string id, string[] tags, int tier, Costs costType)
        : AbilityUpgrade<Ability>(id, tags, tier)
    {
        private string DecoratorId => $"Ability_Parameter_Decorator_{Id}";

        public override void ApplyUpgrade(Ability ability) =>
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator(
                AbilityParameter.CostType, Priority.Absolute, OperationType.Override, (float)costType, DecoratorId, Id));

        public override void RemoveUpgrade(Ability ability) =>
            ability.RemoveParameterDecorator(DecoratorId, AbilityParameter.CostType);

        public override IAbilityUpgrade Copy() => new AbilityUpgradeCostTypeOverride(Id, Tags, Tier, costType);
    }
}
