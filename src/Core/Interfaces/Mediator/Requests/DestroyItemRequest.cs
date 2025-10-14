namespace Core.Interfaces.Mediator.Requests
{
    public record DestroyItemRequest(string ItemInstanceId) : IRequest { }
}
