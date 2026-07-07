namespace Core.Components
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
        bool TryEquip(IEquipItem item, out IEquipItem? replaced);
        bool TryUnequip(EquipmentPiece piece, out IEquipItem? removed);
        void RefreshSlot(EquipmentPiece piece);
    }
}
