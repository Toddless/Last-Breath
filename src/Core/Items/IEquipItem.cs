namespace Core.Items
{
    using System.Collections.Generic;
    using Enums;
    using Modifiers;
    using Entity;

    public interface IEquipItem : IItem
    {
        IReadOnlyList<IModifier> Implicits { get; }
        IReadOnlyList<IModifier> Modifiers { get; }

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
        IReadOnlyList<IModifier> ModifiersPool { get; }

        IEnumerable<IModifierInstance> GetResolvedModifiers(EntityParameter parameter);
        void SetImplicits(IEnumerable<IModifier> modifiers);
        void SetModifiers(IEnumerable<IModifier> modifiers);
        void SetContextImplicits(IEnumerable<ContextModifierEntry> entries);
        void SetContextModifiers(IEnumerable<ContextModifierEntry> entries);
        void OnEquip(IFightable owner);
        void OnUnequip();
        bool Upgrade(int upgradeLevel = 1);
        bool Downgrade(int downgradeLevel = 1);
        // TODO:
        // старый метод для смены модификаторов.
        // Не рассчитан на модификаторы контекста и композитные/условные модификаторы
        void ReplaceAdditionalModifier(int hash, IModifier newModifier);
        void SaveModifiersPool(IEnumerable<IModifier> modifiers);
        void SaveUsedResources(Dictionary<string, int> resources);
        void RemoveAdditionalModifier(int hash);
        void AddAdditionalModifier(IModifier modifier);
        void AddGrant(IItemGrant grant);
    }
}
