namespace Core.MessageBus.Requests
{
    using System.Collections.Generic;
    using Views;

    /// <summary>
    /// The augments the player is carrying, as a socket window shows them. A READ: the tray is a view
    /// of the bag and never a second place augments live in, so nothing here moves anything.
    /// Copies and not records — two copies of one augment rolled different numbers, and it is a copy
    /// that gets dragged into a slot.
    /// </summary>
    public record GetCarriedAugmentsRequest : IRequest<IReadOnlyList<AugmentTrayTileView>>;
}
