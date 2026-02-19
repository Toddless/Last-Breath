namespace Core.Interfaces.MessageBus.Requests
{
    using System.Collections.Generic;

    public record CheckAllRequirementsFulfillRequest(IEnumerable<IRequirement> requrements) : IRequest<bool> { }
}
