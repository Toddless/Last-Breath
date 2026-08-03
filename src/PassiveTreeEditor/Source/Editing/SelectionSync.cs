namespace PassiveTreeEditor.Source.Editing
{
    using System.Collections.Generic;
    using Core.PassiveTree;
    using History;

    /// <summary>
    /// What the selection is after a step through the history. Two rules, both about what the author
    /// sees rather than about the data: a node whose id the step changed is still the node they had
    /// selected, and a node the step took out of the tree cannot stay selected.
    /// <para>The first rule is the one worth writing down. Undoing a rename leaves the tree exactly
    /// right and the panel showing "Nothing selected" unless the selection is carried across with the
    /// id — and a step that looks like it did nothing, on the one field the author was watching, is
    /// read as a broken undo.</para>
    /// <para>Plain C# beside the stack rather than inside the canvas: it is a rule about ids, and a
    /// rule buried in a Godot node is a rule nothing can read or test.</para>
    /// </summary>
    public static class SelectionSync
    {
        public static void Follow(HashSet<string> selection, PassiveTreeDocument document, IdSwap? rename)
        {
            if (rename is { } swap && selection.Remove(swap.From)) selection.Add(swap.To);

            selection.RemoveWhere(id => !document.Contains(id));
        }
    }
}
