namespace LastBreath.Descriptors.Preview
{
    using System;
    using System.Collections.Generic;
    using Core.Crafting;
    using Core.Data;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Items;
    using Core.Modifiers;

    /// <summary>
    /// The domain objects the game's item parser builds, built for a preview. Every one of them is a
    /// Core type: the projects each keep a factory of their own only because each also has an item shape
    /// of its own, and a preview needs none of those.
    /// </summary>
    /// <remarks>The legacy <c>Items</c> catalog is the one exception — its shape lives in the game
    /// project — so a preview never declares that catalog and an id from it is refused out loud rather
    /// than answered with something else.</remarks>
    internal sealed class PreviewItemFactory : IItemGameDataFactory
    {
        private const string NoPlainItemsFormat = "'{0}' is written in the legacy Items catalog, which a preview does not read";

        public IItem CreateItem(string id, Rarity rarity, int maxStackSize, string[] tags) =>
            throw new NotSupportedException(string.Format(NoPlainItemsFormat, id));

        public IEquipItem CreateEquipItem(EquipmentPiece piece, string id, string[] tags) =>
            new EquipItem(piece, id, tags);

        public IWeaponItem CreateWeaponItem(
            WeaponType weaponType, Handedness handedness, float baseDamage, float criticalChance,
            float criticalDamage, string id, string[] tags) =>
            new WeaponItem(weaponType, handedness, baseDamage, criticalChance, criticalDamage, id, tags);

        public IRequirement CreateRequirement(RequirementType type, string id, int amount) =>
            new Requirement(type, id, amount);

        public ICraftingRecipe CreateRecipe(
            string id, string resultItemId, string[] tags, Rarity rarity, List<IRequirement> requirements,
            ItemType itemType, int? unlockAtMastery, int basePrice, string[] optionalResourceCategories) =>
            new CraftingRecipe(id, resultItemId, tags, rarity, requirements, itemType, optionalResourceCategories,
                unlockAtMastery, basePrice);

        public IMaterialCategory CreateMaterialCategory(List<IModifierDescriptor> modifiers, string id) =>
            new Core.Crafting.MaterialCategory(modifiers, id);

        public IUpgradingResource CreateUpgradeResource(
            string id, string[] tags, Rarity rarity, EquipmentCategory? category, int maxStackSize) =>
            new UpgradeResource(id, tags, rarity, category, maxStackSize);

        public IMaterial CreateMaterial(List<IModifierDescriptor> modifiers, IMaterialCategory category) =>
            new MaterialType(modifiers, category);

        public ICraftingResource CreateCraftingResource(
            string id, int maxStackSize, string[] tags, IMaterial material, Rarity rarity) =>
            new CraftingResource(id, maxStackSize, tags, material, rarity);
    }
}
