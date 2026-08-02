namespace Core.Battle.Abilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Data.AbilityData;

    /// <summary>
    /// Whether an augment belongs in a slot — the whole rule, and the only place it is written. The
    /// board asks before seating; a socket window, a conversion or a drop filter asks the same
    /// question here instead of comparing tiers and tags on its own, because two copies of the rule
    /// are two answers: an augment the window offers and the board refuses.
    ///
    /// The augment declares itself and the slot is measured against that declaration, in this order:
    /// the record's own coherence, then tier, then binding — an augment claiming every ability is
    /// past that question, one naming an ability is judged by that name alone, one naming neither is
    /// judged by its tags — then the exclusion group the ability already wears. Nothing of this is
    /// encoded in the data: a record says what the augment is, never which socket takes it.
    /// </summary>
    public static class AugmentFit
    {
        /// <param name="slot">The slot as it stands: the ability it belongs to and the tier it takes.</param>
        /// <param name="abilityTags">The combat tags of the slot's ability.</param>
        /// <param name="augment">The augment's own record.</param>
        /// <param name="occupiedExclusionGroups">The exclusion groups already worn by that ability.</param>
        public static AugmentFitResult Check(
            AbilitySocketPlacement slot,
            IReadOnlyCollection<string> abilityTags,
            AbilityUpgradeData augment,
            IReadOnlyCollection<string> occupiedExclusionGroups)
        {
            if (ContradictsItself(augment)) return AugmentFitResult.ContradictoryDeclaration;

            if (augment.Tier > slot.Tier) return AugmentFitResult.TierAboveSocket;

            AugmentFitResult binding = CheckBinding(slot, abilityTags, augment);
            if (binding != AugmentFitResult.Fits) return binding;

            return Conflicts(augment, occupiedExclusionGroups)
                ? AugmentFitResult.ExclusionGroupTaken
                : AugmentFitResult.Fits;
        }

        /// <summary>Whether the record answers the binding question twice and differently — claiming
        /// every ability while naming one. Picking either half means discarding the other silently,
        /// and a record nobody can read is refused wherever it is held rather than in the one slot
        /// where the two halves happen to agree.</summary>
        private static bool ContradictsItself(AbilityUpgradeData augment) =>
            augment.FitsAnyAbility && !string.IsNullOrWhiteSpace(augment.AbilityId);

        /// <summary>Which ability the augment may go on. Claiming every ability settles it — that is
        /// what a record working through the base contract every ability honours declares, and it is
        /// claimed rather than inferred, so an augment that merely carries no tag is still refused. A
        /// named ability is otherwise the whole answer — an augment strong enough to be written for
        /// one ability is not offered to a family through a tag it happens to carry. Everything else
        /// is tag work, and one shared tag is enough.</summary>
        private static AugmentFitResult CheckBinding(
            AbilitySocketPlacement slot,
            IReadOnlyCollection<string> abilityTags,
            AbilityUpgradeData augment)
        {
            if (augment.FitsAnyAbility) return AugmentFitResult.Fits;

            if (string.IsNullOrWhiteSpace(augment.AbilityId))
                return AbilityTags.SharesAny(augment.Tags, abilityTags)
                    ? AugmentFitResult.Fits
                    : AugmentFitResult.NoSharedTag;

            return string.Equals(augment.AbilityId, slot.AbilityId, StringComparison.Ordinal)
                ? AugmentFitResult.Fits
                : AugmentFitResult.BoundToAnotherAbility;
        }

        /// <summary>Whether the ability already wears the augment's group. An augment belonging to no
        /// group conflicts with nothing, however many of them the ability carries.</summary>
        private static bool Conflicts(AbilityUpgradeData augment, IReadOnlyCollection<string> occupiedExclusionGroups) =>
            !string.IsNullOrWhiteSpace(augment.ExclusionGroup)
            && occupiedExclusionGroups.Contains(augment.ExclusionGroup, StringComparer.Ordinal);
    }
}
