namespace Tooling.Editing.History
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// The undo stack: two lists of commands and nothing else — it knows neither what a node is nor
    /// that a screen exists, which is what makes the one rule that matters here readable in one sitting.
    /// Everything it hands back was captured before the change it reverses, so a step back lands on the
    /// state the author actually had.
    /// <para>One stack carries a whole tool rather than one file: the author renames a record, writes a
    /// name into two locales and moves a line in a third file, and every one of those is the last thing
    /// he did. A stack per file would answer "the last thing you did in this file", which is a question
    /// nobody presses undo to ask. What was changed is the command's to say (<see cref="IOwnedEdit"/>),
    /// so a save still writes one file at a time.</para>
    /// </summary>
    public sealed class EditHistory
    {
        /// <summary>How many steps back the author can go. A node costs roughly ten steps to lay down
        /// and fill in, so this covers about twenty nodes of work — deeper than a mistake is ever
        /// noticed, and bounded because every step keeps alive the objects it took out of the tree.
        /// </summary>
        public const int MaxDepth = 200;

        private readonly List<IEditCommand> _done = [];
        private readonly List<IEditCommand> _undone = [];

        /// <summary>The commands of the step being composed now, if one is open.</summary>
        private readonly List<IEditCommand> _grouped = [];

        /// <summary>The newest command of each document as it was when that document was last written,
        /// held to answer whether the file on screen is still the file on disk. Keyed by reference, the
        /// way an edit names what it changed.</summary>
        private readonly Dictionary<object, IEditCommand?> _savedFor = new(ReferenceEqualityComparer.Instance);

        /// <summary>The newest command is still taking keystrokes. Cleared by anything that ends a run:
        /// a step through the history, or the author leaving the field.</summary>
        private bool _open;

        /// <summary>How many times the step being composed has been opened. A step opened inside one
        /// already open is the same step: a gesture made of gestures is still one thing the author did,
        /// and the outermost of them is the one that names it.</summary>
        private int _groupDepth;

        private string _groupLabel = string.Empty;

        /// <summary>The stack changed shape. What can be undone right now is the only thing outside
        /// that needs to hear about it.</summary>
        public event Action? Changed;

        public bool CanUndo => _done.Count > 0;

        public bool CanRedo => _undone.Count > 0;

        public int Depth => _done.Count;

        /// <summary>The step an undo would take, for a tooltip or a status line.</summary>
        public string? NextUndo => CanUndo ? _done[^1].Label : null;

        public string? NextRedo => CanRedo ? _undone[^1].Label : null;

        /// <summary>
        /// Opens a step several commands go into: everything recorded until it is closed is one thing to
        /// take back, undone in the order that reverses it and redone in the order it happened. A record
        /// renamed is an id in one file and a key in every locale, and an author who took back one of
        /// those would be left with a record whose name is read under one word and whose wording under
        /// another.
        /// <para>A step nothing was recorded into is no step at all. Nothing may be stepped through while
        /// one is open: half a gesture is a state the author never had.</para>
        /// </summary>
        public IDisposable Group(string label) => Open(label, withNewest: false);

        /// <summary>Opens a step that takes the newest one already filed with it. What a change means
        /// elsewhere is known only after it has been made — the keys a record's wording is written under
        /// are named again once the run of keystrokes over its id is over — and the two are one thing the
        /// author did.</summary>
        public IDisposable GroupWithNewest(string label) => Open(label, withNewest: true);

        /// <summary>
        /// Files a change that has already been applied. Recording after the fact rather than executing
        /// here is deliberate: a drag has to move nodes under the cursor to be a drag at all, and a
        /// command that re-applied what the author already did would be a second source of truth about
        /// what happened.
        /// </summary>
        public void Record(IEditCommand command)
        {
            ArgumentNullException.ThrowIfNull(command);

            // A new edit is a new branch: what was undone is no longer ahead of the author.
            _undone.Clear();

            // Inside an open step nothing is merged: a step holds exactly the changes its gesture made,
            // and they are taken back together whether or not two of them wrote the same field.
            if (_groupDepth > 0)
            {
                _grouped.Add(command);
                Changed?.Invoke();
                return;
            }

            if (_open && _done.Count > 0 && _done[^1] is IMergeableEdit top && top.TryAbsorb(command))
            {
                Changed?.Invoke();
                return;
            }

            _open = command is IMergeableEdit;
            Push(command);
        }

        /// <summary>Ends the run of keystrokes the newest command is taking, so the next edit starts a
        /// step of its own. Called when the author leaves a field or moves to another node — the two
        /// moments where "still typing the same thing" stops being true.</summary>
        public void Seal() => _open = false;

        /// <summary>
        /// One document has just been written to disk: this is the state its file now holds. The open run
        /// is closed as part of it — a merged edit keeps its own identity while it absorbs keystrokes,
        /// so a run left open across a save would go on growing inside the very command that marks the
        /// saved state and report a changed file as clean.
        /// </summary>
        public void MarkSaved(object owner)
        {
            ArgumentNullException.ThrowIfNull(owner);

            _open = false;
            _savedFor[owner] = NewestFor(owner);
            Changed?.Invoke();
        }

        /// <summary>
        /// Whether a document still holds what was last written to disk. Stepping back to exactly the
        /// state that was saved makes it clean again, which is the whole reason this is asked of the
        /// stack rather than kept as a flag somebody sets on every edit: a flag can only ever be turned
        /// on.
        /// <para>The answer is conservative where it cannot be sure. A saved step that has fallen off
        /// the far end of a full stack can never be on top again, so the file stays reported as changed
        /// — the direction of this mistake that costs nothing.</para>
        /// </summary>
        public bool IsCleanFor(object owner)
        {
            ArgumentNullException.ThrowIfNull(owner);

            return NewestFor(owner) == _savedFor.GetValueOrDefault(owner);
        }

        /// <summary>Takes over the steps of another stack, oldest first, and leaves it empty. A file the
        /// run lays down is written into before it joins the tool, and the step that wrote it has to
        /// arrive with it or the one gesture that cannot be taken back is the one that created a
        /// file.</summary>
        public void Take(EditHistory other)
        {
            ArgumentNullException.ThrowIfNull(other);

            if (ReferenceEquals(other, this)) return;

            foreach (IEditCommand command in other._done) Push(command);

            other.Clear();
        }

        public IEditCommand? Undo() => Step(_done, _undone, undoing: true);

        public IEditCommand? Redo() => Step(_undone, _done, undoing: false);

        public void Clear()
        {
            _done.Clear();
            _undone.Clear();
            _grouped.Clear();
            _savedFor.Clear();
            _groupDepth = 0;
            _open = false;
            Changed?.Invoke();
        }

        /// <summary>The newest step that changed this document, or nothing when the stack holds none.
        /// A step made of several commands answers for every document any of them touched.</summary>
        private IEditCommand? NewestFor(object owner)
        {
            for (int at = _done.Count - 1; at >= 0; at--)
                if (_done[at] is IOwnedEdit owned && owned.Touches(owner))
                    return _done[at];

            return null;
        }

        /// <summary>Opens the step commands are gathered into. Opened inside one already open it is the
        /// same step, so a gesture that calls another does not split into two things to take back.</summary>
        private IDisposable Open(string label, bool withNewest)
        {
            ArgumentNullException.ThrowIfNull(label);

            if (_groupDepth++ > 0) return new OpenStep(this);

            _groupLabel = label;
            _open = false;

            if (withNewest && _done.Count > 0)
            {
                _grouped.Add(_done[^1]);
                _done.RemoveAt(_done.Count - 1);
            }

            return new OpenStep(this);
        }

        /// <summary>Files what the step gathered, under the name the gesture gave it. A step that
        /// gathered nothing is not filed: the author pressed something that changed nothing, and an undo
        /// that took back nothing would read as the key having been missed.</summary>
        private void Close()
        {
            if (--_groupDepth > 0) return;
            if (_grouped.Count == 0) return;

            var step = new GroupEdit(_groupLabel, [.. _grouped]);

            _grouped.Clear();
            _open = false;
            Push(step);
        }

        private void Push(IEditCommand command)
        {
            _done.Add(command);

            // The oldest step goes rather than the newest: the far end of the stack is the part the
            // author has already stopped thinking about.
            if (_done.Count > MaxDepth) _done.RemoveAt(0);

            Changed?.Invoke();
        }

        /// <summary>Moves the newest command from one side to the other and applies it in that
        /// direction. Undo and redo differ in nothing else, so they are not written twice.</summary>
        private IEditCommand? Step(List<IEditCommand> from, List<IEditCommand> to, bool undoing)
        {
            if (_groupDepth > 0 || from.Count == 0) return null;

            IEditCommand command = from[^1];
            from.RemoveAt(from.Count - 1);

            if (undoing) command.Undo();
            else command.Redo();

            to.Add(command);
            _open = false;
            Changed?.Invoke();
            return command;
        }

        /// <summary>The step being composed, held open for as long as the gesture making it lasts.</summary>
        private sealed class OpenStep(EditHistory history) : IDisposable
        {
            public void Dispose() => history.Close();
        }

        /// <summary>Several changes as one step. Undone backwards and redone forwards: the commands were
        /// applied in order and each of them holds the state it replaced, so anything else would restore
        /// a state made of two halves.</summary>
        private sealed class GroupEdit(string label, IReadOnlyList<IEditCommand> steps) : IOwnedEdit
        {
            public string Label => label;

            public void Undo()
            {
                for (int at = steps.Count - 1; at >= 0; at--) steps[at].Undo();
            }

            public void Redo()
            {
                foreach (IEditCommand step in steps) step.Redo();
            }

            /// <summary>Every document any part of it changed: a rename writes an id in one file and a
            /// key in every locale, and all of them are waiting to be saved because of this one step.</summary>
            public bool Touches(object owner)
            {
                foreach (IEditCommand step in steps)
                    if (step is IOwnedEdit owned && owned.Touches(owner))
                        return true;

                return false;
            }
        }
    }
}
