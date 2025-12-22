namespace Battle.Source.Abilities.Effects
{
    using Core.Enums;
    using Core.Interfaces.Abilities;

    public class RegenerationEffect(
        float amount,
        int duration,
        int stacks,
        StatusEffects statusEffect = StatusEffects.Regeneration)
        : Effect(id:"Effect_Regeneration", duration, stacks, statusEffect)
    {
        public float Amount { get; } = amount;

        public override void TurnEnd()
        {
            Owner?.Heal(Amount);
            base.TurnEnd();
        }

        public override IEffect Clone() => new RegenerationEffect(Amount, Duration, MaxStacks, Status);
    }
}
