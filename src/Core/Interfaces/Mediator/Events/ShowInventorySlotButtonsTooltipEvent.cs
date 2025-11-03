namespace Core.Interfaces.Mediator.Events
{
    using Godot;

    public record ShowInventorySlotButtonsTooltipEvent(Control Source, string ItemInstanceId) : IEvent { }
}
