namespace Core.Interfaces.Events
{
    using Core.Interfaces.Items;
    using Godot;

    public record ShowInventoryItemEvent(ItemInstance Item, Control Source) : IEvent { }
}
