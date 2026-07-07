namespace Core.Items
{
    using Enums;
    using Interfaces;

    public interface IItem : IIdentifiable, IDisplayable, IStackable, ITaggable
    {
        Rarity Rarity { get; set; }
        T Copy<T>();
    }
}
