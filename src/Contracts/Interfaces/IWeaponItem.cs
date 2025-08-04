namespace Contracts.Interfaces
{
    using Contracts.Enums;

    public interface IWeaponItem : IEquipItem
    {
        WeaponType WeaponType { get; }
    }
}
