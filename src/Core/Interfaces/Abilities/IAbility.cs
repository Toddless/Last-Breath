namespace Core.Interfaces.Abilities
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Battle;
    using Components.Decorator;
    using Components.Module;
    using Entity;
    using Enums;

    public interface IAbility : IIdentifiable, IDisplayable, ITaggable
    {
        float Cooldown { get; }
        int CooldownLeft { get; set; }
        int CostValue { get; }
        Costs CostType { get; }
        Dictionary<string, IAbilityActivationModifier> ActivationEffect { get; }
        Dictionary<string, IAbilityPostActivationModifier> PostActivationEffect { get; }
        Dictionary<int, List<IAbilityUpgrade>> Upgrades { get; }

        event Action<Enum>? OnParameterChanged;
        event Action<IAbility, bool>? AbilityResourceChanges;
        event Action<IAbility, int>? CooldownLeftChanges;

        void SetAbilityUpgrades(Dictionary<int, List<IAbilityUpgrade>> upgrades);
        Task Execute(List<IFightable> targets, IBattleField field);
        void AddParameterDecorator<T>(IModuleDecorator<T, IParameterModule<T>> decorator) where T : struct, Enum;
        void RemoveParameterDecorator<T>(string id, T key) where T : struct, Enum;
        void SetOwner(IFightable owner);
        bool IsEnoughResource();
        void RemoveOwner();
        IAbility Copy();
    }
}
