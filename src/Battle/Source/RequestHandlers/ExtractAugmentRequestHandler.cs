namespace Battle.Source.RequestHandlers
{
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.MessageBus.Requests;

    /// <summary>
    /// The way back out of a slot, and the mirror of the install: the same copy the player put in
    /// returns to his bag with the numbers it was minted with.
    /// <para>
    /// The bag takes the item BEFORE the slot lets go of the copy. An augment's numbers were drawn
    /// once and nothing in the game can draw them again, so the one order that cannot lose it is the
    /// one where it never stands outside both: a bag with no room refuses, and the augment is still
    /// worn — which is the state the player can act on — rather than dropped on the way home.
    /// </para>
    /// <para>
    /// An extraction that took ends by putting the board back onto the abilities, exactly as a seating
    /// does: the augment is out of the slot, so the ability must stop doing what it did. A refusal
    /// binds nothing — the arrangement never moved.
    /// </para>
    /// </summary>
    public class ExtractAugmentRequestHandler(
        IAbilitySocketBoard sockets,
        IAugmentItemMinter augments,
        IInventory inventory,
        IAbilityAugmentBinder binder)
        : IRequestHandler<ExtractAugmentRequest, AugmentExtractResult>
    {
        public Task<AugmentExtractResult> HandleRequest(ExtractAugmentRequest request)
        {
            if (sockets.Find(request.SocketId)?.Augment is not { } augment)
                return Task.FromResult(AugmentExtractResult.NothingToExtract);

            if (augments.Restore(augment) is not { } item)
                return Task.FromResult(AugmentExtractResult.CannotBeHeld);

            if (!inventory.TryAddItem(item))
                return Task.FromResult(AugmentExtractResult.NoBagRoom);

            sockets.Extract(request.SocketId);
            binder.Bind();
            return Task.FromResult(AugmentExtractResult.Extracted);
        }
    }
}
