namespace PassiveTreeEditor.Source.Editing.History
{
    using System;

    /// <summary>
    /// One field, set to one value, remembering the value it had. The old value is stored, never
    /// derived: a coordinate is the number the author dragged to, a title is the text they typed, and
    /// anything that recomputed either would be guessing at what was there.
    /// </summary>
    public sealed class ValueEdit<T> : IMergeableEdit
    {
        private readonly EditTarget _target;
        private readonly Action<T> _apply;
        private readonly T _before;

        private T _after;
        private string _label;

        public ValueEdit(EditTarget target, T before, T after, Action<T> apply, string label)
        {
            _target = target;
            _before = before;
            _after = after;
            _apply = apply;
            _label = label;
        }

        public string Label => _label;

        public void Undo() => _apply(_before);

        public void Redo() => _apply(_after);

        /// <summary>Takes the newer step's result and the name that goes with it. A label may spell out
        /// the value it lands on — "budget 63" — so keeping the first one would leave the status line
        /// naming a value the merged step no longer sets, and the author reading it after an undo would
        /// be told a number that was never restored.</summary>
        public bool TryAbsorb(IEditCommand newer)
        {
            if (newer is not ValueEdit<T> other || !other._target.Equals(_target)) return false;

            _after = other._after;
            _label = other._label;
            return true;
        }
    }
}
