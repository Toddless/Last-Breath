namespace Core.Interfaces
{
    using Godot;

    public interface IDisplayable
    {
        Texture2D? Icon { get; }
        Texture2D? FullImage { get; }
        string Description { get; }
        string DisplayName { get; }

    }
}
