namespace Battle.Source.Abilities
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>
    /// Buys a cheaper cast with a longer wait: a share off the price, whole turns added to the cooldown.
    /// Two shapes for the same reason the sibling records state them — a price is a share because the
    /// book's prices run from nothing to five hundred, a wait is counted because turns are whole.
    /// The added turns need no floor: nothing here reduces the wait.
    /// </summary>
    public class AbilityAugmentReduceCostAddCooldown(string id, string[] tags, int tier, float costShare, float cooldownTurns)
        : AbilityAugment<Ability>(id, tags, tier)
    {
        private string CostDecoratorId => $"Ability_Parameter_Decorator_{Id}_Cost";
        private string CooldownDecoratorId => $"Ability_Parameter_Decorator_{Id}_Cooldown";

        public override void ApplyUpgrade(Ability ability)
        {
            ability.AddParameterDecorator(new AbilityParameterShare(
                AbilityParameter.CostValue, OperationType.Subtract, costShare, CostDecoratorId, Id));
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator(
                AbilityParameter.Cooldown, Priority.Weak, OperationType.Add, cooldownTurns, CooldownDecoratorId, Id));
        }

        public override void RemoveUpgrade(Ability ability)
        {
            ability.RemoveParameterDecorator(CostDecoratorId, AbilityParameter.CostValue);
            ability.RemoveParameterDecorator(CooldownDecoratorId, AbilityParameter.Cooldown);
        }

        public override IAbilityAugment Copy() =>
            new AbilityAugmentReduceCostAddCooldown(Id, Tags, Tier, costShare, cooldownTurns);
    }
}
