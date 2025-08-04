namespace Contracts.Interfaces
{
    using System.Collections.Generic;
    using Contracts.Enums;
    using Godot;

    public interface IItem
    {
        string Id { get; }
        int Quantity { get; set; }
        int MaxStackSize { get; }
        Texture2D? Icon { get; }
        Texture2D? FullImage {  get; }
        Rarity Rarity { get; }
        string Description { get; }

        IItem CopyItem(bool subresources = false);
        List<string> GetItemStatsAsStrings();
    }
}
