namespace Core.Events
{
    public record InventoryFullMessage(string ItemId, string InstanceId, int Amount, int MaxStack) : IMessage
    {
    }
}
