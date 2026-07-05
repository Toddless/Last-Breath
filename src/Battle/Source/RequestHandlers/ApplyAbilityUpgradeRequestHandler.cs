namespace Battle.Source.RequestHandlers
{
    using System.Linq;
    using System.Threading.Tasks;
    using Core.Interfaces;
    using Core.Interfaces.MessageBus;
    using Core.Interfaces.MessageBus.Requests;
    using Core.Views;

    /// <summary>
    /// Applies the chosen upgrade to the learned ability instance (base Ability.SelectUpgrade swaps
    /// out the previous choice in that tier) and returns the refreshed detail view.
    /// </summary>
    public class ApplyAbilityUpgradeRequestHandler(IPlayerAccessor playerAccessor)
        : IRequestHandler<ApplyAbilityUpgradeRequest, AbilityUpgradeView>
    {
        public Task<AbilityUpgradeView> HandleRequest(ApplyAbilityUpgradeRequest request)
        {
            var ability = playerAccessor.Player?.AbilityBook.AllAbilities.FirstOrDefault(a => a.Id == request.AbilityId);
            ability?.SelectUpgrade(request.Tier, request.UpgradeInstanceId);
            return Task.FromResult(AbilityUpgradeViewFactory.Build(playerAccessor, request.AbilityId));
        }
    }
}
