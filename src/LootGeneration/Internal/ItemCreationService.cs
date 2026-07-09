namespace LootGeneration.Internal
{
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Core.Data;
    using Core.Enums;
    using Core.Items;
    using Core.Modifiers;
    using Core.Services;
    using Godot;
    using Source;

    /// <summary>Loot-side item spawner: copies a cached template from <see cref="IItemDataProvider"/> and, for
    /// equip items, rolls extra item effects and rarity via <see cref="IItemEffectProvider"/>. The runtime entry
    /// point <see cref="Source.LootGenerationService"/> calls to turn a rolled table id into a concrete drop.</summary>
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

        public IItem CreateItem(string id, List<string> additionalItemEffects, Rarity rarity, float equipEffectChance, float modifierMultiplier) => throw new System.NotImplementedException();

        public IItem CreateItemByRecipe(string recipeId, IEnumerable<IModifier> modifiers) => throw new System.NotImplementedException();

        private void HandleEquipItemGeneration(IEquipItem equip, List<string> additionalItemEffects, Rarity rarity, float equipEffectChance)
        {
            if (equip.Rarity is Rarity.Mythic or Rarity.Unique) return;

            // concat all item effects with effects from context
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

            equip.SetModifiers(chosenMods);
        }
    }
}
