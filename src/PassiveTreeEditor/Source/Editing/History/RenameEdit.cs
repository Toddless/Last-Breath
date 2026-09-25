namespace PassiveTreeEditor.Source.Editing.History
{
    using Core.PassiveTree;

    /// <summary>
    /// A node's id changed back and forth. The document's own rename carries every edge over with it,
    /// so reversing one is the same call in the other direction and never a rebuild of the edges.
    /// <para>The one edit that changes how a node is addressed, and therefore the one that has to
    /// report what it did: whoever still holds the previous id — the selection above all — is holding
    /// an id the tree no longer has.</para>
    /// </summary>
    public sealed class RenameEdit(PassiveTreeDocument document, string from, string to) : IIdChangingEdit
    {
        public string Label => $"rename {from} — {to}";

        public void Undo() => document.Rename(to, from);

        public void Redo() => document.Rename(from, to);

        public IdSwap Swap(bool undoing) => undoing ? new IdSwap(to, from) : new IdSwap(from, to);
    }
}
