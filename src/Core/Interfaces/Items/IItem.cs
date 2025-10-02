namespace Core.Interfaces.Items
{
    using Core.Enums;

    public interface IItem : IIdentifiable, IDisplayable, IStackable
    {
        string InstanceId { get; }
        Rarity Rarity { get; }
        T Copy<T>();
    }
}
