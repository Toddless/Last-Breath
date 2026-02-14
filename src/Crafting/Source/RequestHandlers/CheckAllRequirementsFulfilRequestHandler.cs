namespace Crafting.Source.RequestHandlers
{
    using System.Threading.Tasks;
    using Core.Interfaces.MessageBus;
    using Core.Interfaces.MessageBus.Requests;

    public class CheckAllRequirementsFulfilRequestHandler : IRequestHandler<CheckAllRequirementsFulfillRequest, bool>
    {
        public Task<bool> HandleRequest(CheckAllRequirementsFulfillRequest request)
        {
            return Task.FromResult(true);
        }
    }
}
