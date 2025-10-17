namespace Core.Interfaces
{
    using Godot;

    public interface IDisplayable
    {
        Texture2D? Icon { get; }
        string Description { get; }
        string DisplayName { get; }
    }
}
