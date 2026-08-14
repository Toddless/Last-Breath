namespace Battle.Source.Abilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Data;
    using Core.Data.AbilityData;
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Events;
    using Core.Localization;
    using Godot;
    using Targeting;

    public abstract class Ability(AbilityBaseData data) : IAbility
    {
        // Rolls for cast mutators that fire by chance (item lines like "X% chance the cast is free").
        // One shared, time-seeded generator instead of a fresh one per activation, built lazily on the
        // first real cast — a preview never materializes it.
        private static IRandomNumberGenerator? s_castRnd;

        protected IFightable? Owner;

        /// <summary>
        /// Builds the generator every cast rolls on. The seat starts on <see cref="DefaultCastRandom"/>,
        /// which touches nothing native: the engine generator is a native object, and constructing one
        /// where Godot is not running kills the whole process (0xC0000005) past the reach of any catch.
        /// Which implementation the running game rolls on is a composition decision — the bootstrap puts
        /// <see cref="EngineCastRandom"/> here, so a cast rolls on the engine RNG like the rest of combat.
        /// A new source also drops the stream currently in use, so the swap holds however much has
        /// already been cast.
        /// </summary>
        public static Func<IRandomNumberGenerator> CastRandomSource
        {
            get;
            set
            {
                field = value;
                s_castRnd = null;
            }
        } = DefaultCastRandom;

        /// <summary>The data record the ability was built from — the single source of base values;
        /// <see cref="Copy"/> rebuilds fresh instances from it.</summary>
        protected AbilityBaseData Data { get; } = data;

        /// <summary>The single parameter store: base values registered by the ability, decorated by upgrades.</summary>
        protected AbilityParameterSet Params
        {
            get
            {
                if (field != null) return field;
                field = new AbilityParameterSet();
                RegisterBaseParameters(field);
                field.ParameterChanged += OnParameterChangedInternal;
                return field;
            }
        }

        public float this[string parameter] => Params[parameter];

        public float Effectiveness => Params.ValueOr(AbilityParameter.Effectiveness, 1f);

        public float ValueOr(string parameter, float fallback) => Params.ValueOr(parameter, fallback);

        public bool Declares(string parameter) => Params.Declares(parameter);

        /// <summary>
        /// Presentation grouping key of the CURRENT activation, regenerated per <see cref="Execute"/>.
        /// Damage-dealing descendants stamp it onto their DamageContexts so the BattleDirector
        /// can play the whole cast as one chord.
        /// </summary>
        protected string CastId { get; private set; } = string.Empty;

        /// <summary>
        /// Named values for the description template: placeholder = parameter key ({Cooldown},
        /// {Damage}, {StunDuration}...). Every registered parameter is exposed, decorated values —
        /// upgrades change the text automatically; percent-fractions are rescaled by the template.
        /// </summary>
        protected virtual Dictionary<string, object?> DescriptionValues
        {
            get
            {
                var values = new Dictionary<string, object?>();
                foreach (string key in Params.Keys)
                    values[key] = Params[key];
                values.Remove(AbilityParameter.CostType); // enum stored as float — meaningless as a number
                return values;
            }
        }

        public Costs CostType => (Costs)this[AbilityParameter.CostType];
        public Stance Stance { get; set; } = data.Stance;
        public Dictionary<string, IAbilityActivationModifier> ActivationEffect { get; } = [];
        public Dictionary<string, IActivationRider> ActivationRiders { get; } = [];
        public Dictionary<string, IImpactRider> ImpactRiders { get; } = [];
        public IReadOnlyDictionary<string, IAbilityAugment> InstalledUpgrades => _installedUpgrades;
        private readonly Dictionary<string, IAbilityAugment> _installedUpgrades = new(StringComparer.Ordinal);
        public ITargetingStrategy Targeting { get; set; } = TargetingStrategyFactory.From(data);
        public int CostValue => (int)this[AbilityParameter.CostValue];
        public string Id { get; } = data.Id;
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public string[] Tags { get; } = data.Tags;
        public float Cooldown => this[AbilityParameter.Cooldown];
        public string Description => FormatDescription();
        public string DisplayName => Localization.Localize(Id);
        public int CooldownLeft
        {
            get;
            set
            {
                if (field == value) return;
                field = value;
                CooldownLeftChanges?.Invoke(this, CooldownLeft);
            }
        }


        public Texture2D? Icon
        {
            get
            {
                if (field != null) return field;
                // Change path to the actual assets
                field = ResourceLoader.Load<Texture2D>($"res://Data/Shared/Assets/Icons/{Id}.png");
                return field;
            }
        }


        public event Action<string>? OnParameterChanged;
        public event Action<IAbility, int>? CooldownLeftChanges;
        public event Action<IAbility, bool>? AbilityResourceChanges;

        /// <summary>
        /// The arrangement is taken down and put up whole rather than compared entry by entry: the
        /// values an upgrade lays on are decorators, so removing and re-applying the same set leaves the
        /// same numbers standing, and the alternative would be telling one seated copy from another by
        /// its id — which is precisely what two copies of one record have in common.
        /// Cheap enough for that: nothing in a fight rebinds, the passes come from a taken node, a
        /// seated augment, a load or a new playthrough.
        /// </summary>
        public void InstallUpgrades(IReadOnlyDictionary<string, IAbilityAugment> bySocket)
        {
            foreach (IAbilityAugment worn in _installedUpgrades.Values) worn.Remove(this);
            _installedUpgrades.Clear();

            foreach ((string socketId, IAbilityAugment upgrade) in bySocket)
            {
                upgrade.Apply(this);
                _installedUpgrades[socketId] = upgrade;
            }
        }

        public virtual async Task Execute(List<IFightable> targets, IBattleField field)
        {
            if (Owner == null) return;
            CastId = Guid.NewGuid().ToString();
            // Single gate for every activation path (UI, hotkey, future AI). Events cannot veto, so the check lives here.
            if (IsOwnerParalyzed)
            {
                Owner.CombatEvents.Publish<AbilityActivationRejectedEvent>(new(this));
                return;
            }

            var context = BuildActivationContext(targets, field);
            ApplyActivationMutators(context);

            StartCooldown(context.Cooldown);
            ConsumeResource(context);

            try
            {
                // The announcement that OPENS the cast window is guarded too: it is delivered to one
                // subscriber after another, and a charge that armed itself on it has already attached a
                // modifier to the caster by the time a later subscriber throws.
                Owner.CombatEvents.Publish<AbilityActivatedEvent>(new(this, Owner, VitalsSnapshot.From(Owner), CastId));
                await ExecuteInternal(targets, Owner, field);
                foreach (var rider in ActivationRiders.Values.ToList())
                    await rider.Apply(context);
            }
            finally
            {
                // Single end-of-cast channel: charges, chord playback and the tally of actions the owner
                // spent this turn all close on this event, so the cast is announced finished however it
                // ends — including a throw inside the opening announcement itself. A cast that ends
                // without it leaves the charge attached to the caster for good, the turn short of the
                // action it spent, and every line written for the first action of a turn on for the rest.
                Owner.CombatEvents.Publish<AbilityExecutedEvent>(new(this, Owner, CastId));
            }
        }

        /// <summary>The generator the running game rolls its casts on: the engine RNG, time-seeded.
        /// The return type names the implementation on purpose — the production choice stays readable
        /// (and assertable) without constructing the native object.</summary>
        public static GodotRandomNumberGenerator EngineCastRandom()
        {
            var rnd = new RandomNumberGenerator();
            rnd.Randomize();
            return new GodotRandomNumberGenerator(rnd);
        }

        /// <summary>The generator a cast rolls on until a composition says otherwise: pure C#, time-seeded
        /// and free of the engine, so a host that never boots Godot survives a cast instead of dying on
        /// the first one.</summary>
        public static DefaultRandomNumberGenerator DefaultCastRandom() => new();

        private static IRandomNumberGenerator CastRnd => s_castRnd ??= CastRandomSource();

        /// <summary>Delivery implementations (internal loops and execution strategies) call this on every
        /// impact so per-impact riders fire for each touched target.</summary>
        public async Task ApplyImpactRiders(AbilityImpact impact)
        {
            foreach (var rider in ImpactRiders.Values.ToList())
                await rider.Apply(impact);
        }

        public void AddParameterDecorator(AbilityParameterDecorator decorator) => Params.AddDecorator(decorator);

        public void RemoveParameterDecorator(string decoratorId, string parameter) => Params.RemoveDecorator(decoratorId, parameter);

        public bool TryRegisterParameter(string parameter, float value) => Params.TryRegister(parameter, value);

        public void UnregisterParameter(string parameter) => Params.Unregister(parameter);

        public virtual void SetOwner(IFightable owner)
        {
            Owner = owner;
            Owner.CurrentHealthChanged += OnResourceChanges;
            Owner.CurrentBarrierChanged += OnResourceChanges;
            Owner.CurrentManaChanged += OnResourceChanges;
            Owner.CombatEvents.Subscribe<TurnEndEvent>(OnTurnEnd);
            Owner.CombatEvents.Subscribe<StatusEffectAppliedEvent>(OnOwnerStatusApplied);
            Owner.CombatEvents.Subscribe<StatusEffectRemovedEvent>(OnOwnerStatusRemoved);
        }

        public void RemoveOwner()
        {
            if (Owner == null) return;
            Owner.CurrentManaChanged -= OnResourceChanges;
            Owner.CurrentHealthChanged -= OnResourceChanges;
            Owner.CurrentBarrierChanged -= OnResourceChanges;
            Owner.CombatEvents.Unsubscribe<TurnEndEvent>(OnTurnEnd);
            Owner.CombatEvents.Unsubscribe<StatusEffectAppliedEvent>(OnOwnerStatusApplied);
            Owner.CombatEvents.Unsubscribe<StatusEffectRemovedEvent>(OnOwnerStatusRemoved);
            Owner = null;
        }

        public virtual bool IsEnoughResource()
        {
            if (Owner == null) return false;
            var context = PreviewActivation();
            return context.CostType switch
            {
                Costs.Mana => Owner.CurrentMana >= context.Cost,
                Costs.Health => Owner.CurrentHealth >= context.Cost,
                Costs.Barrier => Owner.CurrentBarrier >= context.Cost,
                _ => false
            };
        }

        public virtual bool CanActivate() => IsEnoughResource() && CooldownLeft == 0 && !IsOwnerParalyzed;

        public bool IsSame(string otherId) => InstanceId.Equals(otherId);

        public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);

        public abstract IAbility Copy();

        /// <summary>The context every effect this cast lays is applied with — one factory so no delivery
        /// has to know about <see cref="EffectApplyingContext.Effectiveness"/>. Sites carrying more write
        /// <c>Laying(target) with { Damage = … }</c>.</summary>
        protected EffectApplyingContext Laying(IFightable target) =>
            new() { Caster = Owner!, Target = target, Source = InstanceId, Effectiveness = Effectiveness };

        protected void ConsumeResource(IAbilityActivationContext context) => Owner?.ConsumeResource(context.CostType, context.Cost);

        protected abstract Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field);

        protected void StartCooldown(float cd) => CooldownLeft = (int)cd;

        protected string FormatDescription() => Localization.RenderDescription(Id, DescriptionValues, TextFormat.Rich);

        protected void OnTurnEnd(TurnEndEvent obj)
        {
            if (CooldownLeft == 0) return;
            CooldownLeft--;
        }

        /// <summary>
        /// Registers the ability's base parameter values: the common keys plus EVERYTHING from the
        /// data's abilityProperties (json camelCase → PascalCase key). Descendants only add
        /// <see cref="AbilityParameterSet.RegisterDefault"/> fallbacks for keys the data may omit.
        /// </summary>
        protected virtual void RegisterBaseParameters(AbilityParameterSet parameters)
        {
            parameters.Register(AbilityParameter.Cooldown, Data.Cooldown);
            parameters.Register(AbilityParameter.CostValue, Data.CostValue);
            parameters.Register(AbilityParameter.CostType, (float)Data.CostsType);
            foreach (var (key, value) in Data.AbilityProperties)
                parameters.Register(ToParameterKey(key), value);
        }

        /// <summary>Damage keys from the data fields — for every damage-dealing ability regardless of family.</summary>
        protected void RegisterDamageParameters(AbilityParameterSet parameters)
        {
            parameters.Register(AbilityParameter.Damage, Data.Damage);
            parameters.Register(AbilityParameter.WeaponDamageScale, Data.WeaponDamageScale);
            parameters.Register(AbilityParameter.SpellDamageScale, Data.SpellDamageScale);
        }

        /// <summary>Shared tail of every Copy(): a fresh instance from the same data, wearing the same
        /// augments in the same slots. The upgrades are copied one by one and applied to the copy — a
        /// shared instance would leak applied state (Learned, decorators) between the two, and the
        /// original's decorators would come off whenever the copy's arrangement was rebuilt.</summary>
        protected IAbility CopyUpgradesTo(Ability copy)
        {
            copy.InstallUpgrades(_installedUpgrades.ToDictionary(
                worn => worn.Key,
                worn => worn.Value.Copy(),
                StringComparer.Ordinal));
            return copy;
        }

        /// <summary>
        /// Effective activation numbers for availability checks and UI: the real cast mutator
        /// pipeline over a preview context (see <see cref="IAbilityActivationContext.IsPreview"/> —
        /// chance-based and self-consuming mutators stay inert, nothing is paid).
        /// </summary>
        protected IAbilityActivationContext PreviewActivation()
        {
            // No battle field exists outside a cast; the IsPreview contract keeps mutators off it.
            var context = BuildActivationContext([], field: null!, isPreview: true);
            ApplyActivationMutators(context);
            return context;
        }

        private AbilityActivationContext BuildActivationContext(List<IFightable> targets, IBattleField field, bool isPreview = false) => new()
        {
            Ability = this,
            Caster = Owner!,
            Field = field,
            // Preview never rolls (IsPreview contract), so the generator is never even built for it.
            Rnd = isPreview ? null! : CastRnd,
            Targets = targets,
            IsPreview = isPreview,
            Cost = CostValue,
            CostType = CostType,
            Cooldown = Cooldown
        };

        // Cast mutators run before anything is paid: ability-scoped first, then entity-scoped (items/effects)
        private void ApplyActivationMutators(AbilityActivationContext context)
        {
            ActivationEffect.Values.ToList().ForEach(mod => mod.Apply(context));
            Owner?.ModifierHandler.Apply(context);
        }

        private static string ToParameterKey(string jsonKey) => char.ToUpperInvariant(jsonKey[0]) + jsonKey[1..];

        private bool IsOwnerParalyzed => Owner != null && (Owner.StatusEffects & StatusEffects.Paralysis) != 0;

        private void OnParameterChangedInternal(string parameter) => OnParameterChanged?.Invoke(parameter);

        private void OnResourceChanges(float obj) => NotifyAvailabilityChanged();

        private void OnOwnerStatusApplied(StatusEffectAppliedEvent evt) => NotifyAvailabilityChanged();

        private void OnOwnerStatusRemoved(StatusEffectRemovedEvent evt) => NotifyAvailabilityChanged();

        private void NotifyAvailabilityChanged() => AbilityResourceChanges?.Invoke(this, CanActivate());
    }
}
