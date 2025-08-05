namespace Core.Interfaces
{
    using Core.Enums;

    public interface IWeaponItem : IEquipItem
    {
        WeaponType WeaponType { get; }
    }
}
