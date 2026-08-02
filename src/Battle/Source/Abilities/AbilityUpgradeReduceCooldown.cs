namespace Battle.Source.Abilities
{
    using Core.Battle.Abilities;

    /// <summary>
    /// Takes a share off the ability's cooldown, for the reason the cost augment states: one record now
    /// serves the whole book, and the waits it goes on run from no turns at all to nine. Turns are
    /// whole, so what the share comes to is rounded and floored at one turn, and it is measured against
    /// the ability's own base wait — see <see cref="AbilityParameterShareCut"/> — so the augment cuts
    /// the same number of turns whatever else is worn beside it.
    /// </summary>
    public class AbilityUpgradeReduceCooldown(string id, string[] tags, int tier, float cooldownShare)
        : AbilityUpgrade<Ability>(id, tags, tier)
    {
        private string DecoratorId => $"Ability_Parameter_Decorator_{Id}";

        public override void ApplyUpgrade(Ability ability) =>
            ability.AddParameterDecorator(new AbilityParameterShareCut(
                AbilityParameter.Cooldown,
                cooldownShare,
                DecoratorId,
                Id));

        public override void RemoveUpgrade(Ability ability) =>
            ability.RemoveParameterDecorator(DecoratorId, AbilityParameter.Cooldown);

        public override IAbilityUpgrade Copy() => new AbilityUpgradeReduceCooldown(Id, Tags, Tier, cooldownShare);
    }
}
