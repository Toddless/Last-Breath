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
    using Core.Enums;
    using Core.Events;
    using Core.Localization;
    using Godot;
    using Targeting;

    public abstract class Ability(AbilityBaseData data) : IAbility
    {
        protected IFightable? Owner;

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

        protected float this[string parameter] => Params[parameter];

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
        public Dictionary<int, List<IAbilityUpgrade>> Upgrades { get; private set; } = [];
        public IReadOnlyDictionary<int, IAbilityUpgrade> CurrentUpgrades => _currentUpgrades;
        private readonly Dictionary<int, IAbilityUpgrade> _currentUpgrades = [];
        public ITargetingStrategy Targeting { get; set; } = TargetingStrategyFactory.From(data);
        public int CostValue => (int)this[AbilityParameter.CostValue];
        public int MasteryLevel { get; set; } = data.MasteryLevel;
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

        public void SetAbilityUpgrades(Dictionary<int, List<IAbilityUpgrade>> upgrades) => Upgrades = upgrades;

        /// <summary>One chosen upgrade per tier: selecting a new one removes the previous choice first.
        /// Matches by stable data Id (save/load) or InstanceId (UI selection).</summary>
        public void SelectUpgrade(int tier, string upgradeId)
        {
            if (!Upgrades.TryGetValue(tier, out var tierUpgrades)) return;
            var upgrade = tierUpgrades.FirstOrDefault(u => u.IsSame(upgradeId) || u.Id == upgradeId);
            if (upgrade == null) return;
            if (_currentUpgrades.TryGetValue(tier, out var current))
            {
                if (current.IsSame(upgrade.InstanceId)) return;
                current.Remove(this);
            }

            upgrade.Apply(this);
            _currentUpgrades[tier] = upgrade;
        }

        public void ClearUpgrade(int tier)
        {
            if (!_currentUpgrades.TryGetValue(tier, out var current)) return;
            current.Remove(this);
            _currentUpgrades.Remove(tier);
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

            var context = new AbilityActivationContext
            {
                Ability = this,
                Caster = Owner,
                Field = field,
                Targets = targets,
                Cost = CostValue,
                CostType = CostType,
                Cooldown = Cooldown
            };
            // Cast mutators run before anything is paid: ability-scoped first, then entity-scoped (items/effects)
            ActivationEffect.Values.ToList().ForEach(mod => mod.Apply(context));
            Owner.ModifierHandler.Apply(context);

            StartCooldown(context.Cooldown);
            ConsumeResource(context);

            Owner.CombatEvents.Publish<AbilityActivatedEvent>(new(this, Owner, VitalsSnapshot.From(Owner), CastId));
            await ExecuteInternal(targets, Owner, field);
            foreach (var rider in ActivationRiders.Values.ToList())
                await rider.Apply(context);
            Owner.CombatEvents.Publish<AbilityExecutedEvent>(new(this, Owner, CastId));
        }

        /// <summary>Delivery implementations (internal loops and execution strategies) call this on every
        /// impact so per-impact riders fire for each touched target.</summary>
        public async Task ApplyImpactRiders(AbilityImpact impact)
        {
            foreach (var rider in ImpactRiders.Values.ToList())
                await rider.Apply(impact);
        }

        public void AddParameterDecorator(AbilityParameterDecorator decorator) => Params.AddDecorator(decorator);

        public void RemoveParameterDecorator(string decoratorId, string parameter) => Params.RemoveDecorator(decoratorId, parameter);

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
            return CostType switch
            {
                Costs.Mana => Owner.CurrentMana >= CostValue,
                Costs.Health => Owner.CurrentHealth >= CostValue,
                Costs.Barrier => Owner.CurrentBarrier >= CostValue,
                _ => false
            };
        }

        public virtual bool CanActivate() => IsEnoughResource() && CooldownLeft == 0 && !IsOwnerParalyzed;

        public bool IsSame(string otherId) => InstanceId.Equals(otherId);

        public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);

        public abstract IAbility Copy();

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

        /// <summary>Shared tail of every Copy(): fresh instance from the same data + the upgrade catalog.</summary>
        protected IAbility CopyUpgradesTo(Ability copy)
        {
            copy.SetAbilityUpgrades(Upgrades.ToDictionary());
            return copy;
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
