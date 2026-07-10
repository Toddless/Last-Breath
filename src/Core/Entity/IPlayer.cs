namespace Core.Entity
{
    using Components;

    public interface IPlayer : IFightable
    {
        string PlayerName { get; }
        IEquipmentComponent EquipmentComponent { get; }
    }
}
