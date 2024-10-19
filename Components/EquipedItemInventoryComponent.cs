namespace Playground.Components
{
    using System;
    using Godot;
    using Playground.Script.Helpers;
    using Playground.Script.Items;

    [GlobalClass]
    public partial class EquipedItemInventoryComponent : Node
    {
        private const string PathToWeapoAreaNode = "/root/MainScene/CharacterBody2D/InventoryComponent/EquipedItemInventoryComponent/WeaponWindow/WeaponLeftIcon/Area2D";
        private const string PathToWeaponSlotIcon = "/root/MainScene/CharacterBody2D/InventoryComponent/EquipedItemInventoryComponent/WeaponWindow/WeaponLeftIcon";
        private const string PathToWeaponWindows = "/root/MainScene/CharacterBody2D/InventoryComponent/EquipedItemInventoryComponent/WeaponWindow";

        private GlobalSignals? _globalSignals;
        private Panel? _weaponWindow;
        private TextureRect? _weaponLeftSlotIcon;
        private Area2D? _weaponLeftSlotArea;


        public override void _Ready()
        {
            _weaponLeftSlotArea = GetNode<Area2D>(PathToWeapoAreaNode);
            _weaponLeftSlotIcon = GetNode<TextureRect>(PathToWeaponSlotIcon);
            _globalSignals = GetNode<GlobalSignals>(NodePathHelper.GlobalSignalPath);
            _weaponWindow = GetNode<Panel>(PathToWeaponWindows);
            _globalSignals.InventoryVisible += WeaponWindowVisible;
            if (_weaponLeftSlotArea == null || _weaponLeftSlotIcon == null)
            {
                ArgumentNullException.ThrowIfNull(_weaponLeftSlotArea);
                ArgumentNullException.ThrowIfNull(_weaponLeftSlotIcon);
            }
            _weaponWindow.Visible =false;
        }

        private void WeaponWindowVisible(bool visible)
        {
            _weaponWindow!.Visible = visible;
        }

        public void SetItemToSlot(Item? item)
        {
            if(item == null)
            {
                return;
            }
            _weaponLeftSlotIcon!.Texture = item.Icon;
            GD.Print("Item set");
        }

        public void RemoveItemFromSlot(Item? item)
        {
            _weaponLeftSlotIcon?.Dispose();
        }
    }
}
