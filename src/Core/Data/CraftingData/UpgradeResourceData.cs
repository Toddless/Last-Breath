namespace Core.Data.CraftingData
{
    public record UpgradeResourceData(
        string Id,
        string[] Tags,
        string Rarity,
        string Category,
        int MaxStackSize);
}
