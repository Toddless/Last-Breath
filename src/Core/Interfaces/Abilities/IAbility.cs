namespace Core.Interfaces.Abilities
{
    using Enums;
    using Entity;
    using System;
    using Components.Module;
    using Components.Decorator;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    public interface IAbility : IIdentifiable, IDisplayable, ITaggable
    {
        float MaxTargets { get; }
        float SpendAbilityPoints { get; set; }
        float Cooldown { get; }
        int CooldownLeft { get; }
        int CostValue { get; }
        Costs CostType { get; }
        AbilityType AbilityType { get; }
        List<IEffect> Effects { get; set; }
        List<IEffect> CasterEffects { get; }
        Dictionary<int, List<IAbilityUpgrade>> Upgrades { get; set; }

        event Action<AbilityParameter>? OnParameterChanged;
        event Action<IAbility, bool>? AbilityResourceChanges;
        event Action<IAbility, int>? CooldownLeftChanges;

        Task Activate(List<IEntity> targets);

        void AddParameterUpgrade<T>(IModuleDecorator<T, IParameterModule<T>> decorator)
            where T : struct, Enum;

        void RemoveParameterUpgrade<T>(string id, T key)
            where T : struct, Enum;

        bool IsEnoughResource();
        void AddCondition(IConditionalModifier modifier);
        void RemoveCondition(string id);
        void ClearConditions();
        void AddEffect(IEffect effect, bool targetEffect = true);
        void RemoveEffect(string id, bool targetEffect = true);
        void SetOwner(IEntity owner);
        void RemoveOwner();
    }
}
