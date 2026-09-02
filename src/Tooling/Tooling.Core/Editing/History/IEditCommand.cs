namespace Tooling.Editing.History
{
    /// <summary>
    /// One reversible change to the tree. A command carries the state it replaced and never a recipe
    /// for working that state out again: an undo that restores something the author did not have is
    /// worse than no undo at all — they press it, see a tree they do not recognise, and from then on
    /// can trust neither the file nor the tool.
    /// </summary>
    public interface IEditCommand
    {
        /// <summary>What the step was, for the status line. An undo the author cannot name is an undo
        /// they have to verify by eye before carrying on.</summary>
        string Label { get; }

        void Undo();

        void Redo();
    }
}
