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

        /// <summary>
        /// The ability's own number under a parameter key, decorated by everything seated on it. This
        /// is what an impact's <c>Source</c> is a channel to: a rider reaching for it at the moment it
        /// works reads what the cast is worth NOW, augments and all, instead of what it was worth when
        /// the rider was built. An unregistered key is reported and answers zero.
        /// </summary>
        float this[string parameter] { get; }

        /// <summary>How strongly what this cast lays lands: the decorated
        /// <see cref="AbilityParameter.Effectiveness"/>, and ONE for an ability that never declared it.</summary>
        float Effectiveness { get; }

        /// <summary>The parameter's decorated value, or the given figure when the ability never declared
        /// it — for readers where absence is an answer rather than a typo.</summary>
        float ValueOr(string parameter, float fallback);

        /// <summary>Whether the ability declared the parameter at all. Distinct from its value being
        /// nothing: a reader that cannot tell the two apart hands a zero on where it meant to hand
        /// nothing, and a zero is a number somebody downstream will use.</summary>
        bool Declares(string parameter);

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
        IReadOnlyDictionary<string, IAbilityAugment> InstalledUpgrades { get; }

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
        void InstallUpgrades(IReadOnlyDictionary<string, IAbilityAugment> bySocket);
        Task Execute(List<IFightable> targets, IBattleField field);
        void AddParameterDecorator(AbilityParameterDecorator decorator);
        void RemoveParameterDecorator(string decoratorId, string parameter);

        /// <summary>
        /// Lends the ability a parameter it does not own, for an upgrade that arrives with a number of
        /// its own: from here on the number is the cast's, readable through the indexer and open to
        /// every decorator seated on that key — which is what makes "applies poison" and "poison lasts
        /// longer" add up without either of them being written for the other.
        /// True when this call is what put the key there; false when the ability already had it, and
        /// then the base value stays the ability's own.
        /// </summary>
        bool TryRegisterParameter(string parameter, float value);

        /// <summary>Takes back a parameter lent by <see cref="TryRegisterParameter"/> — only the
        /// upgrade whose registration succeeded may, or one leaving would strip a number another is
        /// still standing on.</summary>
        void UnregisterParameter(string parameter);
        void SetOwner(IFightable owner);
        bool IsEnoughResource();

        /// <summary>Full availability check: resource, cooldown and the owner's status (Paralysis blocks casting).</summary>
        bool CanActivate();
        void RemoveOwner();
        IAbility Copy();
    }
}
