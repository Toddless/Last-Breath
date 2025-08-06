namespace Core.Interfaces.Items
{
    using System.Collections.Generic;
    using Core.Enums;
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
        string Name { get; }
        List<string> GetItemStatsAsStrings();

        IItem Copy(bool subresources = false);
    }
}
