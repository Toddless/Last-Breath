namespace Battle.Source.Abilities
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>
    /// Takes a share off the ability's cooldown, for the reason the cost augment states: one record now
    /// serves the whole book, and the waits it goes on run from no turns at all to nine. Turns are
    /// whole, so what the share comes to is rounded and floored at one turn, and it is measured against
    /// the ability's own base wait — see <see cref="AbilityParameterShare"/> — so the augment cuts
    /// the same number of turns whatever else is worn beside it. What is left is a wait of at least
    /// <see cref="AbilityParameter.MinimumCooldown"/>: the augment sells a shorter cooldown, never the
    /// removal of one. An ability written to wait for nothing is untouched — the floor holds the cut
    /// back and does not hand out a wait the data never asked for.
    /// </summary>
    public class AbilityUpgradeReduceCooldown(string id, string[] tags, int tier, float cooldownShare)
        : AbilityUpgrade<Ability>(id, tags, tier)
    {
        private string DecoratorId => $"Ability_Parameter_Decorator_{Id}";

        public override void ApplyUpgrade(Ability ability) =>
            ability.AddParameterDecorator(new AbilityParameterShare(
                AbilityParameter.Cooldown,
                OperationType.Subtract,
                cooldownShare,
                DecoratorId,
                Id,
                floor: AbilityParameter.MinimumCooldown));

        public override void RemoveUpgrade(Ability ability) =>
            ability.RemoveParameterDecorator(DecoratorId, AbilityParameter.Cooldown);

        public override IAbilityUpgrade Copy() => new AbilityUpgradeReduceCooldown(Id, Tags, Tier, cooldownShare);
    }
}
