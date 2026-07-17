namespace Crafting.Source.EventHandlers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Core;
    using Core.Crafting;
    using Core.Data;
    using Core.Enums;
    using Core.Events;
    using Core.Inventory;
    using Core.Items;
    using Core.MessageBus;
    using Core.MessageBus.Messages;
    using Godot;

    /// <summary>Shattering an equip: EVERY item yields the dust of its category, and a crafted one
    /// (non-empty used resources) additionally returns part of what built it — required and optional
    /// creation parts alike (the merged view), mastery scales the refund. Loot items remember no
    /// resources, so dust is their whole return. The attempt teaches through the Shatter exp factor.</summary>
    public class DestroyItemMessageHandler(
        IInventory inventory,
        IItemDataProvider provider,
        ICraftingMastery craftingMastery,
        IGameMessageBus gameMessageBus)
        : IMessageHandler<DestroyItemMessage>
    {
        // Category -> dust id: deliberately an explicit local mapping, NOT read from the UpgradeCosts
        // recraft section — a recraft price rebalance there must never silently change what shattering yields.
        private static readonly Dictionary<EquipmentCategory, string> s_dustByCategory = new()
        {
            [EquipmentCategory.Weapon] = "Upgrade_Resource_Weapon_Dust",
            [EquipmentCategory.Armor] = "Upgrade_Resource_Armor_Dust",
            [EquipmentCategory.Jewellery] = "Upgrade_Resource_Jewellery_Dust",
        };

        public Task HandleMessageAsync(DestroyItemMessage message)
        {
            var item = inventory.GetItem<IEquipItem>(message.ItemInstanceId);
            if (item == null) return Task.CompletedTask;

            // Ascended (sealed) items cannot be shattered by design — selling is the only way out.
            // A UI gate will hide the action later; until then the handler is the gate.
            if (item.IsSealed)
            {
                Tracker.TrackInfo($"Refused to shatter sealed item {item.Id} ({item.InstanceId})", this);
                return Task.CompletedTask;
            }

            foreach (var res in item.UsedResources)
                AddToInventory(res.Key, Mathf.RoundToInt(res.Value * craftingMastery.GetCurrentResourceMultiplier()));

            AddDust(item);
            inventory.RemoveItemByInstanceId(message.ItemInstanceId);
            gameMessageBus.PublishMessageAsync(new GainCraftingExpirienceMessage(CraftingMode.Shatter, item.Rarity));

            return Task.CompletedTask;
        }

        /// <summary>The dust of the item's category. Base amount is a FLAT 1 regardless of rarity
        /// (owner's decision); mastery growth uses the raw resource-return channel bonus —
        /// floor(1 × (1 + bonus)) — not the refund fraction, so zero mastery still yields exactly 1.</summary>
        private void AddDust(IEquipItem item)
        {
            string dustId = s_dustByCategory[item.EquipmentPiece.ConvertEquipmentPartToCategory()];
            AddToInventory(dustId, Mathf.FloorToInt(1f + craftingMastery.GetResourceReturnBonus()));
        }

        private void AddToInventory(string id, int amount)
        {
            if (amount <= 0) return;
            if (inventory.TryAddItemStacks(id, amount)) return;

            var itemInstance = provider.CopyItem(id);
            inventory.TryAddItem(itemInstance, amount);
        }
    }
}
