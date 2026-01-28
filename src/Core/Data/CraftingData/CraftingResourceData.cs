namespace Core.Data.CraftingData
{
    public record CraftingResourceData(
        string Id,
        MaterialData Material,
        int MaxStackSize,
        string[] Tags,
        string Rarity);
}
