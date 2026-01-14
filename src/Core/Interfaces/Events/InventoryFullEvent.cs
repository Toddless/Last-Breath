namespace Core.Interfaces.Events
{
    public record InventoryFullEvent(string ItemId, string InstanceId, int Amount, int MaxStack) : IEvent
    {
    }
}
