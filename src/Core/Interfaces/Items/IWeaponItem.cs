namespace Core.Interfaces.Items
{
    using Enums;

    public interface IWeaponItem : IEquipItem
    {
        WeaponType WeaponType { get; }
    }
}
