namespace LastBreath.Script.Items
{
    using Godot;
    using Core.Enums;
    using Core.Modifiers;
    using LastBreath.Script;
    using Core.Interfaces.Data;
    using Core.Interfaces.Items;
    using System.Collections.Generic;
    using LastBreath.Script.Items.ItemData;
    using Core.Interfaces.Skills;
    using Core.Interfaces.Abilities;
    using Core.Interfaces;

    [Tool]
    [GlobalClass]
    public partial class EquipItem : Item, IEquipItem
    {
        protected const float From = 0.8f;
        protected const float To = 1.2f;

        protected List<IModifier> BaseMods = [];
        protected List<IModifier> AdditionalMods = [];
        protected List<IEffect> Effects = [];
        [Export] public EquipmentPart EquipmentPart { get; protected set; }
        [Export] public AttributeType AttributeType { get; protected set; } = AttributeType.None;
        public ICharacter? Owner { get; private set; }
        public IReadOnlyList<IModifier> BaseModifiers => BaseMods;
        public IReadOnlyList<IModifier> AdditionalModifiers => AdditionalMods;
        public ISkill? Skill { get; private set; }

        /// <summary>
        /// Default constructor to instantiate this from Resource
        /// </summary>
        public EquipItem()
        {
        }

        /// <summary>
        /// Constructro to create items via code. 
        /// </summary>
        /// <param name="rarity"></param>
        /// <param name="equipmentPart"></param>
        /// <param name="type"></param>
        public EquipItem(Rarity rarity, EquipmentPart equipmentPart, AttributeType type, ISkill? skill = default) 
        {
            Rarity = rarity;
            EquipmentPart = equipmentPart;
            AttributeType = type;
            Skill = skill;
            SetId();
        }

        public virtual void OnEquip(ICharacter owner)
        {
            Owner = owner;
            BaseMods.ForEach(Owner.Modifiers.AddTemporaryModifier);
            Effects.ForEach(Owner.Effects.AddTemporaryEffect);
        }

        public virtual void OnUnequip()
        {
            if (Owner != null)
            {
                BaseMods.ForEach(Owner.Modifiers.RemoveTemporaryModifier);
                Effects.ForEach(Owner.Effects.RemoveEffect);
                Owner = null;
            }
        }

        public virtual void UpgradeItemLevel() { }
        protected virtual void UpdateItem() { }

        protected override void LoadData()
        {
            var itemStats = DiContainer.GetService<IItemDataProvider<ItemStats>>()?.GetItemData(Id);
            if (itemStats != null)
            {
                BaseMods = ModifiersCreator.ItemStatsToModifier(itemStats, this);
            }
            var mediaData = DiContainer.GetService<IItemDataProvider<ItemMediaData>>()?.GetItemData(Id);
            if (mediaData != null)
            {
                Icon = mediaData.IconTexture;
                FullImage = mediaData.FullTexture;
            }
        }

        protected virtual void SetEffects() { }

        private void SetId()
        {
            if (string.IsNullOrWhiteSpace(Id))
            {
                if (AttributeType != AttributeType.None)
                {
                    Id = $"{GetType().Name}_{AttributeType}_{Rarity}";
                }
                else
                {
                    Id = $"{GetType().Name}_{Rarity}";
                }
            }
        }

        
        public void SetBaseModifiers(IEnumerable<IModifier> modifiers)
        {

        }
        public void SetAdditionalModifiers(IEnumerable<IModifier> modifiers)
        {

        }

        public void SetSkill(ISkill skill) => throw new System.NotImplementedException();
    }
}
