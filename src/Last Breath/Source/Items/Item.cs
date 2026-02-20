namespace LastBreath.Source.Items
{
    using System;
    using System.Linq;
    using Core.Enums;
    using Core.Interfaces.Items;
    using Godot;
    using Utilities;

    [Tool]
    [GlobalClass]
    public partial class Item : Resource, IItem
    {
        [Export] public string Id { get; protected set; } = string.Empty;
        [Export] public Rarity Rarity { get; set; } = Rarity.Rare;
        [Export] public Texture2D? Icon { get; set; }
        [Export] public Texture2D? FullImage { get; set; }
        [Export] public int MaxStackSize { get; set; } = 1;
        [Export] public string[] Tags { get; protected set; } = [];

        public string InstanceId { get; } = Guid.NewGuid().ToString();
        public bool IsSame(string otherId) => throw new NotImplementedException();

        public string DisplayName => Localization.Localize(Id);

        public string Description => Localization.LocalizeDescription(Id);

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

        public bool Equals(IItem other)
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

            return Equals((IItem)obj);
        }

        public override int GetHashCode() => HashCode.Combine(Id);

        public T Copy<T>()
        {
            var duplicate = (IItem)DuplicateDeep();
            return (T)duplicate;
        }

        public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase);
    }
}
