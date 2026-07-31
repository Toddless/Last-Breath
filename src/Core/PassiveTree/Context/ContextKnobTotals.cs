namespace Core.PassiveTree.Context
{
    using System.Collections.Generic;
    using Enums;
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
        /// elsewhere, and a stale id here means no lines rather than a failure.</para></summary>
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

        /// <summary>What the knob is worth to a pipeline: the total, cut to a whole number where the knob
        /// is counted in whole units — its binding reads the floored value, so half a turn of bleed feeds
        /// nothing however the lines behind it were written. The tree's own modifier answers with this and
        /// so does the authoring summary, which is what stops a panel from crediting an allocation with a
        /// fraction no fight will ever see.</summary>
        public static float AsRead(ContextParameter parameter, IEnumerable<ContextModifierLine> lines)
        {
            float total = Sum(lines);

            return ContextKnobs.IsWhole(parameter) ? (int)total : total;
        }
    }
}
