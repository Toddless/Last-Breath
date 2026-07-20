namespace Core.MessageBus.Messages
{
    using Items;

    public record ItemCreatedMessage(IItem CreatedItem) : IMessage { }
}
