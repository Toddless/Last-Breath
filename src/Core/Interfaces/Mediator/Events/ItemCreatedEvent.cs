namespace Core.Interfaces.Mediator.Events
{
    using Core.Interfaces.Items;

    public record ItemCreatedEvent(IItem CreatedItem) : IEvent { }
}
