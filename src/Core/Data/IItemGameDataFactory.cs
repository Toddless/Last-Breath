namespace Core.Data
{
    using System.Collections.Generic;
    using Enums;
    using Interfaces;
    using Interfaces.Crafting;
    using Interfaces.Items;
    using Modifiers;

    public interface IItemGameDataFactory
    {
        IItem CreateItem(string id, Rarity rarity, int maxStackSize, string[] tags);
        IEquipItem CreateEquipItem(EquipmentPiece piece, string id, string[] tags);
        IWeaponItem CreateWeaponItem(WeaponType weaponType, Handedness handedness, float baseDamage, float criticalChance, float criticalDamage, string id, string[] tags);
        IItemGrant? CreateGrant(GrantKind kind, string id, List<IModifier> modifiers);
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
