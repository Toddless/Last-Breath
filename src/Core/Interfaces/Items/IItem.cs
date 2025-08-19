namespace Core.Interfaces.Items
{
    using System.Collections.Generic;
    using Core.Enums;
    using Godot;

    public interface IItem
    {
        string Id { get; }
        int MaxStackSize { get; }
        Texture2D? Icon { get; }
        Texture2D? FullImage {  get; }
        Rarity Rarity { get; }
        string Description { get; }
        string DisplayName { get; }
        List<string> GetItemStatsAsStrings();

        bool HasTag(string tag);
        IItem Copy(bool subresources = false);
    }
}
