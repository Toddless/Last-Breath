namespace Core.Entity.Components
{
    using System;
    using System.Collections.Generic;
    using Enums;
    using Items;

    public interface IEquipmentComponent : IParameterModifierSource
    {
        event Action<EquipmentPiece, IEquipItem?>? EquipmentChanged;

        IReadOnlyDictionary<EquipmentPiece, IEquipItem> Equipped { get; }
        IWeaponItem? Weapon { get; }

        IEquipItem? GetEquipped(EquipmentPiece piece);

        /// <summary>Equips into the slot the item's piece resolves to (rings take the first free
        /// ring slot). An explicit <paramref name="targetSlot"/> — a drop on a concrete paperdoll
        /// slot — overrides the resolution; a slot that doesn't accept the piece refuses.</summary>
        bool TryEquip(IEquipItem item, out IEquipItem? replaced, EquipmentPiece? targetSlot = null);
        bool TryUnequip(EquipmentPiece piece, out IEquipItem? removed);
        void RefreshSlot(EquipmentPiece piece);
    }
}
