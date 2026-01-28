namespace Battle.Source
{
    using Godot;
    using System;
    using Services;
    using Attribute;
    using Stateless;
    using Utilities;
    using Core.Enums;
    using Components;
    using Core.Constants;
    using Core.Interfaces;
    using Core.Interfaces.Items;
    using Core.Interfaces.Events;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using System.Threading.Tasks;
    using Core.Interfaces.Components;
    using System.Collections.Generic;
    using Core.Data;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Events.GameEvents;

    internal partial class Player : CharacterBody2D, IRequireServices, IEntity, ICameraFocus
    {
        public enum State
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

        private readonly StateMachine<State, Trigger> _stateMachine = new(State.Idle);

        private readonly Dictionary<Stance, IStance> _stances = [];

        private readonly RandomNumberGenerator _rnd = new();
        private Vector2 _lastPosition = Vector2.Zero;
        private Direction _direction;
        private float _baseSpeed = 500;
        [Export] private AnimationsComponent? _animationsComponent;
        [Export] private Area2D? _interactionArea;
        [Export] private Godot.Collections.Dictionary<State, AnimatedSprite2D>? _animatedSprites = [];

        private IGameEventBus? _gameEventBus;
        private IBattleEventBus? _battleEventBus;

        public string Id { get; }
        public string InstanceId { get; } = Guid.NewGuid().ToString();

        public Texture2D? Icon { get; }
        public string Description { get; }
        public string DisplayName { get; }
        public string[] Tags { get; } = [];
        public IEntityParametersComponent Parameters { get; private set; }
        public IPassiveSkillsComponent PassiveSkills { get; private set; }
        public IAnimationsComponent Animations => _animationsComponent;
        public IEntityAttribute Dexterity { get; private set; }
        public IEntityAttribute Strength { get; private set; }
        public IEntityAttribute Intelligence { get; private set; }
        public ICombatEventBus CombatEvents { get; private set; }
        public IStance? CurrentStance { get; private set; }
        public ITargetChooser? TargetChooser { get; set; }
        public bool IsFighting { get; set; }
        public bool IsAlive => CurrentHealth > 0;
        public IEffectsComponent Effects { get; private set; }
        public IModifiersComponent Modifiers { get; private set; }
        public IEntityGroup? Group { get; set; }
        public StatusEffects StatusEffects { get; set; } = StatusEffects.None;
        public bool CanMove { get; set; } = true;

        public static Player? Instance { get; private set; }

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


        public override void _Ready()
        {
            if (_interactionArea != null) _interactionArea.BodyEntered += OnBodyEnter;

            _rnd.Randomize();
            _gameEventBus = GameServiceProvider.Instance.GetService<IGameEventBus>();
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
            SetBaseValuesForParameters();
            ConfigureStateMachine();
            CurrentHealth = Parameters.MaxHealth;
            CurrentMana = Parameters.MaxMana;
            Instance = this;

            _stances.Add(Stance.Intelligence, new IntelligenceStance(this));
            _stances.Add(Stance.Strength, new StrengthStance(this));
            _stances.Add(Stance.Dexterity, new DexterityStance(this));
        }

        public override void _Process(double delta)
        {
            if (!CanMove) return;
            Vector2 inputDirection =
                Input.GetVector(Settings.MoveLeft, Settings.MoveRight, Settings.MoveUp, Settings.MoveDown);
            Velocity = inputDirection * _baseSpeed;
            SwitchState(inputDirection);
            MoveAndSlide();
        }

        public void AddItemToInventory(IItem item)
        {
        }

        public float GetDamage() => _rnd.RandfRange(0.9f, 1.1f) * Parameters.Damage;

        public void SetupBattleEventBus(IBattleEventBus bus)
        {
            _battleEventBus = bus;
            _stateMachine.Fire(Trigger.Battle);
            _battleEventBus.Subscribe<BattleEndEvent>(OnBattleEnds);
            _battleEventBus.Subscribe<PlayerChangesStanceEvent>(OnStanceChanges);
        }

        private void OnStanceChanges(PlayerChangesStanceEvent obj)
        {
            CurrentStance?.OnDeactivate();
            CurrentStance = _stances.GetValueOrDefault(obj.Stance);
            CurrentStance?.OnActivate();
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

        public IEntity ChoseTarget(List<IEntity> targets) => throw new NotImplementedException();

        public void Kill() => NotifyShouldDie();

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
                        await Animations.PlayAnimationAsync("Fight_Blocked");
                        break;
                    case AttackResults.Evaded:
                        CombatEvents.Publish<AttackEvadedEvent>(new(context));
                        await Animations.PlayAnimationAsync("Fight_Evaded");
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
            Effects.TriggerBeforeAttack(context);
            await Animations.PlayAnimationAsync("Fight_Attack");
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
            CombatEvents.Publish(new DamageTakenEvent(from, this, damage, type, source, isCrit));
            _battleEventBus?.Publish(new DamageTakenEvent(from, this, damage, type, source, isCrit));
            CurrentHealth -= damage;
            await Animations.PlayAnimationAsync("Fight_Hurt");
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _gameEventBus = provider.GetService<IGameEventBus>();
        }

        private void ConfigureStateMachine()
        {
            _stateMachine.Configure(State.Idle)
                .OnEntry(() => { Animations.PlayAnimation($"{_stateMachine.State}_{_direction}"); })
                .PermitReentry(Trigger.Idle)
                .Permit(Trigger.Walk, State.Walk)
                .Permit(Trigger.Battle, State.Battle);

            _stateMachine.Configure(State.Walk)
                .OnEntry(() => { Animations.PlayAnimation($"{_stateMachine.State}_{_direction}"); })
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

        private void SwitchState(Vector2 direction)
        {
            if (direction == Vector2.Zero)
            {
                _stateMachine.Fire(Trigger.Idle);
                return;
            }

            Direction newDirection = DefineDirection(direction);
            _direction = newDirection;
            _stateMachine.Fire(Trigger.Walk);
        }

        private Direction DefineDirection(Vector2 direction)
        {
            if (direction == Vector2.Zero)
                return _direction;

            if (Mathf.Abs(direction.Y) >= Mathf.Abs(direction.X))
                return direction.Y < 0 ? Direction.Up : Direction.Down;
            return direction.X < 0 ? Direction.Left : Direction.Right;
        }

        private void OnBodyEnter(Node2D body)
        {
            // if (body is IFightable fightable)
            //     _mediator?.PublishAsync(new InitializeFightEvent<IFightable>([fightable, this]));
        }

        private void OnEffectRemoved(IEffect effect)
        {
            _battleEventBus?.Publish<EffectRemovedEvent>(new(effect, this));
        }

        private void OnEffectAdded(IEffect effect)
        {
            _battleEventBus?.Publish<EffectAddedEvent>(new(effect, this));
        }

        private void OnBattleEnds(BattleEndEvent obj)
        {
            _battleEventBus?.Unsubscribe<BattleEndEvent>(OnBattleEnds);
            _battleEventBus?.Unsubscribe<PlayerChangesStanceEvent>(OnStanceChanges);
            Effects.RemoveAllEffects();
            _stateMachine.Fire(Trigger.Idle);
            _battleEventBus = null;
        }

        private void NotifyShouldDie()
        {
            _gameEventBus?.Publish<PlayerDiedEvent>(new(this));
            _battleEventBus?.Publish<PlayerDiedEvent>(new(this));

            Animations.PlayAnimation("Dead");
            Dead?.Invoke(this);
        }

        private void NotifyHealthChanges(float value)
        {
            CurrentHealthChanged?.Invoke(value);
            _gameEventBus?.Publish<PlayerHealthChangesEvent>(new(this, value));
            _battleEventBus?.Publish<PlayerHealthChangesEvent>(new(this, value));
        }

        private void NotifyBarrierChanges(float value)
        {
            CurrentBarrierChanged?.Invoke(value);
            _gameEventBus?.Publish<PlayerBarrierChangesEvent>(new(this, value));
            _battleEventBus?.Publish<PlayerBarrierChangesEvent>(new(this, value));
        }

        private void NotifyManaChanges(float value)
        {
            CurrentManaChanged?.Invoke(value);
            _gameEventBus?.Publish<PlayerManaChangesEvent>(new(this, value));
            _battleEventBus?.Publish<PlayerManaChangesEvent>(new(this, value));
        }

        private void OnParameterChanged(EntityParameter parameter, float value)
        {
            switch (parameter)
            {
                case EntityParameter.Health:
                    _battleEventBus?.Publish<PlayerMaxHealthChanges>(new(this, value));
                    break;
                case EntityParameter.Mana:
                    _battleEventBus?.Publish<PlayerMaxManaChangesEvent>(new(value));
                    break;
            }
        }

        private void SetBaseValuesForParameters()
        {
            foreach (EntityParameter entityParameter in Enum.GetValues<EntityParameter>())
            {
                float value = 0;

                switch (entityParameter)
                {
                    case EntityParameter.Health:
                    case EntityParameter.Barrier:
                        value = 3600;
                        break;
                    case EntityParameter.Mana:
                        value = 500;
                        break;
                    case EntityParameter.Intelligence:
                    case EntityParameter.Strength:
                    case EntityParameter.Dexterity:
                        value = 5f;
                        break;
                    case EntityParameter.Evade:
                    case EntityParameter.Armor:
                    case EntityParameter.Accuracy:
                        value = 500;
                        break;
                    case EntityParameter.CriticalChance:
                        value = 0.25f;
                        break;
                    case EntityParameter.AdditionalHitChance:
                        value = 0.6f;
                        break;
                    case EntityParameter.CriticalDamage:
                        value = 1.5f;
                        break;
                    case EntityParameter.Damage:
                    case EntityParameter.SpellDamage:
                        value = 100;
                        break;
                }

                Parameters.SetBaseValueForParameter(entityParameter, value);
            }
        }

        public Vector2 GetCameraPosition() => GlobalPosition;
    }
}
