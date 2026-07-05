namespace Core.Interfaces.MessageBus.Requests
{
    using Views;

    /// <summary>Selects one upgrade in a tier (base Ability swaps out the previous choice) and returns the refreshed view.</summary>
    public record ApplyAbilityUpgradeRequest(string AbilityId, string UpgradeInstanceId, int Tier) : IRequest<AbilityUpgradeView> { }
}
