namespace Core.Interfaces.UI
{
    using Godot;

    public interface IInitializable
    {
        static abstract PackedScene Initialize();
    }
}
