namespace Contracts.Interfaces
{
    using Godot;

    public interface IItem
    {
        string Id { get; }
        int Quantity { get; }
        int MaxStackSize { get; }
        Texture2D? Icon { get; }
        Texture2D? FullImage {  get; }
    }
}
