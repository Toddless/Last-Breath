namespace Battle.Source.Abilities
{
    using Godot;
    using System;
    using Module;
    using Utilities;
    using Decorators;
    using Core.Enums;
    using System.Linq;
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
        float damage,
        float weaponDamageScale,
        float spellDamageScale,
        List<IEffect> effects,
        Dictionary<int, List<IAbilityUpgrade>> upgrades,
        Costs costType = Costs.Mana,
        AbilityType abilityType = AbilityType.Target) : IAbility
    {
        protected IEntity? Owner;

        protected IModuleManager<AbilityParameter, IParameterModule<AbilityParameter>, AbilityParameterDecorator<AbilityParameter>> ModuleManager
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
        public float Damage => this[AbilityParameter.Damage];
        public float WeaponDamageScale => this[AbilityParameter.WeaponDamageScale];
        public float SpellDamageScale => this[AbilityParameter.SpellDamageScale];
        public Costs CostType => (Costs)this[AbilityParameter.CostType];
        public int CostValue => (int)this[AbilityParameter.CostValue];
        public string Id { get; } = id;
        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public string[] Tags { get; } = tags;
        public int CooldownLeft { get; set; }
        public bool IsEvadable { get; set; }
        public AbilityType AbilityType { get; } = abilityType;
        public List<IEffect> Effects { get; set; } = effects;
        public Dictionary<int, List<IAbilityUpgrade>> Upgrades { get; set; } = upgrades;
        public Dictionary<int, IAbilityUpgradeWrap<IAbility>> CurrentUpgrades { get; set; } = [];
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

        public virtual async Task Execute(List<IEntity> targets)
        {
            if (Owner == null) return;
            StartCooldown();
            ConsumeResource();
            await Owner.Animations.PlayAnimationAsync(Id);
            await ExecuteInternal(targets, Owner);
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

        public void AddEffect(IEffect effect, bool targetEffect = true)
        {
            if (targetEffect) Effects.Add(effect);
        }

        public void RemoveEffect(string id, bool targetEffect = true)
        {
            if (targetEffect)
            {
                var exist = Effects.FirstOrDefault(c => c.Id == id);
                if (exist != null) RemoveFromList(Effects, exist);
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

        protected virtual Task ExecuteInternal(List<IEntity> targets, IEntity owner) => Task.CompletedTask;

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
        }

        protected float ApplyConditionalModifiers(EffectApplyingContext context, AbilityParameter parameter, float baseValue)
        {
            float additiveBonus = 0f;
            float increasedBonus = 1f;
            float multiplyBonus = 1f;

            // foreach (IConditionalModifier conditionalModifier in ConditionalModifiers)
            // {
            //     if (conditionalModifier.Parameter != parameter) continue;
            //     (float Value, ModifierType Type)? result = conditionalModifier.GetValue(context);
            //     if (result == null) continue;
            //
            //     switch (result.Value.Type)
            //     {
            //         case ModifierType.Flat:
            //             additiveBonus += result.Value.Value;
            //             break;
            //         case ModifierType.Increase:
            //             increasedBonus += result.Value.Value;
            //             break;
            //         case ModifierType.Multiplicative:
            //             multiplyBonus += result.Value.Value;
            //             break;
            //     }
            // }

            return ((baseValue + additiveBonus) * increasedBonus) * multiplyBonus;
        }

        protected void OnModuleChanges<TKey>(TKey key) where TKey : struct, Enum => OnParameterChanged?.Invoke(key);

        protected virtual string FormatDescription() => Localization.LocalizeDescriptionFormated(Id);

        protected virtual void OnTurnEnd(TurnEndEvent obj)
        {
            if (CooldownLeft == 0) return;
            CooldownLeft--;
            CooldownLeftChanges?.Invoke(this, CooldownLeft);
        }

        private IModuleManager<AbilityParameter, IParameterModule<AbilityParameter>, AbilityParameterDecorator<AbilityParameter>> CreateModuleManager() =>
            new ModuleManager<AbilityParameter, IParameterModule<AbilityParameter>, AbilityParameterDecorator<AbilityParameter>>(
                new Dictionary<AbilityParameter, IParameterModule<AbilityParameter>>
                {
                    [AbilityParameter.Damage] = new Module<AbilityParameter>(() => damage, AbilityParameter.Damage),
                    [AbilityParameter.Cooldown] = new Module<AbilityParameter>(() => cooldown, AbilityParameter.Cooldown),
                    [AbilityParameter.CostValue] = new Module<AbilityParameter>(() => costValue, AbilityParameter.CostValue),
                    [AbilityParameter.CostType] = new Module<AbilityParameter>(() => (float)costType, AbilityParameter.CostType),
                    [AbilityParameter.WeaponDamageScale] = new Module<AbilityParameter>(() => weaponDamageScale, AbilityParameter.WeaponDamageScale),
                    [AbilityParameter.SpellDamageScale] = new Module<AbilityParameter>(() => spellDamageScale, AbilityParameter.SpellDamageScale)
                });

        private void RemoveFromList(List<IEffect> listEffects, IEffect effect) => listEffects.Remove(effect);

        private void OnResourceChanges(float obj) => AbilityResourceChanges?.Invoke(this, IsEnoughResource());
    }
}
