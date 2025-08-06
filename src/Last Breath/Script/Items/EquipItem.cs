namespace LastBreath.Script.Items
{
    using System.Collections.Generic;
    using System.Text;
    using Core.Enums;
    using Core.Interfaces.Data;
    using Core.Interfaces.Items;
    using Core.Modifiers;
    using LastBreath.Script;
    using LastBreath.Script.Abilities.Interfaces;
    using LastBreath.Script.Items.ItemData;

    public partial class EquipItem : Item, IEquipItem
    {
        protected const float From = 0.8f;
        protected const float To = 1.2f;

        protected List<IModifier> BaseModifiers = [];
        protected List<IEffect> Effects = [];

        public ICharacter? Owner { get; private set; }
        public EquipmentPart EquipmentPart { get; protected set; }
        public AttributeType AttributeType { get; protected set; } = AttributeType.None;
        public IReadOnlyList<IModifier> Modifiers => BaseModifiers;

        public EquipItem(Rarity rarity, EquipmentPart equipmentPart, AttributeType type)
        {
            Rarity = rarity;
            EquipmentPart = equipmentPart;
            AttributeType = type;
        }

        public virtual void OnEquip(ICharacter owner)
        {
            Owner = owner;
            BaseModifiers.ForEach(Owner.Modifiers.AddTemporaryModifier);
            Effects.ForEach(Owner.Effects.AddTemporaryEffect);
        }

        public virtual void OnUnequip()
        {
            if (Owner != null)
            {
                BaseModifiers.ForEach(Owner.Modifiers.RemoveTemporaryModifier);
                Effects.ForEach(Owner.Effects.RemoveEffect);
                Owner = null;
            }
        }

        public override List<string> GetItemStatsAsStrings()
        {
            List<string> stats = [];
            foreach (var modifier in BaseModifiers)
            {
                StringBuilder stringBuilder = new();
                stringBuilder.Append(modifier.Parameter);
                stringBuilder.Append(':');
                stringBuilder.Append(' ');
                stringBuilder.Append(modifier.Value);
                stats.Add(stringBuilder.ToString());
            }
            return stats;
        }

        public virtual void UpgradeItemLevel() { }
        protected virtual void UpdateItem() { }

        protected virtual void LoadData()
        {
            var itemStats = DiContainer.GetService<IItemDataProvider<ItemStats, IEquipItem>>()?.GetItemData(this);
            if (itemStats != null)
            {
                BaseModifiers = ModifiersCreator.ItemStatsToModifier(itemStats, this);
            }
            var mediaData = DiContainer.GetService<IItemDataProvider<ItemMediaData, IEquipItem>>()?.GetItemData(this);
            if (mediaData != null)
            {
                Icon = mediaData.IconTexture;
                FullImage = mediaData.FullTexture;
                SetDescription(mediaData.Description);
                SetName(mediaData.Name);
            }
        }

        protected virtual void SetEffects() { }
    }
}
