using Godot;
using Playground.Script.LootGenerator.BasedOnRarityLootGenerator;
using System;

namespace Playground.Script.Items
{
    [GlobalClass]
    public partial class Item : Resource
    {
        private readonly RandomNumberGenerator randomNumberGenerator = new();

        protected RandomNumberGenerator RandomNumberGenerator
        {
            get { return randomNumberGenerator; }
        }

        public Guid Guid { get; private set; } = Guid.NewGuid();

        [Export]
        public string ItemResourcePath;
        [Export]
        public string ItemName;
        [Export]
        public int StackSize;
        [Export]
        public Texture2D Icon;
        [Export]
        public GlobalRarity Rarity;
        [Export]
        public int Quantity;

        // добавить позже новое поле
        //[Export]
        //public bool IsStackable;
        public Item()
        {
        }

        public Item(string itemName, GlobalRarity rarity, string resourcePath, Texture2D icon, int stackSize, int quantity)
        {
            ItemResourcePath = resourcePath;
            ItemName = itemName;
            StackSize = stackSize;
            Icon = icon;
            Rarity = rarity;
            Quantity = quantity;
        }


        public Item Copy()=> MemberwiseClone() as Item;
    }
}
