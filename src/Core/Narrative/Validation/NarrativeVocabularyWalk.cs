namespace Core.Narrative.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Reads the conditions and actions written into a dialogue or a quest against the specifications
    /// their factories declare. The one place a narrative entry's keys are walked: which key holds an id,
    /// which one has to be written and which one holds another entry are all questions
    /// <see cref="NarrativeVocabulary"/> already answers, and a second list of them here would be a
    /// language of its own the moment a factory grows a key.
    /// </summary>
    internal sealed class NarrativeVocabularyWalk
    {
        private const string TypeMissingFormat = "no '{0}' is written: nothing says which factory reads the entry";

        private const string TypeUnknownFormat = "'{0}' is a {1} type no factory of the vocabulary reads";

        private const string NotAnObjectFormat = "a {0} is written as {1} rather than as an object";

        private const string NotAnArrayFormat = "the {0}s are written as {1} rather than as an array, so none of them is read";

        private const string RequiredFormat = "'{0}' is required by the '{1}' {2} and is not written";

        private const string EmptyFormat = "'{0}' of the '{1}' {2} is written empty, where a record id is meant";

        private const string UnknownFormat = "'{0}' names '{1}', which is in none of {2}";

        private const string ChoiceFormat = "'{0}' is written '{1}', which is none of {2}";

        private const string NoChoiceFormat = "'{0}' is written empty, where one of {1} is meant";

        private const string UndescribedFormat = "the {0} is not among the catalogs this run read, so every id pointing into it is unanswered";

        private const string ConditionWord = "condition";

        private const string ActionWord = "action";

        /// <summary>How the members of a set — the targets a reference may point into, the words a choice
        /// offers — are listed for a reader.</summary>
        private const string ListSeparator = ", ";

        /// <summary>How the place of one entry inside a list is written.</summary>
        private const string IndexFormat = "{0}[{1}]";

        /// <summary>How one step of a place is written under the one before it. A key under a record and a
        /// section under a catalog are the same step, and one word for both is what keeps a place written
        /// one way wherever it is made.</summary>
        private const string KeyFormat = "{0}/{1}";

        private static readonly IReadOnlyDictionary<string, NarrativeRecordSpec> s_conditions =
            NarrativeVocabulary.Conditions.ToDictionary(record => record.TypeName, StringComparer.Ordinal);

        private static readonly IReadOnlyDictionary<string, NarrativeRecordSpec> s_actions =
            NarrativeVocabulary.Actions.ToDictionary(record => record.TypeName, StringComparer.Ordinal);

        private readonly INarrativeIdSource _ids;
        private readonly ICollection<NarrativeFinding> _findings;
        private readonly ISet<NarrativeReferenceTarget> _undescribed;

        internal NarrativeVocabularyWalk(
            INarrativeIdSource ids, ICollection<NarrativeFinding> findings, ISet<NarrativeReferenceTarget> undescribed)
        {
            _ids = ids;
            _findings = findings;
            _undescribed = undescribed;
        }

        /// <summary>The place one element of a list is written at.</summary>
        internal static string At(string where, int index) => string.Format(IndexFormat, where, index);

        /// <summary>The place one key of a record is written at.</summary>
        internal static string Under(string where, string key) => string.Format(KeyFormat, where, key);

        /// <summary>Every condition of a list, or nothing at all when the list is not written.</summary>
        internal void Conditions(JToken? array, string where) => List(array, where, s_conditions, ConditionWord);

        internal void Actions(JToken? array, string where) => List(array, where, s_actions, ActionWord);

        /// <summary>One condition written on its own — a quest objective's predicate.</summary>
        internal void Condition(JToken? token, string where)
        {
            if (token is null) return;

            Entry(token, where, s_conditions, ConditionWord);
        }

        /// <summary>One id written where a record is meant, checked against everywhere it may point. Held
        /// here rather than beside the callers because the data markup on a DTO field and a vocabulary
        /// parameter ask the same question of the same source.</summary>
        internal void Reference(string id, IReadOnlyList<NarrativeReferenceTarget> targets, string where, string named)
        {
            if (targets.Count == 0) return;

            List<NarrativeReferenceTarget> unread = [.. targets.Where(target => !_ids.Describes(target))];
            foreach (NarrativeReferenceTarget target in unread) Undescribed(target);

            // A target this run cannot read leaves the answer unknown: calling the id broken would be the
            // run blaming the author for a catalog it never opened.
            if (unread.Count > 0) return;

            if (targets.Any(target => _ids.Knows(target, id))) return;

            _findings.Add(new NarrativeFinding(
                NarrativeFindingKind.UnknownReference, where, string.Format(UnknownFormat, named, id, Named(targets))));
        }

        /// <summary>The word a token is written as: a value reads as itself, and anything else as the shape
        /// it was written in — the parser reads no member out of either, and naming the shape is what tells
        /// a typo apart from a key written as a whole object.</summary>
        private static string Word(JToken value) => value is JValue { Value: { } raw } ? raw.ToString() ?? string.Empty : value.Type.ToString();

        /// <summary>One word written where a member of a named set is meant — a vocabulary parameter
        /// offering choices, a record field the game parses into an enum. Both are read strictly: a word
        /// that is none of them throws in the parser and takes the whole record out of the game, so it is
        /// named here rather than left to a bare drop.</summary>
        internal void Member(string? value, IReadOnlyList<string> members, string where, string named)
        {
            if (members.Count == 0) return;

            if (value is not { Length: > 0 })
            {
                _findings.Add(new NarrativeFinding(
                    NarrativeFindingKind.UnknownChoice, where, string.Format(NoChoiceFormat, named, Listed(members))));
                return;
            }

            if (members.Contains(value, StringComparer.Ordinal)) return;

            _findings.Add(new NarrativeFinding(
                NarrativeFindingKind.UnknownChoice, where, string.Format(ChoiceFormat, named, value, Listed(members))));
        }

        /// <summary>The members of a set, as one reader-facing phrase.</summary>
        private static string Listed(IReadOnlyList<string> members) => string.Join(ListSeparator, members);

        /// <summary>Where a set of targets points, as one reader-facing phrase.</summary>
        private static string Named(IReadOnlyList<NarrativeReferenceTarget> targets) =>
            Listed([.. targets.Select(Named)]);

        private static string Named(NarrativeReferenceTarget target) =>
            target.Section is { } section ? Under(target.Catalog, section) : target.Catalog;

        /// <summary>Every entry of a list. A key written as anything but a list is said out loud: the
        /// parsers read nothing out of it, so a condition written as a bare object gates nothing and a
        /// silent walk would leave the author reading a clause the game never asks.</summary>
        private void List(JToken? array, string where, IReadOnlyDictionary<string, NarrativeRecordSpec> vocabulary, string word)
        {
            if (array is null or { Type: JTokenType.Null }) return;

            if (array is not JArray entries)
            {
                _findings.Add(new NarrativeFinding(
                    NarrativeFindingKind.UnknownEntry, where, string.Format(NotAnArrayFormat, word, array.Type)));
                return;
            }

            for (int index = 0; index < entries.Count; index++)
                Entry(entries[index], At(where, index), vocabulary, word);
        }

        private void Entry(JToken token, string where, IReadOnlyDictionary<string, NarrativeRecordSpec> vocabulary, string word)
        {
            if (token is not JObject json)
            {
                _findings.Add(new NarrativeFinding(
                    NarrativeFindingKind.UnknownEntry, where, string.Format(NotAnObjectFormat, word, token.Type)));
                return;
            }

            string type = json.Value<string>(NarrativeParameterSchema.TypeKey) ?? string.Empty;
            if (type.Length == 0)
            {
                _findings.Add(new NarrativeFinding(
                    NarrativeFindingKind.UnknownEntry, where, string.Format(TypeMissingFormat, NarrativeParameterSchema.TypeKey)));
                return;
            }

            if (!vocabulary.TryGetValue(type, out NarrativeRecordSpec? spec))
            {
                _findings.Add(new NarrativeFinding(
                    NarrativeFindingKind.UnknownEntry, where, string.Format(TypeUnknownFormat, type, word)));
                return;
            }

            foreach (NarrativeParameterSpec parameter in spec.Parameters)
                Parameter(json, parameter, type, where, word);
        }

        private void Parameter(JObject json, NarrativeParameterSpec parameter, string type, string where, string word)
        {
            JToken? value = json[parameter.JsonName];

            if (value is null or { Type: JTokenType.Null })
            {
                if (parameter.Required)
                    _findings.Add(new NarrativeFinding(
                        NarrativeFindingKind.MissingParameter,
                        where,
                        string.Format(RequiredFormat, parameter.JsonName, type, word)));

                return;
            }

            string at = Under(where, parameter.JsonName);

            switch (parameter.Kind)
            {
                case NarrativeParameterKind.Reference:
                    Written(value, parameter, type, at, where, word);
                    break;
                case NarrativeParameterKind.References:
                    foreach (JToken element in value) Written(element, parameter, type, at, where, word);
                    break;
                case NarrativeParameterKind.NestedCondition:
                    Entry(value, at, s_conditions, ConditionWord);
                    break;
                case NarrativeParameterKind.NestedConditions:
                    List(value, at, s_conditions, ConditionWord);
                    break;
                case NarrativeParameterKind.Choice:
                    Member(Word(value), parameter.Choices, at, parameter.JsonName);
                    break;
                default:
                    break;
            }
        }

        /// <summary>One written reference of an entry: an id nobody typed is the key left unwritten, and an
        /// id typed is held against everywhere the parameter says it may point.</summary>
        private void Written(JToken value, NarrativeParameterSpec parameter, string type, string at, string where, string word)
        {
            string id = value.Value<string>() ?? string.Empty;

            if (id.Length == 0)
            {
                _findings.Add(new NarrativeFinding(
                    NarrativeFindingKind.MissingParameter, where, string.Format(EmptyFormat, parameter.JsonName, type, word)));
                return;
            }

            Reference(id, parameter.Targets, at, parameter.JsonName);
        }

        /// <summary>A catalog this run cannot read, said once however many ids point into it: the fact is
        /// about the run, and repeating it per reference would bury what the data owes.</summary>
        private void Undescribed(NarrativeReferenceTarget target)
        {
            if (!_undescribed.Add(target)) return;

            _findings.Add(new NarrativeFinding(
                NarrativeFindingKind.UndescribedTarget, Named(target), string.Format(UndescribedFormat, Named(target))));
        }
    }
}
