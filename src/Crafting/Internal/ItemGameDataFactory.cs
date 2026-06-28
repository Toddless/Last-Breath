namespace Crafting.Internal
{
    using System;
    using System.Collections.Generic;
    using Core.Data;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Items;
    using Core.Modifiers;
    using Source;

    internal sealed class ItemGameDataFactory : IItemGameDataFactory
    {
        public IItem CreateItem(string id, Rarity rarity, int maxStackSize, string[] tags) =>
            throw new NotImplementedException("Crafting module does not use basic items.");

        public IEquipItem CreateEquipItem(EquipmentPiece piece, string id, string[] tags) =>
            new TestEquipItem(piece, id, tags);

        public IModifier CreateModifier(EntityParameter parameter, ModifierValueType valueType, float value, float weight) =>
            new Modifier(valueType, parameter, value, weight);

        public IModifier CreateMaterialModifier(EntityParameter parameter, ModifierValueType valueType, float baseValue, float weight) =>
            new MaterialModifier(parameter, valueType, baseValue, weight);

        public IRequirement CreateRequirement(RequirementType type, string id, int amount) =>
            new Requirement(type, id, amount);

        public ICraftingRecipe CreateRecipe(string id, string resultItemId, string[] tags, Rarity rarity,
            List<IRequirement> requirements, ItemType itemType, bool isOpened, string[] optionalResourceCategories) =>
            new CraftingRecipe(id, resultItemId, tags, rarity, requirements, itemType, optionalResourceCategories, isOpened);

        public IMaterialCategory CreateMaterialCategory(List<IModifier> modifiers, string id) =>
            new Source.MaterialCategory(modifiers, id);

        public IUpgradingResource CreateUpgradeResource(string id, string[] tags, Rarity rarity, EquipmentCategory category, int maxStackSize) =>
            new UpgradeResource(id, tags, rarity, category, maxStackSize);

        public IMaterial CreateMaterial(List<IModifier> modifiers, IMaterialCategory category) =>
            new MaterialType(modifiers, category);

        public ICraftingResource CreateCraftingResource(string id, int maxStackSize, string[] tags, IMaterial material, Rarity rarity) =>
            new CraftingResource(id, maxStackSize, tags, material, rarity);
    }
}
