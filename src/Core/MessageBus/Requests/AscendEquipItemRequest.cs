namespace Core.MessageBus.Requests
{
    using Results;

    public record AscendEquipItemRequest(string InstanceId) : IRequest<AscensionResult>;
}
