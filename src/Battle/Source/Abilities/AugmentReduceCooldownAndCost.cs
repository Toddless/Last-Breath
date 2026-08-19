namespace Battle.Source.Abilities
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>
    /// Shortens the wait and lowers the price — the bargain with no bill attached. The two halves are
    /// stated in the two different shapes their design lines use, and that is why this is a class rather
    /// than a row of the parameter table: turns are counted, so the wait comes off as whole turns, while
    /// a price runs from nothing to five hundred across the book and only a SHARE of it means the same
    /// thing everywhere.
    /// <para>The flat cut is floored at <see cref="AbilityParameter.MinimumCooldown"/>. A general record
    /// taking whole turns off any wait it is seated on would otherwise drive a short one below zero, and
    /// a negative wait never counts back down to nought — the ability would be uncastable for the rest of
    /// the fight. The design line for this record does not spell the floor out; the two cooldown lines
    /// beside it do, and the mechanism leaves no honest alternative.</para>
    /// </summary>
    public class AugmentReduceCooldownAndCost(string id, string[] tags, int tier, float cooldownTurns, float costShare)
        : Augment<Ability>(id, tags, tier)
    {
        private string CooldownDecoratorId => $"Ability_Parameter_Decorator_{Id}_Cooldown";
        private string CostDecoratorId => $"Ability_Parameter_Decorator_{Id}_Cost";

        public override void ApplyUpgrade(Ability ability)
        {
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator(
                AbilityParameter.Cooldown, Priority.Weak, OperationType.Subtract, cooldownTurns,
                CooldownDecoratorId, Id, floor: AbilityParameter.MinimumCooldown));
            ability.AddParameterDecorator(new AbilityParameterShare(
                AbilityParameter.CostValue, OperationType.Subtract, costShare, CostDecoratorId, Id));
        }

        public override void RemoveUpgrade(Ability ability)
        {
            ability.RemoveParameterDecorator(CooldownDecoratorId, AbilityParameter.Cooldown);
            ability.RemoveParameterDecorator(CostDecoratorId, AbilityParameter.CostValue);
        }

        public override IAugment Copy() =>
            new AugmentReduceCooldownAndCost(Id, Tags, Tier, cooldownTurns, costShare);
    }
}
