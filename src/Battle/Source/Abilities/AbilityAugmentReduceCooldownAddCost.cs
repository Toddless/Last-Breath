namespace Battle.Source.Abilities
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>
    /// Shortens the ability's wait and charges more for the cast, both stated as a share of what the
    /// ability is written with. Shares and not numbers of their own, because one record serves the
    /// whole book: the waits it goes on run from no turns at all to nine and the prices from nothing to
    /// five hundred, and a flat pair of figures would be a rewritten ability at one end and a change
    /// nobody notices at the other. Both shares are measured against the ability's own base numbers and
    /// rounded there — see <see cref="AbilityParameterShare"/> — so the turns taken off and the points
    /// added on are the same whatever else is worn beside this augment. The wait it leaves is at least
    /// <see cref="AbilityParameter.MinimumCooldown"/> — the same floor the plain cut honours, and the
    /// same reason: the record charges for a shorter cooldown, not for the removal of one.
    /// </summary>
    public class AbilityAugmentReduceCooldownAddCost(string id, string[] tags, int tier, float cooldownShare, float costShare)
        : AbilityAugment<Ability>(id, tags, tier)
    {
        private string CooldownDecoratorId => $"Ability_Parameter_Decorator_{Id}_Cooldown";
        private string CostDecoratorId => $"Ability_Parameter_Decorator_{Id}_Cost";

        public override void ApplyUpgrade(Ability ability)
        {
            ability.AddParameterDecorator(new AbilityParameterShare(
                AbilityParameter.Cooldown, OperationType.Subtract, cooldownShare, CooldownDecoratorId, Id,
                floor: AbilityParameter.MinimumCooldown));
            ability.AddParameterDecorator(new AbilityParameterShare(
                AbilityParameter.CostValue, OperationType.Add, costShare, CostDecoratorId, Id));
        }

        public override void RemoveUpgrade(Ability ability)
        {
            ability.RemoveParameterDecorator(CooldownDecoratorId, AbilityParameter.Cooldown);
            ability.RemoveParameterDecorator(CostDecoratorId, AbilityParameter.CostValue);
        }

        public override IAbilityAugment Copy() => new AbilityAugmentReduceCooldownAddCost(Id, Tags, Tier, cooldownShare, costShare);
    }
}
