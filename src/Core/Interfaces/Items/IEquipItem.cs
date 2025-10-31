namespace Core.Interfaces.Items
{
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.Skills;
    using Core.Interfaces.Crafting;
    using System.Collections.Generic;

    public interface IEquipItem : IItem
    {
        public IReadOnlyList<IModifier> BaseModifiers { get; }
        public IReadOnlyList<IModifier> AdditionalModifiers { get; }
        public EquipmentType EquipmentPart { get; }
        public AttributeType AttributeType { get; }
        public ISkill? Skill { get; }
        int UpdateLevel { get; }
        int MaxUpdateLevel { get; }
        IReadOnlyDictionary<string, int> UsedResources { get; }
        IReadOnlyList<IMaterialModifier> ModifiersPool { get; }

        void SetBaseModifiers(IEnumerable<IModifier> modifiers);
        void SetAdditionalModifiers(IEnumerable<IModifier> modifiers);
        void SetSkill(ISkill skill);
        void OnEquip(ICharacter owner);
        void OnUnequip();
        bool Upgrade(int upgradeLevel = 1);
        bool Downgrade(int downgradeLevel = 1);
        void ReplaceAdditionalModifier(int hash, IModifier newModifier);
        void SaveModifiersPool(IEnumerable<IMaterialModifier> modifiers);
        void SaveUsedResources(Dictionary<string, int> resources);
        void RemoveAdditionalModifier(int hash);
        void AddAdditionalModifier(IModifier modifier);
    }
}
