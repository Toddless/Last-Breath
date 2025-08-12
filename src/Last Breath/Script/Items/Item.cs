namespace LastBreath.Script.Items
{
    using System;
    using System.Collections.Generic;
    using Godot;
    using Core.Enums;
    using Core.Interfaces.Items;

    [Tool]
    [GlobalClass]
    public partial class Item : Resource, IItem
    {
        private string _id = string.Empty;
        [Export]
        public string Id
        {
            get => _id;
            protected set
            {
                _id = value;
                LoadData();
            }
        }
        [Export] public Rarity Rarity { get; set; } = Rarity.Rare;
        [Export] public Texture2D? Icon { get; set; }
        [Export] public Texture2D? FullImage { get; set; }
        [Export] public int Quantity { get; set; } = 1;
        [Export] public int MaxStackSize { get; set; } = 1;
        [Export] public string[] Tags = [];

        public string Name => GetLocalizedName();

        public string Description => GetLocalizedDescription();


        public bool Equals(Item other)
        {
            if (other == null || string.IsNullOrEmpty(Name))
            {
                return false;
            }
            return Name.Equals(other.Name) && Quantity == other.Quantity;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            return Equals((Item)obj);
        }

        public override int GetHashCode() => HashCode.Combine(Name, Quantity);

        // TODO: Format strings
        public virtual List<string> GetItemStatsAsStrings() => [];
        public IItem Copy(bool subresources = false) => (IItem)Duplicate(subresources);
        protected virtual void LoadData()
        {

        }
        private string GetLocalizedName() => TranslationServer.Translate(Id);
        private string GetLocalizedDescription() => TranslationServer.Translate(Id + "_Description");
    }
}
