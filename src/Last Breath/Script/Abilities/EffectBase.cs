namespace LastBreath.Script.Abilities
{
    using Godot;
    using System;
    using Core.Interfaces;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Entity;

    public abstract class EffectBase(Core.Enums.Effects effect, int duration = 3, int stacks = 1, bool permanent = false) : IEffect
    {
        public Core.Enums.Effects Effect { get; } = effect;
        public IModifierInstance? Modifier { get; protected set; }
        public int Duration { get; set; } = duration;
        public int Stacks { get; set; } = stacks;
        public bool Permanent { get; } = permanent;

        public bool Expired => !Permanent && Duration < 1;

        public virtual void OnApply(IEntity character)
        {
            character.Modifiers.AddTemporaryModifier(Modifier);
        }

        public virtual void OnRemove(IEntity character)
        {
            character.Modifiers.RemoveTemporaryModifier(Modifier);
        }

        public virtual void OnTick(IEntity character)
        {
            if (!Permanent) Duration--;
            GD.Print($"Effect: {GetType().Name} Duration: {Duration}");
        }

        public virtual void OnStacks(IEffect newEffect)
        {
            Duration = Math.Max(Duration, newEffect.Duration);
            Stacks += newEffect.Stacks;
        }
    }
}
