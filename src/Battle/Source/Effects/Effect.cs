namespace Battle.Source.Effects
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events.GameEvents;
    using Godot;
    using Utilities;

    public abstract class Effect(
        string id,
        int duration,
        int maxStacks,
        StatusEffects statusEffect = StatusEffects.None) : IEffect
    {
        private readonly List<Action> _unsubscribes = [];

        protected EffectApplyingContext? Context { get; private set; }
        /// <summary>True when the stacking rules actually accepted this instance (see EffectsComponent).</summary>
        protected bool IsApplied { get; private set; }
        public IEntity? Target { get; protected set; }
        public string Id { get; } = id;
        public string InstanceId { get; } = Guid.NewGuid().ToString();

        public Texture2D? Icon
        {
            get;
            // {
            //     if (field != null) return field;
            //     field = ResourceLoader.Load<Texture2D>($"res://Source/Abilities/Effects/{Id}.png");
            //     return field;
            // }
        }

        public StatusEffects Status { get; set; } = statusEffect;

        public int Duration
        {
            get;
            set
            {
                if (field == value) return;
                field = value;
                DurationChanged?.Invoke(field);
            }
        } = duration;

        public int MaxStacks { get; set; } = maxStacks;
        public string Source { get; private set; } = string.Empty;
        public bool Expired => Duration == 0;
        public string Description => FormatDescription();
        public string DisplayName => Localization.Localize(Id);

        public event Action<int>? DurationChanged;

        public virtual Task Apply(EffectApplyingContext context)
        {
            Context = context;
            var target = context.Target;
            var caster = context.Caster;
            Target = target;
            Source = context.Source;
            Target.Effects.AddEffect(this);
            // Single-stack rules may reject this instance in favor of a stronger existing one
            IsApplied = Target.Effects.GetBy(e => e.IsSame(InstanceId)).Any();
            // here we need to notify caster that he applied some effect. Target will get notified within TryApplyStatusEffect
            if (target.TryApplyStatusEffect(Status)) caster.CombatEvents.Publish(new StatusEffectAppliedEvent(Status));
            return Task.CompletedTask;
        }

        public virtual void Remove()
        {
            foreach (Action unsubscribe in _unsubscribes) unsubscribe();
            _unsubscribes.Clear();

            Target?.TryRemoveStatusEffect(Status);
            Target?.Effects.RemoveEffect(this);
            Target = null;
            Context = null;
        }

        public virtual void OnStackChanged(int currentStack)
        {
        }

        public virtual void TurnEnd()
        {
            if (Expired) Remove();
            Duration--;
        }

        public virtual void TurnStart()
        {
        }

        public bool IsSame(string otherId) => InstanceId.Equals(otherId);

        public virtual bool IsStronger(IEffect otherEffect) => false;

        public abstract IEffect Copy();

        /// <summary>
        /// Subscribes to a combat event bus; the subscription is removed automatically in <see cref="Remove"/>.
        /// Use for effect reactions (e.g. "on my attack, apply a stack") instead of manual Subscribe/Unsubscribe pairs.
        /// </summary>
        protected void SubscribeUntilRemoved<T>(ICombatEventBus bus, Action<T> handler)
            where T : ICombatEvent
        {
            if (!IsApplied) return; // rejected stacks must not react to anything
            bus.Subscribe(handler);
            _unsubscribes.Add(() => bus.Unsubscribe(handler));
        }

        protected virtual string FormatDescription() => Localization.LocalizeDescription(Id);
    }
}
