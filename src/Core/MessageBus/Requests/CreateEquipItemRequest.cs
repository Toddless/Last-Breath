namespace Core.MessageBus.Requests
{
    using System.Collections.Generic;
    using Items;

    /// <summary>The recipe's mandatory resources and the optional additives travel split: the item
    /// remembers which slot each resource came from (required vs optional creation part).</summary>
    public record CreateEquipItemRequest(string RecipeId, Dictionary<string, int> RequiredResources, Dictionary<string, int> OptionalResources) : IRequest<IEquipItem?> { }
}
