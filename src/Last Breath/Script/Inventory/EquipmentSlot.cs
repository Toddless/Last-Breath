namespace LastBreath.Script.Inventory
{
    using Godot;
    using Core.Interfaces;
    using Core.Interfaces.Items;

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

        public void EquipItem(IEquipItem item, ICharacter owner)
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
