namespace Core.MessageBus.Messages
{
    public record DestroyItemMessage(string ItemInstanceId) : IMessage { }
}
