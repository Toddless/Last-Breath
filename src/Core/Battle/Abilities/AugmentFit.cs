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
    /// tier, then binding — an augment naming an ability is judged by that name alone, one naming
    /// none is judged by its tags — then the exclusion group the ability already wears. Nothing of
    /// this is encoded in the data: a record says what the augment is, never which socket takes it.
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
            if (augment.Tier > slot.Tier) return AugmentFitResult.TierAboveSocket;

            AugmentFitResult binding = CheckBinding(slot, abilityTags, augment);
            if (binding != AugmentFitResult.Fits) return binding;

            return Conflicts(augment, occupiedExclusionGroups)
                ? AugmentFitResult.ExclusionGroupTaken
                : AugmentFitResult.Fits;
        }

        /// <summary>Which ability the augment may go on. A named one is the whole answer — an augment
        /// strong enough to be written for one ability is not offered to a family through a tag it
        /// happens to carry. Everything else is tag work, and one shared tag is enough.</summary>
        private static AugmentFitResult CheckBinding(
            AbilitySocketPlacement slot,
            IReadOnlyCollection<string> abilityTags,
            AbilityUpgradeData augment)
        {
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
