namespace Battle.Source.Abilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Battle;
    using Core.Battle.Abilities;
    using Core.Components;
    using Core.Components.Decorator;
    using Core.Components.Module;
    using Core.Data;
    using Core.Entity;
    using Core.Enums;
    using Core.Events.GameEvents;
    using Core.Localization;
    using Godot;
    using Targeting;

    public abstract class Ability(
        string id,
        string[] tags,
        int cooldown,
        int costValue,
        Costs costType = Costs.Mana) : IAbility
    {
        protected IFightable? Owner;

        protected IModuleManager<AbilityParameter, IParameterModule<AbilityParameter>, AbilityParameterDecorator<AbilityParameter>> ModuleManager
        {
            get
            {
                if (field != null) return field;
                field = new ModuleManager<AbilityParameter, IParameterModule<AbilityParameter>, AbilityParameterDecorator<AbilityParameter>>(CreateBaseModules());
                field.ModuleChanges += OnModuleChanges;
                return field;
            }
        }

        protected float this[AbilityParameter parameter] => ModuleManager.GetModule(parameter).GetValue();

        /// <summary>
        /// Presentation grouping key of the CURRENT activation, regenerated per <see cref="Execute"/>.
        /// Damage-dealing descendants stamp it onto their DamageContexts so the BattleDirector
        /// can play the whole cast as one chord.
        /// </summary>
        protected string CastId { get; private set; } = string.Empty;

        /// <summary>
        /// Named values for the description template: placeholder = parameter name ({Cooldown},
        /// {Damage}, {StunDuration}...). Values go through decorators, so upgrades change the text
        /// automatically. Descendants with their own parameter enum add it via
        /// <see cref="AddModuleValues{TKey}"/>; percent-fractions are rescaled in place (×100).
        /// </summary>
        protected virtual Dictionary<string, object?> DescriptionValues
        {
            get
            {
                var values = new Dictionary<string, object?>();
                AddModuleValues(values, ModuleManager);
                values.Remove(nameof(AbilityParameter.CostType)); // enum stored as float — meaningless as a number
                return values;
            }
        }

        public Costs CostType => (Costs)this[AbilityParameter.CostType];
        public Stance Stance { get; set; }
        public ITargetingStrategy Targeting { get; set; } = new SingleTargetTargeting(TargetRelation.Enemies);
        public int CostValue => (int)this[AbilityParameter.CostValue];
        public int MasteryLevel { get; set; }
        public string Id { get; } = id;
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public string[] Tags { get; } = tags;

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

        public Dictionary<string, IAbilityActivationModifier> ActivationEffect { get; } = [];
        public Dictionary<string, IActivationRider> ActivationRiders { get; } = [];
        public Dictionary<string, IImpactRider> ImpactRiders { get; } = [];
        public Dictionary<int, List<IAbilityUpgrade>> Upgrades { get; private set; } = [];
        public IReadOnlyDictionary<int, IAbilityUpgrade> CurrentUpgrades => _currentUpgrades;
        private readonly Dictionary<int, IAbilityUpgrade> _currentUpgrades = [];
        public float Cooldown => this[AbilityParameter.Cooldown];
        public string Description => FormatDescription();
        public string DisplayName => Localization.Localize(Id);

        public Texture2D? Icon
        {
            get
            {
                if (field != null) return field;
                // Change path to the actual assets
                field = ResourceLoader.Load<Texture2D>($"res://Internal/_Placeholders/Icons/{Id}.png");
                return field;
            }
        }


        public event Action<Enum>? OnParameterChanged;
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

        public virtual void AddParameterDecorator<T>(IModuleDecorator<T, IParameterModule<T>> decorator)
            where T : struct, Enum
        {
            if (decorator is not AbilityParameterDecorator<AbilityParameter> moduleDecorator) return;
            ModuleManager.AddDecorator(moduleDecorator);
        }

        public virtual void RemoveParameterDecorator<T>(string id, T key)
            where T : struct, Enum
        {
            if (key is not AbilityParameter abilityParameter) return;
            ModuleManager.RemoveDecorator(id, abilityParameter);
        }

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

        protected void OnModuleChanges<TKey>(TKey key) where TKey : struct, Enum => OnParameterChanged?.Invoke(key);

        protected string FormatDescription() => Localization.RenderDescription(Id, DescriptionValues, TextFormat.Rich);

        /// <summary>Adds every parameter of a module manager under its enum name; decorated values, not base ones.</summary>
        protected static void AddModuleValues<TKey>(
            Dictionary<string, object?> values,
            IModuleManager<TKey, IParameterModule<TKey>, AbilityParameterDecorator<TKey>> manager)
            where TKey : struct, Enum
        {
            foreach (TKey key in manager.Keys)
                values[key.ToString()] = manager.GetModule(key).GetValue();
        }

        protected void OnTurnEnd(TurnEndEvent obj)
        {
            if (CooldownLeft == 0) return;
            CooldownLeft--;
        }

        protected virtual Dictionary<AbilityParameter, IParameterModule<AbilityParameter>> CreateBaseModules() => new()
        {
            [AbilityParameter.Cooldown] = new Module<AbilityParameter>(() => cooldown, AbilityParameter.Cooldown),
            [AbilityParameter.CostValue] = new Module<AbilityParameter>(() => costValue, AbilityParameter.CostValue),
            [AbilityParameter.CostType] = new Module<AbilityParameter>(() => (float)costType, AbilityParameter.CostType),
        };

        private bool IsOwnerParalyzed => Owner != null && (Owner.StatusEffects & StatusEffects.Paralysis) != 0;

        private void OnResourceChanges(float obj) => NotifyAvailabilityChanged();

        private void OnOwnerStatusApplied(StatusEffectAppliedEvent evt) => NotifyAvailabilityChanged();

        private void OnOwnerStatusRemoved(StatusEffectRemovedEvent evt) => NotifyAvailabilityChanged();

        private void NotifyAvailabilityChanged() => AbilityResourceChanges?.Invoke(this, CanActivate());
    }
}
