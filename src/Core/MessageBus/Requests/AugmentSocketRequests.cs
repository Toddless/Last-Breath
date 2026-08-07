namespace Core.MessageBus.Requests
{
    using Battle.Abilities;

    /// <summary>Moves the augment the bag holds under <paramref name="ItemInstanceId"/> into the slot.
    /// The item LEAVES the bag: one copy of an augment worn and carried at once is one copy that can be
    /// seated twice and sold while worn. A refusal moves nothing and says which gate answered.</summary>
    public record InstallAugmentRequest(string SocketId, string ItemInstanceId) : IRequest<AugmentInstallResult>;

    /// <summary>Takes the augment out of the slot and back into the bag — the same copy, with the
    /// numbers it was minted with. A bag with no room for it is a refusal, not a loss: the augment
    /// stays in the slot, because nothing in the game can draw its numbers again.</summary>
    public record ExtractAugmentRequest(string SocketId) : IRequest<AugmentExtractResult>;
}
