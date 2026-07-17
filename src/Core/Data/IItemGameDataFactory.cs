namespace Core.Data
{
    using System.Collections.Generic;
    using Crafting;
    using Enums;
    using Interfaces;
    using Items;
    using Modifiers;

    public interface IItemGameDataFactory
    {
        IItem CreateItem(string id, Rarity rarity, int maxStackSize, string[] tags);
        IEquipItem CreateEquipItem(EquipmentPiece piece, string id, string[] tags);
        IWeaponItem CreateWeaponItem(WeaponType weaponType, Handedness handedness, float baseDamage, float criticalChance, float criticalDamage, string id, string[] tags);
        IItemGrant? CreateGrant(GrantKind kind, string id, List<IModifier> modifiers, IReadOnlyDictionary<string, float> properties);
        IRequirement CreateRequirement(RequirementType type, string id, int amount);
        ICraftingRecipe CreateRecipe(string id, string resultItemId, string[] tags, Rarity rarity, List<IRequirement> requirements, ItemType itemType, bool isOpened, string[] optionalResourceCategories);
        IMaterialCategory CreateMaterialCategory(List<IModifierDescriptor> modifiers, string id);
        IUpgradingResource CreateUpgradeResource(string id, string[] tags, Rarity rarity, EquipmentCategory? category, int maxStackSize);
        IMaterial CreateMaterial(List<IModifierDescriptor> modifiers, IMaterialCategory category);
        ICraftingResource CreateCraftingResource(string id, int maxStackSize, string[] tags, IMaterial material, Rarity rarity);
    }
}
