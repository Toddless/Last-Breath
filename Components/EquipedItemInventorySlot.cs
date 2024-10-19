namespace Playground.Components
{
    using Godot;
    using Playground.Script.Items;

    [GlobalClass]
    public abstract partial class EquipedItemInventorySlotComponent : Node
    {

        private Item? _item;
    }
}
