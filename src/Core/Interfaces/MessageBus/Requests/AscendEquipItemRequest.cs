namespace Core.Interfaces.MessageBus.Requests
{
    using Results;

    public record AscendEquipItemRequest(string InstanceId) : IRequest<AscensionResult>;
}
