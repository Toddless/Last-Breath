namespace NarrativeEditor.Source.View
{
    using System.Collections.Generic;
    using App;
    using Godot;
    using Tooling.Catalogs;
    using Tooling.Json;
    using Tooling.Narrative;
    using Tooling.Ui;
    using static Tooling.Text.Format;

    /// <summary>
    /// Composition root of the narrative editor: the dialogues and the quests in one list, the record
    /// chosen out of it as an outline of what its author works in — nodes, lines and options; stages,
    /// objectives, routes and endings — and the row picked out of that outline in the inspector every
    /// tool of this repository edits records with.
    /// <para>The outline is a way to an address and not a second editor: picking a row opens the
    /// inspector on the very node, option or stage the row stands for, and every edit is the same edit
    /// the data editor makes, onto the same history of the same file.</para>
    /// </summary>
    public partial class NarrativeRoot : ToolShell
    {
        private const int RecordPaneWidth = 240;
        private const int OutlinePaneWidth = 420;

        private const string ToolTitle = "narrative editor";

        private const string SectionRowFormat = "{0}   ({1})";
        private const string RecordRowFormat = "{0}{1}";
        private const string ReadoutFormat = "{0}   —   {1} record(s), {2} file(s), {3} note(s)";

        private readonly List<Row> _rows = [];

        private ItemList _recordList = null!;
        private Tree _tree = null!;
        private OutlineTree _outline = null!;
        private InspectorPanel _inspector = null!;

        private CatalogWorkspace? _workspace;

        /// <summary>The catalog the record on screen came from, which is what says how its outline is
        /// read: a dialogue and a quest are two shapes and there is no general reading of either.</summary>
        private CatalogView? _view;

        private CatalogRecord? _record;

        /// <summary>What the inspector is standing on now — the record itself, or one node, option,
        /// stage, objective, route or ending inside it. Held to answer whether a redraw would put the
        /// panel where it already is: rebuilding it under the hand that is typing is what an author
        /// reads as the tool having eaten the word.</summary>
        private CatalogRecord? _shown;

        protected override string ToolName => ToolTitle;

        /// <summary>The file the history keys step: the one the record on screen lives in. Every row of
        /// the outline is an address inside that same record, so the outline never moves it.</summary>
        protected override JsonTreeDocument? Stepped => _record?.File.Document;

        protected override Control BuildBody()
        {
            _recordList = new ItemList
            {
                CustomMinimumSize = new Vector2(RecordPaneWidth, 0),
                SizeFlagsVertical = SizeFlags.ExpandFill
            };

            _tree = new Tree
            {
                CustomMinimumSize = new Vector2(OutlinePaneWidth, 0),
                SizeFlagsVertical = SizeFlags.ExpandFill,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };

            _outline = new OutlineTree(_tree);
            _inspector = new InspectorPanel { SizeFlagsHorizontal = SizeFlags.ExpandFill };

            // Two divides rather than one container holding all three panes: nested, each divider
            // starts at the minimum width of the pane before it and moves without touching the other.
            HSplitContainer body = Split();
            HSplitContainer right = Split();

            body.AddChild(_recordList);
            body.AddChild(right);
            right.AddChild(_tree);
            right.AddChild(Scrolled(_inspector));

            _recordList.ItemSelected += index => ShowRecord((int)index);
            _tree.ItemSelected += ShowElement;
            _inspector.Said += Report;

            return body;
        }

        protected override void Opened()
        {
            string root = ToolPaths.SharedDataRoot;

            Root = root;
            _workspace = GameNarrative.Load(root);

            var saver = new CatalogSaver(_workspace);
            saver.Changed += Refresh;
            Saver = saver;

            // The ids of every catalog of the run, for the fields that point at one. A dialogue names
            // npcs, items and quests, so this is most of what the inspector can say about a record here.
            _inspector.References = new ReferenceIndex(_workspace);

            FillRecords();

            Readout = Text(ReadoutFormat, root, _rows.Count - Sections(), FileCount(), _workspace.Report.Count);

            ShowRecord(First());
            ReportIssues(_workspace);
        }

        /// <summary>The marks on the records, the outline of the one on screen, and the inspector on the
        /// row of it the author is standing on.</summary>
        protected override void Redraw()
        {
            MarkRecords();
            _outline.Show(Outlined());
            ShowElement();
        }

        /// <summary>Writes a row for every record of every narrative catalog, under a heading naming the
        /// catalog and counting them. The headings are rows of the same list rather than a second list:
        /// the author picks one thing here, and which of the two catalogs it came from is the answer to a
        /// question he did not ask.</summary>
        private void FillRecords()
        {
            _recordList.Clear();
            _rows.Clear();

            if (_workspace is not { } workspace) return;

            foreach (CatalogView view in GameNarrative.Narrative(workspace))
            {
                int heading = _recordList.AddItem(Text(SectionRowFormat, view.Catalog, view.Records.Count));

                // A heading is not a record and cannot be opened: pressing one would leave the panes
                // showing the record before it while the list stands somewhere else.
                _recordList.SetItemDisabled(heading, true);
                _rows.Add(new Row(view, null));

                foreach (CatalogRecord record in view.Records)
                {
                    _recordList.AddItem(RecordRow(record));
                    _rows.Add(new Row(view, record));
                }
            }
        }

        /// <summary>A record is named by the id it carries now — the npc a dialogue belongs to, the id of
        /// a quest — and by a mark while its file is not on disk.</summary>
        private static string RecordRow(CatalogRecord record) =>
            Text(RecordRowFormat, Mark(CatalogSaver.IsDirty(record.File)), record.CurrentId);

        private void MarkRecords()
        {
            for (int index = 0; index < _rows.Count; index++)
                if (_rows[index].Record is { } record)
                    _recordList.SetItemText(index, RecordRow(record));
        }

        /// <summary>How many rows of the list name a catalog rather than a record.</summary>
        private int Sections() => _workspace is { } workspace ? GameNarrative.Narrative(workspace).Count : 0;

        private int FileCount()
        {
            if (_workspace is not { } workspace) return 0;

            int count = 0;

            foreach (CatalogView view in GameNarrative.Narrative(workspace)) count += view.Files.Count;

            return count;
        }

        /// <summary>The row the tool opens on: the first record of the first catalog that has one.</summary>
        private int First()
        {
            for (int index = 0; index < _rows.Count; index++)
                if (_rows[index].Record is not null)
                    return index;

            return NoSelection;
        }

        private void ShowRecord(int index)
        {
            // Before the record changes: moving away ends whatever keystrokes the outgoing one was
            // taking, so the name of one record and the name of the next are two steps and not one.
            _record?.File.Document.History.Seal();

            Row? row = index >= 0 && index < _rows.Count ? _rows[index] : null;

            _view = row?.View;
            _record = row?.Record;

            if (_record is not null) Show(_recordList, index);

            _outline.Show(Outlined());
            ShowElement();
            Refresh();
        }

        /// <summary>The record on screen as its author's structure, or nothing while none is open.</summary>
        private OutlineNode? Outlined() =>
            _view is { } view && _record is { } record ? GameNarrative.Outlined(view, record) : null;

        /// <summary>
        /// Puts the inspector on the row of the outline the author is standing on. Nothing is done when
        /// it is already standing there — every keystroke of every field arrives here through the
        /// refresh, and a panel rebuilt per letter is one nobody can type into.
        /// <para>The rebuild itself waits for the gesture that asked for it to be over: a row can go
        /// while the author is taking it out from inside the panel, and tearing the panel down from
        /// under the button being pressed is what the inspector defers its own redraws for.</para>
        /// </summary>
        private void ShowElement()
        {
            CatalogRecord? standing = Element(_outline.Selected);

            if (_shown is { } was && standing is { } now && ReferenceEquals(was.File, now.File) && was.Pointer == now.Pointer)
                return;

            _shown = standing;

            Callable.From(() => _inspector.Rebuild(_shown)).CallDeferred();
        }

        /// <summary>
        /// What the inspector edits for one row of the outline: the very node, option, line, stage,
        /// objective, route or ending it stands for, at its own address in the record's own file.
        /// <para>A row standing for a collection — the nodes of a dialogue, the stages of a quest — is
        /// answered with the record itself, which is what holds it. So is a row this build has no schema
        /// for: there is nothing to draw its fields by, and the record it belongs to is the nearest thing
        /// that can be edited at all.</para>
        /// </summary>
        private CatalogRecord? Element(OutlineNode? node)
        {
            if (_record is not { } record) return null;

            return node is { Schema: { } schema }
                ? record with { Pointer = node.Pointer, Schema = schema, Id = node.Label }
                : record;
        }

        /// <summary>One row of the list on the left: the catalog it belongs to, and the record it names —
        /// nothing at all for the row that names the catalog itself.</summary>
        private sealed record Row(CatalogView View, CatalogRecord? Record);
    }
}
