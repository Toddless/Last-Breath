namespace Core.Interfaces.Mediator.Events
{
    public record DestroyItemEvent(string ItemInstanceId) : IEvent { }
}
