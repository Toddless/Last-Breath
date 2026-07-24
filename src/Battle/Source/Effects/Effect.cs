namespace Battle.Source.Effects
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;
    using Core.Localization;
    using Godot;

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

        public IFightable? Target { get; protected set; }
        public string Id { get; } = id;
        public string InstanceId { get; } = Guid.NewGuid().ToString();

        public Texture2D? Icon
        {
            get
            {
                if (field != null) return field;
                field = ResourceLoader.Load<Texture2D>($"res://Data/Shared/Assets/Effects/{Id}.png");
                return field;
            }
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
            // Caster-side application pipeline: item/passive mutators tune the instance
            // (duration, DoT tick) before the stacking rules see it. Descendants have already
            // derived their numbers from the applying context at this point.
            if (context is { IsBonusStack: false })
            {
                var application = new EffectApplicationContext(context.Caster, context.Target, this);
                context.Caster.ModifierHandler.Apply(application);
                // Bonus stacks clone the already-mutated instance and skip the pipeline,
                // so every mutator applies exactly once per stack and cannot recurse.
                for (int i = 0; i < application.BonusStacks; i++)
                    _ = Copy().Apply(context with { IsBonusStack = true });
            }

            // Target-side pipeline sees the final outgoing instance: resistances shorten it or resist outright.
            var incoming = new IncomingEffectContext(context.Caster, context.Target, this);
            context.Target.ModifierHandler.Apply(incoming);
            if (incoming.Rejected)
            {
                context.Target.CombatEvents.Publish(new EffectResistedEvent(this, context.Target, context.Caster));
                return Task.CompletedTask;
            }

            Context = context;
            Target = context.Target;
            Source = context.Source;
            // AddEffect reports whether the stacking rules accepted THIS instance; a rejected
            // single-stack re-application only refreshes the existing effect's duration.
            IsApplied = Target.Effects.AddEffect(this);
            if (IsApplied) Target.CombatEvents.Publish(new EffectAppliedEvent(this, Target, context.Caster));
            // here we need to notify caster that he applied some effect. Target will get notified within TryApplyStatusEffect
            if (Target.TryApplyStatusEffect(Status)) context.Caster.CombatEvents.Publish(new StatusEffectAppliedEvent(Status));
            return Task.CompletedTask;
        }

        public virtual void Remove()
        {
            ClearSubscriptions();

            var target = Target;
            Target = null;
            Context = null;
            if (target == null) return;

            target.Effects.RemoveEffect(this);
            RemoveStatusIfLastCarrier(target);
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

        public bool IsSame(string otherId) => Id.Equals(otherId);

        public virtual bool IsStronger(IEffect otherEffect) => false;

        public abstract IEffect Copy();

        /// <summary>
        /// Subscribes to a combat event bus;  protected void SubscribeUntilRemoved<T>(ICombatEventBus bus, Action<T> handler)
        protected void SubscribeUntilRemoved<T>(ICombatEventBus bus, Action<T> handler)
            where T : ICombatEvent
        {
            if (!IsApplied) return; // rejected stacks must not react to anything
            bus.Subscribe(handler);
            _unsubscribes.Add(() => bus.Unsubscribe(handler));
        }

        /// <summary>
        /// Named values for the description template. The base gives every effect {Duration}
        /// (usable as {Duration|turn|turns}) and {MaxStacks}; descendants extend the dictionary
        /// with their own values ({Damage}, {Stacks}...) on top of base.DescriptionValues.
        /// </summary>
        protected virtual Dictionary<string, object?> DescriptionValues => new() { [nameof(Duration)] = Duration, [nameof(MaxStacks)] = MaxStacks, };

        protected virtual string FormatDescription() => Localization.RenderDescription(Id, DescriptionValues, TextFormat.Rich);

        private void ClearSubscriptions()
        {
            foreach (Action unsubscribe in _unsubscribes) unsubscribe();
            _unsubscribes.Clear();
        }

        /// <summary>
        /// The status flag is shared between effects: it must go away only with the last effect carrying it.
        /// Called after this effect is removed from the target's list, so remaining carriers are counted correctly.
        /// </summary>
        private void RemoveStatusIfLastCarrier(IFightable target)
        {
            if (Status == StatusEffects.None) return;
            bool anotherCarrierExists = target.Effects.GetBy(e => e.Status == Status).Any();
            if (!anotherCarrierExists) target.TryRemoveStatusEffect(Status);
        }
    }
}
