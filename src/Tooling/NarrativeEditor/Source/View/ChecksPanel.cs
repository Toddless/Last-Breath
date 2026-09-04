namespace NarrativeEditor.Source.View
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core.Narrative.Validation;
    using Godot;
    using LastBreath.Descriptors;
    using Tooling.Catalogs;
    using Tooling.Editing.History;
    using Tooling.Json;
    using Tooling.Localization;
    using static Tooling.Text.Format;

    /// <summary>
    /// The narrative held against everything outside it, over the documents open in the tool: the ids it
    /// names, the dialogues its quests are taken from, the routes inside a conversation and the wording
    /// every line is read under. The rules are the game's own, so what stands here and what the game's
    /// own tests report are one answer.
    /// <para>A report and not a gate. Pressing a row opens the record it belongs to, which is the whole
    /// point of naming a place rather than a file — the author is one click from the thing to fix.</para>
    /// </summary>
    public partial class ChecksPanel : VBoxContainer
    {
        private const string Title = "checks";

        private const string CheckText = "Check";

        private const string NotRunText = "press Check to read the narrative as it is written now";

        private const string StaleText = "the documents changed: press Check again";

        private const string CleanText = "nothing to report";

        private const string FoundFormat = "{0} finding(s)";

        private const string RanFormat = "checked the narrative: {0} finding(s)";

        private const string RowFormat = "{0}   {1}";

        private const string NoteRowFormat = "note   {0}";

        private const string FoundAndNotedFormat = "{0} finding(s), {1} note(s)";

        private const string RefusedFormat = "the checks could not be run over the documents as they stand: {0}";

        /// <summary>What separates the steps of the place a finding is in. The first two of them name the
        /// catalog and the record, which is as far as a list of records can be opened to.</summary>
        private const char PlaceSeparator = '/';

        /// <summary>What the place of a record with no id of its own starts the step with. Such a row can
        /// only be read, not opened: the list on the left names records by their ids.</summary>
        private const char PlaceIndex = '[';

        private const int RecordSteps = 2;

        /// <summary>How tall the list asks to be before the divider above it is dragged.</summary>
        private const int PaneHeight = 240;

        private readonly List<Row> _rows = [];

        private readonly List<JsonTreeDocument> _watched = [];

        private readonly List<CatalogView> _catalogs = [];

        /// <summary>How many rows of the list are findings rather than notes about the run itself.</summary>
        private int _findings;

        private Button _check = null!;
        private Label _summary = null!;
        private ItemList _list = null!;

        /// <summary>Nothing has been read yet, or the documents moved under what was. Either way the rows
        /// on screen are not an answer about the narrative as it is written now.</summary>
        private bool _stale = true;

        /// <summary>Everything the tool has open. The checks read every catalog of the run, not the
        /// narrative alone: an id is only answered against a catalog somebody opened.</summary>
        public CatalogWorkspace? Workspace
        {
            get;
            set
            {
                Unwatch();
                field = value;
                Watch();
                Invalidate();
            }
        }

        /// <summary>The stack the tool files every step on. Watched because the .po files are edited onto
        /// it and announce nothing themselves: the wording is what a missing-text finding is about, and a
        /// translation typed a second ago has to leave the rows on screen out of date.</summary>
        /// <remarks>Any step marks the panel stale, the records' own among them — the documents say so too,
        /// and one redraw of a summary line costs nothing.</remarks>
        public EditHistory? History
        {
            get;
            set
            {
                if (field is { } watched) watched.Changed -= Stepped;
                field = value;
                if (field is { } history) history.Changed += Stepped;
            }
        }

        /// <summary>The ids of the run, as the shell already reads them for the inspector's pickers.</summary>
        public ReferenceIndex? References { get; set; }

        /// <summary>The .po files of the run. Null leaves every line unheld, which the run says out loud.</summary>
        public LocalizedTexts? Texts { get; set; }

        /// <summary>What the run came to, for the status line of the shell.</summary>
        public event Action<string>? Said;

        /// <summary>The record one finding belongs to: the catalog it is in and the id it answers to.</summary>
        public event Action<string, string>? Chose;

        public override void _Ready()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            SizeFlagsVertical = SizeFlags.ExpandFill;

            _check = new Button { Text = CheckText };
            _check.Pressed += Start;

            _summary = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };

            _list = new ItemList
            {
                CustomMinimumSize = new Vector2(0, PaneHeight),
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill
            };

            _list.ItemSelected += index => Open((int)index);

            var row = new HBoxContainer();
            row.AddChild(new Label { Text = Title });
            row.AddChild(_check);

            AddChild(row);
            AddChild(_summary);
            AddChild(_list);

            Redraw();
        }

        public override void _ExitTree()
        {
            Unwatch();
            History = null;
        }

        /// <summary>Reads the whole narrative through the game's own rules and lists what it found.</summary>
        /// <remarks>A run that throws is said in the status line and nothing else: the documents are being
        /// typed into, and a half-written record must not take the tool down with it.</remarks>
        private void Start()
        {
            if (Workspace is not { } workspace || References is not { } references) return;

            NarrativeCheckReport report;

            try
            {
                report = NarrativeCheckRun.Over(workspace, references, Texts);
            }
            catch (Exception failure)
            {
                Said?.Invoke(Text(RefusedFormat, failure.Message));
                return;
            }

            _rows.Clear();
            _rows.AddRange(report.Findings.Select(finding =>
                new Row(Text(RowFormat, finding.Kind, finding.Where), finding.Message, Record(finding.Where))));
            _rows.AddRange(report.Notes.Select(note => new Row(Text(NoteRowFormat, note), note, null)));

            _findings = report.Findings.Count;
            _stale = false;

            Said?.Invoke(Text(RanFormat, report.Findings.Count));
            Redraw();
        }

        /// <summary>The rows stop answering for the documents the moment one of them is typed into.</summary>
        private void Invalidate()
        {
            _stale = true;
            Redraw();
        }

        private void Redraw()
        {
            if (_summary is null) return;

            _check.Disabled = Workspace is null || References is null;
            _summary.Text = Summary();

            _list.Clear();

            foreach (Row row in _rows)
            {
                int index = _list.AddItem(row.Text);

                _list.SetItemTooltip(index, row.Tooltip);
                _list.SetItemDisabled(index, row.Record is null);
            }
        }

        private string Summary()
        {
            if (_stale) return _rows.Count == 0 ? NotRunText : StaleText;
            if (_rows.Count == 0) return CleanText;

            int notes = _rows.Count - _findings;

            return notes == 0 ? Text(FoundFormat, _findings) : Text(FoundAndNotedFormat, _findings, notes);
        }

        /// <summary>Opens the record one row belongs to. A row about the run itself — a catalog nobody
        /// described, a locale nobody read — belongs to no record and cannot be opened.</summary>
        private void Open(int index)
        {
            if (index < 0 || index >= _rows.Count) return;
            if (_rows[index].Record is not { } record) return;

            Chose?.Invoke(record.Catalog, record.Id);
        }

        /// <summary>One line of the list: what it says, what it says in full, and the record it can be
        /// opened to — nothing at all for a row that is about the run rather than about a record.</summary>
        private sealed record Row(string Text, string Tooltip, (string Catalog, string Id)? Record);

        /// <summary>The catalog and the record a place names, or nothing when it names neither. A record
        /// listed by its place in the file is named by neither step — the list on the left answers to ids —
        /// and so is a row about a catalog the run could not read.</summary>
        private static (string Catalog, string Id)? Record(string where)
        {
            string[] steps = where.Split(PlaceSeparator);

            if (steps.Length < RecordSteps) return null;
            if (steps[0].Length == 0 || steps[0].Contains(PlaceIndex)) return null;
            if (steps[1].Length == 0 || steps[1][0] == PlaceIndex) return null;

            return (steps[0], steps[1]);
        }

        /// <summary>Listens to every document of the run: an id the narrative points at is written in
        /// another catalog, so a check is out of date as soon as ANY of them is typed into. A file the run
        /// lays down joins them, and the stack the whole tool files its steps on covers the rest — the .po
        /// files among them, whose wording is the very thing a missing-text finding is about.</summary>
        private void Watch()
        {
            if (Workspace is not { } workspace) return;

            foreach (CatalogView view in workspace.Catalogs)
            {
                view.FileAdded += Added;
                _catalogs.Add(view);

                foreach (CatalogFile file in view.Files) Watch(file);
            }
        }

        private void Watch(CatalogFile file)
        {
            file.Document.Changed += Stale;
            _watched.Add(file.Document);
        }

        private void Unwatch()
        {
            foreach (JsonTreeDocument document in _watched) document.Changed -= Stale;
            foreach (CatalogView view in _catalogs) view.FileAdded -= Added;

            _watched.Clear();
            _catalogs.Clear();
        }

        /// <summary>Takes a freshly laid file under the same watch and marks what is on screen out of
        /// date: a record written into a file that did not exist when the last run read is a record the
        /// rows on screen never saw.</summary>
        private void Added(CatalogFile file)
        {
            Watch(file);
            Invalidate();
        }

        private void Stale(JsonPointer pointer) => Invalidate();

        /// <summary>Any step the tool files, whatever it was typed into. The locales are edited onto this
        /// same stack and announce nothing of their own, so a translation written a second ago reaches the
        /// panel this way and no other.</summary>
        private void Stepped() => Invalidate();
    }
}
