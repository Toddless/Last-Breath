namespace PassiveTreeEditor.Source.Editing.History
{
    using System;
    using System.Collections.Generic;
    using Core.PassiveTree;
    using Core.PassiveTree.Editing;

    /// <summary>
    /// One field of several nodes, set together and taken back together. A group edit is one thing the
    /// author did — he had five nodes selected and typed a title once — so it has to be one thing he can
    /// undo; five steps for one gesture would leave him pressing Ctrl+Z until the tree looked right, which
    /// is the same as having no undo at all.
    /// <para>Every node's own previous value travels in the plan. The five titles that became one were five
    /// different titles a moment earlier, and a step back that wrote any one of them into all five would be
    /// an undo handing the author a tree he never had.</para>
    /// </summary>
    public sealed class BatchEdit<T> : IMergeableEdit
    {
        private readonly EditTarget _target;
        private readonly List<NodeValueChange<T>> _changes;
        private readonly Action<PassiveNode, T> _write;

        private string _label;

        public BatchEdit(EditTarget target, List<NodeValueChange<T>> changes, Action<PassiveNode, T> write, string label)
        {
            _target = target;
            _changes = changes;
            _write = write;
            _label = label;
        }

        public string Label => _label;

        public void Undo() => NodeBatch.Apply(_changes, _write, forward: false);

        public void Redo() => NodeBatch.Apply(_changes, _write, forward: true);

        /// <summary>Takes over a newer batch on the same field of the same selection, keeping what each
        /// node started from. A node the earlier batch left out — it already held what was being typed —
        /// joins the run with its own state, so leaving the field is one step whichever nodes each keystroke
        /// happened to move.</summary>
        public bool TryAbsorb(IEditCommand newer)
        {
            if (newer is not BatchEdit<T> other || !other._target.Equals(_target)) return false;

            NodeBatch.Absorb(_changes, other._changes);
            _label = other._label;
            return true;
        }
    }
}
