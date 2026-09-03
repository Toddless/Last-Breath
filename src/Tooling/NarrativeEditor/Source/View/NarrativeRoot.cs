namespace NarrativeEditor.Source.View
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using App;
    using Godot;
    using LastBreath.Descriptors;
    using Tooling.Catalogs;
    using Tooling.Localization;
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

        /// <summary>What the two readings of a record are named on their tabs.</summary>
        private const string DryRunTabName = "dry run";

        private const string ChecksTabName = "checks";

        private const string SectionRowFormat = "{0}   ({1})";
        private const string RecordRowFormat = "{0}{1}";
        private const string ReadoutFormat = "{0}   —   {1} record(s), {2} file(s), {3} note(s)";

        private const string NoTextsFormat = "the locales under {0} could not be read: {1}";

        private readonly List<Row> _rows = [];

        /// <summary>What the run could not read beyond what the narrative catalogs reported: the locales,
        /// when the folder holding them is not there. Kept because the count of them is on the status line
        /// and the list of them is in the dialog, and the two have to be one list.</summary>
        private readonly List<string> _notes = [];

        private ItemList _recordList = null!;
        private OutlineTree _outline = null!;
        private InspectorPanel _inspector = null!;
        private DryRunPanel _dryRun = null!;
        private ChecksPanel _checks = null!;

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

        protected override Control BuildBody()
        {
            _recordList = new ItemList
            {
                CustomMinimumSize = new Vector2(RecordPaneWidth, 0),
                SizeFlagsVertical = SizeFlags.ExpandFill
            };

            var tree = new Tree
            {
                CustomMinimumSize = new Vector2(OutlinePaneWidth, 0),
                SizeFlagsVertical = SizeFlags.ExpandFill,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };

            _outline = new OutlineTree(tree);
            _inspector = new InspectorPanel { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            _dryRun = new DryRunPanel();
            _checks = new ChecksPanel { Name = ChecksTabName };

            // Two divides rather than one container holding all three panes: nested, each divider
            // starts at the minimum width of the pane before it and moves without touching the other.
            HSplitContainer body = Split();
            HSplitContainer right = Split();

            // The run stands under the panel that edits, on a divide of its own: an author reads a
            // dialogue by walking it and writes it in the fields above, and the two are one gesture.
            var edited = new VSplitContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill
            };

            // Two readings of one record, side by side under the same divide: walking the conversation,
            // and holding it against everything outside it. Tabs and not a third pane — an author does
            // one or the other, and both at once would leave neither enough of the window to be read in.
            var read = new TabContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill
            };

            ScrollContainer walked = Scrolled(_dryRun);
            walked.Name = DryRunTabName;

            body.AddChild(_recordList);
            body.AddChild(right);
            right.AddChild(tree);
            right.AddChild(edited);
            edited.AddChild(Scrolled(_inspector));
            edited.AddChild(read);
            read.AddChild(walked);
            read.AddChild(_checks);

            _recordList.ItemSelected += index => ShowRecord((int)index);
            tree.ItemSelected += ShowElement;
            _inspector.Said += Report;
            _dryRun.Said += Report;
            _checks.Said += Report;
            _checks.Chose += OpenRecord;

            return body;
        }

        protected override void Opened()
        {
            string root = ToolPaths.SharedDataRoot;

            Root = root;
            _workspace = GameNarrative.Load(root, History);
            _dryRun.Workspace = _workspace;

            Saver = new CatalogSaver(_workspace);

            // The ids of every catalog of the run, for the fields that point at one. A dialogue names
            // npcs, items and quests, so this is most of what the inspector can say about a record here.
            var references = new ReferenceIndex(_workspace);

            _inspector.References = references;
            _checks.References = references;
            _checks.Workspace = _workspace;

            // The conditions and the actions: the schema can only call them free json, and this is what
            // tells the inspector which key is written from which of the two vocabularies.
            _inspector.Vocabularies = NarrativeFieldVocabularies.Resolve;

            _notes.Clear();
            LoadTexts();

            FillRecords();

            // The notes of the narrative catalogs alone: the run reads every catalog of the game for the
            // ids the narrative points at, and what another one could not answer is not this author's.
            IReadOnlyList<string> report = GameNarrative.Report(_workspace);

            Readout = Text(
                ReadoutFormat, root, _rows.Count - Sections(), FileCount(), report.Count + _notes.Count);

            ShowRecord(First());
            ReportIssues(_workspace, _notes, report);
        }

        /// <summary>Reads the locales that sit beside the catalogs. A run that cannot open them goes on
        /// working on the data and says so once: the wording is one part of the tool, and a folder that is
        /// not there is no reason to refuse the rest of it — the block of text is simply not drawn, and the
        /// rows of the outline fall back to what the engine loaded.</summary>
        private void LoadTexts()
        {
            string folder = ToolPaths.LocalizationRoot;

            try
            {
                LocalizedTexts texts = LocalizedTexts.Load(folder, History);

                Texts = texts;
                _inspector.Texts = texts;
                _outline.Texts = texts;
                _dryRun.Texts = texts;
                _checks.Texts = texts;
            }
            catch (Exception failure) when (failure is IOException or UnauthorizedAccessException
                                               or FormatException or ArgumentException)
            {
                _notes.Add(Text(NoTextsFormat, folder, failure.Message));
            }
        }

        /// <summary>The marks on the records, the outline of the one on screen, and the inspector on the
        /// row of it the author is standing on.</summary>
        protected override void Redraw()
        {
            MarkRecords();
            _outline.Show(Outlined());
            ShowElement();
            _dryRun.Standing(_record, IsDialogue());
        }

        /// <summary>A step through the history may have moved a line of a locale: the inspector copied it
        /// into its boxes, and nothing about the record's own file would say it has changed.</summary>
        protected override void Stepped()
        {
            _shown = null;
            ShowElement();
        }

        /// <summary>Whether the record on screen is a conversation. Only those can be walked: a quest is
        /// read through the dialogue that offers it.</summary>
        private bool IsDialogue() => _view is { } view && GameNarrative.IsDialogue(view);

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

            // The outline and the inspector are drawn by the refresh, which is what draws them after every
            // other change too: doing it here as well would build the same tree twice per record opened.
            Refresh();
        }

        /// <summary>Opens the record a finding belongs to. Nothing happens when this run has no such
        /// record: a check reads every catalog of the game, and the id it names may belong to one this
        /// tool does not list.</summary>
        private void OpenRecord(string catalog, string id)
        {
            int index = _rows.FindIndex(row =>
                row.Record is { } record
                && string.Equals(row.View.Catalog, catalog, StringComparison.Ordinal)
                && string.Equals(record.CurrentId, id, StringComparison.Ordinal));

            if (index >= 0) ShowRecord(index);
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

            if (Same(_shown, standing)) return;

            _shown = standing;

            Callable.From(() => _inspector.Rebuild(_shown, Suffixes(_shown))).CallDeferred();
        }

        /// <summary>Whether two rows are the same place of the same file — nothing on both sides included,
        /// which is where the tool stands while no record is open.</summary>
        private static bool Same(CatalogRecord? one, CatalogRecord? other)
        {
            if (one is null || other is null) return one is null && other is null;

            return ReferenceEquals(one.File, other.File) && one.Pointer == other.Pointer;
        }

        /// <summary>What the row on screen words its own text from: the suffixes the catalog declares while
        /// the inspector stands on the record itself, and none while it stands on something inside it. A
        /// node, a line, an option or a stage is not worded from an id — every one of them writes out the
        /// key it is read under — and offering boxes under keys nothing reads would be inviting the author
        /// to write into the void.</summary>
        private IReadOnlyList<string> Suffixes(CatalogRecord? shown) =>
            _view is { } view && shown is { } row && _record is { } record && row.Pointer == record.Pointer
                ? view.Schema.LocalizedSuffixes
                : [];

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
