namespace Core.Entity
{
    using Components;

    public interface IPlayer : IFightable
    {
        string Name { get; }
        IEquipmentComponent EquipmentComponent { get; }
    }
}
