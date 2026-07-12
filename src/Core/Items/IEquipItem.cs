namespace Core.Items
{
    using System.Collections.Generic;
    using Enums;
    using Modifiers;
    using Entity;

    public interface IEquipItem : IItem
    {
        IReadOnlyList<IModifierInstance> Implicits { get; }
        IReadOnlyList<IModifierInstance> Modifiers { get; }

        /// <summary>Context lines ("healing efficiency +15%", "+1 bleed duration"): scale with upgrades
        /// like regular lines, but attach to the owner's pipelines on equip instead of parameter resolution.</summary>
        IReadOnlyList<ContextModifierEntry> ContextImplicits { get; }
        IReadOnlyList<ContextModifierEntry> ContextModifiers { get; }
        IReadOnlyList<IItemGrant> Grants { get; }
        IReadOnlyCollection<EntityParameter> AffectedParameters { get; }
        EquipmentPiece EquipmentPiece { get; }
        int UpdateLevel { get; set; }
        int MaxUpdateLevel { get; set; }

        /// <summary>Sealed items (ascended to Mythic) can never be modified again: no upgrades, rerolls or new grants.</summary>
        bool IsSealed { get; }
        IReadOnlyDictionary<string, int> UsedResources { get; }
        IReadOnlyList<IModifierDescriptor> ModifiersPool { get; }

        IEnumerable<IModifierInstance> GetResolvedModifiers(EntityParameter parameter);
        void SetImplicits(IEnumerable<IModifier> modifiers);
        void SetModifiers(IEnumerable<IModifier> modifiers);
        void SetContextImplicits(IEnumerable<ContextModifierEntry> entries);
        void SetContextModifiers(IEnumerable<ContextModifierEntry> entries);
        void OnEquip(IFightable owner);
        void OnUnequip();
        bool Upgrade(int upgradeLevel = 1);
        bool Downgrade(int downgradeLevel = 1);
        // Additional (rolled) modifiers are identified by InstanceId: duplicates of the same parameter+type
        // may coexist, so a rebuild/reroll targets exactly one line.
        void ReplaceAdditionalModifier(string instanceId, IModifierInstance newModifier);
        void SaveModifiersPool(IEnumerable<IModifierDescriptor> descriptors);
        void SaveUsedResources(Dictionary<string, int> resources);
        void RemoveAdditionalModifier(string instanceId);
        void AddAdditionalModifier(IModifierInstance modifier);
        void AddAdditionalContextModifier(ContextModifierEntry entry);
        void AddGrant(IItemGrant grant);
    }
}
