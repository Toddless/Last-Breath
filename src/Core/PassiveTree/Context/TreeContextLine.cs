namespace Core.PassiveTree.Context
{
    using Entity;
    using Interfaces;

    /// <summary>
    /// A context line of a taken node with its predicate resolved: the pair a knob's total is added up
    /// from. A line that names no condition carries no predicate and always counts, which is what keeps
    /// an unconditional allocation exactly as expensive to read as it was before conditions existed.
    /// <para>The predicate is per line and per fighter — it holds the state of the one it watches — so it
    /// belongs here rather than on the authored line, which is shared by everyone reading the document.</para>
    /// </summary>
    public sealed class TreeContextLine(ContextModifierLine line, ICondition? condition)
    {
        public ContextModifierLine Line { get; } = line;

        /// <summary>Whether the line counts towards its knob right now.</summary>
        public bool Counts => condition is null || condition.IsMet;

        public void Attach(IFightable owner) => condition?.Attach(owner);

        public void Detach() => condition?.Detach();
    }
}
