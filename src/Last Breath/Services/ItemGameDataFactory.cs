namespace LastBreath.Services
{
    using Items;
    using Core.Data;
    using Core.Enums;
    using Core.Modifiers;
    using Core.Interfaces;
    using Crafting.Source;
    using Core.Interfaces.Items;
    using Core.Interfaces.Crafting;
    using System.Collections.Generic;

    public sealed class ItemGameDataFactory: IItemGameDataFactory
    {
            public IItem CreateItem(string id, Rarity rarity, int maxStackSize, string[] tags) =>
                new Item(id, rarity, maxStackSize, tags);

            public IEquipItem CreateEquipItem(EquipmentPiece piece, string id, string[] tags) =>
                new EquipItem(piece, id, tags);

            public IModifier CreateModifier(EntityParameter parameter, ModifierType type, float value, float weight) =>
                new Modifier(type, parameter, value, weight);

            public IModifier CreateMaterialModifier(EntityParameter parameter, ModifierType type, float baseValue, float weight) =>
                new MaterialModifier(parameter, type, baseValue, weight);

            public IRequirement CreateRequirement(RequirementType type, string id, int amount) =>
                new Requirement(type, id, amount);

            public ICraftingRecipe CreateRecipe(string id, string resultItemId, string[] tags, Rarity rarity,
                List<IRequirement> requirements, ItemType itemType, bool isOpened, string[] optionalResourceCategories) =>
                new CraftingRecipe(id, resultItemId, tags, rarity, requirements, itemType, optionalResourceCategories, isOpened);

            public IMaterialCategory CreateMaterialCategory(List<IModifier> modifiers, string id) =>
                new  Crafting.Source.MaterialCategory(modifiers, id);

            public IUpgradingResource CreateUpgradeResource(string id, string[] tags, Rarity rarity, EquipmentCategory category, int maxStackSize) =>
                new UpgradeResource(id, tags, rarity, category, maxStackSize);

            public IMaterial CreateMaterial(List<IModifier> modifiers, IMaterialCategory category) =>
                new MaterialType(modifiers, category);

            public ICraftingResource CreateCraftingResource(string id, int maxStackSize, string[] tags, IMaterial material, Rarity rarity) =>
                new CraftingResource(id, maxStackSize, tags, material, rarity);
    }
}
