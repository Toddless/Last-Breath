namespace Core.World.Containers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Items;
    using Services;

    /// <summary>The authored position a chest could not mint: its slot, the item it names and what went wrong.</summary>
    public record ChestMintRefusal(string SlotId, string ItemId, string Problem);

    /// <summary>Mints a chest's authored positions one by one and hands back all of their slots or none of them.</summary>
    public sealed class AuthoredChestMinter(IItemCreationService creation)
    {
        /// <summary>An authored position holds what its author wrote: no bonus effect is rolled onto it.</summary>
        private const float BonusEffectChance = 0f;

        /// <summary>Affix values stay at their authored scale: no difficulty stands behind an authored position.</summary>
        private const float ModifierMultiplier = 1f;

        /// <summary>The most one slot of an item that does not stack can hold.</summary>
        private const int SingleUnit = 1;

        private const string CreationFailedFormat = "creation failed with {0}: {1}";

        private const string UnstackableAmountFormat = "the item does not stack, yet the position asks for {0}";

        /// <summary>Every position minted into its slot in authored order; the first refusal ends the mint and leaves no slots.</summary>
        public bool TryMint(ChestDefinition definition, out IReadOnlyList<ChestSlot> slots,
            [NotNullWhen(false)] out ChestMintRefusal? refusal)
        {
            List<ChestSlot> minted = [];
            slots = [];
            foreach (var position in definition.Contents.Items)
            {
                if (!TryCreate(position, out var item, out refusal)) return false;
                refusal = StackRefusal(position, item);
                if (refusal != null) return false;
                minted.Add(new ChestSlot(position.SlotId, item, position.Amount));
            }

            slots = minted;
            refusal = null;
            return true;
        }

        /// <summary>The position's item from the creation service; any failure there becomes the refusal instead of escaping.</summary>
        private bool TryCreate(AuthoredChestItem position, [NotNullWhen(true)] out IItem? item,
            [NotNullWhen(false)] out ChestMintRefusal? refusal)
        {
            try
            {
                item = creation.CreateItem(position.ItemId, [], position.Rarity, BonusEffectChance, ModifierMultiplier, position.Rarity);
                refusal = null;
                return true;
            }
            catch (Exception exception)
            {
                item = null;
                refusal = Refuse(position, string.Format(CreationFailedFormat, exception.GetType().Name, exception.Message));
                return false;
            }
        }

        /// <summary>A refusal for more than one of an item that does not stack: the bag would put one instance into several slots.</summary>
        private static ChestMintRefusal? StackRefusal(AuthoredChestItem position, IItem item) =>
            item.MaxStackSize <= SingleUnit && position.Amount > SingleUnit
                ? Refuse(position, string.Format(UnstackableAmountFormat, position.Amount))
                : null;

        private static ChestMintRefusal Refuse(AuthoredChestItem position, string problem) =>
            new(position.SlotId, position.ItemId, problem);
    }
}
