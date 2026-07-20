namespace LastBreath.Services
{
    using System.Collections.Generic;
    using Core.Crafting;
    using Core.Data;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Items;
    using Core.Modifiers;
    using Items;

    public sealed class ItemGameDataFactory : IItemGameDataFactory
    {
        public IItem CreateItem(string id, Rarity rarity, int maxStackSize, string[] tags) =>
            new Item(id, rarity, maxStackSize, tags);

        public IEquipItem CreateEquipItem(EquipmentPiece piece, string id, string[] tags) =>
            new EquipItem(piece, id, tags);

        public IWeaponItem CreateWeaponItem(WeaponType weaponType, Handedness handedness, float baseDamage, float criticalChance, float criticalDamage, string id, string[] tags) =>
            new WeaponItem(weaponType, handedness, baseDamage, criticalChance, criticalDamage, id, tags);

        public IRequirement CreateRequirement(RequirementType type, string id, int amount) =>
            new Requirement(type, id, amount);

        public ICraftingRecipe CreateRecipe(string id, string resultItemId, string[] tags, Rarity rarity,
            List<IRequirement> requirements, ItemType itemType, bool isOpened, string[] optionalResourceCategories) =>
            new CraftingRecipe(id, resultItemId, tags, rarity, requirements, itemType, optionalResourceCategories, isOpened);

        public IMaterialCategory CreateMaterialCategory(List<IModifierDescriptor> modifiers, string id) =>
            new Core.Crafting.MaterialCategory(modifiers, id);

        public IUpgradingResource CreateUpgradeResource(string id, string[] tags, Rarity rarity, EquipmentCategory? category, int maxStackSize) =>
            new UpgradeResource(id, tags, rarity, category, maxStackSize);

        public IMaterial CreateMaterial(List<IModifierDescriptor> modifiers, IMaterialCategory category) =>
            new MaterialType(modifiers, category);

        public ICraftingResource CreateCraftingResource(string id, int maxStackSize, string[] tags, IMaterial material, Rarity rarity) =>
            new CraftingResource(id, maxStackSize, tags, material, rarity);
    }
}
