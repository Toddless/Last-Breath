namespace LastBreath.Script.Items
{
    using System;
    using System.Collections.Generic;
    using Godot;
    using Core.Enums;
    using Core.Interfaces.Items;
    using System.Linq;

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
        [Export] public int MaxStackSize { get; set; } = 1;
        [Export] public string[] Tags = [];

        public string DisplayName => GetLocalizedName();

        public string Description => GetLocalizedDescription();

        public Item()
        {

        }

        public Item(string id,
            Rarity rarity,
            Texture2D icon,
            Texture2D fullImage,
            int maxStackSize,
            string[] tags)
        {
            Id = id;
            Rarity = rarity;
            Icon = icon;
            FullImage = fullImage;
            MaxStackSize = maxStackSize;
            Tags = tags;
        }

        public bool Equals(Item other)
        {
            if (other == null || string.IsNullOrEmpty(DisplayName))
            {
                return false;
            }
            return Id.Equals(other.Id) && MaxStackSize == other.MaxStackSize;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            return Equals((Item)obj);
        }

        public override int GetHashCode() => HashCode.Combine(Id);

        // TODO: Format strings
        public virtual List<string> GetItemStatsAsStrings() => [];
        public T Copy<T>(bool subresources = false)
        {
            var duplicate = (IItem)Duplicate(subresources);
            return (T)duplicate;
        }

        public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);
        protected virtual void LoadData()
        {

        }
        private string GetLocalizedName() => TranslationServer.Translate(Id);
        private string GetLocalizedDescription() => TranslationServer.Translate(Id + "_Description");
    }
}
