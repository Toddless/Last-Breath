namespace Core.Narrative.Actions
{
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
        private const string TypeName = "GiveItem";

        private static readonly NarrativeRecordSpec s_parameters =
            NarrativeParameterSchema.Of(TypeName, ItemReference.Field, ItemReference.AmountField);

        public string Type => TypeName;

        public NarrativeRecordSpec Parameters => s_parameters;

        public INarrativeAction? Create(JObject json, INarrativeActionParser parser) =>
            ItemReference.Require(json, TypeName) is { } itemId
                ? new GiveItemAction(items, inventory, itemId, ItemReference.Amount(json))
                : null;
    }
}
