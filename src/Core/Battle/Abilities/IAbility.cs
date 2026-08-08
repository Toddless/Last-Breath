namespace Core.Battle.Abilities
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Enums;
    using Entity;
    using Interfaces;

    public interface IAbility : IIdentifiable, IDisplayable, ITaggable
    {
        float Cooldown { get; }
        int CooldownLeft { get; set; }
        int CostValue { get; }
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
        /// <summary>
        /// The augments the ability is wearing, keyed by the ADDRESS of the socket each of them sits in
        /// (<see cref="AbilitySocketPlacement.Address"/>). A socket and not a tier: two nodes of one tier
        /// on one ability are two slots (see <see cref="IAbilitySocketBoard"/>), and a tier-keyed
        /// arrangement could only hold one of them.
        /// Each entry was built from the COPY that was seated, so its numbers are that copy's own.
        /// </summary>
        IReadOnlyDictionary<string, IAbilityUpgrade> InstalledUpgrades { get; }

        event Action<string>? OnParameterChanged;
        event Action<IAbility, bool>? AbilityResourceChanges;
        event Action<IAbility, int>? CooldownLeftChanges;

        /// <summary>
        /// Makes the ability wear exactly these upgrades and nothing else: everything worn is removed
        /// first, then every entry of the set is applied. The whole arrangement arrives at once because
        /// it is not the ability's to decide — the sockets are the truth of what it wears, and an ability
        /// that could be told about one slot at a time is an ability whose build can drift away from them.
        /// </summary>
        /// <param name="bySocket">Socket id → the upgrade the augment in that socket installs.</param>
        void InstallUpgrades(IReadOnlyDictionary<string, IAbilityUpgrade> bySocket);
        Task Execute(List<IFightable> targets, IBattleField field);
        void AddParameterDecorator(AbilityParameterDecorator decorator);
        void RemoveParameterDecorator(string decoratorId, string parameter);
        void SetOwner(IFightable owner);
        bool IsEnoughResource();

        /// <summary>Full availability check: resource, cooldown and the owner's status (Paralysis blocks casting).</summary>
        bool CanActivate();
        void RemoveOwner();
        IAbility Copy();
    }
}
