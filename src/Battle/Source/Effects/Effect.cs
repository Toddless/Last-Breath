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
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Events;
    using Core.Localization;
    using Core.Services;
    using Godot;

    public abstract class Effect(
        string id,
        int duration,
        int maxStacks,
        StatusEffects statusEffect = StatusEffects.None) : IEffect
    {
        private readonly List<Action> _unsubscribes = [];

        /// <summary>Turns this instance has already been given by extensions, all sources together.</summary>
        private int _extendedTurns;

        /// <summary>Resolved on first use rather than in the constructor: effects are built in bulk
        /// (every copy of every stack) while extensions are rare, and the rules are the same file all
        /// the way through a fight.</summary>
        private int? _extensionBudget;

        /// <summary>
        /// The ceiling applied to what an instance claims: the smaller of what was asked for and what the
        /// canon balances the effect at. A cast may drive its own <see cref="AbilityParameter.Stacks"/> as
        /// high as records take it; what it LAYS stops at the number the effect is balanced for. Asked once,
        /// at construction, so the stacking rule, the counters and every copy read one number.
        /// <para>The registry is pulled from the composition at the moment of the question, like
        /// <see cref="ResolveExtensionBudget"/> and for the same reasons: nothing holds a provider instance
        /// that may have been discarded, and no order of loading decides the answer. An effect the canon
        /// leaves out (Docs/PLAN-Augments §4f) and a host that composes no services at all both mean no
        /// ceiling — the ability's own number stands.</para>
        /// </summary>
        private static int Capped(string effectId, int requested)
        {
            int? ceiling = GameServiceProvider.TryGet<IEffectProvider>()?.StackCeilingOf(effectId);
            return ceiling == null ? requested : Math.Min(requested, ceiling.Value);
        }

        /// <summary>The strength the canon gives this id, read the same way and for the same reasons as
        /// <see cref="Capped"/>: at construction, so every copy of every stack answers one number, and out
        /// of the composition, so an effect built where no canon was loaded is simply weak.</summary>
        private static EffectPower PowerOf(string effectId) =>
            GameServiceProvider.TryGet<IEffectProvider>()?.PowerOf(effectId) ?? EffectPower.Weak;

        protected EffectApplyingContext? Context { get; private set; }

        /// <summary>True when the stacking rules actually accepted this instance (see EffectsComponent).</summary>
        protected bool IsApplied { get; private set; }

        public IFightable? Target { get; protected set; }
        public string Id { get; } = id;
        public string InstanceId { get; } = Guid.NewGuid().ToString();

        /// <summary>Buff/debuff split for the UI counters; debuff classes override to true.</summary>
        public virtual bool IsHarmful => false;

        /// <summary>What strength of dispel takes this effect off, as the canon names it.</summary>
        public virtual EffectPower Power { get; } = PowerOf(id);

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

        public int MaxStacks { get; set; } = Capped(id, maxStacks);

        /// <summary>
        /// Turns this instance may gain from <see cref="Extend"/> in total, taken from the combat
        /// rules. The budget belongs to the INSTANCE: two stacks of one effect carry a budget each,
        /// so a second stack is worth stacking rather than a share of the first one's room.
        /// A copy is a new instance with a budget of its own; a spread or a transfer carries the
        /// duration already extended into it, which is deliberate — what moves is the effect as it
        /// stands, and the room to extend it further is the new instance's own.
        /// </summary>
        public int ExtensionBudget => _extensionBudget ??= ResolveExtensionBudget();

        public string Source { get; private set; } = string.Empty;

        /// <summary>Effectiveness of the cast that laid this instance; one for anything not laid by a
        /// cast. Descendants that derive numbers BEFORE <c>base.Apply</c> stamp it themselves.</summary>
        public float Effectiveness { get; protected set; } = 1f;

        /// <summary>The cast that laid this instance; nothing at all when a passive, a grant or a stage
        /// did. Stamped beside <see cref="Effectiveness"/> and read by the records that prolong or
        /// strengthen only what their own ability put there.</summary>
        public AbilityTrace Trace { get; protected set; }

        public bool Expired => Duration == 0;
        public string Description => FormatDescription();
        public string DisplayName => Localization.Localize(Id);

        public event Action<int>? DurationChanged;

        public virtual Task Apply(EffectApplyingContext context)
        {
            // Stamped before anything reads a number off this instance — including the mutator
            // pipelines below, which see the effect as it will actually land.
            Effectiveness = context.Effectiveness;
            Trace = context.Trace;

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

        /// <summary>
        /// The one road that makes a standing effect last longer. Every extension is charged to this
        /// instance's <see cref="ExtensionBudget"/>, so no number of extenders — riders, augments, an
        /// ability extending what it touched — can keep an effect alive for the rest of the fight.
        /// The boundary the budget is drawn at: EXTENSION happens to an effect that has already
        /// landed, APPLICATION shapes the duration an effect lands with (duration scaling, flat
        /// duration bonuses, control resistance, a re-application refreshing the standing stack) and
        /// pays nothing here — those mutators run before there is an instance standing to extend.
        /// An effect whose duration has run out is not extended either: it has lived its last turn and
        /// lies in the list until the turn end takes it away, and lengthening it there would be a
        /// resurrection rather than an extension.
        /// </summary>
        /// <param name="turns">Turns asked for. Nothing and less is not an extension and costs nothing.</param>
        /// <returns>Turns actually added: what was left of the budget when it runs short, zero once it
        /// is spent. A caller reads it to tell a real extension from one the cap swallowed.</returns>
        public int Extend(int turns)
        {
            if (turns <= 0 || Duration <= 0) return 0;

            int granted = Math.Min(turns, ExtensionBudget - _extendedTurns);
            if (granted <= 0) return 0;

            _extendedTurns += granted;
            Duration += granted;
            return granted;
        }

        public virtual bool IsStronger(IEffect otherEffect) => false;

        public abstract IEffect Copy();

        /// <summary>What an authored figure is worth on this instance — the ONE place effectiveness is
        /// applied. Durations and stacks are separate axes and never come through here.</summary>
        protected float Effective(EffectValue value, EffectValueShape shape = EffectValueShape.Plain) => shape switch
        {
            EffectValueShape.ShareGained => 1f + (value.Authored * Effectiveness),
            EffectValueShape.ShareLost => MathF.Max(0f, 1f - (value.Authored * Effectiveness)),
            _ => value.Authored * Effectiveness
        };

        /// <summary> Subscribes to a combat event bus.</summary>
        protected void SubscribeUntilRemoved<T>(ICombatEventBus bus, Action<T> handler)
            where T : ICombatEvent
        {
            if (!IsApplied) return; // rejected stacks must not react to anything
            bus.Subscribe(handler);
            _unsubscribes.Add(() => bus.Unsubscribe(handler));
        }

        /// <summary>Watches the effects standing beside this one leave the bearer, for as long as this one
        /// stands. The same road as the bus overload and dropped by the same removal — an effect chained to
        /// a neighbour must not outlive its own subscription and go on answering for a bearer it left.</summary>
        protected void SubscribeUntilRemoved(IEffectsComponent effects, Action<IEffect> onNeighbourRemoved)
        {
            if (!IsApplied) return; // rejected stacks must not react to anything
            effects.EffectRemoved += onNeighbourRemoved;
            _unsubscribes.Add(() => effects.EffectRemoved -= onNeighbourRemoved);
        }

        /// <summary>
        /// Named values for the description template. The base gives every effect {Duration}
        /// (usable as {Duration|turn|turns}) and {MaxStacks}; descendants extend the dictionary
        /// with their own values ({Damage}, {Stacks}...) on top of base.DescriptionValues.
        /// </summary>
        protected virtual Dictionary<string, object?> DescriptionValues => new() { [nameof(Duration)] = Duration, [nameof(MaxStacks)] = MaxStacks, };

        protected virtual string FormatDescription() => Localization.RenderDescription(Id, DescriptionValues, TextFormat.Rich);

        /// <summary>The budget the combat rules give, or the working default when there are no rules to
        /// reach: effects are built by hosts that compose no services at all, and a cap that quietly
        /// disappears there would be a cap that never held.</summary>
        private static int ResolveExtensionBudget() =>
            GameServiceProvider.TryGet<ICombatRulesProvider>()?.Effects.MaxExtendedTurns
            ?? EffectRules.Default.MaxExtendedTurns;

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
