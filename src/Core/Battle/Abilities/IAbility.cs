namespace Core.Battle.Abilities
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Components.Decorator;
    using Components.Module;
    using Enums;
    using Entity;
    using Interfaces;

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

        /// <summary>False = no backing out once target selection began: the player must pick a
        /// target and the cast fires (charged Armageddon). Default true.</summary>
        bool IsCancellable => true;
        Dictionary<string, IAbilityActivationModifier> ActivationEffect { get; }

        /// <summary>Riders fired once per cast, after execution (self-buffs, cast-scoped debuffs).</summary>
        Dictionary<string, IActivationRider> ActivationRiders { get; }

        /// <summary>Riders fired on every delivery impact (per hit / bounce / attack of a series).</summary>
        Dictionary<string, IImpactRider> ImpactRiders { get; }
        Dictionary<int, List<IAbilityUpgrade>> Upgrades { get; }

        /// <summary>The chosen upgrade per tier (one of three); selection is changeable outside battle.</summary>
        IReadOnlyDictionary<int, IAbilityUpgrade> CurrentUpgrades { get; }

        event Action<Enum>? OnParameterChanged;
        event Action<IAbility, bool>? AbilityResourceChanges;
        event Action<IAbility, int>? CooldownLeftChanges;

        void SetAbilityUpgrades(Dictionary<int, List<IAbilityUpgrade>> upgrades);

        /// <summary>Applies the tier's upgrade, removing the previously chosen one. Accepts the stable
        /// data Id (survives restarts — save/load path) or the InstanceId (UI selection).</summary>
        void SelectUpgrade(int tier, string upgradeId);

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
