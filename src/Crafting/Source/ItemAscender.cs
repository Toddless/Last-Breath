namespace Crafting.Source
{
    using Core.Data;
    using Core.Enums;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Items;
    using Core.Modifiers;
    using Core.Results;
    using Godot;
    using Utilities;

    public class ItemAscender(RandomNumberGenerator rnd, IItemDataProvider itemDataProvider) : IItemAscender
    {
        private const string MythicPoolId = "Mythic";
        // Invented default, tune later: chance to receive a bonus mythic modifier on ascension.
        private const float GiftChance = 0.25f;

        public bool CanAscend(IEquipItem item) =>
            item is IAscendable { IsAscendable: true } && !item.IsSealed && item.Rarity == Rarity.Legendary;

        public AscensionResult TryAscendItem(IEquipItem item)
        {
            if (!CanAscend(item) || item is not IAscendable ascendable) return new AscensionResult(false, null);

            var gift = TryRollGift(item);
            ascendable.TryAscend();
            return new AscensionResult(true, gift);
        }

        private IModifierInstance? TryRollGift(IEquipItem item)
        {
            if (rnd.Randf() > GiftChance) return null;

            var pool = itemDataProvider.GetEquipItemModifierPool(MythicPoolId);
            if (pool.Count == 0) return null;

            (var weightedObjects, float totalWeight) = WeightedRandomPicker.CalculateWeights(pool);
            var picked = WeightedRandomPicker.PickRandom(weightedObjects, totalWeight, rnd);
            var gift = ModifiersCreator.CreateModifierInstance(picked.EntityParameter, picked.ModifierValueType, picked.BaseValue, item.InstanceId);
            gift.Scope = picked.Scope;
            item.AddAdditionalModifier(gift);
            return gift;
        }
    }
}
