namespace LastBreath.Services
{
    using Godot;
    using System;
    using Utilities;
    using Core.Data;
    using Core.Enums;
    using System.Linq;
    using Core.Modifiers;
    using Core.Interfaces;
    using Core.Interfaces.Items;
    using LootGeneration.Source;
    using Core.Interfaces.Crafting;
    using System.Collections.Generic;

    public class ItemCreationService(
        IItemEffectProvider effectProvider,
        RandomNumberGenerator rnd,
        IItemDataProvider dataProvider,
        ICraftingMastery craftingMastery) : IItemCreationService
    {
        public IItem CreateItem(string id, List<string> additionalItemEffects, Rarity rarity, float equipEffectChance)
        {
            var item = dataProvider.CopyItem(id);
            if (item is IEquipItem equipItem) HandleEquipItemGeneration(equipItem, additionalItemEffects, rarity, equipEffectChance);

            return item;
        }

        public IItem CreateItem(string id, List<string> additionalItemEffects, Rarity rarity, float equipEffectChance, float modifierMultiplier) => throw new NotImplementedException();

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

        private void HandleEquipItemGeneration(IEquipItem equip, List<string> additionalItemEffects, Rarity rarity, float equipEffectChance)
        {
            if (equip.Rarity is Rarity.Mythic or Rarity.Unique) return;

            // concat all item effects with effects from context
            var allEffects = effectProvider.GetCopyItemsEffects().Concat(additionalItemEffects).ToList();
            string equipItemEffect = rnd.Randf() <= equipEffectChance
                ? allEffects[rnd.RandiRange(0, allEffects.Count)]
                : string.Empty;
            equip.SetItemEffect(equipItemEffect);
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

            equip.SetAdditionalModifiers(chosenMods);
        }

        private float ApplyPlayerMultiplier(float baseValue, ModifierType type)
        {
            float multiplier = craftingMastery.GetCurrentValueMultiplier();
            if (type == ModifierType.Multiplicative)
                return 1f + (baseValue - 1f) * multiplier;

            return baseValue * multiplier;
        }

        private IItem CreateCoal() => dataProvider.CopyItem("Coal");
    }
}
