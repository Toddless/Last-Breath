namespace Playground
{
    using Godot;
    using Playground.Script;
    using Playground.Script.Items;

    public partial class GlobalSignals : Node
    {
        [Signal]
        public delegate void OnEquipItemEventHandler(Item item);
        [Signal]
        public delegate void InventoryVisibleEventHandler(bool visible);
        [Signal]
        public delegate void PlayerEncountedEventHandler();


        public override void _Ready()
        {
            // TODO: find better place for DI
            // For now this is first initialized class(set as singleton in godot editor), so thats why im configuring DI here 
            DiContainer.Configure();
        }
    }
}
