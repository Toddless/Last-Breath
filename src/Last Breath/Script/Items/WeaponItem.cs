namespace Playground.Script.Items
{
    using Playground.Script.Enums;

    public abstract partial class WeaponItem : EquipItem
    {
        protected float BaseAdditionalHitChance;
        protected float BaseCriticalChance;
        protected float BaseCritDamage;
        protected float BaseDamage;

        protected WeaponItem()
        {
            EquipmentPart = EquipmentPart.Weapon;
        }
    }
}
