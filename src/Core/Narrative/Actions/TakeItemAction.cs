namespace Core.Narrative.Actions
{
    using Inventory;
    using Newtonsoft.Json.Linq;

    /// <summary>Availability is the CALLER's problem: gate the option on HasItem first.</summary>
    public class TakeItemAction(IInventory inventory, string itemId, int amount) : INarrativeAction
    {
        public void Execute(NarrativeContext context) => inventory.RemoveItemById(itemId, amount);
    }

    public class TakeItemActionFactory(IInventory inventory) : INarrativeActionFactory
    {
        public string Type => "TakeItem";

        public INarrativeAction? Create(JObject json, INarrativeActionParser parser)
        {
            string itemId = json.Value<string>("itemId") ?? string.Empty;
            if (itemId.Length == 0)
            {
                Tracker.TrackError("TakeItem action: itemId is missing");
                return null;
            }

            return new TakeItemAction(inventory, itemId, json.Value<int?>("amount") ?? 1);
        }
    }
}
