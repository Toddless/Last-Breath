namespace Core.MessageBus.Requests
{
    using System.Collections.Generic;

    public record RecraftEquipItemModifierRequest(string ItemInstanceId, string ModifierInstanceId, Dictionary<string, int> Resources) : IRequest<RequestResult<string>>
    {
    }
}
