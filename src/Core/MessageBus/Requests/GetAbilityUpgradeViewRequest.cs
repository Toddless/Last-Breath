namespace Core.MessageBus.Requests
{
    using Views;

    /// <summary>Detail view (cost, cooldown, description, upgrade options) for one ability.</summary>
    public record GetAbilityUpgradeViewRequest(string AbilityId) : IRequest<AbilityUpgradeView> { }
}
