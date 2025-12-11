namespace Battle.Source.Abilities.Effects
{
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Entity;

    public class RegenerationEffect(string id, int duration, int stacks, StatusEffects statusEffect = StatusEffects.None)
        : Effect(id, duration, stacks, statusEffect)
    {
        public float Amount { get; set; }

        public override void TurnEnd(IEntity source)
        {
            source.Heal(Amount);
            base.TurnEnd(source);
        }

        public override IEffect Clone() => new RegenerationEffect(Id, Duration, MaxStacks, Status) { Amount = Amount };
    }
}
