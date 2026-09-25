namespace Core.PassiveTree.Context
{
    using System.Collections.Generic;
    using Enums;
    using Interfaces;
    using Modifiers.Conditions;
    using Modifiers.Context;

    /// <summary>
    /// What an allocation says about the battle pipelines: context lines of the taken nodes grouped by the
    /// knob each feeds, and what a knob's lines total. Shared by the fighter's contribution and the
    /// authoring tool's summary, so the number a panel shows and the number a battle applies can't drift
    /// apart.
    /// </summary>
    public static class ContextKnobTotals
    {
        /// <summary>Every context line the taken nodes carry, grouped by the knob it feeds. The knob is
        /// the whole address — bindings read by parameter and the line's value, never its bucket, so
        /// lines of one knob written in different buckets are one contribution (kept apart they'd reach a
        /// pipeline as two modifiers applied in sequence); bucket only decides the line's wording, so it
        /// stays on the line.
        /// <para>Unknown ids are skipped (allocation is re-checked elsewhere). Conditions aren't
        /// evaluated — this is what the allocation carries; what counts right now is the overload
        /// below.</para></summary>
        public static Dictionary<ContextParameter, List<ContextModifierLine>> Gather(
            PassiveTreeDocument document,
            IEnumerable<string> taken)
        {
            Dictionary<ContextParameter, List<ContextModifierLine>> knobs = [];

            foreach (string id in taken)
            {
                PassiveNode? node = document.Find(id);
                if (node is null) continue;

                foreach (ContextModifierLine line in node.ContextModifiers)
                {
                    if (!knobs.TryGetValue(line.Parameter, out List<ContextModifierLine>? lines))
                    {
                        lines = [];
                        knobs[line.Parameter] = lines;
                    }

                    lines.Add(line);
                }
            }

            return knobs;
        }

        /// <summary>The same grouping with every line's predicate resolved, for a reader with a fighter to
        /// answer them against. A line naming an unresolvable condition drops out rather than counting
        /// unconditionally, and a knob left with no lines doesn't appear.</summary>
        public static Dictionary<ContextParameter, List<TreeContextLine>> Gather(
            PassiveTreeDocument document,
            IEnumerable<string> taken,
            IConditionProvider conditions)
        {
            Dictionary<ContextParameter, List<TreeContextLine>> knobs = [];

            foreach (KeyValuePair<ContextParameter, List<ContextModifierLine>> pair in Gather(document, taken))
            {
                List<TreeContextLine> lines = [];
                foreach (ContextModifierLine line in pair.Value)
                    if (conditions.TryResolve(line.Condition, out ICondition? condition))
                        lines.Add(new TreeContextLine(line, condition));

                if (lines.Count > 0) knobs[pair.Key] = lines;
            }

            return knobs;
        }

        /// <summary>What a knob's lines are worth together: plain addition, since the pipeline reads one
        /// number per knob (twenty "+10%" lines must arrive as +200%, not compound). Whole-number knobs
        /// add as a fraction and round once, where rounding each line separately would swallow the
        /// fractions. Answered on demand rather than stored, so the total reflects the current state
        /// without anything needing to be re-attached.</summary>
        public static float Sum(IEnumerable<ContextModifierLine> lines)
        {
            float total = 0f;
            foreach (ContextModifierLine line in lines) total += line.Value;

            return total;
        }

        /// <summary>Same addition over lines with predicates — the one place a condition is weighed. A
        /// line whose condition doesn't hold contributes nothing, and nothing needs re-attaching when it
        /// starts or stops holding since the sum is read afresh each time.</summary>
        public static float Sum(IEnumerable<TreeContextLine> lines)
        {
            float total = 0f;
            foreach (TreeContextLine line in lines)
                if (line.Counts) total += line.Line.Value;

            return total;
        }

        /// <summary>What the knob is worth to a pipeline: the total, floored where the knob counts in
        /// whole units (its binding reads the floored value, so half a turn of bleed feeds nothing however
        /// the lines were written). Both the tree's own modifier and the authoring summary use this, so a
        /// panel can't credit a fraction no fight will ever see.</summary>
        public static float AsRead(ContextParameter parameter, IEnumerable<ContextModifierLine> lines) =>
            InUnitOf(parameter, Sum(lines));

        /// <summary>Same answer for lines with predicates: only what counts right now is summed, then cut
        /// the way the knob is counted.</summary>
        public static float AsRead(ContextParameter parameter, IEnumerable<TreeContextLine> lines) =>
            InUnitOf(parameter, Sum(lines));

        private static float InUnitOf(ContextParameter parameter, float total) =>
            ContextKnobs.IsWhole(parameter) ? (int)total : total;
    }
}
