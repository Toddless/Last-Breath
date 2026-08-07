namespace Core.Battle.Abilities
{
    /// <summary>
    /// What came of taking an augment out of a slot and back into the bag. Every answer but
    /// <see cref="Extracted"/> leaves the augment sitting where it was: its numbers were drawn once
    /// and nothing in the game can draw them again, so an extraction that cannot be finished must not
    /// be started.
    /// </summary>
    public enum AugmentExtractResult
    {
        /// <summary>The augment is out of the slot and in the bag — the same copy, with the numbers it
        /// was minted with.</summary>
        Extracted,

        /// <summary>No slot of that id is open, or the one that is stands empty.</summary>
        NothingToExtract,

        /// <summary>The bag has no room for it. The augment stays in the slot rather than being handed
        /// to a bag that would drop it.</summary>
        NoBagRoom,

        /// <summary>The augment cannot be turned back into a thing to carry: no record declares it any
        /// more. It stays in the slot, where it at least goes on being what it was.</summary>
        CannotBeHeld
    }
}
