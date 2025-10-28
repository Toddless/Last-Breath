namespace Crafting.Source.MediatorHandlers
{
    using System.Threading.Tasks;
    using Core.Interfaces.Mediator;
    using Core.Interfaces.Mediator.Requests;

    public class CheckAllRequirementsFulfilRequestHandler : IRequestHandler<CheckAllRequirementsFulfillRequest, bool>
    {


        public Task<bool> Handle(CheckAllRequirementsFulfillRequest request)
        {
            return Task.FromResult(true);
        }
    }
}
