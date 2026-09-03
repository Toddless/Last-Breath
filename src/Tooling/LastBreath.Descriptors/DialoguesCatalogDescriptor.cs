namespace LastBreath.Descriptors
{
    using System;
    using Core.Data.DialogueData;
    using Core.Data.GameData;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>The shape of the Dialogues catalog around its records: one array under a key, and a
    /// dialogue found by the npc it belongs to rather than by an id of its own. What a record is made of
    /// is read off <see cref="DialogueEntry"/> and stated nowhere here.</summary>
    /// <remarks>
    /// Two things the contract cannot carry are worth saying out loud. The routes between nodes —
    /// <see cref="DialogueEntryRuleEntry.Node"/>, <see cref="DialogueOptionEntry.Next"/> and the speech
    /// check's fallback — name a node of the SAME record, which is a reference no catalog answers: the
    /// records say they point nowhere and a test holds the shipped files to it. And the conditions and
    /// actions are free-form json here — the vocabulary an editor draws them from is the narrative
    /// factories' own, not a record of this catalog.
    /// </remarks>
    public sealed class DialoguesCatalogDescriptor : ICatalogDescriptor
    {
        /// <summary>Root key the records sit under — what <see cref="DialoguesData.Dialogues"/> is
        /// written as.</summary>
        public const string RecordsKey = "dialogues";

        /// <summary>Json name of the field a dialogue is found by: the npc that speaks it, one dialogue
        /// per npc definition — what <see cref="DialogueEntry.NpcId"/> is written as.</summary>
        public const string IdField = "npcId";

        public string Catalog => DataCatalog.Dialogues;

        public CatalogSchema Describe(ISchemaBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            RecordSchema dialogue = builder.Record(typeof(DialogueEntry)) with { IdField = IdField };

            return new CatalogSchema(
                RootShape.ArrayUnderKey,
                [new SectionSchema { Key = RecordsKey, Record = dialogue }],
                // Nothing is worded from a dialogue's id: every line and option carries the key it is
                // read under, written out in full.
                [],
                // The catalog ships one file today and is meant to be split by npc or by story line, so
                // a new dialogue may go to a file of its own.
                new FreeFilePlacement());
        }
    }
}
