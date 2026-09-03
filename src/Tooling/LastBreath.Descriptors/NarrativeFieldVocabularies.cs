namespace LastBreath.Descriptors
{
    using System;
    using System.Collections.Generic;
    using Core.Narrative;
    using Tooling.Catalogs;
    using Tooling.Schema.Model;

    /// <summary>
    /// Which keys of the dialogues and the quests are written from the narrative vocabulary. The DTOs hold
    /// them as free json — the shapes they may take are the factories' and not the catalog's — so nothing
    /// in the schema says a key called <c>visibleConditions</c> is a list of conditions. This is where it
    /// is said, once, for every host and every check that has to know.
    /// </summary>
    /// <remarks>Whether a key holds one entry or a list of them is the parser's answer and not a
    /// preference: every key the providers read through the vocabulary's list parsers must be an array,
    /// and an objective's own predicate, read one entry at a time, must be a single one.</remarks>
    public static class NarrativeFieldVocabularies
    {
        /// <summary>Conditions on the greeting of a dialogue and on a route out of a quest stage.</summary>
        public const string Conditions = "conditions";

        /// <summary>Conditions hiding an option of a dialogue entirely.</summary>
        public const string VisibleConditions = "visibleConditions";

        /// <summary>Conditions greying an option of a dialogue out.</summary>
        public const string EnabledConditions = "enabledConditions";

        /// <summary>Conditions under which a quest may be taken at all.</summary>
        public const string AcceptConditions = "acceptConditions";

        /// <summary>The predicate one objective of a quest is met by — the one key of the narrative
        /// written as a single entry rather than as a list.</summary>
        public const string Condition = "condition";

        /// <summary>Actions run by choosing an option of a dialogue, and actions paid as a reward.</summary>
        public const string Actions = "actions";

        /// <summary>Actions run on entering a node of a dialogue or a stage of a quest.</summary>
        public const string OnEnter = "onEnter";

        /// <summary>Actions run on finishing a stage of a quest.</summary>
        public const string OnComplete = "onComplete";

        public const string OnAccept = "onAccept";

        public const string OnDecline = "onDecline";

        public const string OnFail = "onFail";

        /// <summary>Actions run when a speech check of a dialogue is failed.</summary>
        public const string FailActions = "failActions";

        private static readonly VocabularyBinding s_condition =
            new(NarrativeSchemas.Conditions(), NarrativeParameterSchema.TypeKey, List: false);

        private static readonly VocabularyBinding s_conditions = s_condition with { List = true };

        private static readonly VocabularyBinding s_actions =
            new(NarrativeSchemas.Actions(), NarrativeParameterSchema.TypeKey, List: true);

        /// <summary>Every key of the narrative catalogs the schema can only call free json, and what it
        /// actually holds. A key here and not in the DTOs, or the other way round, is an editor either
        /// offering a vocabulary where the game reads none or drawing raw json where it reads one.</summary>
        public static IReadOnlyDictionary<string, VocabularyBinding> Fields { get; } =
            new Dictionary<string, VocabularyBinding>(StringComparer.Ordinal)
            {
                [Conditions] = s_conditions,
                [VisibleConditions] = s_conditions,
                [EnabledConditions] = s_conditions,
                [AcceptConditions] = s_conditions,
                [Condition] = s_condition,
                [Actions] = s_actions,
                [OnEnter] = s_actions,
                [OnComplete] = s_actions,
                [OnAccept] = s_actions,
                [OnDecline] = s_actions,
                [OnFail] = s_actions,
                [FailActions] = s_actions
            };

        /// <summary>
        /// What one field of a record holds, as far as the vocabulary is concerned: nothing at all for the
        /// fields of a catalog, and a binding for the keys written from it.
        /// <para>A condition nested inside another condition — the parts of an <c>AllOf</c>, the one an
        /// <c>Not</c> turns over — says so in the schema itself, whatever it is named, so it is answered
        /// off the schema and not off a table of names. That is what makes the editor recursive without a
        /// list of every key every condition might one day hold conditions under.</para>
        /// </summary>
        public static VocabularyBinding? Resolve(FieldSchema field)
        {
            ArgumentNullException.ThrowIfNull(field);

            if (field.Kind == FieldKind.Object && Nested(field.Record)) return s_condition;
            if (field.Kind == FieldKind.Array && field.Item is { } item && Nested(item.Record)) return s_conditions;

            return field.Kind == FieldKind.Any ? Fields.GetValueOrDefault(field.JsonName) : null;
        }

        /// <summary>Whether a record is the placeholder the adapter writes where a condition holds another
        /// condition: it has no fields of its own, and its real shape is whichever type its own key names.</summary>
        private static bool Nested(RecordSchema? record) =>
            record is { } named
            && string.Equals(named.TypeName, NarrativeParameterSchema.NestedConditionRecord, StringComparison.Ordinal);
    }
}
