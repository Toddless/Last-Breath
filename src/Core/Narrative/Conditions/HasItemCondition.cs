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
        private const string TypeName = "HasItem";

        /// <summary>The keys of the item reference, under the names the parser below reads them by: the
        /// asking half of the vocabulary writes an item exactly the way the giving half does.</summary>
        private const string ItemIdKey = ItemReference.Key;
        private const string AmountKey = ItemReference.AmountKey;
        private const int DefaultAmount = ItemReference.DefaultAmount;

        private static readonly NarrativeRecordSpec s_parameters =
            NarrativeParameterSchema.Of(TypeName, ItemReference.Field, ItemReference.AmountField);

        public string Type => TypeName;

        public NarrativeRecordSpec Parameters => s_parameters;

        public INarrativeCondition? Create(JObject json, INarrativeConditionParser parser)
        {
            string itemId = json.Value<string>(ItemIdKey) ?? string.Empty;
            if (itemId.Length == 0)
            {
                Tracker.TrackError($"{TypeName} condition: {ItemIdKey} is missing");
                return null;
            }

            return new HasItemCondition(inventory, itemId, json.Value<int?>(AmountKey) ?? DefaultAmount);
        }
    }
}
