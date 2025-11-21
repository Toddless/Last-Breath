namespace Battle.TestData.Abilities.Effects
{
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Entity;

    public abstract class Effect : IEffect
    {
        public string Id { get; set; } = string.Empty;
        public StatusEffects StatusEffect { get; set; } = StatusEffects.None;
        public int Duration { get; set; } = 3;
        public int Stacks { get; set; }
        public bool Permanent => Duration <= 0;
        public bool Expired => Duration == 0;

        public virtual void OnApply(IEntity target, IEntity source, AbilityContext context)
        {
        }

        public virtual void OnTick(IEntity target)
        {
        }

        public virtual void OnRemove(IEntity target)
        {
        }

        public virtual void OnStacks(IEffect newEffect)
        {
        }

        public abstract IEffect Clone();
    }
}
