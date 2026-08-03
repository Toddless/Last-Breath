namespace PassiveTreeEditor.Source.Editing.History
{
    using System.Collections.Generic;

    /// <summary>
    /// A modifier line added to a node or taken off it, at the exact place it sat. Index and instance
    /// both: a restored line has to come back where it was — the order of lines is the order the file
    /// keeps and the order the author reads — and it has to be the same line, because the row that was
    /// editing it and the commands recorded around it all hold it by identity.
    /// </summary>
    public sealed class LineListEdit<TLine>(IList<TLine> lines, int index, TLine line, bool adds, string label) : IEditCommand
    {
        public string Label => label;

        public void Undo()
        {
            if (adds) lines.RemoveAt(index);
            else lines.Insert(index, line);
        }

        public void Redo()
        {
            if (adds) lines.Insert(index, line);
            else lines.RemoveAt(index);
        }
    }
}
