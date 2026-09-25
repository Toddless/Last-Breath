namespace Core.PassiveTree.Context
{
    using Entity;
    using Interfaces;

    /// <summary>A context line of a taken node with its predicate resolved — the pair a knob's total is
    /// added from. No condition means it always counts, keeping an unconditional allocation as cheap to
    /// read as before conditions existed. The predicate is per line and per fighter (it holds the watched
    /// state), so it lives here rather than on the authored line shared by every reader.</summary>
    public sealed class TreeContextLine(ContextModifierLine line, ICondition? condition)
    {
        public ContextModifierLine Line { get; } = line;

        /// <summary>Whether the line counts towards its knob right now.</summary>
        public bool Counts => condition is null || condition.IsMet;

        public void Attach(IFightable owner) => condition?.Attach(owner);

        public void Detach() => condition?.Detach();
    }
}
