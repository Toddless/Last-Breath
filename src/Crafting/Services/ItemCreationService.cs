namespace Crafting.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Data;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Items;
    using Core.Modifiers;
    using Godot;
    using Utilities;

    public class ItemCreationService(ICraftingMastery craftingMastery, RandomNumberGenerator rnd, IItemDataProvider itemDataProvider)
        : IItemCreationService
    {
        public IItem CreateItem(string id, List<string> additionalItemEffects, Rarity rarity, float equipEffectChance, float modifierMultiplier)
        {
            var item = itemDataProvider.CopyItem(id);
            switch (true)
            {
                //case var _ when item is IEquipItem equipItem:

                    //return CreateEquip(equipItem);
                default: return CreateItem(item);
            }
        }

        public IItem CreateItemByRecipe(string recipeId, IEnumerable<IModifier> modifiers)
        {
            var recipe = itemDataProvider.GetRecipe(recipeId);
            string resultItemId = recipe.ResultItemId;
            switch (recipe.ItemType)
            {
                case ItemType.Equipment:
                    (List<WeightedObject<IModifier>> mods, float totalWeight) = WeightedRandomPicker.CalculateWeights(modifiers);
                    return CreateEquip((IEquipItem)itemDataProvider.CopyItem(resultItemId), mods, [], totalWeight, craftingMastery.RollRarity(),
                        craftingMastery.GetCurrentValueMultiplier());
                case ItemType.Consumable or ItemType.Quest or ItemType.Crafting:
                    return CreateItem(itemDataProvider.CopyItem(resultItemId));
            }

            return CreateCoal();
        }

        private IItem CreateItem(IItem item)
        {
            return CreateCoal();
        }

        private IEquipItem CreateEquip(IEquipItem item, List<WeightedObject<IModifier>> modifiers, List<string> itemEffects, float totalWeight, Rarity rarity,
            float modifierMultiplier)
        {
            try
            {
                item.Rarity = rarity;
                int amountModifiers = rarity.ConvertRarityToItemModifierAmount();

                HashSet<IModifier> takenMods = WeightedRandomPicker.PickRandomMultipleWithoutDuplicate(modifiers, totalWeight, amountModifiers, rnd);

                List<IModifierInstance> mods = [];

                mods.AddRange(takenMods.Select(mod =>
                    ModifiersCreator.CreateModifierInstance(
                        mod.EntityParameter,
                        mod.ModifierValueType,
                        mod.BaseValue * modifierMultiplier,
                        item.InstanceId)));

                item.SetModifiers(mods);
                item.SaveModifiersPool(modifiers.Select(x => x.Obj));

                // TODO : Change to get random effect/ability
                return item;
            }
            catch (ArgumentNullException ex)
            {
                Tracker.TrackException($"Failed to copy base item: {item.Id}", ex, this);
                throw new InvalidOperationException($"Cannot create item: base item {item.Id} not found", ex);
            }
        }

        private IItem CreateCoal() => itemDataProvider.CopyItem("Coal");
    }
}
