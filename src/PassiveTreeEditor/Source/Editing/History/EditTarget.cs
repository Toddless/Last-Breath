namespace PassiveTreeEditor.Source.Editing.History
{
    /// <summary>
    /// Which field of which object an edit belongs to — the identity a run of keystrokes merges on.
    /// The owner is compared by reference on purpose: a node is the node it is whatever its id has
    /// become since, and two nodes holding identical values are still two nodes.
    /// </summary>
    public readonly record struct EditTarget(object Owner, string Field);
}
