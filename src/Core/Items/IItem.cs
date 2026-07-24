namespace Core.Items
{
    using Enums;
    using Interfaces;

    public interface IItem : IIdentifiable, IDisplayable, IStackable, ITaggable
    {
        Rarity Rarity { get; set; }

        /// <summary>Authored base gold price ("basePrice" in the item data); the gold value of a
        /// concrete instance is ALWAYS computed by <c>ItemValuation</c> on top of this. 0 = unpriced
        /// (untradable). Equip items keep the base on their blueprint instead — the default stands.</summary>
        int BasePrice => 0;

        T Copy<T>();
    }
}
