namespace Battle.Source
{
    using Godot;
    using System;
    using Services;
    using Attribute;
    using Stateless;
    using Core.Enums;
    using Components;
    using Core.Constants;
    using Core.Interfaces;
    using Core.Interfaces.Data;
    using Core.Interfaces.Items;
    using Core.Interfaces.Events;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using System.Threading.Tasks;
    using Core.Interfaces.Components;
    using System.Collections.Generic;
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
        [Export] private AnimatedSprite2D? _animatedSprite;
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
                float clamped = Mathf.Clamp(value, 0, Parameters.Mana);
                if (Mathf.Abs(clamped - field) < 0.0001f) return;
                field = clamped;
                NotifyManaChanges(field);
            }
        }

        public event Action<float>? CurrentManaChanged;
        public event Action<float>? CurrentBarrierChanged;
        public event Action<float>? CurrentHealthChanged;
        public event Action<float, DamageType, bool>? DamageTaken;
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
            Effects.AllEffectsRemoved += OnAllEffectsRemoved;
            Modifiers.ModifiersChanged += Parameters.OnModifiersChange;
            Parameters.ParameterChanged += OnParameterChanged;
            Parameters.ParameterChanged += Dexterity.OnParameterChanges;
            Parameters.ParameterChanged += Strength.OnParameterChanges;
            Parameters.ParameterChanged += Intelligence.OnParameterChanges;
            CombatEvents = new CombatEventBus();
            SetBaseValuesForParameters();
            ConfigureStateMachine();
            CurrentHealth = Parameters.MaxHealth;
            CurrentMana = Parameters.Mana;
            Instance = this;
        }



        public override void _EnterTree()
        {
            if (GetParent() is MainWorld) _stateMachine.Fire(Trigger.Idle);
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
        }

        public void Heal(float amount) => CurrentHealth += amount;

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

        public void Kill() => Dead?.Invoke(this);

        public async Task ReceiveAttack(IAttackContext context)
        {
            if (_animatedSprite == null) return;
            CalculateFinalDamage(context);
            CombatEvents.Publish<BeforeDamageTakenEvent>(new(context));
            await TakeDamage(context.Attacker, context.FinalDamage, DamageType.Normal, DamageSource.Hit);
            context.Attacker.CombatEvents.Publish(new AfterAttackEvent(context));
            context.Attacker.Effects.TriggerAfterAttack(context);
        }

        public async Task Attack(IAttackContext context)
        {
            if (_animatedSprite == null) return;
            context.IsCritical = context.Rnd.RandFloat() <= Parameters.CriticalChance;
            CombatEvents.Publish(new BeforeAttackEvent(context));
            Effects.TriggerBeforeAttack(context);
            _animatedSprite.Play("Fight_Attack");
            await ToSignal(_animatedSprite, "animation_finished");
            _animatedSprite.Play("Fight_Idle");
        }

        public void OnTurnEnd()
        {
            Effects.TriggerTurnEnd();
            CombatEvents.Publish(new TurnEndGameEvent(this));
            _battleEventBus?.Publish(new TurnEndGameEvent(this));
            _gameEventBus?.Publish(new TurnEndGameEvent(this));
        }

        public void OnTurnStart()
        {
            Effects.TriggerTurnStart();
            CombatEvents.Publish(new TurnStartGameEvent(this));
            _battleEventBus?.Publish(new TurnStartGameEvent(this));
            _gameEventBus?.Publish(new TurnStartGameEvent(this));
        }

        public async Task TakeDamage(IEntity from, float damage, DamageType type, DamageSource source, bool isCrit = false)
        {
            _animatedSprite?.Play("Fight_Hurt");
            CombatEvents.Publish(new DamageTakenEvent(from, damage, type, source, isCrit));
            _battleEventBus?.Publish(new DamageTakenEvent(from, damage, type, source, isCrit));
            DamageTaken?.Invoke(damage, type, isCrit);
            CurrentHealth -= damage;
            await ToSignal(_animatedSprite, "animation_finished");
            _animatedSprite.Play("Fight_Idle");
        }


        public void InjectServices(IGameServiceProvider provider)
        {
            _gameEventBus = provider.GetService<IGameEventBus>();
        }

        private void ConfigureStateMachine()
        {
            _stateMachine.Configure(State.Idle)
                .OnEntry(() => { _animatedSprite?.Play($"{_stateMachine.State}_{_direction}"); })
                .PermitReentry(Trigger.Idle)
                .Permit(Trigger.Walk, State.Walk)
                .Permit(Trigger.Battle, State.Battle);

            _stateMachine.Configure(State.Walk)
                .OnEntry(() => { _animatedSprite?.Play($"{_stateMachine.State}_{_direction}"); })
                .PermitReentry(Trigger.Walk)
                .Permit(Trigger.Idle, State.Idle)
                .Permit(Trigger.Battle, State.Battle);

            _stateMachine.Configure(State.Battle)
                .OnEntry(() =>
                {
                    _animatedSprite?.Play("Fight_Idle");
                    CanMove = false;
                    _lastPosition = Position;
                })
                .OnExit(() =>
                {
                    CanMove = true;
                    Position = _lastPosition;
                })
                .PermitReentry(Trigger.Idle)
                .Permit(Trigger.Idle, State.Idle);
        }

        private void CalculateFinalDamage(IAttackContext context)
        {
            float baseDamage = context.BaseDamage;
            float additionalDamage = context.AdditionalDamage;
            context.FinalDamage = baseDamage + additionalDamage;
            if (context.IsCritical)
                context.FinalDamage *= context.Attacker.Parameters.CriticalDamage;
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

        private void OnAllEffectsRemoved()
        {
            _battleEventBus?.Publish<AllEffectRemoved>(new(this));
        }

        private void OnEffectRemoved(IEffect effect)
        {
            _battleEventBus?.Publish<EffectRemovedEvent>(new(effect, this));
        }

        private void OnEffectAdded(IEffect effect)
        {
            _battleEventBus?.Publish<EffectAddedEvent>(new(effect,this));
        }

        private void NotifyShouldDie()
        {
            Dead?.Invoke(this);
            _gameEventBus?.Publish<PlayerDiedGameEvent>(new(this));
            _battleEventBus?.Publish<PlayerDiedGameEvent>(new(this));
        }

        private void NotifyHealthChanges(float value)
        {
            CurrentHealthChanged?.Invoke(value);
            _gameEventBus?.Publish<PlayerHealthChangesGameEvent>(new(this, value));
            _battleEventBus?.Publish<PlayerHealthChangesGameEvent>(new(this, value));
        }

        private void NotifyBarrierChanges(float value)
        {
            CurrentBarrierChanged?.Invoke(value);
            _gameEventBus?.Publish<PlayerBarrierChangesGameEvent>(new(this, value));
            _battleEventBus?.Publish<PlayerBarrierChangesGameEvent>(new(this, value));
        }

        private void NotifyManaChanges(float value)
        {
            CurrentManaChanged?.Invoke(value);
            _gameEventBus?.Publish<PlayerManaChangesGameEvent>(new(this, value));
            _battleEventBus?.Publish<PlayerManaChangesGameEvent>(new(this, value));
        }

        private void OnParameterChanged(EntityParameter parameter, float value)
        {
            switch (parameter)
            {
                case EntityParameter.Health:
                    _battleEventBus?.Publish<PlayerMaxHealthChanges>(new(this, value));
                    break;
                case EntityParameter.Barrier:
                    _battleEventBus?.Publish<PlayerManaChangesGameEvent>(new(this, value));
                    break;
                case EntityParameter.Mana:
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
                        value = 200;
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
                        value = 380f;
                        break;
                }

                Parameters.SetBaseValueForParameter(entityParameter, value);
            }
        }

        public Vector2 GetCameraPosition() => GlobalPosition;
    }
}
