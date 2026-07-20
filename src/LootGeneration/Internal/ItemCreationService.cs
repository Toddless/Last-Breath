namespace LootGeneration.Internal
{
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

    /// <summary>Loot-side item spawner: mints from <see cref="IItemMinter"/> and, for equip items, rolls
    /// prefix/suffix lines from the item+family descriptor pools scaled by the kill's difficulty multiplier.
    /// The runtime entry point <see cref="Source.LootGenerationService"/> calls to turn a rolled table id
    /// into a concrete drop.</summary>
    public class ItemCreationService(IItemDataProvider dataProvider, IRandomNumberGenerator rnd, IItemMinter itemMinter, IModifierMaterializer materializer, ICraftingEffectProvider effectCatalog, IGrantFactory grantFactory) : IItemCreationService
    {
        public IItem CreateItem(string id)
        {
            // The facade mints equips (rolling their authored ranges) and copies plain resources.
            return itemMinter.MintItem(id);
        }

        public IItem CreateItem(string id, List<string> additionalItemEffects, Rarity rarity, float equipEffectChance, float modifierMultiplier)
        {
            var item = CreateItem(id);
            if (item is IEquipItem equipItem) HandleEquipItemGeneration(equipItem, additionalItemEffects, rarity, equipEffectChance, modifierMultiplier);

            return item;
        }

        public IItem CreateItemByRecipe(string recipeId, IEnumerable<IModifierDescriptor> descriptors, Core.Enums.Rarity? minRarity = null) => throw new System.NotImplementedException();

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
            TryRollGrant(equip, additionalItemEffects, equipEffectChance);
        }

        // The drop's bonus grant. Pool = the ItemEffects catalog (payload travels with the entry).
        // A kill with ItemEffectsModifier(s) narrows the pool to the NPC's signature ids and the grant
        // becomes GUARANTEED per equip item — that is the modifier's identity, difficulty already paid
        // for it. Ordinary kills roll the configured chance against the whole catalog. Rolled AFTER
        // the affix lines, so line sequences of a seeded run stay comparable.
        private void TryRollGrant(IEquipItem equip, List<string> additionalItemEffects, float equipEffectChance)
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
    }
}
