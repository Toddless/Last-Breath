namespace LastBreathTest.BattleSystemTests
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading.Tasks;
    using Core;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Context;
    using Core.Data;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Events;
    using Core.Interfaces;
    using Core.Items;
    using Godot;

    /// <summary>The fighter as a condition sees it: live resource signals, a real parameters/modifiers
    /// pair, a real effects component and ability book, and a count of who is still listening. Shared by
    /// every test over predicates — the catalog hands out live conditions and they need a real subject.</summary>
    internal sealed class ConditionOwner : IFightable
    {
        /// <summary>Seeded so a mitigation roll (suppression) is the same on every run.</summary>
        private static readonly DefaultRandomNumberGenerator s_rolls = new(seed: 1);

        private float _health;
        private float _mana;
        private float _barrier;

        public ConditionOwner()
        {
            Effects = new EffectsComponent(this);
            AbilityBook = new AbilityBookComponent(this);
            Parameters.Initialize(ParameterModifiers.GetModifiers);
            ParameterModifiers.ModifiersChanged += Parameters.OnParameterModifiersChange;
        }

        public event Action<float>? CurrentManaChanged;
        public event Action<float>? CurrentBarrierChanged;
        public event Action<float>? CurrentHealthChanged;

        public IEffectsComponent Effects { get; }
        public IAbilityBookComponent AbilityBook { get; }
        public IParameterModifiersComponent ParameterModifiers { get; } = new ParameterModifiersComponent();
        public IEntityParametersComponent Parameters { get; } = new EntityParametersComponent();
        public IModifierHandlerComponent ModifierHandler { get; } = new ModifierHandlerComponent();
        public ICombatEventBus CombatEvents { get; } = new ConditionBus();
        public StatusEffects StatusEffects { get; private set; } = StatusEffects.None;

        /// <summary>Everything still subscribed to this fighter, across every channel a condition uses.</summary>
        public int ListenerCount =>
            Listeners(CurrentHealthChanged) + Listeners(CurrentManaChanged) + Listeners(CurrentBarrierChanged)
            + ParameterListeners + EffectListeners + BookListeners + ((ConditionBus)CombatEvents).HandlerCount;

        public float CurrentHealth
        {
            get => _health;
            set
            {
                _health = value;
                CurrentHealthChanged?.Invoke(value);
            }
        }

        public float CurrentMana
        {
            get => _mana;
            set
            {
                _mana = value;
                CurrentManaChanged?.Invoke(value);
            }
        }

        public float CurrentBarrier
        {
            get => _barrier;
            set
            {
                _barrier = value;
                CurrentBarrierChanged?.Invoke(value);
            }
        }

        public void SetMaximum(EntityParameter parameter, float value) => Parameters.SetBaseValueForParameter(parameter, value);

        public void ApplyStatus(StatusEffects status)
        {
            StatusEffects |= status;
            CombatEvents.Publish(new StatusEffectAppliedEvent(status));
        }

        public void RemoveStatus(StatusEffects status)
        {
            StatusEffects &= ~status;
            CombatEvents.Publish(new StatusEffectRemovedEvent(status));
        }

        // The rest of the fighter contract: a condition never reaches for any of it.
        public IPassiveSkillsComponent PassiveSkills => throw new NotSupportedException();
        public IEntityAttribute Dexterity => throw new NotSupportedException();
        public IEntityAttribute Strength => throw new NotSupportedException();
        public IEntityAttribute Intelligence => throw new NotSupportedException();
        public IAnimationsComponent Animations => throw new NotSupportedException();
        public IStance CurrentStance => throw new NotSupportedException();
        public IEntityGroup? Group { get; set; }
        public ITargetChooser? TargetChooser { get; set; }
        public bool IsFighting { get; set; }
        public bool IsAlive => CurrentHealth > 0f;
        public bool CanMove { get; set; }
        public string Id => "Fighter";
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public Texture2D? Icon => null;
        public string Description => string.Empty;
        public string DisplayName => Id;

        public bool IsSame(string otherId) => InstanceId == otherId;
        public float GetDamage() => 0f;

        /// <summary>What a cast pays before it announces itself: the debit signals like the real one, so
        /// everything listening for a resource of his recomputes.</summary>
        public void ConsumeResource(Costs type, float amount)
        {
            switch (type)
            {
                case Costs.Mana: CurrentMana -= amount; break;
                case Costs.Health: CurrentHealth -= amount; break;
                case Costs.Barrier: CurrentBarrier -= amount; break;
            }
        }

        /// <summary>A status the fighter does not carry yet takes hold and is announced; anything else is refused.</summary>
        public bool TryApplyStatusEffect(StatusEffects statusEffect)
        {
            if (statusEffect == StatusEffects.None || StatusEffects.HasFlag(statusEffect)) return false;

            ApplyStatus(statusEffect);
            return true;
        }

        /// <summary>The counterpart: only a status he actually carries can leave him.</summary>
        public bool TryRemoveStatusEffect(StatusEffects statusEffect)
        {
            if (statusEffect == StatusEffects.None || !StatusEffects.HasFlag(statusEffect)) return false;

            RemoveStatus(statusEffect);
            return true;
        }

        public IFightable ChoseTarget(List<IFightable> targets) => throw new NotSupportedException();
        public void Kill(bool isDebug = false) => throw new NotSupportedException();
        public void SetupBattleEventBus(IBattleEventBus bus) => throw new NotSupportedException();
        /// <summary>The resolving half of a hit as the fighters publish it: the attack is announced spent
        /// on the ATTACKER's bus, whoever resolves it.</summary>
        public Task ReceiveAttack(IAttackContext context)
        {
            context.Attacker.CombatEvents.Publish(new AfterAttackEvent(context));

            return Task.CompletedTask;
        }

        /// <summary>The joining half of a hit as the fighters publish it: the attack is announced on the
        /// attacker's own bus before anybody resolves it.</summary>
        public Task Attack(IAttackContext context)
        {
            CombatEvents.Publish(new BeforeAttackEvent(context));

            return Task.CompletedTask;
        }

        /// <summary>Opt-in: a blow actually runs the target-side pipeline and comes off the health, the
        /// way a real fighter takes it. Off by default — most walks here only need a fighter to exist,
        /// and every one of them was written against a <see cref="TakeDamage"/> that did nothing.</summary>
        public bool TakesDamageForReal { get; set; }

        public Task TakeDamage(IDamageContext context)
        {
            if (!TakesDamageForReal) return Task.CompletedTask;

            // The shape a real fighter uses: both modifier pipelines, then mitigation, then health.
            // No absorption layers — nothing here wears a shield or a barrier.
            ModifierHandler.Apply(context);
            context.Source.ModifierHandler.Apply(context);
            Calculations.CalculateMitigation(context, this, s_rolls);
            if (context.TotalDamage > 0) CurrentHealth -= context.TotalDamage;
            return Task.CompletedTask;
        }
        public void Heal(IHealContext context) => throw new NotSupportedException();
        public void OnTurnStart() => throw new NotSupportedException();
        public void OnTurnEnd() => throw new NotSupportedException();
        public void AddItemToInventory(IItem item) => throw new NotSupportedException();
        public void InjectServices(IGameServiceProvider provider) => throw new NotSupportedException();

        private int ParameterListeners => Listeners(FieldOf(Parameters, nameof(IEntityParametersComponent.ParameterChanged)));

        private int EffectListeners =>
            Listeners(FieldOf(Effects, nameof(IEffectsComponent.EffectAdded))) + Listeners(FieldOf(Effects, nameof(IEffectsComponent.EffectRemoved)));

        private int BookListeners => Listeners(FieldOf(AbilityBook, nameof(IAbilityBookComponent.ActiveAbilitiesChanged)));

        /// <summary>The components hide their subscriber lists behind field-like events; the leak
        /// tests need the count, and reading the backing field is the only way to it.</summary>
        private static Delegate? FieldOf(object target, string name) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target) as Delegate;

        private static int Listeners(Delegate? handler) => handler?.GetInvocationList().Length ?? 0;

        /// <summary>A combat bus that can be asked what is still listening.</summary>
        private sealed class ConditionBus : ICombatEventBus
        {
            private readonly List<(Type Event, object Handler)> _handlers = [];

            public int HandlerCount => _handlers.Count;

            public void Publish<T>(T evnt)
                where T : notnull, ICombatEvent
            {
                foreach ((Type type, object handler) in _handlers.ToArray())
                    if (type == typeof(T))
                        ((Action<T>)handler).Invoke(evnt);
            }

            public void Subscribe<T>(Action<T> handler)
                where T : notnull, ICombatEvent => _handlers.Add((typeof(T), handler));

            public void Unsubscribe<T>(Action<T> handler)
                where T : notnull, ICombatEvent => _handlers.RemoveAll(entry => entry.Event == typeof(T) && entry.Handler.Equals(handler));

            public void SubscribeAll(Action<ICombatEvent> handler) => _handlers.Add((typeof(ICombatEvent), handler));

            public void UnsubscribeAll(Action<ICombatEvent> handler) => _handlers.RemoveAll(entry => entry.Handler.Equals(handler));

            public void Dispose() => _handlers.Clear();
        }
    }

    /// <summary>An effect with nothing but the traits a condition reads: an id, a source and a side.</summary>
    internal class FakeEffect(string id, bool isHarmful) : IEffect
    {
        public IFightable? Target => null;
        public StatusEffects Status { get; set; } = StatusEffects.None;
        public int Duration { get; set; } = 1;
        public int MaxStacks { get; set; } = 99;
        public string Source => "test";
        public float Effectiveness => 1f;
        public bool IsHarmful => isHarmful;
        public string Id => id;
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public Texture2D? Icon => null;
        public string Description => string.Empty;
        public string DisplayName => Id;

        public event Action<int>? DurationChanged;

        public bool IsSame(string otherId) => InstanceId == otherId;
        public Task Apply(EffectApplyingContext context) => Task.CompletedTask;
        public void OnStackChanged(int currentStack) => DurationChanged?.Invoke(Duration);
        public void Remove()
        {
        }

        public void TurnStart()
        {
        }

        public void TurnEnd()
        {
        }

        public bool IsStronger(IEffect otherEffect) => false;
        public IEffect Copy() => new FakeEffect(Id, IsHarmful);

        /// <summary>The conditions this stand-in is read by never extend anything; a budget-free
        /// duration bump here would be a quiet second opinion about a rule the real effect owns.</summary>
        public int Extend(int turns) => throw new NotSupportedException("conditions do not extend");
    }

    internal sealed class FakeShield() : FakeEffect("Effect_Shield", isHarmful: false), IShieldEffect
    {
        public float Strength => 1f;

        public float Absorb(float damage) => damage;
    }
}
