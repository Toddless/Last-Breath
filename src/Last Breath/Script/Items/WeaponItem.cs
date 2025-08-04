namespace LastBreath.Script.Items
{
    using System.Collections.Generic;
    using System.Text;
    using Contracts.Data;
    using Contracts.Enums;
    using Contracts.Interfaces;
    using Godot;
    using LastBreath.Components.Interfaces;
    using LastBreath.Script.Items.ItemData;

    public abstract partial class WeaponItem : EquipItem, IDamageStrategy, IWeaponItem
    {
        protected RandomNumberGenerator Rnd = new();
        protected float BaseAdditionalHitChance { get; private set; }
        protected float BaseCriticalChance { get; private set; }
        protected float BaseCritDamage { get; private set; }
        protected float BaseDamage { get; private set; }

        public int UpgradeLevel { get; private set; } = 0;

        public WeaponType WeaponType { get; private set; }

        protected WeaponItem(Rarity rarity, WeaponType weaponType) : base(rarity, equipmentPart: EquipmentPart.Weapon, type: AttributeType.None)
        {
            WeaponType = weaponType;
            LoadData();
        }

        public float GetBaseCriticalChance() => BaseCriticalChance;
        public float GetBaseCriticalDamage() => BaseCritDamage;
        public float GetBaseExtraHitChance() => BaseAdditionalHitChance;
        public float GetDamage() => BaseDamage;

        public override void UpgradeItemLevel()
        {
            UpgradeLevel++;
            UpdateItem();
        }

        public override List<string> GetItemStatsAsStrings()
        {
            List<string> list = [];
            StringBuilder sb = new();
            sb.AppendLine($"Critical Chance: {Mathf.RoundToInt(BaseCriticalChance * 10)}%");
            sb.AppendLine($"Critical Damage: {BaseCritDamage}%");
            sb.AppendLine($"Additional Hit Chance: {Mathf.RoundToInt(BaseAdditionalHitChance * 10)}%");
            sb.AppendLine($"Damage: {BaseDamage}");
            list.Add(sb.ToString());
            return list;
        }

        protected override void UpdateItem()
        {
            // TODO end this later
            BaseAdditionalHitChance *= 0.01f;
            BaseCriticalChance *= 0.01f;
            BaseDamage *= 0.01f;
            BaseCritDamage *= 0.01f;
        }

        protected override void LoadData()
        {
            var data = DiContainer.GetService<IItemDataProvider<ItemStats>>()?.GetItemData(this);
            if (data == null)
            {
                // TODO Log
                return;
            }
            BaseCriticalChance = Mathf.Max(0, Rnd.RandfRange(From, To) * data.CritChance);
            BaseAdditionalHitChance = Mathf.Max(0, Rnd.RandfRange(From, To) * data.AdditionalHitChance);
            BaseCritDamage = Mathf.Max(0, Mathf.RoundToInt(Rnd.RandfRange(From, To) * data.CritDamage));
            BaseDamage = Mathf.Max(0, Mathf.RoundToInt(Rnd.RandfRange(From, To) * data.Damage));

            var mediaData = DiContainer.GetService<IItemDataProvider<ItemMediaData>>()?.GetItemData(this);
            if (mediaData == null)
            {
                //TODO Log
                return;
            }
            Icon = mediaData.IconTexture;
            FullImage = mediaData.FullTexture;
            ItemName = mediaData.Name;
            SetNewDescription(mediaData.Description);
        }
    }
}
