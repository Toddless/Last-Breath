namespace Core.Interfaces.Events
{
    using Items;
    using Godot;

    public record ShowInventoryItemMessage(ItemInstance Item, Control Source) : IMessage { }
}
