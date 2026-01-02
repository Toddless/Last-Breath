namespace Battle.Source.Abilities
{
    using Godot;
    using System;
    using Module;
    using Utilities;
    using Decorators;
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;
    using System.Threading.Tasks;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Components;
    using System.Collections.Generic;
    using Core.Interfaces.Events.GameEvents;
    using Core.Interfaces.Components.Module;
    using Core.Interfaces.Components.Decorator;

    public abstract class Ability(
        string id,
        string[] tags,
        int cooldown,
        int costValue,
        float maxTargets,
        List<IEffect> effects,
        List<IEffect> casterEffects,
        Dictionary<int, List<IAbilityUpgrade>> upgrades,
        IStanceMastery? mastery = null,
        Costs costType = Costs.Mana,
        AbilityType abilityType = AbilityType.Target) : IAbility
    {
        protected IEntity? Owner;
        protected readonly IStanceMastery? Mastery = mastery;

        protected IModuleManager<AbilityParameter, IParameterModule<AbilityParameter>, AbilityParameterDecorator> ModuleManager
        {
            get
            {
                if (field != null) return field;

                field = CreateModuleManager();
                field.ModuleChanges += OnModuleChanges;
                return field;
            }
        }

        protected float this[AbilityParameter parameter] => ModuleManager.GetModule(parameter).GetValue();

        public string Id { get; } = id;
        public string InstanceId { get; } = Guid.NewGuid().ToString();

        public string[] Tags { get; } = tags;
        public float SpendAbilityPoints { get; set; }

        public Texture2D? Icon
        {
            get
            {
                if (field != null) return field;
                field = ResourceLoader.Load<Texture2D>($"res://Source/Abilities/{Id}.png");
                return field;
            }
        }

        public int CooldownLeft { get; private set; }

        public int CostValue => (int)this[AbilityParameter.CostValue];

        public Costs CostType => (Costs)this[AbilityParameter.CostType];
        public AbilityType AbilityType { get; } = abilityType;

        public List<IConditionalModifier> ConditionalModifiers { get; } = [];
        public List<IEffect> Effects { get; set; } = effects;
        public List<IEffect> CasterEffects { get; } = casterEffects;
        public Dictionary<int, List<IAbilityUpgrade>> Upgrades { get; set; } = upgrades;

        public float MaxTargets => this[AbilityParameter.Target];
        public float Cooldown => this[AbilityParameter.Cooldown];
        public string Description => FormatDescription();
        public string DisplayName => Localization.Localize(Id);

        public event Action<AbilityParameter>? OnParameterChanged;
        public event Action<IAbility, int>? CooldownLeftChanges;
        public event Action<IAbility, bool>? AbilityResourceChanges;

        public virtual async Task Activate(List<IEntity> targets)
        {
            if (Owner == null) return;

            var context = new EffectApplyingContext { Caster = Owner, Source = Id };
            foreach (IEntity target in targets)
            {
                context.Target = target;
                ApplyTargetEffects(context);
            }

            ApplyCasterEffects(context);
            StartCooldown();
            ConsumeResource();
            await Owner.Animations.PlayAnimationAsync(Id);
        }

        public void AddParameterUpgrade<T>(IModuleDecorator<T, IParameterModule<T>> decorator)
            where T : struct, Enum
        {
            if (decorator is not AbilityParameterDecorator moduleDecorator) return;
            ModuleManager.AddDecorator(moduleDecorator);
        }

        public void RemoveParameterUpgrade<T>(string id, T key)
            where T : struct, Enum
        {
            if (key is not AbilityParameter abilityParameter) return;
            ModuleManager.RemoveDecorator(id, abilityParameter);
        }

        public void AddCondition(IConditionalModifier modifier) => ConditionalModifiers.Add(modifier);
        public void RemoveCondition(string id) => ConditionalModifiers.RemoveAll(c => c.Id == id);
        public void ClearConditions() => ConditionalModifiers.Clear();

        public void AddEffect(IEffect effect, bool targetEffect = true)
        {
            if (targetEffect) Effects.Add(effect);
            else CasterEffects.Add(effect);
        }

        public void RemoveEffect(string id, bool targetEffect = true)
        {
            if (targetEffect)
            {
                var exist = Effects.FirstOrDefault(c => c.Id == id);
                if (exist != null) RemoveFromList(Effects, exist);
            }
            else
            {
                var exist = CasterEffects.FirstOrDefault(c => c.Id == id);
                if (exist != null) RemoveFromList(CasterEffects, exist);
            }
        }

        public virtual void SetOwner(IEntity owner)
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

        protected void ConsumeResource() => Owner?.ConsumeResource(CostType, CostValue);

        protected void StartCooldown()
        {
            CooldownLeft = (int)Cooldown;
            CooldownLeftChanges?.Invoke(this, CooldownLeft);
        }


        protected void ApplyTargetEffects(EffectApplyingContext context)
        {
            foreach (var clone in Effects.Select(effect => effect.Clone()))
                clone.Apply(context);
        }

        protected void ApplyCasterEffects(EffectApplyingContext context)
        {
            if (Owner == null) return;
            context.Target = Owner;
            foreach (var clone in CasterEffects.Select(effect => effect.Clone()))
                clone.Apply(context);
        }

        protected float ApplyConditionalModifiers(EffectApplyingContext context, AbilityParameter parameter, float baseValue)
        {
            float additiveBonus = 0f;
            float increasedBonus = 1f;
            float multiplyBonus = 1f;

            foreach (IConditionalModifier conditionalModifier in ConditionalModifiers)
            {
                if (conditionalModifier.Parameter != parameter) continue;
                (float Value, ModifierType Type)? result = conditionalModifier.GetValue(context);
                if (result == null) continue;

                switch (result.Value.Type)
                {
                    case ModifierType.Flat:
                        additiveBonus += result.Value.Value;
                        break;
                    case ModifierType.Increase:
                        increasedBonus += result.Value.Value;
                        break;
                    case ModifierType.Multiplicative:
                        multiplyBonus += result.Value.Value;
                        break;
                }
            }

            return ((baseValue + additiveBonus) * increasedBonus) * multiplyBonus;
        }

        protected virtual string FormatDescription() => Localization.LocalizeDescriptionFormated(Id);

        protected virtual void OnTurnEnd(TurnEndEvent obj)
        {
            if (CooldownLeft == 0) return;
            CooldownLeft--;
            CooldownLeftChanges?.Invoke(this, CooldownLeft);
        }

        private IModuleManager<AbilityParameter, IParameterModule<AbilityParameter>, AbilityParameterDecorator> CreateModuleManager() =>
            new ModuleManager<AbilityParameter, IParameterModule<AbilityParameter>, AbilityParameterDecorator>(new Dictionary<AbilityParameter, IParameterModule<AbilityParameter>>
            {
                [AbilityParameter.Cooldown] = new Module<AbilityParameter>(() => cooldown, AbilityParameter.Cooldown),
                [AbilityParameter.CostValue] = new Module<AbilityParameter>(() => costValue, AbilityParameter.CostValue),
                [AbilityParameter.CostType] = new Module<AbilityParameter>(() => (float)costType, AbilityParameter.CostType),
                [AbilityParameter.Target] = new Module<AbilityParameter>(() => maxTargets, AbilityParameter.Target)
            });

        private void RemoveFromList(List<IEffect> listEffects, IEffect effect) => listEffects.Remove(effect);
        private void OnModuleChanges(AbilityParameter key) => OnParameterChanged?.Invoke(key);
        private void OnResourceChanges(float obj) => AbilityResourceChanges?.Invoke(this, IsEnoughResource());
    }
}
