namespace Core.Interfaces.Events
{
    using Items;
    using Godot;

    public record ShowInventoryItemEvent(ItemInstance Item, Control Source) : IEvent { }
}
