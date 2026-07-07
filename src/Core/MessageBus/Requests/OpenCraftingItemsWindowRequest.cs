namespace Core.MessageBus.Requests
{
    using System.Collections.Generic;

    public record OpenCraftingItemsWindowRequest(IEnumerable<string> TakenResources, string[] Tags, bool IsSingleChose = false) : IRequest<IEnumerable<string>>;
}
