namespace Crafting.Services
{
    using Godot;
    using System;
    using Core.Data;
    using Utilities;
    using Core.Enums;
    using System.Linq;
    using Core.Modifiers;
    using Core.Interfaces;
    using Core.Interfaces.Items;
    using Core.Interfaces.Crafting;
    using System.Collections.Generic;

    public class ItemCreationService(ICraftingMastery craftingMastery, RandomNumberGenerator rnd, IItemDataProvider itemDataProvider)
        : IItemCreationService
    {
        public IItem CreateItem(string id, List<string> additionalItemEffects, Rarity rarity, float equipEffectChance)
        {
            return CreateCoal();
        }

        public IItem CreateItemByRecipe(string recipeId, IEnumerable<IModifier> resources)
        {
            var recipe = itemDataProvider.GetRecipe(recipeId);
            string resultItemId = recipe.ResultItemId;
            switch (recipe.ItemType)
            {
                case ItemType.Equipment:
                    (List<WeightedObject<IModifier>> mods, float totalWeight) = WeightedRandomPicker.CalculateWeights(resources);
                    return CreateEquip(resultItemId, mods, totalWeight);
                case ItemType.Consumable or ItemType.Quest or ItemType.Crafting:
                    return CreateItem(resultItemId);
            }

            return CreateCoal();
        }

        private IItem CreateItem(string recipeId)
        {
            return CreateCoal();
        }

        private IEquipItem CreateEquip(string itemId, List<WeightedObject<IModifier>> modifiers, float totalWeight)
        {
            try
            {
                var item = (IEquipItem)itemDataProvider.CopyItem(itemId);
                var itemRarity = craftingMastery.RollRarity();
                item.Rarity = itemRarity;
                int amountModifiers = itemRarity.ConvertRarityToItemModifierAmount();

                HashSet<IModifier> takenMods = WeightedRandomPicker.PickRandomMultipleWithoutDuplicate(modifiers, totalWeight, amountModifiers, rnd);

                List<IModifierInstance> mods = [];
                mods.AddRange(takenMods.Select(mod =>
                    ModifiersCreator.CreateModifierInstance(
                        mod.EntityParameter,
                        mod.ModifierType,
                        ApplyPlayerMultiplier(mod.BaseValue, mod.ModifierType),
                        item)));

                item.SetAdditionalModifiers(mods);
                item.SaveModifiersPool(modifiers.Select(x => x.Obj));

                // TODO : Change to get random effect/ability
                return item;
            }
            catch (ArgumentNullException ex)
            {
                Tracker.TrackException($"Failed to copy base item: {itemId}", ex, this);
                throw new InvalidOperationException($"Cannot create item: base item {itemId} not found", ex);
            }
        }

        private float ApplyPlayerMultiplier(float baseValue, ModifierType type)
        {
            float multiplier = craftingMastery.GetCurrentValueMultiplier();
            if (type == ModifierType.Multiplicative)
                return 1f + (baseValue - 1f) * multiplier;

            return baseValue * multiplier;
        }

        private IItem CreateCoal() => itemDataProvider.CopyItem("Coal");
    }
}
