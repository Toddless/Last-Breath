namespace Core.Interfaces.Abilities
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
    }
}
