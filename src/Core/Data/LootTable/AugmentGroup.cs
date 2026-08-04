namespace Core.Data.LootTable
{
    using Enums;

    /// <summary>
    /// A set of augments named by what its members are instead of one by one. A table writing every
    /// augment out would hand the augments' share of the drop to the length of that list, because a
    /// tier picks uniformly among the positions it holds: a hundred augments written down are a
    /// hundred positions, and the equipment beside them is drowned. Named as a set they are ONE
    /// position, and how often augments drop against everything else goes back to being a number a
    /// designer writes.
    /// <para>
    /// Both halves of the filter are named by every group. An unstated half would read as "any", and
    /// a group that says "any augment there is" gives the same answer as the list it replaced — the
    /// share follows whatever the catalog happens to hold.
    /// </para>
    /// <para>
    /// Which augment a group turns into is not decided here and not at load: the set is carried to
    /// the drop and drawn from when the item is minted, so an augment added to the game belongs to
    /// every group that describes it without a table being touched.
    /// </para>
    /// </summary>
    /// <param name="Tier">The augment's own tier — how strong its members are.</param>
    /// <param name="Rarity">Where the members stand on the common item scale.</param>
    public record AugmentGroup(int Tier, Rarity Rarity);
}
