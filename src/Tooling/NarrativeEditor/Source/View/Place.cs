namespace NarrativeEditor.Source.View
{
    using System;
    using Core.Narrative.Validation;

    /// <summary>The record a place in the data belongs to. Written the same way wherever the game names a
    /// place — a finding, the address a fact key is written at — so one reading of it answers for both.</summary>
    internal static class Place
    {
        /// <summary>What separates the steps of a place. The first two of them name the catalog and the
        /// record, which is as far as a list of records can be opened to.</summary>
        private const char Separator = '/';

        /// <summary>What the step of a record with no id of its own starts with. Such a place can only be
        /// read, not opened: the list of records names them by their ids.</summary>
        private const char Index = '[';

        private const int RecordSteps = 2;

        /// <summary>The catalog and the record a place names, or nothing when it names neither. A record
        /// listed by its place in the file is named by neither step, and so is a place that is about the
        /// run — a catalog nobody described, a fact key belonging to no document at all.</summary>
        public static (string Catalog, string Id)? Record(string where)
        {
            string[] steps = where.Split(Separator);

            if (steps.Length < RecordSteps) return null;
            if (steps[0].Length == 0 || steps[0].Contains(Index)) return null;
            if (string.Equals(steps[0], NarrativeChecks.FactsWhere, StringComparison.Ordinal)) return null;
            if (steps[1].Length == 0 || steps[1][0] == Index) return null;

            return (steps[0], steps[1]);
        }
    }
}
