namespace Core.Interfaces.Events
{
    public record DestroyItemMessage(string ItemInstanceId) : IMessage { }
}
