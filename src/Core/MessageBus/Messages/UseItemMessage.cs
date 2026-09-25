namespace Core.MessageBus.Messages
{
    public record UseItemMessage(string ItemInstanceId) : IMessage { }
}
