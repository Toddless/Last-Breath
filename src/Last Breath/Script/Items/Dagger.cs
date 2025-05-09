namespace Playground.Script.Items
{
    using Godot;
    using Playground.Components.Interfaces;
    using Playground.Script.Enums;
    using Playground.Script.Items.ItemData;

    public partial class Dagger : WeaponItem, IDamageStrategy
    {
        public Dagger(GlobalRarity rarity) : base()
        {
            LoadData(rarity);
        }

        public float GetBaseCriticalChance() => BaseCriticalChance;
        public float GetBaseCriticalDamage() => BaseCritDamage;
        public float GetBaseExtraHitChance() => BaseAdditionalHitChance;
        public float GetDamage() => BaseDamage;

        protected override void LoadData(GlobalRarity rarity)
        {
            var data = DiContainer.GetService<IItemStatsHandler>()?.GetWeaponStats(WeaponType.Dagger, rarity);
            if(data == null)
            {
                // TODO Log
                return;
            }
            BaseCriticalChance = Mathf.Max(0, Rnd.RandfRange(From, To) * data.CritChance);
            BaseAdditionalHitChance = Mathf.Max(0, Rnd.RandfRange(From, To) * data.AdditionalHitChance);
            BaseCritDamage = Mathf.Max(0, Mathf.RoundToInt(Rnd.RandfRange(From, To) * data.CritDamage));
            BaseDamage = Mathf.Max(0, Mathf.RoundToInt(Rnd.RandfRange(From, To) * data.BaseDamage));
            var mediaData = ItemsMediaHandler.Inctance?.GetWeaponMediaData(WeaponType.Dagger, rarity);
            if (mediaData == null) return;

            Icon = mediaData.Texture;
        }
    }
}
