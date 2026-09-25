namespace Core.Crafting
{
    using Enums;

    public interface IUpgradingResource : IResource
    {
        /// <summary>Null for category-agnostic consumables (fluxes, creation runes) — dusts and
        /// sharpening runes serve exactly one equipment category.</summary>
        EquipmentCategory? Category { get; }
    }
}
