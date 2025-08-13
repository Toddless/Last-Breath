namespace Core.Interfaces.Crafting
{
    using Godot;

    public interface ICraftingResource
    {
        string Id { get; }
        string DisplayName { get; }
        string Description { get; }
        int Quantity { get; set; }
        int MaxStackSize { get; }
        string[] Tags { get; }
        IMaterialType? MaterialType { get; }
        Texture2D? Icon { get; set; }
        Texture2D? FullImage { get; set; }

        ICraftingResource Copy(bool subresources = false);
    }
}
