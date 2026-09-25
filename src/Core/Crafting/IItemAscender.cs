namespace Core.Crafting
{
    using System.Collections.Generic;
    using Enums;
    using Interfaces;
    using Modifiers;
    using Results;
    using Items;

    public interface IItemAscender
    {
        bool CanAscend(IEquipItem item);
        AscensionResult TryAscendItem(IEquipItem item);

        /// <summary>Ascension price from the UpgradeCosts catalog (only legendaries ascend — no rarity scaling).</summary>
        List<IRequirement> GetAscendResourceCost(EquipmentCategory itemCategory);

        /// <summary>The mythic gift pool a category's mark rolls from — the UI's "possible gifts" list.</summary>
        IReadOnlyList<IModifierDescriptor> GetGiftPool(EquipmentCategory itemCategory);
    }
}
