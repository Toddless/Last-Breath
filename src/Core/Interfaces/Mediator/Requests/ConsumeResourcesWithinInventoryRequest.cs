namespace Core.Interfaces.Mediator.Requests
{
    using System.Collections.Generic;

    public record ConsumeResourcesWithinInventoryRequest(Dictionary<string, int> Resources) : IRequest { }
}
