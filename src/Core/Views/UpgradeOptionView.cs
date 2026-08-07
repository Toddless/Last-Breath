namespace Core.Views
{
    /// <summary>
    /// One augment an ability is wearing: the slot it sits in, what it is called and what THIS copy
    /// does — the numbers printed are the ones that copy rolled, not the record's averages.
    /// Nothing here is a choice. An ability is upgraded by exactly the augments in its sockets, so the
    /// view has one row per filled socket and no row for anything that merely could go in.
    /// </summary>
    /// <param name="SocketId">The slot, which is the passive node that opened it.</param>
    /// <param name="Tier">The tier the augment itself is written at.</param>
    public record UpgradeOptionView(string SocketId, string DisplayName, string Description, int Tier);
}
