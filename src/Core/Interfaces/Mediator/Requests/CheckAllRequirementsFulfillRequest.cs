namespace Core.Interfaces.Mediator.Requests
{
    using System.Collections.Generic;

    public record CheckAllRequirementsFulfillRequest(IEnumerable<IRequirement> requrements) : IRequest<bool> { }
}
