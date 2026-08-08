namespace Core.MessageBus.Requests
{
    using System.Collections.Generic;
    using Enums;
    using Views;

    /// <summary>
    /// The socket sheet, as far as the caller asked for it. A READ and nothing else — an augment moves
    /// only through <see cref="InstallAugmentRequest"/> and <see cref="ExtractAugmentRequest"/>.
    /// <para>
    /// The rows are the abilities that have a slot of any kind, live or closed, plus the ones the book
    /// holds. The first half is what keeps an ability the player no longer owns on screen while his
    /// augments are still in it; the second is a guarantee that an ability which ever reaches the book
    /// by some road other than the tree still gets a row, empty of cells.
    /// </para>
    /// </summary>
    /// <param name="Stance">Only this stance's rows; every stance when null.</param>
    /// <param name="AbilityId">Only this ability's row; every ability when null. What a single-ability
    /// panel (the passive wheel's) asks with.</param>
    public record GetAbilitySocketRowsRequest(Stance? Stance = null, string? AbilityId = null)
        : IRequest<IReadOnlyList<AbilitySocketRowView>>;
}
