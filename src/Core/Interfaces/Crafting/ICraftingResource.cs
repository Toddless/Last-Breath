namespace Core.Interfaces.Crafting
{
    using Godot;

    public interface ICraftingResource
    {
        string Id { get; }
        string DisplayName { get; }
        string Description { get; }
        int MaxStackSize { get; }
        float Quality { get; set; }
        string[] Tags { get; }
        IMaterialType? MaterialType { get; }
        Texture2D? Icon { get; set; }
        Texture2D? FullImage { get; set; }


        bool HasTag(string id);
        ICraftingResource Copy(bool subresources = false);
    }
}
