namespace LastBreath.Script.Items
{
    using System;
    using System.Collections.Generic;
    using Godot;
    using LastBreath.Localization;
    using Contracts.Interfaces;
    using Contracts.Enums;

    [GlobalClass]
    public partial class Item : Resource, IItem
    {
        private string? _id;
        [Export] public LocalizedString? Description;
        [Export] public GlobalRarity Rarity;

        [Export]
        public Texture2D? Icon { get; set; }
        [Export]
        public Texture2D? FullImage { get; set; }
        [Export]
        public int Quantity { get; set; } = 1;
        [Export]
        public int MaxStackSize { get; set; } = 1;
        [Export]
        public LocalizedString? ItemName { get; set; }

        public string Id => _id ??= SetId();


        private string SetId() => $"{ItemName?.Text}_{Rarity}";

        public bool Equals(Item other)
        {
            if (other == null || ItemName == null)
            {
                return false;
            }
            return ItemName.Equals(other.ItemName) && Quantity == other.Quantity;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            return Equals((Item)obj);
        }

        public override int GetHashCode() => HashCode.Combine(ItemName, Quantity);

        // TODO: Format strings
        public virtual List<string> GetItemStatsAsStrings() => [];
    }
}
