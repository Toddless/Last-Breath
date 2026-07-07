namespace Core.Events
{
    using Godot;

    public record ShowInventorySlotButtonsTooltipMessage(Control Source, string ItemInstanceId) : IMessage { }
}
