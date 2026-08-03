namespace PassiveTreeEditor.Source.Editing.History
{
    /// <summary>
    /// A command that swallows the one after it while the author is still making the same edit — typing
    /// into a title, walking a spin box with the arrows. A step per keystroke is useless to press and
    /// would push a session's real work out of a bounded stack; a merged run still holds the state from
    /// before the first keystroke, so nothing about it is approximate.
    /// </summary>
    public interface IMergeableEdit : IEditCommand
    {
        /// <summary>Takes over a newer command's result — and the label describing it — while keeping
        /// this one's original state. A run reports itself by where it has got to, because that is the
        /// only part of it the author can still see; the state it started from is what the step back
        /// restores and is never spoken about. False when the newer command is a different edit, which
        /// ends the run.</summary>
        bool TryAbsorb(IEditCommand newer);
    }
}
