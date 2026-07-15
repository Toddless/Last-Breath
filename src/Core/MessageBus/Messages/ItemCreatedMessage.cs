namespace Core.MessageBus.Messages
{
    using Items;
    using Events;

    public record ItemCreatedMessage(IItem CreatedItem) : IMessage { }
}
