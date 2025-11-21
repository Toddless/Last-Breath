namespace Battle.TestData.Abilities.Effects
{
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Entity;

    public class DamageEffect : Effect
    {
        public int MinDamage { get; init; }
        public int MaxDamage { get; init; }

        public override void OnApply(IEntity target, IEntity source, AbilityContext context)
        {
        }

        public override IEffect Clone() => new DamageEffect
        {
            Id = Id,
            Duration = Duration,
            StatusEffect = StatusEffect,
            Stacks = Stacks,
            MinDamage = MinDamage,
            MaxDamage = MaxDamage,
        };
    }
}
