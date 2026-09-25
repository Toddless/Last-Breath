namespace LastBreath.Descriptors
{
    using System;
    using Core.Battle.Skills;
    using Core.Data.GameData;
    using Core.Data.Schema;
    using Core.Localization;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>The shape of the PassiveSkills catalog: one array under a key, each passive found by its
    /// own id and named and described in the localization by it. What a record is made of is read off
    /// <see cref="PassiveCatalogEntryDto"/> and stated nowhere here.</summary>
    /// <remarks>
    /// The catalog is a COPY of a registry that lives battle-side, where the authoring tool cannot reach
    /// it: a record is the id a grant may write and the names of the numbers that grant must carry, and
    /// nothing else. Two things follow. The fields are words the factory reads and no catalog holds them,
    /// the way a grant's payload keys are; and the named entries are not the whole answer — the stat
    /// family (<see cref="StatPassiveGrammar.IdPrefix"/>) is answered by its PREFIX instead, its ids
    /// invented by the author, so an id under it is a passive this catalog will never write down.
    /// </remarks>
    public sealed class PassiveSkillsCatalogDescriptor : ICatalogDescriptor
    {
        /// <summary>Root key the records sit under — what <see cref="PassiveCatalogDto.Passives"/> is
        /// written as.</summary>
        public const string RecordsKey = "passives";

        /// <summary>Json name of the field carrying a record's id — what
        /// <see cref="PassiveCatalogEntryDto.Id"/> is written as.</summary>
        public const string IdField = "id";

        /// <summary>Json name of the list naming the numbers the passive's factory reads — the NAMES a
        /// grant's payload has to be keyed by, never the numbers themselves, which are the grant's. What
        /// <see cref="PassiveCatalogEntryDto.Fields"/> is written as.</summary>
        public const string FieldsField = "fields";

        /// <summary>The one file of the catalog. Written out rather than taken from the catalog's name:
        /// the folder is named for the records it holds and the file for the document, and they are two
        /// different facts.</summary>
        public const string FileName = "PassiveCatalog";

        public string Catalog => DataCatalog.PassiveSkills;

        public CatalogSchema Describe(ISchemaBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            RecordSchema passive = builder.Record(typeof(PassiveCatalogEntryDto)) with { IdField = IdField };

            return new CatalogSchema(
                RootShape.ArrayUnderKey,
                [new SectionSchema { Key = RecordsKey, Record = passive }],
                // A name and a description: a passive is read on the node that sells it and in the tooltip
                // of the item that grants it, not only listed.
                [LocalizedKeyAttribute.NoSuffix, LocalizationService.DescriptionSuffix],
                new SingleFilePlacement { FileName = FileName });
        }
    }
}
