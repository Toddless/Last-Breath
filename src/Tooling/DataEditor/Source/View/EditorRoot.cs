namespace DataEditor.Source.View
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using App;
    using Godot;
    using Io;
    using Tooling.Catalogs;
    using static Tooling.Text.Format;

    /// <summary>
    /// Composition root of the data editor: three panes built in code — the catalogs the game
    /// describes, the records of the chosen one, and the record itself — over a status line naming
    /// what was read and what is not yet written. The scene file holds nothing but this node; there is
    /// no designer to hand a .tscn to, so the layout lives where it can be reviewed as code.
    /// <para>Every edit goes through the history of the document it belongs to, so undo is per file:
    /// the file of the record on screen is the one the keys move. Saving is not — Ctrl+S writes every
    /// changed file of the run, because the author edits records and has no reason to remember which
    /// catalog each of them came from.</para>
    /// </summary>
    public partial class EditorRoot : Control
    {
        private const int CatalogPaneWidth = 220;
        private const int RecordPaneWidth = 260;
        private const int DialogWidth = 760;
        private const int DialogHeight = 520;
        private const int QuitDialogWidth = 480;
        private const int QuitDialogHeight = 160;

        /// <summary>How many lines of a report the dialog writes out. A run over a data root that is
        /// not there has something to say about every catalog in it, and a list that long is read as
        /// "many" and scrolled past; the count of the rest is the part still worth naming.</summary>
        private const int DialogLines = 60;

        /// <summary>What a selection index means when there is nothing to select.</summary>
        private const int NoSelection = -1;

        private const Key SaveKey = Key.S;
        private const Key UndoKey = Key.Z;
        private const Key RedoKey = Key.Y;

        private const string CatalogRowFormat = "{0}{1}   ({2})";
        private const string RecordRowFormat = "{0}   ·  {1}";
        private const string ReadoutFormat =
            "{0}   —   {1} catalog(s) of {2} described, {3} file(s), {4} record(s), {5} note(s)";

        private const string StatusFormat = "{0}   ·   {1}   ·   {2}   ·   {3}";
        private const string TitleFormat = "{0}data editor   ·   {1}";

        /// <summary>What a name carries while what it names is not on disk.</summary>
        private const string DirtyMark = "*";

        private const string UnsavedFormat = "{0} unsaved file(s)";
        private const string SavedText = "all saved";
        private const string UndoFormat = "undo: {0}";
        private const string NoUndoText = "nothing to undo";
        private const string RedoneFormat = "redo: {0}";
        private const string NothingToUndoText = "nothing to take back";
        private const string NothingToRedoText = "nothing to put back";
        private const string WroteFormat = "wrote {0} file(s)";
        private const string ReadyText = "ready";

        private const string MoreLinesFormat = "…and {0} more";

        /// <summary>What one line of the dialog ends with.</summary>
        private const string LineBreak = "\n";

        private const string NoRootTitle = "Data root not found";
        private const string NoRootHint = "Data/Shared is a symlink to src/SharedData; restore-links.ps1 puts it back.";
        private const string IssuesTitle = "Data issues";
        private const string SaveIssuesTitle = "Not everything could be written";

        private const string QuitTitle = "Unsaved changes";
        private const string QuitQuestion = "Some files have not been written. Save them before leaving?";
        private const string QuitSaveText = "Save and leave";
        private const string QuitLeaveText = "Leave without saving";
        private const string QuitLeaveAction = "leave";

        private ItemList _catalogList = null!;
        private ItemList _recordList = null!;
        private InspectorPanel _inspector = null!;
        private Label _status = null!;
        private AcceptDialog _messageDialog = null!;
        private Label _messageLines = null!;
        private ConfirmationDialog _quitDialog = null!;

        private CatalogWorkspace? _workspace;
        private CatalogSaver? _saver;
        private CatalogView? _catalog;
        private CatalogRecord? _record;

        /// <summary>Where the record on screen sits in the list, so its row can be written again when the
        /// id it is named by is edited.</summary>
        private int _recordIndex = NoSelection;

        /// <summary>What the run last did, for the status line. Kept apart from what it read: the
        /// counts of the read do not change while the tool is open, and the last action does.</summary>
        private string _readout = string.Empty;
        private string _action = ReadyText;

        /// <summary>The folder this run reads, which is what names the window: a title has room for
        /// where the work is and not for how much of it there turned out to be.</summary>
        private string _root = string.Empty;

        public override void _Ready()
        {
            BuildUi();

            // The tool answers the close request itself, or work not yet written would go with the
            // window. Everything else about quitting is left to the engine.
            GetTree().AutoAcceptQuit = false;

            LoadCatalogs();
        }

        public override void _Notification(int what)
        {
            if (what == NotificationWMCloseRequest) Leaving();
        }

        /// <summary>
        /// The tool-wide shortcuts, answered ahead of every panel and every text field.
        /// <para>Plain input rather than unhandled input: a focused text box swallows Ctrl+Z for its own
        /// character-level undo long before an unhandled key is reached, and a tool whose undo means
        /// "one letter" while the caret is in a field and "one edit" everywhere else is a tool whose
        /// undo nobody can predict. The tool has one key for it, and a name typed into a record is a
        /// step in the file's history like any other.</para>
        /// <para>Ctrl+Y and Ctrl+Shift+Z both step forward — both are muscle memory and neither has
        /// anything else to mean here. Held keys repeat and history steps deliberately do not follow
        /// the repeat: one press is one step, so leaning on the key cannot unwind a session faster than
        /// it can be read.</para>
        /// </summary>
        public override void _Input(InputEvent @event)
        {
            if (@event is not InputEventKey { Pressed: true, Echo: false, CtrlPressed: true } key) return;

            switch (key.Keycode)
            {
                case SaveKey:
                    SaveEverything();
                    break;
                case UndoKey when key.ShiftPressed:
                case RedoKey:
                    StepForward();
                    break;
                case UndoKey:
                    StepBack();
                    break;
                default:
                    return;
            }

            GetViewport().SetInputAsHandled();
        }

        /// <summary>A resizable divide. The offset is left at zero, which puts the divider at the end of
        /// the first pane's minimum width — the width each list already declares, written down once.</summary>
        private static HSplitContainer Split() => new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };

        /// <summary>
        /// A vertically scrolling frame around content that grows. Horizontal scrolling is off on
        /// purpose: it is what fixes the width of the content, and wrapped text needs a width before it
        /// can report how tall it is.
        /// </summary>
        private static ScrollContainer Scrolled(Control content)
        {
            var scroll = new ScrollContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
            };

            scroll.AddChild(content);

            return scroll;
        }

        /// <summary>Selects a row and scrolls it into view. A row chosen without a click — the one the
        /// tool opens on, or the first record of a catalog just picked — is otherwise selected where
        /// the list is not looking.</summary>
        private static void Show(ItemList list, int index)
        {
            list.Select(index);
            list.EnsureCurrentIsVisible();
        }

        /// <summary>The lines the dialog writes, and, when there are more than it writes, a last line
        /// counting the ones it does not.</summary>
        private static IEnumerable<string> Shown(IReadOnlyList<string> lines) =>
            lines.Count <= DialogLines
                ? lines
                : [.. lines.Take(DialogLines), Text(MoreLinesFormat, lines.Count - DialogLines)];

        private static string Mark(bool dirty) => dirty ? DirtyMark : string.Empty;

        private void BuildUi()
        {
            SetAnchorsPreset(LayoutPreset.FullRect);

            var root = new VBoxContainer();
            root.SetAnchorsPreset(LayoutPreset.FullRect);
            AddChild(root);

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
            right.AddChild(_recordList);
            right.AddChild(Scrolled(_inspector));
            root.AddChild(body);

            _status = new Label();
            root.AddChild(_status);

            // The dialog's own text is left empty and the lines are written to a label of ours instead:
            // a report is as long as the data is wrong, and only a frame that scrolls can hold one
            // without the dialog growing past the screen. The dialog lays every control child over the
            // same rect, so the label it carries itself would sit under this one.
            _messageLines = new Label
            {
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };

            _messageDialog = new AcceptDialog();
            _messageDialog.AddChild(Scrolled(_messageLines));
            AddChild(_messageDialog);

            BuildQuitDialog();

            _catalogList.ItemSelected += index => ShowCatalog((int)index);
            _recordList.ItemSelected += index => ShowRecord((int)index);
        }

        /// <summary>
        /// The three answers to being closed with work in hand. Leaving without saving is a button of
        /// its own rather than the cancel: cancel is what a stray Escape presses, and the one outcome
        /// that cannot be taken back must not be the one a stray key lands on.
        /// </summary>
        private void BuildQuitDialog()
        {
            _quitDialog = new ConfirmationDialog
            {
                Title = QuitTitle,
                DialogText = QuitQuestion,
                OkButtonText = QuitSaveText
            };

            _quitDialog.AddButton(QuitLeaveText, right: true, QuitLeaveAction);
            _quitDialog.Confirmed += SaveAndLeave;
            _quitDialog.CustomAction += action =>
            {
                if (action.ToString() == QuitLeaveAction) Leave();
            };

            AddChild(_quitDialog);
        }

        /// <summary>
        /// Reads every described catalog from the data root of this run and shows the first of them.
        /// A root that is not there at all is said on its own: every catalog under it is missing for
        /// that one reason, and a list of them names the symptom instead of the cause.
        /// </summary>
        private void LoadCatalogs()
        {
            string root = ToolPaths.SharedDataRoot;

            _root = root;
            _workspace = GameCatalogs.Load(root);
            _saver = new CatalogSaver(_workspace);
            _saver.Changed += Refresh;

            _catalogList.Clear();

            foreach (CatalogView view in _workspace.Catalogs) _catalogList.AddItem(CatalogRow(view));

            _readout = Text(
                ReadoutFormat,
                root,
                _workspace.Catalogs.Count,
                GameCatalogs.TotalCount,
                _workspace.FileCount,
                _workspace.RecordCount,
                _workspace.Report.Count);

            ShowCatalog(_workspace.Catalogs.Count > 0 ? 0 : NoSelection);

            if (!Directory.Exists(root)) ShowMessage(NoRootTitle, [root, NoRootHint]);
            else if (_workspace.Report.Count > 0) ShowMessage(IssuesTitle, _workspace.Report);
        }

        /// <summary>Shows the records of one catalog, and the first of them. Selecting the row in the
        /// list as well, because the catalog is also chosen without a click — the tool opens on one.</summary>
        private void ShowCatalog(int index)
        {
            _catalog = _workspace is not null && index >= 0 && index < _workspace.Catalogs.Count
                ? _workspace.Catalogs[index]
                : null;

            if (_catalog is not null) Show(_catalogList, index);

            _recordList.Clear();

            if (_catalog is not null)
                foreach (CatalogRecord record in _catalog.Records)
                    _recordList.AddItem(RecordRow(record));

            ShowRecord(_catalog is { Records.Count: > 0 } ? 0 : NoSelection);
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

            _recordIndex = _record is null ? NoSelection : index;

            if (_record is not null) Show(_recordList, index);

            _inspector.Rebuild(_record);
            Refresh();
        }

        /// <summary>Writes back everything the run has changed, in every catalog. A file that refused to
        /// be written is named and the rest are still on disk.</summary>
        private CatalogSaveResult SaveEverything()
        {
            CommitTyping();

            if (_saver is not { } saver) return new CatalogSaveResult([], []);

            CatalogSaveResult result = saver.SaveAll();

            _action = Text(WroteFormat, result.Saved.Count);
            Refresh();

            if (result.Notes.Count > 0) ShowMessage(SaveIssuesTitle, result.Notes);

            return result;
        }

        /// <summary>Leaves only once the work is on disk. A save that could not write everything keeps
        /// the tool open with the reason on screen: the point of asking was not to lose it.</summary>
        private void SaveAndLeave()
        {
            if (SaveEverything().Notes.Count == 0) Leave();
        }

        private void Leaving()
        {
            CommitTyping();

            if (_saver is not { AnyDirty: true })
            {
                Leave();
                return;
            }

            _quitDialog.PopupCentered(new Vector2I(QuitDialogWidth, QuitDialogHeight));
        }

        private void Leave() => GetTree().Quit();

        /// <summary>
        /// Ends whatever is being typed, by taking the focus off it. A number keeps what the author has
        /// typed inside its own box until the box is left or Enter is pressed — so a save asked for
        /// straight after typing would write the file without the number on screen and then report
        /// nothing to save, and a close would find nothing unsaved and never ask.
        /// <para>Asked of the focus rather than of the panel: the tool has no list of the boxes it built,
        /// and there is only ever one of them the author is inside.</para>
        /// </summary>
        private void CommitTyping() => GetViewport().GuiGetFocusOwner()?.ReleaseFocus();

        /// <summary>Steps the history of the file the record on screen lives in. Every file carries its
        /// own stack, so an undo takes back an edit of the record being looked at and never one made in
        /// a catalog opened an hour ago.</summary>
        private void StepBack()
        {
            if (_record?.File.Document.History is not { } history) return;

            _action = history.Undo() is { } step ? Text(UndoFormat, step.Label) : NothingToUndoText;
            Refresh();
        }

        private void StepForward()
        {
            if (_record?.File.Document.History is not { } history) return;

            _action = history.Redo() is { } step ? Text(RedoneFormat, step.Label) : NothingToRedoText;
            Refresh();
        }

        /// <summary>Everything that says what the run holds now: the window's name, the marks on the
        /// catalogs, and the status line. Answered from the histories rather than remembered, so a step
        /// back onto the saved state clears the marks the same way a save does.</summary>
        private void Refresh()
        {
            if (_saver is not { } saver) return;

            _status.Text = Text(StatusFormat, _readout, _action, Unsaved(saver), NextUndo());
            GetWindow().Title = Text(TitleFormat, Mark(saver.AnyDirty), _root);

            MarkCatalogs();
            NameRecordRow();
        }

        /// <summary>Writes the selected row again. The id is a field like any other and the inspector
        /// edits it, so the row naming the record has to follow — and only that row can have changed,
        /// because the inspector shows one record at a time.</summary>
        private void NameRecordRow()
        {
            if (_record is not { } record || _recordIndex == NoSelection) return;

            _recordList.SetItemText(_recordIndex, RecordRow(record));
        }

        private static string Unsaved(CatalogSaver saver) =>
            saver.AnyDirty ? Text(UnsavedFormat, saver.DirtyCount) : SavedText;

        /// <summary>What one press of undo would take back, so the author reads what the key means
        /// before pressing it.</summary>
        private string NextUndo() =>
            _record?.File.Document.History.NextUndo is { } step ? Text(UndoFormat, step) : NoUndoText;

        private void MarkCatalogs()
        {
            if (_workspace is not { } workspace) return;

            for (int index = 0; index < workspace.Catalogs.Count; index++)
                _catalogList.SetItemText(index, CatalogRow(workspace.Catalogs[index]));
        }

        private void ShowMessage(string title, IReadOnlyList<string> lines)
        {
            _messageDialog.Title = title;
            _messageLines.Text = string.Join(LineBreak, Shown(lines));
            _messageDialog.PopupCentered(new Vector2I(DialogWidth, DialogHeight));
        }
    }
}
