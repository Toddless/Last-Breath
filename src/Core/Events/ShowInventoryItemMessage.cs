namespace Core.Events
{
    using Godot;
    using Items;

    public record ShowInventoryItemMessage(ItemInstance Item, Control Source) : IMessage { }
}
