namespace Core.Entity.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Entity;
    using Enums;
    using Items;
    using Modifiers;

    public class EquipmentComponent(IFightable owner) : IEquipmentComponent
    {
        private readonly Dictionary<EquipmentPiece, IEquipItem> _slots = [];

        public event Action<EquipmentPiece, IEquipItem?>? EquipmentChanged;
        public event Action<IReadOnlyCollection<EntityParameter>>? SourceChanged;

        public IReadOnlyDictionary<EquipmentPiece, IEquipItem> Equipped => _slots;
        public IWeaponItem? Weapon => GetEquipped(EquipmentPiece.Weapon) as IWeaponItem;

        public IReadOnlyCollection<EntityParameter> AffectedParameters =>
            _slots.Values.SelectMany(item => item.AffectedParameters).ToHashSet();

        public IEquipItem? GetEquipped(EquipmentPiece piece) => _slots.GetValueOrDefault(piece);

        public IEnumerable<IModifierInstance> GetModifiers(EntityParameter parameter) =>
            _slots.Values.SelectMany(item => item.GetResolvedModifiers(parameter));

        public bool TryEquip(IEquipItem item, out IEquipItem? replaced)
        {
            replaced = null;
            var piece = item.EquipmentPiece;
            if (_slots.TryGetValue(piece, out var current))
            {
                if (current.IsSame(item.InstanceId)) return false;
                replaced = current;
                current.OnUnequip();
            }

            _slots[piece] = item;
            item.OnEquip(owner);
            EquipmentChanged?.Invoke(piece, item);
            NotifyChanged(CollectParameters(item, replaced));
            return true;
        }

        public bool TryUnequip(EquipmentPiece piece, out IEquipItem? removed)
        {
            if (!_slots.Remove(piece, out var item))
            {
                removed = null;
                return false;
            }

            removed = item;
            item.OnUnequip();
            EquipmentChanged?.Invoke(piece, null);
            NotifyChanged(item.AffectedParameters);
            return true;
        }

        public void RefreshSlot(EquipmentPiece piece)
        {
            if (_slots.TryGetValue(piece, out var item))
                NotifyChanged(item.AffectedParameters);
        }

        private static IReadOnlyCollection<EntityParameter> CollectParameters(IEquipItem item, IEquipItem? replaced)
        {
            var parameters = item.AffectedParameters.ToHashSet();
            if (replaced != null) parameters.UnionWith(replaced.AffectedParameters);
            return parameters;
        }

        private void NotifyChanged(IReadOnlyCollection<EntityParameter> parameters)
        {
            if (parameters.Count > 0) SourceChanged?.Invoke(parameters);
        }
    }
}
