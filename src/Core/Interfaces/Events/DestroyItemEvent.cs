namespace Core.Interfaces.Events
{
    public record DestroyItemEvent(string ItemInstanceId) : IEvent { }
}
