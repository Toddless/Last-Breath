namespace Core.Data.CraftingData
{
    using Enums;
    using Tooling.Schema;

    /// <summary>Category is optional: dusts and sharpening runes serve one equipment category, fluxes and
    /// creation runes serve any and simply omit the field.</summary>
    public record UpgradeResourceData(
        string Id,
        string[] Tags,
        [property: EnumOf(typeof(Rarity))] string Rarity,
        [property: EnumOf(typeof(EquipmentCategory))] string? Category,
        int MaxStackSize,
        int BasePrice = 0);
}
