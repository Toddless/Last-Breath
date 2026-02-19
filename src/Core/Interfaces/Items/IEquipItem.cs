namespace Core.Interfaces.Items
{
    using Enums;
    using Entity;
    using Modifiers;
    using System.Collections.Generic;

    public interface IEquipItem : IItem
    {
        public IReadOnlyList<IModifier> BaseModifiers { get; }
        public IReadOnlyList<IModifier> AdditionalModifiers { get; }
        public EquipmentType EquipmentPart { get; }
        AttributeType AttributeType { get; set; }
        public string ItemEffect { get; }
        int UpdateLevel { get; set; }
        int MaxUpdateLevel { get; set; }
        IReadOnlyDictionary<string, int> UsedResources { get; }
        IReadOnlyList<IModifier> ModifiersPool { get; }

        void SetBaseModifiers(IEnumerable<IModifier> modifiers);
        void SetAdditionalModifiers(IEnumerable<IModifier> modifiers);
        void SetItemEffect(string effectId);
        void OnEquip(IEntity owner);
        void OnUnequip();
        bool Upgrade(int upgradeLevel = 1);
        bool Downgrade(int downgradeLevel = 1);
        void ReplaceAdditionalModifier(int hash, IModifier newModifier);
        void SaveModifiersPool(IEnumerable<IModifier> modifiers);
        void SaveUsedResources(Dictionary<string, int> resources);
        void RemoveAdditionalModifier(int hash);
        void AddAdditionalModifier(IModifier modifier);
    }
}
