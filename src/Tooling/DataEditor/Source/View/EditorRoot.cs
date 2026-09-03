namespace DataEditor.Source.View
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using App;
    using Godot;
    using LastBreath.Descriptors;
    using Tooling.Catalogs;
    using Tooling.Localization;
    using Tooling.Schema.Model;
    using Tooling.Ui;
    using static Tooling.Text.Format;

    /// <summary>
    /// Composition root of the data editor: three panes built in code — the catalogs the game
    /// describes, the records of the chosen one, and the record itself — under the shell every tool of
    /// this repository wears. The scene file holds nothing but this node; there is no designer to hand a
    /// .tscn to, so the layout lives where it can be reviewed as code.
    /// <para>Every edit goes onto the one history of the run, so undo takes back the last thing the
    /// author did — a value in any file, a text in any locale, a record renamed in both at once. Saving
    /// is the same gesture over the whole run: Ctrl+S writes every changed file, because the author
    /// edits records and has no reason to remember which catalog each of them came from.</para>
    /// </summary>
    public partial class EditorRoot : ToolShell
    {
        private const int CatalogPaneWidth = 220;
        private const int RecordPaneWidth = 260;
        private const int RecordDialogWidth = 460;
        private const int RecordDialogHeight = 220;
        private const int DeleteDialogWidth = 460;
        private const int DeleteDialogHeight = 140;

        /// <summary>Width of the name column of the little dialogs, so what is asked for lines up as one
        /// column the way the inspector's fields do.</summary>
        private const int DialogNameWidth = 90;

        private const string ToolTitle = "data editor";

        private const string CatalogRowFormat = "{0}{1}   ({2})";
        private const string RecordRowFormat = "{0}   ·  {1}";
        private const string ReadoutFormat =
            "{0}   —   {1} catalog(s) of {2} described, {3} file(s), {4} record(s), {5} note(s)";

        private const string AddRecordText = "+";
        private const string CopyRecordText = "⧉";
        private const string RemoveRecordText = "×";

        private const string AddRecordHint = "write a new record into this catalog";
        private const string CopyRecordHint = "write a copy of this record beside it";
        private const string RemoveRecordHint = "take this record out of its file";
        private const string NoCatalogHint = "no catalog is open";
        private const string NoRecordHint = "no record is selected";
        private const string OneRecordHint = "this catalog is written as one record";

        private const string NewRecordTitle = "New record";
        private const string CopyRecordTitle = "Duplicate record";
        private const string RemoveRecordTitle = "Take the record out";

        /// <summary>What a section standing at the root of its file is called; it has no key of its own.</summary>
        private const string RootSectionName = "the document";

        private const string IdName = "id";
        private const string SectionName = "section";
        private const string FileName = "file";

        private const string IdHint = "the id the game reads this record by";
        private const string SectionHint = "the section of the catalog the record is written under";
        private const string FileHint = "the file the record is written to";
        private const string SlotHint = "the value the catalog splits its files by; it is written into the record";

        /// <summary>What the author is told about a file he may name himself: a catalog nothing splits is
        /// split by hand, so a name it does not carry yet is a new file and not a mistake.</summary>
        private const string NewFileHint =
            "the file the record is written to; a name this catalog has not got yet lays a new one down";

        /// <summary>What the list beside the name is for where the name is typed: the files that are
        /// there already, so the author writes into one of them without remembering how it is spelled.</summary>
        private const string HeldFilesHint = "the files this catalog is already written across";

        /// <summary>What a copy is offered under before the author names it. A name is required and no
        /// catalog may write one twice, so the copy has to arrive already carrying a free one.</summary>
        private const string CopySuffix = "_Copy";

        private const string NoTextsFormat = "the locales under {0} could not be read: {1}";

        private const string RemoveQuestionFormat = "Take “{0}” out of {1}?";
        private const string AddedFormat = "added {0}";
        private const string CopiedFormat = "duplicated as {0}";
        private const string RemovedFormat = "took {0} out";

        private ItemList _catalogList = null!;
        private ItemList _recordList = null!;
        private InspectorPanel _inspector = null!;

        private Button _addButton = null!;
        private Button _copyButton = null!;
        private Button _removeButton = null!;

        private ConfirmationDialog _recordDialog = null!;
        private LineEdit _idBox = null!;
        private LineEdit _fileBox = null!;
        private OptionButton _sectionPicker = null!;
        private OptionButton _filePicker = null!;
        private Control _sectionRow = null!;
        private Control _fileRow = null!;
        private ConfirmationDialog _removeDialog = null!;

        private CatalogWorkspace? _workspace;
        private CatalogView? _catalog;
        private CatalogRecord? _record;

        /// <summary>Whether the dialog on screen is asking for a record to copy rather than for a fresh
        /// one. The two ask for the same thing — a name nobody has used — and differ only in what is
        /// written once it is given.</summary>
        private bool _copying;

        /// <summary>Where the list is standing: the row of the record on screen, so it can be written
        /// again when the id it is named by is edited, and the row the last record stood in while there
        /// is none — a record taken out leaves a place, and an undo that puts it back puts it there.</summary>
        private int _recordIndex = NoSelection;

        /// <summary>What the run could not read beyond what the catalogs themselves reported: the locales,
        /// when the folder holding them is not there. Kept because the count of them is on the status line
        /// and the list of them is in the dialog, and the two have to be one list.</summary>
        private readonly List<string> _notes = [];

        protected override string ToolName => ToolTitle;

        /// <summary>The records of the catalog, under the three things that may be done to the list
        /// itself. The buttons sit above the list and not beside a row: two of them speak about the row
        /// selected and the third about no row at all, and one place to look for all three is what makes
        /// them findable.</summary>
        private Control RecordPane()
        {
            var pane = new VBoxContainer
            {
                CustomMinimumSize = new Vector2(RecordPaneWidth, 0),
                SizeFlagsVertical = SizeFlags.ExpandFill
            };

            var actions = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

            _addButton = ActionButton(AddRecordText, AddRecordHint, AskForNewRecord);
            _copyButton = ActionButton(CopyRecordText, CopyRecordHint, AskForCopy);
            _removeButton = ActionButton(RemoveRecordText, RemoveRecordHint, AskToRemove);

            actions.AddChild(_addButton);
            actions.AddChild(_copyButton);
            actions.AddChild(_removeButton);

            pane.AddChild(actions);
            pane.AddChild(_recordList);

            return pane;
        }

        protected override Control BuildBody()
        {
            _catalogList = new ItemList
            {
                CustomMinimumSize = new Vector2(CatalogPaneWidth, 0),
                SizeFlagsVertical = SizeFlags.ExpandFill
            };

            _recordList = new ItemList
            {
                CustomMinimumSize = new Vector2(RecordPaneWidth, 0),
                SizeFlagsVertical = SizeFlags.ExpandFill
            };

            _inspector = new InspectorPanel { SizeFlagsHorizontal = SizeFlags.ExpandFill };

            // Two divides rather than one container holding all three panes: nested, each divider
            // starts at the minimum width of the pane before it and moves without touching the other.
            HSplitContainer body = Split();
            HSplitContainer right = Split();

            body.AddChild(_catalogList);
            body.AddChild(right);
            right.AddChild(RecordPane());
            right.AddChild(Scrolled(_inspector));

            BuildRecordDialog();
            BuildRemoveDialog();

            _catalogList.ItemSelected += index => ShowCatalog((int)index);
            _recordList.ItemSelected += index => ShowRecord((int)index);
            _inspector.Said += Report;

            return body;
        }

        protected override void Opened() => LoadCatalogs();

        private static Button ActionButton(string text, string hint, Action pressed)
        {
            var button = new Button { Text = text, TooltipText = hint, SizeFlagsHorizontal = SizeFlags.ExpandFill };

            button.Pressed += pressed;

            return button;
        }

        /// <summary>One thing the dialog asks for, as a row: what it is called, and the control it is
        /// answered with.</summary>
        private static Control DialogRow(string name, Control value, string hint)
        {
            var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

            row.AddChild(new Label
            {
                Text = name,
                TooltipText = hint,
                MouseFilter = MouseFilterEnum.Pass,
                CustomMinimumSize = new Vector2(DialogNameWidth, 0)
            });

            row.AddChild(value);

            return row;
        }

        /// <summary>
        /// What a record has to be given before it can be written: a name, and — where the catalog does
        /// not answer it on its own — the section it is written under and the file it goes to. Built once
        /// and filled per catalog; the rows the catalog answers itself are hidden rather than shown
        /// empty, because a picker with one entry is a question that was never asked.
        /// <para>The file is asked for two ways in one row, and which of them is shown is the catalog's
        /// to say. A catalog split by a field of the record may only be answered with a value of that
        /// field, so it gets the list; a catalog split by nothing at all is split by the author, so it
        /// gets a box he may type a name the catalog has never had into — otherwise the first record of
        /// an empty folder is one the tool cannot be asked to write.</para>
        /// </summary>
        private void BuildRecordDialog()
        {
            _recordDialog = new ConfirmationDialog { Title = NewRecordTitle, DialogText = string.Empty };

            var body = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

            _idBox = new LineEdit { SizeFlagsHorizontal = SizeFlags.ExpandFill, PlaceholderText = IdName };

            _fileBox = new LineEdit
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                PlaceholderText = FileName,
                TooltipText = NewFileHint
            };

            _sectionPicker = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            _filePicker = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };

            var file = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

            file.AddChild(_fileBox);
            file.AddChild(_filePicker);

            _sectionRow = DialogRow(SectionName, _sectionPicker, SectionHint);
            _fileRow = DialogRow(FileName, file, FileHint);

            body.AddChild(DialogRow(IdName, _idBox, IdHint));
            body.AddChild(_sectionRow);
            body.AddChild(_fileRow);

            _recordDialog.AddChild(body);

            // The section decides which record schema is being written, and the value a catalog splits
            // its files by is a field of that schema: change one and the other has to be offered again.
            _sectionPicker.ItemSelected += _ => FillFilePicker();
            _filePicker.ItemSelected += index => TakeFileName((int)index);

            // Enter answers the dialog: a name typed into the box and submitted to nothing reads as the
            // tool having refused the word.
            _recordDialog.RegisterTextEnter(_idBox);
            _recordDialog.RegisterTextEnter(_fileBox);
            _recordDialog.Confirmed += WriteRecord;

            AddChild(_recordDialog);
        }

        private void BuildRemoveDialog()
        {
            _removeDialog = new ConfirmationDialog { Title = RemoveRecordTitle };

            _removeDialog.Confirmed += RemoveRecord;

            AddChild(_removeDialog);
        }

        /// <summary>
        /// Reads every described catalog from the data root of this run and shows the first of them.
        /// </summary>
        private void LoadCatalogs()
        {
            string root = ToolPaths.SharedDataRoot;

            Root = root;
            _workspace = GameCatalogs.Load(root, History);

            Saver = new CatalogSaver(_workspace);

            // The ids of every catalog of the run, for the fields that point at one. Built here because
            // it answers about the run as a whole and the panel is shown one record at a time.
            _inspector.References = new ReferenceIndex(_workspace);

            // The conditions and the actions of the narrative: the dialogues and the quests are among the
            // catalogs listed here, and their vocabulary keys read as free json without this.
            _inspector.Vocabularies = NarrativeFieldVocabularies.Resolve;

            // The catalogs whose records are drawn by a form of their own rather than field by field.
            _inspector.Forms = Form;

            _notes.Clear();
            LoadTexts();

            _catalogList.Clear();

            foreach (CatalogView view in _workspace.Catalogs) _catalogList.AddItem(CatalogRow(view));

            Readout = Text(
                ReadoutFormat,
                root,
                _workspace.Catalogs.Count,
                GameCatalogs.TotalCount,
                _workspace.FileCount,
                _workspace.RecordCount,
                _workspace.Report.Count + _notes.Count);

            ShowCatalog(_workspace.Catalogs.Count > 0 ? 0 : NoSelection);

            ReportIssues(_workspace, _notes);
        }

        /// <summary>
        /// The form one record is drawn by, where its catalog has one: a loot table is read as which
        /// thing sits in which tier and for how much, which is the question its author asks and the one
        /// thing four levels of nested lists cannot be read as.
        /// <para>Null for every other catalog, which leaves the record drawn field by field the way it
        /// always was. Asked of the open catalog rather than of the record: the record is one of the
        /// catalog's own, and what draws it is the catalog's answer.</para>
        /// </summary>
        private Control? Form(CatalogRecord record)
        {
            if (_catalog is not { } view || view.Catalog != LootTableFormKeys.Catalog) return null;

            var panel = new LootTablePanel
            {
                Layout = LootTableFormKeys.Layout,
                References = _inspector.References,
                Gestures = _inspector.Gestures,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };

            panel.Said += Report;
            panel.Rebuild(record);

            return panel;
        }

        /// <summary>Reads the locales that sit beside the catalogs. A run that cannot open them goes on
        /// working on the data and says so once: the wording is one part of the tool, and a folder that is
        /// not there is no reason to refuse the rest of it — the block of text is simply not drawn.</summary>
        private void LoadTexts()
        {
            string folder = ToolPaths.LocalizationRoot;

            try
            {
                LocalizedTexts texts = LocalizedTexts.Load(folder, History);

                Texts = texts;
                _inspector.Texts = texts;
            }
            catch (Exception failure) when (failure is IOException or UnauthorizedAccessException
                                               or FormatException or ArgumentException)
            {
                _notes.Add(Text(NoTextsFormat, folder, failure.Message));
            }
        }

        /// <summary>Shows the records of one catalog, and the first of them. Selecting the row in the
        /// list as well, because the catalog is also chosen without a click — the tool opens on one.</summary>
        private void ShowCatalog(int index)
        {
            _catalog = _workspace is not null && index >= 0 && index < _workspace.Catalogs.Count
                ? _workspace.Catalogs[index]
                : null;

            if (_catalog is not null) Show(_catalogList, index);

            FillRecords();

            ShowRecord(_catalog is { Records.Count: > 0 } ? 0 : NoSelection);
        }

        /// <summary>Writes a row for every record the catalog holds now.</summary>
        private void FillRecords()
        {
            _recordList.Clear();

            if (_catalog is not { } view) return;

            foreach (CatalogRecord record in view.Records) _recordList.AddItem(RecordRow(record));
        }

        /// <summary>A catalog is named by the records in it, and by a mark while one of its files is not
        /// on disk — the author picks a catalog here, and this is where "there is work in that one" has
        /// to be readable without opening it.</summary>
        private static string CatalogRow(CatalogView view) =>
            Text(CatalogRowFormat, Mark(CatalogSaver.IsDirty(view)), view.Catalog, view.Records.Count);

        /// <summary>A record is named by the id it carries now, and by its file too when the catalog is
        /// written across several — two records of one catalog may share an id only because they sit in
        /// different files, and the list has to be able to say which is which.</summary>
        private string RecordRow(CatalogRecord record) =>
            _catalog is { Files.Count: > 1 }
                ? Text(RecordRowFormat, record.CurrentId, record.File.Name)
                : record.CurrentId;

        private void ShowRecord(int index)
        {
            // Before the record changes, or the run being closed would be the incoming record's: moving
            // away ends whatever keystrokes the outgoing one was taking, so the name of one record and
            // the name of the next are two steps and not one.
            _record?.File.Document.History.Seal();

            _record = _catalog is not null && index >= 0 && index < _catalog.Records.Count
                ? _catalog.Records[index]
                : null;

            // The place is kept when the record goes: a row that stood for a record taken out is where
            // the author is looking, and where an undo puts the record back.
            if (_record is not null)
            {
                _recordIndex = index;
                Show(_recordList, index);
            }

            _inspector.Rebuild(_record, Suffixes(), Neighbour(index));
            Refresh();
        }

        /// <summary>What the open catalog words its records' localization keys with — a name, a name and a
        /// description, or nothing at all.</summary>
        private IReadOnlyList<string> Suffixes()
        {
            if (_catalog is not { } view) return [];

            return view.Schema.LocalizedSuffixes;
        }

        /// <summary>The record standing before this one in the catalog. A record laid down now writes its
        /// keys after that one's, which is what keeps a fresh record inside the section its neighbours are
        /// in; nothing at the top of a catalog, and the keys then go to the end of the files.</summary>
        private string? Neighbour(int index)
        {
            if (_catalog is not { } view || index <= 0 || index > view.Records.Count) return null;

            return view.Records[index - 1].CurrentId;
        }

        // ── records ────────────────────────────────────────────────────────────────────────────

        /// <summary>Asks for the name a fresh record is written under, together with whatever the catalog
        /// itself does not answer: which section it belongs to, and which file it goes to.</summary>
        private void AskForNewRecord()
        {
            if (_catalog is not { } view) return;

            CommitTyping();

            _copying = false;
            _recordDialog.Title = NewRecordTitle;
            _idBox.Text = string.Empty;

            FillSectionPicker(view);
            FillFilePicker();
            AskForName();
        }

        /// <summary>Asks for the name a copy is written under. Nothing else is asked: a copy stands in the
        /// section and the file of the record it was taken from, which is where its author is looking.</summary>
        private void AskForCopy()
        {
            if (_record is not { } record) return;

            CommitTyping();

            _copying = true;
            _recordDialog.Title = CopyRecordTitle;
            _idBox.Text = record.CurrentId + CopySuffix;
            _sectionRow.Visible = false;
            _fileRow.Visible = false;

            AskForName();
        }

        /// <summary>Shows the dialog with the keyboard already in the name box, and whatever is in it
        /// selected: the one thing every one of these gestures needs is a name typed straight away.
        /// Taken once the dialog is on screen — a window still being opened has no focus to hand out.</summary>
        private void AskForName()
        {
            _recordDialog.PopupCentered(new Vector2I(RecordDialogWidth, RecordDialogHeight));

            Callable.From(() =>
            {
                _idBox.GrabFocus();
                _idBox.SelectAll();
            }).CallDeferred();
        }

        /// <summary>Asks before taking a record out, naming the record and the file it would leave. The
        /// step is on the file's history like any other, so it can be taken back — and a gesture that
        /// empties a screen without asking reads as the tool having lost the work.</summary>
        private void AskToRemove()
        {
            if (_record is not { } record) return;

            CommitTyping();

            _removeDialog.DialogText = Text(RemoveQuestionFormat, record.CurrentId, record.File.Name);
            _removeDialog.PopupCentered(new Vector2I(DeleteDialogWidth, DeleteDialogHeight));
        }

        /// <summary>Offers the sections of the catalog, and only when it writes more than one: a catalog
        /// with a single section answers the question itself.</summary>
        private void FillSectionPicker(CatalogView view)
        {
            _sectionPicker.Clear();

            foreach (SectionSchema section in view.Schema.Sections)
                _sectionPicker.AddItem(section.Key.Length == 0 ? RootSectionName : section.Key);

            _sectionPicker.Selected = view.Schema.Sections.Count > 0 ? 0 : NoSelection;
            _sectionRow.Visible = view.Schema.Sections.Count > 1;
        }

        /// <summary>Offers what decides the file: the files the catalog is written across when nothing
        /// about a record says which of them it belongs in, or the values it is split by when something
        /// does. A catalog whose rule answers on its own is asked nothing.
        /// <para>Where the author names the file himself the list stands beside the box as a reminder and
        /// not as the answer, so a catalog with no files yet still has the question put to it.</para></summary>
        private void FillFilePicker()
        {
            _filePicker.Clear();

            if (_catalog is not { } view)
            {
                _fileRow.Visible = false;
                return;
            }

            bool split = view.Schema.Placement is FieldFilePlacement;
            bool named = view.Schema.Placement is FreeFilePlacement;
            IReadOnlyList<string> offered = Offers(view, CatalogEditing.Section(view, ChosenSection(view)));

            foreach (string name in offered) _filePicker.AddItem(name);

            _filePicker.Selected = offered.Count > 0 ? 0 : NoSelection;
            _filePicker.TooltipText = split ? SlotHint : HeldFilesHint;
            _filePicker.Visible = offered.Count > 0;

            // The box opens on the file the list stands on: a record is written into one the catalog
            // already has far more often than into one it has not.
            _fileBox.Visible = named;
            _fileBox.Text = named && offered.Count > 0 ? offered[0] : string.Empty;

            _fileRow.Visible = named || offered.Count > 0;
        }

        /// <summary>Writes the file the author picked out of the ones the catalog has into the box he
        /// answers with: where the name is typed, the list is how it is typed without spelling it.</summary>
        private void TakeFileName(int index)
        {
            if (!_fileBox.Visible || index < 0 || index >= _filePicker.ItemCount) return;

            _fileBox.Text = _filePicker.GetItemText(index);
        }

        /// <summary>What the author may be offered as the file of a new record: the values the catalog
        /// splits its files by, or the files it already has. Nothing at all where the catalog writes one
        /// file and every record goes there.</summary>
        private static IReadOnlyList<string> Offers(CatalogView view, SectionSchema? section) =>
            view.Schema.Placement switch
            {
                FieldFilePlacement placed => Members(section, placed.FieldName),
                FreeFilePlacement => [.. view.Files.Select(file => file.BaseName)],
                _ => []
            };

        /// <summary>The values one field of a record may be written with, when the schema lists them.</summary>
        private static IReadOnlyList<string> Members(SectionSchema? section, string jsonName)
        {
            if (section is null) return [];

            foreach (FieldSchema field in section.Record.Fields)
                if (string.Equals(field.JsonName, jsonName, StringComparison.Ordinal))
                    return [.. field.EnumValues];

            return [];
        }

        /// <summary>The key of the section the dialog stands on, or nothing when the catalog writes one
        /// section and never asked.</summary>
        private string? ChosenSection(CatalogView view) =>
            _sectionRow.Visible && _sectionPicker.Selected >= 0 && _sectionPicker.Selected < view.Schema.Sections.Count
                ? view.Schema.Sections[_sectionPicker.Selected].Key
                : null;

        /// <summary>What the dialog was answered with about the file: the name the author typed where he
        /// names it, and the value he picked where the catalog splits its files by one. An empty box is
        /// handed on as it is — the refusal and its reason belong to the rules and not to the window.</summary>
        private string? ChosenFile()
        {
            if (!_fileRow.Visible) return null;
            if (_fileBox.Visible) return _fileBox.Text.Trim();

            return _filePicker.Selected >= 0 ? _filePicker.GetItemText(_filePicker.Selected) : null;
        }

        /// <summary>Writes what the dialog was asked for: a fresh record, or a copy of the one on screen.</summary>
        private void WriteRecord()
        {
            if (_catalog is not { } view) return;

            string id = _idBox.Text.Trim();

            if (_copying && _record is { } record)
            {
                Told(CatalogEditing.DuplicateRecord(view, record, id), CopiedFormat, id);
                return;
            }

            Told(CatalogEditing.AddRecord(view, ChosenSection(view), ChosenFile(), id), AddedFormat, id);
        }

        private void RemoveRecord()
        {
            if (_catalog is not { } view || _record is not { } record) return;

            // The name is read before the record is taken out: the id is asked of the document, and a
            // record no longer in it falls back to the name the file was opened under — so a record
            // renamed during the run would be reported gone under a word nobody on screen can see.
            string id = record.CurrentId;

            Told(CatalogEditing.RemoveRecord(view, record), RemovedFormat, id);
        }

        /// <summary>Says on the status line what the gesture came to — the record it wrote, or the reason
        /// it refused — and puts the record it left standing on screen.</summary>
        private void Told(CatalogEditResult result, string format, string what)
        {
            Action = result.Note ?? Text(format, what);

            Refresh();

            int index = IndexOf(result.Record);

            // Refresh has already put the list right, and it may have landed on this very record: the
            // panel is built again only when there is another one to show. Asked of the record on
            // screen and not of the place the list stands in, because the place outlives the record.
            if (index != NoSelection && index != IndexOf(_record)) ShowRecord(index);
        }

        /// <summary>Where a record stands in the catalog now. Found by its file and its address rather
        /// than by the object: the list is read again after every change, and what is held from before it
        /// is a record of the same file at the same place and not the same instance.</summary>
        private int IndexOf(CatalogRecord? record)
        {
            if (_catalog is not { } view || record is null) return NoSelection;

            for (int index = 0; index < view.Records.Count; index++)
                if (ReferenceEquals(view.Records[index].File, record.File) && view.Records[index].Pointer == record.Pointer)
                    return index;

            return NoSelection;
        }

        /// <summary>The marks on the catalogs, the rows of the records, and what the buttons over them
        /// may do now.</summary>
        protected override void Redraw()
        {
            MarkCatalogs();
            SyncRecords();
            EnableActions();
        }

        /// <summary>A step through the history may have moved a text of a locale: the inspector copied it
        /// into its boxes, and nothing about the record's own file would say it has changed.</summary>
        protected override void Stepped() => _inspector.Rebuild(_record, Suffixes(), Neighbour(_recordIndex));

        /// <summary>
        /// Brings the list of records up to what the catalog holds now. The catalog is read again first:
        /// a record written, copied or taken out — by a button of this tool or by one press of undo —
        /// moves the addresses of everything below it, and a list of the records from before that would
        /// open the wrong one.
        /// <para>The rows are written again only when there are more or fewer of them than the list
        /// shows. Every keystroke of every field arrives here, and rebuilding a list of hundreds of rows
        /// per letter is a stutter with nothing to show for it.</para>
        /// </summary>
        private void SyncRecords()
        {
            if (_catalog is not { } view)
            {
                NameRecordRow();
                return;
            }

            view.Reread();

            if (_recordList.ItemCount == view.Records.Count)
            {
                NameRecordRow();
                return;
            }

            // The record on screen if it is still there, and its neighbour when it is not: an undo that
            // takes back a creation, and a record just taken out, both leave the author looking at the
            // place the record stood in rather than at nothing.
            int found = IndexOf(_record);
            int at = found != NoSelection ? found : Neighbour(view);

            FillRecords();
            ShowRecord(at);
        }

        /// <summary>The place the record that has gone stood in: the row that took it, or the one just
        /// above it, as long as that row is a record of the same file. A row of another file is not the
        /// place the author was standing in, and the history keys answer for the file on screen — landing
        /// on it would point undo at a file the author never worked in, so nothing is selected instead.
        /// <para>With no record on screen at all the place itself is the answer: the list grew back under
        /// an undo of the gesture that emptied it, and the row it stood in is the record put back.</para></summary>
        private int Neighbour(CatalogView view)
        {
            int taken = Math.Min(_recordIndex, view.Records.Count - 1);

            if (_record is not { } gone) return taken;
            if (Holds(view, taken, gone.File)) return taken;

            return Holds(view, taken - 1, gone.File) ? taken - 1 : NoSelection;
        }

        /// <summary>Whether the catalog lists a record of that file at that place.</summary>
        private static bool Holds(CatalogView view, int index, CatalogFile file) =>
            index >= 0 && index < view.Records.Count && ReferenceEquals(view.Records[index].File, file);

        /// <summary>What the buttons over the list may do now, with the reason on the ones that may do
        /// nothing: a button that answers a press with silence reads as the tool having missed it.</summary>
        private void EnableActions()
        {
            // A catalog written as one record is the file itself: there is nothing to add to it, nothing
            // to copy and nothing to take out of it, and saying so on the button is what keeps a press
            // from being answered with a dialog that can only end in a refusal.
            bool listed = _catalog is { Schema.Shape: not RootShape.Single };
            string why = _catalog is null ? NoCatalogHint : OneRecordHint;
            bool standing = listed && _record is not null;
            string nothing = listed ? NoRecordHint : why;

            Enable(_addButton, listed, AddRecordHint, why);
            Enable(_copyButton, standing, CopyRecordHint, nothing);
            Enable(_removeButton, standing, RemoveRecordHint, nothing);
        }

        private static void Enable(Button button, bool can, string hint, string why)
        {
            button.Disabled = !can;
            button.TooltipText = can ? hint : why;
        }

        /// <summary>Writes the selected row again. The id is a field like any other and the inspector
        /// edits it, so the row naming the record has to follow — and only that row can have changed,
        /// because the inspector shows one record at a time.</summary>
        private void NameRecordRow()
        {
            if (_record is not { } record || _recordIndex == NoSelection) return;

            _recordList.SetItemText(_recordIndex, RecordRow(record));
        }

        private void MarkCatalogs()
        {
            if (_workspace is not { } workspace) return;

            for (int index = 0; index < workspace.Catalogs.Count; index++)
                _catalogList.SetItemText(index, CatalogRow(workspace.Catalogs[index]));
        }
    }
}
