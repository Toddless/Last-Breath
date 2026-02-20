namespace LastBreath.Source.Inventory
{
    using Core.Interfaces.Entity;
    using Core.Interfaces.Items;
    using Godot;

    public partial class EquipmentSlot : Slot
    {

        public override void _Ready()
        {
            ClipContents = true;
        }

        public override void _GuiInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton p && CurrentItem != null)
            {
                GetViewport().SetInputAsHandled();
            }
        }

        public void EquipItem(IEquipItem item, IEntity owner)
        {
            //CurrentItem = item;
            //if (CurrentItem is WeaponItem w && owner is Player p) p.OnEquipWeapon(w);
            //CurrentItem.OnEquip(owner);
            //TextureNormal = item.Icon;
        }

        public void UnequipItem()
        {
            //CurrentItem?.OnUnequip();
            //CurrentItem = null;
            //TextureNormal = DefaltTexture;
        }
    }
}
