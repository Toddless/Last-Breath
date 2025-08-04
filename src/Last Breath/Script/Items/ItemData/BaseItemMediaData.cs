namespace LastBreath.Script.Items.ItemData
{
    using Contracts.Enums;
    using System.Collections.Generic;

    public abstract class BaseItemMediaData
    {
        public Dictionary<AttributeType, ItemResources> AttributeItemResources = [];
        public ItemResources SimpeItemResources { get; set; } = new();

        public ItemMediaData GetAttributeMediadata(AttributeType type, Rarity rarity) => CreateMediaData(AttributeItemResources.GetValueOrDefault(type), rarity);

        public ItemMediaData GetItemMediaData(Rarity rarity) => CreateMediaData(SimpeItemResources, rarity);

        private ItemMediaData CreateMediaData(ItemResources? resources, Rarity rarity) => new()
        {
            Description = resources?.Description.GetValueOrDefault(rarity),
            Name = resources?.Name.GetValueOrDefault(rarity),
            Sound = resources?.Sound.GetValueOrDefault(rarity),
            IconTexture = resources?.IconTexture.GetValueOrDefault(rarity),
            FullTexture = resources?.FullTexture.GetValueOrDefault(rarity),
        };
    }
}
