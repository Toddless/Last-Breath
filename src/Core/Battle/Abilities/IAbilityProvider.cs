namespace Core.Battle.Abilities
{
    using System.Collections.Generic;
    using Enums;

    public interface IAbilityProvider
    {
        IReadOnlyCollection<string> KnownAbilityIds { get; }
        IAbility CreateAbility(string abilityId);

        /// <summary>The upgrade one COPY of an augment installs — the record it names, with the numbers
        /// this copy rolled put in ahead of the record's own. Null when nothing can be built for it:
        /// the catalog declares no such record, or the registry holds no way of making one.</summary>
        IAbilityAugment? CreateUpgrade(AugmentInstance augment);

        /// <summary>The stance an ability belongs to — the ability book partitions by it on Learn.</summary>
        Stance GetAbilityStance(string abilityId);

        /// <summary>Internal-cast-only ability (boss reactions): the unlock service and the mastery
        /// trees must skip it. Default false — only data-backed providers know better.</summary>
        bool IsHidden(string abilityId) => false;
    }
}
