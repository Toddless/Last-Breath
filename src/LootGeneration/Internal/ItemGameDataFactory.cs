namespace LootGeneration.Internal
{
    using temp;
    using System;
    using Core.Data;
    using Core.Enums;
    using Core.Modifiers;
    using Core.Interfaces;
    using Core.Interfaces.Items;
    using Core.Interfaces.Crafting;
    using System.Collections.Generic;

    public class ItemGameDataFactory: IItemGameDataFactory
    {
            public IItem CreateItem(string id, Rarity rarity, int maxStackSize, string[] tags) =>
                throw new NotImplementedException("LootGeneration does not use basic items.");

            public IEquipItem CreateEquipItem(EquipmentPiece piece, string id, string[] tags) =>
                new ExampleEquipItem(piece, id, tags);

            public IModifier CreateModifier(EntityParameter parameter, ModifierValueType valueType, float value, float weight) =>
                new Modifier(valueType, parameter, value, weight);

            public IModifier CreateMaterialModifier(EntityParameter parameter, ModifierValueType valueType, float baseValue, float weight) =>
                new ExampleMaterialModifier(parameter, valueType, baseValue, weight);

            public IRequirement CreateRequirement(RequirementType type, string id, int amount) =>
                new ExampleRequirement(type, id, amount);

            public ICraftingRecipe CreateRecipe(string id, string resultItemId, string[] tags, Rarity rarity,
                List<IRequirement> requirements, ItemType itemType, bool isOpened, string[] optionalResourceCategories) =>
                new ExampleCraftingRecipe(id, resultItemId, tags, rarity, requirements, itemType, optionalResourceCategories, isOpened);

            public IMaterialCategory CreateMaterialCategory(List<IModifier> modifiers, string id) =>
                new ExampleMaterialCategory(modifiers, id);

            public IUpgradingResource CreateUpgradeResource(string id, string[] tags, Rarity rarity, EquipmentCategory category, int maxStackSize) =>
                new ExampleUpgradeResource(id, tags, rarity, category, maxStackSize);

            public IMaterial CreateMaterial(List<IModifier> modifiers, IMaterialCategory category) =>
                new ExampleMaterialType(modifiers, category);

            public ICraftingResource CreateCraftingResource(string id, int maxStackSize, string[] tags, IMaterial material, Rarity rarity) =>
                new ExampleCraftingResource(id, maxStackSize, tags, material, rarity);
    }
}
