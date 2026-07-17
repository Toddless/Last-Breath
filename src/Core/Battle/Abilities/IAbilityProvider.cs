namespace Core.Battle.Abilities
{
    using System.Collections.Generic;
    using Enums;

    public interface IAbilityProvider
    {
        IReadOnlyCollection<string> KnownAbilityIds { get; }
        IAbility CreateAbility(string abilityId);

        /// <summary>The stance an ability belongs to — the ability book partitions by it on Learn.</summary>
        Stance GetAbilityStance(string abilityId);

        /// <summary>The mastery level at which the ability becomes learnable.</summary>
        int GetMasteryLevel(string abilityId);

        /// <summary>Internal-cast-only ability (boss reactions): the unlock service and the mastery
        /// trees must skip it. Default false — only data-backed providers know better.</summary>
        bool IsHidden(string abilityId) => false;
    }
}
