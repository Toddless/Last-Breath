namespace LastBreath.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Core.Crafting;
    using Core.Data;
    using Core.Enums;
    using Core.Items;
    using Core.Modifiers;
    using Core.Services;
    using Godot;
    using LootGeneration.Source;

    public class ItemCreationService(
        IItemEffectProvider effectProvider,
        RandomNumberGenerator rnd,
        IItemDataProvider dataProvider,
        ICraftingMastery craftingMastery) : IItemCreationService
    {
        public IItem CreateItem(string id, List<string> additionalItemEffects, Rarity rarity, float equipEffectChance, float modifierMultiplier)
        {
            var item = dataProvider.CopyItem(id);
            if (item is IEquipItem equipItem) HandleEquipItemGeneration(equipItem, additionalItemEffects, rarity, equipEffectChance, modifierMultiplier);

            return item;
        }

        public IItem CreateItemByRecipe(string recipeId, IEnumerable<IModifier> modifiers)
        {
            var recipe = dataProvider.GetRecipe(recipeId);
            string resultItemId = recipe.ResultItemId;
            switch (recipe.ItemType)
            {
                case ItemType.Equipment:
                    (List<WeightedObject<IModifier>> mods, float totalWeight) = WeightedRandomPicker.CalculateWeights(modifiers);
                    return CreateEquip(resultItemId, mods, totalWeight);
                case ItemType.Consumable or ItemType.Quest or ItemType.Crafting:
                    return dataProvider.CopyItem(resultItemId);
            }

            return CreateCoal();
        }

        private IEquipItem CreateEquip(string itemId, List<WeightedObject<IModifier>> modifiers, float totalWeight)
        {
            try
            {
                var item = (IEquipItem)dataProvider.CopyItem(itemId);
                var itemRarity = craftingMastery.RollRarity();
                item.Rarity = itemRarity;
                int amountModifiers = itemRarity.ConvertRarityToItemModifierAmount();

                HashSet<IModifier> takenMods = WeightedRandomPicker.PickRandomMultipleWithoutDuplicate(modifiers, totalWeight, amountModifiers, rnd);

                List<IModifierInstance> mods = [];
                mods.AddRange(takenMods.Select(mod =>
                    ModifiersCreator.CreateModifierInstance(
                        mod.EntityParameter,
                        mod.ModifierValueType,
                        ApplyPlayerMultiplier(mod.BaseValue, mod.ModifierValueType),
                        item.InstanceId)));

                item.SetModifiers(mods);
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

        private void HandleEquipItemGeneration(IEquipItem equip, List<string> additionalItemEffects, Rarity rarity, float equipEffectChance, float modifierMultiplier)
        {
            if (equip.Rarity is Rarity.Mythic or Rarity.Unique) return;

            equip.Rarity = rarity;

            var modifiersPool = dataProvider.GetEquipItemModifierPool(equip.Id);
            var basePool = dataProvider.GetEquipItemBaseModifierPool(equip.Id);
            // Don't forget to concat item modifiers with modifier from context
            var weighted = WeightedRandomPicker.CalculateWeights(modifiersPool.Concat(basePool));
            var chosenMods = WeightedRandomPicker.PickRandomMultipleWithoutDuplicate(
                weighted.WeightedObjects,
                weighted.TotalWeight,
                rarity.ConvertRarityToItemModifierAmount(),
                rnd);

            equip.SetModifiers(chosenMods.SelectMany(mod => CreateScaledInstances(mod, modifierMultiplier, equip.InstanceId)));
        }

        // Flat/Increase/Multiplicative values all store the bonus delta (Calculations.CalculateModifiers sums
        // each bucket onto 1), so one linear scale is valid for every type. Pool entries are shared between
        // items — scale fresh instances, never the originals.
        private static IEnumerable<IModifier> CreateScaledInstances(IModifier modifier, float multiplier, string instanceId)
        {
            IEnumerable<IModifier> parts = modifier is CompositeModifier composite ? composite.Parts : [modifier];
            return parts.Select(IModifier (part) =>
            {
                var copy = ModifiersCreator.CreateModifierInstance(part.EntityParameter, part.ModifierValueType, part.BaseValue * multiplier, instanceId);
                copy.Scope = part.Scope;
                return copy;
            });
        }

        private float ApplyPlayerMultiplier(float baseValue, ModifierValueType valueType)
        {
            float multiplier = craftingMastery.GetCurrentValueMultiplier();
            if (valueType == ModifierValueType.Multiplicative)
                return 1f + (baseValue - 1f) * multiplier;

            return baseValue * multiplier;
        }

        private IItem CreateCoal() => dataProvider.CopyItem("Coal");
    }
}
