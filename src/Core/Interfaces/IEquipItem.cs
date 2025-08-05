namespace Core.Interfaces
{
    using System.Collections.Generic;
    using Core.Enums;

    public interface IEquipItem : IItem
    {
        public IReadOnlyList<IModifier> Modifiers { get; }
        public EquipmentPart EquipmentPart { get; }
        public AttributeType AttributeType { get; }

        void OnUnequip();
    }
}
