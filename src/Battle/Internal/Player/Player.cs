namespace Battle.Internal.Player
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Attribute;
    using Components;
    using Core.Constants;
    using Core.Data;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Components;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events;
    using Core.Interfaces.Events.GameEvents;
    using Core.Interfaces.Items;
    using Godot;
    using Services;
    using Source;
    using Stateless;
    using AnimationsComponent = Components.AnimationsComponent;

    public partial class Player : CharacterBody2D, IPlayer
    {
        public enum State
        {
            Idle,
            Walk,
            Fight,
        }

        private enum Trigger
        {
            Idle,
            Walk,
            Fight,
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

        private IGameEventBus? _gameEventBus;
        private IBattleEventBus? _battleEventBus;
        private ICombatComponent? _combat;

        public string Id { get; }
        public string InstanceId { get; } = Guid.NewGuid().ToString();

        public Texture2D? Icon { get; }
        public string Description { get; }
        public string DisplayName { get; }
        public string[] Tags { get; } = [];
        public IEntityParametersComponent Parameters { get; private set; }
        public IPassiveSkillsComponent PassiveSkills { get; private set; }
        public IAnimationsComponent Animations => _animationsComponent;
        public IModifierHandlerComponent ModifierHandler { get; private set; }
        public IEntityAttribute Dexterity { get; private set; }
        public IEntityAttribute Strength { get; private set; }
        public IEntityAttribute Intelligence { get; private set; }
        public ICombatEventBus CombatEvents { get; private set; }
        public IStance? CurrentStance { get; private set; }
        public ITargetChooser? TargetChooser { get; set; }
        public bool IsFighting { get; set; }
        public bool IsAlive => CurrentHealth > 0;
        public IEffectsComponent Effects { get; private set; }
        public IParameterModifiersComponent ParameterModifiers { get; private set; }
        public IEntityGroup? Group { get; set; }
        public StatusEffects StatusEffects { get; set; } = StatusEffects.None;
        public bool CanMove { get; set; } = true;
        public string Name { get; private set; } = string.Empty;
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
                float clamped = Mathf.Clamp(value, 0, Parameters.MaxBarrier);
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
        public event Action<IFightable>? Dead;


        public override void _Ready()
        {
            if (_interactionArea != null) _interactionArea.BodyEntered += OnBodyEnter;

            _rnd.Randomize();
            _gameEventBus = GameServiceProvider.Instance.GetService<IGameEventBus>();
            Parameters = new EntityParametersComponent();
            ParameterModifiers = new ParameterModifiersComponent();
            Parameters.Initialize(ParameterModifiers.GetModifiers);
            Effects = new EffectsComponent(this);
            PassiveSkills = new PassiveSkillsComponent(this);
            Dexterity = new Dexterity(ParameterModifiers);
            Strength = new Strength(ParameterModifiers);
            Intelligence = new Intelligence(ParameterModifiers);
            ModifierHandler = new ModifierHandlerComponent();
            _combat = new CombatComponent(this) { GameEventBus = _gameEventBus };
            Effects.EffectAdded += OnEffectAdded;
            Effects.EffectRemoved += OnEffectRemoved;
            ParameterModifiers.ModifiersChanged += Parameters.OnParameterModifiersChange;
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

        public override void _PhysicsProcess(double delta)
        {
            if (!CanMove) return;
            Vector2 inputDirection = Input.GetVector(Settings.MoveLeft, Settings.MoveRight, Settings.MoveUp, Settings.MoveDown);
            Velocity = inputDirection * _baseSpeed;
            SwitchState(inputDirection);
            MoveAndSlide();
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _gameEventBus = provider.GetService<IGameEventBus>();
            if (_combat != null) _combat.GameEventBus = _gameEventBus;
        }

        public void AddItemToInventory(IItem item)
        {
        }

        public float GetDamage() => _rnd.RandfRange(0.9f, 1.1f) * Parameters.Damage;

        public void SetupBattleEventBus(IBattleEventBus bus)
        {
            _battleEventBus = bus;
            _combat!.BattleEventBus = bus;
            _stateMachine.Fire(Trigger.Fight);
            _battleEventBus.Subscribe<BattleEndEvent>(OnBattleEnds);
            _battleEventBus.Subscribe<PlayerChangesStanceEvent>(OnStanceChanges);
        }

        private void OnStanceChanges(PlayerChangesStanceEvent obj)
        {
            CurrentStance?.OnDeactivate();
            CurrentStance = _stances.GetValueOrDefault(obj.Stance);
            CurrentStance?.OnActivate();
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

        public IFightable ChoseTarget(List<IFightable> targets) => throw new NotImplementedException();

        public void Kill() => NotifyShouldDie();

        public void Heal(IHealContext context) => _combat!.Heal(context);

        public Task ReceiveAttack(IAttackContext context) => _combat!.ReceiveAttack(context);

        public Task Attack(IAttackContext context) => _combat!.Attack(context);

        public Task TakeDamage(IDamageContext context) => _combat!.TakeDamage(context);

        public void OnTurnEnd() => _combat!.OnTurnEnd();

        public void OnTurnStart() => _combat!.OnTurnStart();



        private void ConfigureStateMachine()
        {
            _stateMachine.Configure(State.Idle)
                .OnEntry(() => { Animations.PlayAnimation($"{_stateMachine.State}_{_direction}"); })
                .PermitReentry(Trigger.Idle)
                .Permit(Trigger.Walk, State.Walk)
                .Permit(Trigger.Fight, State.Fight);

            _stateMachine.Configure(State.Walk)
                .OnEntry(() => { Animations.PlayAnimation($"{_stateMachine.State}_{_direction}"); })
                .PermitReentry(Trigger.Walk)
                .Permit(Trigger.Idle, State.Idle)
                .Permit(Trigger.Fight, State.Fight);

            _stateMachine.Configure(State.Fight)
                .OnEntry(() =>
                {
                    Animations.PlayAnimation("Idle_Right");
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
            _combat!.BattleEventBus = null;
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
                        value = 1000;
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
                        value = 300;
                        break;
                }

                Parameters.SetBaseValueForParameter(entityParameter, value);
            }
        }

        public Vector2 GetCameraPosition() => GlobalPosition;
    }
}
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             