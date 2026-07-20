namespace Crafting.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Core.Crafting;
    using Core.Data;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Items;
    using Core.Items.Grants;
    using Core.Modifiers;
    using Core.Services;

    /// <summary>Crafting-side item spawner: copies a cached template and rolls affix descriptors from the used
    /// resources into the fresh instance. The runtime entry point that turns a recipe + resources into a rolled item.</summary>
    public class ItemCreationService(
        ICraftingMastery craftingMastery,
        IRandomNumberGenerator rnd,
        IItemDataProvider itemDataProvider,
        IModifierMaterializer materializer,
        IEquipItemMinter equipMinter,
        ICraftingEffectProvider effectCatalog,
        IGrantFactory grantFactory)
        : IItemCreationService
    {
        public IItem CreateItem(string id, List<string> additionalItemEffects, Rarity rarity, float equipEffectChance, float modifierMultiplier) =>
            throw new NotImplementedException("Crafting module creates items only by recipe.");

        public IItem CreateItemByRecipe(string recipeId, IEnumerable<IModifierDescriptor> descriptors, Rarity? minRarity = null)
        {
            var recipe = itemDataProvider.GetRecipe(recipeId);
            // Creation runes floor the mastery roll BEFORE the affix slot split — the raised
            // rarity buys its full line count, not just a label.
            return recipe.ItemType switch
            {
                ItemType.Equipment => CreateEquip(equipMinter.Mint(recipe.ResultItemId), descriptors.ToList(),
                    craftingMastery.RollRarity().ApplyRarityFloor(minRarity), craftingMastery.GetCurrentValueMultiplier()),
                ItemType.Consumable or ItemType.Quest or ItemType.Crafting => itemDataProvider.CopyItem(recipe.ResultItemId),
                _ => CreateCoal(),
            };
        }

        // Rolls affix descriptors from the union of the result item's generation pools (family + own) and
        // the used resources: mastery rarity gives the slot split, mastery quality scales the value bounds.
        // The quality is stamped as PowerMultiplier — the LIVE reroll pool (recomputed from the same sources
        // on every recraft) rescales by it, so nothing is stored on the item.
        private IEquipItem CreateEquip(IEquipItem item, List<IModifierDescriptor> resourceDescriptors, Rarity rarity, float qualityMultiplier)
        {
            try
            {
                item.Rarity = rarity;
                item.PowerMultiplier = qualityMultiplier;

                var scaled = itemDataProvider.GetGenerationPool(item.Id)
                    .Concat(resourceDescriptors)
                    .Select(descriptor => DescriptorOperations.Scale(descriptor, qualityMultiplier))
                    .ToList();
                (int prefixes, int suffixes) = AffixRules.SlotsFor(rarity, rnd);
                var sink = new CollectingSink();
                foreach (var descriptor in AffixRoller.Roll(scaled, prefixes, suffixes, rnd))
                    materializer.Materialize(descriptor, sink, item.InstanceId);
                foreach (var entity in sink.Entities) item.AddAdditionalModifier(entity);
                foreach (var context in sink.Contexts) item.AddAdditionalContextModifier(context);
                TryRollBonusEffect(item);

                return item;
            }
            catch (ArgumentNullException ex)
            {
                Tracker.TrackException($"Failed to copy base item: {item.Id}", ex, this);
                throw new InvalidOperationException($"Cannot create item: base item {item.Id} not found", ex);
            }
        }

        // Mastery channel 4: a crafted item may roll ONE bonus effect (grant) from the ItemEffects
        // catalog — chance = data base × (1 + mastery bonus); the numeric payload travels with the
        // entry, so the strict skill factories always get their properties.
        private void TryRollBonusEffect(IEquipItem item)
        {
            if (effectCatalog.Effects.Count == 0 || rnd.RandFloat() > craftingMastery.GetExtraEffectChance()) return;

            (var weighted, float totalWeight) = WeightedRandomPicker.CalculateWeights(effectCatalog.Effects);
            var picked = WeightedRandomPicker.PickRandom(weighted, totalWeight, rnd);
            var grant = grantFactory.Create(picked.Kind, picked.Id, [], picked.Properties);
            if (grant != null) item.AddGrant(grant);
        }

        private IItem CreateCoal() => itemDataProvider.CopyItem("Coal");
    }
}
