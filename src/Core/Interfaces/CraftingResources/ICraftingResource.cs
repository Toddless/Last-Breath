namespace Core.Interfaces.CraftingResources
{
    using Godot;

    public interface ICraftingResource 
    {
        string Name { get; }
        string Description { get; }
        int Quantity { get; set; }
        int MaxStackSize { get; }
        IResourceMaterialType? MaterialType { get; }
        Texture2D? Icon { get; set; }
        Texture2D? FullImage { get; set; }

        ICraftingResource Copy(bool subresources = false);
    }
}
