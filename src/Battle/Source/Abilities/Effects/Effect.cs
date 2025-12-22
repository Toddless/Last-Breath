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
        int stacks,
        StatusEffects statusEffect = StatusEffects.None) : IEffect
    {
        protected EffectApplyingContext? Context { get; private set; }
        protected IEntity? Owner;
        public string Id { get; } = statusEffect == StatusEffects.None ? id : $"{id}_{statusEffect}";
        public string InstanceId { get; } = Guid.NewGuid().ToString();

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
        public string Source { get; private set; } = string.Empty;
        public bool Expired => Duration == 0;
        public string Description => Localizator.LocalizeDescription(Id);
        public string DisplayName => Localizator.Localize(Id);

        public event Action<int>? DurationChanged;

        public virtual void Apply(EffectApplyingContext context)
        {
            Context = context;
            var target = context.Target;
            var caster = context.Caster;
            Owner = target;
            Source = context.Source;
            target.Effects.AddEffect(this);
            Owner.Dead += OnOwnerDead;
            caster.Dead += OnCasterDead;
            // here we need to notify caster, that he applied some effect. Target will get notified within TryApplyStatusEffect
            if (target.TryApplyStatusEffect(Status)) caster.CombatEvents.Publish(new StatusEffectAppliedEvent(Status));
        }

        private void OnCasterDead(IEntity target)
        {
            if (Context.HasValue) Context.Value.Caster.Dead -= OnCasterDead;
            Owner?.Effects.RemoveEffect(this);
            Context = null;
        }

        private void OnOwnerDead(IFightable owner)
        {
            Owner?.Dead -= OnOwnerDead;
            Owner = null;
        }

        public virtual void TurnEnd()
        {
            Duration--;
            DurationChanged?.Invoke(Duration);
            if (Expired) Remove();
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
        }
    }
}
