namespace Battle.Source.Abilities.Effects
{
    using Godot;
    using System;
    using Utilities;
    using Core.Enums;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Events.GameEvents;

    public abstract class Effect(
        string id,
        int duration,
        int maxStacks,
        StatusEffects statusEffect = StatusEffects.None) : IEffect
    {
        protected EffectApplyingContext? Context { get; private set; }
        public IEntity? Owner { get; protected set; }
        public string Id { get; } = statusEffect == StatusEffects.None ? id : $"{id}_{statusEffect}";
        public string InstanceId { get; } = Guid.NewGuid().ToString();

        public Texture2D? Icon
        {
            get
            {
                if (field != null) return field;
                field = ResourceLoader.Load<Texture2D>($"res://Source/Abilities/Effects/{Id}.png");
                return field;
            }
        }

        public StatusEffects Status { get; set; } = statusEffect;
        public int Duration { get; set; } = duration;
        public int MaxMaxStacks { get; set; } = maxStacks;
        public string Source { get; private set; } = string.Empty;
        public bool Expired => Duration == 0;
        public string Description => FormatDescription();
        public string DisplayName => Localization.Localize(Id);

        public event Action<int>? DurationChanged;

        public virtual void Apply(EffectApplyingContext context)
        {
            Context = context;
            var target = context.Target;
            var caster = context.Caster;
            Owner = target;
            Source = context.Source;
            target.Effects.AddEffect(this);
            // here we need to notify caster, that he applied some effect. Target will get notified within TryApplyStatusEffect
            if (target.TryApplyStatusEffect(Status)) caster.CombatEvents.Publish(new StatusEffectAppliedEvent(Status));
        }

        public virtual void TurnEnd()
        {
            if (Expired) Remove();
            Duration--;
            DurationChanged?.Invoke(Duration);
        }

        public virtual void TurnStart()
        {
        }

        public virtual void BeforeAttack(IAttackContext context)
        {
        }

        public virtual void AfterAttack(IAttackContext context)
        {
        }

        public bool IsSame(string otherId) => InstanceId.Equals(otherId);

        public virtual bool IsStronger(IEffect otherEffect) => false;

        public abstract IEffect Clone();

        public virtual void Remove()
        {
            // effect will be removed form "us" here, so we are publishing event within TryRemoveStatusEffect
            Owner?.TryRemoveStatusEffect(Status);
            Owner?.Effects.RemoveEffect(this);
            Owner = null;
            Context = null;
        }

        protected virtual string FormatDescription() => Localization.LocalizeDescription(Id);
    }
}
