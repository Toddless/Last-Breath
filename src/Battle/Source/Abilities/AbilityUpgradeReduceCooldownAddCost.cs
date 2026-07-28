namespace Battle.Source.Abilities
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>Reduces the ability's cooldown at the price of an increased resource cost.</summary>
    public class AbilityUpgradeReduceCooldownAddCost(string id, string[] tags, int tier, float cooldown, float additionalCost)
        : AbilityUpgrade<Ability>(id, tags, tier)
    {
        private string CooldownDecoratorId => $"Ability_Parameter_Decorator_{Id}_Cooldown";
        private string CostDecoratorId => $"Ability_Parameter_Decorator_{Id}_Cost";

        public override void ApplyUpgrade(Ability ability)
        {
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator(
                AbilityParameter.Cooldown, Priority.Weak, OperationType.Subtract, cooldown, CooldownDecoratorId, Id));
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator(
                AbilityParameter.CostValue, Priority.Weak, OperationType.Add, additionalCost, CostDecoratorId, Id));
        }

        public override void RemoveUpgrade(Ability ability)
        {
            ability.RemoveParameterDecorator(CooldownDecoratorId, AbilityParameter.Cooldown);
            ability.RemoveParameterDecorator(CostDecoratorId, AbilityParameter.CostValue);
        }

        public override IAbilityUpgrade Copy() => new AbilityUpgradeReduceCooldownAddCost(Id, Tags, Tier, cooldown, additionalCost);
    }
}
