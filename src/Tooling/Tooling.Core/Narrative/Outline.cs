namespace Tooling.Narrative
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;
    using Tooling.Json;
    using Tooling.Schema.Model;
    using static Tooling.Text.Format;

    /// <summary>What one row of an outline stands for, so a host may draw the rows of a kind alike
    /// without reading their labels back.</summary>
    public enum OutlineKind
    {
        /// <summary>The dialogue itself, which every row below it belongs to.</summary>
        Dialogue,

        /// <summary>The quest itself.</summary>
        Quest,

        /// <summary>A collection of the record, named and counted; it holds rows and is not one.</summary>
        Group,

        EntryRule,
        Node,
        Line,
        Option,
        Stage,
        Objective,
        Transition,
        Outcome
    }

    /// <summary>
    /// One row of the outline of a narrative record: where it is written, what it is called, what it
    /// stands for, the localization key of the text it shows, and the rows under it.
    /// </summary>
    /// <remarks>The value is not held, only addressed: the row is the author's way to a place in the
    /// document, and an inspector opened on it edits what stands there now.</remarks>
    public sealed record OutlineNode
    {
        public required JsonPointer Pointer { get; init; }

        public required string Label { get; init; }

        public required OutlineKind Kind { get; init; }

        /// <summary>The localization key of the text this row shows to a player; empty for a row that
        /// carries no author-facing text of its own.</summary>
        public string Key { get; init; } = string.Empty;

        /// <summary>The schema of what stands at <see cref="Pointer"/>, for an inspector to draw it
        /// with. Null where the address is not a record — a collection, or an element the build has no
        /// schema for.</summary>
        public RecordSchema? Schema { get; init; }

        public IReadOnlyList<OutlineNode> Children { get; init; } = [];
    }

    /// <summary>
    /// A dialogue and a quest read as an outline: the structure their author works in — nodes, lines
    /// and options; stages, objectives, routes and endings — as rows carrying the address of what they
    /// name. Godot-free, so the shape of the tree is settled here and the host only draws it.
    /// <para>Built from the document rather than from the schema: what the file actually holds is what
    /// the author has to see. The schema rides along to say what each address is, so a row picked in
    /// the tree can be handed to an inspector; a collection the build does not describe still gets its
    /// rows, and they are simply not editable.</para>
    /// </summary>
    public static class Outline
    {
        private const string EntryRulesName = "entryRules";
        private const string NodesName = "nodes";
        private const string LinesName = "lines";
        private const string OptionsName = "options";
        private const string StagesName = "stages";
        private const string ObjectivesName = "objectives";
        private const string TransitionsName = "transitions";
        private const string OutcomeName = "outcome";

        private const string IdName = "id";
        private const string NodeName = "node";
        private const string PriorityName = "priority";
        private const string SpeakerName = "speaker";
        private const string KeyName = "key";
        private const string NextName = "next";
        private const string ToName = "to";
        private const string OptionalName = "optional";
        private const string FailsName = "fails";
        private const string SpeechCheckName = "speechCheck";
        private const string FailNextName = "failNext";

        private const string EntryRulesTitle = "entry rules";
        private const string NodesTitle = "nodes";
        private const string StagesTitle = "stages";

        private const string GroupFormat = "{0}   ({1})";
        private const string EntryRuleFormat = "→ {0}   ·   priority {1}";
        private const string LineFormat = "{0}   ·   {1}";
        private const string OptionFormat = "{0}   → {1}";

        /// <summary>An option naming no node ends the conversation. Said on the row rather than left
        /// blank: "leads nowhere" and "the author has not written where yet" look the same otherwise.</summary>
        private const string OptionEndsFormat = "{0}   ·   ends";

        /// <summary>Where an option that rolls goes when the roll is lost. Written onto the row of the
        /// option itself: the check is not a place of its own the author edits, it is the second route
        /// out of one choice, and a row naming only the first shows half the conversation.</summary>
        private const string OptionFailFormat = "{0}   ·   fail → {1}";

        private const string OptionFailEndsFormat = "{0}   ·   fail ends";

        /// <summary>A node with nothing to say is not a node the game can open on, and a row that only
        /// listed its options would read as one that works.</summary>
        private const string NoLinesFormat = "{0}   ·   no lines";

        private const string ObjectiveFormat = "objective   ·   {0}";
        private const string OptionalObjectiveFormat = "objective   ·   {0}   ·   optional";
        private const string TransitionFormat = "→ {0}";
        private const string OutcomeFormat = "outcome   ·   {0}";
        private const string OutcomeFailsFormat = "outcome   ·   {0}   ·   fails";

        /// <summary>What stands where the file wrote nothing.</summary>
        private const string Missing = "—";

        /// <summary>What stands in place of the count of a collection, and of the name of an ending, when
        /// the key is written as something else entirely. A key nobody wrote and a key holding the wrong
        /// thing both draw no rows, and the author reading "(0)" over an object would be reading that his
        /// nodes are gone.</summary>
        private const string NotAList = "not a list";

        private const string NotARecord = "not a record";

        /// <summary>Stands in for the name of an element that carries none, so every row still has a
        /// word the author can point at.</summary>
        private const string IndexFormat = "#{0}";

        /// <summary>One element of a collection as a row: what the file holds there, where it is
        /// written, what the schema says it is, and its place in the collection.</summary>
        private delegate OutlineNode Row(JToken token, JsonPointer at, RecordSchema? schema, int index);

        /// <summary>
        /// One dialogue: the rules it opens on, then its nodes, and under every node the lines it says
        /// and the options it offers.
        /// </summary>
        public static OutlineNode Dialogue(RecordSchema schema, JToken token, JsonPointer at)
        {
            ArgumentNullException.ThrowIfNull(schema);
            ArgumentNullException.ThrowIfNull(token);
            ArgumentNullException.ThrowIfNull(at);

            return new OutlineNode
            {
                Pointer = at,
                Label = Identity(schema, token, index: 0),
                Kind = OutlineKind.Dialogue,
                Schema = schema,
                Children =
                [
                    Branch(token, EntryRulesName, EntryRulesTitle, at, schema, EntryRule),
                    Branch(token, NodesName, NodesTitle, at, schema, Node)
                ]
            };
        }

        /// <summary>
        /// One quest: its stages, and under every stage what has to be done, where the stage leads and
        /// the ending it may be.
        /// </summary>
        public static OutlineNode Quest(RecordSchema schema, JToken token, JsonPointer at)
        {
            ArgumentNullException.ThrowIfNull(schema);
            ArgumentNullException.ThrowIfNull(token);
            ArgumentNullException.ThrowIfNull(at);

            return new OutlineNode
            {
                Pointer = at,
                Label = Identity(schema, token, index: 0),
                Kind = OutlineKind.Quest,
                Schema = schema,
                Children = [Branch(token, StagesName, StagesTitle, at, schema, Stage)]
            };
        }

        /// <summary>A collection of the record as a row of its own, named and counted, with a row per
        /// element under it. Counted so that a record holding none of something says so: an author
        /// reading a dialogue with no nodes at all has to see the nothing, not an absence of rows.</summary>
        private static OutlineNode Branch(
            JToken token, string name, string title, JsonPointer at, RecordSchema? owner, Row row)
        {
            IReadOnlyList<OutlineNode> rows = Rows(token, name, at, owner, row);

            // What the heading says the collection holds: how many rows, or that the key holds no
            // collection at all.
            object held = Malformed(token, name) ? NotAList : rows.Count;

            return new OutlineNode
            {
                Pointer = at.Append(name),
                Label = Text(GroupFormat, title, held),
                Kind = OutlineKind.Group,
                Children = rows
            };
        }

        /// <summary>A row per element of one collection of the record. Empty where the file holds no
        /// such key, or holds something other than a list there — the outline says what is written, and
        /// what a malformed key means is the inspector's answer to give.</summary>
        private static List<OutlineNode> Rows(
            JToken token, string name, JsonPointer at, RecordSchema? owner, Row row)
        {
            List<OutlineNode> rows = [];

            if (Held(token, name) is not JArray array) return rows;

            JsonPointer list = at.Append(name);
            RecordSchema? element = Field(owner, name)?.Item?.Record;

            for (int index = 0; index < array.Count; index++)
                rows.Add(row(array[index], list.Append(index), element, index));

            return rows;
        }

        private static OutlineNode EntryRule(JToken token, JsonPointer at, RecordSchema? schema, int index) =>
            new()
            {
                Pointer = at,
                Label = Text(EntryRuleFormat, Or(Written(token, NodeName)), Or(Written(token, PriorityName))),
                Kind = OutlineKind.EntryRule,
                Schema = schema
            };

        private static OutlineNode Node(JToken token, JsonPointer at, RecordSchema? schema, int index)
        {
            List<OutlineNode> lines = Rows(token, LinesName, at, schema, Line);
            List<OutlineNode> options = Rows(token, OptionsName, at, schema, Option);
            string id = Identity(schema, token, index);

            return new OutlineNode
            {
                Pointer = at,
                Label = lines.Count > 0 ? id : Text(NoLinesFormat, id),
                Kind = OutlineKind.Node,
                Schema = schema,
                Children = [.. lines, .. options]
            };
        }

        private static OutlineNode Line(JToken token, JsonPointer at, RecordSchema? schema, int index)
        {
            string? key = Written(token, KeyName);

            return new OutlineNode
            {
                Pointer = at,
                Label = Text(LineFormat, Or(Written(token, SpeakerName)), Or(key)),
                Kind = OutlineKind.Line,
                Key = key ?? string.Empty,
                Schema = schema
            };
        }

        private static OutlineNode Option(JToken token, JsonPointer at, RecordSchema? schema, int index)
        {
            string? key = Written(token, KeyName);
            string? next = Written(token, NextName);

            // The key while the author has written one, and otherwise what the option is named by inside
            // its node: an option whose text is not worded yet still has an id the routes point at.
            string named = key is { Length: > 0 } ? key : Identity(schema, token, index);

            return new OutlineNode
            {
                Pointer = at,
                Label = Rolled(token, next is { Length: > 0 }
                    ? Text(OptionFormat, named, next)
                    : Text(OptionEndsFormat, named)),
                Kind = OutlineKind.Option,
                Key = key ?? string.Empty,
                Schema = schema
            };
        }

        /// <summary>Both routes out of an option that rolls: an Influence check runs the option's own
        /// route when it is won and its own second one when it is lost, and a check written with no
        /// route of its own ends the conversation there.</summary>
        private static string Rolled(JToken token, string route)
        {
            if (Held(token, SpeechCheckName) is not JObject check) return route;

            return Written(check, FailNextName) is { Length: > 0 } failed
                ? Text(OptionFailFormat, route, failed)
                : Text(OptionFailEndsFormat, route);
        }

        private static OutlineNode Stage(JToken token, JsonPointer at, RecordSchema? schema, int index)
        {
            List<OutlineNode> under = [
                .. Rows(token, ObjectivesName, at, schema, Objective),
                .. Rows(token, TransitionsName, at, schema, Transition)
            ];

            // Anything written under the key, and not an object alone: an ending the file spelled wrong
            // is one the stage still ends on as far as its author is concerned, and a row that was simply
            // not drawn would leave him reading a stage that goes nowhere.
            if (Held(token, OutcomeName) is { Type: not JTokenType.Null } outcome)
                under.Add(Outcome(outcome, at.Append(OutcomeName), Field(schema, OutcomeName)?.Record));

            return new OutlineNode
            {
                Pointer = at,
                Label = Identity(schema, token, index),
                Kind = OutlineKind.Stage,
                Schema = schema,
                Children = under
            };
        }

        private static OutlineNode Objective(JToken token, JsonPointer at, RecordSchema? schema, int index) =>
            new()
            {
                Pointer = at,
                Label = Text(
                    Flag(token, OptionalName) ? OptionalObjectiveFormat : ObjectiveFormat,
                    Identity(schema, token, index)),
                Kind = OutlineKind.Objective,
                Schema = schema
            };

        private static OutlineNode Transition(JToken token, JsonPointer at, RecordSchema? schema, int index) =>
            new()
            {
                Pointer = at,
                Label = Text(TransitionFormat, Or(Written(token, ToName))),
                Kind = OutlineKind.Transition,
                Schema = schema
            };

        /// <summary>The ending a stage may be. One per stage and written under its own key rather than
        /// listed, so it is drawn where it stands instead of through a collection.</summary>
        private static OutlineNode Outcome(JToken token, JsonPointer at, RecordSchema? schema) =>
            new()
            {
                Pointer = at,
                Label = token is JObject
                    ? Text(Flag(token, FailsName) ? OutcomeFailsFormat : OutcomeFormat,
                        Identity(schema, token, index: 0))
                    : Text(OutcomeFormat, NotARecord),
                Kind = OutlineKind.Outcome,
                Schema = schema
            };

        /// <summary>What the record is named by: the field its schema reads an id from, or the field
        /// every nested record of the narrative writes its own name under, or its place among its
        /// neighbours when it carries neither.</summary>
        private static string Identity(RecordSchema? schema, JToken token, int index) =>
            Written(token, schema?.IdField ?? IdName) is { Length: > 0 } written
                ? written
                : Text(IndexFormat, index);

        /// <summary>One field of a record's schema, or null when this build does not describe it.</summary>
        private static FieldSchema? Field(RecordSchema? owner, string name)
        {
            if (owner is null) return null;

            foreach (FieldSchema field in owner.Fields)
                if (string.Equals(field.JsonName, name, StringComparison.Ordinal))
                    return field;

            return null;
        }

        /// <summary>Whether a key is written as something other than the collection it has to hold. A key
        /// the record does not carry, and one written as nothing at all, are not malformed: the game reads
        /// no elements there either way, and the author has simply not written them yet.</summary>
        private static bool Malformed(JToken token, string name) =>
            Held(token, name) is { Type: not JTokenType.Null } and not JArray;

        private static JToken? Held(JToken token, string name) =>
            token is JObject holder && holder.TryGetValue(name, StringComparison.Ordinal, out JToken? value)
                ? value
                : null;

        /// <summary>The text a key holds, spelled the way the file spells it; null for a key the record
        /// does not hold and for one written as nothing at all.</summary>
        private static string? Written(JToken token, string name) =>
            Held(token, name) is JValue { Value: not null } value ? JsonScalars.Written(value) : null;

        /// <summary>Whether a key is written true. An absent switch is off, which is what the game reads
        /// in its place.</summary>
        private static bool Flag(JToken token, string name) =>
            Held(token, name) is JValue { Type: JTokenType.Boolean } value && value.Value<bool>();

        private static string Or(string? written) => written is { Length: > 0 } ? written : Missing;
    }
}
