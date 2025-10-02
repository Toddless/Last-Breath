namespace LastBreath.Script.Abilities
{
    using System;
    using Core.Interfaces;
    using Core.Interfaces.Abilities;
    using Godot;

    public abstract class EffectBase(Core.Enums.Effects effect, int duration = 3, int stacks = 1, bool permanent = false) : IEffect
    {
        public Core.Enums.Effects Effect { get; } = effect;
        public IItemModifier? Modifier { get; protected set; }
        public int Duration { get; set; } = duration;
        public int Stacks { get; set; } = stacks;
        public bool Permanent { get; } = permanent;

        public bool Expired => !Permanent && Duration < 1;

        public virtual void OnApply(ICharacter character)
        {
            if (Modifier != null)
                character.Modifiers.AddTemporaryModifier(Modifier);
        }

        public virtual void OnRemove(ICharacter character)
        {
            if (Modifier != null)
                character.Modifiers.RemoveTemporaryModifier(Modifier);
        }

        public virtual void OnTick(ICharacter character)
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
