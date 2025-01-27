namespace Playground
{
    using Godot;
    using Playground.Script.Items;

    // Don´t know if i still need this. 
    public partial class GlobalSignals : Node
    {
        [Signal]
        public delegate void OnEquipItemEventHandler(Item item);
        [Signal]
        public delegate void InventoryVisibleEventHandler(bool visible);
        [Signal]
        public delegate void PlayerEncountedEventHandler();
    }
}
