namespace Core.Interfaces.Events
{
    using Godot;

    public record ShowInventorySlotButtonsTooltipEvent(Control Source, string ItemInstanceId) : IEvent { }
}
