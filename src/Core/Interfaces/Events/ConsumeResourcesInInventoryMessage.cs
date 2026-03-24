namespace Core.Interfaces.Events
{
    using System.Collections.Generic;

    public record ConsumeResourcesInInventoryMessage(Dictionary<string, int> Resources) : IMessage { }
}
