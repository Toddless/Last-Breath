namespace Crafting.Services
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

    /// <summary>Crafting-side item spawner: copies a cached template and rolls affix descriptors from the used
    /// resources into the fresh instance. The runtime entry point that turns a recipe + resources into a rolled item.</summary>
    public class ItemCreationService(ICraftingMastery craftingMastery, RandomNumberGenerator rnd, IItemDataProvider itemDataProvider, IModifierMaterializer materializer)
        : IItemCreationService
    {
        public IItem CreateItem(string id, List<string> additionalItemEffects, Rarity rarity, float equipEffectChance, float modifierMultiplier) =>
            throw new NotImplementedException("Crafting module creates items only by recipe.");

        public IItem CreateItemByRecipe(string recipeId, IEnumerable<IModifierDescriptor> descriptors)
        {
            var recipe = itemDataProvider.GetRecipe(recipeId);
            return recipe.ItemType switch
            {
                ItemType.Equipment => CreateEquip((IEquipItem)itemDataProvider.CopyItem(recipe.ResultItemId), descriptors.ToList(),
                    craftingMastery.RollRarity(), craftingMastery.GetCurrentValueMultiplier()),
                ItemType.Consumable or ItemType.Quest or ItemType.Crafting => itemDataProvider.CopyItem(recipe.ResultItemId),
                _ => CreateCoal(),
            };
        }

        // Rolls affix descriptors from the used-resource pool onto the item, scaled by crafting quality. The full
        // (scaled, flattened) pool is saved for reroll so the item's magnitude and its reroll fodder stay in sync.
        // Quality scaling is crafting-only — loot drops never build a pool and never see this multiplier.
        private IEquipItem CreateEquip(IEquipItem item, List<IModifierDescriptor> pool, Rarity rarity, float qualityMultiplier)
        {
            try
            {
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
                Tracker.TrackException($"Failed to copy base item: {item.Id}", ex, this);
                throw new InvalidOperationException($"Cannot create item: base item {item.Id} not found", ex);
            }
        }

        private IItem CreateCoal() => itemDataProvider.CopyItem("Coal");
    }
}
