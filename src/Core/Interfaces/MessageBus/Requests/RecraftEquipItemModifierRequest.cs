namespace Core.Interfaces.MessageBus.Requests
{
    using System.Collections.Generic;
    using Modifiers;

    public record RecraftEquipItemModifierRequest(string ItemInstanceID, int ModifierHash, Dictionary<string, int> Resources) : IRequest<RequestResult<IModifierInstance>>
    {
    }
}
