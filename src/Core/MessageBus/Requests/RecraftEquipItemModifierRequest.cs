namespace Core.MessageBus.Requests
{
    using System.Collections.Generic;

    /// <summary>Reroll one modifier line. The request carries ONLY the optional additives (essences) —
    /// the mandatory price is computed by the handler from the item itself (rarity/category base ×
    /// the reroll-count growth), so a stale UI can never under- or overpay.</summary>
    public record RecraftEquipItemModifierRequest(string ItemInstanceId, string ModifierInstanceId, Dictionary<string, int> AdditiveResources) : IRequest<RequestResult<string>>
    {
    }
}
