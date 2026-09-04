namespace NarrativeEditor.Source.View
{
    using System.Linq;
    using Core.Narrative.Validation;
    using LastBreath.Descriptors;
    using Tooling.Catalogs;
    using Tooling.Localization;
    using Tooling.Ui;
    using static Tooling.Text.Format;

    /// <summary>
    /// The narrative held against everything outside it, over the documents open in the tool: the ids it
    /// names, the dialogues its quests are taken from, the routes inside a conversation and the wording
    /// every line is read under. The rules are the game's own, so what stands here and what the game's
    /// own tests report are one answer.
    /// </summary>
    public partial class ChecksPanel : ChecksPanelBase
    {
        private const string NotRun = "press Check to read the narrative as it is written now";

        private const string FoundFormat = "{0} finding(s)";

        private const string FoundAndNotedFormat = "{0} finding(s), {1} note(s)";

        private const string RanFormat = "checked the narrative: {0} finding(s)";

        private const string RowFormat = "{0}   {1}";

        private const string NoteRowFormat = "note   {0}";

        protected override string NotRunText => NotRun;

        /// <summary>Reads the whole narrative through the game's own rules. Notes come back as rows of
        /// their own and are counted apart: a note is what the run could not ask rather than something
        /// the narrative owes.</summary>
        protected override Reading Read(CatalogWorkspace workspace, ReferenceIndex references, LocalizedTexts? texts)
        {
            NarrativeCheckReport report = NarrativeCheckRun.Over(workspace, references, texts);

            Row[] rows =
            [
                .. report.Findings.Select(finding =>
                    new Row(Text(RowFormat, finding.Kind, finding.Where), finding.Message, Place.Record(finding.Where))),
                .. report.Notes.Select(note => new Row(Text(NoteRowFormat, note), note, null))
            ];

            return new Reading(rows, Counted(report), Text(RanFormat, report.Findings.Count));
        }

        private static string Counted(NarrativeCheckReport report) =>
            report.Notes.Count == 0
                ? Text(FoundFormat, report.Findings.Count)
                : Text(FoundAndNotedFormat, report.Findings.Count, report.Notes.Count);
    }
}
