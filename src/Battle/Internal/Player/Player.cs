namespace Battle.Internal.Player
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core;
    using Core.Attribute;
    using Core.Battle;
    using Core.Components;
    using Core.Constants;
    using Core.Context;
    using Core.Data;
    using Core.Entity;
    using Core.Enums;
    using Core.Events;
    using Core.Events.GameEvents;
    using Core.Items;
    using Core.Services;
    using Godot;
    using Source;
    using Stateless;
    using AnimationsComponent = Components.AnimationsComponent;
    using GameServiceProvider = Services.GameServiceProvider;

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
        public IAbilityBookComponent AbilityBook { get; private set; }
        public IEquipmentComponent EquipmentComponent { get; private set; }
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
        public IEquipmentComponent Equipment { get; private set; }
        public IEntityGroup? Group { get; set; }
        public StatusEffects StatusEffects { get; set; } = StatusEffects.None;
        public bool CanMove { get; set; } = true;
        public string Name { get; private set; } = string.Empty;

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

        public override void _Ready()
        {
            if (_interactionArea != null) _interactionArea.BodyEntered += OnBodyEnter;

            _rnd.Randomize();
            _gameEventBus = GameServiceProvider.Instance.GetService<IGameEventBus>();
            // The player is a scene node, not a container-built service: self-register for UI/services
            GameServiceProvider.Instance.GetService<IPlayerAccessor>().Set(this);
            Parameters = new EntityParametersComponent();
            ParameterModifiers = new ParameterModifiersComponent();
            Parameters.Initialize(ParameterModifiers.GetModifiers);
            Equipment = new EquipmentComponent(this);
            ParameterModifiers.RegisterSource(Equipment);
            Equipment.EquipmentChanged += OnEquipmentChanged;
            Effects = new EffectsComponent(this);
            PassiveSkills = new PassiveSkillsComponent(this);
            Dexterity = new Dexterity(ParameterModifiers);
            Strength = new Strength(ParameterModifiers);
            Intelligence = new Intelligence(ParameterModifiers);
            ModifierHandler = new ModifierHandlerComponent();
            AbilityBook = new AbilityBookComponent(this);
            Effects.EffectsChanged += OnEffectsChanged;
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
            EquipmentComponent = new EquipmentComponent(this);
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
        }

        public void AddItemToInventory(IItem item)
        {
        }

        public float GetDamage() => _rnd.RandfRange(0.9f, 1.1f) * Parameters.Damage;

        public void SetupBattleEventBus(IBattleEventBus bus)
        {
            _battleEventBus = bus;
            _stateMachine.Fire(Trigger.Fight);
            _battleEventBus.Subscribe<BattleEndEvent>(OnBattleEnds);
            _battleEventBus.Subscribe<PlayerChangesStanceEvent>(OnStanceChanges);
        }

        private void OnStanceChanges(PlayerChangesStanceEvent obj)
        {
            CurrentStance?.OnDeactivate();
            CurrentStance = _stances.GetValueOrDefault(obj.Stance);
            CurrentStance?.OnActivate();
            AbilityBook.SetStance(obj.Stance);
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

        public async Task ReceiveAttack(IAttackContext context)
        {
            try
            {
                Calculations.CalculateSucceeded(context);
                switch (context.Result)
                {
                    case AttackResults.Succeed:
                        Calculations.CalculateInitialAttackDamage(context);
                        var damageContext = new DamageContext
                        {
                            Source = context.Attacker,
                            Cause = DamageCause.Attack,
                            IsCrit = context.ForceCriticalAttack || context.IsCritical,
                            SourceAbilityId = context.SourceAbilityId
                        };
                        damageContext.Add(DamageType.Physical, context.FinalDamage);
                        await TakeDamage(damageContext);
                        context.FinalDamage = damageContext.TotalDamage; // actual damage dealt to target (barrier-absorbed included)
                        break;
                    case AttackResults.Blocked:
                        CombatEvents.Publish<AttackBlockedEvent>(new(context));
                        break;
                    case AttackResults.Evaded:
                        CombatEvents.Publish<AttackEvadedEvent>(new(context));
                        break;
                }

                // Single post-attack channel: all reactions (effects, passives, upgrades) subscribe to this event
                context.Attacker.CombatEvents.Publish(new AfterAttackEvent(context));
            }
            catch (Exception e)
            {
                GD.Print($"{e.Message}, {e.StackTrace}");
            }
        }

        public Task Attack(IAttackContext context)
        {
            // BeforeAttack reactions may mutate RawCriticalChance, so the crit roll happens after them
            CombatEvents.Publish(new BeforeAttackEvent(context));
            context.IsCritical = context.Rnd.Randf() <= context.RawCriticalChance;
            return Task.CompletedTask;
        }

        public Task TakeDamage(IDamageContext context)
        {
            // Apply modifiers like "Reduce all damage taken"
            ModifierHandler.Apply(context);
            // apply attackers modifiers like "increase all damage dealt"
            context.Source.ModifierHandler.Apply(context);
            // passive/effects that react right before we are about to take some damage
            CombatEvents.Publish(new BeforeDamageTakenEvent(context));
            Calculations.CalculateMitigation(context, this);

            float remaining = context.TotalDamage;
            if (CurrentBarrier > 0)
            {
                float absorbed = Mathf.Min(CurrentBarrier, remaining);
                context.AbsorbedByBarrier = absorbed;
                CurrentBarrier -= absorbed;
                remaining -= absorbed;
            }

            if (remaining > 0) CurrentHealth -= remaining;

            // Combat bus only: the timeline records it and the BattleDirector republishes it
            // to the battle bus at replay time, so UI reacts when the hit is SHOWN, not resolved.
            CombatEvents.Publish(new DamageTakenEvent(context, this, VitalsSnapshot.From(this)));
            return Task.CompletedTask;
        }

        public void Heal(IHealContext context)
        {
            ModifierHandler.Apply(context);
            if (context.Amount <= 0) return;
            if (context.ConvertToDamage)
            {
                var damageContext = new DamageContext { Source = context.Source, Cause = DamageCause.Passive };
                damageContext.Add(DamageType.Pure, context.Amount);
                _ = TakeDamage(damageContext);
                return;
            }

            float amount = context.Amount;
            CurrentHealth += amount; // applied before publishing so the snapshot reflects the post-heal state
            CombatEvents.Publish(new EntityHealedEvent(this, amount, VitalsSnapshot.From(this)));
        }

        public void OnTurnStart()
        {
            Effects.TriggerTurnStart();
            CombatEvents.Publish(new TurnStartEvent(this));
            _battleEventBus?.Publish(new TurnStartEvent(this));
            _gameEventBus?.Publish(new TurnStartEvent(this));
        }

        public void OnTurnEnd()
        {
            Effects.TriggerTurnEnd();
            CombatEvents.Publish(new TurnEndEvent());
            _battleEventBus?.Publish(new TurnEndEvent());
            _gameEventBus?.Publish(new TurnEndEvent());
        }

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
                    Animations.PlayAnimation($"Idle_{_direction}");
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

        private void OnEffectsChanged()
        {
            _battleEventBus?.Publish<EffectsChangedEvent>(new(this, Effects.GetEffectViews()));
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
            _gameEventBus?.Publish<EntityDiedEvent>(new(this));
            _battleEventBus?.Publish<EntityDiedEvent>(new(this));
            CombatEvents.Publish<EntityDiedEvent>(new(this));
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
                Parameters.SetBaseValueForParameter(entityParameter, GetUnarmedBaseValue(entityParameter));
        }

        private static float GetUnarmedBaseValue(EntityParameter parameter) => parameter switch
        {
            EntityParameter.Health or EntityParameter.Barrier => 10000,
            EntityParameter.Mana => 5000,
            EntityParameter.Intelligence or EntityParameter.Strength or EntityParameter.Dexterity => 5f,
            EntityParameter.Evade or EntityParameter.Armor or EntityParameter.Accuracy => 500,
            EntityParameter.CriticalChance => 0.25f,
            EntityParameter.AdditionalHitChance => 0.6f,
            EntityParameter.CriticalDamage => 1.5f,
            EntityParameter.MulticastChance => 1f,
            EntityParameter.Damage or EntityParameter.SpellDamage => 300,
            _ => 0f
        };

        // Weapon replaces the base of these parameters (not a modifier): base = weapon stats, unarmed profile otherwise.
        private void OnEquipmentChanged(EquipmentPiece piece, IEquipItem? item)
        {
            if (piece != EquipmentPiece.Weapon) return;
            var weapon = Equipment.Weapon;
            Parameters.SetBaseValueForParameter(EntityParameter.Damage, weapon?.Damage ?? GetUnarmedBaseValue(EntityParameter.Damage));
            Parameters.SetBaseValueForParameter(EntityParameter.CriticalChance, weapon?.CriticalChance ?? GetUnarmedBaseValue(EntityParameter.CriticalChance));
            Parameters.SetBaseValueForParameter(EntityParameter.CriticalDamage, weapon?.CriticalDamage ?? GetUnarmedBaseValue(EntityParameter.CriticalDamage));
        }

        public Vector2 GetCameraPosition() => GlobalPosition;
    }
}
