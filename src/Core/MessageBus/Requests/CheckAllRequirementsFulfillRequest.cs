namespace Core.MessageBus.Requests
{
    using System.Collections.Generic;
    using Interfaces;

    public record CheckAllRequirementsFulfillRequest(IEnumerable<IRequirement> requrements) : IRequest<bool> { }
}
