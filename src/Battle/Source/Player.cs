namespace Battle.Source
{
    using Godot;
    using System;
    using Services;
    using Attribute;
    using Stateless;
    using Core.Enums;
    using Components;
    using CombatEvents;
    using Core.Constants;
    using Core.Interfaces.Data;
    using Core.Interfaces.Items;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using System.Threading.Tasks;
    using Abilities.PassiveSkills;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Components;
    using System.Collections.Generic;

    internal partial class Player : CharacterBody2D, IRequireServices, IEntity
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

        private Vector2 _lastPosition = Vector2.Zero;
        private Direction _direction;
        private readonly StateMachine<State, Trigger> _stateMachine = new(State.Idle);
        private float _baseSpeed = 500;
        private readonly Dictionary<Stance, IStance> _stances = [];
        [Export] private AnimatedSprite2D? _animatedSprite;
        [Export] private Area2D? _interactionArea;
        [Export] private Godot.Collections.Dictionary<State, AnimatedSprite2D>? _animatedSprites = [];

        private IMediator _mediator = GameServiceProvider.Instance.GetService<IMediator>();

        public string Id { get; }
        public Texture2D? Icon { get; }
        public string Description { get; }
        public string DisplayName { get; }
        public string[] Tags { get; } = [];
        public IEntityParametersComponent Parameters { get; private set; }
        public IPassiveSkillsComponent PassiveSkills { get; private set; }
        public IEntityAttribute Dexterity { get; private set; }
        public IEntityAttribute Strength { get; private set; }
        public IEntityAttribute Intelligence { get; private set; }
        public IEventBus Events { get; private set; }
        public IStance CurrentStance { get; private set; }
        public ITargetChooser? TargetChooser { get; set; }
        public bool IsFighting { get; set; }
        public bool IsAlive => CurrentHealth > 0;
        public IEffectsComponent Effects { get; private set; }
        public IModifiersComponent Modifiers { get; private set; }
        public IEntityGroup? Group { get; set; }
        public StatusEffects StatusEffects { get; set; } = StatusEffects.None;
        public bool CanMove { get; set; } = true;

        public float CurrentHealth
        {
            get => Mathf.Max(0, field);
            set
            {
                float clamped = Mathf.Clamp(value, 0, Parameters.MaxHealth);
                if (Mathf.Abs(clamped - field) < 0.0001f) return;
                field = clamped;
                if (field <= 0) Dead?.Invoke(this);
                CurrentHealthChanged?.Invoke(field);
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
                CurrentBarrierChanged?.Invoke(field);
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
                CurrentManaChanged?.Invoke(field);
            }
        }

        public event Action<float>? CurrentManaChanged;
        public event Action<float>? CurrentBarrierChanged;
        public event Action<float>? CurrentHealthChanged;
        public event Action<IFightable>? Dead;


        public override void _Ready()
        {
            GameServiceProvider.Instance.GetService<PlayerReference>().SetPlayerReference(this);
            if (_interactionArea != null) _interactionArea.BodyEntered += OnBodyEnter;

            Parameters = new EntityParametersComponent();
            Modifiers = new ModifiersComponent();
            Parameters.Initialize(Modifiers.GetModifiers);
            Effects = new EffectsComponent(this);
            PassiveSkills = new PassiveSkillsComponent(this);
            Dexterity = new Dexterity(Modifiers);
            Strength = new Strength(Modifiers);
            Intelligence = new Intelligence(Modifiers);
            Modifiers.ModifiersChanged += Parameters.OnModifiersChange;
            Parameters.ParameterChanged += OnParameterChanged;
            Parameters.ParameterChanged += Dexterity.OnParameterChanges;
            Parameters.ParameterChanged += Strength.OnParameterChanges;
            Parameters.ParameterChanged += Intelligence.OnParameterChanges;
            Events = new CombatEventBus();
            SetBaseValuesForParameters();
            ConfigureStateMachine();

            var vampire = new VampireAttackPassiveSkill("Vampire_Skill", 0.1f);
            vampire.Attach(this);
            var passive = new ChainAttackPassiveSkill("Chain_Attack_Passive");
            passive.Attach(this);
            var trapped = new TrappedBeastPassiveSkill("Trapped", 0.01f, 0.01f);
            trapped.Attach(this);
            var counter = new CounterAttackPassiveSkill("Counter");
            counter.Attach(this);
        }

        public override void _EnterTree()
        {
            if (GetParent() is BattleArena) _stateMachine.Fire(Trigger.Battle);
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

        public void AddItemToInventory(IItem item) => throw new NotImplementedException();

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

        public bool TryApplyStatusEffect(StatusEffects statusEffect)
        {
            if ((StatusEffects & statusEffect) != 0) return false;
            StatusEffects |= statusEffect;
            Events.Publish(new StatusEffectAppliedEvent(this, statusEffect));
            return true;
        }

        public bool TryRemoveStatusEffect(StatusEffects statusEffect)
        {
            if ((StatusEffects & statusEffect) == 0) return false;
            StatusEffects &= ~statusEffect;
            Events.Publish(new StatusEffectRemovedEvent(this, statusEffect));
            return true;
        }


        public void AllAttacks()
        {
        }

        public void OnBlockAttack()
        {
        }

        public IEntity ChoseTarget(List<IEntity> targets) => throw new NotImplementedException();

        public void Kill() => Dead?.Invoke(this);

        public void OnEvadeAttack()
        {
        }

        public async Task ReceiveAttack(IAttackContext context)
        {
            if (_animatedSprite == null) return;
            _animatedSprite.Play("Fight_Hurt");
            TakeDamage(context.FinalDamage, DamageType.Normal, DamageSource.Hit);
            Events.Publish(new DamageTakenEvent(this, context));
            context.Attacker.Events.Publish(new AfterAttackEvent(context.Attacker, context));
            await ToSignal(_animatedSprite, "animation_finished");
            _animatedSprite.Play("Fight_Idle");
        }

        public async Task Attack(IAttackContext context)
        {
            context.IsCritical = context.Rnd.RandFloat() <= Parameters.CriticalChance;
            if (context.IsCritical)
                context.FinalDamage = Parameters.Damage * Parameters.CriticalDamage;
            Events.Publish(new BeforeAttackEvent(this, context));
            _animatedSprite.Play("Fight_Attack");
            await ToSignal(_animatedSprite, "animation_finished");
            _animatedSprite.Play("Fight_Idle");
        }

        public void OnTurnEnd()
        {
            Effects.TriggerTurnEnd();
            Events.Publish(new TurnEndEvent(this));
        }

        public void OnTurnStart()
        {
            Effects.TriggerTurnStart();
            Events.Publish(new TurnStartEvent(this));
        }

        public void TakeDamage(float damage, DamageType type, DamageSource source, bool isCrit = false)
        {
            CurrentHealth -= damage;
            GD.Print($"Player get damage: {damage}");
        }


        public void InjectServices(IGameServiceProvider provider)
        {
            _mediator = provider.GetService<IMediator>();
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
                    Position = new Vector2(320, 560);
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

        private void OnParameterChanged(EntityParameter parameter, float value)
        {
            switch (parameter)
            {
                case EntityParameter.Health:
                    CurrentHealth = value;
                    break;
                case EntityParameter.Barrier:
                    CurrentBarrier = value;
                    break;
                case EntityParameter.Mana:
                    CurrentMana = value;
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
                        value = 7000;
                        break;
                    case EntityParameter.Mana:
                        value = 50;
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
                        value = 0.05f;
                        break;
                    case EntityParameter.AdditionalHitChance:
                        value = 0.6f;
                        break;
                    case EntityParameter.CriticalDamage:
                        value = 1.5f;
                        break;
                    case EntityParameter.Damage:
                    case EntityParameter.SpellDamage:
                        value = 80f;
                        break;
                }

                Parameters.SetBaseValueForParameter(entityParameter, value);
            }
        }
    }
}
