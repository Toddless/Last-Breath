namespace Battle.TestData.Abilities.Effects
{
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Abilities;

    public class RegenerationEffect(string id, int duration, int stacks, StatusEffects statusEffect = StatusEffects.None)
        : Effect(id, duration, stacks, statusEffect)
    {
        public float Amount { get; set; }

        public override void OnTurnEnd(IEntity target)
        {
            target.Heal(Amount);
            base.OnTurnEnd(target);
        }

        public override IEffect Clone() => new RegenerationEffect(Id, Duration, MaxStacks, Status) { Amount = Amount };
    }
}
