namespace PassiveTreeEditor.Source.Editing
{
    using System;
    using Core.PassiveTree;

    /// <summary>
    /// Copying one modifier line over another, field for field, for both channels, and telling two
    /// snapshots apart. The line object is never swapped out — a restored line is the same line wearing
    /// its old values, which is what lets the undo stack, the inspector rows and the switch-value
    /// memory keep addressing it by identity.
    /// </summary>
    public static class LineSnapshots
    {
        /// <summary>Whether a gesture left the line as it found it. Field for field against the same ones
        /// the snapshot copies: a comparison over fewer of them would let a real edit past as a
        /// no-op, which is the one direction of this mistake that loses work.</summary>
        public static bool Same(ModifierLine first, ModifierLine second) =>
            first.Parameter == second.Parameter
            && first.ValueType == second.ValueType
            && first.Value.Equals(second.Value)
            && first.PerParameter == second.PerParameter
            && string.Equals(first.Condition, second.Condition, StringComparison.Ordinal);

        public static bool Same(ContextModifierLine first, ContextModifierLine second) =>
            first.Parameter == second.Parameter
            && first.ValueType == second.ValueType
            && first.Value.Equals(second.Value)
            && string.Equals(first.Condition, second.Condition, StringComparison.Ordinal);

        /// <summary>The per-unit carrier travels with the rest: the tool authors no field for it, so a copy
        /// that dropped it would quietly strip a hand-written line the first time the node was edited.</summary>
        public static void Assign(ModifierLine target, ModifierLine source)
        {
            target.Parameter = source.Parameter;
            target.ValueType = source.ValueType;
            target.Value = source.Value;
            target.PerParameter = source.PerParameter;
            target.Condition = source.Condition;
        }

        /// <summary>A snapshot of a line that is currently a switch carries the pinned one, not the
        /// number typed before the knob turned it into a switch — that number is kept by
        /// <see cref="ContextLineValues"/> and comes back with the knob, not from here.</summary>
        public static void Assign(ContextModifierLine target, ContextModifierLine source)
        {
            target.Parameter = source.Parameter;
            target.ValueType = source.ValueType;
            target.Value = source.Value;
            target.Condition = source.Condition;
        }
    }
}
