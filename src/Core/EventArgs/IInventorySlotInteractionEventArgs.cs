namespace Core.EventArgs
{
    using Core.Enums;
    using Core.Interfaces.Items;
    using Core.Interfaces.Inventory;

    public interface IInventorySlotInteractionEventArgs
    {
        MouseInteractions Interaction { get; }
        IItem Item { get; }
        IInventorySlot Slot { get; }
    }
}
