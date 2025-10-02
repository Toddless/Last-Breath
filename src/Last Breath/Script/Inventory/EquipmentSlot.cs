namespace LastBreath.Script.Inventory
{
    using System;
    using Core.Enums;
    using Core.Interfaces;
    using Godot;
    using LastBreath.Script.Helpers;
    using LastBreath.Script.Items;

    public partial class EquipmentSlot : Slot
    {
        public event Action<EquipmentSlot, MouseButtonPressed>? EquipItemPressed;

        public override void _Ready()
        {
            ClipContents = true;
            this.MouseEntered += OnMouseEnter;
            this.MouseExited += OnMouseExit;
        }

        public override void _GuiInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton p && CurrentItem != null)
            {
                EquipItemPressed?.Invoke(this, MouseInputHelper.GetPressedButtons(p));
                GetViewport().SetInputAsHandled();
            }
        }

        public void EquipItem(EquipItem item, ICharacter owner)
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
