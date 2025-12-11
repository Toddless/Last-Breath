namespace Battle.Source.Abilities.Effects
{
    using Godot;
    using Utilities;
    using Core.Enums;
    using CombatEvents;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Abilities;

    public abstract class Effect(
        string id,
        int duration,
        int stacks,
        StatusEffects statusEffect = StatusEffects.None) : IEffect
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

        public virtual void Apply(EffectApplyingContext context)
        {
            var target = context.Target;
            Source = context.Source;
            target.Effects.AddEffect(this);
            // here we need to notify caster, that he applied some effect. Target will get notified within TryApplyStatusEffect
            if (target.TryApplyStatusEffect(Status))
                context.Caster.Events.Publish(new StatusEffectAppliedEvent(context.Caster, Status));
        }

        public virtual void TurnEnd(IEntity source)
        {
            Duration--;
            if (Expired) Remove(source);
        }

        public virtual void TurnStart(IEntity source)
        {
        }

        public virtual void BeforeAttack(IEntity source, IAttackContext context)
        {
        }

        public virtual void AfterAttack(IEntity source, IAttackContext context)
        {
        }

        public virtual bool IsStronger(IEffect otherEffect) => false;
        public abstract IEffect Clone();

        public virtual void Remove(IEntity source)
        {
            // effect will be removed form "us" here, so we are publishing event within TryRemoveStatusEffect
            source.TryRemoveStatusEffect(Status);
            source.Effects.RemoveEffect(this);
        }
    }
}
