namespace Playground.Script.Items
{
    using System;
    using Godot;
    using Playground.Localization;
    using Playground.Script.Enums;

    [GlobalClass]
    public partial class Item : Resource
    {
        private string _id = string.Empty;
        [Export]
        public string? ItemResourcePath;
        [Export]
        public string? ItemName;
        [Export]
        public int MaxStackSize;
        [Export]
        public Texture2D? Icon;
        [Export]
        public GlobalRarity Rarity;
        [Export]
        public int Quantity;
        [Export]
        public ItemType Type;
        [Export]
        public LocalizedString Description;

        public string Id
        {
            get
            {
                if (string.IsNullOrEmpty(_id))
                {
                    _id = !string.IsNullOrEmpty(ItemResourcePath)
                        ? ItemResourcePath.GetHashCode().ToString()
                        : "-1";
                }
                return _id;
            }
        }

        public Item(string itemName, GlobalRarity rarity, string resourcePath, Texture2D? icon, int stackSize, int quantity)
        {
            ItemResourcePath = resourcePath;
            ItemName = itemName;
            MaxStackSize = stackSize;
            Icon = icon;
            Rarity = rarity;
            Quantity = quantity;
        }

        public Item()
        {
        }

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

        public override int GetHashCode()
        {
            return HashCode.Combine(ItemName, Quantity);
        }
    }
}
