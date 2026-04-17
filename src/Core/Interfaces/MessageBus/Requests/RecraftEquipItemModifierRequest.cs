namespace Core.Interfaces.MessageBus.Requests
{
    using Modifiers;
    using System.Collections.Generic;

    public record RecraftEquipItemModifierRequest(string ItemInstanceId, int ModifierHash, Dictionary<string, int> Resources) : IRequest<RequestResult<IModifierInstance>>
    {
    }
}
