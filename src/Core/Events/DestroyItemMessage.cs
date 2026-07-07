namespace Core.Events
{
    public record DestroyItemMessage(string ItemInstanceId) : IMessage { }
}
