namespace Core.Interfaces.Items
{
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Modifiers;

    public interface IEquipItem : IItem
    {
        public IReadOnlyList<IModifier> BaseModifiers { get; }
        public IReadOnlyList<IModifier> AdditionalModifiers { get; }
        public EquipmentPart EquipmentPart { get; }
        public AttributeType AttributeType { get; }

        void OnUnequip();
    }
}
