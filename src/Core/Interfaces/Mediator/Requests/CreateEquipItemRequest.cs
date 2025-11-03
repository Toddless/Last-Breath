namespace Core.Interfaces.Mediator.Requests
{
    using System.Collections.Generic;
    using Core.Interfaces.Items;

    public record CreateEquipItemRequest(string RecipeId, Dictionary<string, int> UsedResources) : IRequest<IEquipItem?> { }
}
