namespace Core.Inventory
{
    using System;
    using Enums;
    using Items;

    /// <summary>A bag whose borrowed slot views report clicks on held items. Windows that turn a
    /// click into an action of their own (the trade window puts the clicked item into the deal)
    /// listen here without naming the concrete service.</summary>
    public interface IItemInteractionSource
    {
        /// <summary>A held item was clicked in a slot view, resolved to the live instance.</summary>
        event Action<IItem, MouseInteractions>? ItemInteraction;
    }
}
