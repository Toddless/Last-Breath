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
        private const string TypeName = "TakeItem";

        private static readonly NarrativeRecordSpec s_parameters =
            NarrativeParameterSchema.Of(TypeName, ItemReference.Field, ItemReference.AmountField);

        public string Type => TypeName;

        /// <summary>The bag is emptied by raw id, the same id the giving side mints by.</summary>
        public NarrativeRecordSpec Parameters => s_parameters;

        public INarrativeAction? Create(JObject json, INarrativeActionParser parser) =>
            ItemReference.Require(json, TypeName) is { } itemId
                ? new TakeItemAction(inventory, itemId, ItemReference.Amount(json))
                : null;
    }
}
