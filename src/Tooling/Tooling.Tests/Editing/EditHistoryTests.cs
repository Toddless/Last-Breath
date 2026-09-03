namespace Tooling.Tests.Editing
{
    using Tooling.Editing.History;

    /// <summary>
    /// The undo stack an authoring tool stands on. Everything here is about the stack's own rules rather
    /// than about any one editor: what a step back restores, where the saved state sits, when a run of
    /// keystrokes is one step and when it is two, and what falls off a bounded stack.
    /// <para>These are the failures a tool cannot show on screen. An undo that lands on state the author
    /// never had, a title bar reporting a changed file as saved, a run that files one step per letter and
    /// pushes the session's real work off the far end — each of them is discovered only after the file is
    /// already written, so each of them is pinned here instead.</para>
    /// </summary>
    [TestClass]
    public class EditHistoryTests
    {
        private const string TitleField = "Title";
        private const string BudgetField = "Budget";

        private const string Initial = "initial";
        private const string First = "first";
        private const string Second = "second";
        private const string Third = "third";

        private const string LabelFirst = "set first";
        private const string LabelSecond = "set second";
        private const string LabelThird = "set third";
        private const string LabelRename = "rename";
        private const string LabelStep = "step ";

        private const string OldId = "Node_Old";
        private const string NewId = "Node_New";

        private const string UndoMark = "undo:";
        private const string RedoMark = "redo:";

        [TestMethod]
        public void Undo_PutsBackTheValueTheEditReplaced()
        {
            EditHistory history = new();
            Cell cell = new(Initial);

            history.Record(Write(cell, TitleField, First, LabelFirst));

            Assert.AreEqual(First, cell.Value);
            Assert.IsNotNull(history.Undo());
            Assert.AreEqual(Initial, cell.Value);
            Assert.IsFalse(history.CanUndo);
            Assert.IsTrue(history.CanRedo);
        }

        [TestMethod]
        public void Redo_PutsBackTheValueTheEditWrote()
        {
            EditHistory history = new();
            Cell cell = new(Initial);

            history.Record(Write(cell, TitleField, First, LabelFirst));
            history.Undo();

            Assert.IsNotNull(history.Redo());
            Assert.AreEqual(First, cell.Value);
            Assert.IsTrue(history.CanUndo);
            Assert.IsFalse(history.CanRedo);
        }

        /// <summary>An empty stack is asked to step back exactly as often as a full one — by a toolbar
        /// button that is always there. It answers with nothing and tells nobody anything happened.</summary>
        [TestMethod]
        public void Step_OnEmptyHistory_ReturnsNothingAndDoesNotNotify()
        {
            EditHistory history = new();
            int notified = 0;
            history.Changed += () => notified++;

            Assert.IsNull(history.Undo());
            Assert.IsNull(history.Redo());
            Assert.AreEqual(0, notified);
            Assert.IsNull(history.NextUndo);
            Assert.IsNull(history.NextRedo);
        }

        [TestMethod]
        public void IsClean_OnFreshHistory_IsTrue()
        {
            EditHistory history = new();
            Cell cell = new(Initial);

            Assert.IsTrue(history.IsCleanFor(cell));
        }

        /// <summary>One stack carries the whole tool, and a save writes one file at a time: a step taken in
        /// one document leaves the others exactly as they were written, and a save of one of them says
        /// nothing about the work waiting in the next.</summary>
        [TestMethod]
        public void IsClean_IsAnsweredPerDocument()
        {
            EditHistory history = new();
            Cell one = new(Initial);
            Cell another = new(Initial);

            history.Record(Write(one, TitleField, First, LabelFirst));

            Assert.IsFalse(history.IsCleanFor(one));
            Assert.IsTrue(history.IsCleanFor(another), "nothing was written into the other document");

            history.Record(Write(another, TitleField, Second, LabelSecond));
            history.MarkSaved(another);

            Assert.IsTrue(history.IsCleanFor(another));
            Assert.IsFalse(history.IsCleanFor(one), "a save of one document does not write the other");
        }

        /// <summary>Clean is a position in the stack, not a flag somebody sets. Walking away from the saved
        /// step dirties the document and walking back to it makes it clean again — a flag could only ever
        /// be turned on, and the author would be asked to save a file identical to the one on disk.</summary>
        [TestMethod]
        public void IsClean_FollowsThePositionInTheStack()
        {
            EditHistory history = new();
            Cell cell = new(Initial);

            history.Record(Write(cell, TitleField, First, LabelFirst));
            history.MarkSaved(cell);
            Assert.IsTrue(history.IsCleanFor(cell));

            history.Record(Write(cell, BudgetField, Second, LabelSecond));
            Assert.IsFalse(history.IsCleanFor(cell));

            history.Undo();
            Assert.IsTrue(history.IsCleanFor(cell));

            history.Redo();
            Assert.IsFalse(history.IsCleanFor(cell));
        }

        [TestMethod]
        public void MarkSaved_MovesTheSavePoint()
        {
            EditHistory history = new();
            Cell cell = new(Initial);

            history.Record(Write(cell, TitleField, First, LabelFirst));
            history.MarkSaved(cell);
            history.Record(Write(cell, BudgetField, Second, LabelSecond));
            history.MarkSaved(cell);

            Assert.IsTrue(history.IsCleanFor(cell));

            history.Undo();
            Assert.IsFalse(history.IsCleanFor(cell));

            history.Redo();
            Assert.IsTrue(history.IsCleanFor(cell));
        }

        /// <summary>Saving closes the run it happened in the middle of. A merged command keeps its own
        /// identity while it absorbs its neighbours, so a run that survived the write would go on growing
        /// inside the very command marked as saved — and the document would report itself clean while the
        /// author is still typing.</summary>
        [TestMethod]
        public void MarkSaved_ClosesTheOpenRun()
        {
            EditHistory history = new();
            Cell cell = new(Initial);
            int notified = 0;

            history.Record(Write(cell, TitleField, First, LabelFirst));
            history.Changed += () => notified++;
            history.MarkSaved(cell);

            Assert.AreEqual(1, notified);

            history.Record(Write(cell, TitleField, Second, LabelSecond));

            Assert.AreEqual(2, history.Depth);
            Assert.IsFalse(history.IsCleanFor(cell));

            history.Undo();

            Assert.AreEqual(First, cell.Value);
            Assert.IsTrue(history.IsCleanFor(cell));
        }

        /// <summary>A saved step pushed off the far end of a full stack can never be on top again, so the
        /// document stays reported as changed however far back the author walks. The answer is wrong in the
        /// only direction that costs nothing: an extra save.</summary>
        [TestMethod]
        public void IsClean_AfterTheSavedStepFallsOffTheStack_StaysFalse()
        {
            EditHistory history = new();
            Cell cell = new(Initial);
            List<string> trace = [];

            history.Record(new MarkerEdit(cell, LabelFirst, trace));
            history.MarkSaved(cell);

            for (int step = 0; step < EditHistory.MaxDepth; step++) history.Record(new MarkerEdit(cell, LabelSecond, trace));

            Assert.AreEqual(EditHistory.MaxDepth, history.Depth);
            Assert.IsFalse(history.IsCleanFor(cell));

            while (history.CanUndo) history.Undo();

            Assert.IsFalse(history.IsCleanFor(cell));
        }

        /// <summary>Typing into one field is one thing the author did, so it is one thing he can take back —
        /// and the step back lands on the value the field held before the first keystroke, not on the
        /// letter before last.</summary>
        [TestMethod]
        public void Record_MergesARunOnOneTarget_IntoOneStep()
        {
            EditHistory history = new();
            Cell cell = new(Initial);

            history.Record(Write(cell, TitleField, First, LabelFirst));
            history.Record(Write(cell, TitleField, Second, LabelSecond));
            history.Record(Write(cell, TitleField, Third, LabelThird));

            Assert.AreEqual(1, history.Depth);
            Assert.AreEqual(LabelThird, history.NextUndo);

            history.Undo();

            Assert.AreEqual(Initial, cell.Value);
            Assert.IsFalse(history.CanUndo);
        }

        /// <summary>Leaving the field ends the run: the next keystroke starts a step of its own even though
        /// it writes the same field of the same object.</summary>
        [TestMethod]
        public void Seal_BreaksTheRun()
        {
            EditHistory history = new();
            Cell cell = new(Initial);

            history.Record(Write(cell, TitleField, First, LabelFirst));
            history.Seal();
            history.Record(Write(cell, TitleField, Second, LabelSecond));

            Assert.AreEqual(2, history.Depth);

            history.Undo();
            Assert.AreEqual(First, cell.Value);

            history.Undo();
            Assert.AreEqual(Initial, cell.Value);
        }

        /// <summary>A run merges on the field, so moving to the next field of the same object starts a step
        /// of its own — the author filled in two things and can take back two things.</summary>
        [TestMethod]
        public void Record_DoesNotMerge_AnotherFieldOfTheSameOwner()
        {
            EditHistory history = new();
            Cell cell = new(Initial);

            history.Record(Write(cell, TitleField, First, LabelFirst));
            history.Record(Write(cell, BudgetField, Second, LabelSecond));

            Assert.AreEqual(2, history.Depth);
        }

        /// <summary>And it merges on the object, compared by reference: two cells given the same field the
        /// same value are still two edits.</summary>
        [TestMethod]
        public void Record_DoesNotMerge_TheSameFieldOfAnotherOwner()
        {
            EditHistory history = new();
            Cell one = new(Initial);
            Cell another = new(Initial);

            history.Record(Write(one, TitleField, First, LabelFirst));
            history.Record(Write(another, TitleField, First, LabelFirst));

            Assert.AreEqual(2, history.Depth);
        }

        /// <summary>A step through the history ends the run as surely as leaving the field does. Without
        /// that, an edit made after an undo would be swallowed by the command the author just stepped
        /// past, and one press of undo would take back two separate things.</summary>
        [TestMethod]
        public void Stepping_BreaksTheRun()
        {
            EditHistory history = new();
            Cell cell = new(Initial);

            history.Record(Write(cell, TitleField, First, LabelFirst));
            history.Undo();
            history.Redo();
            history.Record(Write(cell, TitleField, Second, LabelSecond));

            Assert.AreEqual(2, history.Depth);

            history.Undo();
            Assert.AreEqual(First, cell.Value);
        }

        /// <summary>The stack is bounded because every step keeps alive what it took out of the document.
        /// The step that goes is the oldest — the far end is the part the author has stopped thinking
        /// about, while the newest is the one he is about to press undo on.</summary>
        [TestMethod]
        public void Record_DropsTheOldestStep_BeyondMaxDepth()
        {
            EditHistory history = new();
            Cell cell = new(Initial);
            List<string> trace = [];

            for (int step = 0; step <= EditHistory.MaxDepth; step++)
                history.Record(new MarkerEdit(cell, Numbered(step), trace));

            Assert.AreEqual(EditHistory.MaxDepth, history.Depth);
            Assert.AreEqual(Numbered(EditHistory.MaxDepth), history.NextUndo);

            while (history.CanUndo) history.Undo();

            CollectionAssert.DoesNotContain(trace, UndoMark + Numbered(0));
            CollectionAssert.Contains(trace, UndoMark + Numbered(1));
        }

        /// <summary>A new edit is a new branch: what was undone is no longer ahead of the author, and
        /// offering to redo it would replay a change onto a document that has moved on.</summary>
        [TestMethod]
        public void Record_ClearsTheRedoBranch()
        {
            EditHistory history = new();
            Cell cell = new(Initial);

            history.Record(Write(cell, TitleField, First, LabelFirst));
            history.Undo();

            Assert.IsTrue(history.CanRedo);

            history.Record(Write(cell, BudgetField, Second, LabelSecond));

            Assert.IsFalse(history.CanRedo);
            Assert.IsNull(history.NextRedo);
        }

        /// <summary>Everything outside the stack — the toolbar, the title bar, the status line — learns
        /// about the stack only from this event, including when a keystroke was swallowed by the run it
        /// belongs to and the depth did not move.</summary>
        [TestMethod]
        public void Changed_FiresOnRecordUndoAndRedo()
        {
            EditHistory history = new();
            Cell cell = new(Initial);
            int notified = 0;
            history.Changed += () => notified++;

            history.Record(Write(cell, TitleField, First, LabelFirst));
            Assert.AreEqual(1, notified);

            history.Record(Write(cell, TitleField, Second, LabelSecond));
            Assert.AreEqual(2, notified);
            Assert.AreEqual(1, history.Depth);

            history.Undo();
            Assert.AreEqual(3, notified);

            history.Redo();
            Assert.AreEqual(4, notified);
        }

        /// <summary>An undo the author cannot name is an undo he has to verify by eye. The labels name the
        /// step each direction would take, and say nothing when there is no step to take.</summary>
        [TestMethod]
        public void NextUndoAndNextRedo_NameTheSteps()
        {
            EditHistory history = new();
            Cell cell = new(Initial);

            history.Record(Write(cell, TitleField, First, LabelFirst));
            history.Seal();
            history.Record(Write(cell, TitleField, Second, LabelSecond));

            Assert.AreEqual(LabelSecond, history.NextUndo);
            Assert.IsNull(history.NextRedo);

            history.Undo();

            Assert.AreEqual(LabelFirst, history.NextUndo);
            Assert.AreEqual(LabelSecond, history.NextRedo);
        }

        /// <summary>An id is how the rest of a tool holds a record: whatever pointed at the old one points
        /// at nothing the moment a rename is stepped through. The command says which id became which, in
        /// the direction the step was taken, and the step handed outside carries it.</summary>
        [TestMethod]
        public void RenameSwap_ReachesTheHistoryStep()
        {
            EditHistory history = new();
            history.Record(new RenamingEdit(LabelRename, OldId, NewId));

            HistoryStep? undone = Taken(history.Undo(), undoing: true);

            Assert.IsNotNull(undone);
            Assert.AreEqual(LabelRename, undone.Value.Label);
            Assert.AreEqual(new IdSwap(NewId, OldId), undone.Value.Rename);

            HistoryStep? redone = Taken(history.Redo(), undoing: false);

            Assert.IsNotNull(redone);
            Assert.AreEqual(new IdSwap(OldId, NewId), redone.Value.Rename);
        }

        /// <summary>Every other edit leaves the id alone, and a step that reported a rename it did not make
        /// would move a selection the author never touched.</summary>
        [TestMethod]
        public void OrdinaryStep_ReportsNoRename()
        {
            EditHistory history = new();
            Cell cell = new(Initial);
            history.Record(Write(cell, TitleField, First, LabelFirst));

            HistoryStep? undone = Taken(history.Undo(), undoing: true);

            Assert.IsNotNull(undone);
            Assert.IsNull(undone.Value.Rename);
        }

        /// <summary>A document closed and another opened shares nothing with what came before: no steps in
        /// either direction, and a fresh document is by definition the one on disk.</summary>
        [TestMethod]
        public void Clear_EmptiesBothSidesAndForgetsTheSavePoint()
        {
            EditHistory history = new();
            Cell cell = new(Initial);

            history.Record(Write(cell, TitleField, First, LabelFirst));
            history.MarkSaved(cell);
            history.Record(Write(cell, BudgetField, Second, LabelSecond));
            history.Undo();

            int notified = 0;
            history.Changed += () => notified++;

            history.Clear();

            Assert.AreEqual(1, notified);
            Assert.AreEqual(0, history.Depth);
            Assert.IsFalse(history.CanUndo);
            Assert.IsFalse(history.CanRedo);
            Assert.IsTrue(history.IsCleanFor(cell));
        }

        /// <summary>A gesture that changed two documents is one thing the author did. Undone, it is undone
        /// whole and backwards — a rename left half taken back is a record read under one word in its own
        /// file and under another in the locales, which is the state nothing on screen can show.</summary>
        [TestMethod]
        public void Group_FilesEverythingRecordedInIt_AsOneStep()
        {
            EditHistory history = new();
            Cell one = new(Initial);
            Cell another = new(Initial);

            using (history.Group(LabelRename))
            {
                history.Record(Write(one, TitleField, First, LabelFirst));
                history.Record(Write(another, TitleField, Second, LabelSecond));
            }

            Assert.AreEqual(1, history.Depth);
            Assert.AreEqual(LabelRename, history.NextUndo, "the step is named by the gesture, not by its last command");

            history.Undo();

            Assert.AreEqual(Initial, one.Value);
            Assert.AreEqual(Initial, another.Value);
            Assert.IsFalse(history.CanUndo, "half of the gesture is still there to take back");

            history.Redo();

            Assert.AreEqual(First, one.Value);
            Assert.AreEqual(Second, another.Value);
        }

        /// <summary>What a change means elsewhere is known only once it has been made: the id is written
        /// into the record first and the keys are named again after it, and the two are one gesture.</summary>
        [TestMethod]
        public void GroupWithNewest_TakesTheStepAlreadyFiled_IntoTheSameStep()
        {
            EditHistory history = new();
            Cell record = new(Initial);
            Cell locale = new(Initial);

            history.Record(Write(record, TitleField, First, LabelFirst));

            using (history.GroupWithNewest(LabelRename))
            {
                history.Record(Write(locale, TitleField, Second, LabelSecond));
            }

            Assert.AreEqual(1, history.Depth, "the id and the keys named from it are one step");
            Assert.AreEqual(LabelRename, history.NextUndo);

            history.Undo();

            Assert.AreEqual(Initial, record.Value);
            Assert.AreEqual(Initial, locale.Value, "the locale was stepped back with the id");
        }

        /// <summary>A step made of several changes is dirty in every document any of them touched, or a
        /// save would write the record's file and leave the locales it renamed keys in behind.</summary>
        [TestMethod]
        public void Group_IsUnsavedInEveryDocumentItTouched()
        {
            EditHistory history = new();
            Cell one = new(Initial);
            Cell another = new(Initial);

            using (history.Group(LabelRename))
            {
                history.Record(Write(one, TitleField, First, LabelFirst));
                history.Record(Write(another, TitleField, Second, LabelSecond));
            }

            Assert.IsFalse(history.IsCleanFor(one));
            Assert.IsFalse(history.IsCleanFor(another));
        }

        /// <summary>A gesture that recorded nothing is not a step: the author pressed something that changed
        /// nothing, and an undo taking back nothing reads as the key having been missed.</summary>
        [TestMethod]
        public void Group_WithNothingRecordedInIt_FilesNoStep()
        {
            EditHistory history = new();

            using (history.Group(LabelRename))
            {
            }

            Assert.AreEqual(0, history.Depth);
            Assert.IsFalse(history.CanUndo);
        }

        /// <summary>A gesture made of gestures is still one thing the author did, and the outermost of them
        /// is the one that names it: the inner one is flattened into the step already open.</summary>
        [TestMethod]
        public void Group_OpenedInsideAnother_IsTheSameStep()
        {
            EditHistory history = new();
            Cell cell = new(Initial);

            using (history.Group(LabelRename))
            {
                history.Record(Write(cell, TitleField, First, LabelFirst));

                using (history.Group(LabelSecond))
                {
                    history.Record(Write(cell, BudgetField, Second, LabelSecond));
                }

                Assert.AreEqual(0, history.Depth, "the inner gesture filed a step of its own");
            }

            Assert.AreEqual(1, history.Depth);
            Assert.AreEqual(LabelRename, history.NextUndo);
        }

        /// <summary>Nothing is stepped while a gesture is still being made: half of it is a state the author
        /// never had, and the rest of the gesture would go on writing onto what the step left behind.</summary>
        [TestMethod]
        public void Step_WhileAGestureIsOpen_IsRefused()
        {
            EditHistory history = new();
            Cell cell = new(Initial);

            history.Record(Write(cell, TitleField, First, LabelFirst));

            using (history.Group(LabelRename))
            {
                history.Record(Write(cell, BudgetField, Second, LabelSecond));

                Assert.IsNull(history.Undo());
                Assert.IsNull(history.Redo());
                Assert.AreEqual(Second, cell.Value);
            }

            Assert.AreEqual(2, history.Depth);
        }

        /// <summary>Applies the change and hands back the command that files it: the stack records what has
        /// already happened, so a test that recorded without writing would be pinning a stack nobody uses.
        /// </summary>
        private static ValueEdit<string> Write(Cell cell, string field, string value, string label)
        {
            string before = cell.Value;
            cell.Value = value;

            return new ValueEdit<string>(new EditTarget(cell, field), before, value, written => cell.Value = written, label);
        }

        /// <summary>The step as the rest of a tool hears about it — the one line of composition every host
        /// writes, and the reason a command has to declare that it moved an id.</summary>
        private static HistoryStep? Taken(IEditCommand? command, bool undoing) =>
            command is null ? null : new HistoryStep(command.Label, (command as IIdChangingEdit)?.Swap(undoing));

        private static string Numbered(int step) => LabelStep + step;

        /// <summary>A field an edit writes to. Compared by reference, exactly as the stack compares it: two
        /// cells holding the same text are still two cells.</summary>
        private sealed class Cell(string value)
        {
            public string Value { get; set; } = value;
        }

        /// <summary>A step that is not part of any run and writes down that it was taken: two of them in a
        /// row are always two steps, and the trace says which ones survived a bounded stack.</summary>
        private sealed class MarkerEdit(Cell owner, string label, List<string> trace) : IOwnedEdit
        {
            public string Label => label;

            public void Undo() => trace.Add(UndoMark + label);

            public void Redo() => trace.Add(RedoMark + label);

            public bool Touches(object asked) => ReferenceEquals(asked, owner);
        }

        /// <summary>A step that moves a record from one id to another and says so.</summary>
        private sealed class RenamingEdit(string label, string from, string to) : IIdChangingEdit
        {
            public string Label => label;

            public void Undo()
            {
            }

            public void Redo()
            {
            }

            public IdSwap Swap(bool undoing) => undoing ? new IdSwap(to, from) : new IdSwap(from, to);
        }
    }
}
