namespace Playground.Script.Inventory
{
    using System;
    using Godot;
    using Playground.Script.Enums;
    using Playground.Script.Helpers;
    using Playground.Script.Items;

    public partial class EquipmentSlot : TextureButton
    {
        public EquipItem? CurrentItem { get; private set; }

        public event Action<EquipmentSlot, MouseButtonPressed>? EquipItemPressed;

        public override void _GuiInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton p && CurrentItem != null)
            {
                EquipItemPressed?.Invoke(this, MouseInputHelper.GetPressedButtons(p));
                GetViewport().SetInputAsHandled();
            }
        }

        public void EquipItem(EquipItem item)
        {
            CurrentItem = item;
            IgnoreTextureSize = true;
            TextureNormal = item.Icon;
        }

        public void UnequipItem()
        {
            CurrentItem = null;
            TextureNormal = null;
        }
    }
}
