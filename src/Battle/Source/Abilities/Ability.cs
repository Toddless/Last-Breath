namespace Battle.Source.Abilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Enums;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Components;
    using Core.Interfaces.Components.Decorator;
    using Core.Interfaces.Components.Module;
    using Core.Interfaces.Entity;
    using Core.Interfaces.Events.GameEvents;
    using Godot;
    using Module;
    using Source.Decorators;
    using Utilities;

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
        public Costs CostType => (Costs)this[AbilityParameter.CostType];
        public int CostValue => (int)this[AbilityParameter.CostValue];
        public string Id { get; } = id;
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public string[] Tags { get; } = tags;
        public int CooldownLeft { get; set; }
        public Dictionary<string, IAbilityActivationModifier> ActivationEffect { get; } = [];
        public Dictionary<string, IAbilityPostActivationModifier> PostActivationEffect { get; } = [];
        public Dictionary<int, List<IAbilityUpgrade>> Upgrades { get; private set; } = [];
        public Dictionary<int, IAbilityUpgrade> CurrentUpgrades { get; set; } = [];
        public float Cooldown => this[AbilityParameter.Cooldown];
        public string Description => FormatDescription();
        public string DisplayName => Localization.Localize(Id);

        public Texture2D? Icon
        {
            get;
            // {
            //     if (field != null) return field;
            //     field = ResourceLoader.Load<Texture2D>($"res://Source/Abilities/{Id}.png");
            //     return field;
            // }
        }

        public event Action<Enum>? OnParameterChanged;
        public event Action<IAbility, int>? CooldownLeftChanges;
        public event Action<IAbility, bool>? AbilityResourceChanges;

        public void SetAbilityUpgrades(Dictionary<int, List<IAbilityUpgrade>> upgrades) => Upgrades = upgrades;

        public virtual async Task Execute(List<IFightable> targets, IBattleField field)
        {
            if (Owner == null) return;
            var context = new AbilityActivationContext { Caster = Owner, Field = field, Targets = targets };
            ActivationEffect.Values.ToList().ForEach(mod => mod.Apply(context));
            StartCooldown();
            ConsumeResource();
            Owner.CombatEvents.Publish<AbilityActivatedEvent>(new(this));
            await Owner.Animations.PlayAnimationAsync(Id);
            await ExecuteInternal(targets, Owner, field);
            PostActivationEffect.Values.ToList().ForEach(mod => mod.Apply(context));
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
        }

        public void RemoveOwner()
        {
            if (Owner == null) return;
            Owner.CurrentManaChanged -= OnResourceChanges;
            Owner.CurrentHealthChanged -= OnResourceChanges;
            Owner.CurrentBarrierChanged -= OnResourceChanges;
            Owner.CombatEvents.Unsubscribe<TurnEndEvent>(OnTurnEnd);
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

        public bool IsSame(string otherId) => InstanceId.Equals(otherId);

        public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);
        public abstract IAbility Copy();

        protected void ConsumeResource() => Owner?.ConsumeResource(CostType, CostValue);

        protected abstract Task ExecuteInternal(List<IFightable> targets, IFightable owner, IBattleField field);

        protected void StartCooldown()
        {
            CooldownLeft = (int)Cooldown;
            CooldownLeftChanges?.Invoke(this, CooldownLeft);
        }

        protected void OnModuleChanges<TKey>(TKey key) where TKey : struct, Enum => OnParameterChanged?.Invoke(key);

        protected virtual string FormatDescription() => Localization.LocalizeDescriptionFormated(Id);

        protected void OnTurnEnd(TurnEndEvent obj)
        {
            if (CooldownLeft == 0) return;
            CooldownLeft--;
            CooldownLeftChanges?.Invoke(this, CooldownLeft);
        }

        protected virtual Dictionary<AbilityParameter, IParameterModule<AbilityParameter>> CreateBaseModules() => new()
        {
            [AbilityParameter.Cooldown] = new Module<AbilityParameter>(() => cooldown, AbilityParameter.Cooldown),
            [AbilityParameter.CostValue] = new Module<AbilityParameter>(() => costValue, AbilityParameter.CostValue),
            [AbilityParameter.CostType] = new Module<AbilityParameter>(() => (float)costType, AbilityParameter.CostType),
        };

        private void OnResourceChanges(float obj) => AbilityResourceChanges?.Invoke(this, IsEnoughResource());
    }
}
