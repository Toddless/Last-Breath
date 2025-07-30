namespace LastBreath.Script.Items
{
    using LastBreath.Script.Enums;
    using LastBreath.Script.Items.ItemData;

    public partial class Cloak : EquipItem
    {
        public Cloak(GlobalRarity rarity)
            : base(rarity, EquipmentPart.Cloak)
        {
        }

        protected override void LoadData()
        {
            var itemStats = DiContainer.GetService<IItemStatsHandler>()?.GetBodyArmorStats(BodyArmorType.Cloak, Rarity);
            if (itemStats == null)
            {
                // TODO Log
                return;
            }

            BaseModifiers = ModifiersCreator.ItemStatsToModifier(itemStats, this);

            var mediaData = ItemsMediaHandler.Inctance?.GetBodyArmorMediaData(BodyArmorType.Cloak, Rarity);
            if (mediaData == null)
            {
                // TODO Log
                return;
            }
            Icon = mediaData.IconTexture;
            FullImage = mediaData.FullTexture;
            Description = mediaData.Description;
            ItemName = mediaData.Name;
        }
    }
}
