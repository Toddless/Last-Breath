namespace Core.Data.CraftingData
{
    using Enums;
    using Tooling.Schema;

    public record CraftingResourceData(
        string Id,
        MaterialData Material,
        int MaxStackSize,
        string[] Tags,
        [property: EnumOf(typeof(Rarity))] string Rarity,
        int BasePrice = 0);
}
