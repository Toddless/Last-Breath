namespace Core.Interfaces.Items
{
    using Core.Enums;
    using Godot;

    public interface IItem
    {
        string Id { get; }
        string InstanceId { get; }
        int MaxStackSize { get; }
        Texture2D? Icon { get; }
        Texture2D? FullImage {  get; }
        Rarity Rarity { get; }
        string Description { get; }
        string DisplayName { get; }
        bool HasTag(string tag);
        T Copy<T>(bool subresources = false);
    }
}
