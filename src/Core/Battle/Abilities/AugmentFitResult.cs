namespace Core.Battle.Abilities
{
    /// <summary>
    /// The verdict of <see cref="AugmentFit"/>. Refusals are named rather than collapsed into a false,
    /// so a caller can tell the player what went wrong without working the rule out a second time.
    /// </summary>
    public enum AugmentFitResult
    {
        /// <summary>The augment belongs in the slot.</summary>
        Fits,

        /// <summary>The augment is of a higher tier than the slot. A slot takes its own tier and every
        /// tier below it, so a low augment never becomes unusable — a high one stays out.</summary>
        TierAboveSocket,

        /// <summary>The augment names an ability, and it is not the slot's.</summary>
        BoundToAnotherAbility,

        /// <summary>The augment names no ability and shares no tag with the slot's one.</summary>
        NoSharedTag,

        /// <summary>The ability already wears an augment of the same exclusion group.</summary>
        ExclusionGroupTaken
    }
}
