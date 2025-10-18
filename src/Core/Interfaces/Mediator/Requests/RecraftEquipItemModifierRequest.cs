namespace Core.Interfaces.Mediator.Requests
{
    using System.Collections.Generic;

    public record RecraftEquipItemModifierRequest(string ItemInstanceID, int ModifierHash, Dictionary<string, int> Resources) : IRequestWithResponce<RequestResult<IModifierInstance>>
    {
    }
}
