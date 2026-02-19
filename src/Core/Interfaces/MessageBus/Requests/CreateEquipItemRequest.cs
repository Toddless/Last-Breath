namespace Core.Interfaces.MessageBus.Requests
{
    using System.Collections.Generic;
    using Items;

    public record CreateEquipItemRequest(string RecipeId, Dictionary<string, int> UsedResources) : IRequest<IEquipItem?> { }
}
