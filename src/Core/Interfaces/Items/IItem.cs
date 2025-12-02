namespace Core.Interfaces.Items
{
    using Enums;

    public interface IItem : IIdentifiable, IDisplayable, IStackable, ITaggable
    {
        string InstanceId { get; }
        Rarity Rarity { get; set; }
        T Copy<T>();
    }
}
