namespace Core.Interfaces.Events
{
    using Core.Interfaces.Items;

    public record ItemCreatedEvent(IItem CreatedItem) : IEvent { }
}
