namespace Core.Interfaces.Mediator.Events
{
    using System.Collections.Generic;

    public record ConsumeResourcesInInventoryEvent(Dictionary<string, int> Resources) : IEvent { }
}
