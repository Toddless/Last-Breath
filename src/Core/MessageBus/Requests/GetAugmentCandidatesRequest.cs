namespace Core.MessageBus.Requests
{
    using System.Collections.Generic;
    using Views;

    /// <summary>
    /// The carried augments this slot would take, in the order they are offered. A READ: nothing moves
    /// until the player picks one and the install gate is asked for real.
    /// <para>
    /// Which copies those are is not the window's question to answer — the fitting rule lives in one
    /// place, and a list assembled by measuring tiers and tags a second time would offer what the gate
    /// then refuses. The same reason the rows carry copies rather than records: two copies of one
    /// augment rolled different numbers, and it is a copy that goes into the slot.
    /// </para>
    /// </summary>
    /// <param name="SocketAddress">The slot's whole signature, as the board names it
    /// (<see cref="Battle.Abilities.AbilitySocketPlacement.Address"/>). Opaque — it is carried straight
    /// back into the install.</param>
    public record GetAugmentCandidatesRequest(string SocketAddress) : IRequest<IReadOnlyList<AugmentTrayTileView>>;
}
