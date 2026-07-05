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
        int MasteryLevel { get; set; }
        Costs CostType { get; }
        Stance Stance { get; }

        /// <summary>How the ability selects its targets. Swappable per ability.</summary>
        ITargetingStrategy Targeting { get; }
        Dictionary<string, IAbilityActivationModifier> ActivationEffect { get; }
        Dictionary<string, IAbilityPostActivationModifier> PostActivationEffect { get; }
        Dictionary<int, List<IAbilityUpgrade>> Upgrades { get; }

        /// <summary>The chosen upgrade per tier (one of three); selection is changeable outside battle.</summary>
        IReadOnlyDictionary<int, IAbilityUpgrade> CurrentUpgrades { get; }

        event Action<Enum>? OnParameterChanged;
        event Action<IAbility, bool>? AbilityResourceChanges;
        event Action<IAbility, int>? CooldownLeftChanges;

        void SetAbilityUpgrades(Dictionary<int, List<IAbilityUpgrade>> upgrades);

        /// <summary>Applies the tier's upgrade by its InstanceId, removing the previously chosen one.</summary>
        void SelectUpgrade(int tier, string upgradeInstanceId);

        /// <summary>Removes the tier's chosen upgrade without selecting a replacement.</summary>
        void ClearUpgrade(int tier);
        Task Execute(List<IFightable> targets, IBattleField field);
        void AddParameterDecorator<T>(IModuleDecorator<T, IParameterModule<T>> decorator) where T : struct, Enum;
        void RemoveParameterDecorator<T>(string id, T key) where T : struct, Enum;
        void SetOwner(IFightable owner);
        bool IsEnoughResource();

        /// <summary>Full availability check: resource, cooldown and the owner's status (Paralysis blocks casting).</summary>
        bool CanActivate();
        void RemoveOwner();
        IAbility Copy();
    }
}
