namespace Core.Interfaces.Items
{
    using Core.Enums;

    public interface IWeaponItem : IEquipItem
    {
        WeaponType WeaponType { get; }
    }
}
