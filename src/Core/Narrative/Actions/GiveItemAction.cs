namespace Core.Narrative.Actions
{
    using Data.GameData;
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

        private static readonly NarrativeRecordSpec s_parameters = NarrativeParameterSchema.Of(TypeName,
            ItemIdParameter.Field, NarrativeParameterSchema.Integer(ItemIdParameter.AmountKey, ItemIdParameter.DefaultAmount));

        public string Type => TypeName;

        /// <summary>The minter is asked for the raw id, so an item minted outside any catalog is named
        /// here just as well as a catalogued one — the catalogs are what an editor offers, not a gate.</summary>
        public NarrativeRecordSpec Parameters => s_parameters;

        public INarrativeAction? Create(JObject json, INarrativeActionParser parser) =>
            ItemIdParameter.Require(json, TypeName) is { } itemId
                ? new GiveItemAction(items, inventory, itemId, ItemIdParameter.Amount(json))
                : null;
    }

    /// <summary>The item reference both halves of the item vocabulary are addressed by: one key, one
    /// field and one reading of it, so giving and taking cannot drift apart over the same parameter.</summary>
    internal static class ItemIdParameter
    {
        public const string Key = "itemId";
        public const string AmountKey = "amount";
        public const int DefaultAmount = 1;

        public static readonly NarrativeParameterSpec Field = NarrativeParameterSchema.Text(Key, required: true,
            DataCatalog.EquipItems, DataCatalog.Resources, DataCatalog.Items, DataCatalog.Ornaments);

        public static int Amount(JObject json) => json.Value<int?>(AmountKey) ?? DefaultAmount;

        public static string? Require(JObject json, string type)
        {
            string itemId = json.Value<string>(Key) ?? string.Empty;
            if (itemId.Length > 0) return itemId;

            Tracker.TrackError($"{type} action: {Key} is missing");
            return null;
        }
    }
}
