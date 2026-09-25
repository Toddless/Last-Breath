namespace LastBreath.Descriptors
{
    using System;
    using Core.Data.GameData;
    using Core.Localization;
    using Core.Modifiers.Conditions;
    using Tooling.Schema;
    using Tooling.Schema.Model;

    /// <summary>The shape of the Conditions catalog: one array under a key, each predicate found by its
    /// own id, and nine shapes told apart by the field naming the predicate. What each shape is made of is
    /// read off the forms in <c>ConditionForms</c> and stated nowhere here.</summary>
    /// <remarks>
    /// The one described catalog the game parses without a DTO: a record is read as raw json and handed to
    /// whichever factory the type names, so nothing here can be read off the type the file is deserialized
    /// into — there is none. The forms stand in for it, and a test in the game suite holds them against the
    /// names the parser reads by.
    /// <para>Every form carries the two keys a condition begins with — <c>ConditionShape</c> — so that an
    /// entry drawn from a form is one a line can name and a factory can pick. The inversion stays on the
    /// record alone: the file writes it after whatever the type asked for, and a key order built from the
    /// forms would carry it forward to where no record on disk has it.</para>
    /// <para>The player-facing half is not stated: a condition is worded under a key DERIVED from its id
    /// with a PREFIX (<see cref="ConditionalLineText"/>), and the contract builds a key by appending a
    /// suffix. Saying nothing is the honest answer — a suffix invented here would name a key nobody
    /// writes.</para>
    /// </remarks>
    public sealed class ConditionsCatalogDescriptor : ICatalogDescriptor
    {
        /// <summary>Root key the records sit under. The parser's own name for it: both sides would
        /// otherwise spell the word separately.</summary>
        public const string RecordsKey = ConditionFields.Entries;

        /// <summary>Json name of the field carrying a record's id.</summary>
        public const string IdField = ConditionFields.Id;

        /// <summary>Json name of the field whose value picks the shape — which predicate the record
        /// builds.</summary>
        public const string TypeField = ConditionFields.Type;

        /// <summary>Json name of the field inverting the predicate. Shared by every type and written last,
        /// which is why it belongs to the record rather than to a form.</summary>
        public const string NegateField = ConditionFields.Negate;

        /// <summary>Json name of the vital a resource predicate follows.</summary>
        public const string ResourceField = ConditionFields.Resource;

        /// <summary>Json name of the share of that vital's maximum a line stops at.</summary>
        public const string ValueField = ConditionFields.Value;

        /// <summary>Json name of the edge of the vital a boundary predicate sits on.</summary>
        public const string StateField = ConditionFields.State;

        /// <summary>Json name of the statuses and group aliases a mask predicate is written with.</summary>
        public const string StatusesField = ConditionFields.Statuses;

        /// <summary>Json name of the field saying which carried effects are counted.</summary>
        public const string ScopeField = ConditionFields.Scope;

        /// <summary>Json name of the effect whose stacks are counted.</summary>
        public const string EffectIdField = ConditionFields.EffectId;

        /// <summary>Json name of how many the counting predicates must find.</summary>
        public const string CountField = ConditionFields.Count;

        /// <summary>Json name of the stance the owner has to be in.</summary>
        public const string StanceField = ConditionFields.Stance;

        /// <summary>Json name of the action a turn-scoped predicate counts.</summary>
        public const string ActionField = ConditionFields.Action;

        /// <summary>Json name of the turn of the battle a line stops at.</summary>
        public const string TurnField = ConditionFields.Turn;

        /// <summary>The one file of the catalog. Written out rather than taken from the catalog's name:
        /// the two agree today and are two different facts.</summary>
        public const string FileName = "Conditions";

        public string Catalog => DataCatalog.Conditions;

        public CatalogSchema Describe(ISchemaBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            // Registered before the record is read: shapes are applied where the record is BUILT. The two
            // families that read about the fighter being hit rather than about the owner share their form
            // with the owner-side one — the record is written the same way whichever it asks about.
            RecordSchema share = Form(builder, typeof(ConditionOverAResourceShare));
            RecordSchema statuses = Form(builder, typeof(ConditionOverStatuses));

            builder.Polymorphic(typeof(ConditionRecord), new VariantSet(
                [
                    Shape(ConditionTypes.ResourceThreshold, share),
                    Shape(ConditionTypes.TargetResourceThreshold, share),
                    Shape(ConditionTypes.ResourceState, Form(builder, typeof(ConditionOverAResourceBoundary))),
                    Shape(ConditionTypes.Status, statuses),
                    Shape(ConditionTypes.TargetStatus, statuses),
                    Shape(ConditionTypes.Effect, Form(builder, typeof(ConditionOverCarriedEffects))),
                    Shape(ConditionTypes.Stance, Form(builder, typeof(ConditionOverTheStance))),
                    Shape(ConditionTypes.TurnAction, Form(builder, typeof(ConditionOverTurnActions))),
                    Shape(ConditionTypes.BattleTurn, Form(builder, typeof(ConditionOverTheBattleTurn)))
                ],
                TypeField));

            RecordSchema condition = builder.Record(typeof(ConditionRecord)) with { IdField = IdField };

            return new CatalogSchema(
                RootShape.ArrayUnderKey,
                [new SectionSchema { Key = RecordsKey, Record = condition }],
                // Read by the player only as a clause inside the line it gates, under a key its id is the
                // tail of rather than the head.
                [],
                new SingleFilePlacement { FileName = FileName });
        }

        /// <summary>One form, found by the same field the record is: a shape of a condition IS a condition,
        /// and its id names itself rather than anything in another catalog. Built once per form so that the
        /// two shared by a pair of families stay one record between them.</summary>
        private static RecordSchema Form(ISchemaBuilder builder, Type form) =>
            builder.Record(form) with { IdField = IdField };

        private static VariantSchema Shape(string type, RecordSchema record) =>
            new() { DiscriminatorValue = type, Record = record };
    }
}
