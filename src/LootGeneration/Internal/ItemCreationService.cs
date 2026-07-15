namespace LootGeneration.Internal
{
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Core.Data;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Items;
    using Core.Modifiers;
    using Core.Services;
    using Source;

    /// <summary>Loot-side item spawner: copies a cached template from <see cref="IItemDataProvider"/> and, for
    /// equip items, rolls extra item effects and rarity via <see cref="IItemEffectProvider"/>. The runtime entry
    /// point <see cref="Source.LootGenerationService"/> calls to turn a rolled table id into a concrete drop.</summary>
    public class ItemCreationService(IItemEffectProvider effectProvider, IItemDataProvider dataProvider, IRandomNumberGenerator rnd) : IItemCreationService
    {
        public IItem CreateItem(string id)
        {
            return dataProvider.CopyItem(id);
        }

        public IItem CreateItem(string id, List<string> additionalItemEffects, Rarity rarity, float equipEffectChance, float modifierMultiplier)
        {
            var item = CreateItem(id);
            if (item is IEquipItem equipItem) HandleEquipItemGeneration(equipItem, additionalItemEffects, rarity, equipEffectChance, modifierMultiplier);

            return item;
        }

        public IItem CreateItemByRecipe(string recipeId, IEnumerable<IModifierDescriptor> descriptors) => throw new System.NotImplementedException();

        private void HandleEquipItemGeneration(IEquipItem equip, List<string> additionalItemEffects, Rarity rarity, float equipEffectChance, float modifierMultiplier)
        {
            if (equip.Rarity is Rarity.Mythic or Rarity.Unique) return;

            // concat all item effects with effects from context
            equip.Rarity = rarity;

            var modifiersPool = dataProvider.GetEquipItemModifierPool(equip.Id);
            var basePool = dataProvider.GetEquipItemBaseModifierPool(equip.Id);
            var weighted = WeightedRandomPicker.CalculateWeights(modifiersPool.Concat(basePool));
            var chosenMods = WeightedRandomPicker.PickRandomMultipleWithoutDuplicate(
                weighted.WeightedObjects,
                weighted.TotalWeight,
                rarity.ConvertRarityToItemModifierAmount(),
                rnd);

            equip.SetModifiers(chosenMods.SelectMany(mod => CreateScaledInstances(mod, modifierMultiplier, equip.InstanceId)));
        }

        // TODO: duplicated verbatim in Main/Services/ItemCreationService — consolidate loot modifier scaling.
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
    }
}
