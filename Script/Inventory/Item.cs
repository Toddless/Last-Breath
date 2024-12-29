namespace Playground.Script.Items
{
    using Godot;
    using Playground.Script.Enums;
    using System;

    [GlobalClass]
    public partial class Item : Resource
    {
        #region Private fields
        private readonly RandomNumberGenerator _randomNumberGenerator = new();
        #endregion

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
        #endregion

        #region Properties
        protected RandomNumberGenerator RandomNumberGenerator
        {
            get { return _randomNumberGenerator; }
        }
        public Guid Guid { get; private set; } = Guid.NewGuid();
        #endregion

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
        public Item? Copy() => MemberwiseClone() as Item;
    }
}
