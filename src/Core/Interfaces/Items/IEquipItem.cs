namespace Core.Interfaces.Items
{
    using System.Collections.Generic;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Crafting;
    using Core.Interfaces.Skills;

    public interface IEquipItem : IItem
    {
        public IReadOnlyList<IItemModifier> BaseModifiers { get; }
        public IReadOnlyList<IItemModifier> AdditionalModifiers { get; }
        public EquipmentType EquipmentPart { get; }
        public AttributeType AttributeType { get; }
        public ISkill? Skill { get; }
        int UpdateLevel { get; }
        int MaxUpdateLevel { get; }
        IReadOnlyDictionary<string, int> UsedResources { get; }
        IReadOnlyList<IMaterialModifier> ModifiersPool { get; }

        void SetBaseModifiers(IEnumerable<IItemModifier> modifiers);
        void SetAdditionalModifiers(IEnumerable<IItemModifier> modifiers);
        void SetSkill(ISkill skill);
        void OnUnequip();
        bool Upgrade(int upgradeLevel = 1);
        bool Downgrade(int downgradeLevel = 1);
        void ReplaceAdditionalModifier(int hash, IItemModifier newModifier);
        void SaveModifiersPool(IEnumerable<IMaterialModifier> modifiers);
        void SaveUsedResources(Dictionary<string, int> resources);
        void RemoveAdditionalModifier(int hash);
        void AddAdditionalModifier(IItemModifier modifier);
    }
}
