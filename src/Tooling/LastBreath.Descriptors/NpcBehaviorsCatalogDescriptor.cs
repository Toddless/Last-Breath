namespace LastBreath.Descriptors
{
    using System;
    using Core.Data.GameData;
    using Core.Data.NpcData;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>The shape of the NpcBehaviors catalog around its records: one array under a key, one file,
    /// and no author-facing text at all. What an archetype is made of is read off
    /// <see cref="NpcBehaviorData"/> and stated nowhere here.</summary>
    /// <remarks>A record is found by its own id, but the id is not what reaches the game: the provider
    /// keys the archetypes by the stance each names, and an npc gets the one its stance answers to. The id
    /// stays the record's address for the tool, which is what a list of records needs to be edited at all.</remarks>
    public sealed class NpcBehaviorsCatalogDescriptor : ICatalogDescriptor
    {
        /// <summary>Root key the records sit under — what <see cref="NpcBehaviorsData.Behaviors"/> is
        /// written as.</summary>
        public const string RecordsKey = "behaviors";

        /// <summary>Json name of the field carrying a record's id — what
        /// <see cref="NpcBehaviorData.Id"/> is written as.</summary>
        public const string IdField = "id";

        /// <summary>Json name of the field naming the stance an archetype answers for — what
        /// <see cref="NpcBehaviorData.Stance"/> is written as.</summary>
        public const string StanceField = "stance";

        /// <summary>Json name of the field holding the weighted casts of an archetype — what
        /// <see cref="NpcBehaviorData.Abilities"/> is written as.</summary>
        public const string AbilitiesField = "abilities";

        /// <summary>The one file of the catalog. Written out rather than taken from the catalog's name:
        /// the folder is named for the records it holds and the file for one of them, and they are two
        /// different facts.</summary>
        public const string FileName = "NpcBehavior";

        public string Catalog => DataCatalog.NpcBehaviors;

        public CatalogSchema Describe(ISchemaBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            RecordSchema behavior = builder.Record(typeof(NpcBehaviorData)) with { IdField = IdField };

            return new CatalogSchema(
                RootShape.ArrayUnderKey,
                [new SectionSchema { Key = RecordsKey, Record = behavior }],
                // An archetype is scored against and never shown: nothing about it reaches the player.
                [],
                new SingleFilePlacement { FileName = FileName });
        }
    }
}
