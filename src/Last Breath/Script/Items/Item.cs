namespace LastBreath.Script.Items
{
    using System;
    using System.Collections.Generic;
    using Godot;
    using Contracts.Interfaces;
    using Contracts.Enums;
    using LastBreath.Localization;

    [GlobalClass]
    public partial class Item : Resource, IItem
    {
        private string? _id;
        [Export]
        private LocalizedString? _description;

        [Export]
        public Rarity Rarity { get; set; }
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
        public string Description => _description?.Text ?? string.Empty;
        public string Id => _id ??= SetId();


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
        public void SetNewDescription(LocalizedString? description) => _description = description;
        public virtual IItem CopyItem(bool subresources = false) => (IItem)Duplicate(subresources);
        private string SetId() => $"{this.GetType().Name}_{Rarity}";
    }
}
