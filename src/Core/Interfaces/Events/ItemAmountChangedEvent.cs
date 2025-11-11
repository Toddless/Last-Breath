namespace Core.Interfaces.Events
{
    public record ItemAmountChangedEvent(string ItemId, int NewTotalAmount) : IEvent
    {
    }
}
