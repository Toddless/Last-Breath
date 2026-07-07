namespace Core.Events
{
    public record ItemAmountChangedMessage(string ItemId, int NewTotalAmount) : IMessage
    {
    }
}
