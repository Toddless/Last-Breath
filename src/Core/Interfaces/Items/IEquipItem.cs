namespace Core.Interfaces.Items
{
    using System.Collections.Generic;
    using Entity;
    using Enums;
    using Modifiers;

    public interface IEquipItem : IItem
    {
        IReadOnlyList<IModifier> Implicits { get; }
        IReadOnlyList<IModifier> Modifiers { get; }
        IReadOnlyList<IItemGrant> Grants { get; }
        IReadOnlyCollection<EntityParameter> AffectedParameters { get; }
        EquipmentPiece EquipmentPiece { get; }
        string ItemEffect { get; }
        int UpdateLevel { get; set; }
        int MaxUpdateLevel { get; set; }

        /// <summary>Sealed items (ascended to Mythic) can never be modified again: no upgrades, rerolls or new grants.</summary>
        bool IsSealed { get; }
        IReadOnlyDictionary<string, int> UsedResources { get; }
        IReadOnlyList<IModifier> ModifiersPool { get; }

        IEnumerable<IModifierInstance> GetResolvedModifiers(EntityParameter parameter);
        void SetImplicits(IEnumerable<IModifier> modifiers);
        void SetModifiers(IEnumerable<IModifier> modifiers);
        void SetItemEffect(string effectId);
        void OnEquip(IFightable owner);
        void OnUnequip();
        bool Upgrade(int upgradeLevel = 1);
        bool Downgrade(int downgradeLevel = 1);
        void ReplaceAdditionalModifier(int hash, IModifier newModifier);
        void SaveModifiersPool(IEnumerable<IModifier> modifiers);
        void SaveUsedResources(Dictionary<string, int> resources);
        void RemoveAdditionalModifier(int hash);
        void AddAdditionalModifier(IModifier modifier);
        void AddGrant(IItemGrant grant);
    }
}
