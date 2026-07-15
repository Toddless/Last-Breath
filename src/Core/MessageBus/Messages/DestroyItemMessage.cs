namespace Core.MessageBus.Messages
{
    using Events;

    public record DestroyItemMessage(string ItemInstanceId) : IMessage { }
}
