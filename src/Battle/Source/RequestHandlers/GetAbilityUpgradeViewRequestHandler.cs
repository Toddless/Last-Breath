namespace Battle.Source.RequestHandlers
{
    using System.Threading.Tasks;
    using Core.Interfaces;
    using Core.Interfaces.MessageBus;
    using Core.Interfaces.MessageBus.Requests;
    using Core.Views;

    /// <summary>Detail view for one ability, built from the learned instance in the player's book.</summary>
    public class GetAbilityUpgradeViewRequestHandler(IPlayerAccessor playerAccessor)
        : IRequestHandler<GetAbilityUpgradeViewRequest, AbilityUpgradeView>
    {
        public Task<AbilityUpgradeView> HandleRequest(GetAbilityUpgradeViewRequest request) =>
            Task.FromResult(AbilityUpgradeViewFactory.Build(playerAccessor, request.AbilityId));
    }
}
