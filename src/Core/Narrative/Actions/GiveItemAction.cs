namespace Core.Narrative.Actions
{
    using Data;
    using Inventory;
    using Newtonsoft.Json.Linq;

    /// <summary>Mints the item into the player's inventory (equip rewards are fresh rolls, resources
    /// plain copies). Capacity is the CALLER's problem: a quest turn-in must be gated on free space
    /// before executing.</summary>
    public class GiveItemAction(Items.IItemMinter items, IInventory inventory, string itemId, int amount) : INarrativeAction
    {
        public void Execute(NarrativeContext context) => inventory.TryAddItem(items.MintItem(itemId), amount);
    }

    public class GiveItemActionFactory(Items.IItemMinter items, IInventory inventory) : INarrativeActionFactory
    {
        public string Type => "GiveItem";

        public INarrativeAction? Create(JObject json, INarrativeActionParser parser)
        {
            string itemId = json.Value<string>("itemId") ?? string.Empty;
            if (itemId.Length == 0)
            {
                Tracker.TrackError("GiveItem action: itemId is missing");
                return null;
            }

            return new GiveItemAction(items, inventory, itemId, json.Value<int?>("amount") ?? 1);
        }
    }
}
