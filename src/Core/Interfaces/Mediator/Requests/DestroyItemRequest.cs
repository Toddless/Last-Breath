namespace Core.Interfaces.Mediator.Requests
{
    using Core.Interfaces.Items;

    public record DestroyItemRequest(IItem Item) : IRequest { }
}
