namespace Core.Interfaces.Mediator.Events
{
    using Godot;

    public record ClearUiElementsEvent(Control Source) : IEvent
    {
    }
}
