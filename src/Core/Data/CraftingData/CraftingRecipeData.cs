namespace Core.Data.CraftingData
{
    using System.Collections.Generic;
    using Enums;
    using GameData;
    using Newtonsoft.Json;
    using Schema;

    public record CraftingRecipeData
    {
        [JsonProperty("id")] public string Id { get; init; } = string.Empty;

        /// <summary>The equipment template the recipe mints: the blueprint is looked up by this id and the
        /// item is born through the minter, so a recipe naming no template creates nothing.</summary>
        [JsonProperty("resultItemId")]
        [CatalogRef(DataCatalog.EquipItems)]
        public string ResultItemId { get; init; } = string.Empty;

        [JsonProperty("tags")] public string[] Tags { get; init; } = [];
        [JsonProperty("rarity")][EnumOf(typeof(Rarity))] public string Rarity { get; init; } = string.Empty;
        [JsonProperty("unlockAtMastery")] public int? UnlockAtMastery { get; init; }
        [JsonProperty("basePrice")] public int BasePrice { get; init; }
        [JsonProperty("itemType")][EnumOf(typeof(ItemType))] public string ItemType { get; init; } = string.Empty;
        [JsonProperty("requirements")] public List<RecipeRequirementsData> Requirements { get; init; } = [];

        /// <summary>Which resources the optional slots are meant for, as words the resources carry rather
        /// than ids of the material categories: nothing in the game reads the field today, so the words are
        /// carried through as written and answered by no catalog.</summary>
        [JsonProperty("optionalResourceCategories")]
        [NotARef]
        public string[] OptionalResourceCategories { get; init; } = [];
    }
}
