namespace Core.Events
{
    using Items;

    public record ItemCreatedMessage(IItem CreatedItem) : IMessage { }
}
