namespace Battle.TestData.Abilities
{
    using Godot;
    using System;
    using Utilities;
    using Decorators;
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Abilities;
    using System.Collections.Generic;
    using Core.Interfaces.Components;
    using Core.Interfaces.Battle.Module;
    using Core.Interfaces.Battle.Decorator;

    public abstract class Ability(
        string id,
        string[] tags,
        int availablePoints,
        Texture2D? icon,
        List<IEffect> effects,
        Dictionary<int, List<IAbilityUpgrade>> upgrades,
        IAbilityCost cost,
        IStanceMastery mastery)
        : IAbility
    {
        protected readonly IStanceMastery Mastery = mastery;

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
        public string[] Tags { get; } = tags;
        public float AvailablePoints { get; set; } = availablePoints;
        public Texture2D? Icon { get; } = icon;

        public IAbilityCost Cost { get; } = cost;

        public List<IEffect> Effects { get; set; } = effects;
        public Dictionary<int, List<IAbilityUpgrade>> Upgrades { get; set; } = upgrades;

        public float MaxTargets => this[AbilityParameter.Target];
        public float Cooldown => this[AbilityParameter.Cooldown];

        public string Description => Localizator.LocalizeDescription(Id);
        public string DisplayName => Localizator.Localize(Id);

        public event Action<AbilityParameter>? OnParameterChanged;

        public virtual void Activate(AbilityContext context)
        {
            // Where do ability damage, critical chance and critical damage come from?
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


        public virtual bool HasTag(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);

        protected abstract IModuleManager<AbilityParameter, IParameterModule<AbilityParameter>, AbilityParameterDecorator> CreateModuleManager();

        private void OnModuleChanges(AbilityParameter key) => OnParameterChanged?.Invoke(key);
    }
}
