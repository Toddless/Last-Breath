namespace Core.Data.LootTable
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public record LootTableTierData
    {
        [JsonProperty("tier")] public int Tier { get; init; }

        /// <summary>The seats of this tier. Read through <see cref="TableRecordsConverter"/>: a
        /// position must say what it drops and what it costs before it is allowed to take budget.</summary>
        [JsonProperty("items")]
        [JsonConverter(typeof(TableRecordsConverter))]
        public List<TableRecord> Items { get; init; } = [];
    };
}
