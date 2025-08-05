namespace Crafting.Source.CraftingResources
{
    using Godot;

    public interface ICraftingResource 
    {
        string Name { get; }
        string Description { get; }
        ResourceType Type { get; }
        ResourceQuality Quality { get; set; }
        int Quantity { get; set; }
        int MaxStackSize { get; }
        Texture2D? Icon { get; }
        Texture2D? FullImage { get; }

        ICraftingResource Copy(bool subresources = false);
    }
}
