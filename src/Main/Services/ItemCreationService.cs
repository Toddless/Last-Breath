namespace LastBreath.Services
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

    public class ItemCreationService(
        IRandomNumberGenerator rnd,
        IItemDataProvider dataProvider,
        ICraftingMastery craftingMastery,
        IModifierMaterializer materializer,
        IItemMinter itemMinter,
        IEquipItemMinter equipMinter,
        ICraftingEffectProvider effectCatalog,
        IGrantFactory grantFactory) : IItemCreationService
    {
        public IItem CreateItem(string id, List<string> additionalItemEffects, Rarity rarity, float equipEffectChance, float modifierMultiplier, Rarity? fixedRarity = null)
        {
            // The facade mints equips (rolling their authored ranges) and copies plain resources.
            var item = itemMinter.MintItem(id, fixedRarity);
            if (item is IEquipItem equipItem) HandleEquipItemGeneration(equipItem, additionalItemEffects, rarity, equipEffectChance, modifierMultiplier);

            return item;
        }

        public IItem CreateItemByRecipe(string recipeId, IEnumerable<IModifierDescriptor> descriptors, Rarity? minRarity = null)
        {
            var recipe = dataProvider.GetRecipe(recipeId);
            // Creation runes floor the mastery roll BEFORE the affix slot split — the raised
            // rarity buys its full line count, not just a label.
            return recipe.ItemType switch
            {
                ItemType.Equipment => CreateEquip(recipe.ResultItemId, descriptors.ToList(),
                    craftingMastery.RollRarity().ApplyRarityFloor(minRarity), craftingMastery.GetCurrentValueMultiplier()),
                ItemType.Consumable or ItemType.Quest or ItemType.Crafting => dataProvider.CopyItem(recipe.ResultItemId),
                _ => CreateCoal(),
            };
        }

        // Rolls affix descriptors from the union of the result item's generation pools (family + own) and
        // the used resources: mastery rarity gives the slot split, mastery quality scales the value bounds.
        // The quality is stamped as PowerMultiplier — the LIVE reroll pool (recomputed from the same sources
        // on every recraft) rescales by it, so nothing is stored on the item.
        private IEquipItem CreateEquip(string itemId, List<IModifierDescriptor> resourceDescriptors, Rarity rarity, float qualityMultiplier)
        {
            try
            {
                var item = equipMinter.Mint(itemId);
                item.Rarity = rarity;
                item.PowerMultiplier = qualityMultiplier;

                var scaled = dataProvider.GetGenerationPool(item.Id)
                    .Concat(resourceDescriptors)
                    .Select(descriptor => DescriptorOperations.Scale(descriptor, qualityMultiplier))
                    .ToList();
                (int prefixes, int suffixes) = AffixRules.SlotsFor(rarity, rnd);
                var sink = new CollectingSink();
                foreach (var descriptor in AffixRoller.Roll(scaled, prefixes, suffixes, rnd))
                    materializer.Materialize(descriptor, sink, item.InstanceId);
                foreach (var entity in sink.Entities) item.AddAdditionalModifier(entity);
                foreach (var context in sink.Contexts) item.AddAdditionalContextModifier(context);
                // A pool may also hold a rolled grant (behaviour, not a line) — it lands in the item's effect list.
                foreach (var grant in sink.Grants) item.AddGrant(grant);
                TryRollBonusEffect(item);

                return item;
            }
            catch (ArgumentNullException ex)
            {
                Tracker.TrackException($"Failed to copy base item: {itemId}", ex, this);
                throw new InvalidOperationException($"Cannot create item: base item {itemId} not found", ex);
            }
        }

        // Mint already happened: the drop only rolls its affix lines here. Rarity gives the slot split,
        // the union of item + family pools gives the candidates, and the difficulty multiplier scales the
        // value bounds linearly (flat/inc/multi all store the bonus delta). The multiplier is stamped as
        // PowerMultiplier so the LIVE reroll pool (same union, recomputed at recraft time) rescales to
        // the drop's magnitude — loot is rerollable like any equip.
        private void HandleEquipItemGeneration(IEquipItem equip, List<string> additionalItemEffects, Rarity rarity, float equipEffectChance, float modifierMultiplier)
        {
            if (equip.Rarity is Rarity.Mythic or Rarity.Unique) return;

            equip.Rarity = rarity;
            equip.PowerMultiplier = modifierMultiplier;

            var pool = dataProvider.GetGenerationPool(equip.Id)
                .Select(descriptor => DescriptorOperations.Scale(descriptor, modifierMultiplier))
                .ToList();

            (int prefixes, int suffixes) = AffixRules.SlotsFor(rarity, rnd);
            var sink = new CollectingSink();
            foreach (var descriptor in AffixRoller.Roll(pool, prefixes, suffixes, rnd))
                materializer.Materialize(descriptor, sink, equip.InstanceId);

            foreach (var entity in sink.Entities) equip.AddAdditionalModifier(entity);
            foreach (var context in sink.Contexts) equip.AddAdditionalContextModifier(context);
            // A pool may also hold a rolled grant (behaviour, not a line) — it lands in the item's effect list.
            foreach (var grant in sink.Grants) equip.AddGrant(grant);
            TryRollLootGrant(equip, additionalItemEffects, equipEffectChance);
        }

        // The drop's bonus grant (loot channel; the crafting channel is TryRollBonusEffect below).
        // Pool = the ItemEffects catalog (payload travels with the entry). A kill with
        // ItemEffectsModifier(s) narrows the pool to the NPC's signature ids and the grant becomes
        // GUARANTEED per equip item — that is the modifier's identity, difficulty already paid for it.
        // Ordinary kills roll the configured chance against the whole catalog. Rolled AFTER the affix
        // lines, so line sequences of a seeded run stay comparable.
        private void TryRollLootGrant(IEquipItem equip, List<string> additionalItemEffects, float equipEffectChance)
        {
            if (effectCatalog.Effects.Count == 0) return;

            List<CraftingEffectOption> pool;
            if (additionalItemEffects.Count > 0)
            {
                pool = effectCatalog.Effects.Where(option => additionalItemEffects.Contains(option.Id)).ToList();
                foreach (string unknown in additionalItemEffects.Distinct().Where(id => pool.All(option => option.Id != id)))
                    Tracker.TrackNotFound($"ItemEffects catalog entry '{unknown}' (AdditionalItemEffects)", this);
                if (pool.Count == 0) return;
            }
            else
            {
                if (equipEffectChance <= 0f || rnd.RandFloat() > equipEffectChance) return;
                pool = effectCatalog.Effects.ToList();
            }

            (var weighted, float totalWeight) = WeightedRandomPicker.CalculateWeights(pool);
            var picked = WeightedRandomPicker.PickRandom(weighted, totalWeight, rnd);
            var grant = grantFactory.Create(picked.Kind, picked.Id, [], picked.Properties);
            if (grant != null) equip.AddGrant(grant);
        }

        // Mastery channel 4: a crafted item may roll ONE bonus effect (grant) from the ItemEffects
        // catalog — chance = data base × (1 + mastery bonus); the numeric payload travels with the
        // entry, so the strict skill factories always get their properties. Crafting only: loot
        // effects are a separate (not yet wired) pipeline via IItemEffectProvider.
        private void TryRollBonusEffect(IEquipItem item)
        {
            if (effectCatalog.Effects.Count == 0 || rnd.RandFloat() > craftingMastery.GetExtraEffectChance()) return;

            (var weighted, float totalWeight) = WeightedRandomPicker.CalculateWeights(effectCatalog.Effects);
            var picked = WeightedRandomPicker.PickRandom(weighted, totalWeight, rnd);
            var grant = grantFactory.Create(picked.Kind, picked.Id, [], picked.Properties);
            if (grant != null) item.AddGrant(grant);
        }

        private IItem CreateCoal() => dataProvider.CopyItem("Coal");
    }
}
