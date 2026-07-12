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
        ICraftingMastery craftingMastery,
        IModifierMaterializer materializer) : IItemCreationService
    {
        public IItem CreateItem(string id, List<string> additionalItemEffects, Rarity rarity, float equipEffectChance, float modifierMultiplier)
        {
            var item = dataProvider.CopyItem(id);
            if (item is IEquipItem equipItem) HandleEquipItemGeneration(equipItem, additionalItemEffects, rarity, equipEffectChance, modifierMultiplier);

            return item;
        }

        public IItem CreateItemByRecipe(string recipeId, IEnumerable<IModifierDescriptor> descriptors)
        {
            var recipe = dataProvider.GetRecipe(recipeId);
            return recipe.ItemType switch
            {
                ItemType.Equipment => CreateEquip(recipe.ResultItemId, descriptors.ToList(), craftingMastery.RollRarity(), craftingMastery.GetCurrentValueMultiplier()),
                ItemType.Consumable or ItemType.Quest or ItemType.Crafting => dataProvider.CopyItem(recipe.ResultItemId),
                _ => CreateCoal(),
            };
        }

        // Rolls affix descriptors from the used-resource pool onto the item, scaled by crafting quality. The full
        // (scaled, flattened) pool is saved for reroll so the item's magnitude and its reroll fodder stay in sync.
        // Quality scaling is crafting-only — loot drops (HandleEquipItemGeneration) never build a pool and never see it.
        private IEquipItem CreateEquip(string itemId, List<IModifierDescriptor> pool, Rarity rarity, float qualityMultiplier)
        {
            try
            {
                var item = (IEquipItem)dataProvider.CopyItem(itemId);
                item.Rarity = rarity;
                int amount = rarity.ConvertRarityToItemModifierAmount();

                (var weighted, float totalWeight) = WeightedRandomPicker.CalculateWeights(pool);
                var selected = WeightedRandomPicker.PickRandomMultipleWithoutDuplicate(weighted, totalWeight, amount, rnd);

                var sink = new CollectingSink();
                foreach (var descriptor in selected)
                    materializer.Materialize(DescriptorOperations.Scale(descriptor, qualityMultiplier), sink, item.InstanceId);
                foreach (var entity in sink.Entities) item.AddAdditionalModifier(entity);
                foreach (var context in sink.Contexts) item.AddAdditionalContextModifier(context);

                item.SaveModifiersPool(DescriptorOperations.Flatten(pool.Select(descriptor => DescriptorOperations.Scale(descriptor, qualityMultiplier))));
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

        // TODO: duplicated verbatim in LootGeneration/Internal/ItemCreationService — consolidate loot modifier scaling.
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

        private IItem CreateCoal() => dataProvider.CopyItem("Coal");
    }
}
