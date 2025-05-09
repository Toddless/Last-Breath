namespace Playground.Script.Items.ItemData
{
    using Playground.Script.Enums;
    using System.Collections.Generic;

    public abstract class BaseItemMediaData
    {
        public Dictionary<AttributeType, ItemResources> AttributeItemResources = [];
        public ItemResources SimpeItemResources { get; set; } = new();

        public ItemMediaData GetAttributeMediadata(AttributeType type, GlobalRarity rarity) => CreateMediaData(AttributeItemResources.GetValueOrDefault(type), rarity);

        public ItemMediaData GetItemMediaData(GlobalRarity rarity) => CreateMediaData(SimpeItemResources, rarity);

        private ItemMediaData CreateMediaData(ItemResources? resources, GlobalRarity rarity) => new()
        {
            Description = resources?.Description.GetValueOrDefault(rarity),
            Name = resources?.Name.GetValueOrDefault(rarity),
            Sound = resources?.Sound.GetValueOrDefault(rarity),
            Texture = resources?.Texture.GetValueOrDefault(rarity),
        };
    }
}
