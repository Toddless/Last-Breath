namespace Tooling.Editing.History
{
    /// <summary>
    /// A command that says which document it changed. One stack carries the whole tool — a json file, a
    /// locale, and the steps that touch both at once — while a save is asked of one file at a time: an
    /// edit that could not name what it changed would leave that question with no answer but "something,
    /// somewhere", and every file would be written on every save.
    /// </summary>
    public interface IOwnedEdit : IEditCommand
    {
        /// <summary>Whether this step changed <paramref name="owner"/>. Compared by reference, as
        /// <see cref="EditTarget"/> compares it: a document is the document it is, whatever its contents
        /// have become since.</summary>
        bool Touches(object owner);
    }
}
