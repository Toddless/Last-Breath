namespace LastBreath.Descriptors
{
    using System;
    using Core.Data.GameData;
    using Core.Data.QuestData;
    using Core.Data.Schema;
    using Core.Localization;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>The shape of the Quests catalog around its records: one array under a key, each quest
    /// found by its own id and named and described in the localization by it. What a record is made of is
    /// read off <see cref="QuestEntry"/> and stated nowhere here.</summary>
    /// <remarks>
    /// Two things the contract cannot carry are worth saying out loud. A quest words more than its own
    /// name — a stage, an objective and an ending are read under keys built from the id of the quest AND
    /// the id of the nested record — and the suffixes here hang off a record's own id alone, so those
    /// three go unsaid. And the routes out of a stage name a stage of the SAME quest, which is a
    /// reference no catalog answers: the record says it points nowhere and a test holds the shipped
    /// files to it.
    /// </remarks>
    public sealed class QuestsCatalogDescriptor : ICatalogDescriptor
    {
        /// <summary>Root key the records sit under — what <see cref="QuestsData.Quests"/> is written
        /// as.</summary>
        public const string RecordsKey = "quests";

        /// <summary>Json name of the field carrying a record's id — what <see cref="QuestEntry.Id"/> is
        /// written as.</summary>
        public const string IdField = "id";

        public string Catalog => DataCatalog.Quests;

        public CatalogSchema Describe(ISchemaBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            RecordSchema quest = builder.Record(typeof(QuestEntry)) with { IdField = IdField };

            return new CatalogSchema(
                RootShape.ArrayUnderKey,
                [new SectionSchema { Key = RecordsKey, Record = quest }],
                // A name and a description: the journal shows both of them for the quest itself.
                [LocalizedKeyAttribute.NoSuffix, LocalizationService.DescriptionSuffix],
                // The catalog ships one file today and is meant to be split by story line, so a new
                // quest may go to a file of its own.
                new FreeFilePlacement());
        }
    }
}
