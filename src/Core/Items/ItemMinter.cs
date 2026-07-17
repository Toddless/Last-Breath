namespace Core.Items
{
    using Data;

    public interface IItemMinter
    {
        /// <summary>Type-agnostic birth point for callers that only hold an id (quests, narrative,
        /// debug): an equip id mints a fresh roll, anything else is a plain copy of the resource.</summary>
        IItem MintItem(string id);
    }

    public sealed class ItemMinter(IEquipBlueprintProvider blueprints, IEquipItemMinter equipMinter, IItemDataProvider items) : IItemMinter
    {
        public IItem MintItem(string id) =>
            blueprints.GetBlueprint(id) != null ? equipMinter.Mint(id) : items.CopyItem(id);
    }
}
