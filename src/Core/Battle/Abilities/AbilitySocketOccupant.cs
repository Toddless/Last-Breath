namespace Core.Battle.Abilities
{
    /// <summary>
    /// An augment together with the slot it went into. What a save file remembers: a socket id on its
    /// own outlives the slot it named — the next build is free to point that node at another ability
    /// or another tier — so the placement travels with the augment as the signature it was chosen
    /// under, and a slot that no longer matches it is not the slot it came out of.
    /// </summary>
    /// <param name="Slot">The slot as it stood when the augment was installed.</param>
    /// <param name="Augment">Stable id of the augment.</param>
    public readonly record struct AbilitySocketOccupant(AbilitySocketPlacement Slot, string Augment);
}
