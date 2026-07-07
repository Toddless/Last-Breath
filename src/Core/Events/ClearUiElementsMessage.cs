namespace Core.Events
{
    using Godot;

    public record ClearUiElementsMessage(Control Source) : IMessage
    {
    }
}
