namespace Core.Interfaces.Mediator.Requests
{
    using System.Collections.Generic;

    public record OpenCraftingItemsWindowRequest(IEnumerable<string> TakenResources) : IRequestWithResponce<IEnumerable<string>> { }
}
