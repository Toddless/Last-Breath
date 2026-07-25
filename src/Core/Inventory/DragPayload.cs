namespace Core.Inventory
{
    /// <summary>Keys of the inventory drag&amp;drop payload dictionary — the contract between bag
    /// slots and equipment slots. Constants because a typo in a literal breaks the drop silently.</summary>
    public static class DragPayload
    {
        public const string Item = "Item";
        public const string Instance = "Instance";
        public const string Quantity = "Quantity";
        public const string MaxStackSize = "MaxStackSize";
        public const string Source = "Source";
        public const string EquipmentPiece = "EquipmentPiece";
    }
}
