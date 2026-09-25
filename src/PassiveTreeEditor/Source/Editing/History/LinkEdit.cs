namespace PassiveTreeEditor.Source.Editing.History
{
    using Core.PassiveTree;

    /// <summary>
    /// One edge made or broken. Ends are read off the node objects at the moment of the step rather
    /// than copied as ids, so an edge follows a rename exactly the way the document's own edges do.
    /// </summary>
    public sealed class LinkEdit(
        PassiveTreeDocument document,
        PassiveNode first,
        PassiveNode second,
        bool links,
        string label) : IEditCommand
    {
        public string Label => label;

        public void Undo() => Set(!links);

        public void Redo() => Set(links);

        private void Set(bool linked)
        {
            if (linked) document.Link(first.Id, second.Id);
            else document.Unlink(first.Id, second.Id);
        }
    }
}
