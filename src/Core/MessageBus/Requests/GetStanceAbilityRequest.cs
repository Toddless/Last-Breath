namespace Core.MessageBus.Requests
{
    using System.Collections.Generic;
    using Enums;
    using Views;

    /// <summary>All abilities of a stance for the mastery tree, each with its unlock state.</summary>
    public record GetStanceAbilityRequest(Stance Stance) : IRequest<IReadOnlyList<AbilitySlotView>> { }
}
