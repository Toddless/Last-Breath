namespace Core.PassiveTree.Context
{
    using System.Collections.Generic;
    using Enums;
    using Interfaces;
    using Modifiers.Conditions;
    using Modifiers.Context;

    /// <summary>
    /// What an allocation says about the battle pipelines: the context lines of the taken nodes, sorted
    /// into the knob each one feeds, and what a knob's lines come to together. The single place both
    /// readers use — the fighter's contribution and the authoring tool's summary — so the number a panel
    /// shows and the number a battle applies cannot drift apart.
    /// </summary>
    public static class ContextKnobTotals
    {
        /// <summary>Every context line the taken nodes carry, grouped by the knob it feeds. The knob is
        /// the whole of the address: a binding is chosen by the parameter and reads the line's value,
        /// never its bucket, so lines of one knob written in different buckets are halves of one
        /// contribution — kept apart they would reach a pipeline as two modifiers applied one after the
        /// other. What a bucket still decides is the words the line is printed in, which is why it stays
        /// on the line and not here.
        /// <para>Ids the document does not know are skipped: an allocation is re-checked against the tree
        /// elsewhere, and a stale id here means no lines rather than a failure.</para>
        /// <para>Conditions are not looked at: every line is in the group whether it holds right now or
        /// not. This is what an allocation carries, which is the question an authoring tool asks — what
        /// of it counts at this moment is the overload below.</para></summary>
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

        /// <summary>The same grouping with every line's predicate resolved, for the reader that has a
        /// fighter to answer them against. A line naming a condition the catalog does not hold loses its
        /// place instead of counting unconditionally, and a knob left with no lines at all does not appear
        /// — the refusal has to cost the line, not the character.</summary>
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

        /// <summary>
        /// What a knob's lines are worth together. Plain addition: the pipeline reads one number per knob,
        /// so twenty nodes saying "+10%" have to arrive as +200% rather than as twenty steps compounding
        /// into a factor nobody authored. Whole-number knobs are no exception — the total is added up as a
        /// fraction and rounded once, where rounding each line on its own would swallow every fraction.
        /// <para>Answered when it is asked for rather than stored, so the total can account for the state
        /// of the moment without anything having to be re-attached or subscribed to.</para>
        /// </summary>
        public static float Sum(IEnumerable<ContextModifierLine> lines)
        {
            float total = 0f;
            foreach (ContextModifierLine line in lines) total += line.Value;

            return total;
        }

        /// <summary>The same addition over lines that carry a predicate, and the one place a condition is
        /// weighed: a line whose condition does not hold contributes nothing to this total and nothing at
        /// all needs re-attaching when it starts or stops holding — the sum is read afresh every time a
        /// pipeline asks for it.</summary>
        public static float Sum(IEnumerable<TreeContextLine> lines)
        {
            float total = 0f;
            foreach (TreeContextLine line in lines)
                if (line.Counts) total += line.Line.Value;

            return total;
        }

        /// <summary>What the knob is worth to a pipeline: the total, cut to a whole number where the knob
        /// is counted in whole units — its binding reads the floored value, so half a turn of bleed feeds
        /// nothing however the lines behind it were written. The tree's own modifier answers with this and
        /// so does the authoring summary, which is what stops a panel from crediting an allocation with a
        /// fraction no fight will ever see.</summary>
        public static float AsRead(ContextParameter parameter, IEnumerable<ContextModifierLine> lines) =>
            InUnitOf(parameter, Sum(lines));

        /// <summary>The same answer for lines that carry a predicate: only what counts right now is added
        /// up, and the total is cut the way the knob is counted.</summary>
        public static float AsRead(ContextParameter parameter, IEnumerable<TreeContextLine> lines) =>
            InUnitOf(parameter, Sum(lines));

        private static float InUnitOf(ContextParameter parameter, float total) =>
            ContextKnobs.IsWhole(parameter) ? (int)total : total;
    }
}
