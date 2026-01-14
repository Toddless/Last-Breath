namespace Core.Interfaces.Events
{
    using System.Collections.Generic;

    public record ConsumeResourcesInInventoryEvent(Dictionary<string, int> Resources) : IEvent { }
}
