namespace Core.Data
{
    using Enums;
    using Modifiers;
    using Interfaces;
    using Interfaces.Items;
    using Interfaces.Crafting;
    using System.Collections.Generic;

    public interface IItemGameDataFactory
    {
        IItem CreateItem(string id, Rarity rarity, int maxStackSize, string[] tags);
        IEquipItem CreateEquipItem(EquipmentPiece piece, string id, string[] tags);
        IModifier CreateModifier(EntityParameter parameter, ModifierValueType valueType, float value, float weight);
        IModifier CreateMaterialModifier(EntityParameter parameter, ModifierValueType valueType, float baseValue, float weight);
        IRequirement CreateRequirement(RequirementType type, string id, int amount);
        ICraftingRecipe CreateRecipe(string id, string resultItemId, string[] tags, Rarity rarity, List<IRequirement> requirements, ItemType itemType, bool isOpened, string[] optionalResourceCategories);
        IMaterialCategory CreateMaterialCategory(List<IModifier> modifiers, string id);
        IUpgradingResource CreateUpgradeResource(string id, string[] tags, Rarity rarity, EquipmentCategory category, int maxStackSize);
        IMaterial CreateMaterial(List<IModifier> modifiers, IMaterialCategory category);
        ICraftingResource CreateCraftingResource(string id, int maxStackSize, string[] tags, IMaterial material, Rarity rarity);
    }
}
