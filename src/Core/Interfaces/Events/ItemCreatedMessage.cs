namespace Core.Interfaces.Events
{
    using Items;

    public record ItemCreatedMessage(IItem CreatedItem) : IMessage { }
}
