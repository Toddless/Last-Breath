namespace Battle.TestData.Abilities.Effects
{
    using Godot;
    using Utilities;
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Abilities;

    public abstract class Effect(string id, int duration, int stacks, StatusEffects statusEffect = StatusEffects.None)
        : IEffect
    {
        public string Id { get; } = statusEffect == StatusEffects.None ? id : $"{id}_{statusEffect}";

        public Texture2D? Icon
        {
            get;
            // {
            //     // TODO: I think I should change it later
            //    if (field != null) return field;
            //     field = ResourceLoader.Load<Texture2D>($"Abilities/Effects/{Id}.png");
            //     return field;
            // }
        }

        public StatusEffects Status { get; set; } = statusEffect;
        public int Duration { get; set; } = duration;
        public int MaxStacks { get; set; } = stacks;
        public object? Source { get; private set; }
        public bool Expired => Duration == 0;
        public string Description => Localizator.LocalizeDescription(Id);
        public string DisplayName => Localizator.Localize(Id);

        public virtual void OnApply(EffectApplyingContext context)
        {
            var target = context.Target;
            Source = context.Source;
            target.Effects.AddEffect(this);
            target.StatusEffects |= Status;
        }

        public virtual void OnTurnEnd(IEntity target)
        {
            Duration--;
            if (Expired) Remove(target);
        }

        public virtual void OnTurnStart(IEntity target)
        {
        }

        public virtual void OnBeforeAttack(IEntity target, IAttackContext context)
        {
        }

        public virtual void OnAfterAttack(IEntity target, IAttackContext context)
        {
        }

        public virtual bool IsStronger(IEffect otherEffect) => false;
        public abstract IEffect Clone();

        public virtual void Remove(IEntity target)
        {
            target.StatusEffects &= ~Status;
            target.Effects.RemoveEffect(this);
        }
    }
}
