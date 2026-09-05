namespace Tooling.Ui
{
    using System;
    using System.Collections.Generic;
    using Godot;
    using Tooling.Catalogs;
    using Tooling.Editing.History;
    using Tooling.Localization;
    using static Tooling.Text.Format;

    /// <summary>
    /// A panel that reads the whole run at one press and lists what came back. Everything about that is
    /// the same whichever rules are pressed — the button, the list, the documents an answer goes stale
    /// under, the row that opens the record it belongs to — and only WHAT is read and how a count of it
    /// is worded belongs to the panel itself.
    /// <para>A report and not a gate. Pressing a row opens the record it names, which is the whole point
    /// of naming a record rather than a file — the author is one click from the thing to fix.</para>
    /// </summary>
    public abstract partial class ChecksPanelBase : VBoxContainer
    {
        private const string Title = "checks";

        private const string CheckText = "Check";

        private const string StaleText = "the documents changed: press Check again";

        private const string CleanText = "nothing to report";

        private const string RefusedFormat = "the checks could not be run over the documents as they stand: {0}";

        /// <summary>How tall the list asks to be before the divider above it is dragged.</summary>
        private const int PaneHeight = 240;

        private readonly List<Row> _rows = [];

        /// <summary>Everything the rows stop answering for the moment it is typed into: every document of
        /// the run, every file laid down while it is open, and the stack every edit is filed on.</summary>
        private readonly DocumentWatch _watch = new();

        private Button _check = null!;
        private Label _summary = null!;
        private ItemList _list = null!;

        /// <summary>How the rows on screen were counted when they were read. Kept from the reading rather
        /// than counted again: what a row IS belongs to the panel that read it.</summary>
        private string _counted = string.Empty;

        /// <summary>Nothing has been read yet, or the documents moved under what was. Either way the rows
        /// on screen are not an answer about the run as it is written now.</summary>
        private bool _stale = true;

        /// <summary>Everything the tool has open. The checks read every catalog of the run: an id is only
        /// answered against a catalog somebody opened.
        /// <para>Setting it says so: the host reads its catalogs after the panels are built, and a button
        /// that stayed disabled until the first keystroke would be a Check nobody could press.</para></summary>
        public CatalogWorkspace? Workspace
        {
            get => _watch.Workspace;
            set
            {
                _watch.Workspace = value;
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
            get => _watch.History;
            set => _watch.History = value;
        }

        /// <summary>The ids of the run, as the shell already reads them for the inspector's pickers.</summary>
        public ReferenceIndex? References
        {
            get;
            set
            {
                field = value;
                Invalidate();
            }
        }

        /// <summary>The .po files of the run. Null leaves the wording unheld, which is a run with nothing
        /// to say about it rather than one reporting every key missing.
        /// <para>Setting it says so: what a locale words is half of what the rows on screen answered, and
        /// a set handed over or read afresh is not a step on the stack the watch listens to.</para></summary>
        public LocalizedTexts? Texts
        {
            get;
            set
            {
                field = value;
                Invalidate();
            }
        }

        /// <summary>What the run came to, for the status line of the shell.</summary>
        public event Action<string>? Said;

        /// <summary>The record one finding belongs to: the catalog it is in and the id it answers to.</summary>
        public event Action<string, string>? Chose;

        /// <summary>What the panel says before anything has been read at all — which names the rules the
        /// button presses, and is the one line an author reads before he knows what they are.</summary>
        protected abstract string NotRunText { get; }

        public override void _Ready()
        {
            _watch.Changed += Invalidate;

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

            Refresh();
        }

        /// <summary>Lets go of everything watched when the panel leaves the tree: a handler outliving its
        /// panel is a redraw of a control nobody has.</summary>
        public override void _ExitTree() => _watch.Stop();

        /// <summary>Reads the whole run through one set of rules. What comes back is the rows to list and
        /// how they are counted — the panel below knows nothing of either.</summary>
        protected abstract Reading Read(CatalogWorkspace workspace, ReferenceIndex references, LocalizedTexts? texts);

        /// <summary>Reads the run and lists what came back.</summary>
        /// <remarks>A run that throws is said in the status line and nothing else: the documents are being
        /// typed into, and a half-written record must not take the tool down with it.</remarks>
        private void Start()
        {
            if (Workspace is not { } workspace || References is not { } references) return;

            Reading reading;

            try
            {
                reading = Read(workspace, references, Texts);
            }
            catch (Exception failure)
            {
                Said?.Invoke(Text(RefusedFormat, failure.Message));
                return;
            }

            Show(reading);

            Said?.Invoke(reading.Said);
        }

        /// <summary>Lists a reading and stands by it until the documents move. Reached by the button and
        /// by a host that has already made the very same pass while opening the run: the pass reads every
        /// document there is, and one made twice a second apart is a tool that opens slowly for nothing.
        /// <para>Says nothing of itself — whoever hands a reading over is where the author was told about
        /// it, and one answer said twice reads as two.</para></summary>
        protected void Show(Reading reading)
        {
            ArgumentNullException.ThrowIfNull(reading);

            _rows.Clear();
            _rows.AddRange(reading.Rows);

            _counted = reading.Summary;
            _stale = false;

            Refresh();
            Relist();
        }

        /// <summary>The rows stop answering for the documents the moment one of them is typed into. The
        /// list itself stands as it was: it is the last answer there is about the run, and rebuilding it
        /// would drop the row the author is reading out from under him.</summary>
        private void Invalidate()
        {
            _stale = true;
            Refresh();
        }

        /// <summary>What the panel says about the rows, and whether there is anything to press. Asked on
        /// every step of the tool, so it touches nothing but the two.</summary>
        private void Refresh()
        {
            if (_summary is null) return;

            _check.Disabled = Workspace is null || References is null;
            _summary.Text = Summary();
        }

        /// <summary>The rows themselves, which change only when the button is pressed.</summary>
        private void Relist()
        {
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

            return _rows.Count == 0 ? CleanText : _counted;
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
        protected sealed record Row(string Text, string Tooltip, (string Catalog, string Id)? Record);

        /// <summary>What one press came to: the rows to list, how they are counted, and what to say in the
        /// status line. The count is read only where there is something to list — a run that found nothing
        /// is worded the same way whatever was read.</summary>
        protected sealed record Reading(IReadOnlyList<Row> Rows, string Summary, string Said);
    }
}
