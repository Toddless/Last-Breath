namespace Tooling.Editing.History
{
    /// <summary>
    /// A command that moves a node from one id to another. It has to say so, because an id is how the
    /// rest of the tool holds a node: whatever was pointing at the old one is pointing at nothing the
    /// moment the step lands, and dropping it silently is how an undo ends up looking broken while the
    /// data underneath it is perfectly right.
    /// </summary>
    public interface IIdChangingEdit : IEditCommand
    {
        /// <summary>Which id became which, in the direction the step is being taken.</summary>
        IdSwap Swap(bool undoing);
    }
}
