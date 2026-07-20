namespace LastBreath.Player
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Battle.Source;
    using Components;
    using Core;
    using Core.Ai.World;
    using Core.Ai.World.Recovery;
    using Core.Ai.World.Time;
    using Core.Battle;
    using Core.Constants;
    using Core.Context;
    using Core.Data;
    using Core.Entity;
    using Core.Entity.Attribute;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Events;
    using Core.Items;
    using Core.Services;
    using Core.Views.UI;
    using Godot;
    using Stateless;

    public partial class Player : CharacterBody2D, IPlayer
    {
        public enum State
        {
            Idle,
            Walk,
            Fight,
            Dead,
        }

        private enum Trigger
        {
            Idle,
            Walk,
            Fight,
            Die,
            Revive,
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
        private readonly Core.Battle.DamageResolution.DamageResolutionChain _damageChain = Core.Battle.DamageResolution.DamageResolutionChain.CreateDefault();
        private Vector2 _lastPosition = Vector2.Zero;
        private Direction _direction;
        [Export] private AnimationsComponent? _animationsComponent;
        [Export] private Area2D? _interactionArea;
        [Export] private Camera2D? _camera;

        private IGameEventBus? _gameEventBus;
        private IBattleEventBus? _battleEventBus;
        private IUiContextService? _uiContext;
        private IWorldClock? _worldClock;
        private IPlayerLifecycleConfigProvider? _lifecycleConfigProvider;
        private IRestRecoveryService? _restRecovery;
        private IFightable? _lastDamageSource;

        /// <summary>Body state after a defeat; non-null only while lying dead (NPC burn scans read it).</summary>
        public PlayerLifecycle? Lifecycle { get; private set; }

        public string Id { get; }
        public string InstanceId { get; } = Guid.NewGuid().ToString();

        public Texture2D? Icon { get; }

        public string Description { get; }

        // Never-assigned auto-property left the battle log with nameless player entries.
        public string DisplayName => Core.Localization.Localization.Localize("Player");
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
        public IEntityGroup? Group { get; set; }
        public StatusEffects StatusEffects { get; set; } = StatusEffects.None;
        public bool CanMove { get; set; } = true;

        public Fractions Fractions { get; } = Fractions.Human;

        // why not x)
        public string PlayerName { get; private set; } = "Toddless";

        // TakeDamage owns the event order (damage beat first, death after) — see its comment.
        private bool _suppressDeathNotify;

        public float CurrentHealth
        {
            get => Mathf.Max(0, field);
            set
            {
                float clamped = Mathf.Clamp(value, 0, Parameters.MaxHealth);
                if (Mathf.Abs(clamped - field) < 0.0001f) return;
                field = clamped;
                if (field <= 0 && !_suppressDeathNotify) NotifyShouldDie();
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

        /// <summary>The battle flow reparents the player (world ↔ arena spot). Into battle the arena's
        /// own camera frames the field; back in the world the player re-takes the view, snapped to the
        /// new position instead of smoothing across half the map.</summary>
        public override void _Notification(int what)
        {
            if (what == (int)NotificationParented)
                Callable.From(OnReparented).CallDeferred();
        }

        private void OnReparented()
        {
            // Parented also fires during scene instantiation, before the camera enters the tree —
            // that first shot is _Ready's job (MakeCurrent there), not a real reparent.
            if (_camera == null || IsFighting || !_camera.IsInsideTree() || !_camera.Enabled) return;
            _camera.MakeCurrent();
            _camera.ResetSmoothing();
        }

        public override void _Ready()
        {
            _rnd.Randomize();
            if (_camera is { Enabled: true }) _camera.MakeCurrent();
            _gameEventBus = GameServiceProvider.Instance.GetService<IGameEventBus>();
            // Optional service (fail-open): drives the dialogue movement gate below.
            _uiContext = GameServiceProvider.Instance.GetServices<IUiContextService>().FirstOrDefault();
            _worldClock = GameServiceProvider.Instance.GetService<IWorldClock>();
            _lifecycleConfigProvider = GameServiceProvider.Instance.GetService<IPlayerLifecycleConfigProvider>();
            // Rest at a campfire: the recovery zones heal any registered non-fighting participant.
            _restRecovery = GameServiceProvider.Instance.GetService<IRestRecoveryService>();
            _restRecovery?.RegisterParticipant(this, () => GlobalPosition);
            // The player is a scene node, not a container-built service: self-register for UI/services
            GameServiceProvider.Instance.GetService<IPlayerAccessor>().Set(this);
            Parameters = new EntityParametersComponent();
            ParameterModifiers = new ParameterModifiersComponent();
            Parameters.Initialize(ParameterModifiers.GetModifiers);
            EquipmentComponent = new EquipmentComponent(this);
            ParameterModifiers.RegisterSource(EquipmentComponent);
            EquipmentComponent.EquipmentChanged += OnEquipmentChanged;
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
            // The barrier pool starts full like the other vitals: a Barrier stat that never
            // filled itself read as "barrier does not work" (damage went straight to health).
            CurrentBarrier = Parameters.MaxBarrier;
            _stances.Add(Stance.Intelligence, new IntelligenceStance(this));
            _stances.Add(Stance.Strength, new StrengthStance(this));
            _stances.Add(Stance.Dexterity, new DexterityStance(this));
        }

        // The recovery service outlives scene reloads — a stale position delegate on a freed node
        // would be a native call on a disposed object at the next world tick. Reparenting
        // (world ↔ arena) fires _ExitTree too, so _EnterTree symmetrically re-registers.
        public override void _ExitTree() => _restRecovery?.UnregisterParticipant(this);

        public override void _EnterTree() => _restRecovery?.RegisterParticipant(this, () => GlobalPosition);

        public override void _PhysicsProcess(double delta)
        {
            if (!IsAlive)
            {
                Lifecycle?.Tick(); // lying at the defeat spot: only the revive timer runs (game minutes)
                return;
            }

            if (!CanMove) return;
            // TODO:
            // Почему класс игрока что то знает о UI? Необходимо найти иной путь

            // A conversation freezes walking: movement is polled here, so without this gate the
            // player strolls away mid-dialogue. Fail-open — a project without the context tracker
            // (Battle sandbox) isn't gated, and Dialogue only ever fires in the world.
            if (_uiContext != null && (_uiContext.Current & UiContext.Dialogue) != 0)
            {
                Velocity = Vector2.Zero;
                return;
            }

            // Typing is not walking: WASD is polled, so a focused text field (debug console,
            // future chat) would otherwise drive the character while the user types.
            if (GetViewport().GuiGetFocusOwner() is LineEdit or TextEdit)
            {
                Velocity = Vector2.Zero;
                return;
            }

            Vector2 inputDirection = Input.GetVector(Settings.MoveLeft, Settings.MoveRight, Settings.MoveUp, Settings.MoveDown);
            Velocity = inputDirection * Parameters.GetValueForParameter(EntityParameter.MoveSpeed);
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

        /// <summary>Death by fiat (execute, tool). The killer keeps his credit — an execute is a kill —
        /// unless the caller says this is a debug death, which frames nobody.</summary>
        public void Kill(bool isDebug = false)
        {
            if (isDebug) _lastDamageSource = null;
            CurrentHealth = 0; // the setter publishes the death: IsAlive is health-based everywhere
        }

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
                context.Attacker.ModifierHandler.Apply(context);
                context.Attacker.CombatEvents.Publish(new AfterAttackEvent(context));
            }
            catch (Exception e)
            {
                Tracker.TrackException($"Failed to receive attack: {e.Message}, {e.StackTrace}", e, this);
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
            _lastDamageSource = context.Source; // killer attribution: whoever lands the lethal hit
            // Apply modifiers like "Reduce all damage taken"
            ModifierHandler.Apply(context);
            // apply attackers modifiers like "increase all damage dealt"
            context.Source.ModifierHandler.Apply(context);
            // passive/effects that react right before we are about to take some damage
            CombatEvents.Publish(new BeforeDamageTakenEvent(context));
            Calculations.CalculateMitigation(context, this);

            // Post-mitigation absorption layers (shield → barrier → stage guard); the leftover hits health.
            float remaining = _damageChain.Apply(context, this, context.TotalDamage);

            // The damage event must precede the death event in the timeline: the director drops
            // "posthumous" beats, so a death recorded first swallowed its own killing hit
            // (frozen bars, no log line). The setter's death notify is deferred past the publish.
            bool wasAlive = IsAlive;
            _suppressDeathNotify = true;
            if (remaining > 0) CurrentHealth -= remaining;
            _suppressDeathNotify = false;

            // Combat bus only: the timeline records it and the BattleDirector republishes it
            // to the battle bus at replay time, so UI reacts when the hit is SHOWN, not resolved.
            CombatEvents.Publish(new DamageTakenEvent(context, this, VitalsSnapshot.From(this)));
            if (wasAlive && !IsAlive) NotifyShouldDie();
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
            TurnRecovery.Apply(this);
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
                .Permit(Trigger.Idle, State.Idle)
                .Permit(Trigger.Die, State.Dead)
                // A duplicate battle-start signal must degrade to a no-op: throwing here used to
                // kill the whole battle setup before the NPCs reached the arena.
                .Ignore(Trigger.Fight);

            _stateMachine.Configure(State.Dead)
                .OnEntry(() =>
                {
                    Animations.PlayAnimation("Dead");
                    CanMove = false; // after Fight.OnExit restored it
                })
                .OnExit(() => CanMove = true)
                .Permit(Trigger.Revive, State.Idle)
                // A corpse has no other transitions; stray signals must not throw.
                .Ignore(Trigger.Idle)
                .Ignore(Trigger.Walk)
                .Ignore(Trigger.Fight);
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

        private void OnEffectsChanged()
        {
            _battleEventBus?.Publish<EffectsChangedEvent>(new(this, Effects.GetEffectViews()));
        }

        private void OnBattleEnds(BattleEndEvent obj)
        {
            // Own bus first: per-battle passive/effect state resets before the cleanup below
            CombatEvents.Publish(obj);
            _battleEventBus?.Unsubscribe<BattleEndEvent>(OnBattleEnds);
            _battleEventBus?.Unsubscribe<PlayerChangesStanceEvent>(OnStanceChanges);
            Effects.RemoveAllEffects();
            if (IsAlive) _stateMachine.Fire(Trigger.Idle);
            else BeginDeathRest();
            _battleEventBus = null;
        }

        /// <summary>Lost the battle: the body lies at the defeat spot for several game hours while
        /// the world is fast-forwarded; then revives with a fraction of health — unless a passer-by
        /// burns the corpse first (final death).</summary>
        private void BeginDeathRest()
        {
            _stateMachine.Fire(Trigger.Die);
            var config = _lifecycleConfigProvider?.Config ?? new PlayerLifecycleConfig();
            Lifecycle = new PlayerLifecycle(config, _worldClock!, new DefaultRandomNumberGenerator());
            Lifecycle.ReviveReady += OnReviveReady;
            Lifecycle.Burned += OnCorpseBurned;
            Lifecycle.OnDefeated();
            // NUANCE: Engine.TimeScale multiplies the PHYSICS tick rate of every node too, not just
            // the clocks. Fine at ~20 world NPCs; if the world grows to hundreds, switch to scaling
            // only the game timers (world clock + lifecycles) instead of the engine clock.
            Engine.TimeScale = config.DeadTimeScale;
        }

        private void OnReviveReady()
        {
            var config = Lifecycle!.Config;
            ClearLifecycle();
            Engine.TimeScale = 1;
            CurrentBarrier = 0;
            CurrentHealth = Parameters.MaxHealth * config.ReviveHealthPercent;
            CurrentMana = Parameters.MaxMana * config.ReviveManaPercent;
            _stateMachine.Fire(Trigger.Revive);
            _gameEventBus?.Publish(new PlayerRevivedEvent());
        }

        private void OnCorpseBurned()
        {
            ClearLifecycle();
            Engine.TimeScale = 1;
            // The body stays lying; the UI layer answers with the game-over screen.
            _gameEventBus?.Publish(new PlayerFinalDeathEvent());
        }

        private void ClearLifecycle()
        {
            if (Lifecycle == null) return;
            Lifecycle.ReviveReady -= OnReviveReady;
            Lifecycle.Burned -= OnCorpseBurned;
            Lifecycle = null;
        }

        private void NotifyShouldDie()
        {
            _gameEventBus?.Publish<EntityDiedEvent>(new(this, _lastDamageSource));
            _battleEventBus?.Publish<EntityDiedEvent>(new(this, _lastDamageSource));
            CombatEvents.Publish<EntityDiedEvent>(new(this, _lastDamageSource));
            // The arena deliberately ignores the player's EntityDiedEvent: PlayerDiedEvent is
            // the battle-ending signal (PlayerLost). Without it the battle loop never exits.
            _gameEventBus?.Publish<PlayerDiedEvent>(new(this));
            _battleEventBus?.Publish<PlayerDiedEvent>(new(this));
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
                case EntityParameter.Barrier:
                    _battleEventBus?.Publish<PlayerBarrierChangesEvent>(new(this, value));
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
            EntityParameter.Health => 1000,
            EntityParameter.Barrier => 100,
            EntityParameter.Mana => 500,
            EntityParameter.Intelligence or EntityParameter.Strength or EntityParameter.Dexterity => 5f,
            EntityParameter.Evade or EntityParameter.Armor or EntityParameter.Accuracy => 300,
            EntityParameter.CriticalChance => 0.05f,
            // A rare treat, not a machine gun: extra attacks chain (each one re-rolls), so a high
            // base made attack series balloon to 2-3x their planned length. Items/passives are
            // the intended source of this stat.
            EntityParameter.AdditionalHitChance => 0.05f,
            EntityParameter.CriticalDamage => 1.5f,
            EntityParameter.MulticastChance => 0f,
            EntityParameter.Damage => 100,
            EntityParameter.SpellDamage => 50,
            EntityParameter.MoveSpeed => 500,
            _ => 0f
        };

        // Weapon replaces the base of these parameters (not a modifier): base = weapon stats, unarmed profile otherwise.
        private void OnEquipmentChanged(EquipmentPiece piece, IEquipItem? item)
        {
            if (piece != EquipmentPiece.Weapon) return;
            var weapon = EquipmentComponent.Weapon;
            Parameters.SetBaseValueForParameter(EntityParameter.Damage, weapon?.Damage ?? GetUnarmedBaseValue(EntityParameter.Damage));
            Parameters.SetBaseValueForParameter(EntityParameter.CriticalChance, weapon?.CriticalChance ?? GetUnarmedBaseValue(EntityParameter.CriticalChance));
            Parameters.SetBaseValueForParameter(EntityParameter.CriticalDamage, weapon?.CriticalDamage ?? GetUnarmedBaseValue(EntityParameter.CriticalDamage));
        }

        public Vector2 GetCameraPosition() => GlobalPosition;
    }
}
