namespace Core.Interfaces.Mediator.Events
{
    using Godot;
    using Core.Interfaces.Items;

    public record ShowInventoryItemEvent(ItemInstance Item, Control Source) : IEvent { }
}
