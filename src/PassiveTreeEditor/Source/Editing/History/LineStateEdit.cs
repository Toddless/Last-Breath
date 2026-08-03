namespace PassiveTreeEditor.Source.Editing.History
{
    using System;

    /// <summary>
    /// A modifier line put back the way it was, field for field. A line is snapshotted whole rather
    /// than one field at a time because a single gesture can move several of them at once — pointing a
    /// context line at a switch knob rewrites its bucket and its number along with its knob — and a
    /// command that remembered only the field the author touched would restore a line that never
    /// existed. The line object is never replaced, only refilled: rows, the switch-value memory and
    /// later commands in this stack all hold the line by identity.
    /// </summary>
    public sealed class LineStateEdit<TLine> : IMergeableEdit
        where TLine : class
    {
        private readonly TLine _line;
        private readonly TLine _before;
        private readonly Action<TLine, TLine> _assign;
        private readonly EditTarget _target;

        private TLine _after;
        private string _label;

        public LineStateEdit(TLine line, TLine before, TLine after, Action<TLine, TLine> assign, EditTarget target, string label)
        {
            _line = line;
            _before = before;
            _after = after;
            _assign = assign;
            _target = target;
            _label = label;
        }

        public string Label => _label;

        public void Undo() => _assign(_line, _before);

        public void Redo() => _assign(_line, _after);

        /// <summary>Takes the newer step's result and the name that goes with it, so that what the
        /// status line calls the step keeps describing the state the step actually lands on.</summary>
        public bool TryAbsorb(IEditCommand newer)
        {
            if (newer is not LineStateEdit<TLine> other || !other._target.Equals(_target)) return false;

            _after = other._after;
            _label = other._label;
            return true;
        }
    }
}
