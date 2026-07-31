namespace Core.Modifiers.Context
{
    using System;
    using System.Collections.Generic;
    using Enums;

    /// <summary>
    /// What the binding table says about a context knob, asked of the table itself instead of restated
    /// as a second list — so wiring a knob, dropping one, or changing what its binding does with the
    /// line's value needs no edit here. Two answers come out of it: whether the knob reaches a battle
    /// pipeline at all, and whether it is a switch or carries a number.
    /// Authoring leans on both: the editor offers only knobs that are wired and only the value types
    /// each one accepts, and the two readers — the tree file and the item pools — refuse a line the knob
    /// cannot take. An unbound knob throws the moment it is attached and a switch pinned onto a knob
    /// that expects an amount reaches battle as a bonus nobody authored; neither is a failure a data
    /// file may be able to plant.
    /// </summary>
    public static class ContextKnobs
    {
        private static readonly List<ContextParameter> s_bound = [];
        private static readonly List<ContextParameter> s_flags = [];

        static ContextKnobs() => Probe();

        /// <summary>The wired knobs, in declaration order — the pick list of the authoring tools.</summary>
        public static IReadOnlyList<ContextParameter> Bound => s_bound;

        /// <summary>The switch knobs: their binding reads no value, so such a line is on because it
        /// exists and carries no number to roll, scale or author.</summary>
        public static IReadOnlyList<ContextParameter> Flags => s_flags;

        /// <summary>Also false for a value outside the enum: a numeric "parameter" in a file parses into
        /// a member that does not exist, and it has no binding either.</summary>
        public static bool IsBound(ContextParameter parameter) => s_bound.Contains(parameter);

        public static bool IsFlag(ContextParameter parameter) => s_flags.Contains(parameter);

        /// <summary>Why the knob refuses this line, or null when it takes it — the single question both
        /// readers ask before they build one. A knob nobody wired refuses every line: it reaches no
        /// pipeline and throws the moment something attaches it. Beyond that a flag is a switch with no
        /// number behind it, so it belongs on the knobs whose binding reads no value and nowhere else:
        /// elsewhere it drops the authored amount and pins the knob to one — a full-strength bonus that
        /// only shows up once the line reaches a battle pipeline — while an amount written on a switch is
        /// a number nothing will ever read.</summary>
        public static string? WhyRefused(ContextParameter parameter, ModifierValueType valueType)
        {
            if (!IsBound(parameter)) return $"context parameter '{parameter}' reaches no battle pipeline";

            bool flagLine = valueType == ModifierValueType.Flag;
            if (flagLine == IsFlag(parameter)) return null;

            return flagLine
                ? $"context parameter '{parameter}' takes an amount — a flag line drops it and pins the knob to one"
                : $"context parameter '{parameter}' is a switch — a line written as {valueType} carries a number nothing reads";
        }

        /// <summary>Builds every knob's binding once and reads both facts off the attempt: whether a
        /// binding exists, and whether it took a view of the line's value. Nothing is attached to an
        /// entity, so the probe leaves no trace on anything.</summary>
        private static void Probe()
        {
            foreach (ContextParameter parameter in Enum.GetValues<ContextParameter>())
            {
                var views = new ContextModifierBindings.ValueViews();

                try
                {
                    ContextModifierBindings.Create(new ContextModifierEntry(parameter, ModifierValueType.Increase, 0f), views);
                }
                catch (NotSupportedException)
                {
                    // A knob nobody wired: it stays out of the list and out of every authoring surface.
                    continue;
                }

                s_bound.Add(parameter);
                if (!views.Taken) s_flags.Add(parameter);
            }
        }
    }
}
