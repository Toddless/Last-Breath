namespace LootGeneration.Internal
{
    using Godot;
    using Source;
    using Utilities;
    using Core.Data;
    using Core.Enums;
    using System.Linq;
    using Core.Interfaces;
    using Core.Interfaces.Items;
    using System.Collections.Generic;

    public class ItemCreationService(IItemEffectProvider effectProvider, IItemDataProvider dataProvider, RandomNumberGenerator rnd) : IItemCreationService
    {
        public IItem CreateItem(string id)
        {
            return dataProvider.CopyItem(id);
        }

        public IItem CreateItem(string id, List<string> additionalItemEffects, Rarity rarity, float equipEffectChance)
        {
            var item = CreateItem(id);
            if (item is IEquipItem equipItem) HandleEquipItemGeneration(equipItem, additionalItemEffects, rarity, equipEffectChance);

            return item;
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

            // Don't forget to concat item modifiers with modifier from context
            var weighted = WeightedRandomPicker.CalculateWeights(modifiersPool);
            var chosenMods = WeightedRandomPicker.PickRandomMultipleWithoutDuplicate(
                weighted.WeightedObjects,
                weighted.TotalWeight,
                rarity.ConvertRarityToItemModifierAmount(),
                rnd);

            equip.SetAdditionalModifiers(chosenMods);
        }
    }
}
