namespace Core.Interfaces.Items
{
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Interfaces.Skills;
    using Core.Modifiers;

    public interface IEquipItem : IItem
    {
        public IReadOnlyList<IModifier> BaseModifiers { get; }
        public IReadOnlyList<IModifier> AdditionalModifiers { get; }
        public EquipmentType EquipmentPart { get; }
        public AttributeType AttributeType { get; }
        public ISkill? Skill { get; }
        int UpdateLevel { get; }

        void SetBaseModifiers(IEnumerable<IModifier> modifiers);
        void SetAdditionalModifiers(IEnumerable<IModifier> modifiers);
        void SetSkill(ISkill skill);
        void OnUnequip();
        bool Upgrade(int upgradeLevel = 1);
        bool Downgrade(int downgradeLevel = 1);
        void ReplaceAdditionalModifier(int hash, IModifier newModifier);
    }
}
