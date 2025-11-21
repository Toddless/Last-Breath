namespace Core.Interfaces.Events
{
    using Items;

    public record ItemCreatedEvent(IItem CreatedItem) : IEvent { }
}
