namespace Tooling.Ui
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using LastBreath.Descriptors;
    using Tooling.Catalogs;
    using Tooling.Catalogs.Checks;
    using Tooling.Localization;
    using static Tooling.Text.Format;

    /// <summary>
    /// The catalogs held against each other and against the wording beside them, over the documents open
    /// in the tool: the ids they name, the names they are found by, the shapes their records are written
    /// in and the keys their text is read under. The rules are the game's own tests', so what stands here
    /// and what a run over the shipped data reports are one answer.
    /// </summary>
    public partial class CatalogChecksPanel : ChecksPanelBase
    {
        private const string NotRun = "press Check to read the catalogs as they are written now";

        private const string FoundFormat = "{0} finding(s)";

        private const string RanFormat = "checked the catalogs: {0} finding(s)";

        private const string RowFormat = "{0}   {1}";

        private const string NamedRowFormat = "{0}   {1}/{2}   {3}";

        private const string TooltipFormat = "{0}{1}{2}";

        private const string TooltipBreak = "\n";

        /// <summary>How a finding of one of the game's own rules is labelled: the sort, and the rule that
        /// spoke. Four different facts share one sort, and the sort alone would call them one thing.</summary>
        private const string RuledFormat = "{0} {1}";

        protected override string NotRunText => NotRun;

        /// <summary>Lists a pass somebody else has already made over these very documents — the one the
        /// host makes while it opens the run. The rows stand as if the button had been pressed, because
        /// the reading is the same reading: pressing it again would walk every catalog a second time to
        /// come back with what is already on screen.</summary>
        public void Show(IReadOnlyList<CatalogFinding> found) => Show(Listed(found));

        /// <summary>Reads every catalog through the same rules the game's own tests run.</summary>
        protected override Reading Read(CatalogWorkspace workspace, ReferenceIndex references, LocalizedTexts? texts) =>
            Listed(CatalogCheckRun.Over(workspace, references, texts));

        /// <summary>What one pass comes to on screen: a row per finding, and how they are counted.</summary>
        private static Reading Listed(IReadOnlyList<CatalogFinding> found)
        {
            ArgumentNullException.ThrowIfNull(found);

            return new Reading(
                [.. found.Select(Written)],
                Text(FoundFormat, found.Count),
                Text(RanFormat, found.Count));
        }

        /// <summary>One finding as a row: what it is and where, the whole of what it says under the mouse,
        /// and the record it can be opened to — nothing at all for a finding about the run itself, a
        /// catalog nobody described or a locale writing one key twice.</summary>
        private static Row Written(CatalogFinding finding) =>
            new(
                finding.Catalog.Length == 0
                    ? Text(RowFormat, Sort(finding), finding.Named)
                    : Text(NamedRowFormat, Sort(finding), finding.Catalog, finding.Record, finding.Named),
                Text(TooltipFormat, finding.Message, finding.Where.Length == 0 ? string.Empty : TooltipBreak, finding.Where),
                finding.Catalog.Length > 0 && finding.Record.Length > 0 ? (finding.Catalog, finding.Record) : null);

        /// <summary>What the row calls the finding: its sort, and beside it the rule that spoke where the
        /// finding is one of the game's own rules over the meaning of its data.</summary>
        private static string Sort(CatalogFinding finding) =>
            finding.Rule.Length == 0 ? finding.Kind.ToString() : Text(RuledFormat, finding.Kind, finding.Rule);
    }
}
