namespace Playground.Script.Items
{
    using System.Collections.Generic;
    using System.Text;
    using Playground.Script.Abilities.Interfaces;
    using Playground.Script.Abilities.Modifiers;
    using Playground.Script.Enums;
    using Playground.Script.Items.ItemData;

    public partial class EquipItem : Item
    {
        protected const float From = 0.8f;
        protected const float To = 1.2f;

        protected List<IModifier> BaseModifiers = [];
        protected List<IEffect> Effects = [];
        public ICharacter? Owner { get; private set; }
        public EquipmentPart EquipmentPart { get; protected set; }

        public EquipItem(GlobalRarity rarity, EquipmentPart equipmentPart)
        {
            Rarity = rarity;
            EquipmentPart = equipmentPart;
            LoadData();
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
            var itemStats = GetItemStats();
            if (itemStats != null)
            {
                BaseModifiers = ModifiersCreator.ItemStatsToModifier(itemStats, this);

                LoadMediaData();
            }
        }

        private void LoadMediaData()
        {
            var mediaData = GetItemMediaData();
            if (mediaData != null)
            {
                Icon = mediaData.IconTexture;
                Description = mediaData.Description;
                ItemName = mediaData.Name;
                FullImage = mediaData.FullTexture;
            }
        }

        protected virtual void SetEffects() { }

        protected virtual ItemStats? GetItemStats() => EquipmentPart switch
        {
            EquipmentPart.Cloak => DiContainer.GetService<IItemStatsHandler>()?.GetBodyArmorStats(BodyArmorType.Cloak, Rarity),
            EquipmentPart.Amulet => DiContainer.GetService<IItemStatsHandler>()?.GetJewelleryStats(JewelleryType.Amulet, Rarity),
            EquipmentPart.Belt => DiContainer.GetService<IItemStatsHandler>()?.GetJewelleryStats(JewelleryType.Belt, Rarity),
            _ => null
        };

        protected virtual ItemMediaData? GetItemMediaData() => EquipmentPart switch
        {
            EquipmentPart.Cloak => ItemsMediaHandler.Inctance?.GetBodyArmorMediaData(BodyArmorType.Cloak, Rarity),
            EquipmentPart.Amulet => ItemsMediaHandler.Inctance?.GetJewelleryMediaData(JewelleryType.Amulet, Rarity),
            EquipmentPart.Belt => ItemsMediaHandler.Inctance?.GetJewelleryMediaData(JewelleryType.Belt, Rarity),
            _ => null
        };
    }
}
