namespace Core.Interfaces.Events
{
    using Godot;

    public record ClearUiElementsEvent(Control Source) : IEvent
    {
    }
}
