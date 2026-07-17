namespace Core.Entity
{
    using Components;
    using Enums;

    public interface IPlayer : IFightable
    {
        Fractions Fractions { get; }
        string PlayerName { get; }
        IEquipmentComponent EquipmentComponent { get; }
    }
}
