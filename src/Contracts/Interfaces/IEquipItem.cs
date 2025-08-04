namespace Contracts.Interfaces
{
    using System.Collections.Generic;
    using Contracts.Enums;

    public interface IEquipItem : IItem
    {
        public IReadOnlyList<IModifier> Modifiers { get; }
        public EquipmentPart EquipmentPart { get; }
        public AttributeType AttributeType { get; }

        void OnUnequip();
    }
}
