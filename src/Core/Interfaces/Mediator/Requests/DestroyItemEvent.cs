namespace Core.Interfaces.Mediator.Requests
{
    public record DestroyItemEvent(string ItemInstanceId) : IEvent { }
}
