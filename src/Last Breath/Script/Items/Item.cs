namespace Playground.Script.Items
{
    using System;
    using System.Collections.Generic;
    using Godot;
    using Playground.Localization;
    using Playground.Script.Enums;

    [GlobalClass]
    public partial class Item : Resource
    {
        private string? _id;
        //  [Export] public string? ItemResourcePath;
        [Export] public LocalizedString? ItemName;
        [Export] public int MaxStackSize = 1;
        [Export] public Texture2D? Icon, FullImage;
        [Export] public GlobalRarity Rarity;
        [Export] public int Quantity = 1;
        [Export] public LocalizedString? Description;

        public string Id => _id ??= SetId();
        private static string SetId()
        {
            // TODO: Later item name is LocalizedString, i need to take an en name
          //  if (string.IsNullOrEmpty(ItemName?.Text)) return string.Empty;
            //return ItemName.Text.Replace(' ', '_');
            return Guid.NewGuid().ToString();
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

        public override int GetHashCode()=>HashCode.Combine(ItemName, Quantity);

        // TODO: Format strings
        public virtual List<string> GetItemStats() => [];
    }
}
