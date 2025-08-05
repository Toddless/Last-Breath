namespace LastBreath.Script.Items
{
    using System;
    using System.Collections.Generic;
    using Godot;
    using LastBreath.Localization;
    using Core.Enums;
    using Core.Interfaces;

    [GlobalClass]
    public partial class Item : Resource, IItem
    {
        private string? _id;
        [Export] private LocalizedString? _description;
        [Export] private LocalizedString? _name;

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

        public string Name => _name?.Text ?? string.Empty;
        public string Description => _description?.Text ?? string.Empty;
        public string Id => _id ??= SetId();


        public bool Equals(Item other)
        {
            if (other == null || _name == null)
            {
                return false;
            }
            return _name.Equals(other._name) && Quantity == other.Quantity;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            return Equals((Item)obj);
        }

        public override int GetHashCode() => HashCode.Combine(_name, Quantity);

        // TODO: Format strings
        public virtual List<string> GetItemStatsAsStrings() => [];
        public void SetDescription(LocalizedString? description) => _description = description;
        public virtual IItem CopyItem(bool subresources = false) => (IItem)Duplicate(subresources);
        public void SetName(LocalizedString? name) => _name = name;
        private string SetId() => $"{this.GetType().Name}_{Rarity}";
    }
}
