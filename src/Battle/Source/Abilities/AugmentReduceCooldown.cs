namespace Battle.Source.Abilities
{
    using Core.Battle.Abilities;
    using Core.Enums;

    /// <summary>
    /// Takes whole turns off the ability's cooldown. Turns and not a share, because the design line says
    /// turns: a wait is counted, a player reads "two turns sooner" and gets two turns sooner on every
    /// ability it is worn on. What is left is a wait of at least
    /// <see cref="AbilityParameter.MinimumCooldown"/> — the record sells a shorter cooldown, never the
    /// removal of one, and a flat cut without that floor would drive a short wait below zero, where it
    /// never counts back down and the ability can never be cast again. An ability written to wait for
    /// nothing is untouched: the floor holds the cut back and hands out no wait the data never asked for.
    /// </summary>
    public class AugmentReduceCooldown(string id, string[] tags, int tier, float cooldownTurns)
        : Augment<Ability>(id, tags, tier)
    {
        private string DecoratorId => $"Ability_Parameter_Decorator_{Id}";

        public override void ApplyUpgrade(Ability ability) =>
            ability.AddParameterDecorator(new SimpleAbilityParameterDecorator(
                AbilityParameter.Cooldown,
                Priority.Weak,
                OperationType.Subtract,
                cooldownTurns,
                DecoratorId,
                Id,
                floor: AbilityParameter.MinimumCooldown));

        public override void RemoveUpgrade(Ability ability) =>
            ability.RemoveParameterDecorator(DecoratorId, AbilityParameter.Cooldown);

        public override IAugment Copy() => new AugmentReduceCooldown(Id, Tags, Tier, cooldownTurns);
    }
}
