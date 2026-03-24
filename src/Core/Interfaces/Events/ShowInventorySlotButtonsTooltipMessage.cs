namespace Core.Interfaces.Events
{
    using Godot;

    public record ShowInventorySlotButtonsTooltipMessage(Control Source, string ItemInstanceId) : IMessage { }
}
