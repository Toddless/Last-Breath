namespace NarrativeEditor.Source.View
{
    using System;
    using System.Collections.Generic;
    using Core.Narrative.Facts;
    using Godot;
    using LastBreath.Descriptors;
    using Tooling.Catalogs;
    using Tooling.Editing.History;
    using Tooling.Localization;
    using static Tooling.Text.Format;

    /// <summary>
    /// Every fact of the world the narrative keeps, and who keeps it: the families the game's own code
    /// writes, the words the dialogues and the quests invent, and under each key the places that write it
    /// and the places that read it back.
    /// <para>The one thing a dialogue says to another dialogue is a fact key, and nothing in a document
    /// says who is listening. Pressing a place opens the record it is written in — a key read in a quest
    /// and raised in no conversation is the author one click from the option that should raise it.</para>
    /// </summary>
    public partial class FactsPanel : VBoxContainer
    {
        private const string Title = "facts";

        private const string ReadText = "Read";

        private const string FilterHint = "filter";

        private const string NotReadText = "press Read to list the facts as they are written now";

        private const string StaleText = "the documents changed: press Read again";

        private const string NoneText = "no fact key is written at all";

        private const string NoneMatchingFormat = "no fact key holds “{0}”";

        private const string ReadFormat = "{0} key(s), {1} written by nobody, {2} read by nobody";

        private const string RanFormat = "read the facts: {0} key(s)";

        private const string RefusedFormat = "the facts could not be read over the documents as they stand: {0}";

        /// <summary>A key and what stands on either side of it: how many places write it, how many read
        /// it back.</summary>
        private const string KeyFormat = "{0}   —   {1} writer(s), {2} reader(s)";

        private const string NeverWrittenMark = "   ✗ nobody writes it";

        private const string NeverReadMark = "   ✗ nobody reads it";

        /// <summary>How one place under a key is written: which side it is on, and where it stands.</summary>
        private const string WriterFormat = "        writes   {0}";

        private const string ReaderFormat = "        reads    {0}";

        /// <summary>What a key row says in full: the family it belongs to, for a word the code declares
        /// nothing about.</summary>
        private const string FamilyFormat = "one of the '{0}' the code keeps";

        private const string InventedText = "a word of the author's own: no code declares it";

        private const string DeclaredText = "a family the code keeps, with its parameters named";

        /// <summary>How tall the list asks to be before the divider above it is dragged.</summary>
        private const int PaneHeight = 240;

        private readonly List<Row> _rows = [];

        /// <summary>Everything the rows stop answering for the moment it is typed into.</summary>
        private readonly DocumentWatch _watch = new();

        private Button _read = null!;
        private LineEdit _filter = null!;
        private Label _summary = null!;
        private ItemList _list = null!;

        /// <summary>What the last reading came to, or nothing while none has been made. Held rather than
        /// re-read per keystroke of the filter: the filter narrows what is listed and does not ask the
        /// documents anything.</summary>
        private FactKeyRegistry? _facts;

        /// <summary>Nothing has been read yet, or the documents moved under what was.</summary>
        private bool _stale = true;

        /// <summary>Everything the tool has open. Every catalog and not the narrative alone: a reading is
        /// the game's own narrative run, which answers the ids too.</summary>
        public CatalogWorkspace? Workspace
        {
            get => _watch.Workspace;
            set => _watch.Workspace = value;
        }

        /// <summary>The stack the tool files every step on, watched for the same reason the checks watch
        /// it: a step files a change no document announces on its own.</summary>
        public EditHistory? History
        {
            get => _watch.History;
            set => _watch.History = value;
        }

        /// <summary>The ids of the run, as the shell already reads them for the inspector's pickers.</summary>
        public ReferenceIndex? References { get; set; }

        /// <summary>The .po files of the run; the narrative run reads them and this panel does not.</summary>
        public LocalizedTexts? Texts { get; set; }

        /// <summary>What the reading came to, for the status line of the shell.</summary>
        public event Action<string>? Said;

        /// <summary>The record one place belongs to: the catalog it is in and the id it answers to.</summary>
        public event Action<string, string>? Chose;

        public override void _Ready()
        {
            _watch.Changed += Invalidate;

            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            SizeFlagsVertical = SizeFlags.ExpandFill;

            _read = new Button { Text = ReadText };
            _read.Pressed += Start;

            _filter = new LineEdit { PlaceholderText = FilterHint, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            _filter.TextChanged += _ => Redraw();

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
            row.AddChild(_read);
            row.AddChild(_filter);

            AddChild(row);
            AddChild(_summary);
            AddChild(_list);

            Redraw();
        }

        public override void _ExitTree() => _watch.Stop();

        /// <summary>Reads every fact key of the narrative through the game's own run.</summary>
        /// <remarks>A run that throws is said in the status line and nothing else: the documents are being
        /// typed into, and a half-written record must not take the tool down with it.</remarks>
        private void Start()
        {
            if (Workspace is not { } workspace || References is not { } references) return;

            try
            {
                _facts = NarrativeFactKeys.Over(workspace, references, Texts).Keys;
            }
            catch (Exception failure)
            {
                Said?.Invoke(Text(RefusedFormat, failure.Message));
                return;
            }

            _stale = false;

            Said?.Invoke(Text(RanFormat, _facts.Keys.Count));
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

            _read.Disabled = Workspace is null || References is null;

            Fill();

            _summary.Text = Summary();
            _list.Clear();

            foreach (Row row in _rows)
            {
                int index = _list.AddItem(row.Text);

                _list.SetItemTooltip(index, row.Tooltip);
                _list.SetItemDisabled(index, row.Record is null);
            }
        }

        /// <summary>One row per key the filter names, and one under it per place writing or reading it.
        /// The places are rows of the same list rather than a column of their own: a key is answered by
        /// WHERE it is written, and a count on its own is a number nobody can act on.</summary>
        private void Fill()
        {
            _rows.Clear();

            if (_facts is not { } facts) return;

            foreach (FactKeyEntry key in NarrativeFactKeys.Matching(facts, _filter.Text))
            {
                _rows.Add(new Row(KeyText(key), KeyTooltip(key), null));

                foreach (string writer in key.Writers) _rows.Add(Address(WriterFormat, writer));
                foreach (string reader in key.Readers) _rows.Add(Address(ReaderFormat, reader));
            }
        }

        /// <summary>One place under a key. A place in the code names no record and can only be read: what
        /// writes a fact lives in four projects, and none of them is a document this tool has open.</summary>
        private static Row Address(string format, string where) =>
            new(Text(format, where), where, Place.Record(where));

        private static string KeyText(FactKeyEntry key)
        {
            string written = Text(KeyFormat, key.Key, key.Writers.Count, key.Readers.Count);

            if (key.NeverWritten) return written + NeverWrittenMark;

            return key.NeverRead ? written + NeverReadMark : written;
        }

        /// <summary>What a key says in full: whether the code declares it, and which family it belongs to
        /// when the code declares one it fits into.</summary>
        private static string KeyTooltip(FactKeyEntry key)
        {
            if (key.Declared) return DeclaredText;

            return key.Family is { } family ? Text(FamilyFormat, family) : InventedText;
        }

        private string Summary()
        {
            if (_stale) return _facts is null ? NotReadText : StaleText;
            if (_facts is not { } facts) return NotReadText;
            if (facts.Keys.Count == 0) return NoneText;
            if (_rows.Count == 0) return Text(NoneMatchingFormat, _filter.Text);

            int unwritten = 0;
            int unread = 0;

            foreach (FactKeyEntry key in facts.Keys)
            {
                if (key.NeverWritten) unwritten++;
                if (key.NeverRead) unread++;
            }

            return Text(ReadFormat, facts.Keys.Count, unwritten, unread);
        }

        /// <summary>Opens the record one place belongs to. A row naming a key, or a place in the code,
        /// belongs to no record and cannot be opened.</summary>
        private void Open(int index)
        {
            if (index < 0 || index >= _rows.Count) return;
            if (_rows[index].Record is not { } record) return;

            Chose?.Invoke(record.Catalog, record.Id);
        }

        /// <summary>One line of the list: what it says, what it says in full, and the record it can be
        /// opened to — nothing at all for a row naming a key or a place in the code.</summary>
        private sealed record Row(string Text, string Tooltip, (string Catalog, string Id)? Record);
    }
}
