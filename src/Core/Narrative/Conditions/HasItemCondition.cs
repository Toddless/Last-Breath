namespace Core.Narrative.Conditions
{
    using Data.GameData;
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
        private const string ItemIdKey = "itemId";
        private const string AmountKey = "amount";
        private const int DefaultAmount = 1;

        private static readonly NarrativeRecordSpec s_parameters = NarrativeParameterSchema.Of(TypeName,
            NarrativeParameterSchema.Text(ItemIdKey, required: true,
                DataCatalog.EquipItems, DataCatalog.Resources, DataCatalog.Items, DataCatalog.Ornaments),
            NarrativeParameterSchema.Integer(AmountKey, DefaultAmount));

        public string Type => TypeName;

        /// <summary>The bag is searched by raw id, so gold and augments — minted outside any catalog —
        /// are named in itemId just as well as the catalogued items are.</summary>
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
