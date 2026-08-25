namespace Core.Battle.Skills
{
    using System;
    using System.Collections.Generic;
    using Enums;

    /// <summary>
    /// One row of the stat family's field editor. Either the three words of a stat line held apart — the
    /// shape a dropdown can pick from — or a key the grammar refused, kept exactly as the file wrote it.
    /// <para>The two shapes are one type on purpose: the rows of a record are an ordered list, and a
    /// refused key that lived in a list of its own would come back somewhere other than where its author
    /// put it.</para>
    /// </summary>
    public sealed class StatFieldRow
    {
        /// <summary>The key verbatim, on a row the grammar could not read; null on a row the dropdowns
        /// own. A row that keeps it writes it back untouched — a tool that cannot read a key has no
        /// business rewriting it, and the author is who decides what it was meant to say.</summary>
        public string? RawName { get; set; }

        public EntityParameter Parameter { get; set; }

        public ModifierValueType ValueType { get; set; }

        /// <summary>The carrier the line is measured per unit of; null for a line worth its number
        /// outright, which is what makes the key two words instead of three.</summary>
        public EntityParameter? PerParameter { get; set; }

        public float Value { get; set; }

        public bool IsRaw => RawName is not null;

        /// <summary>The key this row is written under: the raw one it came in as, or the one its words
        /// spell through the grammar.</summary>
        public string Name => RawName ?? StatPassiveGrammar.Key(Parameter, ValueType, PerParameter);
    }

    /// <summary>One row of a named passive's field editor: a number under a name. A required row is one
    /// the catalog names — the factory reads it, so the name is not the author's to type over and the row
    /// is not his to remove; anything else is a free pair he wrote himself.</summary>
    public sealed class CatalogFieldRow
    {
        public required string Name { get; init; }

        public float Value { get; set; }

        public bool IsRequired { get; init; }
    }

    /// <summary>
    /// The named numbers of a node as an editor shows them, and back again. Everything about the trip
    /// between a file's <c>{name: number}</c> and a panel of controls lives here, outside any widget, so
    /// the round trip can be held to by a test rather than by a click.
    /// <para><b>The file is not changed by any of this.</b> A stat row spells the same key the grammar has
    /// always read, a refused key survives untouched, and a catalog row is the plain pair it always was —
    /// the rows are a way of showing the record, never a second format for it.</para>
    /// </summary>
    public static class PassiveFieldRows
    {
        /// <summary>The buckets a stat line may be written in. Flag is absent because the grammar refuses
        /// it outright: offering it would be a dropdown whose every choice costs the whole grant.</summary>
        public static readonly ModifierValueType[] ValueTypes =
        [
            ModifierValueType.Flat,
            ModifierValueType.Increase,
            ModifierValueType.Multiplicative
        ];

        /// <summary>The record as stat rows, in the order it was written. A key the grammar reads becomes
        /// words a dropdown can pick; one it refuses becomes a raw row rather than nothing at all — a
        /// hand-written mistake has to stay visible where it can be corrected, and a tool that dropped it
        /// would answer a typo by deleting the line.</summary>
        public static List<StatFieldRow> ReadStat(IEnumerable<KeyValuePair<string, float>> properties)
        {
            List<StatFieldRow> rows = [];

            foreach (KeyValuePair<string, float> property in properties)
            {
                rows.Add(StatPassiveGrammar.TryReadLine(property.Key, property.Value, out StatPassiveLine line, out _)
                    ? new StatFieldRow
                    {
                        Parameter = line.Parameter,
                        ValueType = line.ValueType,
                        PerParameter = line.PerParameter,
                        Value = line.Value
                    }
                    : new StatFieldRow { RawName = property.Key, Value = property.Value });
            }

            return rows;
        }

        /// <summary>The rows as the record again, in the order they stand. Order is the whole point of
        /// handing the list back whole: a dictionary gives a fresh key the slot a removed one left.</summary>
        public static List<KeyValuePair<string, float>> Write(IEnumerable<StatFieldRow> rows)
        {
            List<KeyValuePair<string, float>> written = [];
            foreach (StatFieldRow row in rows) written.Add(new KeyValuePair<string, float>(row.Name, row.Value));

            return written;
        }

        public static List<KeyValuePair<string, float>> Write(IEnumerable<CatalogFieldRow> rows)
        {
            List<KeyValuePair<string, float>> written = [];
            foreach (CatalogFieldRow row in rows) written.Add(new KeyValuePair<string, float>(row.Name, row.Value));

            return written;
        }

        /// <summary>Whether another row already writes this key. Two rows under one name are one number in
        /// the file and a coin toss over which of them survives, so the gesture that would make the pair is
        /// what gets refused — <paramref name="exceptIndex"/> is the row doing the asking.</summary>
        public static bool Holds(IReadOnlyList<StatFieldRow> rows, string name, int exceptIndex)
        {
            for (int index = 0; index < rows.Count; index++)
                if (index != exceptIndex && string.Equals(rows[index].Name, name, StringComparison.Ordinal))
                    return true;

            return false;
        }

        /// <summary>A stat row no row on the node is using yet, so pressing the button twice never authors
        /// the same line twice. Found rather than assumed: a fixed default would be taken already on the
        /// second press, and a row that collided would be swallowed by the record it is written into.</summary>
        public static StatFieldRow Fresh(IReadOnlyList<StatFieldRow> rows)
        {
            EntityParameter[] parameters = Enum.GetValues<EntityParameter>();

            foreach (EntityParameter parameter in parameters)
                foreach (ModifierValueType valueType in ValueTypes)
                    if (!Holds(rows, StatPassiveGrammar.Key(parameter, valueType, null), exceptIndex: -1))
                        return new StatFieldRow { Parameter = parameter, ValueType = valueType };

            // Every two-word line there is, already written on one node. Nothing sensible is left to offer;
            // the first one back is a row the author can point somewhere else.
            return new StatFieldRow { Parameter = parameters[0], ValueType = ModifierValueType.Flat };
        }

        /// <summary>The parameters a dropdown may offer beside a chosen one. The grammar refuses a line
        /// measured per unit of the parameter it feeds, so the pair is never offered in the first place —
        /// a key that cannot be picked is a key that cannot be authored.</summary>
        public static List<EntityParameter> ParametersExcept(EntityParameter? excluded)
        {
            List<EntityParameter> offered = [];

            foreach (EntityParameter parameter in Enum.GetValues<EntityParameter>())
                if (excluded != parameter)
                    offered.Add(parameter);

            return offered;
        }

        /// <summary>The record as named rows, with the catalog's own fields marked. Nothing is added and
        /// nothing is reordered: this is the record being shown, and a field the node does not carry is
        /// missing — which is what the warning beside it says.</summary>
        public static List<CatalogFieldRow> ReadFields(IReadOnlyList<string> required,
            IEnumerable<KeyValuePair<string, float>> properties)
        {
            var names = new HashSet<string>(required, StringComparer.Ordinal);
            List<CatalogFieldRow> rows = [];

            foreach (KeyValuePair<string, float> property in properties)
                rows.Add(new CatalogFieldRow
                {
                    Name = property.Key,
                    Value = property.Value,
                    IsRequired = names.Contains(property.Key)
                });

            return rows;
        }

        /// <summary>The record with every field the catalog names present, missing ones written in as zero
        /// and appended in catalog order. What the author already wrote keeps its place and its number:
        /// materialising a field must not be a way to reorder a record or to overwrite balance.
        /// <para>Zero is a number like any other here. A field created at zero is created, not pending —
        /// the tool has no way to tell a zero the author meant from one it wrote itself, and inventing a
        /// distinction would be inventing a value that is not in the file.</para></summary>
        public static List<KeyValuePair<string, float>> WithRequired(IReadOnlyList<string> required,
            IEnumerable<KeyValuePair<string, float>> properties)
        {
            List<KeyValuePair<string, float>> written = [];
            var present = new HashSet<string>(StringComparer.Ordinal);

            foreach (KeyValuePair<string, float> property in properties)
            {
                written.Add(property);
                present.Add(property.Key);
            }

            foreach (string field in required)
                if (present.Add(field))
                    written.Add(new KeyValuePair<string, float>(field, 0f));

            return written;
        }
    }
}
