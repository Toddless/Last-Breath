namespace Core.Narrative.Conditions
{
    using Inventory;
    using Newtonsoft.Json.Linq;

    /// <summary>Reads the live inventory instead of a "picked up" counter — that is what lets a
    /// quest item found before the quest count, and a sold-off one stop counting.</summary>
    public class HasItemCondition(IInventory inventory, string itemId, int amount) : INarrativeCondition
    {
        public bool IsMet(NarrativeContext context) => inventory.GetTotalItemAmount(itemId) >= amount;
    }

    public class HasItemConditionFactory(IInventory inventory) : INarrativeConditionFactory
    {
        public string Type => "HasItem";

        public INarrativeCondition? Create(JObject json, INarrativeConditionParser parser)
        {
            string itemId = json.Value<string>("itemId") ?? string.Empty;
            if (itemId.Length == 0)
            {
                Tracker.TrackError("HasItem condition: itemId is missing");
                return null;
            }

            return new HasItemCondition(inventory, itemId, json.Value<int?>("amount") ?? 1);
        }
    }
}
