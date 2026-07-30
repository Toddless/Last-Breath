namespace Battle.Internal.Npc
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Components;
    using Core;
    using Core.Ai;
    using Core.Ai.World;
    using Core.Ai.World.Activities;
    using Core.Ai.World.Recovery;
    using Core.Ai.World.Skirmish;
    using Core.Ai.World.SmartPoints;
    using Core.Ai.World.Time;
    using Core.Battle;
    using Core.Context;
    using Core.Data;
    using Core.Data.NpcData;
    using Core.Entity;
    using Core.Entity.Attribute;
    using Core.Entity.Components;
    using Core.Entity.NpcModifiers;
    using Core.Enums;
    using Core.Events;
    using Core.Items;
    using Core.Modifiers;
    using Core.Narrative.Facts;
    using Core.Services;
    using Godot;
    using Source;
    using GameServiceProvider = Services.GameServiceProvider;

    public partial class BaseNpc : CharacterBody2D, IFightableNpc, IWorldAgent, ISkirmishParticipant
    {
        /// <summary>Close enough to a movement destination to stop.</summary>
        private const float ArriveDistance = 5f;

        /// <summary>How close the player must stand to burn a body.</summary>
        private const float BurnDistance = 150f;

        /// <summary>Hostile NPCs this close start an abstract skirmish (NPC-vs-NPC contact distance).</summary>
        private const float SkirmishEngageDistance = 90f;

        /// <summary>Skirmish opportunities are scanned this often, not every physics frame.</summary>
        private const float SkirmishScanInterval = 0.5f;

        /// <summary>No battle-starting contact right after a battle: both sides return to the
        /// engagement point overlapping each other — without this pause the fight restarts
        /// instantly (flee was pointless, survivors chained battles).</summary>
        private const float PostBattleContactGraceSeconds = 3f;

        private const string UndeadRisingModifierSource = "UndeadRising";
        private readonly Core.Battle.DamageResolution.DamageResolutionChain _damageChain = Core.Battle.DamageResolution.DamageResolutionChain.CreateDefault();
        private const string UID = "uid://ww6a71b2bbov";
        [Export] private Area2D? _interactionArea;
        private Vector2 _lastPosition = Vector2.Zero;
        private float _contactGraceSeconds;
        private IGameEventBus? _gameEventBus;
        private IBattleEventBus? _battleEventBus;
        private IPlayerAccessor? _playerAccessor;
        private IFactionRelationService? _factionRelations;
        private INpcWorldRegistry? _npcRegistry;
        private INpcSkirmishService? _skirmishService;
        private IWorldClock? _worldClock;
        private IWorldBrain? _brain;
        private INpcLifecycle? _lifecycle;
        private ISmartPointRegistry? _smartPoints;
        private IWorldFactsService? _worldFacts;
        private IRecoveryConfigProvider? _recoveryConfig;
        private IRestRecoveryService? _recovery;
        private bool _hostileToPlayer;
        private float _skirmishScanCooldown;
        private float _corpseScanCooldown;
        private IReadOnlyList<Vector2>? _patrolRoute;
        private Vector2? _moveDestination;
        private float _moveSpeed;
        private string _lastMoveAnimation = string.Empty;

        private float _baseSpeed = 500;
        private readonly RandomNumberGenerator _rnd = new();
        [Export] private AnimationsComponent? _animationsComponent;

        /// <summary>The fighter's own roll stream behind the domain contract: defensive rolls
        /// (suppression) burn it instead of an anonymous generator built for a single hit.</summary>
        private IRandomNumberGenerator CombatRolls => field ??= new GodotRandomNumberGenerator(_rnd);

        [Export] public string Id { get; private set; } = string.Empty;
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        [Export] public string[] Tags { get; private set; } = [];
        public Texture2D? Icon { get; } = null;
        public string Description { get; } = string.Empty;
        public string DisplayName { get; } = string.Empty;
        public IEntityParametersComponent Parameters { get; private set; }
        public IPassiveSkillsComponent PassiveSkills { get; private set; }
        public IAnimationsComponent Animations => _animationsComponent;
        public IModifierHandlerComponent ModifierHandler { get; private set; }
        public IAbilityBookComponent AbilityBook { get; private set; }
        public IEntityAttribute Dexterity { get; private set; }
        public IEntityAttribute Strength { get; private set; }
        public IEntityAttribute Intelligence { get; private set; }
        public ICombatEventBus CombatEvents { get; private set; }
        public IStance CurrentStance { get; private set; }
        public ITargetChooser? TargetChooser { get; set; }
        public bool IsFighting { get; set; }
        public bool IsAlive => CurrentHealth > 0;
        public IEffectsComponent Effects { get; private set; }
        public IParameterModifiersComponent ParameterModifiers { get; private set; }
        public IEntityGroup? Group { get; set; }
        public StatusEffects StatusEffects { get; set; } = StatusEffects.None;
        public bool CanMove { get; set; }
        public int Level { get; private set; } = 150;
        public Rarity Rarity { get; private set; } = Rarity.Legendary;
        public EntityType EntityType { get; private set; } = EntityType.Regular;
        public Fractions Fraction { get; private set; } = Fractions.Human;
        public INpcModifiersComponent NpcModifiers { get; private set; }
        public IBehaviorProfile? Behavior { get; set; }

        /// <summary>Combat reactions from the definition; the arena's reactions driver reads them.</summary>
        public IReadOnlyList<NpcReactionConfig> Reactions { get; private set; } = [];

        /// <summary>Boss stages from the definition; the arena's stages controller reads them.</summary>
        public IReadOnlyList<NpcStageConfig> Stages { get; private set; } = [];

        /// <summary>Index into <see cref="Stages"/> this NPC currently fights in.</summary>
        public int CurrentStageIndex { get; private set; }

        /// <summary>Definition originals of the base parameters: stage multipliers always scale
        /// from these, so switching stages never loses the base.</summary>
        private IReadOnlyDictionary<EntityParameter, float>? _definitionParameters;

        /// <summary>Body state for the save system; null until <see cref="ApplyDefinition"/> ran (legacy NPCs).</summary>
        public INpcLifecycle? Lifecycle => _lifecycle;

        /// <summary>True after this NPC rose as undead — the save system persists risen ones as world deviations.</summary>
        public bool IsRisen { get; private set; }

        /// <summary>Battle-scoped summon: no loot, no experience, no corpse, no world return.
        /// Raised once by the battle summon spawner right after the definition is applied.</summary>
        public bool IsSummon { get; private set; }

        /// <summary>Species capability from the definition (interaction.canTalk); hostility never changes it.</summary>
        public bool CanTalk { get; private set; }

        /// <summary>The rising's parameter bonus, kept for the save round-trip.</summary>
        public float RisingBonus { get; private set; }

        // ---- IWorldAgent (the brain's view of this body) ----
        public Vector2 HomePosition { get; private set; }

        /// <summary>World-space position; Node2D.Position is parent-local and must not leak into the brain.</summary>
        Vector2 IWorldAgent.Position => GlobalPosition;

        Vector2 ISkirmishParticipant.Position => GlobalPosition;

        // TakeDamage owns the event order (damage beat first, death after) — see its comment.
        private bool _suppressDeathNotify;
        // Who landed the last hit: the killer reported with the death (reputation, kill facts, loot).
        // Cleared by a world defeat and by a debug kill — neither has anyone to blame.
        private IFightable? _lastDamageSource;

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

        public override void _Ready()
        {
            _interactionArea?.BodyEntered += OnBodyEnter;
            _interactionArea?.InputEvent += OnInteractionAreaInput;

            _rnd.Randomize();
            Parameters = new EntityParametersComponent();
            ParameterModifiers = new ParameterModifiersComponent();
            Parameters.Initialize(ParameterModifiers.GetModifiers);
            Effects = new EffectsComponent(this);
            PassiveSkills = new PassiveSkillsComponent(this);
            Dexterity = new Dexterity(ParameterModifiers);
            Strength = new Strength(ParameterModifiers);
            Intelligence = new Intelligence(ParameterModifiers);
            NpcModifiers = new NpcModifiersComponent(this);
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

            CurrentHealth = Parameters.MaxHealth;
            CurrentMana = Parameters.MaxMana;
            CurrentBarrier = Parameters.MaxBarrier; // starts full like the other vitals
        }

        public void InjectServices(IGameServiceProvider provider)
        {
            _gameEventBus = GameServiceProvider.Instance.GetService<IGameEventBus>();
            _playerAccessor = GameServiceProvider.Instance.GetService<IPlayerAccessor>();
            _factionRelations = GameServiceProvider.Instance.GetService<IFactionRelationService>();
            _npcRegistry = GameServiceProvider.Instance.GetService<INpcWorldRegistry>();
            _skirmishService = GameServiceProvider.Instance.GetService<INpcSkirmishService>();
            _worldClock = GameServiceProvider.Instance.GetService<IWorldClock>();
            _smartPoints = GameServiceProvider.Instance.GetService<ISmartPointRegistry>();
            _worldFacts = GameServiceProvider.Instance.GetService<IWorldFactsService>();
            _recoveryConfig = GameServiceProvider.Instance.GetService<IRecoveryConfigProvider>();
            _recovery = GameServiceProvider.Instance.GetService<IRestRecoveryService>();
            _npcRegistry.Register(this);
            _recovery?.RegisterParticipant(this, () => GlobalPosition);
            _gameEventBus.Subscribe<WorldStimulusEvent>(OnWorldStimulus);
        }


        public override void _PhysicsProcess(double delta)
        {
            if (!IsAlive)
            {
                _lifecycle?.Tick((float)delta); // a lying body: only the resurrection timer runs
                return;
            }

            if (_contactGraceSeconds > 0) _contactGraceSeconds -= (float)delta;
            _brain?.Tick((float)delta);
            TryScanForSkirmish((float)delta);
            TryScanForPlayerCorpse((float)delta);
            ProcessLocomotion();
        }

        /// <summary>Humanoid passers-by may burn the player's corpse (the lifecycle rolls the
        /// chance ONCE per NPC per death — standing next to the body doesn't re-roll).</summary>
        private void TryScanForPlayerCorpse(float delta)
        {
            _corpseScanCooldown -= delta;
            if (_corpseScanCooldown > 0) return;
            _corpseScanCooldown = SkirmishScanInterval;

            if (Fraction is not (Fractions.Human or Fractions.Dwarf or Fractions.Elf)) return;
            if (_playerAccessor?.Player is not Player.Player { IsAlive: false, Lifecycle: { } lifecycle } corpse) return;
            if (GlobalPosition.DistanceTo(corpse.GlobalPosition) > lifecycle.Config.BurnRadius) return;

            lifecycle.TryBurnRoll(InstanceId); // the player reacts to the Burned event itself
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        /// <summary>
        /// Turns the randomly-initialized NPC into a data-driven one: overrides the rolled base
        /// parameters, fixes the stance, learns the rolled abilities (Learn auto-equips them)
        /// and attaches the combat behavior. Call after _Ready has built the components.
        /// </summary>
        public void ApplyDefinition(NpcDefinition definition)
        {
            Id = definition.NpcId;
            Level = definition.Level;
            Rarity = definition.Rarity;
            EntityType = definition.EntityType;
            Fraction = definition.Fraction;
            Behavior = definition.Behavior;
            Reactions = definition.Reactions;
            Stages = definition.Stages;
            CanTalk = definition.CanTalk;
            _definitionParameters = definition.Parameters;

            ApplyVisual(definition.NpcId);

            foreach ((EntityParameter parameter, float value) in definition.Parameters)
                Parameters.SetBaseValueForParameter(parameter, value);

            AbilityBook.SetStance(definition.Stance);
            foreach (var ability in definition.Abilities)
                AbilityBook.Learn(definition.Stance, ability);

            // A staged boss opens weakened: stage 0 scales the just-written bases and owns the ability set.
            if (Stages.Count > 0) ApplyStage(0);

            // Loot-side today (difficulty/budget); parameter buffs come when NpcBuffId gets a consumer.
            NpcModifiers.AddModifiers(definition.Modifiers.ToList());

            GrantControlResistance();
            Battle.Source.ExhaustionGrant.Attach(this);
            AttachAuthoredPassives(definition.Passives);

            CurrentHealth = Parameters.MaxHealth;
            CurrentMana = Parameters.MaxMana;
            CurrentBarrier = Parameters.MaxBarrier; // starts full like the other vitals

            AttachWorldBrain(definition.World);

            _lifecycle = new NpcLifecycle(definition.Lifecycle, new DefaultRandomNumberGenerator());
            _lifecycle.ResurrectionReady += OnResurrectionReady;
        }

        /// <summary>Per-NPC art from the shared visual library; no entry — the scene's placeholder frames stay.</summary>
        private void ApplyVisual(string npcId)
        {
            var config = GameServiceProvider.Instance.GetService<Battle.Source.Presentation.INpcVisualProvider>()?.GetVisual(npcId);
            if (config?.Frames == null) return;
            _animationsComponent?.ApplyVisual(config.Frames, config.Scale);
        }

        /// <summary>Bosses and archons get diminishing returns on hard control (CombatRules.json);
        /// the resistance fades over the bearer's turns, so the decay ticks on own turn end.</summary>
        private void GrantControlResistance()
        {
            var rules = GameServiceProvider.Instance.GetService<Core.Battle.ICombatRulesProvider>().ControlResistance;
            if (!rules.AppliesTo.Contains(EntityType)) return;
            var resistance = new Core.Modifiers.Context.ControlResistanceModifier(rules);
            ModifierHandler.Add(resistance);
            CombatEvents.Subscribe<TurnEndEvent>(_ => resistance.DecayTick());
        }

        /// <summary>Authored passives of the kit (Deep Wounds etc.): resolved through the skill
        /// registry — an unknown id or a missing property is reported by the provider and skipped;
        /// a sandbox without the registry simply attaches nothing.</summary>
        private void AttachAuthoredPassives(IReadOnlyList<NpcPassiveData> passives)
        {
            if (passives.Count == 0) return;
            var provider = GameServiceProvider.Instance.GetService<Core.Battle.Skills.ISkillProvider>();
            if (provider == null) return;

            foreach (var entry in passives)
            {
                var skill = provider.CreateSkill(entry.Id, new Core.Battle.Skills.SkillProperties(entry.Id, entry.Properties));
                if (skill != null) PassiveSkills.AddSkill(skill);
            }
        }

        /// <summary>
        /// Boss stage switch: base parameters scale from the definition originals (the base is never
        /// lost) and the ability book is rebuilt to the stage's set. On-attack effects of the stage
        /// are battle-scoped and wired by the arena's BossStagesController.
        /// </summary>
        public void ApplyStage(int stageIndex)
        {
            if (stageIndex < 0 || stageIndex >= Stages.Count || _definitionParameters == null) return;

            var stage = Stages[stageIndex];
            CurrentStageIndex = stageIndex;

            foreach ((EntityParameter parameter, float value) in _definitionParameters)
                Parameters.SetBaseValueForParameter(parameter, value * stage.ParameterMultiplier);

            ReplaceStageAbilities(stage);
        }

        /// <summary>Unknown ids are reported and skipped — a typo must not abort the stage switch.</summary>
        private void ReplaceStageAbilities(NpcStageConfig stage)
        {
            foreach (var learned in AbilityBook.AllAbilities)
                AbilityBook.Forget(learned.InstanceId);

            var provider = GameServiceProvider.Instance.GetService<Core.Battle.Abilities.IAbilityProvider>();
            foreach (string abilityId in stage.Abilities)
            {
                if (!provider.KnownAbilityIds.Contains(abilityId))
                {
                    Tracker.TrackNotFound($"Stage ability '{abilityId}' of npc '{Id}'", this);
                    continue;
                }

                AbilityBook.Learn(AbilityBook.CurrentStance, provider.CreateAbility(abilityId));
            }
        }

        /// <summary>Must be set before <see cref="ApplyDefinition"/> — the brain takes the route at construction.</summary>
        public void SetPatrolRoute(IReadOnlyList<Vector2> points) => _patrolRoute = points;

        /// <summary>The spawn position becomes home: the leash and calm activities anchor to it.</summary>
        private void AttachWorldBrain(WorldBrainConfig? config)
        {
            HomePosition = GlobalPosition;
            if (config == null) return;

            _hostileToPlayer = config.HostileToPlayer;
            var context = new WorldActivityContext
            {
                PatrolRoute = _patrolRoute,
                Clock = _worldClock,
                Npcs = _npcRegistry,
                Relations = _factionRelations,
                Points = _smartPoints,
                Facts = _worldFacts,
                Recovery = _recoveryConfig?.Config,
                Self = this,
            };
            _brain = new WorldBrain(this, config, new DefaultRandomNumberGenerator(), context);
            CanMove = true;
        }

        // ---- activity poses (world flavor; a missing clip degrades to Idle) ----
        public void SetActivityPose(string clip)
        {
            _lastMoveAnimation = string.Empty; // the next movement must re-apply its directional clip
            Animations.PlayAnimation(Animations.HasClip(clip) ? clip : "Idle_Down");
        }

        public void ClearActivityPose()
        {
            _lastMoveAnimation = string.Empty;
            Animations.PlayAnimation("Idle_Down");
        }

        public float HealthPercent => Parameters.MaxHealth > 0 ? CurrentHealth / Parameters.MaxHealth : 1f;

        public void MoveTo(Vector2 destination, float speed)
        {
            CanMove = true; // symmetric to StopMoving's freeze: a new intent unfreezes the body
            _moveDestination = destination;
            _moveSpeed = speed;
        }

        public void StopMoving()
        {
            CanMove = false;
            _lastPosition = Position;
            _moveDestination = null;
            Velocity = Vector2.Zero;
        }

        /// <summary>
        /// Vision by distance polling (no physics layers involved). Only enemies are reported —
        /// the nearest of: the player (personal override or faction standing) and hostile NPCs
        /// (faction matrix). TODO: line-of-sight raycast when collision layers are defined.
        /// </summary>
        public TargetSighting? GetSighting(float visionRadius)
        {
            Vector2? nearest = null;
            float nearestDistance = visionRadius;

            if (ConsidersPlayerAnEnemy() && _playerAccessor?.Player is { IsAlive: true } and Node2D playerNode)
                Consider(playerNode.GlobalPosition, ref nearest, ref nearestDistance);

            if (_npcRegistry != null && _factionRelations != null)
            {
                foreach (var other in _npcRegistry.All)
                {
                    if (!IsSkirmishableEnemy(other)) continue;
                    Consider(other.Position, ref nearest, ref nearestDistance);
                }
            }

            return nearest == null ? null : new TargetSighting(nearest.Value);
        }

        /// <summary>Bandits/beasts carry a personal override; everyone else follows the player's faction standing.
        /// Public: the battle-site marker asks it to turn arrivals into join requests.</summary>
        public bool ConsidersPlayerAnEnemy() =>
            _hostileToPlayer || _factionRelations?.IsHostileToPlayer(Fraction) == true;

        private void Consider(Vector2 candidate, ref Vector2? nearest, ref float nearestDistance)
        {
            float distance = GlobalPosition.DistanceTo(candidate);
            if (distance > nearestDistance) return;
            nearest = candidate;
            nearestDistance = distance;
        }

        /// <summary>An NPC worth chasing/fighting: alive, free and hostile in either direction.</summary>
        private bool IsSkirmishableEnemy(ISkirmishParticipant other)
        {
            if (other.InstanceId == InstanceId || !other.IsAlive || other.IsFighting) return false;
            return _factionRelations!.IsHostile(Fraction, other.Fraction) ||
                   _factionRelations.IsHostile(other.Fraction, Fraction);
        }

        /// <summary>Hostile NPC at contact distance → the abstract skirmish takes both squads over.</summary>
        private void TryScanForSkirmish(float delta)
        {
            if (_brain == null || _skirmishService == null || _npcRegistry == null || _factionRelations == null) return;
            if (IsFighting) return;

            _skirmishScanCooldown -= delta;
            if (_skirmishScanCooldown > 0) return;
            _skirmishScanCooldown = SkirmishScanInterval;

            foreach (var other in _npcRegistry.All)
            {
                if (!IsSkirmishableEnemy(other)) continue;
                if (GlobalPosition.DistanceTo(other.Position) > SkirmishEngageDistance) continue;
                if (_skirmishService.TryStart(this, other)) return;
            }
        }

        /// <summary>
        /// Straight-line steering with collision sliding — enough for the small open 2D world.
        /// Seam for later: swap the direction source to NavigationAgent2D once a navmesh exists.
        /// </summary>
        private void ProcessLocomotion()
        {
            if (_moveDestination == null || !CanMove || IsFighting) return;

            var toDestination = _moveDestination.Value - GlobalPosition;
            if (toDestination.Length() <= ArriveDistance)
            {
                StopMoving();
                return;
            }

            Velocity = toDestination.Normalized() * _moveSpeed;
            MoveAndSlide();
            UpdateMoveAnimation(Velocity);
        }

        // TODO: switch to Walk_* clips when they exist; Idle_* keeps the direction readable for now.
        private void UpdateMoveAnimation(Vector2 velocity)
        {
            string clip = Mathf.Abs(velocity.X) >= Mathf.Abs(velocity.Y)
                ? velocity.X >= 0 ? "Idle_Right" : "Idle_Left"
                : velocity.Y >= 0
                    ? "Idle_Down"
                    : "Idle_Up";

            if (clip == _lastMoveAnimation) return;
            _lastMoveAnimation = clip;
            Animations.PlayAnimation(clip);
        }

        private void OnWorldStimulus(WorldStimulusEvent evnt) => _brain?.OnStimulus(evnt.Stimulus);

        public void AddItemToInventory(IItem item)
        {
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
                        var damageContext = Calculations.ComposeAttackDamage(context);
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
            _lastDamageSource = context.Source; // killer attribution: whoever lands the lethal hit
            ModifierHandler.Apply(context);
            context.Source.ModifierHandler.Apply(context);
            CombatEvents.Publish(new BeforeDamageTakenEvent(context));
            Calculations.CalculateMitigation(context, this, CombatRolls);

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

        public float GetDamage() => _rnd.RandfRange(0.9f, 1.1f) * Parameters.Damage;

        public void SetupBattleEventBus(IBattleEventBus bus)
        {
            _battleEventBus = bus;
            _battleEventBus.Subscribe<BattleEndEvent>(OnBattleEnd);
        }

        /// <summary>Symmetric detach: a summon freed mid-battle must not hear the later
        /// BattleEndEvent — the handler touches the freed native side.</summary>
        public void RemoveBattleEventBus()
        {
            _battleEventBus?.Unsubscribe<BattleEndEvent>(OnBattleEnd);
            _battleEventBus = null;
        }

        /// <summary>Marks the body as a battle-scoped summon (see <see cref="IsSummon"/>).</summary>
        public void MarkAsSummon() => IsSummon = true;

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

        /// <summary>An execute on a staged boss with a pending transition converts into the
        /// transition instead of a death: the lethal blow routes through TakeDamage, where the
        /// stage-guard floor clamps it to the threshold (design: the floor guards executes too).
        /// A plain execute keeps the killer his credit (the last damage source — an execute follows
        /// its own hit); only a debug/tool death frames nobody.</summary>
        public void Kill(bool isDebug = false)
        {
            bool guarded = IsAlive && Effects.GetBy(e => e is Core.Battle.Abilities.IStageGuardEffect).Any();
            if (!guarded)
            {
                if (isDebug) _lastDamageSource = null;
                CurrentHealth = 0; // the setter publishes the death: IsAlive is health-based everywhere
                return;
            }

            var lethal = new DamageContext { Source = this, Cause = DamageCause.Ability };
            lethal.Add(DamageType.Pure, CurrentHealth);
            _ = TakeDamage(lethal);
        }

        public IFightable ChoseTarget(List<IFightable> targets)
        {
            TargetChooser ??= new ChoosePlayerAsTarget();

            return TargetChooser.Choose(targets);
        }

        private void OnBodyEnter(Node2D body)
        {
            if (IsFighting || !IsAlive) return; // a lying body must not start battles
            if (_contactGraceSeconds > 0) return; // fresh out of a battle: let the loser leave
            // The player's flag guards the battle-start window: a second NPC touching in the same
            // frame must not publish a second BattleInitializedEvent. A dead player is a corpse,
            // not a battle target.
            if (body is not IPlayer player || player.IsFighting || !player.IsAlive) return;
            // Contact is not a war declaration: a neutral local (merchant, future villagers) bumped
            // into is no battle. Hostility comes from the standing/personal layers, not the touch.
            if (!ConsidersPlayerAnEnemy()) return;
            try
            {
                List<IFightable> fighters = [];
                if (Group != null)
                {
                    // Only free, living squadmates join: a member frozen in an NPC skirmish must not
                    // fight in two battles at once (tracker #62). Filter BEFORE the Attacked
                    // notification — it raises IsFighting on the whole group.
                    fighters.AddRange(Group.GetEntitiesInGroup<IFightable>()
                        .Where(member => member.IsAlive && !member.IsFighting));
                    Group.NotifyAllInGroup(GroupNotification.Attacked);
                }
                else
                    fighters.Add(this);

                StopMoving();
                _gameEventBus?.Publish(new BattleInitializedEvent(player, fighters));
                // A fight breaking out is audible: nearby brains investigate (they filter by hearing radius).
                _gameEventBus?.Publish(new WorldStimulusEvent(new Stimulus(StimulusType.Noise, GlobalPosition)));
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
            // Own bus first: per-battle passive/effect state resets before the cleanup below
            CombatEvents.Publish(obj);
            Effects.RemoveAllEffects();
            CanMove = true;
            Position = _lastPosition;

            if (IsAlive)
            {
                _brain?.OnBattleEnded(); // grace period: no instant re-aggression at the arena exit
                _contactGraceSeconds = PostBattleContactGraceSeconds;
            }
            else BecomeBody();

            _battleEventBus = null;
        }

        /// <summary>
        /// The defeat is not the end: the body stays in the world. Non-undead start the rise
        /// timer, undead lie dormant; either can be burned by the player (final death).
        /// NPCs without a data definition have no body rules — free them instead of leaking.
        /// </summary>
        private void BecomeBody()
        {
            if (_lifecycle == null)
            {
                QueueFree();
                return;
            }

            Group?.RemoveFromGroup(this);
            Group = null;
            StopMoving();
            _lifecycle.OnDefeated(Fraction == Fractions.Undead);
            // TODO:
            // новая анимация данного состояния.
            Animations.PlayAnimation("Dead");
        }

        /// <summary>Dies outside a player battle (lost skirmish): the body lifecycle takes over.</summary>
        public void DefeatInWorld()
        {
            // A stale source from an old player battle must not frame the player for a lost skirmish.
            _lastDamageSource = null;
            CurrentHealth = 0;
            IsFighting = false;
            BecomeBody();
        }

        /// <summary>Burns the lying body — final death; the spawn point spawns a replacement.</summary>
        public bool TryBurnBody()
        {
            if (_lifecycle?.TryBurn() != true) return false;
            _gameEventBus?.Publish(new NpcFinalDeathEvent(InstanceId, Id, GlobalPosition));
            QueueFree();
            return true;
        }

        /// <summary>Click on a lying body with the player standing next to it — burn it for good.</summary>
        private void OnInteractionAreaInput(Node viewport, InputEvent @event, long shapeIdx)
        {
            if (_lifecycle is not { CanBeBurned: true }) return;
            if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
            if (!IsPlayerWithin(BurnDistance)) return;

            TryBurnBody();
        }

        private bool IsPlayerWithin(float distance) =>
            _playerAccessor?.Player is Node2D playerNode && GlobalPosition.DistanceTo(playerNode.GlobalPosition) <= distance;

        private void OnResurrectionReady(float parameterBonus)
        {
            var previousFraction = Fraction;
            BecomeRisenUndead(parameterBonus);
            _gameEventBus?.Publish(new NpcFactionChangedEvent(InstanceId, Id, previousFraction, Fractions.Undead, GlobalPosition));
        }

        /// <summary>Save-load path: rebuilds a lying body. Health drops through the normal property —
        /// in Battle the game-bus death event has no subscribers; a future loot orchestrator in Main
        /// must check ILoadScope before reacting to deaths.</summary>
        public void RestoreAsBody(NpcLifeStage stage, float resurrectDelay, float elapsed)
        {
            CurrentHealth = 0;
            StopMoving();
            _lifecycle?.RestoreState(stage, resurrectDelay, elapsed);
            Animations.PlayAnimation("Dead");
        }

        /// <summary>Save-load path: rebuilds a wild risen undead. No NpcFactionChangedEvent —
        /// the original spawn point already replaced this NPC before the save.</summary>
        public void RestoreAsRisen(float parameterBonus) => BecomeRisenUndead(parameterBonus);

        private void BecomeRisenUndead(float parameterBonus)
        {
            Fraction = Fractions.Undead;
            IsRisen = true;
            RisingBonus = parameterBonus;
            // A risen boss skips the weakened opening act: it stands up in its final stage
            // (no immunity, no transition events — this is not a mid-battle transformation).
            if (Stages.Count > 0) ApplyStage(Stages.Count - 1);
            ApplyRisingBonus(parameterBonus);
            CurrentHealth = Parameters.MaxHealth;
            CurrentMana = Parameters.MaxMana;
            CurrentBarrier = Parameters.MaxBarrier; // starts full like the other vitals
            Modulate = new Color(0.65f, 1f, 0.75f); // placeholder undead look until dedicated sprites exist
            Animations.PlayAnimation("Idle_Down");
        }

        /// <summary>The longer the body lay, the stronger the rising — Increase modifiers on the core parameters.</summary>
        private void ApplyRisingBonus(float bonus)
        {
            if (bonus <= 0) return;

            EntityParameter[] boosted = [EntityParameter.Health, EntityParameter.PhysicalDamage, EntityParameter.SpellDamage, EntityParameter.Armor];
            foreach (var parameter in boosted)
                ParameterModifiers.AddModifier(ModifiersCreator.CreateModifierInstance(parameter, ModifierValueType.Increase, bonus, UndeadRisingModifierSource));
        }

        private void OnEffectsChanged()
        {
            _battleEventBus?.Publish<EffectsChangedEvent>(new(this, Effects.GetEffectViews()));
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
            _gameEventBus?.Publish<EntityDiedEvent>(new(this, _lastDamageSource));
            _battleEventBus?.Publish<EntityDiedEvent>(new(this, _lastDamageSource));
            CombatEvents.Publish<EntityDiedEvent>(new(this, _lastDamageSource));
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
                        value = 1000;
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
                        value = 0.25f;
                        break;
                    case EntityParameter.AdditionalHitChance:
                        value = 0.05f; // extra attacks chain — a high base balloons attack series
                        break;
                    case EntityParameter.CriticalDamage:
                        value = 1.5f;
                        break;
                    case EntityParameter.PhysicalDamage:
                    case EntityParameter.SpellDamage:
                    case EntityParameter.Accuracy:
                    case EntityParameter.Evade:
                        value = rnd.RandfRange(50, 1000);
                        break;
                }

                Parameters.SetBaseValueForParameter(entityParameter, value);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;

            Battle.Source.ExhaustionGrant.Detach(this);
            _npcRegistry?.Unregister(this);
            _recovery?.UnregisterParticipant(this);
            _smartPoints?.Release(InstanceId);
            _gameEventBus?.Unsubscribe<WorldStimulusEvent>(OnWorldStimulus);
            _battleEventBus = null;
            _gameEventBus = null;
            _brain = null;
        }
    }
}
