namespace Core.Interfaces.Items
{
    using Enums;

    public interface IItem : IIdentifiable, IDisplayable, IStackable, ITaggable
    {
        Rarity Rarity { get; set; }
        T Copy<T>();
    }
}
