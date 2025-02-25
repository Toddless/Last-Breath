namespace Playground.Script.Items
{
    using System;
    using Godot;
    using Playground.Script.Enums;

    [GlobalClass]
    public partial class Item : Resource
    {
        #region Export fields
        [Export]
        public string? ItemResourcePath;
        [Export]
        public string? ItemName;
        [Export]
        public int MaxStackSize;
        [Export]
        public Texture2D? Icon;
        [Export]
        public PackedScene? ItemScene;
        [Export]
        public GlobalRarity Rarity;
        [Export]
        public int Quantity;
        // Id for quest items, or something special. By default should be empty
        [Export]
        public string SpecialId = string.Empty;
        #endregion

        public string Guid { get; private set; } = System.Guid.NewGuid().ToString();

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

        public void Add(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount to add must be greater than zero.");
            }
            Quantity += amount;
        }

        public bool Remove(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount to remove must be greater than zero.");
            }
            if (Quantity < amount)
            {
                return false;
            }

            Quantity -= amount;
            return true;
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
