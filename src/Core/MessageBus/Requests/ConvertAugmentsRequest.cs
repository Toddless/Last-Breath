namespace Core.MessageBus.Requests
{
    using System.Collections.Generic;
    using Results;

    /// <summary>
    /// Hands three augments the bag holds over for one drawn in their place — the same tier, the same
    /// rarity, a record none of the three named, and numbers rolled from scratch. The three ids may
    /// name copies of one record or of three; what they may not do is name the same copy twice.
    /// <para>
    /// The tier and the rarity are not asked for: they follow from what is handed over, and a request
    /// that named them could disagree with the copies it carries. A refusal costs nothing at all.
    /// </para>
    /// </summary>
    /// <param name="ItemInstanceIds">The copies offered, by the ids the bag holds them under.</param>
    public record ConvertAugmentsRequest(IReadOnlyList<string> ItemInstanceIds) : IRequest<AugmentConversionResult>;
}
