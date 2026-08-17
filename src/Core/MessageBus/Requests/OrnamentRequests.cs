namespace Core.MessageBus.Requests
{
    using Battle.Abilities;

    /// <summary>Moves the ornament the bag holds under <paramref name="ItemInstanceId"/> onto the
    /// ability, which gains one more socket of the ornament's tier. The item LEAVES the bag: an ornament
    /// worn and carried at once is one that could be attached twice, and there are three in the game. A
    /// refusal moves nothing and says which gate answered.</summary>
    public record AttachOrnamentRequest(string ItemInstanceId, string AbilityId) : IRequest<OrnamentAttachResult>;

    /// <summary>Takes the ornament off whatever ability wears it and back into the bag, and its socket
    /// with it. Refused while that socket holds an augment: the augment comes out on its own road, so
    /// that the fitting rule judges it again wherever it goes next.</summary>
    public record DetachOrnamentRequest(string OrnamentId) : IRequest<OrnamentDetachResult>;
}
