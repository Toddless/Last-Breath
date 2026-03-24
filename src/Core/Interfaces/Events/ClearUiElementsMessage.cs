namespace Core.Interfaces.Events
{
    using Godot;

    public record ClearUiElementsMessage(Control Source) : IMessage
    {
    }
}
