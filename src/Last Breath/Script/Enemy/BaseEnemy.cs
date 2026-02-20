namespace LastBreath
{
    using Godot;
    using System;
    using Core.Enums;
    using Core.Interfaces.Items;
    using Core.Interfaces.Battle;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Battle.Source;
    using Battle.Source.Attribute;
    using Battle.Source.Components;
    using Battle.Source.PassiveSkills;
    using Core.Data;
    using Core.Interfaces;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Components;
    using Utilities;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events;
    using Core.Interfaces.Events.GameEvents;
    using Stateless;

    public partial class BaseEnemy : CharacterBody2D, IEntity
    {
       [Export] private Area2D? _interactionArea;
        private Vector2 _lastPosition = Vector2.Zero;
        private IGameEventBus? _gameEventBus;
        private IBattleEventBus? _battleEventBus;

        private enum State
        {
            Idle,
            Walk,
            Battle,
        }

        private enum Trigger
        {
            Idle,
            Walk,
            Battle,
        }

        private enum Direction
        {
            Up,
            Down,
            Left,
            Right
        }

        private Direction _direction;
        private float _baseSpeed = 500;
        private readonly StateMachine<State, Trigger> _stateMachine = new(State.Idle);
        private readonly RandomNumberGenerator _rnd = new();

        [Export] public string Id { get; private set; } = string.Empty;
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        [Export] public string[] Tags { get; private set; } = [];
        public Texture2D? Icon { get; } = null;
        public string Description { get; } = string.Empty;
        public string DisplayName { get; } = string.Empty;
        public IEntityParametersComponent Parameters { get; private set; }
        public IPassiveSkillsComponent PassiveSkills { get; private set; }
        public IAnimationsComponent Animations { get; private set; }
        public IEntityAttribute Dexterity { get; private set; }
        public IEntityAttribute Strength { get; private set; }
        public IEntityAttribute Intelligence { get; private set; }
        public ICombatEventBus CombatEvents { get; private set; }
        public IStance CurrentStance { get; private set; }
        public ITargetChooser? TargetChooser { get; set; }
        public bool IsFighting { get; set; }
        public bool IsAlive => CurrentHealth > 0;
        public IEffectsComponent Effects { get; private set; }
        public IModifiersComponent Modifiers { get; private set; }
        public IEntityGroup? Group { get; set; }
        public StatusEffects StatusEffects { get; set; } = StatusEffects.None;
        public bool CanMove { get; set; }

        public float CurrentHealth
        {
            get => Mathf.Max(0, field);
            set
            {
                float clamped = Mathf.Clamp(value, 0, Parameters.MaxHealth);
                if (Mathf.Abs(clamped - field) < 0.0001f) return;
                field = clamped;
                if (field <= 0) NotifyShouldDie();
                NotifyHealthChanges(field);
            }
        }

        public float CurrentBarrier
        {
            get => Mathf.Max(0, field);
            set
            {
                float clamped = Mathf.Clamp(value, 0, Parameters.MaxHealth);
                if (Mathf.Abs(clamped - field) < 0.0001f) return;
                field = clamped;
                NotifyBarrierChanges(field);
            }
        }

        public float CurrentMana
        {
            get => Mathf.Max(0, field);
            set
            {
                float clamped = Mathf.Clamp(value, 0, Parameters.MaxMana);
                if (Mathf.Abs(clamped - field) < 0.0001f) return;
                field = clamped;
                NotifyManaChanges(field);
            }
        }

        public event Action<float>? CurrentManaChanged;
        public event Action<float>? CurrentBarrierChanged;
        public event Action<float>? CurrentHealthChanged;
        public event Action<IEntity>? Dead;
        public event Action<float, DamageType, bool>? DamageTaken;


        public override void _Ready()
        {
            Animations.PlayAnimation("Idle");
            if (_interactionArea != null)
                _interactionArea.BodyEntered += OnBodyEnter;

            _rnd.Randomize();
            Parameters = new EntityParametersComponent();
            Modifiers = new ModifiersComponent();
            Parameters.Initialize(Modifiers.GetModifiers);
            Effects = new EffectsComponent(this);
            PassiveSkills = new PassiveSkillsComponent(this);
            Dexterity = new Dexterity(Modifiers);
            Strength = new Strength(Modifiers);
            Intelligence = new Intelligence(Modifiers);
            Effects.EffectAdded += OnEffectAdded;
            Effects.EffectRemoved += OnEffectRemoved;
            Modifiers.ModifiersChanged += Parameters.OnModifiersChange;
            Parameters.ParameterChanged += OnParameterChanged;
            Parameters.ParameterChanged += Dexterity.OnParameterChanges;
            Parameters.ParameterChanged += Strength.OnParameterChanges;
            Parameters.ParameterChanged += Intelligence.OnParameterChanges;
            CombatEvents = new CombatEventBus();
            var passive = new ChainAttackPassiveSkill();
            passive.Attach(this);
            var passiveTwo = new CounterAttackPassiveSkill();
            passiveTwo.Attach(this);
            var mana = new ManaBurnPassiveSkill(0.15f);
            mana.Attach(this);
            var burning = new BurningPassiveSkill(0.3f, 3, 3);
            burning.Attach(this);
            var regen = new RegenerationPassiveSkill(0.05f);
            regen.Attach(this);
            ConfigureStateMachine();
            SetBaseValuesForParameters();

            CurrentHealth = Parameters.MaxHealth;
            CurrentMana = Parameters.MaxMana;
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _gameEventBus = provider.GetService<IGameEventBus>();
        }

        private void ConfigureStateMachine()
        {
            _stateMachine.Configure(State.Idle)
                .OnEntry(() => { Animations.PlayAnimation($"Idle"); })
                .PermitReentry(Trigger.Idle)
                .Permit(Trigger.Walk, State.Walk)
                .Permit(Trigger.Battle, State.Battle);

            _stateMachine.Configure(State.Walk)
                .PermitReentry(Trigger.Walk)
                .Permit(Trigger.Idle, State.Idle)
                .Permit(Trigger.Battle, State.Battle);

            _stateMachine.Configure(State.Battle)
                .OnEntry(() =>
                {
                    Animations.PlayAnimation("Idle");
                    CanMove = false;
                    _lastPosition = Position;
                })
                .OnExit(() =>
                {
                    CanMove = true;
                    Position = _lastPosition;
                })
                .Permit(Trigger.Idle, State.Idle);
        }

        public void AddItemToInventory(IItem item)
        {
        }

        public float GetDamage() => _rnd.RandfRange(0.9f, 1.1f) * Parameters.Damage;

        public void SetupBattleEventBus(IBattleEventBus bus)
        {
            // TODO:
            _battleEventBus = bus;
            _battleEventBus.Subscribe<BattleEndEvent>(OnBattleEnd);
        }

        public void Heal(float amount)
        {
            CombatEvents.Publish<EntityHealedEvent>(new(this, amount));
            _battleEventBus?.Publish(new EntityHealedEvent(this, amount));
            CurrentHealth += amount;
        }

        public void ConsumeResource(Costs type, float amount)
        {
            switch (type)
            {
                case Costs.Barrier:
                    CurrentBarrier -= amount;
                    break;
                case Costs.Health:
                    CurrentHealth -= amount;
                    break;
                case Costs.Mana:
                    CurrentMana -= amount;
                    break;
            }
        }

        public bool IsSame(string otherId) => InstanceId.Equals(otherId);

        public bool TryApplyStatusEffect(StatusEffects statusEffect)
        {
            if ((StatusEffects & statusEffect) != 0) return false;
            StatusEffects |= statusEffect;
            CombatEvents.Publish(new StatusEffectAppliedEvent(statusEffect));
            return true;
        }

        public bool TryRemoveStatusEffect(StatusEffects statusEffect)
        {
            if ((StatusEffects & statusEffect) == 0) return false;
            StatusEffects &= ~statusEffect;
            CombatEvents.Publish(new StatusEffectRemovedEvent(statusEffect));
            return true;
        }

        public void Kill() => NotifyShouldDie();

        public IEntity ChoseTarget(List<IEntity> targets)
        {
            TargetChooser ??= new ChoosePlayerAsTarget();

            return TargetChooser.Choose(targets);
        }

        public async Task ReceiveAttack(IAttackContext context)
        {
            try
            {
                Calculations.CalculateHitSucceeded(context);
                switch (context.Result)
                {
                    case AttackResults.Succeed:
                        Calculations.CalculateFinalDamage(context);
                        CombatEvents.Publish<BeforeDamageTakenEvent>(new(context));
                        await TakeDamage(context.Attacker, context.FinalDamage, DamageType.Normal, DamageSource.Hit);
                        break;
                    case AttackResults.Blocked:
                        CombatEvents.Publish<AttackBlockedEvent>(new(context));
                        await Animations.PlayAnimationAsync("Blocked");
                        break;
                    case AttackResults.Evaded:
                        CombatEvents.Publish<AttackEvadedEvent>(new(context));
                        await Animations.PlayAnimationAsync("Evaded");
                        break;
                }

                context.Attacker.CombatEvents.Publish(new AfterAttackEvent(context));
                context.Attacker.Effects.TriggerAfterAttack(context);
            }
            catch (Exception e)
            {
                GD.Print($"{e.Message}, {e.StackTrace}");
            }
        }

        public async Task Attack(IAttackContext context)
        {
            context.IsCritical = context.Rnd.RandFloat() <= Parameters.CriticalChance;
            CombatEvents.Publish(new BeforeAttackEvent(context));
            await Animations.PlayAnimationAsync("Attack");
        }

        public void OnTurnEnd()
        {
            Effects.TriggerTurnEnd();
            CombatEvents.Publish(new TurnEndEvent(this));
            _battleEventBus?.Publish(new TurnEndEvent(this));
            _gameEventBus?.Publish(new TurnEndEvent(this));
        }

        public void OnTurnStart()
        {
            Effects.TriggerTurnStart();
            CombatEvents.Publish(new TurnStartEvent(this));
            _battleEventBus?.Publish(new TurnStartEvent(this));
            _gameEventBus?.Publish(new TurnStartEvent(this));
        }

        public async Task TakeDamage(IEntity from, float damage, DamageType type, DamageSource source, bool isCrit = false)
        {
            // TODO: Here i need to calculate final damage with armor/resistance etc
            CombatEvents.Publish(new DamageTakenEvent(from, this, damage, type, source, isCrit));
            _battleEventBus?.Publish(new DamageTakenEvent(from, this, damage, type, source, isCrit));
            CurrentHealth -= damage;
            await Animations.PlayAnimationAsync("Hurt");
        }

        private void OnBodyEnter(Node2D body)
        {
            if (IsFighting) return;
            try
            {
                switch (body)
                {
                    case IPlayer player:
                        {
                            List<IEntity> fighters = [];
                            if (Group != null)
                            {
                                Group.NotifyAllInGroup(GroupNotification.Attacked);
                                fighters.AddRange(Group.GetEntitiesInGroup<IEntity>());
                            }
                            else
                                fighters.Add(this);

                            _stateMachine.Fire(Trigger.Battle);
                            _gameEventBus?.Publish(new BattleStartEvent(player, fighters));
                            break;
                        }
                    case IFightable fighter:
                        break;
                }
            }
            catch (Exception e)
            {
                Tracker.TrackException("Failed to handle body enter", e, this);
            }
        }

        private void OnParameterChanged(EntityParameter parameter, float value)
        {
            switch (parameter)
            {
                case EntityParameter.Health:
                    _battleEventBus?.Publish<EntityMaxHealthChangesEvent>(new(this, value));
                    break;
                case EntityParameter.Barrier:
                    break;
                case EntityParameter.Mana:
                    _battleEventBus?.Publish<EntityMaxManaChangesEvent>(new(this, value));
                    break;
            }
        }

        private void OnBattleEnd(BattleEndEvent obj)
        {
            Effects.RemoveAllEffects();
            if (IsAlive) _stateMachine.Fire(Trigger.Idle);
            _battleEventBus = null;
        }

        private void OnEffectRemoved(IEffect effect)
        {
            _battleEventBus?.Publish<EffectRemovedEvent>(new(effect, this));
        }

        private void OnEffectAdded(IEffect effect)
        {
            _battleEventBus?.Publish<EffectAddedEvent>(new(effect, this));
        }

        private void NotifyHealthChanges(float value)
        {
            CurrentHealthChanged?.Invoke(value);
            _gameEventBus?.Publish<EntityHealthChangesEvent>(new(this, value));
            _battleEventBus?.Publish<EntityHealthChangesEvent>(new(this, value));
        }

        private void NotifyBarrierChanges(float value)
        {
            CurrentBarrierChanged?.Invoke(value);
            _gameEventBus?.Publish<EntityBarrierChanges>(new(this, value));
            _battleEventBus?.Publish<EntityBarrierChanges>(new(this, value));
        }

        private void NotifyManaChanges(float value)
        {
            CurrentManaChanged?.Invoke(value);
            _gameEventBus?.Publish<EntityManaChangesEvent>(new(this, value));
            _battleEventBus?.Publish<EntityManaChangesEvent>(new(this, value));
        }

        private void NotifyShouldDie()
        {
            _gameEventBus?.Publish<EntityDiedEvent>(new(this));
            _battleEventBus?.Publish<EntityDiedEvent>(new(this));

            Animations.PlayAnimation("Dead");
            Dead?.Invoke(this);
        }

        private void SetBaseValuesForParameters()
        {
            var rnd = new RandomNumberGenerator();
            rnd.Randomize();
            foreach (EntityParameter entityParameter in Enum.GetValues<EntityParameter>())
            {
                float value = 0;

                switch (entityParameter)
                {
                    case EntityParameter.Health:
                    case EntityParameter.Barrier:
                        value = 10000;
                        break;
                    case EntityParameter.Mana:
                        value = 50;
                        break;
                    case EntityParameter.Intelligence:
                    case EntityParameter.Strength:
                    case EntityParameter.Dexterity:
                        value = rnd.RandfRange(1, 5);
                        break;
                    case EntityParameter.Armor:
                        value = 200;
                        break;
                    case EntityParameter.CriticalChance:
                    case EntityParameter.AdditionalHitChance:
                        value = 0.25f;
                        break;
                    case EntityParameter.CriticalDamage:
                        value = 1.5f;
                        break;
                    case EntityParameter.Damage:
                    case EntityParameter.SpellDamage:
                        value = rnd.RandfRange(50, 250);
                        break;
                    case EntityParameter.Accuracy:
                    case EntityParameter.Evade:
                        value = rnd.RandfRange(50, 500);
                        break;
                }

                Parameters.SetBaseValueForParameter(entityParameter, value);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;

            _battleEventBus = null;
            _gameEventBus = null;
        }
    }
}
