namespace PassiveTreeEditor.Source.Editing.History
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The undo stack: two lists of commands and nothing else — it knows neither what a node is nor
    /// that a screen exists, which is what makes the one rule that matters here readable in one sitting.
    /// Everything it hands back was captured before the change it reverses, so a step back lands on the
    /// state the author actually had.
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

        /// <summary>The newest command is still taking keystrokes. Cleared by anything that ends a run:
        /// a step through the history, or the author leaving the field.</summary>
        private bool _open;

        /// <summary>The command that was newest when the file was last written, held to answer whether
        /// the tree on screen is still the tree on disk.</summary>
        private IEditCommand? _savedAt;

        /// <summary>The stack changed shape. What can be undone right now is the only thing outside
        /// that needs to hear about it.</summary>
        public event Action? Changed;

        public bool CanUndo => _done.Count > 0;

        public bool CanRedo => _undone.Count > 0;

        public int Depth => _done.Count;

        /// <summary>
        /// Whether the tree matches what was last written to disk. Stepping back to exactly the state
        /// that was saved makes it clean again, which is the whole reason this is asked of the stack
        /// rather than kept as a flag somebody sets on every edit: a flag can only ever be turned on.
        /// <para>The answer is conservative where it cannot be sure. A saved step that has fallen off
        /// the far end of a full stack can never be on top again, so the tree stays reported as
        /// changed — the direction of this mistake that costs nothing.</para>
        /// </summary>
        public bool IsClean => Newest() == _savedAt;

        /// <summary>The step an undo would take, for a tooltip or a status line.</summary>
        public string? NextUndo => CanUndo ? _done[^1].Label : null;

        public string? NextRedo => CanRedo ? _undone[^1].Label : null;

        /// <summary>
        /// Files a change that has already been applied. Recording after the fact rather than executing
        /// here is deliberate: a drag has to move nodes under the cursor to be a drag at all, and a
        /// command that re-applied what the author already did would be a second source of truth about
        /// what happened.
        /// </summary>
        public void Record(IEditCommand command)
        {
            // A new edit is a new branch: what was undone is no longer ahead of the author.
            _undone.Clear();

            if (_open && _done.Count > 0 && _done[^1] is IMergeableEdit top && top.TryAbsorb(command))
            {
                Changed?.Invoke();
                return;
            }

            _done.Add(command);
            _open = command is IMergeableEdit;

            // The oldest step goes rather than the newest: the far end of the stack is the part the
            // author has already stopped thinking about.
            if (_done.Count > MaxDepth) _done.RemoveAt(0);

            Changed?.Invoke();
        }

        /// <summary>Ends the run of keystrokes the newest command is taking, so the next edit starts a
        /// step of its own. Called when the author leaves a field or moves to another node — the two
        /// moments where "still typing the same thing" stops being true.</summary>
        public void Seal() => _open = false;

        /// <summary>
        /// The tree has just been written to disk: this is the state the file now holds. The open run
        /// is closed as part of it — a merged edit keeps its own identity while it absorbs keystrokes,
        /// so a run left open across a save would go on growing inside the very command that marks the
        /// saved state and report a changed tree as clean.
        /// </summary>
        public void MarkSaved()
        {
            _open = false;
            _savedAt = Newest();
            Changed?.Invoke();
        }

        public IEditCommand? Undo() => Step(_done, _undone, undoing: true);

        public IEditCommand? Redo() => Step(_undone, _done, undoing: false);

        public void Clear()
        {
            _done.Clear();
            _undone.Clear();
            _open = false;
            _savedAt = null;
            Changed?.Invoke();
        }

        private IEditCommand? Newest() => _done.Count > 0 ? _done[^1] : null;

        /// <summary>Moves the newest command from one side to the other and applies it in that
        /// direction. Undo and redo differ in nothing else, so they are not written twice.</summary>
        private IEditCommand? Step(List<IEditCommand> from, List<IEditCommand> to, bool undoing)
        {
            if (from.Count == 0) return null;

            IEditCommand command = from[^1];
            from.RemoveAt(from.Count - 1);

            if (undoing) command.Undo();
            else command.Redo();

            to.Add(command);
            _open = false;
            Changed?.Invoke();
            return command;
        }
    }
}
