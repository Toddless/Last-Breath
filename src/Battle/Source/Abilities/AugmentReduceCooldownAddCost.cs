namespace Battle.Source.Abilities
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>
    /// Shortens the ability's wait and charges more for the cast, stated in the two forms the design
    /// line uses: the wait in whole TURNS, because turns are counted and a player reads "two turns
    /// sooner"; the price as a SHARE of what the ability charges, because one record serves the whole
    /// book and the prices it goes on run from nothing to five hundred, where a flat figure would be a
    /// rewritten ability at one end and a change nobody notices at the other. The share is measured
    /// against the ability's own base and rounded there — see <see cref="AbilityParameterShare"/> — so
    /// the points added on are the same whatever else is worn beside this augment. The wait it leaves is
    /// at least <see cref="AbilityParameter.MinimumCooldown"/> — the same floor the plain cut honours,
    /// and the same reason: the record charges for a shorter cooldown, not for the removal of one.
    /// </summary>
    public class AugmentReduceCooldownAddCost(string id, string[] tags, int tier, float cooldownTurns, float costShare)
        : Augment<Ability>(id, tags, tier)
    {
        private string CooldownDecoratorId => $"Ability_Parameter_Decorator_{Id}_Cooldown";
        private string CostDecoratorId => $"Ability_Parameter_Decorator_{Id}_Cost";

        public override void ApplyUpgrade(Ability ability)
        {
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator(
                AbilityParameter.Cooldown, Priority.Weak, OperationType.Subtract, cooldownTurns, CooldownDecoratorId, Id,
                floor: AbilityParameter.MinimumCooldown));
            ability.AddParameterDecorator(new AbilityParameterShare(
                AbilityParameter.CostValue, OperationType.Add, costShare, CostDecoratorId, Id));
        }

        public override void RemoveUpgrade(Ability ability)
        {
            ability.RemoveParameterDecorator(CooldownDecoratorId, AbilityParameter.Cooldown);
            ability.RemoveParameterDecorator(CostDecoratorId, AbilityParameter.CostValue);
        }

        public override IAugment Copy() => new AugmentReduceCooldownAddCost(Id, Tags, Tier, cooldownTurns, costShare);
    }
}
