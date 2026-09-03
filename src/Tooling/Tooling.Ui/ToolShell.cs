namespace Tooling.Ui
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Godot;
    using Tooling.Catalogs;
    using Tooling.Json;
    using Tooling.Localization;
    using static Tooling.Text.Format;

    /// <summary>
    /// What every authoring tool over the game's catalogs is, whatever it shows: a body of panes above
    /// a line saying what was read and what is not yet written, the keys that save and step the history,
    /// and the promise that work in hand does not leave with the window.
    /// <para>The host says what it draws (<see cref="BuildBody"/>), what it opens on
    /// (<see cref="Opened"/>), what it redraws after a change (<see cref="Redraw"/>) and which file the
    /// history keys step (<see cref="Stepped"/>). Everything else here is the same in every tool, and
    /// two readings of "is there work unsaved" would be two answers.</para>
    /// </summary>
    public abstract partial class ToolShell : Control
    {
        /// <summary>What a selection index means when there is nothing to select.</summary>
        protected const int NoSelection = -1;

        /// <summary>What a name carries while what it names is not on disk.</summary>
        protected const string DirtyMark = "*";

        private const int DialogWidth = 760;
        private const int DialogHeight = 520;
        private const int QuitDialogWidth = 480;
        private const int QuitDialogHeight = 160;

        /// <summary>How many lines of a report the dialog writes out. A run over a data root that is
        /// not there has something to say about every catalog in it, and a list that long is read as
        /// "many" and scrolled past; the count of the rest is the part still worth naming.</summary>
        private const int DialogLines = 60;

        private const Key SaveKey = Key.S;
        private const Key UndoKey = Key.Z;
        private const Key RedoKey = Key.Y;

        private const string StatusFormat = "{0}   ·   {1}   ·   {2}   ·   {3}";
        private const string TitleFormat = "{0}{1}   ·   {2}";

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

        private Label _status = null!;
        private AcceptDialog _messageDialog = null!;
        private Label _messageLines = null!;
        private ConfirmationDialog _quitDialog = null!;

        /// <summary>What the run last did, for the status line. Kept apart from what it read: the
        /// counts of the read do not change while the tool is open, and the last action does.</summary>
        protected string Action { get; set; } = ReadyText;

        /// <summary>What the run read, said once and repeated on every refresh.</summary>
        protected string Readout { get; set; } = string.Empty;

        /// <summary>The folder this run reads, which is what names the window: a title has room for
        /// where the work is and not for how much of it there turned out to be.</summary>
        protected string Root { get; set; } = string.Empty;

        /// <summary>What the run has changed and how it gets back to disk; null until the host has read
        /// anything at all.</summary>
        protected CatalogSaver? Saver { get; set; }

        /// <summary>The .po files of the run, or null while it edits none. Held by the shell for the
        /// reason the saver is: the status line, the save key and the question asked on the way out are
        /// one answer about all the work in hand, and the wording is part of it.</summary>
        protected LocalizedTexts? Texts
        {
            get => field;
            set
            {
                // The one it held stops speaking first: a set read again over a run that has already read
                // one would leave the old files refreshing a status line that no longer counts them.
                if (field is { } held) held.Changed -= Refresh;

                field = value;

                if (value is not null) value.Changed += Refresh;
            }
        }

        /// <summary>What this tool is called in the window's title.</summary>
        protected abstract string ToolName { get; }

        /// <summary>Whether anything at all is waiting to be written — a catalog file or a locale. One
        /// question with one answer: the mark on the title, the count on the status line and the dialog
        /// on the way out all read it here.</summary>
        private bool AnyDirty => Saver is { AnyDirty: true } || Texts is { IsDirty: true };

        private int DirtyCount => (Saver?.DirtyCount ?? 0) + (Texts?.DirtyCount ?? 0);

        /// <summary>A save that wrote nothing and had nothing to complain about.</summary>
        private static CatalogSaveResult Nothing => new([], []);

        /// <summary>The file the history keys step. Every file carries its own stack, so an undo takes
        /// back an edit of what is being looked at and never one made somewhere else.</summary>
        protected abstract JsonTreeDocument? Stepped { get; }

        public override void _Ready()
        {
            SetAnchorsPreset(LayoutPreset.FullRect);

            var root = new VBoxContainer();
            root.SetAnchorsPreset(LayoutPreset.FullRect);
            AddChild(root);

            root.AddChild(BuildBody());

            _status = new Label();
            root.AddChild(_status);

            BuildMessageDialog();
            BuildQuitDialog();

            // The tool answers the close request itself, or work not yet written would go with the
            // window. Everything else about quitting is left to the engine.
            GetTree().AutoAcceptQuit = false;

            Opened();
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
        /// the first pane's minimum width — the width each pane already declares, written down once.</summary>
        protected static HSplitContainer Split() => new()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };

        /// <summary>
        /// A vertically scrolling frame around content that grows. Horizontal scrolling is off on
        /// purpose: it is what fixes the width of the content, and wrapped text needs a width before it
        /// can report how tall it is.
        /// </summary>
        protected static ScrollContainer Scrolled(Control content)
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
        protected static void Show(ItemList list, int index)
        {
            list.Select(index);
            list.EnsureCurrentIsVisible();
        }

        protected static string Mark(bool dirty) => dirty ? DirtyMark : string.Empty;

        /// <summary>The panes of this tool, laid out by it. Built once, before anything is read: the
        /// shell hangs the status line under whatever comes back.</summary>
        protected abstract Control BuildBody();

        /// <summary>Reads what this run works on and shows it. Called once the body and the dialogs are
        /// up, so a report about the data has a window to be shown in.</summary>
        protected abstract void Opened();

        /// <summary>Everything the host shows that follows a change: rows renamed, marks moved, buttons
        /// enabled. Called after the status line is written, on every refresh.</summary>
        protected abstract void Redraw();

        /// <summary>What a gesture came to when the thing on screen cannot show it — a key already
        /// written, a key nobody named — on the line that says what the run last did.</summary>
        protected void Report(string what)
        {
            Action = what;
            Refresh();
        }

        /// <summary>Everything that says what the run holds now: the window's name, the status line, and
        /// whatever the host draws. Answered from the histories rather than remembered, so a step back
        /// onto the saved state clears the marks the same way a save does.</summary>
        protected void Refresh()
        {
            if (Saver is null) return;

            _status.Text = Text(StatusFormat, Readout, Action, Unsaved(), NextUndo());
            GetWindow().Title = Text(TitleFormat, Mark(AnyDirty), ToolName, Root);

            Redraw();
        }

        /// <summary>Writes back everything the run has changed — every catalog and every locale. The
        /// author edits records and their wording together and has no reason to remember which of the two
        /// a gesture landed in. A file that refused to be written is named and the rest are still on
        /// disk.</summary>
        protected CatalogSaveResult SaveEverything()
        {
            CommitTyping();

            // Each half asked for on its own: a run that has read the locales and no catalogs still has
            // work in hand, and a save that gave up on finding no saver would drop it.
            CatalogSaveResult catalogs = Saver?.SaveAll() ?? Nothing;
            CatalogSaveResult texts = Texts?.SaveAll() ?? Nothing;
            CatalogSaveResult result = new([.. catalogs.Saved, .. texts.Saved], [.. catalogs.Notes, .. texts.Notes]);

            Action = Text(WroteFormat, result.Saved.Count);
            Refresh();

            if (result.Notes.Count > 0) ShowMessage(SaveIssuesTitle, result.Notes);

            return result;
        }

        /// <summary>Says what the run could not read: the catalogs' own notes, and whatever else the host
        /// has to add — the locales it could not open. A root that is not there at all is said on its
        /// own: every catalog under it is missing for that one reason, and a list of them names the
        /// symptom instead of the cause.</summary>
        protected void ReportIssues(CatalogWorkspace workspace, IReadOnlyList<string>? also = null)
        {
            if (!Directory.Exists(workspace.Root))
            {
                ShowMessage(NoRootTitle, [workspace.Root, NoRootHint]);
                return;
            }

            List<string> notes = [.. workspace.Report, .. also ?? []];

            if (notes.Count > 0) ShowMessage(IssuesTitle, notes);
        }

        protected void ShowMessage(string title, IReadOnlyList<string> lines)
        {
            _messageDialog.Title = title;
            _messageLines.Text = string.Join(LineBreak, Shown(lines));
            _messageDialog.PopupCentered(new Vector2I(DialogWidth, DialogHeight));
        }

        /// <summary>
        /// Ends whatever is being typed, by taking the focus off it. A number keeps what the author has
        /// typed inside its own box until the box is left or Enter is pressed — so a save asked for
        /// straight after typing would write the file without the number on screen and then report
        /// nothing to save, and a close would find nothing unsaved and never ask.
        /// <para>Asked of the focus rather than of the panel: the tool has no list of the boxes it built,
        /// and there is only ever one of them the author is inside.</para>
        /// </summary>
        protected void CommitTyping() => GetViewport().GuiGetFocusOwner()?.ReleaseFocus();

        /// <summary>The lines the dialog writes, and, when there are more than it writes, a last line
        /// counting the ones it does not.</summary>
        private static IEnumerable<string> Shown(IReadOnlyList<string> lines) =>
            lines.Count <= DialogLines
                ? lines
                : [.. lines.Take(DialogLines), Text(MoreLinesFormat, lines.Count - DialogLines)];

        private string Unsaved() => AnyDirty ? Text(UnsavedFormat, DirtyCount) : SavedText;

        private void BuildMessageDialog()
        {
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

        /// <summary>What one press of undo would take back, so the author reads what the key means
        /// before pressing it.</summary>
        private string NextUndo() =>
            Stepped?.History.NextUndo is { } step ? Text(UndoFormat, step) : NoUndoText;

        private void StepBack()
        {
            if (Stepped?.History is not { } history) return;

            Action = history.Undo() is { } step ? Text(UndoFormat, step.Label) : NothingToUndoText;
            Refresh();
        }

        private void StepForward()
        {
            if (Stepped?.History is not { } history) return;

            Action = history.Redo() is { } step ? Text(RedoneFormat, step.Label) : NothingToRedoText;
            Refresh();
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

            if (!AnyDirty)
            {
                Leave();
                return;
            }

            _quitDialog.PopupCentered(new Vector2I(QuitDialogWidth, QuitDialogHeight));
        }

        private void Leave() => GetTree().Quit();
    }
}
