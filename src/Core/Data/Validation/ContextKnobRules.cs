namespace Core.Data.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Enums;
    using Modifiers;

    /// <summary>One pool an item's lines may be rolled from, under the id an author will search the data
    /// by: a pool of the modifier catalog, a material category, or one resource's own descriptors. The
    /// descriptors are what the game's own parser made of the file — value-type aliases resolved and
    /// refused entries already dropped — so the entries held here are the entries the game rolls.</summary>
    public sealed record RollablePool(string Source, IReadOnlyList<IModifierDescriptor> Descriptors);

    /// <summary>
    /// A context knob is applied by its parameter alone: the binding reads the line's number, and the
    /// value type the line was written in reaches no pipeline at all — healing efficiency works the same
    /// written flat or written increased. Duplicate control disagrees: a line's identity counts the value
    /// type, so one knob written in two of them is two identities. An item may then roll both entries,
    /// each attaches on its own, and the bonuses multiply — larger than either line says, and invisible
    /// to every duplicate check.
    /// <para>Hence the rule: across every pool an item's lines can come from, a knob is written in ONE
    /// value type. Composite parts count as their own entries — a bundle may carry a knob among its
    /// parts, and such a part attaches like any other line.</para>
    /// </summary>
    public static class ContextKnobRules
    {
        /// <summary>How many value types make a knob written two ways.</summary>
        private const int TwoWays = 2;

        private const string SplitFormat =
            "'{0}' is written as {1}: a knob is applied by its parameter alone, so entries written in "
            + "different value types are the same bonus twice, and an item rolling both wears them multiplied.";

        private const string WrittenFormat = "{0} in [{1}]";

        private const string AndWord = " and as ";

        private const string Separator = ", ";

        /// <summary>Every knob the pools disagree about, one finding per knob and in the order the
        /// parameters are named. A knob written one way everywhere is nothing to report, and so is a run
        /// holding no pools at all — the rule answers about what it was given.</summary>
        public static IReadOnlyList<DataFinding> Check(IReadOnlyList<RollablePool> pools)
        {
            ArgumentNullException.ThrowIfNull(pools);

            List<Entry> written = Written(pools);
            List<DataFinding> found = [];

            foreach (IGrouping<ContextParameter, Entry> knob in written
                .GroupBy(entry => entry.Parameter)
                .OrderBy(knob => knob.Key.ToString(), StringComparer.Ordinal))
            {
                if (knob.Select(entry => entry.ValueType).Distinct().Count() < TwoWays) continue;

                found.Add(new DataFinding(
                    DataFindingKind.SplitValueType,
                    Catalog: string.Empty,
                    Record: string.Empty,
                    knob.Key.ToString(),
                    Where: string.Empty,
                    string.Format(SplitFormat, knob.Key, Split(knob))));
            }

            return found;
        }

        /// <summary>How many knob entries the pools hold, bundles unfolded — what the rule actually read.
        /// A gate over the shipped data asks it to know the difference between "nothing disagrees" and
        /// "nothing was read": the pools are full of plain lines, and counting those would call a run that
        /// met no knob at all a run that checked something.</summary>
        public static int Knobs(IReadOnlyList<RollablePool> pools)
        {
            ArgumentNullException.ThrowIfNull(pools);

            return Written(pools).Count;
        }

        /// <summary>Every knob entry the pools hold, under the pool it was met in.</summary>
        private static List<Entry> Written(IReadOnlyList<RollablePool> pools)
        {
            List<Entry> written = [];
            HashSet<IModifierDescriptor> counted = new(ReferenceEqualityComparer.Instance);

            foreach (RollablePool pool in pools)
                foreach (IModifierDescriptor descriptor in pool.Descriptors)
                    Read(descriptor, pool.Source, counted, written);

            return written;
        }

        /// <summary>One descriptor and, where it is a bundle, everything inside it. Descriptors are taken
        /// down once per INSTANCE: a material category's entries are the very objects every material of
        /// that category carries, so counting by instance names them under the category instead of once
        /// per resource that shares them.</summary>
        private static void Read(
            IModifierDescriptor descriptor, string source, HashSet<IModifierDescriptor> counted, List<Entry> written)
        {
            if (!counted.Add(descriptor)) return;

            switch (descriptor)
            {
                case CompositeDescriptor composite:
                    foreach (IModifierDescriptor part in composite.Parts) Read(part, source, counted, written);
                    break;
                case ContextDescriptor context:
                    written.Add(new Entry(context.Parameter, context.ValueType, source));
                    break;
            }
        }

        /// <summary>What an author has to fix: each value type the knob is written in, and every pool,
        /// material category or resource the entries of that type sit under.</summary>
        private static string Split(IEnumerable<Entry> knob) =>
            string.Join(AndWord, knob
                .GroupBy(entry => entry.ValueType)
                .OrderBy(bucket => bucket.Key)
                .Select(bucket => string.Format(
                    WrittenFormat,
                    bucket.Key,
                    string.Join(Separator, bucket.Select(entry => entry.Source).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)))));

        /// <summary>One knob entry: what it tunes, how it was written, and the pool it was met in.</summary>
        private readonly record struct Entry(ContextParameter Parameter, ModifierValueType ValueType, string Source);
    }
}
